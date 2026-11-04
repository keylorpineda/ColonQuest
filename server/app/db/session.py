"""Async engine and session factory for request handlers."""

from collections.abc import AsyncIterator
from functools import lru_cache

from sqlalchemy.engine import make_url
from sqlalchemy.ext.asyncio import (
    AsyncEngine,
    AsyncSession,
    async_sessionmaker,
    create_async_engine,
)

from app.config import get_settings
from app.db.base import Base


def normalize_url(url: str) -> str:
    """Use the psycopg 3 driver even when given a plain postgresql:// URL (e.g. from Neon)."""
    parsed = make_url(url)
    if parsed.drivername in {"postgresql", "postgres"}:
        parsed = parsed.set(drivername="postgresql+psycopg")
    return parsed.render_as_string(hide_password=False)


def is_sqlite(url: str) -> bool:
    return make_url(url).get_backend_name() == "sqlite"


@lru_cache
def get_engine() -> AsyncEngine:
    settings = get_settings()
    if not settings.database_url:
        raise RuntimeError("DATABASE_URL is not configured")
    url = normalize_url(settings.database_url)
    if is_sqlite(url):
        # Background writers (the year planner) can overlap a request: wait instead of failing.
        return create_async_engine(url, connect_args={"timeout": 30})
    return create_async_engine(
        url,
        pool_pre_ping=True,  # Neon suspends idle computes
        pool_size=5,
        max_overflow=5,
    )


@lru_cache
def get_sessionmaker() -> async_sessionmaker[AsyncSession]:
    return async_sessionmaker(get_engine(), expire_on_commit=False)


async def prepare_local_database() -> None:
    """Create the tables of a local SQLite file; Postgres gets its schema from the migrations."""
    url = get_settings().database_url
    if url and is_sqlite(url):
        async with get_engine().begin() as connection:
            await connection.run_sync(Base.metadata.create_all)


async def get_session() -> AsyncIterator[AsyncSession]:
    """FastAPI dependency that yields a session per request."""
    async with get_sessionmaker()() as session:
        yield session

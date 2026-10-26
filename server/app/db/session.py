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


def normalize_url(url: str) -> str:
    """Use the psycopg 3 driver even when given a plain postgresql:// URL (e.g. from Neon)."""
    parsed = make_url(url)
    if parsed.drivername in {"postgresql", "postgres"}:
        parsed = parsed.set(drivername="postgresql+psycopg")
    return parsed.render_as_string(hide_password=False)


@lru_cache
def get_engine() -> AsyncEngine:
    settings = get_settings()
    if not settings.database_url:
        raise RuntimeError("DATABASE_URL is not configured")
    return create_async_engine(
        normalize_url(settings.database_url),
        pool_pre_ping=True,  # Neon suspends idle computes
        pool_size=5,
        max_overflow=5,
    )


@lru_cache
def get_sessionmaker() -> async_sessionmaker[AsyncSession]:
    return async_sessionmaker(get_engine(), expire_on_commit=False)


async def get_session() -> AsyncIterator[AsyncSession]:
    """FastAPI dependency that yields a session per request."""
    async with get_sessionmaker()() as session:
        yield session

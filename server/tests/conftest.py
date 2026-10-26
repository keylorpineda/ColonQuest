import asyncio
import os
import sys
from collections.abc import AsyncIterator
from pathlib import Path
from typing import Any

# Tests must never reach real services configured in a developer's server/.env (Neon, Meta).
# Environment variables take precedence over the .env file in pydantic-settings.
os.environ["DATABASE_URL"] = ""
os.environ["MODEL_API_KEY"] = ""

import httpx2
import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy.ext.asyncio import AsyncSession, create_async_engine

from app.db.session import normalize_url

TEST_DATABASE_URL = os.environ.get("TEST_DATABASE_URL", "")
SERVER_DIR = Path(__file__).resolve().parents[1]

requires_db = pytest.mark.skipif(not TEST_DATABASE_URL, reason="TEST_DATABASE_URL not set")


@pytest.fixture
def anyio_backend() -> str | tuple[str, dict[str, Any]]:
    # Async psycopg cannot run on Windows' default ProactorEventLoop.
    if sys.platform == "win32":
        return "asyncio", {"loop_factory": asyncio.SelectorEventLoop}
    return "asyncio"


@pytest.fixture(scope="session")
def migrated_database() -> str:
    """Apply migrations once to the disposable test database."""
    if not TEST_DATABASE_URL:
        pytest.skip("TEST_DATABASE_URL not set")
    url = normalize_url(TEST_DATABASE_URL)
    config = Config(str(SERVER_DIR / "alembic.ini"))
    config.set_main_option("script_location", str(SERVER_DIR / "migrations"))
    config.set_main_option("sqlalchemy.url", url.replace("%", "%%"))
    command.upgrade(config, "head")
    return url


@pytest.fixture
async def db(migrated_database: str) -> AsyncIterator[AsyncSession]:
    """A session whose changes are rolled back after each test."""
    engine = create_async_engine(migrated_database)
    async with engine.connect() as connection:
        transaction = await connection.begin()
        session = AsyncSession(
            bind=connection,
            expire_on_commit=False,
            join_transaction_mode="create_savepoint",
        )
        try:
            yield session
        finally:
            await session.close()
            await transaction.rollback()
    await engine.dispose()


@pytest.fixture
async def api(db: AsyncSession) -> AsyncIterator[httpx2.AsyncClient]:
    """HTTP client for the app with the request session bound to the test transaction."""
    from app.api.deps import get_optional_session
    from app.db.session import get_session
    from app.main import create_app

    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_optional_session] = override_session
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as client:
        yield client

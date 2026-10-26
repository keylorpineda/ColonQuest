"""Shared FastAPI dependencies."""

from collections.abc import AsyncIterator
from typing import Annotated

from fastapi import Depends, Request
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import Settings, get_settings
from app.cost_guard import client_hash
from app.db.session import get_session, get_sessionmaker

DbSession = Annotated[AsyncSession, Depends(get_session)]
AppSettings = Annotated[Settings, Depends(get_settings)]


async def get_optional_session() -> AsyncIterator[AsyncSession | None]:
    """A session when DATABASE_URL is configured, otherwise None (used by /health)."""
    if not get_settings().database_url:
        yield None
        return
    async with get_sessionmaker()() as session:
        yield session


OptionalDbSession = Annotated[AsyncSession | None, Depends(get_optional_session)]


def client_ip(request: Request) -> str:
    """Client IP behind Render's proxy: first X-Forwarded-For entry, else the socket peer."""
    forwarded = request.headers.get("x-forwarded-for", "")
    if forwarded:
        return forwarded.split(",")[0].strip()
    return request.client.host if request.client else "unknown"


def get_client_hash(request: Request, settings: AppSettings) -> str:
    return client_hash(client_ip(request), settings.client_hash_salt.get_secret_value())


ClientHash = Annotated[str, Depends(get_client_hash)]

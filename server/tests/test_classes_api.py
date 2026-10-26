import uuid

import pytest
from httpx2 import AsyncClient
from sqlalchemy.ext.asyncio import AsyncSession

from app.db import GameSession, SchoolClass

pytestmark = pytest.mark.anyio


async def test_create_class_and_join_it(api: AsyncClient, db: AsyncSession) -> None:
    created = await api.post("/api/classes", json={"name": "Finanzas 10-1"})

    assert created.status_code == 201
    code = created.json()["code"]
    stored = await db.get(SchoolClass, code)
    assert stored is not None and stored.name == "Finanzas 10-1"

    joined = await api.post("/api/sessions", json={"classCode": code})
    assert joined.status_code == 201
    game_session = await db.get(GameSession, uuid.UUID(joined.json()["sessionId"]))
    assert game_session is not None and game_session.class_code == code


async def test_class_name_is_required(api: AsyncClient) -> None:
    response = await api.post("/api/classes", json={"name": ""})
    assert response.status_code == 422

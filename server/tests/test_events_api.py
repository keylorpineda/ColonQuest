import random
import uuid
from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.events import get_game_master
from app.config import Settings
from app.db import Decision, ModelCall
from app.db.session import get_session
from app.game_master.service import GameMaster
from app.main import create_app
from app.providers.base import ProviderUnavailableError
from app.response_cache import TTLCache

from .fakes import FakeProvider, fail, ok

pytestmark = pytest.mark.anyio

STATE: dict[str, Any] = {
    "month": 3,
    "salaryNet": 450000,
    "cash": 120000,
    "savings": 50000,
    "emergencyFund": 0,
    "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "wellbeing": 60,
}


@pytest.fixture
def provider() -> FakeProvider:
    return FakeProvider()


@pytest.fixture
async def client(db: AsyncSession, provider: FakeProvider) -> AsyncIterator[httpx2.AsyncClient]:
    settings = Settings(_env_file=None, game_master_model="muse-wonder-1.0")  # type: ignore[call-arg]
    game_master = GameMaster(provider, settings, TTLCache(10, 60), rng=random.Random(0))
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_game_master] = lambda: game_master
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def new_session(client: httpx2.AsyncClient) -> str:
    response = await client.post("/api/sessions", json={})
    session_id: str = response.json()["sessionId"]
    return session_id


async def test_model_event(
    client: httpx2.AsyncClient, provider: FakeProvider, db: AsyncSession
) -> None:
    provider.steps.append(ok())
    session_id = await new_session(client)

    response = await client.post(f"/api/sessions/{session_id}/event", json=STATE)

    assert response.status_code == 200
    assert response.headers["X-Event-Source"] == "model"
    assert response.json()["eventId"] == "evt_llanta_ponchada"
    calls = await db.scalar(
        select(func.count()).where(ModelCall.session_id == uuid.UUID(session_id))
    )
    assert calls == 1


async def test_provider_down_still_returns_an_event(
    client: httpx2.AsyncClient, provider: FakeProvider
) -> None:
    provider.steps.append(fail(ProviderUnavailableError("down")))
    session_id = await new_session(client)

    response = await client.post(f"/api/sessions/{session_id}/event", json=STATE)

    assert response.status_code == 200
    assert response.headers["X-Event-Source"] == "fallback"
    assert response.json()["choices"]


async def test_previous_decisions_feed_the_prompt(
    client: httpx2.AsyncClient, provider: FakeProvider, db: AsyncSession
) -> None:
    provider.steps.append(ok())
    session_id = await new_session(client)
    db.add(
        Decision(
            session_id=uuid.UUID(session_id),
            month=2,
            event_id="evt_moto_dañada",
            teaching_concept="emergency_fund",
            choice_id="pay_cash",
            state_after=STATE | {"month": 2},
        )
    )
    await db.flush()

    await client.post(f"/api/sessions/{session_id}/event", json=STATE)

    user_prompt = provider.calls[0]["user"]
    assert '"avoid":["evt_moto_dañada"]' in user_prompt
    assert '"emergency_fund"' not in user_prompt.split('"notTaughtYet"')[1]


async def test_unknown_session_and_invalid_state(client: httpx2.AsyncClient) -> None:
    unknown = await client.post(f"/api/sessions/{uuid.uuid4()}/event", json=STATE)
    assert unknown.status_code == 404

    session_id = await new_session(client)
    bad = await client.post(f"/api/sessions/{session_id}/event", json=STATE | {"cash": -5})
    assert bad.status_code == 422


async def test_agentic_game_master_reads_stored_history(db: AsyncSession) -> None:
    import json

    from app.game_master.agentic import AgenticGameMaster

    from .fakes import MODEL_EVENT, FakeToolProvider, final_turn, tool_turn

    settings = Settings(  # type: ignore[call-arg]
        _env_file=None, game_master_model="muse-wonder-1.0", agentic_game_master=True
    )
    tool_provider = FakeToolProvider(
        tool_turn(("get_player_history", {})), final_turn(json.dumps(MODEL_EVENT))
    )
    agentic = AgenticGameMaster(
        GameMaster(FakeProvider(), settings, TTLCache(10, 60)), tool_provider, settings
    )
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_game_master] = lambda: agentic
    async with httpx2.AsyncClient(
        transport=httpx2.ASGITransport(app=app), base_url="http://test"
    ) as http:
        session_id = (await http.post("/api/sessions", json={})).json()["sessionId"]
        db.add(
            Decision(
                session_id=uuid.UUID(session_id),
                month=2,
                event_id="evt_moto_dañada",
                teaching_concept="emergency_fund",
                choice_id="pay_card",
                state_after=STATE | {"month": 2},
            )
        )
        await db.flush()
        response = await http.post(f"/api/sessions/{session_id}/event", json=STATE)

    assert response.status_code == 200
    assert response.headers["X-Event-Source"] == "agent"
    history_result = tool_provider.calls[1]["messages"][3]["content"]
    assert '"choiceId": "pay_card"' in history_result
    assert '"month": 2' in history_result

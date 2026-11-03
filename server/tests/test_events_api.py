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


async def test_season_plan_is_stored_and_adjusted(db: AsyncSession) -> None:
    from contextlib import asynccontextmanager

    from app.api.events import get_session_factory
    from app.config import get_settings
    from app.db import GameSession
    from app.game_master.planner import SeasonPlanner
    from app.game_master.season import SeasonPlan, default_plan
    from app.providers.base import ProviderResponseError
    from app.schemas import PlayerState

    settings = Settings(  # type: ignore[call-arg]
        _env_file=None, game_master_model="muse-wonder-1.0", season_planner=True
    )
    provider = FakeProvider(ok(), ok())
    game_master = GameMaster(provider, settings, TTLCache(10, 60), rng=random.Random(0))
    app = create_app()
    app.state.season_planner = SeasonPlanner(
        FakeProvider(fail(ProviderResponseError("bad plan"))), settings
    )

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    @asynccontextmanager
    async def background_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_settings] = lambda: settings
    app.dependency_overrides[get_game_master] = lambda: game_master
    app.dependency_overrides[get_session_factory] = lambda: background_session
    async with httpx2.AsyncClient(
        transport=httpx2.ASGITransport(app=app), base_url="http://test"
    ) as http:
        session_id = (await http.post("/api/sessions", json={})).json()["sessionId"]
        first = await http.post(f"/api/sessions/{session_id}/event", json=STATE)

        expected = default_plan(PlayerState.model_validate(STATE))
        stored = await db.get(GameSession, uuid.UUID(session_id))
        assert stored is not None
        assert SeasonPlan.model_validate(stored.season_plan) == expected
        focus = expected.for_month(3)
        assert f'"teachingConcept":"{focus.teaching_concept.value}"' in provider.calls[0]["user"]

        card = STATE["creditCard"]
        for month, balance in ((3, 0), (4, 150000)):
            db.add(
                Decision(
                    session_id=uuid.UUID(session_id),
                    month=month,
                    event_id=f"evt_mes_{month}",
                    teaching_concept="compound_interest",
                    choice_id="card",
                    state_after=STATE | {"month": month, "creditCard": card | {"balance": balance}},
                )
            )
        await db.flush()
        second = await http.post(f"/api/sessions/{session_id}/event", json=STATE | {"month": 5})

    assert first.status_code == second.status_code == 200
    await db.refresh(stored)
    adjusted = SeasonPlan.model_validate(stored.season_plan)
    reinforced = [m for m in adjusted.months if m.month > 5 and m.theme.startswith("Refuerzo")]
    assert [(m.month, m.teaching_concept.value) for m in reinforced] == [(6, "compound_interest")]


async def test_first_event_does_not_wait_for_the_season_planner(db: AsyncSession) -> None:
    from contextlib import asynccontextmanager

    from app.api.events import get_session_factory
    from app.config import get_settings
    from app.db import GameSession
    from app.game_master.planner import SeasonPlanner
    from app.game_master.season import SeasonPlan, default_plan
    from app.providers.base import StructuredResult, Usage
    from app.schemas import PlayerState

    settings = Settings(_env_file=None, season_planner=True)  # type: ignore[call-arg]
    event_provider = FakeProvider(ok())
    game_master = GameMaster(event_provider, settings, TTLCache(10, 60), rng=random.Random(0))
    default = default_plan(PlayerState.model_validate(STATE))
    months = list(default.months)
    months[1] = months[1].model_copy(update={"theme": "Plan escrito por Wonder"})
    wonder_plan = default.model_copy(update={"months": months})
    events_before_plan: list[int] = []

    def plan_step() -> StructuredResult[Any]:
        events_before_plan.append(len(event_provider.calls))
        return StructuredResult(wonder_plan, Usage(model="muse-wonder-1.0"), latency_ms=12000)

    app = create_app()
    app.state.season_planner = SeasonPlanner(FakeProvider(plan_step), settings)

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    @asynccontextmanager
    async def background_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_settings] = lambda: settings
    app.dependency_overrides[get_game_master] = lambda: game_master
    app.dependency_overrides[get_session_factory] = lambda: background_session
    async with httpx2.AsyncClient(
        transport=httpx2.ASGITransport(app=app), base_url="http://test"
    ) as http:
        session_id = (await http.post("/api/sessions", json={})).json()["sessionId"]
        response = await http.post(f"/api/sessions/{session_id}/event", json=STATE)

    assert response.status_code == 200
    # The event was generated with the default plan before the planner was even called.
    assert events_before_plan == [1]
    focus = default.for_month(3)
    assert f'"teachingConcept":"{focus.teaching_concept.value}"' in event_provider.calls[0]["user"]
    stored = await db.get(GameSession, uuid.UUID(session_id))
    assert stored is not None
    await db.refresh(stored)
    assert SeasonPlan.model_validate(stored.season_plan) == wonder_plan

import uuid
from collections.abc import AsyncIterator
from decimal import Decimal

import httpx2
import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from app.agents.trace import AGENT_TRACES, MAX_STEPS_PER_SESSION, AgentTraces
from app.db import GameSession, ModelCall
from app.db.session import get_session
from app.main import create_app


@pytest.fixture
async def client(db: AsyncSession) -> AsyncIterator[httpx2.AsyncClient]:
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


@pytest.mark.anyio
async def test_trace_lists_calls_oldest_first_and_adds_the_cost(
    client: httpx2.AsyncClient, db: AsyncSession
) -> None:
    session = GameSession()
    db.add(session)
    await db.flush()
    for endpoint, provider, cost in (
        ("transcribe", "meta_api", "0.001"),
        ("negotiation_agent", "open_weights", "0.002"),
    ):
        db.add(
            ModelCall(
                session_id=session.id,
                provider=provider,
                model="m",
                endpoint=endpoint,
                input_tokens=10,
                output_tokens=5,
                estimated_cost_usd=Decimal(cost),
                latency_ms=1200,
                ok=True,
            )
        )
    await db.commit()
    AGENT_TRACES.add(session.id, "event_agent", 1, "project_debt", '{"balance":1}', {"months": 3})

    response = await client.get(f"/api/sessions/{session.id}/trace")

    assert response.status_code == 200
    body = response.json()
    assert [call["endpoint"] for call in body["calls"]] == ["transcribe", "negotiation_agent"]
    assert body["calls"][1]["provider"] == "open_weights"
    assert body["totalCostUsd"] == pytest.approx(0.003)
    assert body["toolSteps"][0]["tool"] == "project_debt"
    assert "months" in body["toolSteps"][0]["result"]
    assert body["seasonPlan"] == []


@pytest.mark.anyio
async def test_trace_of_an_unknown_session_is_404(client: httpx2.AsyncClient) -> None:
    response = await client.get(f"/api/sessions/{uuid.uuid4()}/trace")
    assert response.status_code == 404


def test_traces_keep_only_the_last_steps_of_a_session() -> None:
    traces = AgentTraces()
    session_id = uuid.uuid4()
    for step in range(MAX_STEPS_PER_SESSION + 5):
        traces.add(session_id, "e", step, "t", "{}", {})

    steps = traces.for_session(session_id)

    assert len(steps) == MAX_STEPS_PER_SESSION
    assert steps[-1].step == MAX_STEPS_PER_SESSION + 4


def test_traces_ignore_a_call_without_a_session() -> None:
    traces = AgentTraces()
    traces.add(None, "e", 1, "t", "{}", {})
    assert traces.for_session(uuid.uuid4()) == []

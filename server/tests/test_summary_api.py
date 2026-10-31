import uuid
from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.summary import get_summary_writer
from app.config import Settings
from app.db import GameSession, Negotiation
from app.db.session import get_session
from app.main import create_app
from app.providers.base import ProviderUnavailableError
from app.summary.writer import SummaryWriter

from .fakes import FakeProvider, fail

pytestmark = pytest.mark.anyio

FINAL_STATE: dict[str, Any] = {
    "month": 12,
    "salaryNet": 450000,
    "cash": 80000,
    "savings": 120000,
    "emergencyFund": 50000,
    "creditCard": {"balance": 150000, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "wellbeing": 70,
}


@pytest.fixture
async def client(db: AsyncSession) -> AsyncIterator[httpx2.AsyncClient]:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    writer = SummaryWriter(provider, Settings(_env_file=None))  # type: ignore[call-arg]
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_summary_writer] = lambda: writer
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def test_summary_closes_the_session(client: httpx2.AsyncClient, db: AsyncSession) -> None:
    session_id = (await client.post("/api/sessions", json={})).json()["sessionId"]
    db.add(
        Negotiation(
            session_id=uuid.UUID(session_id),
            offer_id="celular_gama_alta",
            transcript="¿Cuánto pago en total?",
            npc_reply="₡569 400",
            key_questions_asked=["total_cost"],
            revealed_facts=["total_cost"],
            deal_status="walked_away",
            source="model",
        )
    )
    await db.flush()

    response = await client.post(f"/api/sessions/{session_id}/summary", json=FINAL_STATE)

    assert response.status_code == 200
    body = response.json()
    assert body["netWorth"] == 100000
    assert body["keyQuestionsAsked"] == ["total_cost"]
    assert body["source"] == "fallback"
    assert body["cardImageKey"] == "card_year_some_debt"
    game_session = await db.get(GameSession, uuid.UUID(session_id))
    assert game_session is not None and game_session.finished_at is not None
    assert game_session.final_state == FINAL_STATE | {
        "installments": [],
        "informalLoans": [],
        "history": [],
        "characterName": None,
        "week": None,
        "jobPosition": None,
        "language": None,
    }

    again = await client.post(f"/api/sessions/{session_id}/summary", json=FINAL_STATE)
    assert again.status_code == 409

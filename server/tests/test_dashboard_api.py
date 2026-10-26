from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.dashboard import get_class_explainer
from app.classes.explainer import ClassExplainer
from app.config import Settings
from app.db import Decision, GameSession, Negotiation, SchoolClass
from app.db.session import get_session
from app.main import create_app
from app.providers.base import ProviderUnavailableError

from .fakes import FakeProvider, fail

pytestmark = pytest.mark.anyio


def state(card_balance: int, month: int) -> dict[str, Any]:
    return {
        "month": month,
        "salaryNet": 450000,
        "cash": 100000,
        "savings": 0,
        "emergencyFund": 0,
        "creditCard": {
            "balance": card_balance,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "installments": [],
        "informalLoans": [],
        "history": [],
        "wellbeing": 60,
    }


@pytest.fixture
async def client(db: AsyncSession) -> AsyncIterator[httpx2.AsyncClient]:
    explainer = ClassExplainer(
        FakeProvider(fail(ProviderUnavailableError("down"))),
        Settings(_env_file=None),  # type: ignore[call-arg]
    )
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_class_explainer] = lambda: explainer
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def test_dashboard_aggregates_only_this_class(
    client: httpx2.AsyncClient, db: AsyncSession
) -> None:
    db.add_all(
        [SchoolClass(code="UNA101", name="Finanzas 10-1"), SchoolClass(code="OTRA22", name="Otra")]
    )
    await (
        db.flush()
    )  # no ORM relationship, so insert classes before the sessions that reference them
    student = GameSession(class_code="UNA101", final_state=state(150000, 12))
    unfinished = GameSession(class_code="UNA101")
    outsider = GameSession(class_code="OTRA22")
    db.add_all([student, unfinished, outsider])
    await db.flush()
    db.add_all(
        [
            Decision(
                session_id=student.id,
                month=1,
                event_id="e1",
                teaching_concept="budget",
                choice_id="a",
                state_after=state(0, 1),
            ),
            Decision(
                session_id=student.id,
                month=2,
                event_id="e2",
                teaching_concept="minimum_payment",
                choice_id="b",
                state_after=state(90000, 2),
            ),
            Decision(
                session_id=outsider.id,
                month=1,
                event_id="e1",
                teaching_concept="budget",
                choice_id="a",
                state_after=state(0, 1),
            ),
            Negotiation(
                session_id=student.id,
                offer_id="celular_gama_alta",
                transcript="t",
                npc_reply="r",
                key_questions_asked=["total_cost"],
                revealed_facts=[],
                deal_status="open",
                source="model",
            ),
            Negotiation(
                session_id=unfinished.id,
                offer_id="celular_gama_alta",
                transcript="t",
                npc_reply="r",
                key_questions_asked=["term"],
                revealed_facts=[],
                deal_status="open",
                source="model",
            ),
        ]
    )
    await db.flush()

    response = await client.get("/api/classes/UNA101/dashboard")

    assert response.status_code == 200
    body = response.json()
    assert (body["students"], body["finishedGames"], body["negotiations"]) == (2, 1, 2)
    assert body["trapDetectionPct"] == 50
    assert body["averageFinalDebt"] == 150000
    concepts = {item["concept"]: item for item in body["concepts"]}
    assert concepts["budget"]["decisions"] == 1
    assert concepts["minimum_payment"]["riskyChoicePct"] == 100
    assert body["source"] == "fallback"
    assert "pago mínimo" in body["explanation"]


async def test_unknown_class(client: httpx2.AsyncClient) -> None:
    assert (await client.get("/api/classes/NOPE99/dashboard")).status_code == 404
    assert (await client.get("/api/classes/bad/dashboard")).status_code == 422


async def test_empty_class(client: httpx2.AsyncClient, db: AsyncSession) -> None:
    db.add(SchoolClass(code="VACIA1", name="Vacía"))
    await db.flush()
    body = (await client.get("/api/classes/VACIA1/dashboard")).json()
    assert body["students"] == 0
    assert body["concepts"] == []
    assert body["trapDetectionPct"] is None

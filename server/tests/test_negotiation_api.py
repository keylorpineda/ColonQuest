import uuid
from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.negotiation import get_negotiator
from app.config import Settings
from app.db import Negotiation
from app.db.session import get_session
from app.main import create_app
from app.negotiation.service import Negotiator, SellerTurn
from app.providers.base import StructuredResult, Usage

from .fakes import FakeProvider

pytestmark = pytest.mark.anyio


def seller(reply: str, facts: list[str]) -> Any:
    def step() -> StructuredResult[Any]:
        turn = SellerTurn(npc_reply=reply, revealed_facts=facts, deal_status="open")
        return StructuredResult(value=turn, usage=Usage(model="m"), latency_ms=1)

    return step


@pytest.fixture
def provider() -> FakeProvider:
    return FakeProvider()


@pytest.fixture
async def client(db: AsyncSession, provider: FakeProvider) -> AsyncIterator[httpx2.AsyncClient]:
    settings = Settings(_env_file=None, game_master_model="muse-wonder-1.0")  # type: ignore[call-arg]
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    app.dependency_overrides[get_negotiator] = lambda: Negotiator(provider, settings)
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http


async def test_turns_are_stored_and_fed_back(
    client: httpx2.AsyncClient, provider: FakeProvider, db: AsyncSession
) -> None:
    provider.steps += [
        seller("Solo ₡19 600 al mes, mae.", ["monthly_payment"]),
        seller("Diay, el seguro es ₡3 500 al mes.", ["insurance_fee"]),
    ]
    session_id = (await client.post("/api/sessions", json={})).json()["sessionId"]
    url = f"/api/sessions/{session_id}/negotiation"

    first = await client.post(url, json={"offerId": "celular_gama_alta", "text": "Hola, ¿qué tal?"})
    second = await client.post(
        url, json={"offerId": "celular_gama_alta", "text": "¿Y el seguro cuánto es?"}
    )

    assert first.status_code == second.status_code == 200
    assert second.headers["X-Reply-Source"] == "model"
    assert second.json()["keyQuestionsAsked"] == ["insurance"]
    assert "Hola, ¿qué tal?" in provider.calls[1]["user"]
    rows = (
        await db.scalars(select(Negotiation).where(Negotiation.session_id == uuid.UUID(session_id)))
    ).all()
    assert [row.revealed_facts for row in rows] == [["monthly_payment"], ["insurance_fee"]]
    assert rows[1].key_questions_asked == ["insurance"]


async def test_seller_remembers_questions_asked_to_other_sellers(
    client: httpx2.AsyncClient, provider: FakeProvider
) -> None:
    provider.steps += [
        seller("Diay, el seguro es ₡3 500 al mes.", ["insurance_fee"]),
        seller("Mae, esta pantalla es una ganga.", []),
        seller("Solo ₡19 600 al mes, mae.", ["monthly_payment"]),
    ]
    session_id = (await client.post("/api/sessions", json={})).json()["sessionId"]
    url = f"/api/sessions/{session_id}/negotiation"

    await client.post(url, json={"offerId": "celular_gama_alta", "text": "¿Cuánto es el seguro?"})
    await client.post(url, json={"offerId": "pantalla_65", "text": "Hola"})
    await client.post(url, json={"offerId": "celular_gama_alta", "text": "¿Y la cuota?"})

    assert "playerHabits" not in provider.calls[0]["user"]
    assert '"playerHabits":{"askedOtherSellers":["insurance"]}' in provider.calls[1]["user"]
    # Questions asked in this same offer are already in its conversation, not in the habits.
    assert "playerHabits" not in provider.calls[2]["user"]


async def test_unknown_offer_and_session(client: httpx2.AsyncClient) -> None:
    session_id = (await client.post("/api/sessions", json={})).json()["sessionId"]
    unknown_offer = await client.post(
        f"/api/sessions/{session_id}/negotiation", json={"offerId": "lancha", "text": "Hola"}
    )
    unknown_session = await client.post(
        f"/api/sessions/{uuid.uuid4()}/negotiation",
        json={"offerId": "celular_gama_alta", "text": "Hola"},
    )
    assert unknown_offer.status_code == 404
    assert unknown_session.status_code == 404

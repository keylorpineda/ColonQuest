import uuid
from collections.abc import AsyncIterator
from typing import Any

import httpx2
import pytest
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.api.talk import get_talker
from app.config import Settings
from app.db import GameSession, ModelCall
from app.db.session import get_session
from app.main import create_app
from app.providers.base import ProviderUnavailableError, StructuredResult, Usage
from app.schemas.talk import TalkFact, TalkPartner, TalkRequest, TalkTurn
from app.talks.service import (
    TalkResponder,
    TalkUnavailableError,
    invented_figures,
    system_prompt,
    user_prompt,
)

from .fakes import FakeLedger, FakeProvider, Step, fail

pytestmark = pytest.mark.anyio

LENDER: dict[str, Any] = {
    "partner": "lender",
    "text": "How much is the interest?",
    "language": "en",
    "sheet": [
        {"fact": "rate", "text": "20% a month"},
        {"fact": "total_cost", "text": "Lend ₡100 000 and in 3 months they give back ₡172 800."},
    ],
    "amounts": [100000, 172800],
    "percents": [20],
    "history": [{"player": "Hi", "reply": "Need some cash fast?"}],
}


def answer(text: str, *facts: TalkFact) -> Step:
    def step() -> StructuredResult[Any]:
        return StructuredResult(
            value=TalkTurn(npc_reply=text, asked_facts=list(facts)),
            usage=Usage(model="m"),
            latency_ms=1,
        )

    return step


def responder(provider: FakeProvider) -> TalkResponder:
    return TalkResponder(provider, Settings(_env_file=None))  # type: ignore[call-arg]


@pytest.mark.parametrize(
    "reply",
    [
        "It's 20% a month, friend.",
        "Lend ₡100 000 and you give me back ₡172 800.",
        "Give me 100,000 and it comes back as 172.800.",
        "Three months, 3 easy payments, 20 por ciento.",
    ],
)
async def test_the_figures_on_the_sheet_pass(reply: str) -> None:
    assert invented_figures(reply, [100000, 172800], [20]) == []


@pytest.mark.parametrize(
    "reply",
    [
        "It's only 15% a month.",
        "You give me back ₡150 000.",
        "Lend 50000 today.",
        "Ten por ciento, 10 por ciento nada más.",
    ],
)
async def test_any_other_figure_is_invented(reply: str) -> None:
    assert invented_figures(reply, [100000, 172800], [20])


async def test_the_model_only_gets_its_character_and_the_true_sheet() -> None:
    request = TalkRequest.model_validate(LENDER)

    assert "gota a gota" in system_prompt(TalkPartner.LENDER, "en")
    assert "every word is in English" in system_prompt(TalkPartner.LENDER, "en")
    assert "Costa Rican Spanish" in system_prompt(TalkPartner.COLLECTORS, "es")
    assert "pyramid" in system_prompt(TalkPartner.PYRAMID, "en")
    prompt = user_prompt(request)
    assert "₡172 800" in prompt
    assert "How much is the interest?" in prompt
    assert "Need some cash fast?" in prompt


async def test_a_valid_reply_comes_back_with_what_was_asked() -> None:
    provider = FakeProvider(answer("It's 20% a month, friend.", TalkFact.RATE))

    reply = await responder(provider).reply(
        FakeLedger(), uuid.uuid4(), TalkRequest.model_validate(LENDER)
    )

    assert reply.npc_reply == "It's 20% a month, friend."
    assert reply.asked_facts == [TalkFact.RATE]
    assert reply.source == "model"


async def test_an_invented_figure_gets_one_more_try() -> None:
    provider = FakeProvider(answer("Only 5% a month."), answer("It's 20% a month."))

    reply = await responder(provider).reply(
        FakeLedger(), uuid.uuid4(), TalkRequest.model_validate(LENDER)
    )

    assert reply.npc_reply == "It's 20% a month."
    assert len(provider.calls) == 2


async def test_two_invented_figures_leave_it_to_the_local_rules() -> None:
    provider = FakeProvider(answer("Only 5%."), answer("Only 6%."))

    with pytest.raises(TalkUnavailableError):
        await responder(provider).reply(
            FakeLedger(), uuid.uuid4(), TalkRequest.model_validate(LENDER)
        )


async def test_a_provider_failure_leaves_it_to_the_local_rules() -> None:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))

    with pytest.raises(TalkUnavailableError):
        await responder(provider).reply(
            FakeLedger(), uuid.uuid4(), TalkRequest.model_validate(LENDER)
        )


@pytest.fixture
async def client(db: AsyncSession) -> AsyncIterator[tuple[httpx2.AsyncClient, Any]]:
    app = create_app()

    async def override_session() -> AsyncIterator[AsyncSession]:
        yield db

    app.dependency_overrides[get_session] = override_session
    transport = httpx2.ASGITransport(app=app)
    async with httpx2.AsyncClient(transport=transport, base_url="http://test") as http:
        yield http, app


async def test_the_endpoint_answers_and_records_the_call(
    client: tuple[httpx2.AsyncClient, Any], db: AsyncSession
) -> None:
    http, app = client
    session = GameSession()
    db.add(session)
    await db.commit()
    provider = FakeProvider(answer("It's 20% a month.", TalkFact.RATE))
    app.dependency_overrides[get_talker] = lambda: responder(provider)

    response = await http.post(f"/api/sessions/{session.id}/talk", json=LENDER)

    assert response.status_code == 200
    assert response.json() == {
        "npcReply": "It's 20% a month.",
        "askedFacts": ["rate"],
        "source": "model",
    }
    call = await db.scalar(select(ModelCall).where(ModelCall.session_id == session.id))
    assert call is not None
    assert call.endpoint == "debt_talk"


async def test_the_endpoint_says_503_when_the_local_rules_must_answer(
    client: tuple[httpx2.AsyncClient, Any], db: AsyncSession
) -> None:
    http, app = client
    session = GameSession()
    db.add(session)
    await db.commit()
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    app.dependency_overrides[get_talker] = lambda: responder(provider)

    response = await http.post(f"/api/sessions/{session.id}/talk", json=LENDER)

    assert response.status_code == 503


async def test_a_talk_needs_at_least_one_true_fact(
    client: tuple[httpx2.AsyncClient, Any], db: AsyncSession
) -> None:
    http, app = client
    session = GameSession()
    db.add(session)
    await db.commit()
    app.dependency_overrides[get_talker] = lambda: responder(FakeProvider())

    response = await http.post(f"/api/sessions/{session.id}/talk", json=LENDER | {"sheet": []})

    assert response.status_code == 422

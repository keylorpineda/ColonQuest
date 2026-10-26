import uuid
from typing import Any

import pytest

from app.config import Settings
from app.cost_guard import BudgetMode
from app.negotiation.seller_sheet import OFFERS
from app.negotiation.service import (
    Negotiator,
    PreviousTurn,
    SellerTurn,
    build_user_prompt,
    scripted_deal_status,
)
from app.providers.base import (
    ProviderResponseError,
    ProviderUnavailableError,
    StructuredResult,
    Usage,
)
from app.schemas import DealStatus, KeyQuestion, SellerFact

from .fakes import FakeLedger, FakeProvider, Step, fail

pytestmark = pytest.mark.anyio

OFFER = OFFERS["celular_gama_alta"]
SESSION_ID = uuid.uuid4()


def seller(reply: str, facts: list[str] | None = None, status: str = "open") -> Step:
    def step() -> StructuredResult[Any]:
        turn = SellerTurn(npc_reply=reply, revealed_facts=facts or [], deal_status=status)
        usage = Usage(model="muse-wonder-1.0", input_tokens=500, output_tokens=80)
        return StructuredResult(value=turn, usage=usage, latency_ms=600)

    return step


def make_negotiator(provider: FakeProvider) -> Negotiator:
    settings = Settings(_env_file=None, game_master_model="muse-wonder-1.0")  # type: ignore[call-arg]
    return Negotiator(provider, settings)


async def test_model_reply_with_true_figures() -> None:
    provider = FakeProvider(seller("Con seguro son ₡3 500 al mes, mae.", ["insurance_fee"]))
    ledger = FakeLedger()
    outcome = await make_negotiator(provider).reply(
        ledger, SESSION_ID, OFFER, "¿Y el seguro cuánto cuesta?"
    )

    assert outcome.source == "model"
    assert outcome.reply.revealed_facts == [SellerFact.INSURANCE_FEE]
    assert outcome.reply.key_questions_asked == [KeyQuestion.INSURANCE]
    assert outcome.reply.transcript == "¿Y el seguro cuánto cuesta?"
    assert [r["ok"] for r in ledger.records] == [True]


async def test_invented_amount_is_retried_then_falls_back() -> None:
    provider = FakeProvider(
        seller("Tranqui, en total son ₡400 000."),
        seller("Le hago precio: ₡300 000."),
    )
    ledger = FakeLedger()
    outcome = await make_negotiator(provider).reply(
        ledger, SESSION_ID, OFFER, "¿Cuánto pago en total?"
    )

    assert outcome.source == "fallback"
    assert "₡569 400" in outcome.reply.npc_reply
    assert outcome.reply.revealed_facts == [SellerFact.TOTAL_COST]
    assert len(provider.calls) == 2
    assert [r["ok"] for r in ledger.records] == [True, True]  # both calls were paid for


async def test_invalid_json_then_valid_reply() -> None:
    provider = FakeProvider(fail(ProviderResponseError("bad")), seller("La cuota es ₡19 600."))
    ledger = FakeLedger()
    outcome = await make_negotiator(provider).reply(ledger, SESSION_ID, OFFER, "Hola")

    assert outcome.source == "model"
    assert [r["ok"] for r in ledger.records] == [False, True]


async def test_provider_down_uses_scripted_reply() -> None:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    outcome = await make_negotiator(provider).reply(
        FakeLedger(), SESSION_ID, OFFER, "No gracias, mejor no"
    )

    assert outcome.source == "fallback"
    assert outcome.reply.deal_status == DealStatus.WALKED_AWAY


async def test_budget_exhausted_skips_the_model() -> None:
    provider = FakeProvider()
    outcome = await make_negotiator(provider).reply(
        FakeLedger(mode=BudgetMode.FALLBACK_ONLY), SESSION_ID, OFFER, "¿De contado cuánto?"
    )

    assert provider.calls == []
    assert outcome.reply.key_questions_asked == [KeyQuestion.CASH_PRICE]
    assert "₡350 000" in outcome.reply.npc_reply


@pytest.mark.parametrize(
    ("text", "status"),
    [
        ("Trato hecho, me lo llevo", DealStatus.ACCEPTED),
        ("No me interesa", DealStatus.WALKED_AWAY),
        ("¿Y cuántos meses son?", DealStatus.OPEN),
    ],
)
def test_scripted_deal_status(text: str, status: DealStatus) -> None:
    assert scripted_deal_status(text) == status


def test_prompt_includes_sheet_and_recent_history() -> None:
    history = [PreviousTurn(player=f"p{i}", seller=f"s{i}") for i in range(8)]
    prompt = build_user_prompt(OFFER, history, "¿Hay comisión?")

    assert '"total_cost":"₡569 400"' in prompt
    assert '"playerSays":"¿Hay comisión?"' in prompt
    assert '"p1"' not in prompt and '"p2"' in prompt

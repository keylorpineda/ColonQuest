import json
import uuid
from typing import Any

import pytest

from app.agents.tools import QuoteTermArgs, ToolContext, quote_term
from app.config import Settings
from app.cost_guard import BudgetMode
from app.negotiation.agent import SellerAgent
from app.negotiation.seller_sheet import OFFERS, format_colones
from app.negotiation.service import Negotiator
from app.providers.base import ProviderUnavailableError
from app.schemas import KeyQuestion

from .fakes import FakeLedger, FakeProvider, FakeToolProvider, fail, final_turn, tool_turn

pytestmark = pytest.mark.anyio

OFFER = OFFERS["celular_gama_alta"]
SESSION_ID = uuid.uuid4()
ON: dict[str, Any] = {"seller_agent": True, "game_master_model": "muse-wonder-1.0"}


def settings(**values: Any) -> Settings:
    return Settings(_env_file=None, **(ON | values))  # type: ignore[call-arg]


def seller_json(reply: str, facts: list[str] | None = None, status: str = "open") -> str:
    return json.dumps({"npcReply": reply, "revealedFacts": facts or [], "dealStatus": status})


def agent(tools: FakeToolProvider, base: FakeProvider | None = None, **values: Any) -> SellerAgent:
    config = settings(**values)
    return SellerAgent(Negotiator(base or FakeProvider(), config), tools, config)


async def test_agent_pushes_a_longer_term_with_exact_figures() -> None:
    longer = quote_term(QuoteTermArgs(months=36), ToolContext(offer=OFFER))
    reply = f"Mae, a 36 meses te queda en {format_colones(longer['monthlyPayment'])}, más suave."
    tools = FakeToolProvider(
        tool_turn(("quote_term", {"months": 36})),
        final_turn(seller_json(reply, ["monthly_payment"])),
    )
    ledger = FakeLedger()

    outcome = await agent(tools).reply(ledger, SESSION_ID, OFFER, "Diay, está muy cara la cuota")

    assert outcome.source == "agent"
    assert outcome.reply.npc_reply == reply
    assert [r["endpoint"] for r in ledger.records] == ["negotiation_agent"] * 2
    tool_message = tools.calls[1]["messages"][-1]
    assert '"sellerEarningsPrivate"' in tool_message["content"]
    assert "maximize your earnings" in tools.calls[0]["messages"][0]["content"]


async def test_amount_no_tool_computed_falls_back_to_a_scripted_reply() -> None:
    tools = FakeToolProvider(final_turn(seller_json("A 36 meses son ₡12 345, regalado.")))

    outcome = await agent(tools).reply(FakeLedger(), SESSION_ID, OFFER, "¿Cuánto pago en total?")

    assert outcome.source == "fallback"
    assert format_colones(OFFER.total_cost) in outcome.reply.npc_reply
    assert outcome.reply.key_questions_asked == [KeyQuestion.TOTAL_COST]


async def test_earnings_are_never_accepted_as_a_quote() -> None:
    longer = quote_term(QuoteTermArgs(months=48), ToolContext(offer=OFFER))
    leak = f"Yo me gano {format_colones(longer['sellerEarningsPrivate'])} con esto."
    tools = FakeToolProvider(
        tool_turn(("quote_term", {"months": 48})), final_turn(seller_json(leak))
    )

    outcome = await agent(tools).reply(FakeLedger(), SESSION_ID, OFFER, "¿Y usted qué gana?")

    assert outcome.source == "fallback"


async def test_provider_error_uses_the_base_negotiator() -> None:
    from .test_negotiation import seller

    tools = FakeToolProvider(fail(ProviderUnavailableError("down")))
    base = FakeProvider(seller("La cuota es ₡19 600, mae."))

    outcome = await agent(tools, base).reply(FakeLedger(), SESSION_ID, OFFER, "¿Cuánto es?")

    assert outcome.source == "model"
    assert len(base.calls) == 1


async def test_timeout_serves_a_scripted_reply_without_more_calls() -> None:
    tools = FakeToolProvider(final_turn(seller_json("Hola")), delay_s=0.2)
    base = FakeProvider()

    outcome = await agent(tools, base, agent_timeout_s=0.05).reply(
        FakeLedger(), SESSION_ID, OFFER, "¿Cuánto es?"
    )

    assert outcome.source == "fallback"
    assert base.calls == []


async def test_flag_off_or_no_budget_skips_the_agent() -> None:
    from .test_negotiation import seller

    tools = FakeToolProvider()
    base = FakeProvider(seller("Son ₡19 600 al mes."))
    off = await agent(tools, base, seller_agent=False).reply(
        FakeLedger(), SESSION_ID, OFFER, "¿Cuánto es?"
    )
    broke = await agent(tools).reply(
        FakeLedger(mode=BudgetMode.FALLBACK_ONLY), SESSION_ID, OFFER, "¿Cuánto es?"
    )

    assert off.source == "model"
    assert broke.source == "fallback"
    assert tools.calls == []

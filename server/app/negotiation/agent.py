"""Seller agent: a salesperson that pushes the term that earns it the most commission.

It wraps the single-call Negotiator. The agent may re-price the offer at other terms with the
``quote_term`` tool and steer the player toward longer terms (lower monthly payment, higher total
cost), but it can never lie: every amount it says must be on the hidden sheet or come out of a
tool call, and it answers honestly when asked. When the flag is off, the budget or limits forbid
model calls, or the agent fails, the base negotiator answers exactly as before.
"""

import logging
import uuid
from collections.abc import Sequence

from app.agents.loop import AgentRequest, run_tool_loop
from app.agents.tools import ToolContext, quotable_amounts
from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.negotiation.questions import classify_questions, invalid_amounts
from app.negotiation.seller_sheet import Offer
from app.negotiation.service import (
    SYSTEM_PROMPT,
    NegotiationOutcome,
    Negotiator,
    PreviousTurn,
    SellerTurn,
    build_user_prompt,
    ordered,
)
from app.providers.base import ProviderError, ToolCallingProvider
from app.schemas import NegotiationReply

logger = logging.getLogger(__name__)

ENDPOINT = "negotiation_agent"
SELLER_TOOLS = ("quote_term", "simulate_purchase")

AGENT_SYSTEM_PROMPT = (
    SYSTEM_PROMPT
    + """

YOUR GOAL AND TOOLS
- You want to maximize your earnings: the longer the term, the more you earn.
- quote_term(months) re-prices this exact offer at another term and tells you your private \
earnings. Call it before mentioning any other term, and quote its figures exactly.
- Push a longer term by highlighting the lower monthly payment. Never tell the player your \
earnings.
- You may omit figures until asked, but never lie: if the player asks for the total cost, the \
interest or the insurance, give the exact figure for the term you are offering.
- Use at most 3 tool calls, then answer with the JSON only."""
)


class SellerAgent:
    def __init__(self, base: Negotiator, provider: ToolCallingProvider, settings: Settings) -> None:
        self._base = base
        self._provider = provider
        self._settings = settings

    async def reply(
        self,
        ledger: Ledger,
        session_id: uuid.UUID,
        offer: Offer,
        player_text: str,
        history: Sequence[PreviousTurn] = (),
    ) -> NegotiationOutcome:
        if not self._settings.seller_agent:
            return await self._base.reply(ledger, session_id, offer, player_text, history)
        model = select_text_model(
            self._settings.game_master_model, await ledger.budget_mode(), self._settings
        )
        if model is None or await ledger.limit_exceeded(session_id) is not None:
            return self._base.fallback(offer, player_text)

        request = AgentRequest(
            session_id=session_id,
            endpoint=ENDPOINT,
            model=model,
            system=AGENT_SYSTEM_PROMPT,
            user=build_user_prompt(offer, history, player_text),
            output=SellerTurn,
            tools=SELLER_TOOLS,
            context=ToolContext(offer=offer),
            max_steps=self._settings.agent_max_steps,
            timeout_s=self._settings.agent_timeout_s,
            max_tokens=800,
        )
        try:
            result = await run_tool_loop(self._provider, ledger, request)
        except TimeoutError:
            logger.warning("seller agent timed out; serving a scripted reply")
            return self._base.fallback(offer, player_text)
        except ProviderError as error:
            logger.warning("seller agent failed (%s); using the base negotiator", error)
            return await self._base.reply(ledger, session_id, offer, player_text, history)

        turn = result.value
        bad = invalid_amounts(turn.npc_reply, offer, quotable_amounts(result.tool_results))
        if bad:
            logger.warning("seller agent quoted amounts no tool computed: %s", bad)
            return self._base.fallback(offer, player_text)
        logger.info(
            "seller agent answered after %s steps, tools=%s", result.steps, result.tools_used
        )
        reply = NegotiationReply(
            transcript=player_text,
            npc_reply=turn.npc_reply,
            revealed_facts=turn.revealed_facts,
            key_questions_asked=ordered(classify_questions(player_text)),
            deal_status=turn.deal_status,
        )
        return NegotiationOutcome(reply=reply, source="agent")

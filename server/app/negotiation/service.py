"""One negotiation turn: the player speaks, the seller answers using the hidden sheet.

Key questions are classified deterministically. The model only writes the seller's reply; a
reply quoting any amount that is not on the sheet is treated as invalid (retried once), and any
failure ends in a scripted reply, so the negotiation never breaks.
"""

import asyncio
import json
import logging
import re
import time
import uuid
from collections.abc import Sequence
from dataclasses import dataclass
from typing import Annotated, Protocol

from pydantic import Field

from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.negotiation.questions import classify_questions, fallback_reply, invalid_amounts
from app.negotiation.seller_sheet import Offer
from app.providers.base import (
    ModelProvider,
    ProviderError,
    ProviderResponseError,
    ProviderTimeoutError,
    Usage,
)
from app.schemas import DealStatus, KeyQuestion, NegotiationReply, SellerFact
from app.schemas.base import ContractModel

logger = logging.getLogger(__name__)

ENDPOINT = "negotiation"
MAX_ATTEMPTS = 2

SYSTEM_PROMPT = """You play a friendly but pushy Costa Rican salesperson in the game \
"Colón Quest". You earn a commission on installment sales.

RULES
- Speak Costa Rican Spanish ("mae", "diay", "tuanis" are fine; nothing vulgar), at most 2 short \
sentences, at most 300 characters.
- You receive a hidden sheet with the true figures. Never state any amount that is not on it, \
never change a figure, never invent discounts.
- Highlight the low monthly payment. Reveal the insurance, commission, interest rate, term, \
cash price or total cost ONLY when the player asks about it directly. If they ask, answer honestly.
- playerHabits, when present, lists the key questions this player asked other sellers in this \
game. Change your tactic to match: if they always ask about the insurance or the total, \
anticipate it ("ya sé que vos preguntás por el seguro, mae") and sell on another angle. Habits \
never allow a figure that is not on the sheet.
- revealedFacts lists every fact from the sheet you mention in this reply.
- dealStatus: "accepted" only if the player clearly agrees to buy, "walked_away" if they clearly \
leave or refuse, otherwise "open".

Answer with JSON only, matching the provided schema."""

ACCEPT_PATTERN = re.compile(r"\b(acepto|me lo llevo|trato hecho|de una|deme uno)\b", re.IGNORECASE)
LEAVE_PATTERN = re.compile(r"\b(no gracias|me voy|mejor no|paso|no me interesa)\b", re.IGNORECASE)


class SellerTurn(ContractModel):
    """What the model returns; key questions are not trusted to the model."""

    npc_reply: Annotated[str, Field(min_length=1, max_length=300)]
    revealed_facts: Annotated[list[SellerFact], Field(max_length=len(SellerFact))]
    deal_status: DealStatus


@dataclass(frozen=True, slots=True)
class PreviousTurn:
    player: str
    seller: str


@dataclass(frozen=True, slots=True)
class NegotiationOutcome:
    reply: NegotiationReply
    source: str  # "model" or "fallback"


def ordered(questions: set[KeyQuestion]) -> list[KeyQuestion]:
    return [question for question in KeyQuestion if question in questions]


def scripted_deal_status(text: str) -> DealStatus:
    if ACCEPT_PATTERN.search(text):
        return DealStatus.ACCEPTED
    if LEAVE_PATTERN.search(text):
        return DealStatus.WALKED_AWAY
    return DealStatus.OPEN


def build_user_prompt(
    offer: Offer,
    history: Sequence[PreviousTurn],
    player_text: str,
    habits: Sequence[KeyQuestion] = (),
) -> str:
    payload: dict[str, object] = {
        "product": offer.item,
        "seller": offer.seller_name,
        "hiddenSheet": {fact.value: text for fact, text in offer.facts.items()},
        "conversation": [{"player": t.player, "seller": t.seller} for t in history[-6:]],
        "playerSays": player_text,
    }
    if habits:
        payload["playerHabits"] = {"askedOtherSellers": [question.value for question in habits]}
    return json.dumps(payload, ensure_ascii=False, separators=(",", ":"))


class NegotiationResponder(Protocol):
    async def reply(
        self,
        ledger: Ledger,
        session_id: uuid.UUID,
        offer: Offer,
        player_text: str,
        history: Sequence[PreviousTurn] = (),
        habits: Sequence[KeyQuestion] = (),
    ) -> NegotiationOutcome: ...


class Negotiator:
    def __init__(self, provider: ModelProvider, settings: Settings) -> None:
        self._provider = provider
        self._settings = settings

    async def reply(
        self,
        ledger: Ledger,
        session_id: uuid.UUID,
        offer: Offer,
        player_text: str,
        history: Sequence[PreviousTurn] = (),
        habits: Sequence[KeyQuestion] = (),
    ) -> NegotiationOutcome:
        questions = classify_questions(player_text)
        mode = await ledger.budget_mode()
        model = select_text_model(self._settings.game_master_model, mode, self._settings)
        if model is None or await ledger.limit_exceeded(session_id) is not None:
            return self.fallback(offer, player_text, questions)

        prompt = build_user_prompt(offer, history, player_text, habits)
        try:
            async with asyncio.timeout(self._settings.model_timeout_s):
                turn = await self._ask_model(ledger, session_id, model, offer, prompt)
        except (TimeoutError, ProviderTimeoutError):
            await ledger.record(
                usage=Usage(model=model),
                endpoint=ENDPOINT,
                latency_ms=0,
                ok=False,
                session_id=session_id,
            )
            return self.fallback(offer, player_text, questions)
        except ProviderError:
            return self.fallback(offer, player_text, questions)

        reply = NegotiationReply(
            transcript=player_text,
            npc_reply=turn.npc_reply,
            revealed_facts=turn.revealed_facts,
            key_questions_asked=ordered(questions),
            deal_status=turn.deal_status,
        )
        return NegotiationOutcome(reply=reply, source="model")

    async def _ask_model(
        self, ledger: Ledger, session_id: uuid.UUID, model: str, offer: Offer, prompt: str
    ) -> SellerTurn:
        for attempt in range(1, MAX_ATTEMPTS + 1):
            last_attempt = attempt == MAX_ATTEMPTS
            started = time.perf_counter()
            try:
                result = await self._provider.structured(
                    model=model,
                    system=SYSTEM_PROMPT,
                    user=prompt,
                    output=SellerTurn,
                    reasoning_effort=self._settings.live_reasoning_effort,
                    max_tokens=1500,  # reasoning tokens count against this
                )
            except ProviderError as error:
                await ledger.record(
                    usage=Usage(model=model),
                    endpoint=ENDPOINT,
                    latency_ms=int((time.perf_counter() - started) * 1000),
                    ok=False,
                    session_id=session_id,
                )
                if isinstance(error, ProviderResponseError) and not last_attempt:
                    logger.warning("invalid seller reply, retrying")
                    continue
                raise

            # The call succeeded and was paid for, even if its content is rejected below.
            await ledger.record(
                usage=result.usage,
                endpoint=ENDPOINT,
                latency_ms=result.latency_ms,
                ok=True,
                session_id=session_id,
            )
            bad_amounts = invalid_amounts(result.value.npc_reply, offer)
            if not bad_amounts:
                return result.value
            logger.warning("seller quoted amounts not on the sheet: %s", bad_amounts)
            if last_attempt:
                raise ProviderResponseError(f"seller quoted invented amounts: {bad_amounts}")
        raise AssertionError("unreachable")  # pragma: no cover

    def fallback(
        self, offer: Offer, player_text: str, questions: set[KeyQuestion] | None = None
    ) -> NegotiationOutcome:
        """Scripted reply that never calls a model."""
        if questions is None:
            questions = classify_questions(player_text)
        text, facts = fallback_reply(offer, questions)
        reply = NegotiationReply(
            transcript=player_text,
            npc_reply=text,
            revealed_facts=facts,
            key_questions_asked=ordered(questions),
            deal_status=scripted_deal_status(player_text),
        )
        return NegotiationOutcome(reply=reply, source="fallback")

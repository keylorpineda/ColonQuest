"""The collectors, the lender and the pyramid stand, in the model's words.

The figures come from the game: the request carries the true sheet and the only amounts and
percentages the character may say. The model decides how to say them and when, and any reply with
another figure is rejected. When there is no model or no valid reply this raises TalkUnavailable,
and the game answers with its own local rules, so a talk never stops.
"""

import asyncio
import json
import logging
import re
import time
import uuid

from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.negotiation.questions import THOUSANDS_SEPARATORS
from app.providers.base import ModelProvider, ProviderError, ProviderResponseError, Usage
from app.schemas.talk import TalkPartner, TalkReply, TalkRequest, TalkTurn

logger = logging.getLogger(__name__)

ENDPOINT = "debt_talk"
MAX_ATTEMPTS = 2
HISTORY_TURNS = 6

RULES = """You play a character in "ColonQuest", a financial-literacy game set in Costa Rica. The \
player talks to you in their own words.

RULES
- {language}, at most 2 short sentences, at most 300 characters. Stay in character. No lectures.
- "sheet" lists the only true facts, written to you: "you" there is you. Never state any amount \
or percentage that is not on it, never change a figure, never promise anything that is not on it.
- Reveal a fact from the sheet ONLY when the player asks about it. If they ask, you may be evasive \
in tone but the figure or the fact you give must be the true one.
- If the player names an amount that is not on the sheet, do not calculate it: answer with what is \
on the sheet.
- Answer what the player just said. Never repeat a line you already said in "conversation", nor \
open every line the same way: a catchphrase like "pura vida" at most once in the whole conversation.
- You are a person in a town, not part of a game: never mention a game, buttons or a sheet. Never \
say a deal is closed or that money changed hands: the player decides that outside this talk.
- askedFacts lists the facts the player's last line asked about: total_cost, rate, arrangement, \
what_if_not_paid, where_money_comes, guarantee, legal. An empty list if none.

Answer with JSON only, matching the provided schema.

WHO YOU ARE
{character}"""

CHARACTERS = {
    TalkPartner.COLLECTORS: (
        "A debt collector at the player's door. Firm and tired, polite but with no patience. You "
        "want the money today. You mention an arrangement only if the player asks for one. You "
        "never threaten: collecting is legal, threatening is not."
    ),
    TalkPartner.LENDER: (
        "A 'gota a gota' street lender. Friendly and fast, you sell how easy it is: no papers, "
        "money today. You avoid the rate and the total until asked, and you make them sound small. "
        "If asked what happens when someone falls behind, the menace is implied, never graphic."
    ),
    TalkPartner.PYRAMID: (
        "A promoter at the 'Invierte Ya' stand, a pyramid scheme. Excited and in a hurry: places "
        "are running out. You dodge questions about where the money comes from, then admit it if "
        "the player insists. You are selling a dream, and you never call it a pyramid."
    ),
    TalkPartner.NEIGHBOR: (
        "A neighbor the player meets around town, warm and down to earth. You give short, "
        "practical money advice from what you know, as someone who has made those mistakes, never "
        "as a teacher. If the player talks about something else, chat a little and bring it back "
        "to everyday money."
    ),
    TalkPartner.STRANGER: (
        "A stranger trying the SINPE scam: you claim you sent the player money by mistake and ask "
        "for it back right now. You are in a hurry, friendly and a little pushy. You dodge how you "
        "got the number and why it cannot wait, and admit nothing unless the player presses hard."
    ),
}

LANGUAGE_RULE = {
    "es": "Speak Costa Rican Spanish, vos or usted, mae or diay are fine, nothing vulgar",
    "en": "Speak natural English with a friendly Costa Rican setting; every word is in English",
}

NUMBER = re.compile(
    r"(?<![\d.,])\d{1,3}(?:" + THOUSANDS_SEPARATORS + r"\d{3})+(?!\d)|(?<![\d.,])\d{4,}(?!\d)"
)
PERCENT = re.compile(r"(\d+)(?:[.,]\d+)?\s*(?:%|por ?ciento|percent)", re.IGNORECASE)


class TalkUnavailableError(Exception):
    """No model answer can be given: the game uses its local rules."""


def system_prompt(partner: TalkPartner, language: str) -> str:
    rule = LANGUAGE_RULE["en" if language.lower().startswith("en") else "es"]
    return RULES.format(language=rule, character=CHARACTERS[partner])


def user_prompt(request: TalkRequest) -> str:
    return json.dumps(
        {
            "sheet": [{line.fact.value: line.text} for line in request.sheet],
            "conversation": [
                {"player": turn.player, "you": turn.reply}
                for turn in request.history[-HISTORY_TURNS:]
            ],
            "playerSays": request.text,
        },
        ensure_ascii=False,
        separators=(",", ":"),
    )


def invented_figures(reply: str, amounts: list[int], percents: list[int]) -> list[str]:
    """Amounts and percentages in the reply that the sheet does not have."""
    allowed = set(amounts)
    bad = [found for found in NUMBER.findall(reply) if int(re.sub(r"\D", "", found)) not in allowed]
    bad += [found + "%" for found in PERCENT.findall(reply) if int(found) not in percents]
    return bad


class TalkResponder:
    def __init__(self, provider: ModelProvider, settings: Settings) -> None:
        self._provider = provider
        self._settings = settings

    async def reply(self, ledger: Ledger, session_id: uuid.UUID, request: TalkRequest) -> TalkReply:
        model = select_text_model(
            self._settings.game_master_model, await ledger.budget_mode(), self._settings
        )
        if model is None or await ledger.limit_exceeded(session_id) is not None:
            raise TalkUnavailableError("no budget for a model")

        try:
            async with asyncio.timeout(self._settings.model_timeout_s):
                turn = await self._ask(ledger, session_id, model, request)
        except (TimeoutError, ProviderError) as error:
            logger.warning("debt talk failed (%s); the game uses its local rules", error)
            raise TalkUnavailableError(str(error)) from error
        return TalkReply(npc_reply=turn.npc_reply, asked_facts=turn.asked_facts, source="model")

    async def _ask(
        self, ledger: Ledger, session_id: uuid.UUID, model: str, request: TalkRequest
    ) -> TalkTurn:
        system = system_prompt(request.partner, request.language.value)
        user = user_prompt(request)
        for attempt in range(1, MAX_ATTEMPTS + 1):
            started = time.perf_counter()
            try:
                result = await self._provider.structured(
                    model=model,
                    system=system,
                    user=user,
                    output=TalkTurn,
                    reasoning_effort=self._settings.live_reasoning_effort,
                    max_tokens=2400,  # reasoning tokens count against this
                )
            except ProviderError:
                await ledger.record(
                    usage=Usage(model=model),
                    endpoint=ENDPOINT,
                    latency_ms=int((time.perf_counter() - started) * 1000),
                    ok=False,
                    session_id=session_id,
                )
                raise

            await ledger.record(
                usage=result.usage,
                endpoint=ENDPOINT,
                latency_ms=result.latency_ms,
                ok=True,
                session_id=session_id,
            )
            bad = invented_figures(result.value.npc_reply, request.amounts, request.percents)
            if not bad:
                return result.value
            logger.warning("talk reply with figures off the sheet (try %s): %s", attempt, bad)
        raise ProviderResponseError("talk replies kept inventing figures")

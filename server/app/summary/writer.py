"""Short year-end narrative written by Muse Wonder from the deterministic stats.

The player waits on a loading screen here, so the call gets SUMMARY_TIMEOUT_S instead of the 8 s
of live calls (ADR 0004).
"""

import asyncio
import json
import logging
import time
import uuid
from dataclasses import dataclass
from typing import Annotated

from pydantic import Field

from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.negotiation.questions import quoted_amounts
from app.negotiation.seller_sheet import format_colones
from app.providers.base import ModelProvider, ProviderError, Usage
from app.schemas.base import ContractModel
from app.summary.stats import SessionStats

logger = logging.getLogger(__name__)

ENDPOINT = "summary"

SYSTEM_PROMPT = """You write the closing message of "ColonQuest", a financial-literacy game \
for Costa Rican high-school students.

- Costa Rican Spanish, warm and direct, second person, at most 500 characters.
- Mention one thing the player did well and one habit to watch, based only on the data.
- Only quote amounts that appear in the data, written like ₡85 000. Never invent numbers.
- No lecturing: describe consequences, not right or wrong.

Answer with JSON only, matching the provided schema."""

SYSTEM_PROMPT_EN = """You write the closing message of "ColonQuest", a financial-literacy game \
for Costa Rican high-school students.

- Natural English, warm and direct, second person, at most 500 characters. \
Keep Costa Rican setting and colones amounts.
- Mention one thing the player did well and one habit to watch, based only on the data.
- Only quote amounts that appear in the data, written like ₡85 000. Never invent numbers.
- No lecturing: describe consequences, not right or wrong.

Answer with JSON only, matching the provided schema."""


def get_system_prompt(language: str = "es") -> str:
    if language and language.lower().startswith("en"):
        return SYSTEM_PROMPT_EN
    return SYSTEM_PROMPT


class SummaryText(ContractModel):
    narrative: Annotated[str, Field(min_length=1, max_length=600)]


@dataclass(frozen=True, slots=True)
class WrittenSummary:
    narrative: str
    source: str  # "model" or "fallback"


def stats_payload(stats: SessionStats, language: str = "es") -> str:
    payload = {
        "monthsPlayed": stats.months_played,
        "decisions": stats.decisions,
        "conceptsTaught": [concept.value for concept in stats.concepts_taught],
        "negotiations": stats.negotiations,
        "dealsAccepted": stats.deals_accepted,
        "keyQuestionsAsked": [question.value for question in stats.key_questions_asked],
        "totalDebt": format_colones(stats.total_debt),
        "netWorth": format_colones(stats.net_worth)
        if stats.net_worth >= 0
        else "-" + format_colones(-stats.net_worth),
        "wellbeing": stats.wellbeing,
    }
    if language and language != "es":
        payload["language"] = language
    return json.dumps(payload, ensure_ascii=False)


def fallback_narrative(stats: SessionStats, language: str = "es") -> str:
    if language and language.lower().startswith("en"):
        if stats.net_worth >= 0:
            opening = f"You closed the year with a net worth of {format_colones(stats.net_worth)}."
        else:
            opening = (
                f"You closed the year owing {format_colones(stats.total_debt)}, "
                "more than what you have saved."
            )
        if stats.key_questions_asked:
            count = len(stats.key_questions_asked)
            questions = (
                f"While negotiating you asked {count} of the key questions; "
                "asking about the total cost saves you from deceptive installments."
            )
        else:
            questions = "Next time you are offered installments, ask how much you pay in total."
        return f"{opening} {questions}"

    if stats.net_worth >= 0:
        opening = f"Cerraste el año con un patrimonio de {format_colones(stats.net_worth)}."
    else:
        opening = (
            f"Cerraste el año debiendo {format_colones(stats.total_debt)}, "
            "más de lo que tenés ahorrado."
        )
    if stats.key_questions_asked:
        questions = (
            f"Al negociar hiciste {len(stats.key_questions_asked)} de las preguntas clave; "
            "preguntar el costo total te salva de las cuotas engañosas."
        )
    else:
        questions = "La próxima vez que te ofrezcan cuotas, preguntá cuánto pagás en total."
    return f"{opening} {questions}"


class SummaryWriter:
    def __init__(self, provider: ModelProvider, settings: Settings) -> None:
        self._provider = provider
        self._settings = settings

    async def write(
        self,
        ledger: Ledger,
        session_id: uuid.UUID,
        stats: SessionStats,
        language: str = "es",
    ) -> WrittenSummary:
        mode = await ledger.budget_mode()
        model = select_text_model(self._settings.summary_model, mode, self._settings)
        if model is None:
            return WrittenSummary(fallback_narrative(stats, language=language), "fallback")

        started = time.perf_counter()
        try:
            async with asyncio.timeout(self._settings.summary_timeout_s):
                result = await self._provider.structured(
                    model=model,
                    system=get_system_prompt(language),
                    user=stats_payload(stats, language=language),
                    output=SummaryText,
                    reasoning_effort=self._settings.live_reasoning_effort,
                    max_tokens=2000,  # reasoning tokens count against this
                    timeout_s=self._settings.summary_timeout_s,
                )
        except (TimeoutError, ProviderError):
            await ledger.record(
                usage=Usage(model=model),
                endpoint=ENDPOINT,
                latency_ms=int((time.perf_counter() - started) * 1000),
                ok=False,
                session_id=session_id,
            )
            return WrittenSummary(fallback_narrative(stats, language=language), "fallback")

        await ledger.record(
            usage=result.usage,
            endpoint=ENDPOINT,
            latency_ms=result.latency_ms,
            ok=True,
            session_id=session_id,
        )
        narrative = result.value.narrative
        invented = [amount for amount in quoted_amounts(narrative) if amount not in stats.amounts]
        if invented:
            logger.warning("summary quoted invented amounts: %s", invented)
            return WrittenSummary(fallback_narrative(stats, language=language), "fallback")
        return WrittenSummary(narrative, "model")

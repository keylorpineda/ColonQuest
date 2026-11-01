"""Plain-language explanation of the class figures for the teacher.

Muse Spark only words the numbers it is given. Replies quoting other colón amounts, budget limits
or provider errors fall back to a scripted explanation.
"""

import asyncio
import json
import logging
import time
import uuid
from dataclasses import dataclass
from typing import Annotated

from pydantic import Field

from app.classes.metrics import ConceptMetric
from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.negotiation.questions import quoted_amounts
from app.negotiation.seller_sheet import format_colones
from app.providers.base import ModelProvider, ProviderError, Usage
from app.schemas.base import ContractModel

logger = logging.getLogger(__name__)

ENDPOINT = "class_dashboard"

CONCEPT_NAMES = {
    "budget": "presupuesto",
    "emergency_fund": "fondo de emergencia",
    "compound_interest": "interés compuesto",
    "minimum_payment": "pago mínimo",
    "installments_total_cost": "costo total de las cuotas",
    "informal_lending": "préstamos informales",
    "pyramid_scheme": "esquemas piramidales",
    "inflation": "inflación",
    "exchange_rate": "tipo de cambio",
    "payroll_deductions": "rebajos de planilla",
    "invest_in_yourself": "invertir en uno mismo",
}

CONCEPT_NAMES_EN = {
    "budget": "budgeting",
    "emergency_fund": "emergency fund",
    "compound_interest": "compound interest",
    "minimum_payment": "minimum payment",
    "installments_total_cost": "total cost of installments",
    "informal_lending": "informal loans",
    "pyramid_scheme": "pyramid schemes",
    "inflation": "inflation",
    "exchange_rate": "exchange rate",
    "payroll_deductions": "payroll deductions",
    "invest_in_yourself": "investing in yourself",
}

SYSTEM_PROMPT = """You explain classroom results of "Colón Quest", a financial-literacy game, to \
a Costa Rican high-school teacher.

- Spanish, clear and professional, at most 600 characters, no bullet points.
- Point out the concept with the highest share of risky choices and suggest one short classroom \
discussion about it. Mention how many negotiation turns caught the hidden costs.
- Use only the numbers in the data. Never invent figures or students.

Answer with JSON only, matching the provided schema."""

SYSTEM_PROMPT_EN = SYSTEM_PROMPT.replace("- Spanish, clear", "- English, clear")


class ExplanationText(ContractModel):
    explanation: Annotated[str, Field(min_length=1, max_length=700)]


@dataclass(frozen=True, slots=True)
class ClassFigures:
    students: int
    finished_games: int
    negotiations: int
    trap_detection_pct: int | None
    average_final_debt: int | None
    concepts: list[ConceptMetric]

    @property
    def amounts(self) -> frozenset[int]:
        return frozenset({self.average_final_debt} if self.average_final_debt is not None else ())


@dataclass(frozen=True, slots=True)
class Explanation:
    text: str
    source: str


def is_english(language: str) -> bool:
    return language.lower().startswith("en")


def figures_payload(figures: ClassFigures, language: str = "es") -> str:
    names = CONCEPT_NAMES_EN if is_english(language) else CONCEPT_NAMES
    return json.dumps(
        {
            "students": figures.students,
            "finishedGames": figures.finished_games,
            "negotiationTurns": figures.negotiations,
            "trapDetectionPct": figures.trap_detection_pct,
            "averageFinalDebt": (
                format_colones(figures.average_final_debt)
                if figures.average_final_debt is not None
                else None
            ),
            "concepts": [
                {
                    "concept": names[metric.concept.value],
                    "decisions": metric.decisions,
                    "riskyChoicePct": metric.risky_choice_pct,
                }
                for metric in figures.concepts
            ],
        },
        ensure_ascii=False,
    )


def fallback_explanation_en(figures: ClassFigures) -> str:
    if figures.students == 0:
        return "Nobody in this class has played yet. Share the code with your students."
    parts = [f"{figures.students} students played and {figures.finished_games} finished the year."]
    measured = [m for m in figures.concepts if m.risky_choice_pct is not None]
    if measured:
        worst = max(measured, key=lambda metric: metric.risky_choice_pct or 0)
        parts.append(
            "The topic with the most decisions that raised their debt was "
            f"{CONCEPT_NAMES_EN[worst.concept.value]} ({worst.risky_choice_pct}%); "
            "it is worth discussing in class."
        )
    if figures.trap_detection_pct is not None:
        parts.append(
            f"In the negotiations, {figures.trap_detection_pct}% of the turns asked about the "
            "total cost, the insurance or hidden charges."
        )
    return " ".join(parts)


def fallback_explanation(figures: ClassFigures, language: str = "es") -> str:
    if is_english(language):
        return fallback_explanation_en(figures)
    if figures.students == 0:
        return "Todavía nadie de esta clase ha jugado. Compartí el código con tus estudiantes."
    parts = [
        f"{figures.students} estudiantes jugaron y {figures.finished_games} terminaron el año."
    ]
    measured = [m for m in figures.concepts if m.risky_choice_pct is not None]
    if measured:
        worst = max(measured, key=lambda metric: metric.risky_choice_pct or 0)
        parts.append(
            f"El tema con más decisiones que aumentaron la deuda fue "
            f"{CONCEPT_NAMES[worst.concept.value]} ({worst.risky_choice_pct}%); "
            "vale la pena discutirlo en clase."
        )
    if figures.trap_detection_pct is not None:
        parts.append(
            f"En las negociaciones, el {figures.trap_detection_pct}% de los turnos preguntó por "
            "el costo total, el seguro o cargos escondidos."
        )
    return " ".join(parts)


class ClassExplainer:
    def __init__(self, provider: ModelProvider, settings: Settings) -> None:
        self._provider = provider
        self._settings = settings

    async def explain(
        self,
        ledger: Ledger,
        figures: ClassFigures,
        session_id: uuid.UUID | None = None,
        language: str = "es",
    ) -> Explanation:
        if figures.students == 0:
            return Explanation(fallback_explanation(figures, language), "fallback")
        model = select_text_model(
            self._settings.class_report_model, await ledger.budget_mode(), self._settings
        )
        if model is None:
            return Explanation(fallback_explanation(figures, language), "fallback")

        started = time.perf_counter()
        try:
            async with asyncio.timeout(self._settings.model_timeout_s):
                result = await self._provider.structured(
                    model=model,
                    system=SYSTEM_PROMPT_EN if is_english(language) else SYSTEM_PROMPT,
                    user=figures_payload(figures, language),
                    output=ExplanationText,
                    reasoning_effort=self._settings.live_reasoning_effort,
                    max_tokens=2000,  # reasoning tokens count against this
                )
        except (TimeoutError, ProviderError):
            await ledger.record(
                usage=Usage(model=model),
                endpoint=ENDPOINT,
                latency_ms=int((time.perf_counter() - started) * 1000),
                ok=False,
                session_id=session_id,
            )
            return Explanation(fallback_explanation(figures, language), "fallback")

        await ledger.record(
            usage=result.usage,
            endpoint=ENDPOINT,
            latency_ms=result.latency_ms,
            ok=True,
            session_id=session_id,
        )
        text = result.value.explanation
        if any(amount not in figures.amounts for amount in quoted_amounts(text)):
            logger.warning("class explanation quoted invented amounts")
            return Explanation(fallback_explanation(figures, language), "fallback")
        return Explanation(text, "model")

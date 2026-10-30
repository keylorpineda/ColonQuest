"""Season planner: Muse Wonder drafts the year's plan once per session; the default plan backs it.

The model only chooses the order of concepts and the themes. The plan schema enforces the
calendar rules, and any failure (budget, timeout, invalid answer) uses ``default_plan``. It runs
as a background task after the first event (6 to 18 s with Wonder), never on a live request.
"""

import asyncio
import json
import time
import uuid
from dataclasses import dataclass

from app.config import Settings
from app.cost_guard import select_text_model
from app.game_master.ledger import Ledger
from app.game_master.prompt import state_signals
from app.game_master.season import (
    CONCEPT_EVENT_TYPE,
    DECEMBER_TYPES,
    MIN_DISTINCT_CONCEPTS,
    SeasonPlan,
    default_plan,
)
from app.providers.base import ModelProvider, ProviderError, Usage
from app.schemas import EventType, PlayerState, TeachingConcept

ENDPOINT = "season_plan"


def _values(enum: type[EventType | TeachingConcept]) -> str:
    return ", ".join(member.value for member in enum)


SYSTEM_PROMPT = f"""You plan the 12-month season of "Colón Quest", a financial-literacy game \
for Costa Rican high-school students. Each month later gets one event built around your plan.

- Return exactly 12 months, numbered 1 to 12 in order.
- Cover at least {MIN_DISTINCT_CONCEPTS} different teaching concepts across the year.
- Put the concepts most relevant to the player's situation early (a maxed-out card means \
minimum_payment and compound_interest; informal loans mean informal_lending; large savings mean \
pyramid_scheme).
- Month 12 is December: aguinaldo and marchamo, type one of {", ".join(sorted(DECEMBER_TYPES))}.
- teachingConcept is one of: {_values(TeachingConcept)}
- eventType is one of: {_values(EventType)}
- theme: a short idea for the month in Costa Rican Spanish, at most 80 characters, no amounts.

Answer with JSON only, matching the provided schema."""


@dataclass(frozen=True, slots=True)
class PlannedSeason:
    plan: SeasonPlan
    source: str  # "model" or "default"


def build_user_prompt(state: PlayerState) -> str:
    payload = {
        "player": state.model_dump(
            mode="json", include={"salary_net", "cash", "savings", "emergency_fund", "wellbeing"}
        ),
        "signals": state_signals(state),
        "suggestedTypes": {c.value: t.value for c, t in CONCEPT_EVENT_TYPE.items()},
    }
    return "Plan the season for this player.\n" + json.dumps(
        payload, ensure_ascii=False, separators=(",", ":")
    )


class SeasonPlanner:
    def __init__(self, provider: ModelProvider, settings: Settings) -> None:
        self._provider = provider
        self._settings = settings

    async def plan(
        self, ledger: Ledger, session_id: uuid.UUID, state: PlayerState
    ) -> PlannedSeason:
        fallback = PlannedSeason(default_plan(state), "default")
        if await ledger.limit_exceeded(session_id) is not None:
            return fallback
        model = select_text_model(
            self._settings.planner_model, await ledger.budget_mode(), self._settings
        )
        if model is None:
            return fallback

        started = time.perf_counter()
        try:
            async with asyncio.timeout(self._settings.planner_timeout_s):
                result = await self._provider.structured(
                    model=model,
                    system=SYSTEM_PROMPT,
                    user=build_user_prompt(state),
                    output=SeasonPlan,
                    reasoning_effort=self._settings.live_reasoning_effort,
                    max_tokens=4000,  # 12 months of JSON plus the reasoning before it
                    timeout_s=self._settings.planner_timeout_s,
                )
        except (TimeoutError, ProviderError):
            await ledger.record(
                usage=Usage(model=model),
                endpoint=ENDPOINT,
                latency_ms=int((time.perf_counter() - started) * 1000),
                ok=False,
                session_id=session_id,
            )
            return fallback

        await ledger.record(
            usage=result.usage,
            endpoint=ENDPOINT,
            latency_ms=result.latency_ms,
            ok=True,
            session_id=session_id,
        )
        return PlannedSeason(result.value, "model")

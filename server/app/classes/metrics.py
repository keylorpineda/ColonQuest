"""Teacher dashboard figures. All numbers come from stored telemetry, never from a model.

A decision counts as "risky" when the player's total debt went up compared with the state after
their previous decision. That works for any event, model-written or hand-written.
"""

import uuid
from collections import defaultdict
from collections.abc import Iterable, Sequence
from dataclasses import dataclass
from typing import Any

from app.schemas import PlayerState, TeachingConcept
from app.summary.stats import total_debt

TRAP_QUESTIONS = frozenset({"total_cost", "hidden_fees", "insurance"})


@dataclass(frozen=True, slots=True)
class DecisionRow:
    session_id: uuid.UUID
    teaching_concept: str
    state_after: dict[str, Any]


@dataclass(frozen=True, slots=True)
class ConceptMetric:
    concept: TeachingConcept
    decisions: int
    risky_choice_pct: int | None  # None when no decision had a previous state to compare


def concept_metrics(rows: Iterable[DecisionRow]) -> list[ConceptMetric]:
    """Rows must be ordered by session and time (decision id)."""
    totals: dict[str, int] = defaultdict(int)
    compared: dict[str, int] = defaultdict(int)
    risky: dict[str, int] = defaultdict(int)
    previous_debt: dict[uuid.UUID, int] = {}

    for row in rows:
        debt = total_debt(PlayerState.model_validate(row.state_after))
        totals[row.teaching_concept] += 1
        before = previous_debt.get(row.session_id)
        if before is not None:
            compared[row.teaching_concept] += 1
            if debt > before:
                risky[row.teaching_concept] += 1
        previous_debt[row.session_id] = debt

    return [
        ConceptMetric(
            concept=concept,
            decisions=totals[concept.value],
            risky_choice_pct=(
                round(100 * risky[concept.value] / compared[concept.value])
                if compared[concept.value]
                else None
            ),
        )
        for concept in TeachingConcept
        if totals[concept.value]
    ]


def trap_detection_pct(key_questions_per_negotiation: Sequence[Sequence[str]]) -> int | None:
    """Share of negotiation turns where the player asked about total cost or hidden charges."""
    if not key_questions_per_negotiation:
        return None
    detected = sum(
        bool(TRAP_QUESTIONS & set(questions)) for questions in key_questions_per_negotiation
    )
    return round(100 * detected / len(key_questions_per_negotiation))


def average_final_debt(final_states: Iterable[dict[str, Any]]) -> int | None:
    debts = [total_debt(PlayerState.model_validate(state)) for state in final_states]
    return round(sum(debts) / len(debts)) if debts else None

"""Figures for the final summary, computed from the engine state and stored telemetry.

Nothing here comes from a model: the narrative may only repeat these numbers.
"""

from collections.abc import Iterable, Sequence
from dataclasses import dataclass

from app.schemas import KeyQuestion, PlayerState, TeachingConcept


@dataclass(frozen=True, slots=True)
class NegotiationRecord:
    key_questions: Sequence[str]
    deal_status: str


@dataclass(frozen=True, slots=True)
class SessionStats:
    months_played: int
    decisions: int
    concepts_taught: list[TeachingConcept]
    negotiations: int
    deals_accepted: int
    key_questions_asked: list[KeyQuestion]
    total_debt: int
    net_worth: int
    wellbeing: int

    @property
    def amounts(self) -> frozenset[int]:
        """Numbers the narrative is allowed to quote as colones."""
        return frozenset({self.total_debt, self.net_worth, abs(self.net_worth)})


def total_debt(state: PlayerState) -> int:
    installments = sum(item.monthly * item.months_left for item in state.installments)
    informal = sum(loan.balance for loan in state.informal_loans)
    return state.credit_card.balance + installments + informal


def net_worth(state: PlayerState) -> int:
    assets = state.cash + state.savings + state.emergency_fund
    return assets - total_debt(state)


def build_stats(
    state: PlayerState,
    decision_concepts: Sequence[str],
    negotiations: Iterable[NegotiationRecord],
) -> SessionStats:
    taught = {TeachingConcept(concept) for concept in decision_concepts}
    records = list(negotiations)
    asked = {KeyQuestion(question) for record in records for question in record.key_questions}
    return SessionStats(
        months_played=state.month,
        decisions=len(decision_concepts),
        concepts_taught=[concept for concept in TeachingConcept if concept in taught],
        negotiations=len(records),
        deals_accepted=sum(record.deal_status == "accepted" for record in records),
        key_questions_asked=[question for question in KeyQuestion if question in asked],
        total_debt=total_debt(state),
        net_worth=net_worth(state),
        wellbeing=state.wellbeing,
    )

"""Season plan: which teaching concept and kind of event each month of the year focuses on.

The plan is created once per session (by the model, or deterministically as a fallback) and
adjusted month by month without any model call: when a decision increases the player's debt,
that concept comes back in a later month as reinforcement.
"""

from collections.abc import Sequence
from typing import Annotated, Self

from pydantic import Field, model_validator

from app.schemas import EventType, PlayerState, TeachingConcept
from app.schemas.base import ContractModel

DECEMBER = 12
DECEMBER_TYPES = frozenset({EventType.BONUS, EventType.EXPENSE})
MIN_DISTINCT_CONCEPTS = 8

CONCEPT_EVENT_TYPE: dict[TeachingConcept, EventType] = {
    TeachingConcept.BUDGET: EventType.EXPENSE,
    TeachingConcept.EMERGENCY_FUND: EventType.EMERGENCY,
    TeachingConcept.COMPOUND_INTEREST: EventType.OFFER,
    TeachingConcept.MINIMUM_PAYMENT: EventType.EXPENSE,
    TeachingConcept.INSTALLMENTS_TOTAL_COST: EventType.OFFER,
    TeachingConcept.INFORMAL_LENDING: EventType.SCAM,
    TeachingConcept.PYRAMID_SCHEME: EventType.SCAM,
    TeachingConcept.INFLATION: EventType.MACRO_NEWS,
    TeachingConcept.EXCHANGE_RATE: EventType.MACRO_NEWS,
    TeachingConcept.PAYROLL_DEDUCTIONS: EventType.INCOME,
    TeachingConcept.INVEST_IN_YOURSELF: EventType.OFFER,
}

THEMES: dict[TeachingConcept, str] = {
    TeachingConcept.BUDGET: "Repartir el salario entre gastos fijos y gustos",
    TeachingConcept.EMERGENCY_FUND: "Un imprevisto que pone a prueba el colchón",
    TeachingConcept.COMPOUND_INTEREST: "Intereses que crecen solos, a favor o en contra",
    TeachingConcept.MINIMUM_PAYMENT: "La tentación de pagar solo el mínimo de la tarjeta",
    TeachingConcept.INSTALLMENTS_TOTAL_COST: "Una compra en cuotas con costos escondidos",
    TeachingConcept.INFORMAL_LENDING: "Plata rápida de un prestamista gota a gota",
    TeachingConcept.PYRAMID_SCHEME: "Un negocio que promete multiplicar los ahorros",
    TeachingConcept.INFLATION: "Todo sube de precio este mes",
    TeachingConcept.EXCHANGE_RATE: "Una compra en dólares y el tipo de cambio",
    TeachingConcept.PAYROLL_DEDUCTIONS: "Revisar la colilla: CCSS, renta y horas extra",
    TeachingConcept.INVEST_IN_YOURSELF: "Cursos del INA o la UNA: invertir en uno mismo",
}
DECEMBER_THEME = "Aguinaldo y gastos de fin de año"


class PlannedMonth(ContractModel):
    month: Annotated[int, Field(ge=1, le=12)]
    teaching_concept: TeachingConcept
    event_type: EventType
    theme: Annotated[str, Field(min_length=1, max_length=80)]


class SeasonPlan(ContractModel):
    months: Annotated[list[PlannedMonth], Field(min_length=12, max_length=12)]

    @model_validator(mode="after")
    def check_calendar(self) -> Self:
        if [month.month for month in self.months] != list(range(1, 13)):
            raise ValueError("plan must list months 1 to 12 in order")
        if self.months[DECEMBER - 1].event_type not in DECEMBER_TYPES:
            raise ValueError("December must be about aguinaldo or year-end expenses")
        if len({month.teaching_concept for month in self.months}) < MIN_DISTINCT_CONCEPTS:
            raise ValueError(f"plan must cover at least {MIN_DISTINCT_CONCEPTS} concepts")
        return self

    def for_month(self, month: int) -> PlannedMonth:
        return self.months[month - 1]


def concept_priority(state: PlayerState) -> list[TeachingConcept]:
    """Most relevant concepts first, based on the player's starting situation."""
    card = state.credit_card
    priority: list[TeachingConcept] = []
    if card.limit and card.balance * 100 >= card.limit * 50:
        priority += [TeachingConcept.MINIMUM_PAYMENT, TeachingConcept.COMPOUND_INTEREST]
    if state.informal_loans:
        priority.append(TeachingConcept.INFORMAL_LENDING)
    if state.installments:
        priority.append(TeachingConcept.INSTALLMENTS_TOTAL_COST)
    if state.emergency_fund == 0:
        priority.append(TeachingConcept.EMERGENCY_FUND)
    if state.savings + state.emergency_fund >= state.salary_net:
        priority.append(TeachingConcept.PYRAMID_SCHEME)
    rest = [concept for concept in TeachingConcept if concept not in priority]
    return list(dict.fromkeys([TeachingConcept.BUDGET, *priority, *rest]))


def default_plan(state: PlayerState) -> SeasonPlan:
    """Deterministic plan: every concept once in months 1-11, December is bonus/budget."""
    ordered = concept_priority(state)
    concepts = ordered[:11]
    if len(concepts) < 11 and ordered:
        concepts = ([*ordered, *ordered])[:11]
    months = [
        PlannedMonth(
            month=index + 1,
            teaching_concept=concept,
            event_type=CONCEPT_EVENT_TYPE[concept],
            theme=THEMES[concept],
        )
        for index, concept in enumerate(concepts)
    ]
    months.append(
        PlannedMonth(
            month=DECEMBER,
            teaching_concept=TeachingConcept.BUDGET,
            event_type=EventType.BONUS,
            theme=DECEMBER_THEME,
        )
    )
    return SeasonPlan(months=months)


def adjust_plan(
    plan: SeasonPlan, next_month: int, risky_concepts: Sequence[TeachingConcept]
) -> SeasonPlan:
    """Reinforce concepts the player struggled with in a later month (never December).

    Idempotent: a concept already planned for an upcoming month is left alone.
    """
    months = list(plan.months)
    for concept in dict.fromkeys(risky_concepts):
        upcoming = range(next_month + 1, DECEMBER)
        if any(months[m - 1].teaching_concept == concept for m in upcoming):
            continue
        replaceable = [m for m in upcoming if months[m - 1].teaching_concept not in risky_concepts]
        if not replaceable:
            continue
        target = replaceable[0]
        months[target - 1] = PlannedMonth(
            month=target,
            teaching_concept=concept,
            event_type=CONCEPT_EVENT_TYPE[concept],
            theme=("Refuerzo: " + THEMES[concept])[:80],
        )
    return SeasonPlan(months=months)

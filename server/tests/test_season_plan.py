from typing import Any

import pytest
from pydantic import ValidationError

from app.game_master.season import (
    SeasonPlan,
    adjust_plan,
    concept_priority,
    default_plan,
)
from app.schemas import EventType, PlayerState, TeachingConcept

BASE: dict[str, Any] = {
    "month": 1,
    "salaryNet": 450000,
    "cash": 120000,
    "savings": 0,
    "emergencyFund": 0,
    "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "wellbeing": 60,
}


def state(**overrides: Any) -> PlayerState:
    return PlayerState.model_validate(BASE | overrides)


def test_default_plan_covers_every_concept_and_december() -> None:
    plan = default_plan(state())
    assert [month.month for month in plan.months] == list(range(1, 13))
    assert {month.teaching_concept for month in plan.months} == set(TeachingConcept)
    assert plan.for_month(12).event_type == EventType.BONUS
    assert plan.for_month(1).teaching_concept == TeachingConcept.BUDGET


def test_maxed_card_brings_minimum_payment_forward() -> None:
    card = {"balance": 270000, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10}
    plan = default_plan(state(creditCard=card))
    assert plan.for_month(2).teaching_concept == TeachingConcept.MINIMUM_PAYMENT
    assert plan.for_month(3).teaching_concept == TeachingConcept.COMPOUND_INTEREST


def test_big_savings_bring_the_pyramid_scheme_early() -> None:
    priority = concept_priority(state(savings=500000, emergencyFund=100000))
    assert priority.index(TeachingConcept.PYRAMID_SCHEME) <= 2
    assert TeachingConcept.EMERGENCY_FUND not in priority[:2]


def test_plan_validation_rules() -> None:
    months = [m.model_dump() for m in default_plan(state()).months]
    with pytest.raises(ValidationError):
        SeasonPlan.model_validate({"months": months[:11]})
    shuffled = [months[1], months[0], *months[2:]]
    with pytest.raises(ValidationError):
        SeasonPlan.model_validate({"months": shuffled})
    december_scam = [*months[:11], months[11] | {"eventType": "scam"}]
    with pytest.raises(ValidationError):
        SeasonPlan.model_validate({"months": december_scam})
    monotone = [m | {"teachingConcept": "budget"} for m in months]
    with pytest.raises(ValidationError):
        SeasonPlan.model_validate({"months": monotone})


def test_risky_concept_is_reinforced_once_in_a_later_month() -> None:
    plan = default_plan(state())
    first_slot = next(
        m.month for m in plan.months if m.teaching_concept == TeachingConcept.MINIMUM_PAYMENT
    )
    adjusted = adjust_plan(
        plan, next_month=first_slot + 1, risky_concepts=[TeachingConcept.MINIMUM_PAYMENT]
    )

    reinforced = [
        m
        for m in adjusted.months
        if m.month > first_slot + 1 and m.teaching_concept == TeachingConcept.MINIMUM_PAYMENT
    ]
    assert len(reinforced) == 1
    assert reinforced[0].theme.startswith("Refuerzo")
    assert adjusted.for_month(12) == plan.for_month(12)
    again = adjust_plan(
        adjusted, next_month=first_slot + 1, risky_concepts=[TeachingConcept.MINIMUM_PAYMENT]
    )
    assert again == adjusted


def test_no_room_left_keeps_the_plan() -> None:
    plan = default_plan(state())
    assert adjust_plan(plan, next_month=11, risky_concepts=[TeachingConcept.INFLATION]) == plan


def test_default_plan_follows_the_language_of_the_player() -> None:
    spanish = default_plan(state())
    english = default_plan(state(language="en"))

    assert [m.teaching_concept for m in english.months] == [
        m.teaching_concept for m in spanish.months
    ]
    assert english.months[0].theme == "Splitting the salary between bills and treats"
    assert "Aguinaldo (the year-end bonus)" in english.months[-1].theme
    assert all(m.theme != n.theme for m, n in zip(english.months, spanish.months, strict=True))


def test_adjust_plan_reinforces_in_the_language_of_the_player() -> None:
    plan = default_plan(state(language="en"))
    concept = plan.months[1].teaching_concept

    adjusted = adjust_plan(plan, 3, [concept], english=True)

    assert any(m.theme.startswith("Practice again: ") for m in adjusted.months)

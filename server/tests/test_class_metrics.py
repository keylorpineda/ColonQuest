import uuid
from typing import Any

from app.classes.metrics import (
    DecisionRow,
    average_final_debt,
    concept_metrics,
    trap_detection_pct,
)
from app.schemas import TeachingConcept


def state(card_balance: int, month: int = 1) -> dict[str, Any]:
    return {
        "month": month,
        "salaryNet": 450000,
        "cash": 100000,
        "savings": 0,
        "emergencyFund": 0,
        "creditCard": {
            "balance": card_balance,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "wellbeing": 60,
    }


def test_risky_choice_is_debt_increase_versus_previous_decision() -> None:
    a, b = uuid.uuid4(), uuid.uuid4()
    rows = [
        DecisionRow(a, "budget", state(0)),  # first decision: nothing to compare
        DecisionRow(a, "emergency_fund", state(85000)),  # debt up: risky
        DecisionRow(a, "minimum_payment", state(60000)),  # debt down
        DecisionRow(b, "budget", state(50000)),
        DecisionRow(b, "emergency_fund", state(50000)),  # unchanged: not risky
    ]

    metrics = {metric.concept: metric for metric in concept_metrics(rows)}

    assert metrics[TeachingConcept.BUDGET].decisions == 2
    assert metrics[TeachingConcept.BUDGET].risky_choice_pct is None
    assert metrics[TeachingConcept.EMERGENCY_FUND].risky_choice_pct == 50
    assert metrics[TeachingConcept.MINIMUM_PAYMENT].risky_choice_pct == 0
    assert TeachingConcept.PYRAMID_SCHEME not in metrics


def test_trap_detection() -> None:
    assert trap_detection_pct([]) is None
    assert trap_detection_pct([["total_cost"], ["term"], ["insurance"], []]) == 50


def test_average_final_debt() -> None:
    assert average_final_debt([]) is None
    assert average_final_debt([state(100000), state(50001)]) == 75000

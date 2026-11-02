from app.schemas import KeyQuestion, PlayerState, TeachingConcept
from app.schemas.player_state import Installment
from app.summary.stats import (
    HEAVY_DEBT_FROM,
    NegotiationRecord,
    build_stats,
    net_worth,
    total_debt,
    year_card_key,
)

STATE = PlayerState.model_validate(
    {
        "month": 12,
        "salaryNet": 450000,
        "cash": 80000,
        "savings": 120000,
        "emergencyFund": 50000,
        "creditCard": {
            "balance": 150000,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "installments": [{"item": "Celular", "monthly": 19600, "monthsLeft": 10}],
        "informalLoans": [{"lender": "El Flaco", "balance": 40000, "monthlyRatePct": 20}],
        "wellbeing": 70,
    }
)


def test_debt_and_net_worth() -> None:
    assert total_debt(STATE) == 150000 + 196000 + 40000
    assert net_worth(STATE) == 250000 - 386000


def test_build_stats_orders_and_deduplicates() -> None:
    stats = build_stats(
        STATE,
        decision_concepts=["budget", "emergency_fund", "budget"],
        negotiations=[
            NegotiationRecord(["insurance", "total_cost"], "open"),
            NegotiationRecord(["total_cost"], "accepted"),
        ],
    )

    assert stats.decisions == 3
    assert stats.concepts_taught == [TeachingConcept.BUDGET, TeachingConcept.EMERGENCY_FUND]
    assert stats.key_questions_asked == [KeyQuestion.TOTAL_COST, KeyQuestion.INSURANCE]
    assert stats.negotiations == 2
    assert stats.deals_accepted == 1
    assert stats.net_worth == -136000
    assert {386000, -136000, 136000} == stats.amounts


def test_empty_game() -> None:
    stats = build_stats(STATE, decision_concepts=[], negotiations=[])
    assert stats.decisions == 0
    assert stats.concepts_taught == []
    assert stats.key_questions_asked == []


def test_year_card_follows_final_debt() -> None:
    assert year_card_key(0) == "card_year_debt_free"
    assert year_card_key(1) == "card_year_some_debt"
    assert year_card_key(HEAVY_DEBT_FROM - 1) == "card_year_some_debt"
    assert year_card_key(HEAVY_DEBT_FROM) == "card_year_heavy_debt"
    assert build_stats(STATE, [], []).card_image_key == "card_year_some_debt"


def test_rent_is_not_a_debt_and_what_was_bought_counts() -> None:
    state = STATE.model_copy(
        update={
            "installments": [
                *STATE.installments,
                Installment(item="Alquiler: Casa Vecina D", monthly=260000, months_left=12),
            ],
            "assets_value": 2720000,
        }
    )

    assert total_debt(state) == total_debt(STATE), "the lease adds nothing to what is owed"
    assert net_worth(state) == net_worth(STATE) + 2720000


def test_the_months_played_come_from_the_run_when_it_says_them() -> None:
    assert build_stats(STATE, [], []).months_played == 12
    closed = STATE.model_copy(update={"month": 4, "months_played": 3})
    assert build_stats(closed, [], []).months_played == 3

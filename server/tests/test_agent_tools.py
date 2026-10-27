import json

import pytest

from app.agents.tools import (
    TOOLS,
    HistoryItem,
    ProjectDebtArgs,
    SimulatePurchaseArgs,
    ToolContext,
    execute_tool,
    project_debt,
    simulate_purchase,
    tool_schemas,
)
from app.negotiation.seller_sheet import OFFERS
from app.providers.base import ToolCall
from app.schemas import TeachingConcept

CONTEXT = ToolContext(
    history=[HistoryItem(1, "evt_moto_dañada", "emergency_fund", "pay_card")],
    taught=[TeachingConcept.EMERGENCY_FUND],
)


def call(name: str, args: object) -> ToolCall:
    return ToolCall(id="c1", name=name, arguments=json.dumps(args))


def test_simulate_purchase_matches_the_seller_sheet() -> None:
    offer = OFFERS["celular_gama_alta"]
    result = simulate_purchase(
        SimulatePurchaseArgs(
            price=offer.cash_price,
            months=offer.term_months,
            monthly_rate_pct=float(offer.monthly_rate_pct),
            insurance_monthly=offer.insurance_monthly,
            commission=offer.commission,
        )
    )
    assert result["monthlyPayment"] == offer.monthly_payment == 19600
    assert result["totalCost"] == offer.total_cost == 569400
    assert result["overCashPrice"] == 219400
    assert all(isinstance(value, int) for value in result.values())


def test_minimum_payment_debt_never_pays_off() -> None:
    result = project_debt(
        ProjectDebtArgs(balance=180000, monthly_rate_pct=3.5, monthly_payment=6000, months=12)
    )
    assert result["monthsToPayOff"] is None
    assert result["neverPaysOff"] is True
    assert result["finalBalance"] > 180000
    assert len(result["balances"]) == 12


def test_debt_paid_off_stops_early() -> None:
    result = project_debt(
        ProjectDebtArgs(balance=100000, monthly_rate_pct=2, monthly_payment=60000, months=12)
    )
    # 100 000 + 2 000 - 60 000 = 42 000; 42 000 + 840 - 60 000 -> 0
    assert result["balances"] == [42000, 0]
    assert result["monthsToPayOff"] == 2
    assert result["totalInterest"] == 2840


def test_history_and_concepts_read_only_the_context() -> None:
    history = execute_tool(call("get_player_history", {}), CONTEXT, TOOLS)
    concepts = execute_tool(call("get_concepts_not_yet_taught", {}), CONTEXT, TOOLS)
    assert history == {
        "decisions": [
            {
                "month": 1,
                "eventId": "evt_moto_dañada",
                "teachingConcept": "emergency_fund",
                "choiceId": "pay_card",
            }
        ]
    }
    assert "emergency_fund" not in concepts["notTaughtYet"]
    assert len(concepts["notTaughtYet"]) == len(TeachingConcept) - 1


@pytest.mark.parametrize(
    ("tool_call", "message"),
    [
        (ToolCall("c", "transfer_money", "{}"), "unknown tool"),
        (ToolCall("c", "project_debt", "not json"), "invalid arguments"),
        (
            call(
                "project_debt",
                {"balance": -5, "monthly_rate_pct": 1, "monthly_payment": 1, "months": 1},
            ),
            "invalid arguments",
        ),
        (
            call(
                "simulate_purchase", {"price": 1000, "months": 6, "monthly_rate_pct": 2, "extra": 1}
            ),
            "invalid arguments",
        ),
    ],
)
def test_bad_calls_return_errors_instead_of_raising(tool_call: ToolCall, message: str) -> None:
    assert message in execute_tool(tool_call, CONTEXT, TOOLS)["error"]


def test_disallowed_tool_is_rejected() -> None:
    result = execute_tool(call("get_player_history", {}), CONTEXT, allowed=["simulate_purchase"])
    assert "unknown tool" in result["error"]


def test_schemas_are_openai_function_tools() -> None:
    schemas = tool_schemas(TOOLS)
    names = {schema["function"]["name"] for schema in schemas}
    assert names == set(TOOLS)
    simulate = next(s for s in schemas if s["function"]["name"] == "simulate_purchase")
    assert simulate["type"] == "function"
    assert simulate["function"]["parameters"]["additionalProperties"] is False
    assert "price" in simulate["function"]["parameters"]["required"]


def test_quote_term_reprices_the_offer_and_keeps_earnings_private() -> None:
    from app.agents.tools import quotable_amounts

    offer = OFFERS["celular_gama_alta"]
    context = ToolContext(offer=offer)
    same = execute_tool(ToolCall("c1", "quote_term", '{"months": 24}'), context, ["quote_term"])
    longer = execute_tool(ToolCall("c2", "quote_term", '{"months": 36}'), context, ["quote_term"])

    assert same["monthlyPayment"] == offer.monthly_payment
    assert same["totalCost"] == offer.total_cost
    assert longer["monthlyPayment"] < same["monthlyPayment"]
    assert longer["totalCost"] > same["totalCost"]
    assert longer["sellerEarningsPrivate"] > same["sellerEarningsPrivate"] > offer.commission

    allowed = quotable_amounts([same, longer])
    assert longer["monthlyWithInsurance"] in allowed
    assert longer["sellerEarningsPrivate"] not in allowed


def test_quote_term_needs_an_offer_and_a_sane_term() -> None:
    no_offer = execute_tool(ToolCall("c1", "quote_term", '{"months": 24}'), CONTEXT, ["quote_term"])
    assert "error" in no_offer
    context = ToolContext(offer=OFFERS["moto_125"])
    too_long = execute_tool(
        ToolCall("c2", "quote_term", '{"months": 120}'), context, ["quote_term"]
    )
    assert too_long["error"].startswith("invalid arguments")

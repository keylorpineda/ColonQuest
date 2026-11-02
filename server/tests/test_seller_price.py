from typing import Any

import pytest

from app.agents.tools import QUOTABLE_KEYS, QuoteTermArgs, ToolContext, quote_term
from app.negotiation.agent import SELLER_TOOLS, get_agent_system_prompt, term_offer
from app.negotiation.questions import wrong_cash_prices
from app.negotiation.seller_sheet import MAX_TERM_MONTHS, OFFERS, offer_on_the_table

PHONE = OFFERS["celular_gama_alta"]


def test_the_seller_has_no_tool_that_takes_a_price() -> None:
    assert SELLER_TOOLS == ("quote_term",)


@pytest.mark.parametrize("language", ["en", "es"])
def test_the_seller_quotes_a_longer_term_at_once(language: str) -> None:
    assert "never promise to check it later" in get_agent_system_prompt(language)


@pytest.mark.parametrize(
    "reply",
    [
        "The price is ₡569 400, friend.",
        "De contado sale en ₡400 000, mae.",
        "In cash it comes out to ₡299 000.",
        "El precio es ₡19 600.",
    ],
)
def test_any_other_amount_called_the_price_is_wrong(reply: str) -> None:
    assert wrong_cash_prices(reply, PHONE)


@pytest.mark.parametrize(
    "reply",
    [
        "De contado sale en ₡350 000, mae.",
        "The cash price is ₡350,000.",
        "Son solo ₡19 600 al mes.",
        "In total you pay ₡569 400.",
        "El precio total es ₡569 400.",
        "The total price is ₡569 400.",
        "A ese precio son ₡19 600 al mes.",
    ],
)
def test_the_true_price_and_the_other_figures_pass(reply: str) -> None:
    assert wrong_cash_prices(reply, PHONE) == []


def test_another_term_keeps_the_price_and_moves_the_payment_and_the_total() -> None:
    longer = PHONE.at_term(36)

    assert longer.cash_price == PHONE.cash_price
    assert longer.commission == PHONE.commission
    assert longer.monthly_payment < PHONE.monthly_payment
    assert longer.total_cost > PHONE.total_cost
    assert wrong_cash_prices("De contado sale en ₡350 000.", longer) == []


def test_the_tool_and_the_sheet_agree_on_a_term() -> None:
    quoted = quote_term(QuoteTermArgs(months=36), ToolContext(offer=PHONE))
    sheet = PHONE.at_term(36)

    assert quoted["monthlyPayment"] == sheet.monthly_payment
    assert quoted["totalCost"] == sheet.total_cost
    assert "sellerEarningsPrivate" not in QUOTABLE_KEYS


def test_the_seller_cannot_quote_below_the_offers_own_term_but_can_come_back_to_it() -> None:
    longer = ToolContext(offer=PHONE.at_term(36))

    assert "error" in quote_term(QuoteTermArgs(months=12), longer)
    assert quote_term(QuoteTermArgs(months=24), longer)["months"] == 24


@pytest.mark.parametrize(
    ("term", "expected"),
    [(None, 24), (0, 24), (12, 24), (36, 36), (48, 48), (60, 24), (MAX_TERM_MONTHS + 1, 24)],
)
def test_the_term_on_the_table_is_only_one_the_seller_could_quote(
    term: int | None, expected: int
) -> None:
    assert offer_on_the_table(PHONE, term).term_months == expected


def test_the_last_term_quoted_is_the_one_on_the_table() -> None:
    results: list[dict[str, Any]] = [{"months": 36}, {"months": 48}]

    assert term_offer(PHONE, results).term_months == 48
    assert term_offer(PHONE, []).term_months == 24
    assert term_offer(PHONE.at_term(36), [{"months": 24}]).term_months == 24

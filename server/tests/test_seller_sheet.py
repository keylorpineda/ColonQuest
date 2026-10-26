from decimal import Decimal

import pytest

from app.negotiation.seller_sheet import OFFERS, Offer, format_colones, get_offer
from app.schemas import SellerFact


def test_format_colones() -> None:
    assert format_colones(31500) == "₡31 500"
    assert format_colones(1_200_000) == "₡1 200 000"
    assert format_colones(0) == "₡0"


def test_celular_amortization() -> None:
    offer = get_offer("celular_gama_alta")
    assert offer is not None
    # 350 000 at 2.5 % monthly over 24 months = 19 569.4..., rounded up to 19 600
    assert offer.monthly_payment == 19_600
    assert offer.total_cost == (19_600 + 3_500) * 24 + 15_000


def test_zero_interest_offer() -> None:
    offer = Offer("x", "x", "x", 120_000, 12, Decimal(0), 0, 0)
    assert offer.monthly_payment == 10_000
    assert offer.total_cost == 120_000


@pytest.mark.parametrize("offer", OFFERS.values(), ids=lambda offer: offer.offer_id)
def test_every_offer_costs_more_than_cash(offer: Offer) -> None:
    assert isinstance(offer.monthly_payment, int)
    assert offer.monthly_payment % 100 == 0
    assert offer.total_cost > offer.cash_price
    assert set(offer.facts) == set(SellerFact)
    assert offer.total_cost in offer.allowed_amounts


def test_unknown_offer() -> None:
    assert get_offer("lancha") is None

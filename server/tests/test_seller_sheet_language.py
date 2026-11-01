import json

from app.negotiation.questions import fallback_reply
from app.negotiation.seller_sheet import OFFERS
from app.negotiation.service import build_user_prompt
from app.schemas import KeyQuestion, SellerFact

OFFER = OFFERS["celular_gama_alta"]


def test_every_offer_has_an_english_name() -> None:
    assert all(offer.item_en for offer in OFFERS.values())


def test_the_english_sheet_has_the_same_figures_in_english() -> None:
    assert OFFER.facts_en[SellerFact.TERM_MONTHS] == "24 months"
    assert OFFER.facts_en[SellerFact.INTEREST_RATE] == "2.5% a month"
    assert OFFER.facts_en[SellerFact.INSURANCE_FEE] == "₡3 500 a month"
    assert OFFER.facts_en[SellerFact.TOTAL_COST] == OFFER.facts[SellerFact.TOTAL_COST]


def test_the_seller_gets_the_sheet_in_the_language_of_the_player() -> None:
    english = json.loads(build_user_prompt(OFFER, [], "Any hidden fees?", language="en"))
    spanish = json.loads(build_user_prompt(OFFER, [], "¿Algún cargo?"))

    assert english["product"] == "high-end phone"
    assert english["hiddenSheet"]["term_months"] == "24 months"
    assert spanish["product"] == "celular de gama alta"
    assert spanish["hiddenSheet"]["term_months"] == "24 meses"


def test_the_scripted_seller_speaks_only_english() -> None:
    pitch, _ = fallback_reply(OFFER, set(), language="en")
    answer, _ = fallback_reply(OFFER, {KeyQuestion.INSURANCE, KeyQuestion.TERM}, language="en")

    assert "high-end phone" in pitch
    assert "a month" in answer
    assert "meses" not in answer
    assert "al mes" not in answer

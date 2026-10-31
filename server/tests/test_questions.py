import pytest

from app.negotiation.questions import (
    classify_questions,
    facts_answering,
    fallback_reply,
    invalid_amounts,
    normalize,
    quoted_amounts,
)
from app.negotiation.seller_sheet import OFFERS
from app.schemas import KeyQuestion, SellerFact

OFFER = OFFERS["celular_gama_alta"]


def test_normalize_removes_accents_and_case() -> None:
    assert normalize("¿Cuánto es el INTERÉS?") == "¿cuanto es el interes?"


@pytest.mark.parametrize(
    ("text", "expected"),
    [
        ("¿Cuánto pago en total con los seguros?", {KeyQuestion.TOTAL_COST, KeyQuestion.INSURANCE}),
        ("¿Y cuál es la tasa de interés?", {KeyQuestion.INTEREST_RATE}),
        ("¿Hay alguna comisión o cargo extra?", {KeyQuestion.HIDDEN_FEES}),
        ("¿Cuántos meses son?", {KeyQuestion.TERM}),
        ("¿Y de contado cuánto sale?", {KeyQuestion.CASH_PRICE}),
        ("¿Hay letra pequeña?", {KeyQuestion.HIDDEN_FEES}),
        ("Diay mae, qué tuanis el celular", set()),
        ("How much is it in total with insurance?", {KeyQuestion.TOTAL_COST, KeyQuestion.INSURANCE}),
        ("What is the interest rate?", {KeyQuestion.INTEREST_RATE}),
        ("Are there any extra fees or commission?", {KeyQuestion.HIDDEN_FEES}),
        ("How many months is the term?", {KeyQuestion.TERM}),
        ("How much in cash upfront?", {KeyQuestion.CASH_PRICE}),
        ("Is there any fine print?", {KeyQuestion.HIDDEN_FEES}),
    ],
)
def test_classify_questions(text: str, expected: set[KeyQuestion]) -> None:
    assert classify_questions(text) == expected


@pytest.mark.parametrize(
    ("text", "expected"),
    [
        ("La cuota es de ₡19 600 al mes", [19600]),
        ("son ₡569.400 en total y ₡3,500 de seguro", [569400, 3500]),
        ("le sale en 350 000 colones", [350000]),
        ("solo 24 meses", []),
        ("₡15000 de comisión", [15000]),
        ("₡19" + chr(0xA0) + "600 con espacio duro", [19600]),
        ("₡1a234 no es un monto válido", [1]),
    ],
)
def test_quoted_amounts(text: str, expected: list[int]) -> None:
    assert quoted_amounts(text) == expected


def test_invalid_amounts_detects_made_up_figures() -> None:
    assert invalid_amounts("La cuota es ₡19 600 y el seguro ₡3 500", OFFER) == []
    assert invalid_amounts("Tranquilo, el total es ₡400 000", OFFER) == [400000]


def test_facts_answering_keeps_stable_order_without_duplicates() -> None:
    facts = facts_answering({KeyQuestion.INSURANCE, KeyQuestion.HIDDEN_FEES})
    assert facts == [SellerFact.COMMISSION, SellerFact.INSURANCE_FEE]


def test_fallback_reply_reveals_only_what_was_asked() -> None:
    reply, revealed = fallback_reply(OFFER, {KeyQuestion.TOTAL_COST})
    assert revealed == [SellerFact.TOTAL_COST]
    assert "₡569 400" in reply
    assert invalid_amounts(reply, OFFER) == []


def test_fallback_pitch_when_nothing_was_asked() -> None:
    reply, revealed = fallback_reply(OFFER, set())
    assert revealed == [SellerFact.MONTHLY_PAYMENT]
    assert "₡19 600" in reply


def test_fallback_reply_english() -> None:
    pitch, revealed = fallback_reply(OFFER, set(), language="en")
    assert revealed == [SellerFact.MONTHLY_PAYMENT]
    assert "Dude" in pitch
    assert "₡19 600" in pitch

    reply, revealed = fallback_reply(OFFER, {KeyQuestion.TOTAL_COST}, language="en")
    assert revealed == [SellerFact.TOTAL_COST]
    assert "in total you end up paying" in reply
    assert "₡569 400" in reply

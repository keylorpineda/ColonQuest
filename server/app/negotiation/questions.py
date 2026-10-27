"""Deterministic analysis of a negotiation turn.

- classify_questions: which key questions the player asked (impact metric, free and repeatable).
- quoted_amounts / invalid_amounts: the seller may hide figures but never lie about them, so any
  colón amount in a reply that is not on the hidden sheet makes the reply invalid.
- fallback_reply: a scripted answer used when the model is unavailable.
"""

import re
import unicodedata
from collections.abc import Collection

from app.negotiation.seller_sheet import Offer
from app.schemas import KeyQuestion, SellerFact

QUESTION_PATTERNS: dict[KeyQuestion, tuple[str, ...]] = {
    KeyQuestion.TOTAL_COST: (
        r"\btotal\b",
        r"al final",
        r"cuanto (?:pago|sale|cuesta|termino pagando|seria) (?:todo|en todo)",
        r"\bcosto (?:real|completo)",
    ),
    KeyQuestion.HIDDEN_FEES: (
        r"comision",
        r"cargos?\b",
        r"cobros? (?:extra|adicional)",
        r"costos? (?:extra|adicional|oculto|escondido)",
        r"letra (?:pequena|chiquita)",
        r"algo mas que (?:pagar|cobren)",
        r"escondid",
    ),
    KeyQuestion.INTEREST_RATE: (r"interes", r"\btasa\b"),
    KeyQuestion.TERM: (
        r"\bplazo\b",
        r"cuantos meses",
        r"cuantas cuotas",
        r"cuanto tiempo",
        r"por cuantos",
    ),
    KeyQuestion.CASH_PRICE: (
        r"contado",
        r"precio real",
        r"sin cuotas",
        r"en efectivo",
        r"de una vez",
    ),
    KeyQuestion.INSURANCE: (r"seguro", r"poliza"),
}

FACTS_FOR_QUESTION: dict[KeyQuestion, tuple[SellerFact, ...]] = {
    KeyQuestion.TOTAL_COST: (SellerFact.TOTAL_COST,),
    KeyQuestion.HIDDEN_FEES: (SellerFact.COMMISSION, SellerFact.INSURANCE_FEE),
    KeyQuestion.INTEREST_RATE: (SellerFact.INTEREST_RATE,),
    KeyQuestion.TERM: (SellerFact.TERM_MONTHS,),
    KeyQuestion.CASH_PRICE: (SellerFact.CASH_PRICE,),
    KeyQuestion.INSURANCE: (SellerFact.INSURANCE_FEE,),
}

# Thousands separators: space, dot, comma, no-break space and narrow no-break space.
NO_BREAK_SPACE, NARROW_NO_BREAK_SPACE = chr(0xA0), chr(0x202F)
THOUSANDS_SEPARATORS = f"[ .,{NO_BREAK_SPACE}{NARROW_NO_BREAK_SPACE}]"
# ₡85 000, ₡85.000, ₡85,000, 85 000 colones, 85000 colones
AMOUNT_PATTERN = re.compile(
    rf"₡\s?(\d{{1,3}}(?:{THOUSANDS_SEPARATORS}\d{{3}})+|\d+)"
    rf"|(\d{{1,3}}(?:{THOUSANDS_SEPARATORS}\d{{3}})+|\d{{4,}})\s*(?:colones|rojos|cañas)",
    re.IGNORECASE,
)


def normalize(text: str) -> str:
    """Lowercase without accents so "interés", "Interes" and "INTERÉS" match the same pattern."""
    decomposed = unicodedata.normalize("NFKD", text.lower())
    return "".join(char for char in decomposed if not unicodedata.combining(char))


def classify_questions(text: str) -> set[KeyQuestion]:
    normalized = normalize(text)
    return {
        question
        for question, patterns in QUESTION_PATTERNS.items()
        if any(re.search(pattern, normalized) for pattern in patterns)
    }


def quoted_amounts(text: str) -> list[int]:
    amounts = []
    for match in AMOUNT_PATTERN.finditer(text):
        digits = re.sub(r"\D", "", match.group(1) or match.group(2))
        if digits:
            amounts.append(int(digits))
    return amounts


def invalid_amounts(reply: str, offer: Offer, extra: Collection[int] = ()) -> list[int]:
    """Amounts in the reply that are neither on the seller's sheet nor computed by its tools."""
    return [
        amount
        for amount in quoted_amounts(reply)
        if amount not in offer.allowed_amounts and amount not in extra
    ]


def facts_answering(questions: set[KeyQuestion]) -> list[SellerFact]:
    facts: list[SellerFact] = []
    for question in KeyQuestion:
        if question in questions:
            facts.extend(fact for fact in FACTS_FOR_QUESTION[question] if fact not in facts)
    return facts


def fallback_reply(offer: Offer, questions: set[KeyQuestion]) -> tuple[str, list[SellerFact]]:
    """Scripted seller answer that only reveals what the player asked about."""
    facts = facts_answering(questions)
    if not facts:
        pitch = (
            f"Mae, este {offer.item} sale a solo {offer.facts[SellerFact.MONTHLY_PAYMENT]} "
            "al mes. ¡Se lo lleva hoy mismo!"
        )
        return pitch, [SellerFact.MONTHLY_PAYMENT]
    labels = {
        SellerFact.CASH_PRICE: "de contado sale en",
        SellerFact.MONTHLY_PAYMENT: "la cuota es de",
        SellerFact.TERM_MONTHS: "el plazo es de",
        SellerFact.INTEREST_RATE: "la tasa es de",
        SellerFact.INSURANCE_FEE: "el seguro es de",
        SellerFact.COMMISSION: "la comisión de apertura es de",
        SellerFact.TOTAL_COST: "en total terminás pagando",
    }
    parts = [f"{labels[fact]} {offer.facts[fact]}" for fact in facts]
    return "Bueno, diay... " + "; ".join(parts) + ".", facts

import copy
from typing import Any

import pytest
from pydantic import ValidationError

from app.schemas import GameEvent, NegotiationReply, PlayerState

PLAYER_STATE: dict[str, Any] = {
    "month": 3,
    "salaryNet": 450000,
    "cash": 120000,
    "savings": 50000,
    "emergencyFund": 0,
    "creditCard": {"balance": 180000, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
    "installments": [{"item": "Celular", "monthly": 25000, "monthsLeft": 33}],
    "informalLoans": [],
    "history": ["paid_minimum_only", "skipped_savings"],
    "wellbeing": 62,
}

GAME_EVENT: dict[str, Any] = {
    "eventId": "evt_moto_dañada",
    "type": "emergency",
    "title": "Se te dañó la moto",
    "narrative": "Texto corto en español tico, máximo 280 caracteres.",
    "npc": {"id": "mecanico_don_rafa", "name": "Don Rafa", "role": "mechanic"},
    "imageKey": "scene_taller_mecanico",
    "choices": [
        {
            "id": "pay_cash",
            "label": "Pagar con efectivo",
            "effects": [{"kind": "expense", "amount": 85000, "source": "cash"}],
        },
        {
            "id": "card",
            "label": "Pasarlo a la tarjeta",
            "effects": [{"kind": "expense", "amount": 85000, "source": "credit_card"}],
        },
    ],
    "teachingConcept": "emergency_fund",
}

NEGOTIATION_REPLY: dict[str, Any] = {
    "transcript": "¿Cuánto pago en total con los seguros?",
    "npcReply": "Mae, la cuota es nada... bueno, con el seguro son ₡31 500 al mes.",
    "revealedFacts": ["insurance_fee", "total_cost"],
    "keyQuestionsAsked": ["total_cost", "hidden_fees"],
    "dealStatus": "open",
}


def test_contract_examples_round_trip() -> None:
    for model, payload in (
        (PlayerState, PLAYER_STATE),
        (GameEvent, GAME_EVENT),
        (NegotiationReply, NEGOTIATION_REPLY),
    ):
        parsed = model.model_validate(payload)
        assert parsed.model_dump(mode="json", exclude_unset=True) == payload


def test_python_names_are_snake_case() -> None:
    state = PlayerState.model_validate(PLAYER_STATE)
    assert state.salary_net == 450000
    assert state.credit_card.monthly_rate_pct == 3.5


@pytest.mark.parametrize("amount", [85000.0, 85000.5, "85000"])
def test_money_must_be_integer_colones(amount: object) -> None:
    payload = copy.deepcopy(GAME_EVENT)
    payload["choices"][0]["effects"][0]["amount"] = amount
    with pytest.raises(ValidationError):
        GameEvent.model_validate(payload)


def test_cash_cannot_be_negative() -> None:
    with pytest.raises(ValidationError):
        PlayerState.model_validate({**PLAYER_STATE, "cash": -1})


def test_narrative_max_280_characters() -> None:
    with pytest.raises(ValidationError):
        GameEvent.model_validate({**GAME_EVENT, "narrative": "a" * 281})


@pytest.mark.parametrize(
    ("field", "value"),
    [("type", "lottery"), ("teachingConcept", "crypto"), ("eventId", "Has Spaces")],
)
def test_closed_enums_and_ids(field: str, value: str) -> None:
    with pytest.raises(ValidationError):
        GameEvent.model_validate({**GAME_EVENT, field: value})


def test_unknown_fields_are_rejected() -> None:
    with pytest.raises(ValidationError):
        GameEvent.model_validate({**GAME_EVENT, "secretBonus": 1})


def test_event_needs_two_to_four_unique_choices() -> None:
    one_choice = {**GAME_EVENT, "choices": GAME_EVENT["choices"][:1]}
    duplicated = {**GAME_EVENT, "choices": [GAME_EVENT["choices"][0]] * 2}
    for payload in (one_choice, duplicated):
        with pytest.raises(ValidationError):
            GameEvent.model_validate(payload)


@pytest.mark.parametrize(
    "effect",
    [
        {"kind": "expense", "amount": -100, "source": "cash"},
        {"kind": "expense", "amount": 100},
        {"kind": "new_installment", "amount": 25000},
        {"kind": "new_informal_loan", "amount": 50000},
    ],
)
def test_effect_rules(effect: dict[str, Any]) -> None:
    payload = copy.deepcopy(GAME_EVENT)
    payload["choices"][0]["effects"] = [effect]
    with pytest.raises(ValidationError):
        GameEvent.model_validate(payload)


def test_signed_effects_allow_negative_amounts() -> None:
    payload = copy.deepcopy(GAME_EVENT)
    payload["choices"][0]["effects"] = [{"kind": "wellbeing_change", "amount": -10}]
    event = GameEvent.model_validate(payload)
    assert event.choices[0].effects[0].amount == -10

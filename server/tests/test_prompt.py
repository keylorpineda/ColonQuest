import json

from app.game_master.prompt import (
    SYSTEM_PROMPT,
    PromptContext,
    build_user_prompt,
    state_signals,
)
from app.schemas import EffectKind, EventType, PlayerState, TeachingConcept

STATE = PlayerState.model_validate(
    {
        "month": 12,
        "salaryNet": 400000,
        "cash": 60000,
        "savings": 50000,
        "emergencyFund": 0,
        "creditCard": {
            "balance": 270000,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "installments": [{"item": "Celular", "monthly": 25000, "monthsLeft": 20}],
        "history": [f"h{i}" for i in range(10)],
        "wellbeing": 55,
    }
)


def test_system_prompt_lists_every_enum_value_and_range() -> None:
    for enum in (EventType, EffectKind, TeachingConcept):
        for member in enum:
            assert member.value in SYSTEM_PROMPT
    assert "20000 to 250000" in SYSTEM_PROMPT  # emergency range
    assert "280 characters" in SYSTEM_PROMPT


def test_system_prompt_is_stable_for_input_caching() -> None:
    from app.game_master import prompt

    # Built once at import: identical bytes on every call keep the provider's input cache warm.
    assert prompt.SYSTEM_PROMPT is SYSTEM_PROMPT
    assert str(STATE.salary_net) not in SYSTEM_PROMPT


def test_state_signals() -> None:
    signals = state_signals(STATE)
    assert signals == {
        "cardUtilizationPct": 90,
        "monthlyInstallments": 25000,
        "hasInformalLoans": False,
        "isDecember": True,
        "lowCash": True,
    }


def test_user_prompt_payload() -> None:
    context = PromptContext(
        state=STATE,
        recent_event_ids=["evt_moto_dañada"],
        taught_concepts=[TeachingConcept.BUDGET],
    )
    text = build_user_prompt(context)
    payload = json.loads(text.split("\n", 1)[1])

    assert payload["month"] == 12
    assert payload["avoid"] == ["evt_moto_dañada"]
    assert "budget" not in payload["notTaughtYet"]
    assert len(payload["notTaughtYet"]) == len(TeachingConcept) - 1
    assert payload["recentHistory"] == [f"h{i}" for i in range(4, 10)]
    assert payload["player"]["salaryNet"] == 400000
    assert "evt_moto_dañada" in text  # not ASCII-escaped

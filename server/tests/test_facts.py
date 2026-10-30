import copy
import uuid
from typing import Any

import pytest

from app.game_master.facts import assumes_a_vehicle, owns_vehicle
from app.game_master.fallback import VEHICLE_EVENT_IDS, pick_fallback_event
from app.game_master.prompt import PromptContext, state_signals
from app.schemas import GameEvent, PlayerState

from .fakes import MODEL_EVENT, FakeLedger, FakeProvider, ok
from .test_game_master import make_gm

SESSION_ID = uuid.uuid4()


def player(**changes: Any) -> PlayerState:
    return PlayerState.model_validate(
        {
            "month": 3,
            "salaryNet": 332686,
            "cash": 120000,
            "savings": 50000,
            "emergencyFund": 0,
            "creditCard": {
                "balance": 0,
                "limit": 300000,
                "monthlyRatePct": 3.5,
                "minPaymentPct": 10,
            },
            "wellbeing": 60,
            **changes,
        }
    )


def event_about(narrative: str, event_type: str = "emergency") -> dict[str, Any]:
    event = copy.deepcopy(MODEL_EVENT)
    event["type"] = event_type
    event["narrative"] = narrative
    return event


MOTO = player(installments=[{"item": "moto 125 cc", "monthly": 30000, "monthsLeft": 40}])
CAR = player(history=["bought_car"])
BROKEN_BIKE = "Se te dañó la moto camino al brete."


@pytest.mark.parametrize(("state", "expected"), [(player(), False), (MOTO, True), (CAR, True)])
def test_owns_vehicle_reads_the_state(state: PlayerState, expected: bool) -> None:
    assert owns_vehicle(state) is expected
    assert state_signals(state)["hasVehicle"] is expected


def test_a_motor_is_not_a_motorbike() -> None:
    fridge = player(installments=[{"item": "Motor de la refri", "monthly": 9000, "monthsLeft": 6}])
    assert not owns_vehicle(fridge)


def test_an_expense_for_a_vehicle_needs_one() -> None:
    event = GameEvent.model_validate(event_about(BROKEN_BIKE))
    assert assumes_a_vehicle(event, player())
    assert not assumes_a_vehicle(event, MOTO)


@pytest.mark.parametrize("event_type", ["offer", "scam", "macro_news", "income", "bonus"])
def test_other_events_may_mention_a_vehicle(event_type: str) -> None:
    event = GameEvent.model_validate(event_about("Un señor en moto te busca.", event_type))
    assert not assumes_a_vehicle(event, player())


def test_fallback_skips_vehicles_without_one() -> None:
    for month in (3, 12):
        state = player(month=month)
        picks = {pick_fallback_event(state).event_id for _ in range(200)}
        assert not picks & VEHICLE_EVENT_IDS


def test_fallback_can_use_vehicles_with_one() -> None:
    picks = {pick_fallback_event(MOTO).event_id for _ in range(600)}
    assert "evt_moto_dañada" in picks


@pytest.mark.anyio
async def test_model_event_about_a_missing_vehicle_is_retried() -> None:
    provider = FakeProvider(ok(event_about(BROKEN_BIKE)), ok())
    outcome = await make_gm(provider).next_event(
        FakeLedger(), SESSION_ID, PromptContext(state=player())
    )

    assert outcome.source == "model"
    assert outcome.event.narrative == MODEL_EVENT["narrative"]
    assert len(provider.calls) == 2


@pytest.mark.anyio
async def test_model_insisting_on_a_missing_vehicle_falls_back() -> None:
    provider = FakeProvider(ok(event_about(BROKEN_BIKE)), ok(event_about(BROKEN_BIKE)))
    outcome = await make_gm(provider).next_event(
        FakeLedger(), SESSION_ID, PromptContext(state=player())
    )

    assert outcome.source == "fallback"
    assert outcome.fallback_reason == "invalid_response"
    assert outcome.event.event_id not in VEHICLE_EVENT_IDS

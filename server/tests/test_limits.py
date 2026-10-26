import pytest

from app.game_master.limits import (
    CASH_FLOW_RANGES,
    KIND_RANGES,
    amount_range,
    clamp_effect,
    clamp_event,
)
from app.schemas import Effect, EffectKind, EventType, GameEvent, MoneySource


def expense(amount: int) -> Effect:
    return Effect(kind=EffectKind.EXPENSE, amount=amount, source=MoneySource.CASH)


def event_with(effects: list[Effect], event_type: EventType = EventType.EMERGENCY) -> GameEvent:
    return GameEvent.model_validate(
        {
            "eventId": "evt_test",
            "type": event_type,
            "title": "Prueba",
            "narrative": "Algo pasó.",
            "choices": [
                {"id": "a", "label": "Opción A", "effects": [e.model_dump() for e in effects]},
                {"id": "b", "label": "Opción B", "effects": []},
            ],
            "teachingConcept": "emergency_fund",
        }
    )


def test_every_event_type_and_kind_has_a_range() -> None:
    assert set(CASH_FLOW_RANGES) == set(EventType)
    for event_type in EventType:
        for kind in EffectKind:
            allowed = amount_range(event_type, kind)
            assert allowed.low <= allowed.high


@pytest.mark.parametrize(
    ("amount", "expected"),
    [(1_000_000, 250_000), (250_000, 250_000), (85_000, 85_000), (20_000, 20_000), (1, 20_000)],
)
def test_emergency_expense_boundaries(amount: int, expected: int) -> None:
    clamped, notes = clamp_effect(EventType.EMERGENCY, expense(amount))
    assert clamped.amount == expected
    assert bool(notes) == (amount != expected)


@pytest.mark.parametrize("kind", list(KIND_RANGES))
def test_kind_ranges_apply_regardless_of_event_type(kind: EffectKind) -> None:
    allowed = KIND_RANGES[kind]
    months = 6 if kind in (EffectKind.NEW_INSTALLMENT, EffectKind.NEW_INFORMAL_LOAN) else None
    too_big = Effect(kind=kind, amount=allowed.high + 1, months=months)
    clamped, _ = clamp_effect(EventType.OFFER, too_big)
    assert clamped.amount == allowed.high


def test_negative_wellbeing_is_clamped_to_floor() -> None:
    effect = Effect(kind=EffectKind.WELLBEING_CHANGE, amount=-90)
    clamped, _ = clamp_effect(EventType.SCAM, effect)
    assert clamped.amount == -20


def test_installment_months_are_clamped() -> None:
    effect = Effect(kind=EffectKind.NEW_INSTALLMENT, amount=25_000, months=60)
    clamped, notes = clamp_effect(EventType.OFFER, effect)
    assert clamped.months == 48
    assert any("months" in note for note in notes)


def test_clamp_event_keeps_valid_effects_and_reports_changes() -> None:
    event = event_with([expense(85_000), expense(900_000)])
    safe, notes = clamp_event(event)
    amounts = [effect.amount for effect in safe.choices[0].effects]
    assert amounts == [85_000, 250_000]
    assert notes == ["a.expense: amount 900000 -> 250000"]
    assert safe.event_id == event.event_id


def test_clamped_event_is_still_valid_contract() -> None:
    safe, _ = clamp_event(event_with([expense(10**8)], EventType.BONUS))
    GameEvent.model_validate(safe.model_dump())

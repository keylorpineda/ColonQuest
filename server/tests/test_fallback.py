import random

import pytest

from app.game_master.fallback import (
    DECEMBER_EVENT_IDS,
    load_fallback_events,
    pick_fallback_event,
)
from app.game_master.limits import clamp_event
from app.schemas import PlayerState, TeachingConcept

STATE = PlayerState.model_validate(
    {
        "month": 3,
        "salaryNet": 450000,
        "cash": 120000,
        "savings": 50000,
        "emergencyFund": 0,
        "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
        "wellbeing": 60,
    }
)


def test_catalog_size_and_unique_ids() -> None:
    events = load_fallback_events()
    assert 15 <= len(events) <= 20
    ids = [event.event_id for event in events]
    assert len(ids) == len(set(ids))


def test_catalog_covers_every_teaching_concept() -> None:
    covered = {event.teaching_concept for event in load_fallback_events()}
    assert covered == set(TeachingConcept)


@pytest.mark.parametrize("event", load_fallback_events(), ids=lambda event: event.event_id)
def test_fallback_amounts_are_already_inside_limits(event: object) -> None:
    clamped, notes = clamp_event(event)  # type: ignore[arg-type]
    assert notes == []
    assert clamped == event


def test_avoids_recent_events() -> None:
    events = load_fallback_events()
    recent = [event.event_id for event in events[:-1]]
    picked = pick_fallback_event(STATE, recent_event_ids=recent, rng=random.Random(1))
    assert picked.event_id not in recent or picked.event_id in DECEMBER_EVENT_IDS


def test_december_prefers_aguinaldo_or_marchamo() -> None:
    december = STATE.model_copy(update={"month": 12})
    for seed in range(10):
        picked = pick_fallback_event(december, rng=random.Random(seed))
        assert picked.event_id in DECEMBER_EVENT_IDS


def test_other_months_skip_december_events() -> None:
    for seed in range(30):
        picked = pick_fallback_event(STATE, rng=random.Random(seed))
        assert picked.event_id not in DECEMBER_EVENT_IDS


def test_prefers_untaught_concepts() -> None:
    taught = [concept for concept in TeachingConcept if concept != TeachingConcept.PYRAMID_SCHEME]
    picked = pick_fallback_event(STATE, taught_concepts=taught, rng=random.Random(3))
    assert picked.teaching_concept == TeachingConcept.PYRAMID_SCHEME


def test_never_fails_when_everything_is_recent() -> None:
    all_ids = [event.event_id for event in load_fallback_events()]
    assert pick_fallback_event(STATE, recent_event_ids=all_ids, rng=random.Random(0))

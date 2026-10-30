"""Hand-written fallback events used when the model fails, times out or the budget runs out."""

import random
from collections.abc import Sequence
from functools import lru_cache
from pathlib import Path

from pydantic import TypeAdapter

from app.game_master.facts import owns_vehicle
from app.schemas import GameEvent, PlayerState, TeachingConcept

FALLBACK_EVENTS_PATH = Path(__file__).with_name("fallback_events.json")
DECEMBER_EVENT_IDS = frozenset({"evt_aguinaldo", "evt_marchamo"})
VEHICLE_EVENT_IDS = frozenset({"evt_moto_dañada", "evt_marchamo"})

_events_adapter = TypeAdapter(list[GameEvent])


@lru_cache
def load_fallback_events() -> tuple[GameEvent, ...]:
    """Load and validate the catalog once, so bad content fails at startup, not mid-game."""
    events = _events_adapter.validate_json(FALLBACK_EVENTS_PATH.read_bytes())
    return tuple(events)


def pick_fallback_event(
    state: PlayerState,
    recent_event_ids: Sequence[str] = (),
    taught_concepts: Sequence[TeachingConcept] = (),
    rng: random.Random | None = None,
) -> GameEvent:
    """Pick a fallback that fits the month and avoids repeating recent events.

    December prefers aguinaldo and marchamo; otherwise events teaching concepts the player has
    not seen yet come first. Events about a vehicle are only for a player who has one.
    """
    rng = rng or random.Random()  # noqa: S311 - gameplay variety, not security
    events = load_fallback_events()
    if not owns_vehicle(state):
        events = tuple(event for event in events if event.event_id not in VEHICLE_EVENT_IDS)
    recent = set(recent_event_ids)
    fresh = [event for event in events if event.event_id not in recent] or list(events)

    if state.month == 12:
        december = [event for event in fresh if event.event_id in DECEMBER_EVENT_IDS]
        if december:
            return rng.choice(december)
    else:
        fresh = [event for event in fresh if event.event_id not in DECEMBER_EVENT_IDS] or fresh

    taught = set(taught_concepts)
    untaught = [event for event in fresh if event.teaching_concept not in taught]
    return rng.choice(untaught or fresh)

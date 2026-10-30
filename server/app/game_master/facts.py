"""What the player really has, so an event never talks about a thing the player does not own.

The contract does not carry the player's belongings, but what matters here can be read from the
state: a car leaves its mark in the history, and a car or a motorbike bought in installments stays
in the installments.
"""

import re

from app.schemas import EventType, GameEvent, PlayerState

VEHICLE_WORDS = re.compile(
    r"\b(motos?|motocicletas?|carros?|marchamo|embrague|llantas?|riteve|rtv|gasolina)\b",
    re.IGNORECASE,
)
BOUGHT_CAR = "bought_car"
FRIDGE_WORDS = re.compile(r"\b(refri|refris|refrigeradora|refrigerador)\b", re.IGNORECASE)
BOUGHT_FRIDGE = "bought_furniture_fridge"
WASHER_WORDS = re.compile(r"\b(lavadora|lavadoras)\b", re.IGNORECASE)

# In these types something happens to the player's own things. An offer or a scam may well talk
# about a motorbike the player could buy, or about a lender who comes riding one.
OWN_THINGS_EVENTS = frozenset({EventType.EXPENSE, EventType.EMERGENCY})


def owns_vehicle(state: PlayerState) -> bool:
    if BOUGHT_CAR in state.history:
        return True
    return any(VEHICLE_WORDS.search(item.item) for item in state.installments)


def owns_fridge(state: PlayerState) -> bool:
    return BOUGHT_FRIDGE in state.history or any(
        FRIDGE_WORDS.search(item.item) for item in state.installments
    )


def assumes_a_vehicle(event: GameEvent, state: PlayerState) -> bool:
    """True when the event charges the player for a vehicle the player does not have."""
    return not owns_vehicle(state) and _mentions(event, VEHICLE_WORDS)


def assumes_missing_thing(event: GameEvent, state: PlayerState) -> bool:
    """True when the event charges the player for a vehicle or an appliance the player lacks."""
    return (
        assumes_a_vehicle(event, state)
        or (not owns_fridge(state) and _mentions(event, FRIDGE_WORDS))
        or _mentions(event, WASHER_WORDS)  # the game sells no washing machine for the room
    )


def _mentions(event: GameEvent, words: re.Pattern[str]) -> bool:
    if event.type not in OWN_THINGS_EVENTS:
        return False
    texts = [event.title, event.narrative, *(choice.label for choice in event.choices)]
    return any(words.search(text) for text in texts)

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

# In these types something happens to the player's own things. An offer or a scam may well talk
# about a motorbike the player could buy, or about a lender who comes riding one.
OWN_THINGS_EVENTS = frozenset({EventType.EXPENSE, EventType.EMERGENCY})


def owns_vehicle(state: PlayerState) -> bool:
    if BOUGHT_CAR in state.history:
        return True
    return any(VEHICLE_WORDS.search(item.item) for item in state.installments)


def assumes_a_vehicle(event: GameEvent, state: PlayerState) -> bool:
    """True when the event charges the player for a vehicle the player does not have."""
    if event.type not in OWN_THINGS_EVENTS or owns_vehicle(state):
        return False
    texts = [event.title, event.narrative, *(choice.label for choice in event.choices)]
    return any(VEHICLE_WORDS.search(text) for text in texts)

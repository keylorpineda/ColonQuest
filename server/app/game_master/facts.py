"""What the user really has, so an event never talks about a thing the user does not own.

The contract does not carry the user's belongings, but what matters here can be read from the
state: a car leaves its mark in the history, and a car or a motorbike bought in installments stays
in the installments.
"""

import re

from app.schemas import EffectKind, EventType, GameEvent, PlayerState

VEHICLE_WORDS = re.compile(
    r"\b(motos?|motocicletas?|carros?|marchamo|embrague|llantas?|riteve|rtv|gasolina)\b",
    re.IGNORECASE,
)
BOUGHT_CAR = "bought_car"
FRIDGE_WORDS = re.compile(r"\b(refri|refris|refrigeradora|refrigerador)\b", re.IGNORECASE)
BOUGHT_FRIDGE = "bought_furniture_fridge"
WASHER_WORDS = re.compile(r"\b(lavadora|lavadoras)\b", re.IGNORECASE)

# In these types something happens to the user's own things. An offer or a scam may well talk
# about a motorbike the user could buy, or about a lender who comes riding one.
OWN_THINGS_EVENTS = frozenset({EventType.EXPENSE, EventType.EMERGENCY})

# The close of the month already charges these: a choice that pays them would charge them twice.
FIXED_COSTS = re.compile(
    r"\b(rent|room|bus pass|bills?|utilit\w+|alquiler|cuarto|pase de bus|recibos?)\b",
    re.IGNORECASE,
)
AMOUNT = re.compile(r"₡\s?(\d[\d\s.,]*\d|\d)")


def owns_vehicle(state: PlayerState) -> bool:
    if BOUGHT_CAR in state.history:
        return True
    return any(VEHICLE_WORDS.search(item.item) for item in state.installments)


def owns_fridge(state: PlayerState) -> bool:
    return BOUGHT_FRIDGE in state.history or any(
        FRIDGE_WORDS.search(item.item) for item in state.installments
    )


def assumes_a_vehicle(event: GameEvent, state: PlayerState) -> bool:
    """True when the event charges the user for a vehicle the user does not have."""
    return not owns_vehicle(state) and _mentions(event, VEHICLE_WORDS)


def assumes_missing_thing(event: GameEvent, state: PlayerState) -> bool:
    """True when the event charges the user for a vehicle or an appliance the user lacks."""
    return (
        assumes_a_vehicle(event, state)
        or (not owns_fridge(state) and _mentions(event, FRIDGE_WORDS))
        or _mentions(event, WASHER_WORDS)  # the simulator sells no washing machine for the room
    )


def charges_fixed_costs(event: GameEvent) -> bool:
    """True when a choice charges the rent or the bills, which the month close charges anyway."""
    return any(
        FIXED_COSTS.search(choice.label)
        and any(effect.kind is EffectKind.EXPENSE for effect in choice.effects)
        for choice in event.choices
    )


def label_lies_about_money(event: GameEvent) -> bool:
    """True when a choice names an amount that is not what it moves: the label promises one
    figure and the engine applies another."""
    for choice in event.choices:
        amounts = {effect.amount for effect in choice.effects}
        amounts.add(sum(e.amount for e in choice.effects if e.kind is EffectKind.EXPENSE))
        for found in AMOUNT.findall(choice.label):
            if int(re.sub(r"\D", "", found)) not in amounts:
                return True
    return False


def is_dishonest(event: GameEvent) -> bool:
    """True when the event would charge something twice or say one figure and apply another."""
    return charges_fixed_costs(event) or label_lies_about_money(event)


def _mentions(event: GameEvent, words: re.Pattern[str]) -> bool:
    if event.type not in OWN_THINGS_EVENTS:
        return False
    texts = [event.title, event.narrative, *(choice.label for choice in event.choices)]
    return any(words.search(text) for text in texts)

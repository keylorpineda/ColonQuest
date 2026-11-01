"""Allowed ranges for effect amounts. The model proposes, these limits decide.

Money ranges are in whole colones and depend on the event type, so an "emergency" can never
cost a million colones just because the model said so. The Unity finance engine validates
again on its side.
"""

from dataclasses import dataclass

from app.images.keys import SCENE_BY_CONCEPT, SCENE_KEYS
from app.schemas import Effect, EffectKind, EventType, GameEvent


@dataclass(frozen=True, slots=True)
class Range:
    low: int
    high: int

    def clamp(self, value: int) -> int:
        return max(self.low, min(self.high, value))


# Cash-flow effects (expense / income) by event type.
CASH_FLOW_RANGES: dict[EventType, Range] = {
    EventType.INCOME: Range(10_000, 300_000),
    EventType.EXPENSE: Range(5_000, 150_000),
    EventType.EMERGENCY: Range(20_000, 250_000),
    EventType.OFFER: Range(10_000, 600_000),
    EventType.SCAM: Range(10_000, 500_000),
    EventType.MACRO_NEWS: Range(0, 100_000),
    EventType.BONUS: Range(10_000, 450_000),
}

# Effects whose meaning does not depend on the event type.
KIND_RANGES: dict[EffectKind, Range] = {
    EffectKind.NEW_INSTALLMENT: Range(5_000, 80_000),  # monthly payment
    EffectKind.NEW_INFORMAL_LOAN: Range(20_000, 300_000),  # principal
    EffectKind.SAVINGS_CHANGE: Range(-300_000, 300_000),
    EffectKind.WELLBEING_CHANGE: Range(-20, 20),  # points on a 0-100 scale
    EffectKind.PRICE_INFLATION: Range(1, 15),  # percent
}

MONTH_RANGES: dict[EffectKind, Range] = {
    EffectKind.NEW_INSTALLMENT: Range(3, 48),
    EffectKind.NEW_INFORMAL_LOAN: Range(1, 12),
}


def amount_range(event_type: EventType, kind: EffectKind) -> Range:
    if kind in (EffectKind.EXPENSE, EffectKind.INCOME):
        return CASH_FLOW_RANGES[event_type]
    return KIND_RANGES[kind]


def clamp_effect(event_type: EventType, effect: Effect) -> tuple[Effect, list[str]]:
    """Return the effect with amount and months inside the allowed range."""
    notes: list[str] = []
    update: dict[str, int] = {}

    allowed = amount_range(event_type, effect.kind)
    amount = allowed.clamp(effect.amount)
    if amount != effect.amount:
        update["amount"] = amount
        notes.append(f"{effect.kind}: amount {effect.amount} -> {amount}")

    month_range = MONTH_RANGES.get(effect.kind)
    if month_range is not None and effect.months is not None:
        months = month_range.clamp(effect.months)
        if months != effect.months:
            update["months"] = months
            notes.append(f"{effect.kind}: months {effect.months} -> {months}")

    return (effect.model_copy(update=update) if update else effect), notes


def clamp_event(event: GameEvent) -> tuple[GameEvent, list[str]]:
    """Clamp every effect of every choice and the scene. Returns the safe event and the changes."""
    notes: list[str] = []
    image_key = event.image_key
    if image_key is not None and image_key not in SCENE_KEYS:
        image_key = SCENE_BY_CONCEPT[event.teaching_concept]
        notes.append(f"imageKey: {event.image_key} -> {image_key}")

    choices = []
    for choice in event.choices:
        effects = []
        for effect in choice.effects:
            clamped, effect_notes = clamp_effect(event.type, effect)
            effects.append(clamped)
            notes.extend(f"{choice.id}.{note}" for note in effect_notes)
        choices.append(choice.model_copy(update={"effects": effects}))
    return event.model_copy(update={"choices": choices, "image_key": image_key}), notes

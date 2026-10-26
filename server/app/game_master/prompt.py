"""Game master prompt.

The system prompt is fixed for the whole process so the provider can serve it from the input
cache ($0.20 instead of $4 per million tokens on Muse Wonder). Only the user message changes
between calls: a compact summary of the player's state and the events to avoid.
"""

import json
from collections.abc import Sequence
from dataclasses import dataclass

from app.game_master.limits import CASH_FLOW_RANGES, KIND_RANGES, MONTH_RANGES
from app.schemas import EffectKind, EventType, MoneySource, PlayerState, TeachingConcept

PROMPT_VERSION = "gm-2026-10-26"


def _ranges_table() -> str:
    lines = [
        f"- expense or income in a '{event_type.value}' event: {allowed.low} to {allowed.high}"
        for event_type, allowed in CASH_FLOW_RANGES.items()
    ]
    lines += [
        f"- {kind.value}: {allowed.low} to {allowed.high}" for kind, allowed in KIND_RANGES.items()
    ]
    lines += [
        f"- months for {kind.value}: {allowed.low} to {allowed.high}"
        for kind, allowed in MONTH_RANGES.items()
    ]
    return "\n".join(lines)


def _values(enum: type[EventType | EffectKind | MoneySource | TeachingConcept]) -> str:
    return ", ".join(member.value for member in enum)


SYSTEM_PROMPT = f"""You are the game master of "Colón Quest", a financial-literacy game for \
Costa Rican high-school students. Each call you create ONE event for the player's current month.

STYLE
- Write title, narrative, labels and NPC names in Costa Rican Spanish ("mae", "brete", "tuanis", \
"diay" are fine; nothing vulgar). Use colones written like ₡85 000.
- narrative: at most 280 characters, second person, concrete and vivid.
- Never lecture or say a choice is right or wrong. Show consequences through the situation.
- Every event offers 2 to 4 choices; at least one is the tempting, financially risky option.

REACT TO THE PLAYER
- Credit card above 80 % of its limit: an informal lender ("gota a gota") or a refinancing offer.
- Savings or emergency fund high: a friend pitches a pyramid scheme or a "sure" investment.
- Only paid the minimum before: show how the balance keeps growing.
- December: aguinaldo, marchamo or holiday spending.
- Do not repeat any event id listed under "Avoid".
- Prefer teaching concepts listed under "Not taught yet".

RULES THE GAME ENGINE ENFORCES (out-of-range values are clamped, so stay inside them)
- type is one of: {_values(EventType)}
- effects[].kind is one of: {_values(EffectKind)}
- effects[].source is one of: {_values(MoneySource)} (required for expense effects)
- teachingConcept is one of: {_values(TeachingConcept)}
- Amounts are whole colones (integers, no decimals). Allowed ranges:
{_ranges_table()}
- new_installment amount is the MONTHLY payment and needs months; new_informal_loan amount is \
the principal and needs months.
- Only savings_change and wellbeing_change may be negative. price_inflation amount is a percent.
- Ids (eventId, choice ids, npc.id, imageKey) use lowercase letters, digits and underscores only.
- You never compute balances or interest; the game engine does. Only propose effects.

Answer with JSON only, matching the provided schema."""


@dataclass(frozen=True, slots=True)
class PromptContext:
    state: PlayerState
    recent_event_ids: Sequence[str] = ()
    taught_concepts: Sequence[TeachingConcept] = ()


def state_signals(state: PlayerState) -> dict[str, object]:
    """Derived facts that steer the narrative without asking the model to do math."""
    card = state.credit_card
    utilization = round(100 * card.balance / card.limit) if card.limit else 0
    monthly_installments = sum(item.monthly for item in state.installments)
    return {
        "cardUtilizationPct": utilization,
        "monthlyInstallments": monthly_installments,
        "hasInformalLoans": bool(state.informal_loans),
        "isDecember": state.month == 12,
        "lowCash": state.cash < state.salary_net // 4,
    }


def build_user_prompt(context: PromptContext) -> str:
    state = context.state
    untaught = [c.value for c in TeachingConcept if c not in set(context.taught_concepts)]
    payload = {
        "month": state.month,
        "player": state.model_dump(
            mode="json", include={"salary_net", "cash", "savings", "emergency_fund", "wellbeing"}
        ),
        "creditCard": state.credit_card.model_dump(mode="json"),
        "installments": [item.model_dump(mode="json") for item in state.installments],
        "informalLoans": [loan.model_dump(mode="json") for loan in state.informal_loans],
        "recentHistory": list(state.history[-6:]),
        "signals": state_signals(state),
        "avoid": list(context.recent_event_ids),
        "notTaughtYet": untaught,
    }
    return "Create the event for this month.\n" + json.dumps(
        payload, ensure_ascii=False, separators=(",", ":")
    )

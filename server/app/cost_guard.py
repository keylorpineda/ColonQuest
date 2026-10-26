"""Cost control: every model call is priced, recorded in model_calls and counted against the
daily budget (DAILY_BUDGET_USD).

Prices are USD per million tokens from the Meta Model API price list in CLAUDE.md.
"""

import uuid
from dataclasses import dataclass
from datetime import UTC, datetime, time
from decimal import Decimal
from enum import StrEnum

from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from app.db import ModelCall
from app.providers.base import Usage

MILLION = Decimal(1_000_000)
USD_PER_AUDIO_HOUR = Decimal("0.18")
USD_PER_IMAGE = Decimal("0.01")


@dataclass(frozen=True, slots=True)
class TokenPrice:
    input: Decimal
    cached_input: Decimal
    output: Decimal


TOKEN_PRICES: dict[str, TokenPrice] = {
    "muse-wonder-1.0": TokenPrice(Decimal("4"), Decimal("0.20"), Decimal("20")),
    "muse-spark-1.3": TokenPrice(Decimal("1.25"), Decimal("0.125"), Decimal("4.25")),
    "muse-spark-1.3-contributor": TokenPrice(Decimal("1.25"), Decimal("0.125"), Decimal("4.25")),
}
# Unknown text models are priced like the most expensive one so the guard errs on the safe side.
FALLBACK_PRICE = TOKEN_PRICES["muse-wonder-1.0"]


class BudgetMode(StrEnum):
    NORMAL = "normal"
    DOWNGRADED = "downgraded"  # >= 80 %: game master switches to Muse Spark
    FALLBACK_ONLY = "fallback_only"  # >= 100 %: no model calls, fallback events only


DOWNGRADE_AT = Decimal("0.8")


@dataclass(frozen=True, slots=True)
class BudgetStatus:
    spent_usd: Decimal
    budget_usd: Decimal
    mode: BudgetMode

    @property
    def remaining_usd(self) -> Decimal:
        return max(Decimal(0), self.budget_usd - self.spent_usd)


def estimate_cost(usage: Usage) -> Decimal:
    """Estimated USD cost of one call, rounded to 6 decimals like the model_calls column."""
    if usage.images:
        cost = USD_PER_IMAGE * usage.images
    elif usage.audio_seconds:
        cost = USD_PER_AUDIO_HOUR * Decimal(str(usage.audio_seconds)) / Decimal(3600)
    else:
        price = TOKEN_PRICES.get(usage.model, FALLBACK_PRICE)
        cached = min(usage.cached_input_tokens, usage.input_tokens)
        fresh = usage.input_tokens - cached
        cost = (
            fresh * price.input + cached * price.cached_input + usage.output_tokens * price.output
        ) / MILLION
    return cost.quantize(Decimal("0.000001"))


def budget_mode(spent: Decimal, budget: Decimal) -> BudgetMode:
    if budget <= 0 or spent >= budget:
        return BudgetMode.FALLBACK_ONLY
    if spent >= budget * DOWNGRADE_AT:
        return BudgetMode.DOWNGRADED
    return BudgetMode.NORMAL


def start_of_day(now: datetime | None = None) -> datetime:
    now = now or datetime.now(UTC)
    return datetime.combine(now.date(), time.min, tzinfo=UTC)


async def record_call(
    db: AsyncSession,
    *,
    usage: Usage,
    endpoint: str,
    latency_ms: int,
    ok: bool,
    session_id: uuid.UUID | None = None,
    provider: str = "meta_api",
) -> ModelCall:
    """Persist one model call with its estimated cost. Failed calls are recorded at zero cost."""
    call = ModelCall(
        session_id=session_id,
        provider=provider,
        model=usage.model,
        endpoint=endpoint,
        input_tokens=usage.input_tokens,
        cached_input_tokens=usage.cached_input_tokens,
        output_tokens=usage.output_tokens,
        estimated_cost_usd=estimate_cost(usage) if ok else Decimal(0),
        latency_ms=latency_ms,
        ok=ok,
    )
    db.add(call)
    await db.flush()
    return call


async def spent_today(db: AsyncSession, now: datetime | None = None) -> Decimal:
    total = await db.scalar(
        select(func.coalesce(func.sum(ModelCall.estimated_cost_usd), 0)).where(
            ModelCall.created_at >= start_of_day(now)
        )
    )
    return Decimal(total or 0)


async def budget_status(db: AsyncSession, budget_usd: float) -> BudgetStatus:
    spent = await spent_today(db)
    budget = Decimal(str(budget_usd))
    return BudgetStatus(spent_usd=spent, budget_usd=budget, mode=budget_mode(spent, budget))

"""Cost control: every model call is priced, recorded in model_calls and counted against the
daily budget of its provider: DAILY_BUDGET_USD for the Meta Model API and
OPEN_WEIGHTS_DAILY_BUDGET_USD for the open-weights NPCs on Baseten.

Prices are USD per million tokens from the Meta Model API price list in CLAUDE.md.
"""

import hashlib
import logging
import time as clock
import uuid
from collections.abc import Callable
from contextlib import AbstractAsyncContextManager
from dataclasses import dataclass
from datetime import UTC, datetime, time
from decimal import Decimal
from enum import StrEnum

from sqlalchemy import func, select
from sqlalchemy.exc import SQLAlchemyError
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import Settings
from app.db import GameSession, ModelCall
from app.providers.base import Usage

logger = logging.getLogger(__name__)

MILLION = Decimal(1_000_000)
META_PROVIDER = "meta_api"
OPEN_WEIGHTS_PROVIDER = "open_weights"
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
# Muse Spark Open on Baseten, paid with partner credits: same list price as Muse Spark.
OPEN_WEIGHTS_PRICE = TokenPrice(Decimal("1.25"), Decimal("0.15"), Decimal("4.25"))


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
    """Estimated USD cost of one call, rounded to 6 decimals like the model_calls column.

    Open-weights calls are priced too, but each provider is summed against its own budget.
    """
    if usage.provider != META_PROVIDER:
        cost = token_cost(usage, OPEN_WEIGHTS_PRICE)
    elif usage.images:
        cost = USD_PER_IMAGE * usage.images
    elif usage.audio_seconds:
        cost = USD_PER_AUDIO_HOUR * Decimal(str(usage.audio_seconds)) / Decimal(3600)
    else:
        cost = token_cost(usage, TOKEN_PRICES.get(usage.model, FALLBACK_PRICE))
    return cost.quantize(Decimal("0.000001"))


def token_cost(usage: Usage, price: TokenPrice) -> Decimal:
    cached = min(usage.cached_input_tokens, usage.input_tokens)
    fresh = usage.input_tokens - cached
    return (
        fresh * price.input + cached * price.cached_input + usage.output_tokens * price.output
    ) / MILLION


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
    provider: str | None = None,
) -> ModelCall:
    """Persist one model call with its estimated cost. Failed calls are recorded at zero cost."""
    call = ModelCall(
        session_id=session_id,
        provider=provider or usage.provider,
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


async def spent_today(
    db: AsyncSession, now: datetime | None = None, provider: str = META_PROVIDER
) -> Decimal:
    total = await db.scalar(
        select(func.coalesce(func.sum(ModelCall.estimated_cost_usd), 0)).where(
            ModelCall.created_at >= start_of_day(now), ModelCall.provider == provider
        )
    )
    return Decimal(total or 0)


async def budget_status(
    db: AsyncSession, budget_usd: float, provider: str = META_PROVIDER
) -> BudgetStatus:
    spent = await spent_today(db, provider=provider)
    budget = Decimal(str(budget_usd))
    return BudgetStatus(spent_usd=spent, budget_usd=budget, mode=budget_mode(spent, budget))


SessionFactory = Callable[[], AbstractAsyncContextManager[AsyncSession]]


class OpenWeightsBudget:
    """Whether today's open-weights spend reached OPEN_WEIGHTS_DAILY_BUDGET_USD.

    Checked before every NPC call, so the answer is cached for ``ttl_s`` instead of querying the
    database on each turn. A failing query never blocks the NPCs: it counts as not exhausted.
    """

    def __init__(
        self, session_factory: SessionFactory, budget_usd: float, ttl_s: float = 60
    ) -> None:
        self._session_factory = session_factory
        self._budget_usd = budget_usd
        self._ttl_s = ttl_s
        self._checked_at: float | None = None
        self._exhausted = False

    async def exhausted(self) -> bool:
        now = clock.monotonic()
        if self._checked_at is not None and now - self._checked_at < self._ttl_s:
            return self._exhausted
        try:
            async with self._session_factory() as db:
                status = await budget_status(db, self._budget_usd, OPEN_WEIGHTS_PROVIDER)
        except (SQLAlchemyError, OSError) as error:
            logger.warning("could not read the open-weights spend (%s)", error)
            return False
        self._checked_at = now
        self._exhausted = status.mode == BudgetMode.FALLBACK_ONLY
        if self._exhausted:
            logger.warning("open-weights budget spent (%s USD); NPCs use Meta", status.spent_usd)
        return self._exhausted


def select_text_model(configured: str, mode: BudgetMode, settings: Settings) -> str | None:
    """Model to use for a text call, or None when only fallbacks are allowed.

    Downgraded mode swaps Muse Wonder for the cheaper Spark model; Spark stays as is.
    """
    if mode == BudgetMode.FALLBACK_ONLY:
        return None
    if mode == BudgetMode.DOWNGRADED and configured.startswith("muse-wonder"):
        return settings.downgrade_model
    return configured


def client_hash(client_ip: str, salt: str) -> str:
    """Pseudonymous client id for per-IP limits; raw IPs are never stored."""
    return hashlib.sha256(f"{salt}:{client_ip}".encode()).hexdigest()


class LimitExceeded(StrEnum):
    SESSION = "session"
    CLIENT = "client"


async def calls_for_session(db: AsyncSession, session_id: uuid.UUID) -> int:
    count = await db.scalar(
        select(func.count()).select_from(ModelCall).where(ModelCall.session_id == session_id)
    )
    return int(count or 0)


async def calls_for_client_today(db: AsyncSession, hashed_client: str) -> int:
    count = await db.scalar(
        select(func.count())
        .select_from(ModelCall)
        .join(GameSession, GameSession.id == ModelCall.session_id)
        .where(GameSession.client_hash == hashed_client, ModelCall.created_at >= start_of_day())
    )
    return int(count or 0)


async def check_limits(
    db: AsyncSession, settings: Settings, session_id: uuid.UUID, hashed_client: str | None
) -> LimitExceeded | None:
    """Return which limit is exceeded, if any. Callers serve a fallback instead of the model."""
    if await calls_for_session(db, session_id) >= settings.max_calls_per_session:
        return LimitExceeded.SESSION
    if hashed_client is not None:
        client_calls = await calls_for_client_today(db, hashed_client)
        if client_calls >= settings.max_calls_per_client_per_day:
            return LimitExceeded.CLIENT
    return None

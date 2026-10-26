from datetime import UTC, datetime, timedelta
from decimal import Decimal

import pytest
from sqlalchemy.ext.asyncio import AsyncSession

from app.cost_guard import (
    BudgetMode,
    budget_mode,
    budget_status,
    estimate_cost,
    record_call,
    spent_today,
    start_of_day,
)
from app.db import ModelCall
from app.providers.base import Usage


def test_wonder_token_pricing_with_cache() -> None:
    usage = Usage(
        model="muse-wonder-1.0", input_tokens=3000, cached_input_tokens=2000, output_tokens=500
    )
    # 1000 fresh * 4 + 2000 cached * 0.20 + 500 * 20, per million
    assert estimate_cost(usage) == Decimal("0.014400")


def test_spark_is_cheaper_than_wonder() -> None:
    tokens = {"input_tokens": 2000, "output_tokens": 400}
    spark = estimate_cost(Usage(model="muse-spark-1.3", **tokens))
    wonder = estimate_cost(Usage(model="muse-wonder-1.0", **tokens))
    assert spark < wonder


def test_unknown_model_uses_most_expensive_price() -> None:
    tokens = {"input_tokens": 1000, "output_tokens": 1000}
    assert estimate_cost(Usage(model="mystery", **tokens)) == estimate_cost(
        Usage(model="muse-wonder-1.0", **tokens)
    )


def test_audio_and_image_pricing() -> None:
    assert estimate_cost(Usage(model="muse-voice-transcribe-1.0", audio_seconds=3600)) == Decimal(
        "0.180000"
    )
    assert estimate_cost(Usage(model="muse-image-1.0", images=3)) == Decimal("0.030000")


@pytest.mark.parametrize(
    ("spent", "mode"),
    [
        ("0", BudgetMode.NORMAL),
        ("3.99", BudgetMode.NORMAL),
        ("4.00", BudgetMode.DOWNGRADED),
        ("4.99", BudgetMode.DOWNGRADED),
        ("5.00", BudgetMode.FALLBACK_ONLY),
        ("7", BudgetMode.FALLBACK_ONLY),
    ],
)
def test_budget_thresholds(spent: str, mode: BudgetMode) -> None:
    assert budget_mode(Decimal(spent), Decimal(5)) == mode


def test_zero_budget_means_fallback_only() -> None:
    assert budget_mode(Decimal(0), Decimal(0)) == BudgetMode.FALLBACK_ONLY


def test_start_of_day_is_utc_midnight() -> None:
    now = datetime(2026, 10, 30, 15, 45, tzinfo=UTC)
    assert start_of_day(now) == datetime(2026, 10, 30, tzinfo=UTC)


@pytest.mark.anyio
async def test_record_and_sum_daily_spend(db: AsyncSession) -> None:
    usage = Usage(model="muse-wonder-1.0", input_tokens=1_000_000, output_tokens=0)
    await record_call(db, usage=usage, endpoint="event", latency_ms=900, ok=True)
    await record_call(db, usage=usage, endpoint="event", latency_ms=8000, ok=False)
    yesterday = ModelCall(
        model="muse-wonder-1.0",
        endpoint="event",
        estimated_cost_usd=Decimal(100),
        latency_ms=1,
        ok=True,
        created_at=datetime.now(UTC) - timedelta(days=1),
    )
    db.add(yesterday)
    await db.flush()

    assert await spent_today(db) == Decimal(4)
    status = await budget_status(db, budget_usd=4.5)
    assert status.mode == BudgetMode.DOWNGRADED
    assert status.remaining_usd == Decimal("0.5")

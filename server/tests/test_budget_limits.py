import uuid

import pytest
from pydantic import SecretStr
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import Settings
from app.cost_guard import (
    BudgetMode,
    LimitExceeded,
    check_limits,
    client_hash,
    record_call,
    select_text_model,
)
from app.db import GameSession
from app.providers.base import Usage
from app.response_cache import TTLCache, cache_key

SETTINGS = Settings(
    _env_file=None,  # type: ignore[call-arg]
    max_calls_per_session=2,
    max_calls_per_client_per_day=3,
    client_hash_salt=SecretStr("pepper"),
)


@pytest.mark.parametrize(
    ("configured", "mode", "expected"),
    [
        ("muse-wonder-1.0", BudgetMode.NORMAL, "muse-wonder-1.0"),
        ("muse-wonder-1.0", BudgetMode.DOWNGRADED, "muse-spark-1.3"),
        ("muse-spark-1.3-contributor", BudgetMode.DOWNGRADED, "muse-spark-1.3-contributor"),
        ("muse-wonder-1.0", BudgetMode.FALLBACK_ONLY, None),
    ],
)
def test_select_text_model(configured: str, mode: BudgetMode, expected: str | None) -> None:
    assert select_text_model(configured, mode, SETTINGS) == expected


def test_client_hash_is_stable_salted_and_not_the_ip() -> None:
    first = client_hash("203.0.113.7", "pepper")
    assert first == client_hash("203.0.113.7", "pepper")
    assert first != client_hash("203.0.113.7", "salt")
    assert "203.0.113.7" not in first


def test_cache_key_is_order_independent_for_dicts() -> None:
    assert cache_key("m", {"a": 1, "b": 2}) == cache_key("m", {"b": 2, "a": 1})
    assert cache_key("m", "x") != cache_key("m", "y")


def test_ttl_cache_expiry_and_lru() -> None:
    now = [0.0]
    cache: TTLCache[str] = TTLCache(max_entries=2, ttl_s=10, clock=lambda: now[0])
    cache.set("a", "A")
    cache.set("b", "B")
    assert cache.get("a") == "A"
    cache.set("c", "C")  # evicts "b", the least recently used
    assert cache.get("b") is None
    now[0] = 11
    assert cache.get("a") is None
    assert (cache.hits, cache.misses) == (1, 2)


def test_ttl_zero_disables_cache() -> None:
    cache: TTLCache[str] = TTLCache(max_entries=5, ttl_s=0)
    cache.set("a", "A")
    assert cache.get("a") is None
    assert len(cache) == 0


@pytest.mark.anyio
async def test_session_and_client_limits(db: AsyncSession) -> None:
    hashed = client_hash("198.51.100.1", "pepper")
    first, second = GameSession(client_hash=hashed), GameSession(client_hash=hashed)
    db.add_all([first, second])
    await db.flush()
    usage = Usage(model="muse-spark-1.3", input_tokens=10, output_tokens=10)

    assert await check_limits(db, SETTINGS, first.id, hashed) is None
    for _ in range(2):
        await record_call(
            db, usage=usage, endpoint="event", latency_ms=1, ok=True, session_id=first.id
        )
    assert await check_limits(db, SETTINGS, first.id, hashed) == LimitExceeded.SESSION

    await record_call(
        db, usage=usage, endpoint="event", latency_ms=1, ok=True, session_id=second.id
    )
    assert await check_limits(db, SETTINGS, second.id, hashed) == LimitExceeded.CLIENT
    assert await check_limits(db, SETTINGS, second.id, None) is None
    assert await check_limits(db, SETTINGS, uuid.uuid4(), "other-client") is None

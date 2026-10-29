import copy
import random
import uuid
from typing import Any

import pytest

from app.config import Settings
from app.cost_guard import BudgetMode, LimitExceeded
from app.game_master.prompt import SYSTEM_PROMPT, PromptContext
from app.game_master.service import EventOutcome, EventSource, FallbackReason, GameMaster
from app.providers.base import (
    ProviderResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
)
from app.response_cache import TTLCache
from app.schemas import PlayerState

from .fakes import MODEL_EVENT, FakeLedger, FakeProvider, fail, ok

pytestmark = pytest.mark.anyio

SESSION_ID = uuid.uuid4()
STATE = PlayerState.model_validate(
    {
        "month": 3,
        "salaryNet": 450000,
        "cash": 120000,
        "savings": 50000,
        "emergencyFund": 0,
        "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
        "wellbeing": 60,
    }
)


def make_gm(provider: FakeProvider, **settings: Any) -> GameMaster:
    config = Settings(_env_file=None, game_master_model="muse-wonder-1.0", **settings)  # type: ignore[call-arg]
    return GameMaster(provider, config, TTLCache(max_entries=10, ttl_s=60), rng=random.Random(0))


async def next_event(
    gm: GameMaster, ledger: FakeLedger, recent: tuple[str, ...] = ()
) -> EventOutcome:
    context = PromptContext(state=STATE, recent_event_ids=recent)
    return await gm.next_event(ledger, SESSION_ID, context)


async def test_model_event_is_returned_and_recorded() -> None:
    provider, ledger = FakeProvider(ok()), FakeLedger()
    outcome = await next_event(make_gm(provider), ledger)

    assert outcome.source == EventSource.MODEL
    assert outcome.event.event_id == "evt_llanta_ponchada"
    assert provider.calls[0]["system"] is SYSTEM_PROMPT
    assert provider.calls[0]["reasoning_effort"] == "minimal"  # the player waits on this call
    assert [r["ok"] for r in ledger.records] == [True]


async def test_out_of_range_amounts_are_clamped() -> None:
    greedy = copy.deepcopy(MODEL_EVENT)
    greedy["choices"][0]["effects"][0]["amount"] = 2_000_000
    outcome = await next_event(make_gm(FakeProvider(ok(greedy))), FakeLedger())

    assert outcome.event.choices[0].effects[0].amount == 250_000
    assert outcome.adjustments


async def test_invalid_response_is_retried_once() -> None:
    provider, ledger = FakeProvider(fail(ProviderResponseError("bad json")), ok()), FakeLedger()
    outcome = await next_event(make_gm(provider), ledger)

    assert outcome.source == EventSource.MODEL
    assert [r["ok"] for r in ledger.records] == [False, True]


async def test_two_invalid_responses_fall_back() -> None:
    provider = FakeProvider(fail(ProviderResponseError("a")), fail(ProviderResponseError("b")))
    outcome = await next_event(make_gm(provider), FakeLedger())

    assert outcome.source == EventSource.FALLBACK
    assert outcome.fallback_reason == FallbackReason.INVALID_RESPONSE
    assert len(provider.calls) == 2


@pytest.mark.parametrize(
    ("error", "reason"),
    [
        (ProviderUnavailableError("503"), FallbackReason.PROVIDER_ERROR),
        (ProviderTimeoutError("slow"), FallbackReason.TIMEOUT),
    ],
)
async def test_provider_errors_fall_back_without_retry(
    error: Exception, reason: FallbackReason
) -> None:
    provider, ledger = FakeProvider(fail(error)), FakeLedger()
    outcome = await next_event(make_gm(provider), ledger)

    assert outcome.fallback_reason == reason
    assert len(provider.calls) == 1
    assert [r["ok"] for r in ledger.records] == [False]


async def test_slow_model_hits_the_overall_timeout() -> None:
    provider, ledger = FakeProvider(ok(), delay_s=0.5), FakeLedger()
    outcome = await next_event(make_gm(provider, model_timeout_s=0.05), ledger)

    assert outcome.fallback_reason == FallbackReason.TIMEOUT
    assert [r["ok"] for r in ledger.records] == [False]


async def test_budget_exhausted_skips_the_model() -> None:
    provider = FakeProvider()
    outcome = await next_event(make_gm(provider), FakeLedger(mode=BudgetMode.FALLBACK_ONLY))

    assert outcome.fallback_reason == FallbackReason.BUDGET
    assert provider.calls == []


async def test_downgraded_budget_uses_spark() -> None:
    provider = FakeProvider(ok())
    outcome = await next_event(make_gm(provider), FakeLedger(mode=BudgetMode.DOWNGRADED))

    assert outcome.model == "muse-spark-1.3"
    assert provider.calls[0]["model"] == "muse-spark-1.3"


@pytest.mark.parametrize(
    ("limit", "reason"),
    [
        (LimitExceeded.SESSION, FallbackReason.SESSION_LIMIT),
        (LimitExceeded.CLIENT, FallbackReason.CLIENT_LIMIT),
    ],
)
async def test_limits_skip_the_model(limit: LimitExceeded, reason: FallbackReason) -> None:
    provider = FakeProvider()
    outcome = await next_event(make_gm(provider), FakeLedger(limit=limit))

    assert outcome.fallback_reason == reason
    assert provider.calls == []


async def test_identical_request_is_served_from_cache() -> None:
    provider = FakeProvider(ok())
    gm = make_gm(provider)
    first = await next_event(gm, FakeLedger())
    second = await next_event(gm, FakeLedger())

    assert (first.source, second.source) == (EventSource.MODEL, EventSource.CACHE)
    assert second.event == first.event
    assert len(provider.calls) == 1


async def test_fallback_avoids_recent_events() -> None:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    outcome = await next_event(make_gm(provider), FakeLedger(), recent=("evt_moto_dañada",))

    assert outcome.event.event_id != "evt_moto_dañada"

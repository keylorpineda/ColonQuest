import copy
import json
import random
import uuid
from typing import Any

import pytest

from app.agents.tools import HistoryItem
from app.config import Settings
from app.cost_guard import BudgetMode, LimitExceeded
from app.game_master.agentic import AgenticGameMaster
from app.game_master.prompt import PromptContext
from app.game_master.service import EventSource, FallbackReason, GameMaster
from app.providers.base import ProviderResponseError
from app.response_cache import TTLCache
from app.schemas import PlayerState, TeachingConcept

from .fakes import (
    MODEL_EVENT,
    FakeLedger,
    FakeProvider,
    FakeToolProvider,
    fail,
    final_turn,
    ok,
    tool_turn,
)

pytestmark = pytest.mark.anyio

SESSION_ID = uuid.uuid4()
STATE = PlayerState.model_validate(
    {
        "month": 4,
        "salaryNet": 450000,
        "cash": 40000,
        "savings": 0,
        "emergencyFund": 0,
        "creditCard": {
            "balance": 250000,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "wellbeing": 50,
    }
)
CONTEXT = PromptContext(
    state=STATE,
    recent_event_ids=["evt_moto_dañada"],
    taught_concepts=[TeachingConcept.EMERGENCY_FUND],
    history=[HistoryItem(3, "evt_moto_dañada", "emergency_fund", "pay_card")],
)


def make(
    tool_provider: FakeToolProvider, base_provider: FakeProvider | None = None, **settings: Any
) -> AgenticGameMaster:
    values: dict[str, Any] = {"game_master_model": "muse-wonder-1.0", "agentic_game_master": True}
    config = Settings(_env_file=None, **(values | settings))  # type: ignore[call-arg]
    base = GameMaster(
        base_provider or FakeProvider(), config, TTLCache(10, 60), rng=random.Random(0)
    )
    return AgenticGameMaster(base, tool_provider, config)


async def test_agent_reads_history_then_writes_the_event() -> None:
    tool_provider = FakeToolProvider(
        tool_turn(
            ("get_player_history", {}),
            (
                "project_debt",
                {"balance": 250000, "monthly_rate_pct": 3.5, "monthly_payment": 25000, "months": 6},
            ),
        ),
        final_turn(json.dumps(MODEL_EVENT, ensure_ascii=False)),
    )
    ledger = FakeLedger()

    outcome = await make(tool_provider).next_event(ledger, SESSION_ID, CONTEXT)

    assert outcome.source == EventSource.AGENT
    assert outcome.event.event_id == "evt_llanta_ponchada"
    tool_message = tool_provider.calls[1]["messages"][3]
    assert "evt_moto_dañada" in tool_message["content"]
    assert {r["endpoint"] for r in ledger.records} == {"event_agent"}


async def test_agent_event_is_clamped() -> None:
    greedy = copy.deepcopy(MODEL_EVENT)
    greedy["choices"][0]["effects"][0]["amount"] = 5_000_000
    outcome = await make(FakeToolProvider(final_turn(json.dumps(greedy)))).next_event(
        FakeLedger(), SESSION_ID, CONTEXT
    )
    assert outcome.event.choices[0].effects[0].amount == 250_000
    assert outcome.adjustments


async def test_flag_off_uses_the_base_game_master() -> None:
    tool_provider, base_provider = FakeToolProvider(), FakeProvider(ok())
    outcome = await make(tool_provider, base_provider, agentic_game_master=False).next_event(
        FakeLedger(), SESSION_ID, CONTEXT
    )
    assert outcome.source == EventSource.MODEL
    assert tool_provider.calls == []


async def test_agent_failure_falls_back_to_single_call() -> None:
    tool_provider = FakeToolProvider(fail(ProviderResponseError("bad")))
    outcome = await make(tool_provider, FakeProvider(ok())).next_event(
        FakeLedger(), SESSION_ID, CONTEXT
    )
    assert outcome.source == EventSource.MODEL


async def test_agent_timeout_serves_a_fallback_without_more_calls() -> None:
    tool_provider = FakeToolProvider(final_turn("{}"), delay_s=0.5)
    base_provider = FakeProvider()
    outcome = await make(tool_provider, base_provider, agent_timeout_s=0.05).next_event(
        FakeLedger(), SESSION_ID, CONTEXT
    )
    assert outcome.source == EventSource.FALLBACK
    assert outcome.fallback_reason == FallbackReason.TIMEOUT
    assert base_provider.calls == []
    assert outcome.event.event_id != "evt_moto_dañada"


@pytest.mark.parametrize(
    "ledger",
    [FakeLedger(mode=BudgetMode.FALLBACK_ONLY), FakeLedger(limit=LimitExceeded.SESSION)],
)
async def test_budget_and_limits_skip_the_agent(ledger: FakeLedger) -> None:
    tool_provider = FakeToolProvider()
    outcome = await make(tool_provider).next_event(ledger, SESSION_ID, CONTEXT)
    assert tool_provider.calls == []
    assert outcome.source == EventSource.FALLBACK


async def test_downgraded_budget_runs_the_agent_on_spark() -> None:
    tool_provider = FakeToolProvider(final_turn(json.dumps(MODEL_EVENT)))
    await make(tool_provider).next_event(
        FakeLedger(mode=BudgetMode.DOWNGRADED), SESSION_ID, CONTEXT
    )
    assert tool_provider.calls[0]["model"] == "muse-spark-1.3"

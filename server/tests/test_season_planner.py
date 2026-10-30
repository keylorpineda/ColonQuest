import uuid
from typing import Any

import pytest

from app.config import Settings
from app.cost_guard import BudgetMode, LimitExceeded
from app.game_master.planner import SeasonPlanner
from app.game_master.season import default_plan
from app.providers.base import ProviderResponseError, StructuredResult, Usage
from app.schemas import PlayerState, TeachingConcept

from .fakes import FakeLedger, FakeProvider, fail

pytestmark = pytest.mark.anyio

STATE = PlayerState.model_validate(
    {
        "month": 1,
        "salaryNet": 450000,
        "cash": 120000,
        "savings": 0,
        "emergencyFund": 0,
        "creditCard": {"balance": 0, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10},
        "wellbeing": 60,
    }
)
SETTINGS = Settings(_env_file=None, season_planner=True)  # type: ignore[call-arg]
SESSION = uuid.uuid4()


def model_plan() -> Any:
    plan = default_plan(STATE)
    months = list(plan.months)
    months[1], months[2] = months[2], months[1]
    swapped = plan.model_copy(update={"months": months})

    def step() -> StructuredResult[Any]:
        usage = Usage(model="muse-spark-1.3-contributor", input_tokens=500, output_tokens=600)
        return StructuredResult(value=swapped, usage=usage, latency_ms=900)

    return step


async def test_model_plan_is_used_and_recorded() -> None:
    provider = FakeProvider(model_plan())
    ledger = FakeLedger()

    planned = await SeasonPlanner(provider, SETTINGS).plan(ledger, SESSION, STATE)

    assert planned.source == "model"
    assert planned.plan.months[1] == default_plan(STATE).months[2]
    assert provider.calls[0]["model"] == "muse-wonder-1.0"
    assert provider.calls[0]["timeout_s"] == SETTINGS.planner_timeout_s == 30.0
    assert '"cardUtilizationPct":0' in provider.calls[0]["user"]
    assert ledger.records[0]["endpoint"] == "season_plan"
    assert ledger.records[0]["ok"] is True


async def test_invalid_answer_falls_back_to_the_default_plan() -> None:
    provider = FakeProvider(fail(ProviderResponseError("bad json")))
    ledger = FakeLedger()

    planned = await SeasonPlanner(provider, SETTINGS).plan(ledger, SESSION, STATE)

    assert planned.source == "default"
    assert planned.plan == default_plan(STATE)
    assert planned.plan.for_month(1).teaching_concept == TeachingConcept.BUDGET
    assert ledger.records[0]["ok"] is False


@pytest.mark.parametrize(
    "ledger",
    [
        FakeLedger(mode=BudgetMode.FALLBACK_ONLY),
        FakeLedger(limit=LimitExceeded.SESSION),
    ],
)
async def test_budget_or_limits_skip_the_model(ledger: FakeLedger) -> None:
    provider = FakeProvider()

    planned = await SeasonPlanner(provider, SETTINGS).plan(ledger, SESSION, STATE)

    assert planned.source == "default"
    assert provider.calls == []
    assert ledger.records == []

import uuid
from typing import Any

import pytest

from app.config import Settings
from app.cost_guard import BudgetMode
from app.negotiation.questions import quoted_amounts
from app.providers.base import ProviderUnavailableError, StructuredResult, Usage
from app.schemas import KeyQuestion
from app.summary.stats import SessionStats
from app.summary.writer import SummaryText, SummaryWriter, fallback_narrative, stats_payload

from .fakes import FakeLedger, FakeProvider, Step, fail

pytestmark = pytest.mark.anyio

STATS = SessionStats(
    months_played=12,
    decisions=12,
    concepts_taught=[],
    negotiations=2,
    deals_accepted=0,
    key_questions_asked=[KeyQuestion.TOTAL_COST],
    total_debt=386000,
    net_worth=-136000,
    wellbeing=70,
)


def text(narrative: str) -> Step:
    def step() -> StructuredResult[Any]:
        value = SummaryText(narrative=narrative)
        return StructuredResult(value=value, usage=Usage(model="muse-spark-1.3"), latency_ms=5)

    return step


def make_writer(provider: FakeProvider) -> SummaryWriter:
    return SummaryWriter(provider, Settings(_env_file=None))  # type: ignore[call-arg]


async def test_model_narrative_with_true_amounts() -> None:
    provider = FakeProvider(text("Terminaste debiendo ₡386 000, pero preguntaste el total."))
    written = await make_writer(provider).write(FakeLedger(), uuid.uuid4(), STATS)

    assert written.source == "model"
    assert provider.calls[0]["model"] == "muse-spark-1.3-contributor"
    assert '"totalDebt": "₡386 000"' in provider.calls[0]["user"]


async def test_invented_amount_uses_fallback() -> None:
    provider = FakeProvider(text("Ahorraste ₡1 000 000, ¡tuanis!"))
    written = await make_writer(provider).write(FakeLedger(), uuid.uuid4(), STATS)

    assert written.source == "fallback"


@pytest.mark.parametrize("ledger", [FakeLedger(mode=BudgetMode.FALLBACK_ONLY), FakeLedger()])
async def test_budget_or_provider_failure_uses_fallback(ledger: FakeLedger) -> None:
    provider = FakeProvider(fail(ProviderUnavailableError("down")))
    written = await make_writer(provider).write(ledger, uuid.uuid4(), STATS)

    assert written.source == "fallback"


def test_fallback_only_quotes_real_amounts() -> None:
    narrative = fallback_narrative(STATS)
    assert set(quoted_amounts(narrative)) <= STATS.amounts
    assert "1 de las preguntas clave" in narrative


def test_payload_formats_negative_net_worth() -> None:
    assert '"netWorth": "-₡136 000"' in stats_payload(STATS)

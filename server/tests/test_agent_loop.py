import json
import uuid

import pytest
from pydantic import BaseModel

from app.agents.loop import AgentRequest, run_tool_loop
from app.agents.tools import ToolContext
from app.providers.base import ProviderResponseError, ProviderUnavailableError

from .fakes import FakeLedger, FakeToolProvider, fail, final_turn, tool_turn

pytestmark = pytest.mark.anyio


class Answer(BaseModel):
    text: str


def request(**overrides: object) -> AgentRequest[Answer]:
    values: dict[str, object] = {
        "session_id": uuid.uuid4(),
        "endpoint": "event_agent",
        "model": "muse-wonder-1.0",
        "system": "sys",
        "user": "usr",
        "output": Answer,
        "tools": ["project_debt", "get_concepts_not_yet_taught"],
        "context": ToolContext(),
    }
    return AgentRequest(**(values | overrides))  # type: ignore[arg-type]


async def test_tool_results_are_fed_back_until_the_final_answer() -> None:
    provider = FakeToolProvider(
        tool_turn(
            (
                "project_debt",
                {"balance": 100000, "monthly_rate_pct": 2, "monthly_payment": 60000, "months": 3},
            )
        ),
        final_turn('{"text": "listo"}'),
    )
    ledger = FakeLedger()

    result = await run_tool_loop(provider, ledger, request())

    assert result.value == Answer(text="listo")
    assert result.steps == 2
    assert result.tools_used == ["project_debt"]
    assert result.tool_results[0]["monthsToPayOff"] == 2
    second_messages = provider.calls[1]["messages"]
    assert second_messages[2]["role"] == "assistant"
    assert second_messages[3]["role"] == "tool"
    assert json.loads(second_messages[3]["content"])["finalBalance"] == 0
    assert [r["endpoint"] for r in ledger.records] == ["event_agent", "event_agent"]


async def test_disallowed_tool_gets_an_error_result() -> None:
    provider = FakeToolProvider(tool_turn(("simulate_purchase", {})), final_turn('{"text": "ok"}'))
    result = await run_tool_loop(provider, FakeLedger(), request())
    assert "unknown tool" in result.tool_results[0]["error"]


async def test_step_limit() -> None:
    turns = [tool_turn(("get_concepts_not_yet_taught", {})) for _ in range(5)]
    provider = FakeToolProvider(*turns)
    with pytest.raises(ProviderResponseError, match="5 steps"):
        await run_tool_loop(provider, FakeLedger(), request())
    assert len(provider.calls) == 5


async def test_timeout() -> None:
    provider = FakeToolProvider(final_turn('{"text": "tarde"}'), delay_s=0.5)
    with pytest.raises(TimeoutError):
        await run_tool_loop(provider, FakeLedger(), request(timeout_s=0.05))


@pytest.mark.parametrize("content", [None, "not json", '{"other": 1}'])
async def test_invalid_final_answer(content: str | None) -> None:
    provider = FakeToolProvider(final_turn(content))  # type: ignore[arg-type]
    with pytest.raises(ProviderResponseError):
        await run_tool_loop(provider, FakeLedger(), request())


async def test_provider_error_is_recorded_and_raised() -> None:
    provider = FakeToolProvider(fail(ProviderUnavailableError("down")))
    ledger = FakeLedger()
    with pytest.raises(ProviderUnavailableError):
        await run_tool_loop(provider, ledger, request())
    assert [r["ok"] for r in ledger.records] == [False]

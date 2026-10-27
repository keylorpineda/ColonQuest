"""Test doubles for the model provider and the ledger."""

import asyncio
import uuid
from collections.abc import Callable
from typing import Any

from pydantic import BaseModel

from app.cost_guard import BudgetMode, LimitExceeded
from app.providers.base import StructuredResult, Usage
from app.schemas import GameEvent

MODEL_EVENT: dict[str, Any] = {
    "eventId": "evt_llanta_ponchada",
    "type": "emergency",
    "title": "Llanta ponchada",
    "narrative": "Se te ponchó la llanta camino al cole.",
    "choices": [
        {
            "id": "fix",
            "label": "Arreglarla",
            "effects": [{"kind": "expense", "amount": 30000, "source": "cash"}],
        },
        {
            "id": "bus",
            "label": "Irme en bus",
            "effects": [{"kind": "wellbeing_change", "amount": -3}],
        },
    ],
    "teachingConcept": "emergency_fund",
}

Step = Callable[[], StructuredResult[Any]]


def ok(event: dict[str, Any] = MODEL_EVENT) -> Step:
    def step() -> StructuredResult[Any]:
        usage = Usage(model="muse-wonder-1.0", input_tokens=900, output_tokens=200)
        return StructuredResult(value=GameEvent.model_validate(event), usage=usage, latency_ms=700)

    return step


def fail(error: Exception) -> Step:
    def step() -> StructuredResult[Any]:
        raise error

    return step


class FakeProvider:
    def __init__(self, *steps: Step, delay_s: float = 0) -> None:
        self.steps = list(steps)
        self.calls: list[dict[str, Any]] = []
        self.delay_s = delay_s

    async def structured[T: BaseModel](self, **kwargs: Any) -> StructuredResult[T]:
        self.calls.append(kwargs)
        if self.delay_s:
            await asyncio.sleep(self.delay_s)
        return self.steps.pop(0)()


class FakeLedger:
    def __init__(
        self, mode: BudgetMode = BudgetMode.NORMAL, limit: LimitExceeded | None = None
    ) -> None:
        self.mode = mode
        self.limit = limit
        self.records: list[dict[str, Any]] = []

    async def budget_mode(self) -> BudgetMode:
        return self.mode

    async def limit_exceeded(self, session_id: uuid.UUID) -> LimitExceeded | None:
        return self.limit

    async def record(self, **kwargs: Any) -> None:
        self.records.append(kwargs)


class FakeToolProvider:
    """Scripted chat() turns for agent tests; each step returns a ChatTurn or raises."""

    def __init__(self, *turns: Callable[[], Any], delay_s: float = 0) -> None:
        self.turns = list(turns)
        self.calls: list[dict[str, Any]] = []
        self.delay_s = delay_s

    async def chat(self, **kwargs: Any) -> Any:
        self.calls.append({**kwargs, "messages": list(kwargs["messages"])})
        if self.delay_s:
            await asyncio.sleep(self.delay_s)
        return self.turns.pop(0)()


def tool_turn(*calls: tuple[str, dict[str, Any]]) -> Callable[[], Any]:
    import json as _json

    from app.providers.base import ChatTurn, ToolCall

    def step() -> Any:
        tool_calls = [
            ToolCall(id=f"call_{i}", name=name, arguments=_json.dumps(args))
            for i, (name, args) in enumerate(calls)
        ]
        message = {
            "role": "assistant",
            "tool_calls": [
                {
                    "id": c.id,
                    "type": "function",
                    "function": {"name": c.name, "arguments": c.arguments},
                }
                for c in tool_calls
            ],
        }
        return ChatTurn(
            None, tool_calls, Usage(model="muse-wonder-1.0", output_tokens=20), 300, message
        )

    return step


def final_turn(content: str) -> Callable[[], Any]:
    from app.providers.base import ChatTurn

    def step() -> Any:
        return ChatTurn(content, [], Usage(model="muse-wonder-1.0", output_tokens=200), 500, {})

    return step

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

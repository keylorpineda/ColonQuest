"""Bounded tool-calling loop shared by the agentic game master and the seller agent.

At most ``max_steps`` model calls inside ``timeout_s``; every call is recorded through the
ledger. Callers catch ProviderError and TimeoutError and fall back to their single-call version.
"""

import asyncio
import json
import time
import uuid
from collections.abc import Sequence
from dataclasses import dataclass, field
from typing import Any

from pydantic import BaseModel, ValidationError

from app.agents.tools import ToolContext, execute_tool, tool_schemas
from app.game_master.ledger import Ledger
from app.providers.base import (
    ProviderError,
    ProviderResponseError,
    ReasoningEffort,
    ToolCallingProvider,
    Usage,
)


@dataclass(frozen=True, slots=True)
class AgentResult[T: BaseModel]:
    value: T
    steps: int
    tools_used: list[str] = field(default_factory=list)
    tool_results: list[dict[str, Any]] = field(default_factory=list)


@dataclass(frozen=True, slots=True)
class AgentRequest[T: BaseModel]:
    session_id: uuid.UUID | None
    endpoint: str
    model: str
    system: str
    user: str
    output: type[T]
    tools: Sequence[str]
    context: ToolContext
    max_steps: int = 5
    timeout_s: float = 10.0
    reasoning_effort: ReasoningEffort | None = "minimal"
    max_tokens: int = 4096  # per step; reasoning tokens count against it


async def run_tool_loop[T: BaseModel](
    provider: ToolCallingProvider, ledger: Ledger, request: AgentRequest[T]
) -> AgentResult[T]:
    messages: list[dict[str, Any]] = [
        {"role": "system", "content": request.system},
        {"role": "user", "content": request.user},
    ]
    schemas = tool_schemas(request.tools)
    used: list[str] = []
    results: list[dict[str, Any]] = []

    async with asyncio.timeout(request.timeout_s):
        for step in range(1, request.max_steps + 1):
            started = time.perf_counter()
            try:
                turn = await provider.chat(
                    model=request.model,
                    messages=messages,
                    tools=schemas,
                    output=request.output,
                    reasoning_effort=request.reasoning_effort,
                    max_tokens=request.max_tokens,
                )
            except ProviderError:
                await ledger.record(
                    usage=Usage(model=request.model),
                    endpoint=request.endpoint,
                    latency_ms=int((time.perf_counter() - started) * 1000),
                    ok=False,
                    session_id=request.session_id,
                )
                raise
            await ledger.record(
                usage=turn.usage,
                endpoint=request.endpoint,
                latency_ms=turn.latency_ms,
                ok=True,
                session_id=request.session_id,
            )

            if turn.tool_calls:
                messages.append(turn.message or {"role": "assistant", "content": turn.content})
                for call in turn.tool_calls:
                    result = execute_tool(call, request.context, request.tools)
                    used.append(call.name)
                    results.append(result)
                    messages.append(
                        {
                            "role": "tool",
                            "tool_call_id": call.id,
                            "content": json.dumps(result, ensure_ascii=False),
                        }
                    )
                continue

            if not turn.content:
                raise ProviderResponseError("agent returned neither content nor tool calls")
            try:
                value = request.output.model_validate_json(turn.content)
            except ValidationError as error:
                raise ProviderResponseError(f"agent answer failed validation: {error}") from error
            return AgentResult(value=value, steps=step, tools_used=used, tool_results=results)

    raise ProviderResponseError(f"agent did not answer within {request.max_steps} steps")

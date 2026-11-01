"""What the agents' tools did in each session, kept in memory for the judges' demo.

It is evidence, not game state: nothing reads it to decide anything, and a restart loses it. Calls
to the models themselves are in the ``model_calls`` table; only the tool steps live here.
"""

import json
import uuid
from dataclasses import dataclass
from typing import Any

from app.response_cache import TTLCache

MAX_STEPS_PER_SESSION = 40
MAX_SESSIONS = 200
TTL_S = 3 * 60 * 60
MAX_TEXT = 400


@dataclass(frozen=True, slots=True)
class ToolStep:
    endpoint: str
    step: int
    tool: str
    arguments: str
    result: str


class AgentTraces:
    def __init__(self) -> None:
        self._steps: TTLCache[list[ToolStep]] = TTLCache(MAX_SESSIONS, TTL_S)

    def add(
        self,
        session_id: uuid.UUID | None,
        endpoint: str,
        step: int,
        tool: str,
        arguments: str,
        result: dict[str, Any],
    ) -> None:
        if session_id is None:
            return
        key = str(session_id)
        steps = self._steps.get(key) or []
        steps.append(
            ToolStep(
                endpoint=endpoint,
                step=step,
                tool=tool,
                arguments=arguments[:MAX_TEXT],
                result=json.dumps(result, ensure_ascii=False)[:MAX_TEXT],
            )
        )
        self._steps.set(key, steps[-MAX_STEPS_PER_SESSION:])

    def for_session(self, session_id: uuid.UUID) -> list[ToolStep]:
        return list(self._steps.get(str(session_id)) or [])


AGENT_TRACES = AgentTraces()

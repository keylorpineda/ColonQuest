"""Game master orchestration: budget -> limits -> cache -> model (one retry) -> clamp -> fallback.

The game never breaks because of the model: every failure path ends in a hand-written event.
"""

import asyncio
import logging
import random
import time
import uuid
from dataclasses import dataclass, field
from enum import StrEnum
from typing import Protocol

from app.config import Settings
from app.cost_guard import BudgetMode, select_text_model
from app.game_master.facts import assumes_a_vehicle
from app.game_master.fallback import pick_fallback_event
from app.game_master.ledger import Ledger
from app.game_master.limits import clamp_event
from app.game_master.prompt import PROMPT_VERSION, SYSTEM_PROMPT, PromptContext, build_user_prompt
from app.providers.base import (
    ModelProvider,
    ProviderError,
    ProviderResponseError,
    ProviderTimeoutError,
    Usage,
)
from app.response_cache import TTLCache, cache_key
from app.schemas import GameEvent

logger = logging.getLogger(__name__)

ENDPOINT = "event"
MAX_ATTEMPTS = 2  # first try plus one retry when the response is invalid


class EventSource(StrEnum):
    MODEL = "model"
    AGENT = "agent"
    CACHE = "cache"
    FALLBACK = "fallback"


class FallbackReason(StrEnum):
    BUDGET = "budget_exhausted"
    SESSION_LIMIT = "session_limit"
    CLIENT_LIMIT = "client_limit"
    TIMEOUT = "timeout"
    PROVIDER_ERROR = "provider_error"
    INVALID_RESPONSE = "invalid_response"


@dataclass(frozen=True, slots=True)
class EventOutcome:
    event: GameEvent
    source: EventSource
    model: str | None = None
    fallback_reason: FallbackReason | None = None
    adjustments: list[str] = field(default_factory=list)


class EventGenerator(Protocol):
    """Anything that produces the month's event: GameMaster or AgenticGameMaster."""

    async def next_event(
        self, ledger: Ledger, session_id: uuid.UUID, context: PromptContext
    ) -> EventOutcome: ...


class GameMaster:
    def __init__(
        self,
        provider: ModelProvider,
        settings: Settings,
        cache: TTLCache[GameEvent],
        rng: random.Random | None = None,
    ) -> None:
        self._provider = provider
        self._settings = settings
        self._cache = cache
        self._rng = rng

    async def next_event(
        self, ledger: Ledger, session_id: uuid.UUID, context: PromptContext
    ) -> EventOutcome:
        mode = await ledger.budget_mode()
        model = select_text_model(self._settings.game_master_model, mode, self._settings)
        if model is None or mode == BudgetMode.FALLBACK_ONLY:
            return self._fallback(context, FallbackReason.BUDGET)

        limit = await ledger.limit_exceeded(session_id)
        if limit is not None:
            reason = (
                FallbackReason.SESSION_LIMIT if limit == "session" else FallbackReason.CLIENT_LIMIT
            )
            return self._fallback(context, reason)

        user_prompt = build_user_prompt(context)
        key = cache_key(PROMPT_VERSION, model, user_prompt)
        cached = self._cache.get(key)
        if cached is not None:
            return EventOutcome(event=cached, source=EventSource.CACHE, model=model)

        try:
            async with asyncio.timeout(self._settings.model_timeout_s):
                event, adjustments = await self._ask_model(
                    ledger, session_id, model, user_prompt, context
                )
        except TimeoutError:
            await self._record_failure(ledger, session_id, model, started=None)
            return self._fallback(context, FallbackReason.TIMEOUT)
        except ProviderTimeoutError:
            return self._fallback(context, FallbackReason.TIMEOUT)
        except ProviderResponseError:
            return self._fallback(context, FallbackReason.INVALID_RESPONSE)
        except ProviderError:
            return self._fallback(context, FallbackReason.PROVIDER_ERROR)

        self._cache.set(key, event)
        return EventOutcome(
            event=event, source=EventSource.MODEL, model=model, adjustments=adjustments
        )

    async def _ask_model(
        self,
        ledger: Ledger,
        session_id: uuid.UUID,
        model: str,
        user_prompt: str,
        context: PromptContext,
    ) -> tuple[GameEvent, list[str]]:
        for attempt in range(1, MAX_ATTEMPTS + 1):
            started = time.perf_counter()
            try:
                result = await self._provider.structured(
                    model=model,
                    system=SYSTEM_PROMPT,
                    user=user_prompt,
                    output=GameEvent,
                    reasoning_effort=self._settings.live_reasoning_effort,
                )
            except ProviderResponseError:
                await self._record_failure(ledger, session_id, model, started)
                if attempt == MAX_ATTEMPTS:
                    raise
                logger.warning("game master returned an invalid event, retrying")
                continue
            except ProviderError:
                await self._record_failure(ledger, session_id, model, started)
                raise

            await ledger.record(
                usage=result.usage,
                endpoint=ENDPOINT,
                latency_ms=result.latency_ms,
                ok=True,
                session_id=session_id,
            )
            event, adjustments = clamp_event(result.value)
            if assumes_a_vehicle(event, context.state):
                if attempt == MAX_ATTEMPTS:
                    raise ProviderResponseError("the event assumes a vehicle the player lacks")
                logger.warning("game master assumed a vehicle the player lacks, retrying")
                continue
            if adjustments:
                logger.info("clamped game master event: %s", adjustments)
            return event, adjustments
        raise AssertionError("unreachable")  # pragma: no cover

    async def _record_failure(
        self, ledger: Ledger, session_id: uuid.UUID, model: str, started: float | None
    ) -> None:
        latency_ms = int((time.perf_counter() - started) * 1000) if started else 0
        await ledger.record(
            usage=Usage(model=model),
            endpoint=ENDPOINT,
            latency_ms=latency_ms,
            ok=False,
            session_id=session_id,
        )

    def fallback_event(self, context: PromptContext, reason: FallbackReason) -> EventOutcome:
        """Hand-written event without calling any model."""
        return self._fallback(context, reason)

    def _fallback(self, context: PromptContext, reason: FallbackReason) -> EventOutcome:
        logger.info("serving fallback event: %s", reason)
        event = pick_fallback_event(
            context.state,
            recent_event_ids=context.recent_event_ids,
            taught_concepts=context.taught_concepts,
            rng=self._rng,
        )
        return EventOutcome(event=event, source=EventSource.FALLBACK, fallback_reason=reason)

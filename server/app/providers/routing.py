"""Provider routing: try the open-weights endpoint first and fall back to the Meta API.

The game never breaks because the partner endpoint is down or slow: any error or a timeout
shorter than the caller's own deadline sends the same request to Meta.
"""

import asyncio
import logging
from typing import Any

from pydantic import BaseModel

from app.config import Settings
from app.providers.base import (
    ChatModelProvider,
    ChatTurn,
    ProviderError,
    ReasoningEffort,
    StructuredResult,
)
from app.providers.open_weights import OpenWeightsProvider

logger = logging.getLogger(__name__)


class FallbackProvider:
    def __init__(
        self, primary: ChatModelProvider, fallback: ChatModelProvider, timeout_s: float
    ) -> None:
        self._primary = primary
        self._fallback = fallback
        self._timeout_s = timeout_s

    async def structured[T: BaseModel](
        self,
        *,
        model: str,
        system: str,
        user: str,
        output: type[T],
        reasoning_effort: ReasoningEffort | None = None,
        max_tokens: int = 2048,
        timeout_s: float | None = None,
    ) -> StructuredResult[T]:
        try:
            async with asyncio.timeout(self._timeout_s):
                return await self._primary.structured(
                    model=model,
                    system=system,
                    user=user,
                    output=output,
                    reasoning_effort=reasoning_effort,
                    max_tokens=max_tokens,
                    timeout_s=timeout_s,
                )
        except (ProviderError, TimeoutError) as error:
            logger.warning("open-weights provider failed (%s), falling back to Meta API", error)
        return await self._fallback.structured(
            model=model,
            system=system,
            user=user,
            output=output,
            reasoning_effort=reasoning_effort,
            max_tokens=max_tokens,
            timeout_s=timeout_s,
        )

    async def chat(
        self,
        *,
        model: str,
        messages: list[dict[str, Any]],
        tools: list[dict[str, Any]],
        output: type[BaseModel],
        reasoning_effort: ReasoningEffort | None = None,
        max_tokens: int = 2048,
    ) -> ChatTurn:
        """One seller-agent step on open weights; this step alone goes to Meta if it fails."""
        try:
            async with asyncio.timeout(self._timeout_s):
                return await self._primary.chat(
                    model=model,
                    messages=messages,
                    tools=tools,
                    output=output,
                    reasoning_effort=reasoning_effort,
                    max_tokens=max_tokens,
                )
        except (ProviderError, TimeoutError) as error:
            logger.warning("open-weights agent step failed (%s), falling back to Meta API", error)
        return await self._fallback.chat(
            model=model,
            messages=messages,
            tools=tools,
            output=output,
            reasoning_effort=reasoning_effort,
            max_tokens=max_tokens,
        )


def build_npc_provider(
    settings: Settings, meta: ChatModelProvider, open_weights: OpenWeightsProvider | None
) -> ChatModelProvider:
    """Provider for NPC dialogue according to NPC_PROVIDER."""
    if settings.npc_provider != "open_weights":
        return meta
    if open_weights is None or not open_weights.configured:
        logger.warning("NPC_PROVIDER=open_weights but the endpoint is not configured; using Meta")
        return meta
    return FallbackProvider(open_weights, meta, settings.open_weights_timeout_s)

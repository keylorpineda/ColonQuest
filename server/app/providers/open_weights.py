"""Open-weights provider for NPC dialogue (Muse Spark Open or Muse Glimmer on Baseten/Modal).

Baseten and Modal expose OpenAI-compatible endpoints, so this reuses the shared chat logic with
another base URL, key and model. The model id requested by callers is a Meta id; this provider
always answers with its own configured open-weights model.
"""

import logging

import httpx2
from openai import AsyncOpenAI
from pydantic import BaseModel

from app.config import Settings
from app.providers.base import ProviderUnavailableError, ReasoningEffort, StructuredResult
from app.providers.openai_compat import structured_completion

logger = logging.getLogger(__name__)

PROVIDER = "open_weights"
# Muse Spark Open always reasons before answering, and reasoning tokens count against max_tokens.
# Without headroom a long prompt can spend the whole budget thinking and return no answer.
REASONING_HEADROOM_TOKENS = 3072


class OpenWeightsProvider:
    def __init__(self, settings: Settings, http_client: httpx2.AsyncClient | None = None) -> None:
        self._settings = settings
        self._http = http_client or httpx2.AsyncClient(timeout=settings.open_weights_timeout_s)
        self._client = AsyncOpenAI(
            api_key=settings.open_weights_api_key.get_secret_value() or "missing-key",
            base_url=settings.open_weights_base_url or "http://open-weights.invalid/v1",
            timeout=settings.open_weights_timeout_s,
            max_retries=0,
            http_client=self._http,
        )

    @property
    def configured(self) -> bool:
        return bool(
            self._settings.open_weights_base_url
            and self._settings.open_weights_api_key.get_secret_value()
        )

    async def aclose(self) -> None:
        await self._http.aclose()

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
        if not self.configured:
            raise ProviderUnavailableError("open-weights endpoint is not configured")
        # NPC lines are short: keep the reasoning low so the reply stays fast.
        return await structured_completion(
            self._client,
            provider=PROVIDER,
            model=self._settings.open_weights_npc_model,
            system=system,
            user=user,
            output=output,
            reasoning_effort=reasoning_effort or "low",
            max_tokens=max_tokens + REASONING_HEADROOM_TOKENS,
            timeout_s=timeout_s,
        )

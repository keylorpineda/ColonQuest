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
    ) -> StructuredResult[T]:
        if not self.configured:
            raise ProviderUnavailableError("open-weights endpoint is not configured")
        # Open-weights servers may not implement reasoning_effort; leave it to their defaults.
        return await structured_completion(
            self._client,
            provider=PROVIDER,
            model=self._settings.open_weights_npc_model,
            system=system,
            user=user,
            output=output,
            max_tokens=max_tokens,
        )

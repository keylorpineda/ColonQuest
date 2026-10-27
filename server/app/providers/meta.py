"""Meta Model API provider (see docs/adr/0001-meta-api-contracts.md).

Chat and images go through the OpenAI SDK pointed at the Meta base URL; transcription is a
multipart call the SDK does not cover, so it uses httpx2 directly.
"""

import base64
import json
import logging
import time
from typing import Any

import httpx2
import openai
from openai import AsyncOpenAI
from pydantic import BaseModel

from app.config import Settings
from app.providers.base import (
    GeneratedImage,
    ProviderResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
    ReasoningEffort,
    StructuredResult,
    Transcription,
    Usage,
)
from app.providers.openai_compat import (
    elapsed_ms,
    structured_completion,
    translate_sdk_error,
)

logger = logging.getLogger(__name__)

PROVIDER = "meta_api"


class MetaProvider:
    """Thin async wrapper around the Meta Model API."""

    def __init__(self, settings: Settings, http_client: httpx2.AsyncClient | None = None) -> None:
        self._settings = settings
        self._http = http_client or httpx2.AsyncClient(timeout=settings.model_timeout_s)
        self._client = AsyncOpenAI(
            api_key=settings.model_api_key.get_secret_value() or "missing-key",
            base_url=settings.meta_base_url,
            timeout=settings.model_timeout_s,
            max_retries=0,  # retries and fallbacks are decided by the game master layer
            http_client=self._http,
        )

    async def aclose(self) -> None:
        await self._http.aclose()

    def _require_key(self) -> None:
        """Without a key every call would fail with 401; skip the network and fall back now."""
        if not self._settings.model_api_key.get_secret_value():
            raise ProviderUnavailableError("MODEL_API_KEY is not configured")

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
        """Ask for JSON that matches ``output`` and validate it with Pydantic."""
        self._require_key()
        return await structured_completion(
            self._client,
            provider=PROVIDER,
            model=model,
            system=system,
            user=user,
            output=output,
            reasoning_effort=reasoning_effort,
            max_tokens=max_tokens,
        )

    async def generate_image(self, prompt: str, *, size: str = "1024x1024") -> GeneratedImage:
        """Generate one PNG image and return its bytes."""
        self._require_key()
        started = time.perf_counter()
        try:
            response = await self._client.images.generate(
                model=self._settings.image_model,
                prompt=prompt,
                n=1,
                size=size,  # Meta accepts any WxH aspect ratio
                response_format="b64_json",
                output_format="png",
            )
        except openai.OpenAIError as error:
            raise translate_sdk_error(error, PROVIDER) from error

        if not response.data or not response.data[0].b64_json:
            raise ProviderResponseError("image response has no data")
        image = response.data[0]
        return GeneratedImage(
            data=base64.b64decode(image.b64_json or ""),
            media_type="image/png",
            revised_prompt=image.revised_prompt,
            usage=Usage(model=self._settings.image_model, images=1),
            latency_ms=elapsed_ms(started),
        )

    async def transcribe(
        self, wav: bytes, *, language_bias: list[str] | None = None
    ) -> Transcription:
        """Transcribe a mono 16-bit PCM WAV clip (16 or 24 kHz)."""
        self._require_key()
        request: dict[str, Any] = {
            "model": self._settings.transcribe_model,
            "audioEncoding": "WAV",
            "mode": "PUSH_TO_TALK",
            "languageBias": language_bias or ["Spanish"],
        }
        started = time.perf_counter()
        try:
            response = await self._http.post(
                f"{self._settings.meta_base_url}/asr/transcribe",
                headers={
                    "Authorization": f"Bearer {self._settings.model_api_key.get_secret_value()}",
                    "Accept": "application/json",
                },
                files={
                    "request": (None, json.dumps(request), "application/json"),
                    "audio": ("clip.wav", wav, "audio/wav"),
                },
            )
        except httpx2.TimeoutException as error:
            raise ProviderTimeoutError(str(error)) from error
        except httpx2.TransportError as error:
            raise ProviderUnavailableError(str(error)) from error

        if response.status_code in (401, 403):
            logger.error("Meta Model API rejected the credentials (%s)", response.status_code)
            raise ProviderUnavailableError(f"transcribe returned {response.status_code}")
        if response.status_code == 429 or response.status_code >= 500:
            raise ProviderUnavailableError(f"transcribe returned {response.status_code}")
        if response.status_code != 200:
            raise ProviderResponseError(f"transcribe returned {response.status_code}")
        body = response.json()
        text = body.get("transcript") if isinstance(body, dict) else None
        if not isinstance(text, str):
            raise ProviderResponseError("transcribe response has no transcript")
        duration_ms = body.get("audioDurationMs", 0)
        return Transcription(
            text=text.strip(),
            usage=Usage(
                model=self._settings.transcribe_model,
                audio_seconds=float(duration_ms) / 1000 if isinstance(duration_ms, int) else 0.0,
            ),
            latency_ms=elapsed_ms(started),
        )

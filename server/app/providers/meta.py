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
from pydantic import BaseModel, ValidationError

from app.config import Settings
from app.providers.base import (
    GeneratedImage,
    ProviderError,
    ProviderResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
    ReasoningEffort,
    StructuredResult,
    Transcription,
    Usage,
)

logger = logging.getLogger(__name__)


def _elapsed_ms(started: float) -> int:
    return int((time.perf_counter() - started) * 1000)


def _translate_sdk_error(error: openai.OpenAIError) -> ProviderError:
    if isinstance(error, openai.APITimeoutError):
        return ProviderTimeoutError(str(error))
    if isinstance(error, openai.AuthenticationError | openai.PermissionDeniedError):
        # A bad or missing key will not fix itself on retry: fail fast to the fallback.
        logger.error("Meta Model API rejected the credentials (%s)", error.status_code)
        return ProviderUnavailableError(f"{error.status_code}: credentials rejected")
    if isinstance(error, openai.APIConnectionError | openai.RateLimitError):
        return ProviderUnavailableError(str(error))
    if isinstance(error, openai.APIStatusError) and error.status_code >= 500:
        return ProviderUnavailableError(f"{error.status_code}: {error.message}")
    return ProviderResponseError(str(error))


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
        extra: dict[str, Any] = {}
        if reasoning_effort is not None:
            extra["reasoning_effort"] = reasoning_effort
        started = time.perf_counter()
        try:
            completion = await self._client.chat.completions.create(
                model=model,
                messages=[
                    {"role": "system", "content": system},
                    {"role": "user", "content": user},
                ],
                response_format={
                    "type": "json_schema",
                    "json_schema": {
                        "name": output.__name__,
                        "schema": output.model_json_schema(by_alias=True),
                    },
                },
                max_tokens=max_tokens,
                **extra,
            )
        except openai.OpenAIError as error:
            raise _translate_sdk_error(error) from error

        content = completion.choices[0].message.content if completion.choices else None
        if not content:
            raise ProviderResponseError("empty completion")
        try:
            value = output.model_validate_json(content)
        except ValidationError as error:
            raise ProviderResponseError(f"schema validation failed: {error}") from error

        usage = completion.usage
        cached = 0
        if usage and usage.prompt_tokens_details and usage.prompt_tokens_details.cached_tokens:
            cached = usage.prompt_tokens_details.cached_tokens
        return StructuredResult(
            value=value,
            usage=Usage(
                model=model,
                input_tokens=usage.prompt_tokens if usage else 0,
                output_tokens=usage.completion_tokens if usage else 0,
                cached_input_tokens=cached,
            ),
            latency_ms=_elapsed_ms(started),
        )

    async def generate_image(self, prompt: str, *, size: str = "1024x1024") -> GeneratedImage:
        """Generate one PNG image and return its bytes."""
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
            raise _translate_sdk_error(error) from error

        if not response.data or not response.data[0].b64_json:
            raise ProviderResponseError("image response has no data")
        image = response.data[0]
        return GeneratedImage(
            data=base64.b64decode(image.b64_json or ""),
            media_type="image/png",
            revised_prompt=image.revised_prompt,
            usage=Usage(model=self._settings.image_model, images=1),
            latency_ms=_elapsed_ms(started),
        )

    async def transcribe(
        self, wav: bytes, *, language_bias: list[str] | None = None
    ) -> Transcription:
        """Transcribe a mono 16-bit PCM WAV clip (16 or 24 kHz)."""
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
            latency_ms=_elapsed_ms(started),
        )

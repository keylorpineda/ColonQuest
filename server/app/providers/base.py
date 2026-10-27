"""Provider-agnostic types so callers never depend on a vendor SDK."""

from dataclasses import dataclass
from typing import Literal, Protocol

from pydantic import BaseModel

ReasoningEffort = Literal["minimal", "low", "medium", "high", "xhigh", "max"]


class ProviderError(Exception):
    """Base error for any model provider failure."""


class ProviderTimeoutError(ProviderError):
    """The provider did not answer within the configured timeout."""


class ProviderUnavailableError(ProviderError):
    """Network failure, rate limit or 5xx from the provider."""


class ProviderResponseError(ProviderError):
    """The provider answered, but the payload is missing or fails schema validation."""


@dataclass(frozen=True, slots=True)
class Usage:
    model: str
    input_tokens: int = 0
    output_tokens: int = 0
    cached_input_tokens: int = 0
    audio_seconds: float = 0.0
    images: int = 0
    provider: str = "meta_api"  # recorded in model_calls; "open_weights" for Baseten/Modal


@dataclass(frozen=True, slots=True)
class StructuredResult[T: BaseModel]:
    value: T
    usage: Usage
    latency_ms: int


@dataclass(frozen=True, slots=True)
class Transcription:
    text: str
    usage: Usage
    latency_ms: int


@dataclass(frozen=True, slots=True)
class GeneratedImage:
    data: bytes
    media_type: str
    revised_prompt: str | None
    usage: Usage
    latency_ms: int


class Transcriber(Protocol):
    async def transcribe(
        self, wav: bytes, *, language_bias: list[str] | None = None
    ) -> Transcription: ...


class ImageGenerator(Protocol):
    async def generate_image(self, prompt: str, *, size: str = "1024x1024") -> GeneratedImage: ...


class ModelProvider(Protocol):
    """Interface shared by the Meta API provider and the optional open-weights provider."""

    async def structured[T: BaseModel](
        self,
        *,
        model: str,
        system: str,
        user: str,
        output: type[T],
        reasoning_effort: ReasoningEffort | None = None,
        max_tokens: int = 2048,
    ) -> StructuredResult[T]: ...

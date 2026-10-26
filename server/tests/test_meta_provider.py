import base64
import json
from collections.abc import Callable
from typing import Any

import httpx2
import pytest
from pydantic import BaseModel, SecretStr

from app.config import Settings
from app.providers.base import (
    ProviderResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
)
from app.providers.meta import MetaProvider

pytestmark = pytest.mark.anyio

Handler = Callable[[httpx2.Request], httpx2.Response]


class Greeting(BaseModel):
    text: str
    count: int


def make_provider(handler: Handler) -> MetaProvider:
    settings = Settings(model_api_key=SecretStr("test-key"), _env_file=None)  # type: ignore[call-arg]
    client = httpx2.AsyncClient(transport=httpx2.MockTransport(handler))
    return MetaProvider(settings, http_client=client)


def completion(content: str | None) -> dict[str, Any]:
    return {
        "id": "cmpl_1",
        "object": "chat.completion",
        "created": 0,
        "model": "muse-spark-1.3",
        "choices": [
            {
                "index": 0,
                "finish_reason": "stop",
                "message": {"role": "assistant", "content": content},
            }
        ],
        "usage": {
            "prompt_tokens": 120,
            "completion_tokens": 30,
            "total_tokens": 150,
            "prompt_tokens_details": {"cached_tokens": 100},
        },
    }


async def test_structured_sends_schema_and_parses_result() -> None:
    seen: dict[str, Any] = {}

    def handler(request: httpx2.Request) -> httpx2.Response:
        seen["url"] = str(request.url)
        seen["auth"] = request.headers["authorization"]
        seen["body"] = json.loads(request.content)
        return httpx2.Response(200, json=completion('{"text": "hola", "count": 2}'))

    provider = make_provider(handler)
    result = await provider.structured(
        model="muse-spark-1.3",
        system="sys",
        user="usr",
        output=Greeting,
        reasoning_effort="low",
    )

    assert result.value == Greeting(text="hola", count=2)
    assert result.usage.input_tokens == 120
    assert result.usage.cached_input_tokens == 100
    assert seen["url"] == "https://api.meta.ai/v1/chat/completions"
    assert seen["auth"] == "Bearer test-key"
    assert seen["body"]["reasoning_effort"] == "low"
    assert seen["body"]["response_format"]["type"] == "json_schema"
    assert seen["body"]["response_format"]["json_schema"]["name"] == "Greeting"


@pytest.mark.parametrize("content", [None, "not json", '{"text": "hola"}'])
async def test_structured_rejects_invalid_content(content: str | None) -> None:
    provider = make_provider(lambda _: httpx2.Response(200, json=completion(content)))
    with pytest.raises(ProviderResponseError):
        await provider.structured(model="m", system="s", user="u", output=Greeting)


@pytest.mark.parametrize(
    ("status", "error"),
    [
        (429, ProviderUnavailableError),
        (503, ProviderUnavailableError),
        (401, ProviderUnavailableError),
        (403, ProviderUnavailableError),
        (400, ProviderResponseError),
    ],
)
async def test_structured_translates_http_errors(status: int, error: type[Exception]) -> None:
    provider = make_provider(lambda _: httpx2.Response(status, json={"error": {"message": "x"}}))
    with pytest.raises(error):
        await provider.structured(model="m", system="s", user="u", output=Greeting)


async def test_structured_timeout() -> None:
    def handler(request: httpx2.Request) -> httpx2.Response:
        raise httpx2.ReadTimeout("slow", request=request)

    provider = make_provider(handler)
    with pytest.raises(ProviderTimeoutError):
        await provider.structured(model="m", system="s", user="u", output=Greeting)


async def test_generate_image_decodes_png() -> None:
    png = b"\x89PNG fake"

    def handler(request: httpx2.Request) -> httpx2.Response:
        body = json.loads(request.content)
        assert body["model"] == "muse-image-1.0"
        assert body["response_format"] == "b64_json"
        return httpx2.Response(
            200,
            json={"created": 0, "data": [{"b64_json": base64.b64encode(png).decode()}]},
        )

    image = await make_provider(handler).generate_image("pixel art mechanic")
    assert image.data == png
    assert image.media_type == "image/png"


async def test_transcribe_sends_multipart_wav() -> None:
    def handler(request: httpx2.Request) -> httpx2.Response:
        assert str(request.url) == "https://api.meta.ai/v1/asr/transcribe"
        body = request.content.decode("latin-1")
        assert '"audioEncoding": "WAV"' in body
        assert '"languageBias": ["Spanish"]' in body
        assert "audio/wav" in body
        return httpx2.Response(
            200,
            json={
                "sessionId": "s",
                "transcript": " ¿Cuánto es en total? ",
                "audioDurationMs": 2500,
                "turns": [],
            },
        )

    result = await make_provider(handler).transcribe(b"RIFF....WAVE")
    assert result.text == "¿Cuánto es en total?"
    assert result.usage.audio_seconds == 2.5


@pytest.mark.parametrize(
    ("status", "error"),
    [
        (429, ProviderUnavailableError),
        (500, ProviderUnavailableError),
        (401, ProviderUnavailableError),
        (400, ProviderResponseError),
    ],
)
async def test_transcribe_errors(status: int, error: type[Exception]) -> None:
    provider = make_provider(lambda _: httpx2.Response(status, json={}))
    with pytest.raises(error):
        await provider.transcribe(b"RIFF")


def test_api_key_is_never_in_repr() -> None:
    settings = Settings(model_api_key=SecretStr("super-secret"), _env_file=None)  # type: ignore[call-arg]
    assert "super-secret" not in repr(settings)

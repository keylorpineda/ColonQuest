import asyncio
import json
from typing import Any

import httpx2
import pytest
from pydantic import BaseModel, SecretStr

from app.config import Settings
from app.providers.base import (
    ProviderResponseError,
    ProviderUnavailableError,
    StructuredResult,
    Usage,
)
from app.providers.open_weights import REASONING_HEADROOM_TOKENS, OpenWeightsProvider
from app.providers.routing import FallbackProvider, build_npc_provider

from .fakes import FakeProvider, fail

pytestmark = pytest.mark.anyio


class Line(BaseModel):
    text: str


def open_weights_settings(**overrides: Any) -> Settings:
    values: dict[str, Any] = {
        "npc_provider": "open_weights",
        "open_weights_base_url": "https://model-abc.api.baseten.co/v1",
        "open_weights_api_key": SecretStr("ow-key"),
        "open_weights_npc_model": "muse-spark-open",
    }
    return Settings(_env_file=None, **(values | overrides))  # type: ignore[call-arg]


def completion(content: str) -> dict[str, Any]:
    return {
        "id": "c",
        "object": "chat.completion",
        "created": 0,
        "model": "muse-spark-open",
        "choices": [
            {
                "index": 0,
                "finish_reason": "stop",
                "message": {"role": "assistant", "content": content},
            }
        ],
        "usage": {"prompt_tokens": 40, "completion_tokens": 8, "total_tokens": 48},
    }


async def test_open_weights_uses_its_own_endpoint_and_model() -> None:
    seen: dict[str, Any] = {}

    def handler(request: httpx2.Request) -> httpx2.Response:
        seen["url"] = str(request.url)
        seen["auth"] = request.headers["authorization"]
        seen["body"] = json.loads(request.content)
        return httpx2.Response(200, json=completion('{"text": "Mae, solo ₡19 600 al mes."}'))

    provider = OpenWeightsProvider(
        open_weights_settings(),
        http_client=httpx2.AsyncClient(transport=httpx2.MockTransport(handler)),
    )
    result = await provider.structured(
        model="muse-wonder-1.0", system="s", user="u", output=Line, max_tokens=600
    )

    assert result.value.text.startswith("Mae")
    assert result.usage.provider == "open_weights"
    assert result.usage.model == "muse-spark-open"
    assert seen["url"] == "https://model-abc.api.baseten.co/v1/chat/completions"
    assert seen["auth"] == "Bearer ow-key"
    assert seen["body"]["model"] == "muse-spark-open"
    # Muse Spark Open always reasons: keep it low and leave room for the answer after it.
    assert seen["body"]["reasoning_effort"] == "low"
    assert seen["body"]["max_tokens"] == 600 + REASONING_HEADROOM_TOKENS


async def test_unconfigured_open_weights_fails_fast() -> None:
    provider = OpenWeightsProvider(open_weights_settings(open_weights_base_url=""))
    assert not provider.configured
    with pytest.raises(ProviderUnavailableError):
        await provider.structured(model="m", system="s", user="u", output=Line)


def meta_answer() -> Any:
    usage = Usage(model="muse-wonder-1.0")
    return lambda: StructuredResult(value=Line(text="desde Meta"), usage=usage, latency_ms=1)


@pytest.mark.parametrize(
    "error", [ProviderUnavailableError("down"), ProviderResponseError("bad json")]
)
async def test_fallback_provider_uses_meta_when_primary_fails(error: Exception) -> None:
    primary, meta = FakeProvider(fail(error)), FakeProvider(meta_answer())
    result = await FallbackProvider(primary, meta, timeout_s=1).structured(
        model="muse-wonder-1.0", system="s", user="u", output=Line
    )

    assert result.value.text == "desde Meta"
    assert meta.calls[0]["model"] == "muse-wonder-1.0"


async def test_fallback_provider_times_out_slow_primary() -> None:
    slow = FakeProvider(meta_answer(), delay_s=0.5)
    meta = FakeProvider(meta_answer())
    started = asyncio.get_running_loop().time()
    await FallbackProvider(slow, meta, timeout_s=0.05).structured(
        model="m", system="s", user="u", output=Line
    )
    assert asyncio.get_running_loop().time() - started < 0.4
    assert len(meta.calls) == 1


def test_build_npc_provider_routing() -> None:
    meta = FakeProvider()
    configured = OpenWeightsProvider(open_weights_settings())
    unconfigured = OpenWeightsProvider(open_weights_settings(open_weights_api_key=SecretStr("")))

    assert build_npc_provider(Settings(_env_file=None), meta, configured) is meta  # type: ignore[call-arg]
    assert build_npc_provider(open_weights_settings(), meta, unconfigured) is meta
    assert isinstance(
        build_npc_provider(open_weights_settings(), meta, configured), FallbackProvider
    )


async def test_open_weights_chat_sends_tools_with_low_effort_and_headroom() -> None:
    seen: dict[str, Any] = {}

    def handler(request: httpx2.Request) -> httpx2.Response:
        seen["body"] = json.loads(request.content)
        return httpx2.Response(200, json=completion('{"text": "Mae, a 36 meses sale tuanis."}'))

    provider = OpenWeightsProvider(
        open_weights_settings(),
        http_client=httpx2.AsyncClient(transport=httpx2.MockTransport(handler)),
    )
    tool = {"type": "function", "function": {"name": "quote_term", "parameters": {}}}
    turn = await provider.chat(
        model="muse-spark-1.3",
        messages=[{"role": "user", "content": "¿Y a más meses?"}],
        tools=[tool],
        output=Line,
        reasoning_effort="minimal",
        max_tokens=2400,
    )

    assert turn.content is not None and "36 meses" in turn.content
    assert turn.usage.provider == "open_weights"
    assert seen["body"]["model"] == "muse-spark-open"
    assert seen["body"]["tools"][0]["function"]["name"] == "quote_term"
    assert seen["body"]["reasoning_effort"] == "low"
    assert seen["body"]["max_tokens"] == 2400 + REASONING_HEADROOM_TOKENS


async def test_fallback_provider_sends_a_failed_agent_step_to_meta() -> None:
    from app.providers.base import ChatTurn

    from .fakes import FakeToolProvider

    def meta_turn() -> ChatTurn:
        return ChatTurn(content='{"text": "Meta"}', tool_calls=[], usage=Usage("m"), latency_ms=1)

    def down() -> ChatTurn:
        raise ProviderUnavailableError("baseten down")

    primary, meta = FakeToolProvider(down), FakeToolProvider(meta_turn)
    turn = await FallbackProvider(primary, meta, timeout_s=1).chat(  # type: ignore[arg-type]
        model="muse-spark-1.3", messages=[], tools=[], output=Line
    )

    assert turn.content == '{"text": "Meta"}'
    assert meta.calls[0]["model"] == "muse-spark-1.3"


async def test_spent_open_weights_budget_sends_npcs_to_meta() -> None:
    class Spent:
        async def exhausted(self) -> bool:
            return True

    primary, meta = FakeProvider(meta_answer()), FakeProvider(meta_answer())
    router = FallbackProvider(primary, meta, timeout_s=1, budget=Spent())  # type: ignore[arg-type]
    result = await router.structured(model="muse-spark-1.3", system="s", user="u", output=Line)

    assert result.value.text == "desde Meta"
    assert primary.calls == []
    assert len(meta.calls) == 1

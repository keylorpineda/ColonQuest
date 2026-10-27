"""Shared logic for OpenAI-compatible chat endpoints (Meta Model API, Baseten, Modal).

Providers differ only in base URL, key and model ids, so the request shape, error mapping and
usage accounting live here once.
"""

import logging
import time
from typing import Any, cast

import openai
from openai import AsyncOpenAI
from openai.types.chat import ChatCompletionMessageParam
from openai.types.shared_params import ResponseFormatJSONSchema
from pydantic import BaseModel, ValidationError

from app.providers.base import (
    ChatTurn,
    ProviderError,
    ProviderResponseError,
    ProviderTimeoutError,
    ProviderUnavailableError,
    ReasoningEffort,
    StructuredResult,
    ToolCall,
    Usage,
)

logger = logging.getLogger(__name__)


def elapsed_ms(started: float) -> int:
    return int((time.perf_counter() - started) * 1000)


def translate_sdk_error(error: openai.OpenAIError, provider: str) -> ProviderError:
    if isinstance(error, openai.APITimeoutError):
        return ProviderTimeoutError(str(error))
    if isinstance(error, openai.AuthenticationError | openai.PermissionDeniedError):
        # A bad or missing key will not fix itself on retry: fail fast to the fallback.
        logger.error("%s rejected the credentials (%s)", provider, error.status_code)
        return ProviderUnavailableError(f"{error.status_code}: credentials rejected")
    if isinstance(error, openai.APIConnectionError | openai.RateLimitError):
        return ProviderUnavailableError(str(error))
    if isinstance(error, openai.APIStatusError) and error.status_code >= 500:
        return ProviderUnavailableError(f"{error.status_code}: {error.message}")
    return ProviderResponseError(str(error))


def usage_from(completion: Any, model: str, provider: str) -> Usage:
    usage = completion.usage
    cached = 0
    if usage and usage.prompt_tokens_details and usage.prompt_tokens_details.cached_tokens:
        cached = usage.prompt_tokens_details.cached_tokens
    return Usage(
        model=model,
        input_tokens=usage.prompt_tokens if usage else 0,
        output_tokens=usage.completion_tokens if usage else 0,
        cached_input_tokens=cached,
        provider=provider,
    )


def json_schema_format(output: type[BaseModel]) -> ResponseFormatJSONSchema:
    return {
        "type": "json_schema",
        "json_schema": {
            "name": output.__name__,
            "schema": output.model_json_schema(by_alias=True),
        },
    }


async def structured_completion[T: BaseModel](
    client: AsyncOpenAI,
    *,
    provider: str,
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
    messages: list[ChatCompletionMessageParam] = [
        {"role": "system", "content": system},
        {"role": "user", "content": user},
    ]
    started = time.perf_counter()
    try:
        completion = await client.chat.completions.create(
            model=model,
            messages=messages,
            response_format=json_schema_format(output),
            max_tokens=max_tokens,
            **extra,
        )
    except openai.OpenAIError as error:
        raise translate_sdk_error(error, provider) from error

    content = completion.choices[0].message.content if completion.choices else None
    if not content:
        raise ProviderResponseError("empty completion")
    try:
        value = output.model_validate_json(content)
    except ValidationError as error:
        raise ProviderResponseError(f"schema validation failed: {error}") from error
    return StructuredResult(
        value=value,
        usage=usage_from(completion, model, provider),
        latency_ms=elapsed_ms(started),
    )


async def chat_completion(
    client: AsyncOpenAI,
    *,
    provider: str,
    model: str,
    messages: list[dict[str, Any]],
    tools: list[dict[str, Any]],
    output: type[BaseModel],
    reasoning_effort: ReasoningEffort | None = None,
    max_tokens: int = 2048,
) -> ChatTurn:
    """One step of a tool-calling conversation; the final answer must match ``output``."""
    extra: dict[str, Any] = {}
    if reasoning_effort is not None:
        extra["reasoning_effort"] = reasoning_effort
    started = time.perf_counter()
    try:
        completion = await client.chat.completions.create(
            model=model,
            messages=cast(list[ChatCompletionMessageParam], messages),
            tools=cast(Any, tools),
            response_format=json_schema_format(output),
            max_tokens=max_tokens,
            **extra,
        )
    except openai.OpenAIError as error:
        raise translate_sdk_error(error, provider) from error
    if not completion.choices:
        raise ProviderResponseError("empty completion")

    message = completion.choices[0].message
    calls = [
        ToolCall(id=call.id, name=call.function.name, arguments=call.function.arguments or "{}")
        for call in (message.tool_calls or [])
        if call.type == "function"
    ]
    return ChatTurn(
        content=message.content,
        tool_calls=calls,
        usage=usage_from(completion, model, provider),
        latency_ms=elapsed_ms(started),
        message=message.model_dump(exclude_none=True, exclude={"annotations", "audio", "refusal"}),
    )

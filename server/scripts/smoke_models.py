"""Live smoke test of every Meta model the game uses (SCRUM-20).

Usage (from server/, with MODEL_API_KEY in .env):
    uv run python scripts/smoke_models.py --audio path/to/question.wav --image-out muse.png

Makes real, billed calls: Muse Wonder and Muse Spark write one monthly event each, Muse Voice
Transcribe transcribes a Spanish clip and Muse Image draws one portrait. It prints latency, tokens
and the estimated cost of each call, and exits 1 if any model fails. Costs a few cents.
"""

import argparse
import asyncio
import sys
from collections.abc import Awaitable, Callable
from decimal import Decimal
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.config import get_settings
from app.cost_guard import estimate_cost
from app.game_master.limits import clamp_event
from app.game_master.prompt import SYSTEM_PROMPT, PromptContext, build_user_prompt
from app.providers.base import ProviderError, Usage
from app.providers.meta import MetaProvider
from app.schemas import GameEvent, PlayerState

STATE = PlayerState.model_validate(
    {
        "month": 3,
        "salaryNet": 450000,
        "cash": 120000,
        "savings": 50000,
        "emergencyFund": 0,
        "creditCard": {
            "balance": 250000,
            "limit": 300000,
            "monthlyRatePct": 3.5,
            "minPaymentPct": 10,
        },
        "installments": [{"item": "Celular", "monthly": 25000, "monthsLeft": 20}],
        "history": ["paid_minimum_only"],
        "wellbeing": 55,
    }
)
PORTRAIT_PROMPT = (
    "Pixel art portrait of a friendly Costa Rican electronics salesman behind a store counter, "
    "limited palette, 32-bit era, front facing, plain background"
)
EVENT_MODELS = ("muse-wonder-1.0", "muse-spark-1.3")


def report(name: str, usage: Usage, latency_ms: int, detail: str) -> Decimal:
    cost = estimate_cost(usage)
    tokens = f"in={usage.input_tokens} out={usage.output_tokens}" if usage.input_tokens else ""
    print(f"OK    {name:<28} {latency_ms:>6} ms  ${cost:<9} {tokens}  {detail}")
    return cost


async def check(name: str, call: Callable[[], Awaitable[Decimal]]) -> Decimal | None:
    try:
        return await call()
    except (ProviderError, OSError, ValueError) as error:
        print(f"FAIL  {name:<28} {type(error).__name__}: {error}")
        return None


async def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--audio", type=Path, help="mono 16-bit WAV at 16 or 24 kHz, in Spanish")
    parser.add_argument("--image-out", type=Path, help="where to save the generated PNG")
    args = parser.parse_args()

    settings = get_settings()
    if not settings.model_api_key.get_secret_value():
        print("MODEL_API_KEY is not set in server/.env")
        return 1
    provider = MetaProvider(settings)
    results: list[Decimal | None] = []
    try:
        prompt = build_user_prompt(PromptContext(state=STATE))
        for model in EVENT_MODELS:

            async def event(model: str = model) -> Decimal:
                result = await provider.structured(
                    model=model,
                    system=SYSTEM_PROMPT,
                    user=prompt,
                    output=GameEvent,
                    reasoning_effort="low",
                    max_tokens=2048,
                )
                clamped, adjustments = clamp_event(result.value)
                detail = f'"{clamped.title}" ({clamped.type.value}, {len(adjustments)} clamped)'
                return report(model, result.usage, result.latency_ms, detail)

            results.append(await check(model, event))

        if args.audio:

            async def transcribe() -> Decimal:
                result = await provider.transcribe(args.audio.read_bytes())
                return report(
                    settings.transcribe_model, result.usage, result.latency_ms, f'"{result.text}"'
                )

            results.append(await check(settings.transcribe_model, transcribe))
        else:
            print(f"SKIP  {settings.transcribe_model:<28} pass --audio to test it")

        async def image() -> Decimal:
            result = await provider.generate_image(PORTRAIT_PROMPT, size="512x512")
            if args.image_out:
                args.image_out.write_bytes(result.data)
            detail = f"{len(result.data) // 1024} KB" + (
                f" -> {args.image_out}" if args.image_out else ""
            )
            return report(settings.image_model, result.usage, result.latency_ms, detail)

        results.append(await check(settings.image_model, image))
    finally:
        await provider.aclose()

    total = sum((cost for cost in results if cost is not None), Decimal(0))
    failed = sum(cost is None for cost in results)
    print(f"\n{len(results) - failed} of {len(results)} models answered, estimated cost ${total}")
    return 1 if failed else 0


if __name__ == "__main__":
    if sys.platform == "win32":
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())  # type: ignore[attr-defined]
    sys.exit(asyncio.run(main()))

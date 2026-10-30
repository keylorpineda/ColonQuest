"""On-demand pixel-art image generation with cost guard and a 1x1 PNG fallback."""

import asyncio
import base64
import time
from dataclasses import dataclass

from app.config import Settings
from app.cost_guard import BudgetMode
from app.game_master.ledger import Ledger
from app.providers.base import ImageGenerator, ProviderError, Usage

ENDPOINT = "image_generation"

STYLE_PROMPT = (
    "16-bit pixel art in a warm Costa Rican palette, limited 32-color palette, "
    "crisp pixel outlines, no text or watermarks, consistent top-down RPG framing"
)
# Unity draws the year's figures on top of the card, so the illustration must stay blank.
CARD_STYLE_SUFFIX = "no text, no numbers, no letters, leave the upper third empty for an overlay"
CARD_SUBJECTS = {
    "card_year_debt_free": "a young Costa Rican celebrating at a sunny viewpoint over the valley",
    "card_year_some_debt": "a young Costa Rican walking home at dusk, calm but thoughtful",
    "card_year_heavy_debt": "a young Costa Rican under a rainy sky looking at an empty wallet",
}

# 1x1 transparent PNG so Unity's texture loader never fails when budget or credentials are absent.
FALLBACK_PNG: bytes = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg=="
)


@dataclass(frozen=True, slots=True)
class ImageOutcome:
    data: bytes
    media_type: str
    source: str
    cacheable: bool


def build_image_prompt(key: str) -> str:
    """Build a consistent pixel-art prompt from an imageKey identifier."""
    words = key.replace("_", " ").strip()
    if key.startswith("card_"):
        subject = CARD_SUBJECTS.get(key, f"postcard illustration of {words}")
        return f"{STYLE_PROMPT}, {CARD_STYLE_SUFFIX}. Subject: {subject}."
    if key.startswith(("npc_", "portrait_")):
        subject = f"character bust portrait of {words} in a Costa Rican neighborhood"
    elif key.startswith("scene_"):
        subject = f"everyday Costa Rican scene: {words}"
    else:
        subject = f"Costa Rican everyday life scene or character: {words}"
    return f"{STYLE_PROMPT}. Subject: {subject}."


class ImageService:
    def __init__(self, generator: ImageGenerator, settings: Settings) -> None:
        self._generator = generator
        self._settings = settings

    async def generate(self, ledger: Ledger, key: str) -> ImageOutcome:
        """Generate an image with Muse Image when budget and rate limits allow it."""
        if await ledger.budget_mode() == BudgetMode.FALLBACK_ONLY:
            return ImageOutcome(FALLBACK_PNG, "image/png", "fallback", cacheable=False)

        started = time.perf_counter()
        try:
            async with asyncio.timeout(self._settings.image_timeout_s):
                image = await self._generator.generate_image(
                    build_image_prompt(key), size="512x512"
                )
        except (TimeoutError, ProviderError):
            await ledger.record(
                usage=Usage(model=self._settings.image_model, images=0),
                endpoint=ENDPOINT,
                latency_ms=int((time.perf_counter() - started) * 1000),
                ok=False,
                session_id=None,
            )
            return ImageOutcome(FALLBACK_PNG, "image/png", "fallback", cacheable=False)

        await ledger.record(
            usage=image.usage,
            endpoint=ENDPOINT,
            latency_ms=image.latency_ms,
            ok=True,
            session_id=None,
        )
        return ImageOutcome(image.data, image.media_type, "model", cacheable=True)

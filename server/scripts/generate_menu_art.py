"""Generate candidates for the background of the main menu with Muse Image (SCRUM-80).

Usage (from server/, with MODEL_API_KEY in .env):
    IMAGE_TIMEOUT_S=120 uv run python scripts/generate_menu_art.py --out DIR
    IMAGE_TIMEOUT_S=120 uv run python scripts/generate_menu_art.py --out DIR --only 2

Makes real, billed calls: $0.01 per image, one call per variant. Like the smoke test of the
models it is a tool run by hand, outside the game, so it does not touch the database.
"""

import argparse
import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.config import get_settings
from app.providers.meta import MetaProvider

SIZE = "1920x1080"
COST_PER_IMAGE = 0.01

BASE = (
    "Key art for a polished 2D indie PC game, one continuous wide landscape from edge to edge, in "
    "the style of a modern travel poster and a hand-inked graphic novel: confident ink linework "
    "with varied line weight, flat colors with two or three clean shadow tones, a harmonious, "
    "slightly muted limited palette, several overlapping layers of hills that get paler with "
    "distance for a real sense of depth. Crisp and clean, 2D and clearly drawn by hand, but "
    "sophisticated and not childish. No photorealism, no 3D, no airbrushed gradients. "
    "Foreground right: a keel-billed toucan with natural proportions, not too big, on a branch "
    "with broad tropical leaves, seen in profile looking left, holding a green paper banknote "
    "crosswise in the tip of its beak. The banknote is blank, with only a border and an oval. "
    "Middle and far distance across the whole width: a Costa Rican valley with neat rows of "
    "coffee on rolling hills, a small white church with a red tin roof and a few houses, and a "
    "tall volcano with a thin wisp of smoke. "
    "The left side of the scene continues the same landscape, with quiet distant hills and sky. "
    "Absolutely no text, no letters, no numbers, no logos, no signs, no people, no frames, no "
    "panels, no split screen. "
)

VARIANTS = (
    "Early morning: soft pale blue sky fading to warm cream at the horizon, thin flat clouds.",
    "Late afternoon: warm golden light, peach and amber sky, long flat shadows on the hills.",
    "After the rain: a teal and sage palette, low flat clouds around the volcano, fresh greens.",
)


async def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", type=Path, required=True, help="folder for the PNG files")
    parser.add_argument(
        "--only", type=int, choices=range(1, len(VARIANTS) + 1), help="one variant, from 1"
    )
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)

    numbers = [args.only] if args.only else list(range(1, len(VARIANTS) + 1))
    provider = MetaProvider(get_settings())
    failed = 0
    try:
        for number in numbers:
            try:
                image = await provider.generate_image(BASE + VARIANTS[number - 1], size=SIZE)
            except Exception as error:  # a tool run by hand: report it and go on with the others
                print(f"variant {number}: failed ({type(error).__name__})")
                failed += 1
                continue

            path = args.out / f"menu_{number}.png"
            path.write_bytes(image.data)
            print(f"variant {number}: {path} ({len(image.data) // 1024} KB)")
    finally:
        await provider.aclose()

    print(f"estimated cost: ${COST_PER_IMAGE * (len(numbers) - failed):.2f}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))

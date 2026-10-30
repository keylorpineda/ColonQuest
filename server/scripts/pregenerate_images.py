"""Pre-generate the NPC portraits, event scenes and year cards with Muse Image (SCRUM-49).

Usage (from server/, with MODEL_API_KEY and DATABASE_URL in .env):
    uv run python scripts/pregenerate_images.py            # only the missing keys
    uv run python scripts/pregenerate_images.py --force    # regenerate every key
    uv run python scripts/pregenerate_images.py --only npc_kevin_vendedor

Makes real, billed calls: each image takes 14 to 17 s and costs $0.01. Images are stored in the
`images` table exactly like GET /api/images/{key}, so the game reads them from the cache and never
waits on Muse Image. Run it against the QA database first. Exits 1 if any image fails.
"""

import argparse
import asyncio
import json
import sys
from decimal import Decimal
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from sqlalchemy import select

from app.config import get_settings
from app.db import CachedImage
from app.db.session import get_engine, get_sessionmaker
from app.game_master.ledger import DbLedger
from app.images.service import CARD_SUBJECTS, ImageService
from app.providers.meta import MetaProvider

FALLBACK_EVENTS = Path(__file__).resolve().parents[1] / "app/game_master/fallback_events.json"
PORTRAIT_KEYS = (
    "npc_don_rafa_mecanico",
    "npc_kevin_vendedor",
    "npc_dona_marta_vendedora",
    "npc_prestamista_gota_a_gota",
    "npc_amigo_piramide",
)
COST_PER_IMAGE = Decimal("0.01")


def scene_keys() -> list[str]:
    """Every imageKey used by the hand-written fallback events."""
    events = json.loads(FALLBACK_EVENTS.read_text(encoding="utf-8"))
    return sorted({event["imageKey"] for event in events if event.get("imageKey")})


def all_keys() -> list[str]:
    return [*PORTRAIT_KEYS, *scene_keys(), *CARD_SUBJECTS]


async def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--force", action="store_true", help="regenerate keys already cached")
    parser.add_argument("--only", nargs="+", metavar="KEY", help="limit the run to these keys")
    args = parser.parse_args()

    settings = get_settings()
    if not settings.model_api_key.get_secret_value():
        print("MODEL_API_KEY is not set in server/.env")
        return 1
    keys = args.only or all_keys()
    provider = MetaProvider(settings)
    service = ImageService(provider, settings)
    generated = skipped = failed = 0
    total_ms = 0
    try:
        async with get_sessionmaker()() as db:
            cached = set(
                (
                    await db.execute(
                        select(CachedImage.key).where(
                            CachedImage.key.in_(keys), CachedImage.data.is_not(None)
                        )
                    )
                ).scalars()
            )
            ledger = DbLedger(db, settings, None)
            for key in keys:
                if key in cached and not args.force:
                    print(f"SKIP  {key:<30} already cached")
                    skipped += 1
                    continue
                outcome = await service.generate(ledger, key)
                if not outcome.cacheable:
                    await db.commit()  # keep the failed call in model_calls
                    print(f"FAIL  {key:<30} {outcome.source}")
                    failed += 1
                    continue
                await db.merge(
                    CachedImage(key=key, media_type=outcome.media_type, data=outcome.data)
                )
                await db.commit()
                latency_ms = outcome.latency_ms
                total_ms += latency_ms
                generated += 1
                print(f"OK    {key:<30} {latency_ms:>6} ms  {len(outcome.data) // 1024} KB")
    finally:
        await provider.aclose()
        await get_engine().dispose()

    cost = COST_PER_IMAGE * generated
    average = total_ms // generated if generated else 0
    print(
        f"\n{generated} generated, {skipped} skipped, {failed} failed; "
        f"average {average} ms, estimated cost ${cost}"
    )
    return 1 if failed else 0


if __name__ == "__main__":
    if sys.platform == "win32":
        asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())  # type: ignore[attr-defined]
    sys.exit(asyncio.run(main()))

"""Copy the pre-generated images from the database into app/images/bundled, so a local run has them.

Usage (from server/, with the DATABASE_URL that holds the images in .env):
    uv run python scripts/export_images.py

Only the keys in app/images/keys.py are written, one PNG per key. The local backend serves them
before it would pay Muse Image for one (GET /api/images/{key}). Makes no model calls.
"""

import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from sqlalchemy import select

from app.db import CachedImage
from app.db.session import get_sessionmaker
from app.images.bundled import BUNDLED_DIR
from app.images.keys import PREGENERATED_KEYS


async def main() -> int:
    BUNDLED_DIR.mkdir(parents=True, exist_ok=True)
    async with get_sessionmaker()() as db:
        rows = (
            await db.execute(select(CachedImage).where(CachedImage.key.in_(PREGENERATED_KEYS)))
        ).scalars()
        written = 0
        for image in rows:
            if image.data and image.media_type == "image/png":
                (BUNDLED_DIR / f"{image.key}.png").write_bytes(image.data)
                written += 1
    missing = sorted(key for key in PREGENERATED_KEYS if not (BUNDLED_DIR / f"{key}.png").exists())
    print(f"Wrote {written} images to {BUNDLED_DIR}.")
    if missing:
        print(f"Missing: {', '.join(missing)}. Run scripts/pregenerate_images.py first.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(asyncio.run(main(), loop_factory=asyncio.SelectorEventLoop))

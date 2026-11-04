"""The pre-generated images shipped with the repository, for a local run with an empty database.

scripts/export_images.py writes one PNG per key of app/images/keys.py here. GET /api/images/{key}
serves them before it would pay Muse Image, so the portraits and scenes show up with no wait.
"""

from pathlib import Path

from app.images.keys import PREGENERATED_KEYS

BUNDLED_DIR = Path(__file__).resolve().parent


def bundled_image(key: str) -> bytes | None:
    """The shipped PNG of a pre-generated key, or None. Keys outside the list are never read."""
    if key not in PREGENERATED_KEYS:
        return None
    path = BUNDLED_DIR / f"{key}.png"
    return path.read_bytes() if path.is_file() else None

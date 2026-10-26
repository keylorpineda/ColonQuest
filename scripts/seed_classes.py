"""Create Class Mode classes through the API (local or deployed).

Usage:
    python scripts/seed_classes.py "Finanzas 10-1" "Finanzas 10-2"
    API_URL=https://colon-quest-api.onrender.com python scripts/seed_classes.py "Piloto UNA"

Prints one "code<TAB>name" line per class so the codes can be handed to teachers.
"""

import json
import os
import sys
import urllib.request

API_URL = os.environ.get("API_URL", "http://127.0.0.1:8000").rstrip("/")


def create_class(name: str) -> str:
    request = urllib.request.Request(  # noqa: S310 - URL comes from the operator's environment
        f"{API_URL}/api/classes",
        data=json.dumps({"name": name}).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=15) as response:  # noqa: S310
        code: str = json.load(response)["code"]
    return code


def main(names: list[str]) -> int:
    if not names:
        print(__doc__, file=sys.stderr)
        return 2
    for name in names:
        print(f"{create_class(name)}\t{name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

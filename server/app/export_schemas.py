"""Export the contract JSON schemas and OpenAPI document to shared/schemas.

Usage (from server/):
    uv run python -m app.export_schemas          # write files
    uv run python -m app.export_schemas --check  # exit 1 if committed files are stale
"""

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from pydantic import BaseModel

from app.main import create_app
from app.schemas import GameEvent, NegotiationReply, PlayerState
from app.schemas.api import (
    CreateSessionRequest,
    DecisionRequest,
    HealthResponse,
    NegotiationRequest,
)

OUTPUT_DIR = Path(__file__).resolve().parents[2] / "shared" / "schemas"
CONTRACT_MODELS: tuple[type[BaseModel], ...] = (
    PlayerState,
    GameEvent,
    NegotiationReply,
    CreateSessionRequest,
    DecisionRequest,
    HealthResponse,
    NegotiationRequest,
)


def _render(document: dict[str, Any]) -> str:
    return json.dumps(document, indent=2, sort_keys=True, ensure_ascii=False) + "\n"


def build_documents() -> dict[str, str]:
    """Return the expected file contents keyed by file name."""
    documents = {
        f"{model.__name__}.schema.json": _render(
            model.model_json_schema(by_alias=True, mode="serialization")
        )
        for model in CONTRACT_MODELS
    }
    documents["openapi.json"] = _render(create_app().openapi())
    return documents


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="fail if files are out of date")
    args = parser.parse_args(argv)

    stale: list[str] = []
    for name, content in build_documents().items():
        path = OUTPUT_DIR / name
        current = path.read_text(encoding="utf-8") if path.exists() else None
        if current == content:
            continue
        if args.check:
            stale.append(name)
        else:
            OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8", newline="\n")
            print(f"wrote {path.relative_to(OUTPUT_DIR.parents[1])}")

    if stale:
        print(
            "Contract schemas are out of date: "
            + ", ".join(stale)
            + "\nRun `uv run python -m app.export_schemas` and commit shared/schemas.",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

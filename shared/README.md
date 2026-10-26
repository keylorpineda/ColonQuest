# Shared API contract

JSON schemas exported from the backend's Pydantic models. **The Pydantic models are the source of truth.**
Unity DTOs are generated or hand-written from these files.

Any change here must be announced to the other teammate before merging.

## Files

| File | Source |
| --- | --- |
| `schemas/PlayerState.schema.json` | `app.schemas.PlayerState` |
| `schemas/GameEvent.schema.json` | `app.schemas.GameEvent` |
| `schemas/NegotiationReply.schema.json` | `app.schemas.NegotiationReply` |
| `schemas/openapi.json` | FastAPI OpenAPI document |

## Regenerate

```bash
cd server
uv run python -m app.export_schemas
```

CI runs `uv run python -m app.export_schemas --check` and fails if these files are stale.

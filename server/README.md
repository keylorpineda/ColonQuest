# Colón Quest — Server

FastAPI backend: AI game master, negotiation, image cache, cost guard and Class Mode telemetry.

See the [root README](../README.md) for setup commands and [CONTRIBUTING.md](../CONTRIBUTING.md) for conventions.

## Layout

```
app/
  main.py        # FastAPI app factory and router wiring
  config.py      # Settings loaded from environment variables
  api/           # HTTP routers (one module per resource)
tests/           # pytest suite
```

## Database

Neon (PostgreSQL 17) through SQLAlchemy 2 and psycopg 3. `DATABASE_URL` comes from `.env`;
plain `postgresql://` URLs from the Neon console are switched to the psycopg driver automatically.

```bash
uv run alembic upgrade head                                 # apply migrations
uv run alembic revision --autogenerate -m "describe change"  # after editing app/db/models.py
```

`tests/test_migrations.py` upgrades, checks that models and migrations match, downgrades and
upgrades again. It only runs when `TEST_DATABASE_URL` points at a disposable database (CI uses a
Postgres service container). Never point it at Neon.

## Running locally on Windows

On Windows `uv run uvicorn app.main:app` starts, but every database call fails with
`Psycopg cannot use the 'ProactorEventLoop'` and `/api/health` says `database: unavailable`.
psycopg 3 needs the selector event loop and uvicorn picks the proactor one there. Use the
launcher instead; it runs the same app:

```bash
uv run python ../scripts/run_dev_server.py              # http://127.0.0.1:8000
uv run python ../scripts/run_dev_server.py --port 8010
```

It has no auto reload. Linux, macOS and Render run plain `uvicorn` without any change.

Without `MODEL_API_KEY` every model call falls back: events come from `fallback_events.json`,
negotiation answers from the seller template, and the response header `X-Event-Source` or
`X-Reply-Source` says `fallback`. That is enough to play and to test the Unity client end to end.

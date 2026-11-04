# ColonQuest — Server

FastAPI backend: AI simulation director, negotiation, image cache, cost guard and Class Mode telemetry.

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

SQLAlchemy 2. `DATABASE_URL` comes from `.env`: by default a local SQLite file, whose tables are created on
start. For PostgreSQL 17, plain `postgresql://` URLs are switched to the psycopg driver automatically.

```bash
uv run alembic upgrade head                                 # apply migrations
uv run alembic revision --autogenerate -m "describe change"  # after editing app/db/models.py
```

`tests/test_migrations.py` upgrades, checks that models and migrations match, downgrades and
upgrades again. It only runs when `TEST_DATABASE_URL` points at a disposable database (CI uses a
Postgres service container). Never point it at Neon.

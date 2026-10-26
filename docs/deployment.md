# Deployment guide (Render + WebGL)

## Backend on Render (`render.yaml`)

The repository root includes [`render.yaml`](../render.yaml), a Render Blueprint that deploys the
FastAPI backend (`server/`) from `main` only after GitHub Actions CI passes (`autoDeployTrigger: checksPass`).

### 1. Create the service on Render

1. In Render: **New → Blueprint** and select `keylorpineda/ColonQuest`.
2. Fill in the prompted environment variables (`sync: false`):

| Variable | Description | Example |
| --- | --- | --- |
| `MODEL_API_KEY` | Meta Model API key (never committed) | `meta-...` |
| `DATABASE_URL` | Neon PostgreSQL connection string (plain `postgresql://...` works) | `postgresql://user:pass@ep-...neon.tech/neondb?sslmode=require` |
| `ALLOWED_ORIGINS` | Comma-separated origins allowed by CORS (WebGL domain + local dev) | `https://colon-quest.pages.dev,http://localhost:8000` |
| `GAME_MASTER_MODEL` | Primary model for monthly events and negotiation (`muse-wonder-1.0` in prod) | `muse-wonder-1.0` |
| `SUMMARY_MODEL` | Model for end-of-game summaries and teacher explanations | `muse-spark-1.3` |
| `DAILY_BUDGET_USD` | Daily spend ceiling before automatic Spark downgrade (80%) and fallback (100%) | `5.0` |

### 2. Health check and migrations

- On startup, Render runs `uv run alembic upgrade head && uv run uvicorn app.main:app --host 0.0.0.0 --port $PORT`.
- Render polls `GET /api/health`, which reports database connectivity and remaining daily budget.

### 3. Verify the deployed service

Run the end-to-end HTTP smoke test against the live URL:

```bash
API_URL=https://colon-quest-api.onrender.com python scripts/smoke_test.py
```

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
| `SUMMARY_MODEL` | Model for the year-end summary, behind a loading screen | `muse-wonder-1.0` |
| `PLANNER_MODEL` | Model of the season planner, which runs in the background after the first event | `muse-wonder-1.0` |
| `PLANNER_TIMEOUT_S` | Timeout of the background season planner | `30` |
| `SUMMARY_TIMEOUT_S` | Timeout of the year-end summary; Unity waits up to 30 s there | `25` |
| `CLASS_REPORT_MODEL` | Model for the teacher dashboard explanation (a live call, keep it on Spark) | `muse-spark-1.3` |
| `DAILY_BUDGET_USD` | Daily Meta spend ceiling before automatic Spark downgrade (80%) and fallback (100%) | `5.0` |
| `OPEN_WEIGHTS_DAILY_BUDGET_USD` | Daily Baseten spend; once reached, the NPCs use Meta until midnight UTC | `3` |

### 2. Health check and migrations

- On startup, Render runs `uv run alembic upgrade head && uv run uvicorn app.main:app --host 0.0.0.0 --port $PORT`.
- Render polls `GET /api/health`, which reports database connectivity and remaining daily budget.

### 3. Verify the deployed service

Run the end-to-end HTTP smoke test against the live URL:

```bash
API_URL=https://colon-quest-api.onrender.com python scripts/smoke_test.py
```

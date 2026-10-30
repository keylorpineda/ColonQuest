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

## WebGL build on Cloudflare Pages (`pnpm`)

The Unity WebGL build is a static site published to Cloudflare Pages with `wrangler`. The
repository root has a `package.json` whose only dependency is `wrangler` (development only), and
`pnpm` is pinned through `packageManager`. CI (`Web / build check`) only runs
`pnpm install --frozen-lockfile`; publishing is always manual.

### 1. Build the WebGL player into `unity/Builds/webgl`

The menu **Colon Quest > Build > WebGL** writes to the system temp folder
(`%TEMP%\ColonQuestBuild\webgl`) unless it receives `-buildPath`. Build from the command line so
the output lands where the scripts expect it (the folder is ignored by git):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -quit -projectPath unity -executeMethod ColonQuest.EditorTools.BuildScript.WebGL -buildPath unity/Builds/webgl -logFile -
```

Or build from the menu and copy the folder to `unity/Builds/webgl`. The build uses Gzip with the
decompression fallback, so Cloudflare Pages needs no custom headers.

Before building for production, call `ApiConfig.Use("https://colon-quest-api.onrender.com/api")`
in WebGL builds so the game talks to Render instead of localhost.

### 2. Preview and publish

```bash
pnpm install
pnpm webgl:serve
```

`webgl:serve` serves the build at http://localhost:8080 with the same server Cloudflare uses.
Then log in once and publish:

```bash
pnpm exec wrangler login
pnpm webgl:deploy
```

`webgl:deploy` runs `wrangler pages deploy unity/Builds/webgl --project-name colon-quest`. The
first run creates the project; the site is served at `https://colon-quest.pages.dev`.

### 3. Allow the published domain in CORS

Add the published origin to `ALLOWED_ORIGINS` in Render (comma-separated, keep the existing
values), for example `https://colon-quest.pages.dev`. Render redeploys after the change.

### 4. Keep Render awake

Point an UptimeRobot HTTP monitor at `https://colon-quest-api.onrender.com/api/ping` every 5
minutes. It answers without touching Neon or any model, so it wakes Render without waking the
database.

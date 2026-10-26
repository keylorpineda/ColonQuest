# Colón Quest

> A 2D financial-literacy game for Costa Rican students, built for the **Meta Global AI Developer Hackathon 2026** (Indie track).

Around 87% of Costa Ricans carry some kind of debt. Colón Quest turns everyday money decisions into a story with consequences: an AI game master narrates each month, and the player negotiates by voice with characters who try to trick them — installment plans with hidden fees, informal "gota a gota" loans, and pyramid schemes.

**Team:** DoWhile Team — Keylor (backend, AI, finance engine) · Jefferson (Unity, art, game flow)

## Architecture

```
Unity 2D (WebGL + desktop)
  ├─ Deterministic finance engine (C#)
  ├─ Top-down pixel-art map with task stations
  └─ Overlay panels (SINPE, account statement, negotiation)
        │  HTTPS + JSON
        ▼
FastAPI backend (Python 3.12, Pydantic v2)
  ├─ Game master, negotiation, images, summary
  ├─ Cost guard and caching
  └─ Session telemetry and Class Mode
        │                         │
        ▼                         ▼
Meta Model API              Neon (PostgreSQL)
```

Core rule: **the AI tells the story; the finance engine keeps the books.** No amount, interest rate or balance ever comes from a language model.

## Repository layout

| Path | Owner | Contents |
| --- | --- | --- |
| [`server/`](server/) | Keylor | FastAPI app, model providers, cost guard, migrations, tests |
| [`unity/`](unity/) | Jefferson | Unity 2D project |
| [`shared/`](shared/) | Both | JSON schemas exported from the backend (the API contract) |
| [`scripts/`](scripts/) | Both | Image pre-generation, test class seeding |
| [`docs/`](docs/) | Both | Architecture, decisions, pilot results |

## Getting started (backend)

Requires [uv](https://docs.astral.sh/uv/) (it installs Python 3.12 for you).

```bash
cd server
cp .env.example .env   # fill in your own values, never commit .env
uv sync
uv run uvicorn app.main:app --reload
```

Open <http://localhost:8000/docs> for the OpenAPI docs.

```bash
uv run pytest
uv run ruff check .
uv run ruff format --check .
uv run mypy app
```

## Contributing

We track work in Jira (project key `CQ`) and link every branch, commit and pull request to a ticket. `main` holds stable releases; all work branches off `develop` and returns to it through pull requests. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a PR.

## License

[MIT](LICENSE)

# ADR 0003: Agentic game master and seller

- **Status:** Accepted (features off by default)
- **Date:** 2026-10-27
- **Tickets:** SCRUM-69, SCRUM-70, SCRUM-71

## Context

The Best Agentic Use-Case prize rewards agents that decide and run multi-step tasks. The game
master already works with one model call per month; agents must never make the demo slower or
less reliable.

## Decisions

- **Tools are deterministic and read-only** (`app/agents/tools.py`): `simulate_purchase`,
  `project_debt`, `get_player_history`, `get_concepts_not_yet_taught`. They compute with
  `Decimal` and integer colones and never write the player's state.
- **Bounded loop** (`app/agents/loop.py`): at most `AGENT_MAX_STEPS` (5) model calls inside
  `AGENT_TIMEOUT_S` (10 s); every step is recorded in `model_calls` (`endpoint=event_agent`).
- **Agentic game master** (`AGENTIC_GAME_MASTER`): reads the history and computes figures before
  writing. The event is clamped like any model event. Provider errors fall back to the
  single-call game master; a **timeout serves a hand-written event directly** (deviation from
  CLAUDE.md, which says "use the base version") so a player never waits for a second model call
  after the full agent budget.
- **Season planner** (`SEASON_PLANNER`, `app/game_master/planner.py`): on the first event of a
  session Muse Spark drafts a 12-month plan (`sessions.season_plan`, JSONB). The schema enforces
  months 1 to 12 in order, at least 8 distinct concepts and December as aguinaldo or year-end
  expenses; any failure stores the deterministic `default_plan`, ordered by the player's profile.
  Each later event adjusts the plan without a model call: when the last decision left the player
  with more debt than the one before, that concept is reinforced in the next free month (never
  December, never twice). The month's slot reaches the game master as `plannedFocus`.
- Budget and per-session/IP limits are checked before the agent starts; at 80 % of the daily
  budget the agent runs on Spark, at 100 % it never runs.

## How to enable

Apply the migration first (`uv run alembic upgrade head`), then set `SEASON_PLANNER=true` and
`AGENTIC_GAME_MASTER=true` in Render once the playable month is stable, then watch
`SELECT endpoint, count(*), avg(latency_ms) FROM model_calls GROUP BY 1`.

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
- Budget and per-session/IP limits are checked before the agent starts; at 80 % of the daily
  budget the agent runs on Spark, at 100 % it never runs.

## How to enable

Set `AGENTIC_GAME_MASTER=true` in Render once the playable month is stable, then watch
`SELECT endpoint, count(*), avg(latency_ms) FROM model_calls GROUP BY 1`.

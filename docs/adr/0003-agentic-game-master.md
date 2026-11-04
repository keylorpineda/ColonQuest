# ADR 0003: Agentic simulation director and seller

- **Status:** Accepted. **On by default:** `AGENTIC_GAME_MASTER`, `SEASON_PLANNER` and `SELLER_AGENT` are `true` in
  `server/.env.example`. The season planner runs on Muse Wonder in the background, and the seller agent on Muse
  Spark Open in Baseten. Measured times: [Benchmarks](../../BENCHMARKS.md).
- **Date:** 2026-10-27
- **Tickets:** SCRUM-69, SCRUM-70, SCRUM-71

## Context

The Best Agentic Use-Case prize rewards agents that decide and run multi-step tasks. The simulation
director already works with one model call per month; agents must never make the demo slower or
less reliable.

## Decisions

- **Tools are deterministic and read-only** (`app/agents/tools.py`): `simulate_purchase`,
  `project_debt`, `get_player_history`, `get_concepts_not_yet_taught`. They compute with
  `Decimal` and integer colones and never write the user's state.
- **Bounded loop** (`app/agents/loop.py`): at most `AGENT_MAX_STEPS` (5) model calls inside
  `AGENT_TIMEOUT_S` (10 s); every step is recorded in `model_calls` (`endpoint=event_agent`).
- **Agentic simulation director** (`AGENTIC_GAME_MASTER`): reads the history and computes figures before
  writing. The event is clamped like any model event. Provider errors fall back to the
  single-call simulation director; a **timeout serves a hand-written event directly** (deviation from
  CLAUDE.md, which says "use the base version") so a user never waits for a second model call
  after the full agent budget.
- **Season planner** (`SEASON_PLANNER`, `app/game_master/planner.py`): the first event of a
  session stores the deterministic `default_plan`, ordered by the user's profile, and answers
  at once. A FastAPI background task, with its own database session, then asks Muse Wonder
  (`PLANNER_MODEL`, `PLANNER_TIMEOUT_S` = 30 s) for a 12-month plan (`sessions.season_plan`,
  JSONB); if it validates and the stored plan is still the default one, it replaces it from
  month 2. Measured on Oct 29: 6.2 to 16.7 s and about $0.015 per plan, none of it on the
  user's request. The schema enforces months 1 to 12 in order, at least 8 distinct concepts and
  December as aguinaldo or year-end expenses; any failure keeps the default plan.
  Each later event adjusts the plan without a model call: when the last decision left the user
  with more debt than the one before, that concept is reinforced in the next free month (never
  December, never twice). The month's slot reaches the simulation director as `plannedFocus`.
- **Seller agent** (`SELLER_AGENT`, `app/negotiation/agent.py`): the salesperson tries to
  maximize its earnings, which grow with the term. `quote_term(months)` re-prices the offer under
  negotiation and returns the seller's private earnings. The agent may omit figures until asked
  but never lie: every amount in its reply must be on the hidden sheet or in a quotable tool
  result (its earnings never are), otherwise a scripted reply is served. Steps are recorded as
  `negotiation_agent`; provider errors use the single-call negotiator, timeouts the scripted
  reply, and `X-Reply-Source: agent` marks agent replies.
- **Seller on open weights** (Oct 29): with `NPC_PROVIDER=open_weights` the agent steps run on
  Muse Spark Open in Baseten (`OpenWeightsProvider.chat`, `reasoning_effort: low` plus 3 072
  tokens of headroom); any failing or late step goes to Meta through `FallbackProvider.chat`.
  Baseten does not enforce the JSON schema when tools are present, so the prompt spells out the
  three keys of the final answer.
- **Seller memory**: each turn also receives `playerHabits`, the key questions the user asked
  other sellers in the same session (`negotiations.key_questions_asked`), so the seller changes
  tactic ("ya sé que vos preguntás por el seguro"). The figure guard is unchanged.
- Measured on QA (Oct 29, 5 turns, 2 offers): 3.3 to 5.3 s per turn, 8 Baseten steps of 1.5 to
  2.5 s, `quote_term` used to push 60 months; one invented amount was caught by the guard and
  served the scripted reply.
- Budget and per-session/IP limits are checked before the agent starts; at 80 % of the daily
  budget the agent runs on Spark, at 100 % it never runs.

## How to enable

They are already on in `server/.env.example`. Each one can be turned off in `server/.env` (`SEASON_PLANNER`,
`SELLER_AGENT`, `AGENTIC_GAME_MASTER`); on Postgres, apply the migrations first (`uv run alembic upgrade head`). Watch
`SELECT endpoint, count(*), avg(latency_ms) FROM model_calls GROUP BY 1`.

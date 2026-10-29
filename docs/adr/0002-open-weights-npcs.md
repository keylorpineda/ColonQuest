# ADR 0002: Open-weights provider for NPC dialogue

- **Status:** Accepted (feature off by default)
- **Date:** 2026-10-27
- **Ticket:** SCRUM-68

## Context

The Best Build on Meta Open Weights prize needs a central part of the game running on Muse Spark
Open or Muse Glimmer through a partner (Baseten or Modal). Negotiation is the highest-volume
model use, so moving it also protects the Meta API credits during judging.

## Decision

- `NPC_PROVIDER=open_weights` routes negotiation NPCs to `OpenWeightsProvider`, an
  OpenAI-compatible client (`OPEN_WEIGHTS_BASE_URL`, `OPEN_WEIGHTS_API_KEY`,
  `OPEN_WEIGHTS_NPC_MODEL`). The game master, voice and images stay on the Meta API.
- `FallbackProvider` gives the partner `OPEN_WEIGHTS_TIMEOUT_S` (5 s) and then sends the same
  request to Meta, leaving room inside the 8 s turn deadline. Errors never reach the player.
- `model_calls.provider` records who answered (`open_weights` or `meta_api`), which is the
  evidence for the prize. Open-weights usage costs zero against `DAILY_BUDGET_USD` because it
  runs on partner credits.
- If the endpoint is not configured, the app logs a warning and keeps using Meta.

## How to enable

1. Deploy Muse Spark Open on Baseten or Modal and copy its OpenAI-compatible base URL and key.
2. Set `NPC_PROVIDER=open_weights`, `OPEN_WEIGHTS_BASE_URL` and `OPEN_WEIGHTS_API_KEY` in Render.
3. Play a negotiation and check `SELECT provider, count(*) FROM model_calls GROUP BY 1`.

## Baseten in practice (Oct 28)

- Hackathon keys only accept `meta-models/muse-spark-open-1026` at `https://inference.baseten.co/v1`.
- The endpoint accepts `response_format` with a strict JSON schema.
- Muse Spark Open always reasons, and reasoning tokens count against `max_tokens`: with the
  negotiation prompt it spent the whole 1 500-token budget thinking and returned no answer. The
  provider now sends `reasoning_effort: low` and adds 3 072 tokens of headroom.
- Measured: a negotiation turn takes about 2 s (435 input, ~450 output tokens, about $0.0025),
  quoting exactly the figures of the hidden sheet.

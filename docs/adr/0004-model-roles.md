# ADR 0004: One clear role per model, chosen by measured latency

- **Status:** Accepted (roles marked *Pending* are still to be built)
- **Date:** 2026-10-29
- **Tickets:** SCRUM-20, SCRUM-41, SCRUM-42, SCRUM-49

## Context

The judges score "Use of Meta tools" (25 %) by whether each model has a clear job. On Oct 28 a
live smoke test (`server/scripts/smoke_models.py`) and an end-to-end run against the QA database
showed two problems:

1. **Every Meta model reasons before it answers.** Reasoning makes calls slow, and reasoning
   tokens count against `max_tokens`, so a short budget returns an empty answer and the game
   silently falls back to hand-written content.
2. **Unity gives up early.** `ApiConfig.EventTimeoutSeconds` is 8 s for the monthly event and
   `DefaultTimeoutSeconds` is 10 s for every other call. Any model slower than that is invisible
   to the player.

As a result Muse Wonder and Muse Image were configured but never reached the player.

## Measurements (Oct 28, real keys)

All times are wall-clock from the backend, single requests from Costa Rica.

### Monthly event (game master prompt, about 1 600 input tokens)

| Model | `reasoning_effort` | Samples (s) | Output tokens | Cost per event |
| --- | --- | --- | --- | --- |
| Muse Wonder | `low` | 14.6, 18.3 | ~1 190 | ~$0.028 |
| Muse Wonder | `minimal` | 5.8, 6.7, 7.2, 10.5 | ~500 to 570 | ~$0.011 |
| Muse Spark 1.3 | `low` | 5.1, 6.5, 7.7 | 1 260 to 2 030 | $0.008 to $0.011 |
| Muse Spark 1.3 | `minimal` | 3.0, 3.4, 3.9, 4.4 | 530 to 770 | ~$0.004 |
| Either | `none` | rejected | | Not supported (400) |

### Features end to end (local backend, QA database, after the fixes in PR #48)

| Feature | Provider and model | Latency | Result |
| --- | --- | --- | --- |
| Monthly event | Meta, Spark `minimal` | 2.5 to 3.8 s | OK |
| Agentic game master | Meta, Spark `minimal` | 1.4 to 4.2 s | OK, called `get_player_history` and `get_concepts_not_yet_taught` |
| Season planner | Meta, Spark `minimal` | 3.6 s | OK, 12-month plan stored (failed before: budget spent on reasoning) |
| Negotiation | Baseten, Muse Spark Open | 1.9 to 2.8 s | OK, exact figures of the hidden sheet (₡19 600, ₡3 500, ₡569 400) |
| Seller agent | Meta, Spark `minimal` | 2.1 s | OK (failed before: budget spent on reasoning) |
| Voice | Meta, Voice Transcribe | 0.7 to 1.5 s | OK ("cuánto pago en total con el seguro y la comisión") |
| Year-end summary | Meta, Spark `minimal` | 1.7 to 3.2 s | OK (failed before: 700-token budget) |
| Class Mode report | Meta, Spark `minimal` | 2.2 to 2.7 s | OK |
| Image 512×512 | Meta, Muse Image | 13.8 to 17.4 s | OK with a 30 s timeout (was always cut at 8 s) |

### Baseten (Muse Spark Open, `meta-models/muse-spark-open-1026`)

- On a one-line prompt it spent 363 to 1 435 reasoning tokens. With the negotiation prompt and a
  1 500-token budget it returned an empty answer every time.
- With `reasoning_effort: low` and 3 072 tokens of headroom: 1.9 to 2.8 s, 435 input and about
  450 output tokens, about $0.0025 per turn.
- Strict JSON-schema output works.

### Prices and budgets

| Model | Input / cached / output per million tokens | Other |
| --- | --- | --- |
| Muse Wonder | $4 / $0.20 / $20 | |
| Muse Spark 1.3 | $1.25 / $0.15 / $4.25 | |
| Muse Spark Open on Baseten | $1.25 / $0.15 / $4.25 | Paid with $100 of hackathon credits, up to 2M tokens per minute |
| Muse Image | | $0.01 per image |
| Voice Transcribe | | $0.18 per hour of audio |

Meta credits: $150, with `DAILY_BUDGET_USD=5` (30 days). A 3-month run costs about $0.04 of Meta
credits; all the tests on Oct 28 cost under $0.15.

## Decisions

| Model | Role | Why | Status |
| --- | --- | --- | --- |
| **Muse Spark 1.3** | Live game master (monthly event, agentic game master with tools), Class Mode report, Meta fallback for the sellers | The only Meta text model that answers inside Unity's 8 s every time (3.0 to 4.4 s with `minimal`) | Done, live on Render |
| **Muse Wonder** | Plans the player's year and narrates the year-end summary; writes the offline event bank | Too slow and too variable for live calls (5.8 to 10.5 s even at `minimal`), but the best writer. These calls happen where the player is not waiting, or once offline | *Pending* |
| **Muse Spark Open** (Baseten) | The sellers: tactics to earn commission, exact figures through tools, memory of what the player asked during the run | Fastest option (about 2 s), paid by Baseten credits instead of Meta, and the central piece for the Open Weights prize | Dialogue done and live; tactics and memory *pending* |
| **Muse Image** | NPC portraits, event scenes and the illustration of the shareable year card | 14 to 17 s per image rules out live use, so images are pre-generated and cached. The year card is an illustration **without text**: the code draws the figures on top, because no amount may come from a model and image models misdraw digits | *Pending* |
| **Voice Transcribe** | The player's voice when negotiating | 0.7 to 1.5 s and almost free; typing always works as a fallback | Done |

### Rules that follow from the measurements

- Calls the player waits on use `LIVE_REASONING_EFFORT=minimal`.
- Every `max_tokens` leaves room for reasoning (2 000 or more for text, plus 3 072 on Baseten).
- A slow model never sits on the request path of a live call: it runs in the background, offline,
  or behind a loading screen with its own timeout.
- Muse Wonder may return to the live game master only if Unity requests the next event ahead of
  time, with an automatic switch to Spark when Wonder is late.

## Consequences

- The README and the submission can name one clear role for each of the five models.
- Wonder and Image add almost no cost: about $0.03 per finished run for the summary and plan,
  and about $0.30 once for the pre-generated images.
- Unity needs a longer timeout for the year-end summary and must draw the figures on the year card.

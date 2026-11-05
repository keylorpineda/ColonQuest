<p align="center"><b>English</b> | <a href="BENCHMARKS.es.md">Español</a></p>

<p align="center">
  <a href="README.md"><img src="docs/assets/tab-readme-en.svg" alt="README"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
  <a href="RUN_AND_BUILD.md"><img src="docs/assets/tab-run-en.svg" alt="Run and build, for the judges"></a>
</p>

<p align="center">
  <img src="docs/assets/benchmarks-en.svg?v=1" width="100%" alt="The latency and cost of every Meta model in ColonQuest, measured with real keys, and the decisions that came from them. The full content of this card is available as text in the section below.">
</p>

<details open>
<summary><b>Read this page as text</b></summary>

## Benchmarks and model decisions

How each Meta model got its job in ColonQuest. We measured every model with real keys on 28 and 29 October
2026, single requests from Costa Rica, timed by the backend from request to answer. The raw record, with every
sample, is [ADR 0004](docs/adr/0004-model-roles.md).

### In one minute

- **The simulator waits 8 seconds for the monthly event**, then plays a scripted one. Any model slower than that
  is invisible to the user.
- **Only Muse Spark answers inside 8 s every time** (3.0 to 4.4 s with `minimal` effort), so it is the live
  simulation director.
- **Muse Wonder writes best but takes 5.8 to 10.5 s**, so it only runs where nobody waits: planning your year in
  the background and writing your ending.
- **Muse Spark Open on Baseten is the fastest** (1.9 to 2.8 s), and it is paid with Baseten credits, so it voices
  every character you talk to.
- **Muse Image takes 14 to 17 s per picture**, so every picture is drawn once beforehand and ships in the zip.

### The monthly event: which model, which effort

The simulation director's prompt, about 1 600 input tokens.

| Model | Reasoning effort | Samples | Output tokens | Cost per event |
| --- | --- | --- | --- | --- |
| Muse Wonder | `low` | 14.6 s, 18.3 s | about 1 190 | about $0.028 |
| Muse Wonder | `minimal` | 5.8 s, 6.7 s, 7.2 s, 10.5 s | 500 to 570 | about $0.011 |
| Muse Spark 1.3 | `low` | 5.1 s, 6.5 s, 7.7 s | 1 260 to 2 030 | $0.008 to $0.011 |
| **Muse Spark 1.3** | **`minimal`** | **3.0 s, 3.4 s, 3.9 s, 4.4 s** | **530 to 770** | **about $0.004** |
| Either | `none` | rejected by the API | | not supported |

**Decision:** Muse Spark with `minimal` effort for every call the user waits on (`LIVE_REASONING_EFFORT`).

### Every feature, end to end

What each feature took through the real backend, with the provider and model it uses today.

| Feature | Provider and model | Time | What came back |
| --- | --- | --- | --- |
| Monthly event | Meta API, Muse Spark `minimal` | 2.5 to 3.8 s | A valid event |
| Agentic simulation director | Meta API, Muse Spark `minimal` | 1.4 to 4.2 s | Called `get_player_history` and `get_concepts_not_yet_taught` before writing |
| Season planner | Meta API, Muse Wonder `minimal`, in the background | 6.2 to 16.7 s | A 12-month plan; the first event never waits for it |
| Negotiation | Baseten, Muse Spark Open | 1.9 to 2.8 s | The exact figures of the hidden sheet (₡19 600, ₡3 500, ₡569 400) |
| Seller agent with its tool | Baseten, Muse Spark Open | 3.3 to 5.3 s | Re-priced the offer with `quote_term` to push 60 months |
| Voice | Meta API, Muse Voice Transcribe | 0.7 to 1.5 s | "cuánto pago en total con el seguro y la comisión" |
| Year-end story | Meta API, Muse Wonder `minimal` | 5.2 to 7.6 s | Written behind a 25 s loading screen |
| Class Mode report | Meta API, Muse Spark `minimal` | 2.2 to 2.7 s | A plain explanation of figures computed in SQL |
| Image, 512 × 512 | Meta API, Muse Image | 13.8 to 17.4 s | Drawn once and shipped; never generated while you play |

### What we learned the hard way

- **Every Meta model reasons before it answers**, and those tokens count against `max_tokens`. With a 700-token
  budget the summary came back empty, and the simulator fell back without a word. Every call now leaves 2 000
  tokens or more.
- **Baseten spent 363 to 1 435 reasoning tokens on a one-line prompt.** With a 1 500-token budget the negotiation
  came back empty every time. With `reasoning_effort: low` and 3 072 tokens more, it answers in 1.9 to 2.8 s.
- **`reasoning_effort: none` does not exist** in the Meta API; `minimal` is the lowest.
- **Muse Image ignores the requested size** and returns 1 600 × 1 600 PNGs of 3 to 4 MB, so the backend shrinks
  each one to 512 pixels and 256 colors before keeping it.

### What a run costs

Unit costs from the `model_calls` table with the official prices.

| Call | In a guided run | Unit cost | Subtotal |
| --- | --- | --- | --- |
| Monthly event, Muse Spark | 3 | about $0.004 | $0.012 |
| Season plan, Muse Wonder | 1 | about $0.015 | $0.015 |
| Year-end story, Muse Wonder | 1 | about $0.008 | $0.008 |
| Characters, Muse Spark Open on Baseten | about 8 turns | about $0.0025 | $0.020 |
| Voice, Muse Voice Transcribe | about 1 minute | $0.18 an hour | $0.003 |
| Pictures, Muse Image | drawn once | $0.01 each | $0 per run |
| **Total** | | | **about $0.06** |

| Model | Price per million tokens (input / cached / output) |
| --- | --- |
| Muse Wonder | $4 / $0.20 / $20 |
| Muse Spark 1.3 | $1.25 / $0.15 / $4.25 |
| Muse Spark Open on Baseten | $1.25 / $0.15 / $4.25 |

Muse Image costs $0.01 a picture and Muse Voice Transcribe $0.18 an hour of audio.

### The decisions, and what we turned down

| Decision | Why | What we turned down |
| --- | --- | --- |
| Muse Spark is the live simulation director | The only model always inside the 8 s | Muse Wonder live: 5.8 to 10.5 s even at `minimal` |
| Muse Wonder plans the year and writes the ending | The best writer, where nobody waits | Running it on every event |
| The characters run on Muse Spark Open in Baseten | The fastest, a separate budget, and an open model at the center of the simulator | Modal: we would have had to deploy and keep the model, with cold starts |
| Every picture is drawn beforehand | 14 to 17 s is too long to wait | Drawing live, even with a cache |
| Typing always works next to voice | A microphone can fail | Voice as the only input |
| Every model has a fallback | The simulator must never stop | Showing an error |

If Baseten fails, takes more than 5 s or reaches its own daily budget, the characters switch to Muse Spark on the
Meta API, and `model_calls` records which provider answered. The other decisions:
[ADR 0001](docs/adr/0001-meta-api-contracts.md) (the Meta API contracts),
[ADR 0002](docs/adr/0002-open-weights-npcs.md) (open weights on Baseten),
[ADR 0003](docs/adr/0003-agentic-game-master.md) (the agents).

### Measure it yourself

With your key in `server/.env`, from `server/`:

```bash
uv run python scripts/smoke_models.py --audio question.wav --image-out muse.png
```

It calls Muse Wonder, Muse Spark, Muse Voice Transcribe and Muse Image once each and prints the time, tokens and
cost of every call; it costs a few cents. `--audio` takes any short WAV clip and can be left out. In the simulator, **L** shows every
call of your run with its model, provider, time and cost.

</details>

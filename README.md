<p align="center"><b>English</b> | <a href="README.es.md">Español</a></p>

<p align="center">
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
  <a href="RUN_AND_BUILD.md"><img src="docs/assets/tab-run-en.svg" alt="Run and build, for the judges"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-en.svg?v=2" width="100%" alt="ColonQuest, a personal-finance simulator that plays like a game, from Costa Rica to the world. The full content of this card is available as text in the section below.">
</p>

<details>
<summary><b>Read this page as text</b></summary>

## ColonQuest

**From Costa Rica to the World.** A personal-finance simulator that plays like a game: you live on a
Costa Rican minimum wage for three months, while people try to sell you things. Built by the
DoWhile Team for the Meta Global AI Developer Hackathon 2026 (Indie track). You decide, Muse Spark
narrates, the C# engine computes the exact colones, and you negotiate by voice. The AI tells the
story; the engine keeps the books.

### The problem

In Costa Rica 87% of adults have some kind of debt and spend on average 34% of their income paying
it, 58.3% for those who earn under ₡500 000 a month. 80.88% of the people surveyed by the Universidad
Nacional never received any financial education. Nobody teaches you to ask the right question while
a salesperson is smiling at you. That is the skill ColonQuest trains, against four real traps:

- **Installments:** just one payment a month, plus hidden insurance.
- **Minimum payment:** you pay a little and the debt keeps growing.
- **Gota a gota:** fast cash today, and collectors at your door. The police counted 2 930 cases in 2025.
- **Pyramid:** bring three friends and get rich. Promise.

Every figure and its source: [The situation in Costa Rica](COSTA_RICA.md).

### How a month plays

Your salary arrives with CCSS and income tax already deducted. You decide what to pay, save or
borrow. Muse Spark writes one event built for your situation and the C# engine applies it in
whole colones. When a seller shows up, you negotiate out loud: the real price, rate, insurance and
commission only come out if you ask.

### Play it in about three minutes

Choose **Guided simulation**, the first entry of the main menu. It is the real game on a short
stretch of the town, for three months, and you only make the decisions:

1. **Month 1.** You say how you get to the end of the month. Your salary lands by SINPE, Muse Wonder
   writes your year in the background, and the game master brings an event built for your situation.
2. **Month 2.** An arrow leads you to the store, where the seller keeps quiet about what you do not
   ask. Talk or type.
3. **Month 3.** What comes next depends on your money: short of it, the lender and then the
   collectors; with some to spare, a pyramid. You can ask them questions before you sign.

The panel on the right shows every real call as it happens, read back from the server: model,
provider, latency and cost. Press **L** to hide it. The run ends with a report drawn from the engine's
numbers: what you have, what you owe, your money month by month, your grades and a picture chosen by what happened in your run.
The objectives, the arrow and the map marks work the same in the full **Story mode**, where the people
on the street also talk to you through Muse Spark Open.

### Try every feature in two minutes

Choose **Feature demo** in the main menu to reach each feature on its own, with the same panel of
real calls.

| Station | Try this | Models |
| --- | --- | --- |
| Game master | Pick a player's situation, then a choice, and see the engine apply it | Muse Spark with tools |
| Negotiation | Talk or type to a seller and open the hidden sheet line by line | Muse Spark Open, Muse Voice Transcribe |
| Debts and traps | Talk to the collector, the lender and the pyramid promoter, and open what they hide | Muse Spark Open, Muse Voice Transcribe |
| Year plan and ending | Compare the plans for two profiles and read how a year ended | Muse Wonder |
| Engine and guards | Try to sneak in an amount out of range, see the live budgets | None, by design |
| Class Mode | Run a real test class of three simulated students, labelled as such | Muse Spark |

It needs internet, because the models run on the backend. A whole tour costs about $0.10.

### Judging criteria

| Criterion | Weight | What we built |
| --- | --- | --- |
| Impact | 30% | Sourced Costa Rican figures, the real minimum wage and rate cap in the engine, telemetry per decision, Class Mode for teachers |
| Meta tools | 25% | Five Meta models, one clear job each, plus tool-using agents |
| Demo | 25% | A guided three-month run and a feature demo in the game's menu; every model call has a timeout and a fallback |
| Technical depth | 20% | Validated contracts, an exact engine, more than 1 300 tests: the engine's NUnit and the server's pytest run in CI, the Unity suites locally |

### Meta models

| Model | Role |
| --- | --- |
| Muse Spark | Live game master: the monthly event, with tools; Class Mode reports |
| Muse Wonder | Plans your year in the background and writes the year-end summary |
| Muse Spark Open (Baseten) | The sellers, with commission tactics and memory of what you asked; the collectors, the lender, the pyramid promoter and the people of the town |
| Muse Voice Transcribe | You negotiate out loud; typing always works too |
| Muse Image | Portraits, event scenes and the picture of your ending, chosen by what happened in your run; pre-generated and without text |

Each role follows measured latency: only Spark answers inside the game's 8 s every time, so the
slow writer, Wonder, only runs where you are not waiting. See
[ADR 0004](docs/adr/0004-model-roles.md).

### Agents

Behind feature flags, see [ADR 0003](docs/adr/0003-agentic-game-master.md).

- **Game master:** calls read-only tools such as `simulate_purchase` and `project_debt`, so the
  consequences it narrates use real numbers.
- **Season planner:** sets the teaching arc of the year and brings back the concepts a player
  struggled with.
- **Seller:** pushes longer terms to earn more commission, but can only quote figures from its
  hidden sheet or its own tools.

### Architecture

A Unity 2D client (C# finance engine) talks JSON with a FastAPI backend (Pydantic v2 contracts, cost
guard, agents). The backend calls the Meta Model API, stores sessions and decisions in Postgres, and
runs the characters on Muse Spark Open in Baseten, falling back to Meta if it fails or runs out of its
own daily budget.

- No amount, rate or balance ever comes from a model.
- Money is an integer number of colones.
- Every model answer is validated, with a fallback.
- Every call goes through a daily cost guard.
- API keys live only in the backend.

### Accessibility

| Area | Status | What it means |
| --- | --- | --- |
| Voice is optional | Done | Every negotiation turn accepts typed text |
| One-click questions | Done | The key questions are buttons; typing or talking is optional |
| Plain language | Done | Short texts in plain English or everyday Costa Rican Spanish |
| No dead ends | Done | If a model fails, scripted content takes over |
| Readable page | Done | This text version, and the card stops moving under reduced motion |
| Keyboard play | In progress | Movement and every panel without a mouse |
| Not by color alone | In progress | Every color also has a label or an icon |
| Adjustable text | Planned | A larger font for panels and dialogue |

### Run it and build it

The game reads which backend to use from `colonquest.env`, and the backend reads its keys from
`server/.env`. Change them and build from there; the keys never go inside the executable. The full
guide: [Run and build](RUN_AND_BUILD.md).

```bash
cd server
cp .env.example .env
uv sync
uv run alembic upgrade head
uv run python -m app.dev
```

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

Run the checks with `uv run pytest` in `server/`.

Work is tracked in Jira (project `SCRUM`); read [CONTRIBUTING.md](CONTRIBUTING.md) and
[SECURITY.md](SECURITY.md). The cards are generated with `python scripts/build_readme_art.py`.

### Team

DoWhile Team: Keylor Pineda and Jefferson Abarca, built together for the Meta Global AI Developer
Hackathon 2026. From Costa Rica to the World. Released under the [MIT License](LICENSE).

</details>

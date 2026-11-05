<p align="center"><b>English</b> | <a href="README.es.md">Español</a></p>

<p align="center">
  <a href="https://github.com/keylorpineda/ColonQuest/releases/latest"><img src="docs/assets/tab-download-en.svg" alt="Download the Windows build"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
  <a href="RUN_AND_BUILD.md"><img src="docs/assets/tab-run-en.svg" alt="Run and build, for the judges"></a>
  <a href="BENCHMARKS.md"><img src="docs/assets/tab-benchmarks-en.svg" alt="Benchmarks and model decisions"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-en.svg?v=6" width="100%" alt="ColonQuest, a personal-finance simulator, from Costa Rica to the world. The full content of this card is available as text in the section below.">
</p>

<details>
<summary><b>Read this page as text</b></summary>

## ColonQuest

**From Costa Rica to the World.** A personal-finance simulator: you live on a
Costa Rican minimum wage for three months, while people try to sell you things. Built by the
DoWhile Team for the Meta Global AI Developer Hackathon 2026 (Indie track). You decide, Muse Spark
narrates, the C# engine computes the exact colones, and you negotiate by voice. The AI tells the
story; the engine keeps the books.

### The problem

| Figure | In Costa Rica | Source |
| --- | --- | --- |
| **34%** | of monthly income goes to paying debts, on average | [OCF, 2025](https://delfino.cr/2025/10/aunque-los-costarricenses-comprometen-en-promedio-menos-dinero-para-el-pago-de-deudas-el-nivel-de-endeudamiento-es-alto-para-buena-parte-de-la-poblacion) |
| **58.3%** | for people who earn under ₡500 000 (about $1 000) a month | Same survey |
| **80.88%** | of people surveyed never had any financial education | [Universidad Nacional, 2024](https://www.elfinancierocr.com/economia-y-politica/una-cartera-muy-estrecha-menos-del-10-de-las/22FABMYC6ZE47JR73C2UYYMBJU/story) |
| **2 930** | cases of street lenders counted by investigators in 2025 | [OIJ, 2026](https://semanariouniversidad.com/pais/oij-registra-mas-de-2-000-casos-vinculados-a-prestamos-gota-a-gota-en-lo-que-va-de-2026/) |

### Why financial education: the usury law and its gap

1. **People owed more than they could pay.** Until 2020 Costa Rica had no legal maximum on interest, and
   nothing stopped payroll deductions from leaving a worker below the minimum needed to live. That year,
   households put 52% of their income into paying debts ([OCF](https://delfino.cr/2025/10/aunque-los-costarricenses-comprometen-en-promedio-menos-dinero-para-el-pago-de-deudas-el-nivel-de-endeudamiento-es-alto-para-buena-parte-de-la-poblacion)).
2. **The usury law answered it.** Law 9859, against credit usury, came into force in June 2020. It sets a
   maximum yearly rate for every loan, card and microcredit, recalculated by the central bank every six
   months (36.48% in colones for the second half of 2026). Lenders must check the person's total debt at
   the regulator's credit bureau before lending, and payroll deductions can no longer touch the minimum
   wage that cannot be seized ([CRHoy](https://crhoy.com/nacionales/que-es-la-ley-de-usura-y-por-que-genera-debate-en-la-campana-presidencial/),
   [Delfino](https://delfino.cr/2020/09/tesoreria-nacional-no-admitira-deducciones-por-planilla-a-salarios-inferiores-a-199-760)).
   By 2025 the share of income going to debts was down to 34%.
3. **It also left people out.** With a cap, banks lend less to whoever looks risky. The OECD says the cap
   is causing financial exclusion that pushes people to informal credit, and microlenders estimate about
   60 000 people left out since 2020 ([El Observador](https://observador.cr/tasa-de-usura-esta-causando-exclusion-financiera-en-costa-rica-y-aumenta-los-creditos-informales-advierte-la-ocde/)).
   The debate is open: there were more credit cards in 2024 than in 2019, and no study proves the
   exclusion fed the street lenders ([El Financiero](https://www.elfinancierocr.com/finanzas/ley-de-usura-levanta-posiciones-encontradas/UBJQZ373BJGQDBISGMWT25SCGA/story/)).
4. **Gota a gota filled the gap, outside the law.** Street lenders charge up to 20% a week or a month,
   many times the legal cap, mixing usury, extortion and violence ([UN Costa Rica, 2025](https://observador.cr/onu-prestamos-gota-a-gota-combinan-usura-extorsion-y-violencia-en-costa-rica/)).
   Extortion complaints tied to informal loans rose from 47 in 2020 to 428 in 2024. Of those who went to
   one, 40% needed the money urgently and 32% assumed a bank would turn them down; only 14% had actually
   been turned down ([OIJ, in El Financiero](https://www.elfinancierocr.com/finanzas/ley-de-usura-levanta-posiciones-encontradas/UBJQZ373BJGQDBISGMWT25SCGA/story/)).
5. **A law can cap a rate; it cannot teach the question.** The legal market has a ceiling, the illegal one
   is a message away, and 8 in 10 people never had financial education. That is why the country passed a
   financial education law in 2025, and the gap ColonQuest trains for: knowing the total cost, spotting a
   rate above the cap, and saying no before signing.

Nobody teaches you to ask the right question while a salesperson is smiling at you. That is the skill
ColonQuest trains, against four real traps:

- **Installments:** just one payment a month, plus hidden insurance.
- **Minimum payment:** you pay a little and the debt keeps growing.
- **Gota a gota ("drop by drop"):** fast cash from a street lender today, and collectors at your door.
- **Pyramid:** bring three friends and get rich. Promise.

Plain explanations, local terms and how far to trust each number: [The situation in Costa Rica](COSTA_RICA.md).

### How a month works

Your salary arrives with CCSS and income tax already deducted. You decide what to pay, save or
borrow. Muse Spark writes one event built for your situation and the C# engine applies it in
whole colones. When a seller shows up, you negotiate out loud: the real price, rate, insurance and
commission only come out if you ask.

### Try it in about three minutes

It runs on your own computer with your own Meta key; there is no hosted service. [Download the Windows build](https://github.com/keylorpineda/ColonQuest/releases/latest),
unzip it, double-click `start_colonquest.bat` and paste your key when it asks. It installs what it needs, starts the
local backend and opens the simulator; the first start takes about a minute ([details](RUN_AND_BUILD.md#1-evaluate-it-with-your-own-key),
[questions](RUN_AND_BUILD.md#questions-a-judge-may-have)). Choose **Guided simulation**,
the first entry of the main menu. It is the real simulator on a short
stretch of the town, for three months, and you only make the decisions:

1. **Month 1.** You say how you get to the end of the month. Your salary lands by SINPE, Muse Wonder
   writes your year in the background, and the simulation director brings an event built for your situation.
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
| Simulation director | Pick a user's situation, then a choice, and see the engine apply it | Muse Spark with tools |
| Negotiation | Talk or type to a seller and open the hidden sheet line by line | Muse Spark Open, Muse Voice Transcribe |
| Debts and traps | Talk to the collector, the lender and the pyramid promoter, and open what they hide | Muse Spark Open, Muse Voice Transcribe |
| Year plan and ending | Compare the plans for two profiles and read how a year ended | Muse Wonder |
| Engine and guards | Try to sneak in an amount out of range, see the live budgets | None, by design |
| Class Mode | Run a real test class of three simulated students, labelled as such | Muse Spark |

It needs the local backend with your Meta key, because the models run there. A whole tour costs about $0.10.

### Judging criteria

| Criterion | Weight | What we built |
| --- | --- | --- |
| Impact | 30% | Sourced Costa Rican figures, the real minimum wage and the Law 9859 rate cap in the engine, telemetry per decision, Class Mode for teachers |
| Meta tools | 25% | Five Meta models, one clear job each, plus tool-using agents |
| Demo | 25% | A guided three-month run and a feature demo in the simulator's menu; every model call has a timeout and a fallback |
| Technical depth | 20% | Validated contracts, an exact engine, more than 1 300 tests: the engine's NUnit, the server's pytest and Unity's EditMode run in CI on every change |

### Categories we enter

| Category | What it asks for | Where it is in ColonQuest |
| --- | --- | --- |
| **Indie track** | A project judged on impact, use of Meta tools, demo and technical depth | The [judging criteria](#judging-criteria) above, and a guided three-month run you can try in three minutes |
| **Best Build on Meta Open Weights** | A central part of the project running on Muse Spark Open or Muse Glimmer | The heart of the simulator is talking to people who want your money, and every one of them runs on Muse Spark Open in Baseten: sellers, collectors, the lender, the pyramid promoter and the townspeople. Each call records its provider |
| **Best Agentic Use-Case** | Agents that decide and carry out tasks of several steps | Three agents: the simulation director calls four read-only tools before writing (up to 5 steps in 10 s), the season planner plans your year and adjusts it to your decisions, and the seller re-prices its offer with a tool to push the term that earns it more. See [Agents](#agents) |
| **Best Creative Build** | An original project made with Muse Image or Muse Wonder | An original Costa Rican town and cast. Muse Image draws the portraits, the scenes and ten different endings; Muse Wonder plans your year and writes how it ended |
| **Most Viral Project** and **Community Vote** | The public's choice | The demo clip on X and the votes in the public gallery |

### Meta models: who does what, and where it runs

Five Meta models, one job each. Four run on the **Meta Model API** (`https://api.meta.ai/v1`). The open
model, **Muse Spark Open, runs on Baseten** (`https://inference.baseten.co/v1`), and every character you
talk to lives there. No amount, rate or balance comes from any of them: the C# engine and the backend
compute every figure, and the models only choose how to tell it.

| Model and ID | Runs on | What it does in the simulator | When it is called | Measured time |
| --- | --- | --- | --- | --- |
| **Muse Spark** `muse-spark-1.3` | Meta Model API | The live simulation director: writes the event of each month for your situation, calling read-only tools first. Also writes the Class Mode report from SQL figures | Each month, while you wait | 1.4 to 4.4 s |
| **Muse Wonder** `muse-wonder-1.0` | Meta Model API | Plans your year (which concepts, in which order) and writes the story of your final report. Can also write extra fallback events offline | In the background after the first event, and at the end of the run; never on a live request | 5 to 17 s |
| **Muse Spark Open** `meta-models/muse-spark-open-1026` | **Baseten** | Every character you talk to: the sellers who hide the insurance and the commission until you ask, the collectors, the gota a gota lender, the pyramid promoter and the people of the town. The seller is an agent that re-prices its offer with a tool | Each turn of a conversation | 1.9 to 5.3 s |
| **Muse Voice Transcribe** `muse-voice-transcribe-1.0` | Meta Model API | Turns what you say into text, so you can negotiate out loud. Typing always works too | Each spoken turn | 0.7 to 1.5 s |
| **Muse Image** `muse-image-1.0` | Meta Model API | Portraits, event scenes and the ten pictures of the ending, chosen by the engine's numbers. No image has text or numbers | Pre-generated once and shipped in the zip; never while you play | 14 to 17 s each |

If Baseten fails, takes more than 5 s or reaches its own daily budget, the characters switch to Muse
Spark on the Meta API; if a model fails altogether, scripted content answers. The panel on the right of
the simulator and the `model_calls` table show which model and which provider answered every call, with
its time and cost. Each role follows measured latency: only Spark answers inside the simulator's 8 s
every time, so Wonder only runs where you are not waiting. Every measurement, the cost of a run and what we
turned down: **[Benchmarks and model decisions](BENCHMARKS.md)**.

### Agents

On by default in the local backend, each behind its own switch in `server/.env`; if an agent fails, the single-call version answers, and if it takes too long, a scripted event does. See [ADR 0003](docs/adr/0003-agentic-game-master.md).

| Agent | Model and provider | What it decides | Its tools |
| --- | --- | --- | --- |
| **Simulation director** | Muse Spark, Meta API | Which event fits your month and how to tell it, after looking at your history and the real cost of each option | `get_player_history`, `get_concepts_not_yet_taught`, `simulate_purchase`, `project_debt` |
| **Season planner** | Muse Wonder, Meta API, in the background | The teaching arc of your year: which concept comes when, bringing back the ones you struggled with | Your profile and your decisions so far |
| **Seller** | Muse Spark Open, Baseten | Which term to push so it earns more commission, while answering honestly when you ask | `quote_term`, which re-prices the offer at another term with the engine's formula |

Tools only read data or compute; none of them writes your state. Every amount an agent says comes
from the hidden sheet or a tool, and the event still goes through the same range check as any other.

### Architecture

A Unity 2D client (C# finance engine) talks JSON with a FastAPI backend (Pydantic v2 contracts, cost
guard, agents) that runs on your computer. It calls the Meta Model API, stores sessions and decisions in SQLite or Postgres, and
runs the characters on Muse Spark Open in Baseten, falling back to Meta if it fails or runs out of its
own daily budget. The diagram, the path of one event, the guardrails and the cost of a run (about $0.06)
are in [Architecture](docs/architecture.md).

- No amount, rate or balance ever comes from a model.
- Money is an integer number of colones.
- Every model answer is validated, with a fallback.
- Every call goes through a daily cost guard.
- API keys live only in the backend.

### Run it and build it

Everything runs on your computer with your own keys. `start_colonquest.bat` installs uv if it is missing, asks for
your Meta key, starts the local backend (SQLite, no database to install) and opens the simulator. The keys stay in
`server/.env` and never go inside the executable. On other systems, `python scripts/run_local.py` does the same.
The full guide: [Run and build](RUN_AND_BUILD.md).

```bash
start_colonquest.bat
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

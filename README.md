<p align="center"><b>English</b> | <a href="README.es.md">Español</a></p>

<p align="center">
  <a href="https://github.com/keylorpineda/ColonQuest/releases/latest"><img src="docs/assets/tab-download-en.svg" alt="Download the Windows build"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
  <a href="RUN_AND_BUILD.md"><img src="docs/assets/tab-run-en.svg" alt="Run and build, for the judges"></a>
  <a href="BENCHMARKS.md"><img src="docs/assets/tab-benchmarks-en.svg" alt="Benchmarks and model decisions"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-en.svg?v=10" width="100%" alt="ColonQuest, a personal-finance simulator, from Costa Rica to the world. The full content of this card is available as text in the section below.">
</p>

<details>
<summary><b>Read this page as text</b></summary>

## ColonQuest

**From Costa Rica to the World.** A personal-finance simulator: you live on a
Costa Rican minimum wage for three months, while people try to sell you things. Built by the
DoWhile Team for the Meta Global AI Developer Hackathon 2026, in the Indie Developer Track, for the problem statement
Build for Your World. You decide, Muse Spark
narrates, the C# engine computes the exact colones, and you negotiate by voice. The AI tells the
story; the engine keeps the books.

### Indie Developer Track: Build for Your World

We submit to the **Indie Developer Track** under the problem statement **Build for Your World**. Our world is Costa
Rica, and we chose the problem we see around us every month: people sign their first card, their first installment
plan or their first loan without ever having practiced, and the bill comes later.

- **Why this problem.** A third of monthly income goes to paying debts, and more than half for those who earn under
  $1 000. Eight in ten people never had any financial education. The usury law of 2020 capped the legal rates, but
  street lenders (gota a gota) charge many times that cap and reach people by WhatsApp, Facebook and TikTok. The
  figures and their sources are in [The situation in Costa Rica](COSTA_RICA.md).
- **Why a simulator.** Nobody can practice a bad loan in real life without paying for it. Here a mistake costs
  nothing, and its consequence shows up in the next months instead of being explained in a lecture.
- **How ColonQuest addresses it.** You live on the real 2026 minimum wage, with the real CCSS and income tax, a card
  under the real legal cap of Law 9859, and the four traps people really meet: installments with hidden costs,
  the minimum payment, the gota a gota lender and the pyramid. The skill it trains is the one that protects you:
  asking for the total cost, the rate and the insurance before you sign. The server records which of those
  questions you asked, so a teacher can see it in Class Mode.
- **Made from here.** Costa Rican Spanish and English, colones, SINPE, the soda, the INA and the UNA, the marchamo
  and the aguinaldo. It runs on any school computer with Windows and the school's own key, with no service to pay.
- **From Costa Rica to the World.** The same problem exists wherever credit is easy and financial education is
  scarce. The rules live in the finance engine, not in the models, so another country would change its salary, its
  deductions and its legal cap, and keep the rest.

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

### Try it

It runs on your own computer with your own Meta key; there is no hosted service. [Download the Windows build](https://github.com/keylorpineda/ColonQuest/releases/latest),
unzip it, double-click `start_colonquest.bat` and paste your key when it asks. It installs what it needs, starts the
local backend and opens the simulator; the first start takes about a minute ([details](RUN_AND_BUILD.md#1-evaluate-it-with-your-own-key),
[questions](RUN_AND_BUILD.md#questions-a-judge-may-have)).

### Three ways in, and why each one exists

The main menu offers three ways into the same simulator. They share the finance engine, the models and the backend;
what changes is how much of the town you walk and how much time you need.

| Menu entry | What it is | Time | Why we built it |
| --- | --- | --- | --- |
| **Guided simulation** | The real simulator, three months, on a short stretch of the town. You only make the decisions | About 3 minutes | So a judge or a student sees a whole run, from the first salary to the final report, without learning the town first |
| **Feature demo** | Each AI feature on its own screen, one click away, with what the model did beside it | About 2 minutes | So a judge can check every model, agent and guardrail directly, without having to reach it inside a run |
| **Story mode** | The full open world, with three save slots | As long as you like | The way a student plays it for real: work shifts, rent, a car, a farm, the whole town |

If you only have a few minutes, start with **Guided simulation** and then open **Feature demo**.

### Guided simulation: a whole run in three minutes

You choose how you get to the end of the month, and the engine sets your starting money. Both profiles earn the same
salary: the 2026 minimum wage after CCSS and income tax.

| Profile | Cash | Savings | Card balance |
| --- | --- | --- | --- |
| "I barely make it, with a loaded card" | ₡40 000 | ₡0 | ₡240 000 |
| "I have some savings" | ₡120 000 | ₡400 000 | ₡40 000 |

1. **Month 1.** Your salary lands by SINPE. Muse Wonder starts writing your year in the background, and the simulation
   director brings an event built for your situation, with options whose cost the engine computes.
2. **Month 2.** An arrow leads you to the store. The seller quotes a low monthly payment and keeps quiet about the
   insurance, the commission and the total until you ask. Talk or type, or press the question buttons.
3. **Month 3.** What comes next depends on your money after two months of your own decisions, not on the profile you
   chose: short of a salary, the gota a gota lender, and the collectors if you sign; with money to spare, a friend with
   a pyramid. You can ask them anything before you decide.

The run ends with a report drawn from the engine's numbers: what you have, what you owe, your money month by month,
your grades, the questions you asked and a picture chosen by what happened.

To keep it to three minutes, the guided run stops the clock, takes out the traffic and the police, opens only the doors
on its route and never touches your saved games. Everything else is the same as in story mode.

**What to look for:** the seller never lies about a figure but hides what you do not ask; the total cost appears only
after the right question; the engine refuses an amount out of range; and the panel on the right shows every real call
as it happens, read back from the server, with its model, its provider (Meta API or Baseten), its time and its cost.
Press **L** to hide it.

### Feature demo: every AI piece, one click away

Each station sends a real request to the backend and shows the answer next to why that model has that job.

| Station | What you do | What it shows | Model and provider |
| --- | --- | --- | --- |
| Simulation director | Pick a user's situation, then a choice | The event the model writes, the tools it called and how the engine applies the choice | Muse Spark with tools, Meta API |
| Negotiation | Talk or type to a seller, then open the hidden sheet | The seller only reveals the insurance and the commission when asked; the sheet opens line by line | Muse Spark Open on Baseten, Muse Voice Transcribe |
| Debts and traps | Talk to the collector, the lender and the pyramid promoter | Each has its own personality and says the uncomfortable part only if you ask | Muse Spark Open on Baseten, Muse Voice Transcribe |
| Year plan and ending | Compare the plans for two profiles, read how a year ended | The plan Muse Wonder writes in the background, and the story of an ending | Muse Wonder, Meta API |
| Engine and guards | Try to sneak in an amount out of range | The engine rejects or clamps it, and the live daily budgets | None, on purpose |
| Class Mode | Create a real test class of three simulated students, labelled as such | The teacher's panel: figures from SQL, explained in plain words by the model | Muse Spark, Meta API |

It needs the local backend with your Meta key, because the models run there. A whole tour costs about $0.10.

### Story mode: the whole town

The free run: three months of four days each, with work shifts at the soda, rent, bills, a bank, a cooperative, a
farm, a car agency, a hospital and a police station. The people on the street also talk to you through Muse Spark
Open. The same objectives, arrow and map marks of the guided run tell you what comes next.

### Everything the simulator has

ColonQuest is a gamified simulation: you live a young Costa Rican's first months on their own, in a town you walk
around, and every colón follows the real rules. This is what is inside.

**Your money, by the real rules.** The C# finance engine counts in whole colones and applies:

- The 2026 minimum wage, with CCSS (10.83%) and income tax taken off, paid by SINPE.
- Cash, a savings account at the bank and an emergency fund.
- A credit card at 3% a month, with its minimum payment, its statement and interest on the balance.
- Installment purchases with the insurance and the commission the seller does not mention.
- Gota a gota loans and their collectors, and a pyramid that pays the first ones with the money of the last.
- Electricity, water, internet and phone bills that pile up if you skip them, until the power is cut.
- Rent or a mortgage, a car with a down payment and installments, a farm to invest in coffee or cattle, and furniture
  bought cash or in installments.
- The December bonus (aguinaldo) and the car tax (marchamo), for runs that reach December.

**A town to live in.** About 17 places, each one a money decision: your mother's house, your rented room, Soda El
Muelle where you work, the bank with its cash machines and deposit window, the cooperative, the farm, the store, the
car agency, the real estate agency, the hospital, the police station, the corner shop, Don Rafa's garage, the INA and
UNA courses, a quick-loans stall and an "investment" stand. Each day stands for a week, from 6:00 to 22:00, with
traffic, pedestrians, a dog, boats, seagulls, the beach, nightfall with street lights and sound made in code.

**Work and study.** Your first job has a time clock: arriving late gets a warning and then docked pay. In the
afternoons you can take courses at the INA or the UNA to move up. You can also cook at home to save a little.

**A phone (P).** SINPE notices, your bank, your bills, goals (an emergency fund, paying down the card, furnishing
your room, saving, buying a motorbike), a search engine that explains each money idea in plain words, a loan
calculator, a block puzzle and a casino with real stakes from your cash.

**People who want your money.** Sellers who hide the costs until you ask, collectors at the curb, a gota a gota lender whose
rate, once you ask, is many times the legal cap, a friend with a sure business, neighbors who chat, and a stranger who
asks you to send money. They all speak through Muse Spark Open, by voice or by text, and none of them can invent a
figure.

**Consequences.** Walk into a stranger's house and the owner calls the police: one to three days in a cell, or bail.
A car can run you over: the hospital and your health insurance decide what it costs. Skip the bills and the power
goes out. Sign with the lender and the collectors come.

**Guidance, so nobody gets lost.** An opening scene (you turn 18, your mother gives you a present, the phone brings
your first job), a task list (Tab), objectives with an arrow and map marks, a minimap and a full map (M), subtitles,
a HUD you can hide (H), the calls panel (L), pause, three save slots, English or Spanish, and sound settings.

**The end of a run.** A report from the engine's numbers: what you have and what you owe, your money month by month,
grades from A to D in budget, emergency fund, debts, installments and investments, the key questions you asked, what
you bought, and one of ten pictures chosen by how it went.

**For teachers.** Class Mode: the teacher gives the group a code and sees, per concept and with no personal data, who
chose the risky option, who caught the trap in the negotiation and the average debt at the end. The figures come from
SQL; Muse Spark only explains them.

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
| **Indie Developer Track**, problem statement **Build for Your World** | A project for your own world, judged on impact, use of Meta tools, demo and technical depth | Our world is Costa Rica: [why we chose it](#indie-developer-track-build-for-your-world), the [judging criteria](#judging-criteria) above, and a guided run you can try in three minutes |
| **Best Build on Meta Open Weights** | A central part of the project running on Muse Spark Open or Muse Glimmer | The heart of the simulator is talking to people who want your money, and every one of them runs on Muse Spark Open in Baseten: sellers, collectors, the lender, the pyramid promoter and the townspeople. Each call records its provider |
| **Best Agentic Use-Case** | Agents that decide and carry out tasks of several steps | Three agents: the simulation director calls four read-only tools before writing (up to 5 steps in 10 s), the season planner plans your year and adjusts it to your decisions, and the seller re-prices its offer with a tool to push the term that earns it more. See [Agents](#agents) |
| **Best Creative Build** | An original project made with Muse Image or Muse Wonder | An original Costa Rican town and cast. Muse Image draws the portraits, the scenes and ten different endings; Muse Wonder plans your year and writes how it ended |
| **Community Vote** | The public's choice | The votes in the hackathon's public gallery |

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

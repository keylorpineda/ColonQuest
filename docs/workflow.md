# Development workflow

## Tools

| Tool | Role |
| --- | --- |
| Jira (project `CQ`) | Backlog, sprints, ticket status |
| GitHub | Code, pull requests, CI |
| GitHub for Jira app | Shows branches, commits, PRs and builds on each Jira ticket |
| Atlassian MCP (`.mcp.json`) | Lets Claude Code read and create Jira tickets from the repo |

## Linking GitHub and Jira

1. In Jira: **Apps → Explore more apps → GitHub for Jira** → install.
2. Connect the GitHub account `keylorpineda` and select the `ColonQuest` repository.
3. Any branch, commit or PR that contains a key like `CQ-12` now appears in that ticket's
   **Development** panel.

## Sprints

Submission deadline: **Monday, November 2, 2026**. Everything must be finished by Nov 1–2.

| Sprint | Dates (2026) | Goal |
| --- | --- | --- |
| Sprint 1: Foundations | Oct 26 – Oct 27 | Every Meta model answers from the backend; the player walks between stations |
| Sprint 2: Playable Month | Oct 28 – Oct 29 | One full month playable end to end |
| Sprint 3: Negotiation & Deploy | Oct 30 – Oct 31 | Negotiation works in the deployed web build (voice, or text as fallback) |
| Sprint 4: Pilot & Submission | Nov 1 – Nov 2 | Pilot with students, video, README and gallery submission |

Decision gates: open-weights provider on **Oct 28** (only if credits are approved). The agentic
game master stays in the backlog and only starts if the playable month is done by **Oct 29**.

If something slips, cut in this order: agentic game master, open-weights provider, Class Mode,
real-time images, web voice (text stays). The playable month and text negotiation are never cut.

## Continuous integration

Two workflows run on every pull request into `develop` or `main`. Branch protection requires
both checks and an up-to-date branch, and pull request runs test the merge commit, so CI always
validates exactly what will land.

| Required check | Workflow | What it does |
| --- | --- | --- |
| `CI passed` | `.github/workflows/ci.yml` | Gate job over every CI job below |
| `Title and branch reference a Jira ticket` | `.github/workflows/pr-title.yml` | Enforces `type(scope): summary [CQ-n]` and the branch rules |

Jobs in `ci.yml`, run in parallel and only when their area changed:

| Job | Runs when | Notes |
| --- | --- | --- |
| Server / lint | `server/` or `shared/` changed | Ruff lint and format check |
| Server / type-check | `server/` or `shared/` changed | `mypy --strict` |
| Server / test | `server/` or `shared/` changed | pytest with coverage; any warning fails the test |
| Unity / EditMode tests | `unity/` changed and the Unity project exists | Needs `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` secrets |
| Workflows / actionlint | `.github/workflows/` changed | Lints the workflow files |

Speed techniques:

- Path filtering: unaffected jobs are skipped, and the gate treats them as passed.
- Caching: uv packages and the Python install (`setup-uv`), Ruff and mypy caches, and the Unity `Library` folder.
- `uv sync --frozen` with a committed `uv.lock`: no dependency resolution in CI.
- Superseded runs on the same PR are cancelled; runs on `main` and `develop` never are.
- Every job has a short timeout so a hung run cannot block merges.

## Definition of Done

- Code merged into `develop` through a reviewed PR with green CI.
- Tests added for logic (finance engine, validation, cost guard).
- No secrets committed; money handled as integers.
- Jira ticket moved to **Done**.

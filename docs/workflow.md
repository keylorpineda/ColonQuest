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

## Definition of Done

- Code merged into `develop` through a reviewed PR with green CI.
- Tests added for logic (finance engine, validation, cost guard).
- No secrets committed; money handled as integers.
- Jira ticket moved to **Done**.

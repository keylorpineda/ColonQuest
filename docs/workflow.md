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

| Sprint | Dates (2026) | Goal |
| --- | --- | --- |
| Sprint 1 — Foundations | Oct 26 – Oct 27 | Every Meta model answers from the backend; the player walks between stations |
| Sprint 2 — Playable Month | Oct 28 – Oct 30 | One full month playable end to end |
| Sprint 3 — Negotiation | Oct 30 – Oct 31 | Negotiation works in the web build (voice, or text as fallback) |
| Sprint 4 — Pilot & Deploy | Nov 1 – Nov 2 | Deployed build tested with students |
| Sprint 5 — Submission & Campaign | Nov 3 – Nov 8 | Submitted before Nov 4, 8:00 p.m. CR; demo stays up; X campaign |

## Definition of Done

- Code merged into `develop` through a reviewed PR with green CI.
- Tests added for logic (finance engine, validation, cost guard).
- No secrets committed; money handled as integers.
- Jira ticket moved to **Done**.

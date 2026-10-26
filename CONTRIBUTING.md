# Contributing to Colón Quest

Thanks for helping! This guide keeps the repository tidy and every change traceable to a Jira ticket.

## Branching model

| Branch | Purpose | Receives changes from |
| --- | --- | --- |
| `main` | Stable, demo-ready releases only. Every merge is tagged (`v0.1.0`, ...) | `develop` (release PR) or `hotfix/*` |
| `develop` | Integration branch. All day-to-day work lands here | Feature branches (`feat/*`, `fix/*`, `chore/*`, ...) |
| `<type>/SCRUM-<n>-...` | One branch per Jira ticket, always created from `develop` | — |
| `hotfix/SCRUM-<n>-...` | Urgent fix for the live demo, created from `main` | — |

Nobody pushes directly to `main` or `develop`; everything goes through a pull request.

## Workflow at a glance

1. Pick a ticket in Jira (project key **`SCRUM`**) and move it to **In Progress**.
2. Update `develop` and branch from it:
   ```bash
   git switch develop && git pull
   git switch -c feat/SCRUM-12-game-master-endpoint
   ```
3. Commit using [Conventional Commits](https://www.conventionalcommits.org/) with the ticket key.
4. Open a small pull request **into `develop`** using the PR template. CI must pass.
5. Squash-merge. Jira picks up the branch, commits and PR automatically through the GitHub for Jira app.

### Releases

When `develop` is stable and demo-ready, open a PR from `develop` into `main` titled
`release: vX.Y.Z`, merge it with a merge commit (not squash), and tag `main`:

```bash
git switch main && git pull
git tag -a v0.1.0 -m "v0.1.0" && git push origin v0.1.0
```

### Hotfixes

Branch `hotfix/SCRUM-<n>-...` from `main`, open the PR into `main`, then merge `main` back into `develop`.

## Branch names

```
<type>/SCRUM-<number>-<short-kebab-description>
```

Examples: `feat/SCRUM-12-game-master-endpoint`, `fix/SCRUM-40-negative-cash-balance`, `chore/SCRUM-1-repository-setup`.

## Commit messages

```
<type>(<scope>): <imperative summary> [SCRUM-<number>]

<optional body: what and why, wrapped at 72 characters>

<optional footer, e.g. BREAKING CHANGE: ...>
```

- **Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- **Scopes:** `server`, `unity`, `engine`, `shared`, `scripts`, `docs`, `ci`, `repo`.
- Summary in English, imperative mood, lowercase, no trailing period, max 72 characters.

Example:

```
feat(server): add health endpoint with remaining budget [SCRUM-7]
```

Enable the local template once per clone:

```bash
git config commit.template .gitmessage
```

### Jira smart commits (optional)

You can transition or comment on a ticket from a commit footer:

```
SCRUM-7 #comment Health check now reports the daily budget
SCRUM-7 #done
```

## Pull requests

- Title follows the commit format: `feat(server): add health endpoint [SCRUM-7]`. CI checks it.
- Feature PRs target `develop`; only release and hotfix PRs target `main`.
- Keep PRs small and focused on a single ticket.
- Fill in every section of the template.
- Changes to the API contract in `shared/` must be announced to the other teammate.

## Code standards

- **All code in English** (names, comments, docs). In-game text in Costa Rican Spanish.
- **Money is always an integer** (colones). Never use `float` for amounts.
- **API keys live only in backend environment variables.** Never in Unity, never in the repository.
- Every model response is validated against a Pydantic schema, with a predefined fallback.

### Backend (`server/`)

- Python 3.12, FastAPI, Pydantic v2, managed with `uv`.
- `ruff check`, `ruff format`, `mypy --strict` and `pytest` must pass.

### Unity (`unity/`)

- C# naming follows the [.NET conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- The finance engine is a pure C# class with EditMode tests.

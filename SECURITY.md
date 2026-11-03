<p align="center">
  <a href="README.md"><img src="docs/assets/tab-readme-en.svg" alt="README"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
  <a href="RUN_AND_BUILD.md"><img src="docs/assets/tab-run-en.svg" alt="Run and build, for the judges"></a>
</p>

<p align="center">
  <img src="docs/assets/security-en.svg" width="100%" alt="How to report a vulnerability in ColonQuest and how its secrets are handled. The full content of this card is available as text in the section below.">
</p>

<details open>
<summary><b>Read this page as text</b></summary>

# Security Policy

## Reporting a vulnerability

Please **do not** open a public issue for security problems. Use [GitHub private vulnerability reporting](https://github.com/keylorpineda/ColonQuest/security/advisories/new) or email **keylorpineda729@gmail.com**.

We aim to acknowledge reports within 48 hours during the hackathon.

## Secrets

- API keys and database URLs are read from environment variables on the backend only.
- `.env` files are ignored by git. Use `server/.env.example` as the reference.
- If a secret is ever committed, rotate it immediately; removing it from history is not enough.

</details>

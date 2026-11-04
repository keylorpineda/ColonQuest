# Scripts

One-off and maintenance scripts, e.g. `pregenerate_images.py` and test class seeding.
Scripts read secrets from environment variables, never from arguments or files in the repo.

- `build_readme_art.py`: regenerates the cards of the README and the docs (`docs/assets/*.svg`).
- `run_tests.py`: runs only the test suites a change can affect, and skips those that already passed as they are.
- `run_local.py`: asks for your keys, starts the backend on this computer (SQLite, no hosted service) and opens the
  simulator. `start_colonquest.bat` at the root runs it on Windows, installing uv first if it is missing.
- `build_game.py`: builds the Windows simulator and zips it with the backend and the launcher in `dist/`.

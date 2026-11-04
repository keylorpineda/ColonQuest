# Scripts

One-off and maintenance scripts, e.g. `pregenerate_images.py` and test class seeding.
Scripts read secrets from environment variables, never from arguments or files in the repo.

- `build_readme_art.py`: regenerates the cards of the README and the docs (`docs/assets/*.svg`).
- `run_tests.py`: runs only the test suites a change can affect, and skips those that already passed as they are.
- `run_local.py`: starts the backend on this computer, with SQLite and no hosted service.
- `build_game.py`: builds the Windows simulator from `colonquest.env` and zips it in `dist/`.

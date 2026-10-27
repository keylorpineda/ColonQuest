# Scripts

One-off and maintenance scripts, e.g. `pregenerate_images.py` and test class seeding.
Scripts read secrets from environment variables, never from arguments or files in the repo.

- `build_readme_art.py`: regenerates the README card (`docs/assets/readme-en.svg` and `readme-es.svg`).
- `run_dev_server.py`: starts the backend for local development on Windows, where plain `uvicorn` cannot talk to the database (see `server/README.md`).

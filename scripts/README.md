# Scripts

One-off and maintenance scripts, e.g. `pregenerate_images.py` and test class seeding.
Scripts read secrets from environment variables, never from arguments or files in the repo.

- `build_readme_art.py`: regenerates the cards of the README and the docs (`docs/assets/*.svg`).
- `build_game.py`: builds the Windows game from `colonquest.env` and zips it in `dist/`.

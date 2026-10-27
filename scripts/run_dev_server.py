"""Run the backend for local development, on Windows too.

Usage (from the repository root, with the server dependencies installed):
    cd server && uv run python ../scripts/run_dev_server.py            # http://127.0.0.1:8000
    cd server && uv run python ../scripts/run_dev_server.py --port 8010

Why this exists: psycopg 3 cannot run in async mode on the Windows default event loop
(ProactorEventLoop), and ``uvicorn app.main:app`` picks that loop there, so every database call
fails and /api/health reports ``database: unavailable``. This script starts the same app on the
selector loop. On Linux and macOS, and on Render, plain ``uvicorn`` works and this is not needed.

It reads ``server/.env`` as usual. There is no auto reload: restart it after changing code.
"""

import argparse
import asyncio
import os
import sys
from pathlib import Path

SERVER_DIR = Path(__file__).resolve().parent.parent / "server"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    # The app package lives in server/, and settings read server/.env from the working directory.
    sys.path.insert(0, str(SERVER_DIR))

    os.chdir(SERVER_DIR)

    import uvicorn

    config = uvicorn.Config("app.main:app", host=args.host, port=args.port, loop="none")
    server = uvicorn.Server(config)
    asyncio.run(server.serve(), loop_factory=asyncio.SelectorEventLoop)


if __name__ == "__main__":
    main()

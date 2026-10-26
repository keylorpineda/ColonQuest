"""Local development server that also works on Windows.

Async psycopg cannot run on Windows' default ProactorEventLoop, so this entry point starts
uvicorn on a SelectorEventLoop. Production (Linux on Render) runs plain uvicorn.

    uv run python -m app.dev
"""

import asyncio
import os

import uvicorn


def main() -> None:
    config = uvicorn.Config(
        "app.main:app",
        host=os.environ.get("HOST", "127.0.0.1"),
        port=int(os.environ.get("PORT", "8000")),
        log_level="info",
    )
    server = uvicorn.Server(config)
    asyncio.run(server.serve(), loop_factory=asyncio.SelectorEventLoop)


if __name__ == "__main__":
    main()

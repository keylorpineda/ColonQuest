"""Start the ColonQuest backend on this computer: no hosted service, no database to install.

    python scripts/run_local.py

Creates server/.env from its example the first time, installs the dependencies with uv and serves
http://localhost:8000/api. Data stays in a local SQLite file. Without MODEL_API_KEY in server/.env
the simulator runs on its scripted content; with it, the live models answer.
"""

import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "server"
ENV_FILE = SERVER / ".env"
ENV_EXAMPLE = SERVER / ".env.example"
EXE = ROOT / "dist" / "ColonQuest-Windows" / "ColonQuest.exe"
PORT = os.environ.get("PORT", "8000")


def read_value(name: str) -> str:
    for line in ENV_FILE.read_text(encoding="utf-8").splitlines():
        if line.strip().startswith(f"{name}="):
            return line.split("=", 1)[1].strip()
    return ""


def main() -> int:
    uv = shutil.which("uv")
    if uv is None:
        print("uv is not installed. Install it with: python -m pip install uv")
        return 1

    if not ENV_FILE.exists():
        shutil.copyfile(ENV_EXAMPLE, ENV_FILE)
        print("Created server/.env. Put your keys there to turn on the live AI.")

    if subprocess.run([uv, "sync", "--no-dev"], cwd=SERVER, check=False).returncode != 0:
        print("uv could not install the dependencies.")
        return 1

    if read_value("DATABASE_URL").startswith("postgres"):
        migrate = [uv, "run", "--no-dev", "alembic", "upgrade", "head"]
        if subprocess.run(migrate, cwd=SERVER, check=False).returncode != 0:
            print("The migrations failed: check DATABASE_URL in server/.env.")
            return 1

    mode = "live AI" if read_value("MODEL_API_KEY") else "scripted content (no MODEL_API_KEY)"
    print(f"\nColonQuest backend: http://localhost:{PORT}/api ({mode})")
    print("Keep this window open and start ColonQuest.exe. Ctrl+C stops the server.\n")
    if EXE.exists() and "--no-open" not in sys.argv:
        subprocess.Popen([str(EXE)], cwd=EXE.parent)

    try:
        command = [uv, "run", "--no-dev", "python", "-m", "app.dev"]
        return subprocess.run(command, cwd=SERVER, check=False).returncode
    except KeyboardInterrupt:
        return 0


if __name__ == "__main__":
    sys.exit(main())

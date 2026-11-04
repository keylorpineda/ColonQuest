"""Start the ColonQuest backend on this computer and open the simulator: no hosted service, no database.

    start_colonquest.bat              (Windows: installs uv if it is missing, then runs this)
    python scripts/run_local.py       (any system with Python 3.12 and uv)
    python scripts/run_local.py --no-open --no-prompt

The first run creates server/.env from its example and asks for your Meta Model API key (and,
optionally, a Baseten key). It checks the key, installs the dependencies with uv, starts
http://localhost:8000/api, waits until it answers and then opens ColonQuest.exe. Closing the
simulator stops the backend. Data stays in a local SQLite file. Without a key the simulator runs
on its scripted content. Standard library only, so uv can run it before anything is installed.
"""

import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "server"
ENV_FILE = SERVER / ".env"
ENV_EXAMPLE = SERVER / ".env.example"
# The release zip keeps the build in game/; a build made from the repository lands in dist/.
EXE_CANDIDATES = (
    ROOT / "game" / "ColonQuest.exe",
    ROOT / "dist" / "ColonQuest-Windows" / "ColonQuest.exe",
)
PORT = os.environ.get("PORT", "8000")
API_URL = f"http://localhost:{PORT}/api"
READY_TIMEOUT_S = 120


def find_uv() -> str | None:
    home_uv = Path.home() / ".local" / "bin" / ("uv.exe" if os.name == "nt" else "uv")
    for candidate in (os.environ.get("UV"), "uv", str(home_uv)):
        if candidate and (found := shutil.which(candidate)):
            return found
    return None


def read_value(name: str) -> str:
    for line in ENV_FILE.read_text(encoding="utf-8").splitlines():
        if line.strip().startswith(f"{name}="):
            return line.split("=", 1)[1].strip()
    return ""


def write_value(name: str, value: str) -> None:
    lines = ENV_FILE.read_text(encoding="utf-8").splitlines()
    for index, line in enumerate(lines):
        if line.strip().startswith(f"{name}="):
            lines[index] = f"{name}={value}"
            break
    else:
        lines.append(f"{name}={value}")
    ENV_FILE.write_text("\n".join(lines) + "\n", encoding="utf-8")


def list_models(base_url: str, key: str) -> list[str] | None:
    """The model ids a key can use, or None if the provider rejected it. Free: no tokens are used."""
    request = urllib.request.Request(
        base_url.rstrip("/") + "/models", headers={"Authorization": f"Bearer {key}"}
    )
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            return [model["id"] for model in json.load(response).get("data", [])]
    except urllib.error.HTTPError as error:
        return None if error.code in (401, 403) else []
    except (OSError, ValueError):
        return []  # offline or an unexpected answer: the backend reports it on the first call


def check_meta_key() -> None:
    """Check the Meta key for free; a rejected one is asked for again, or cleared if skipped."""
    tries = 3
    while True:
        key = read_value("MODEL_API_KEY")
        if not key:
            return
        models = list_models(read_value("META_BASE_URL") or "https://api.meta.ai/v1", key)
        if models is not None:
            break
        tries -= 1
        print("Meta rejected that key.")
        if tries == 0 or "--no-prompt" in sys.argv or not sys.stdin.isatty():
            print("The simulator will use its scripted content. Fix MODEL_API_KEY in server/.env.")
            return
        write_value("MODEL_API_KEY", input("Paste it again (Enter to skip): ").strip())
    wanted = {read_value(name) for name in ("GAME_MASTER_MODEL", "SUMMARY_MODEL", "IMAGE_MODEL")}
    missing = sorted(model for model in wanted if model and models and model not in models)
    if missing:
        print(f"Your Meta key does not list {', '.join(missing)}; those calls fall back to scripted content.")
    else:
        print("Meta key accepted.")


def ask_for_keys() -> None:
    """Ask once, in the console, for the keys that turn on the live models. Enter skips each one."""
    if "--no-prompt" in sys.argv or not sys.stdin.isatty() or read_value("MODEL_API_KEY"):
        return
    print(
        "\nColonQuest uses your own keys; they are saved only in server/.env on this computer.\n"
        "Press Enter to skip a key. Without the Meta key the simulator runs on scripted content."
    )
    meta = input("Meta Model API key (https://dev.meta.ai): ").strip()
    if not meta:
        return
    write_value("MODEL_API_KEY", meta)
    if not read_value("OPEN_WEIGHTS_API_KEY"):
        baseten = input("Baseten key for Muse Spark Open (optional): ").strip()
        if baseten:
            write_value("OPEN_WEIGHTS_API_KEY", baseten)


def wait_until_ready(server: subprocess.Popen[bytes]) -> bool:
    deadline = time.monotonic() + READY_TIMEOUT_S
    while time.monotonic() < deadline:
        if server.poll() is not None:
            return False
        try:
            with urllib.request.urlopen(f"http://127.0.0.1:{PORT}/api/ping", timeout=2):
                return True
        except OSError:
            time.sleep(0.5)
    return False


def stop(server: subprocess.Popen[bytes]) -> None:
    """Stop the backend and its children: on Windows the venv's python.exe starts the real one."""
    if server.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(["taskkill", "/T", "/F", "/PID", str(server.pid)], capture_output=True, check=False)
    else:
        server.terminate()
    try:
        server.wait(timeout=10)
    except subprocess.TimeoutExpired:
        server.kill()


def main() -> int:
    uv = find_uv()
    if uv is None:
        print("uv is not installed. Run start_colonquest.bat, or install it: https://docs.astral.sh/uv/")
        return 1

    if not ENV_FILE.exists():
        shutil.copyfile(ENV_EXAMPLE, ENV_FILE)
        print("Created server/.env.")
    ask_for_keys()
    check_meta_key()

    print("Installing the backend (the first time takes about a minute)...")
    if subprocess.run([uv, "sync", "--no-dev", "--quiet"], cwd=SERVER, check=False).returncode != 0:
        print("uv could not install the dependencies.")
        return 1

    if read_value("DATABASE_URL").startswith("postgres"):
        migrate = [uv, "run", "--no-dev", "alembic", "upgrade", "head"]
        if subprocess.run(migrate, cwd=SERVER, check=False).returncode != 0:
            print("The migrations failed: check DATABASE_URL in server/.env.")
            return 1

    server = subprocess.Popen(
        [uv, "run", "--no-dev", "python", "-m", "app.dev"],
        cwd=SERVER,
        env={**os.environ, "PORT": PORT},
    )
    try:
        if not wait_until_ready(server):
            print(f"The backend did not start. Is port {PORT} taken? Set PORT to use another one.")
            stop(server)
            return 1

        mode = "live AI" if read_value("MODEL_API_KEY") else "scripted content (no MODEL_API_KEY)"
        print(f"\nColonQuest backend: {API_URL} ({mode})")
        exe = next((path for path in EXE_CANDIDATES if path.exists()), None)
        if exe is None or "--no-open" in sys.argv:
            print("Keep this window open and start ColonQuest.exe. Ctrl+C stops the backend.\n")
            return server.wait()

        print("Opening ColonQuest. Closing it stops the backend.\n")
        game = subprocess.Popen(
            [str(exe)], cwd=exe.parent, env={**os.environ, "COLONQUEST_API_URL": API_URL}
        )
        game.wait()
        return 0
    except KeyboardInterrupt:
        return 0
    finally:
        stop(server)


if __name__ == "__main__":
    sys.exit(main())

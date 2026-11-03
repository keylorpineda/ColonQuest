"""Run only the test suites a change can affect, skipping those that already passed as they are.

Usage:
    python scripts/run_tests.py              # every suite whose inputs changed since it last passed
    python scripts/run_tests.py --fast       # the same without PlayMode, which takes six minutes
    python scripts/run_tests.py --failed     # only the PlayMode tests that failed last time
    python scripts/run_tests.py --full       # everything, ignoring the cache
    python scripts/run_tests.py engine server

A suite is skipped when the files it depends on are exactly those of its last green run. The cache is
the fingerprint of those files in .test-cache.json, so a run can never be skipped because of its date.
A server run without its database skips the tests that need it, so it is reported but never cached.
Standard library only.
"""

import argparse
import hashlib
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / ".test-cache.json"
UNITY_VERSION = "6000.3.27f1"
DEFAULT_UNITY = Path(rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe")
DB_PORT = 55432
DB_URL = f"postgresql://test:test@localhost:{DB_PORT}/colon_quest_test"
DB_CONTAINER = "cq-test-pg"

# What Unity rewrites by itself while it runs: it never counts as a change.
UNITY_NOISE = (
    ":!unity/Assets/InitTestScene*",
    ":!unity/ProjectSettings/GraphicsSettings.asset",
    ":!unity/ProjectSettings/QualitySettings.asset",
    ":!unity/ProjectSettings/URPProjectSettings.asset",
)
UNITY_INPUTS = ("unity/Assets", "unity/Packages", "unity/ProjectSettings", *UNITY_NOISE)
INPUTS = {
    "engine": ("unity/Assets/Scripts/Finance", "unity/Assets/Tests/EditMode", "unity/Tests.DotNet"),
    "server": ("server", "shared"),
    "editmode": UNITY_INPUTS,
    "playmode": UNITY_INPUTS,
}
SLOWEST = "playmode"


def git(*args: str) -> str:
    return subprocess.run(
        ["git", *args], cwd=ROOT, check=True, capture_output=True, encoding="utf-8", errors="replace"
    ).stdout


def fingerprint(paths: tuple[str, ...]) -> str:
    """The committed files of a suite, plus whatever differs from them in the working tree."""
    digest = hashlib.sha256(git("ls-files", "-s", "--", *paths).encode())
    for line in git("status", "--porcelain", "-uall", "--", *paths).splitlines():
        name = line[3:].split(" -> ")[-1].strip('"')
        path = ROOT / name
        digest.update(line.encode())
        if path.is_file():
            digest.update(hashlib.sha256(path.read_bytes()).digest())
    return digest.hexdigest()


def load_cache() -> dict[str, dict[str, object]]:
    try:
        return json.loads(CACHE.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def save_cache(cache: dict[str, dict[str, object]]) -> None:
    CACHE.write_text(json.dumps(cache, indent=2), encoding="utf-8")


def database_is_up() -> bool:
    try:
        with socket.create_connection(("localhost", DB_PORT), timeout=1):
            return True
    except OSError:
        return False


def start_database() -> bool:
    """Starts the disposable test database when Docker is running. It is left up for the next run."""
    if database_is_up():
        return True
    docker = shutil.which("docker")
    if docker is None or subprocess.run([docker, "info"], capture_output=True, check=False).returncode != 0:
        return False
    subprocess.run([docker, "rm", "-f", DB_CONTAINER], capture_output=True, check=False)
    subprocess.run(
        [
            docker, "run", "-d", "--rm", "--name", DB_CONTAINER,
            "-e", "POSTGRES_USER=test", "-e", "POSTGRES_PASSWORD=test", "-e", "POSTGRES_DB=colon_quest_test",
            "-p", f"{DB_PORT}:5432", "postgres:17-alpine",
        ],
        capture_output=True,
        check=False,
    )
    for _ in range(30):
        ready = subprocess.run(
            [docker, "exec", DB_CONTAINER, "pg_isready", "-U", "test"], capture_output=True, check=False
        )
        if ready.returncode == 0:
            return True
        time.sleep(1)
    return False


def run_engine() -> tuple[bool, str]:
    done = subprocess.run(
        ["dotnet", "test", "unity/Tests.DotNet/ColonQuest.Finance.Tests.csproj", "--nologo", "--verbosity", "minimal"],
        cwd=ROOT,
        check=False,
    )
    return done.returncode == 0, ""


def run_server() -> tuple[bool, str]:
    env = dict(os.environ)
    full = start_database()
    if full:
        env["TEST_DATABASE_URL"] = DB_URL
    else:
        env.pop("TEST_DATABASE_URL", None)
    server = ROOT / "server"
    for command in (["uv", "run", "ruff", "check", "."], ["uv", "run", "mypy", "app"], ["uv", "run", "pytest", "-q"]):
        if subprocess.run(command, cwd=server, env=env, check=False).returncode != 0:
            return False, ""
    return True, "" if full else "no database: its tests were skipped, so this run is not cached"


def restore_unity_noise() -> None:
    for scene in (ROOT / "unity" / "Assets").glob("InitTestScene*"):
        scene.unlink(missing_ok=True)
    subprocess.run(
        ["git", "checkout", "--", "unity/ProjectSettings/GraphicsSettings.asset", "unity/ProjectSettings/QualitySettings.asset"],
        cwd=ROOT,
        capture_output=True,
        check=False,
    )


def run_unity(platform: str, only: list[str] | None = None) -> tuple[bool, str, list[str]]:
    unity = Path(os.environ.get("UNITY_PATH", DEFAULT_UNITY))
    if not unity.exists():
        return False, f"Unity is not at {unity}: set UNITY_PATH", []
    if (ROOT / "unity" / "Temp" / "UnityLockfile").exists() and _editor_is_open():
        return False, "the Unity editor is open on this project: close it first", []

    with tempfile.TemporaryDirectory() as folder:
        results = Path(folder) / "results.xml"
        log = Path(folder) / "unity.log"
        command = [
            str(unity), "-batchmode", "-projectPath", str(ROOT / "unity"), "-runTests",
            "-testPlatform", platform, "-testResults", str(results), "-logFile", str(log),
        ]
        if only:
            command += ["-testFilter", "|".join(only)]
        subprocess.run(command, check=False)
        restore_unity_noise()
        if not results.exists():
            text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
            errors = sorted({line for line in text.splitlines() if "error CS" in line or "No valid Unity" in line})
            return False, "\n".join(errors[:8]) or "Unity gave no results", []
        root = ET.parse(results).getroot()
        failed = [case.get("fullname", "") for case in root.iter("test-case") if case.get("result") == "Failed"]
        note = f"{root.get('passed')} of {root.get('total')} passed"
        for case in root.iter("test-case"):
            if case.get("result") == "Failed":
                message = case.find(".//message")
                note += f"\n  FAILED {case.get('name')}: {(message.text or '')[:200] if message is not None else ''}"
        return root.get("failed") == "0", note, failed


def _editor_is_open() -> bool:
    if sys.platform != "win32":
        return False
    tasks = subprocess.run(["tasklist", "/FI", "IMAGENAME eq Unity.exe"], capture_output=True, encoding="utf-8", errors="replace", check=False)
    return "Unity.exe" in tasks.stdout


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("suites", nargs="*", help=f"limit the run to these suites: {', '.join(INPUTS)}")
    parser.add_argument("--fast", action="store_true", help="leave PlayMode out")
    parser.add_argument("--full", action="store_true", help="ignore the cache")
    parser.add_argument("--failed", action="store_true", help="only the PlayMode tests that failed last time")
    args = parser.parse_args()
    unknown = [suite for suite in args.suites if suite not in INPUTS]
    if unknown:
        parser.error(f"unknown suites: {', '.join(unknown)}")

    cache = load_cache()
    if args.failed:
        failed = list(cache.get("playmode-failed", {}).get("tests", []))  # type: ignore[arg-type]
        if not failed:
            print("No PlayMode test failed last time.")
            return 0
        print(f"Running {len(failed)} failed PlayMode tests.")
        passed, note, still = run_unity("PlayMode", failed)
        print(note)
        cache["playmode-failed"] = {"tests": still}
        save_cache(cache)
        return 0 if passed else 1

    wanted = args.suites or list(INPUTS)
    if args.fast:
        wanted = [suite for suite in wanted if suite != SLOWEST]

    report: list[str] = []
    worst = 0
    for suite in wanted:
        mark = fingerprint(INPUTS[suite])
        if not args.full and cache.get(suite, {}).get("fingerprint") == mark:
            report.append(f"{suite:9} unchanged since it passed, skipped")
            continue

        print(f"\n--- {suite} ---", flush=True)
        started = time.perf_counter()
        keep = True
        if suite == "engine":
            passed, note = run_engine()
        elif suite == "server":
            passed, note = run_server()
            keep = not note
        else:
            passed, note, failed = run_unity("EditMode" if suite == "editmode" else "PlayMode")
            if suite == SLOWEST:
                cache["playmode-failed"] = {"tests": failed}

        seconds = time.perf_counter() - started
        report.append(f"{suite:9} {'PASSED' if passed else 'FAILED'} in {seconds:.0f} s  {note}")
        if passed and keep:
            cache[suite] = {"fingerprint": fingerprint(INPUTS[suite])}
        elif not passed:
            cache.pop(suite, None)
            worst = 1
        save_cache(cache)

    print("\n" + "\n".join(report))
    return worst


if __name__ == "__main__":
    sys.exit(main())

"""Build the Windows simulator and zip it with its local backend, ready for the judges.

Usage:
    cp colonquest.env.example colonquest.env
    python scripts/build_game.py                  # build with Unity, then package
    python scripts/build_game.py --package-only   # package the last build again
    python scripts/build_game.py --remote         # allow a backend that is not on this computer

Leaves dist/ColonQuest-Windows.zip: start_colonquest.bat at the top, the simulator in game/ with
colonquest.env beside the exe, and the backend in server/ without any .env. The judge unzips it,
double-clicks start_colonquest.bat and pastes a Meta key. UNITY_PATH overrides where Unity is.
Close the Unity editor first. API keys belong in server/.env and never reach the zip.
Standard library only.
"""

import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENV_FILE = ROOT / "colonquest.env"
ENV_EXAMPLE = ROOT / "colonquest.env.example"
DIST = ROOT / "dist"
BUILD = DIST / "ColonQuest-Windows"
PACKAGE = DIST / "ColonQuest"
ARCHIVE = DIST / "ColonQuest-Windows"
# What the release zip carries besides the build: the launcher and the backend it starts.
LAUNCHER = ROOT / "start_colonquest.bat"
SERVER_FILES = ("app", "migrations", "alembic.ini", "pyproject.toml", "uv.lock", ".env.example", "README.md")
SKIPPED = shutil.ignore_patterns("__pycache__", "*.pyc", ".env", "*.db", "*.db-*")
LOG = DIST / "unity-build.log"
UNITY_VERSION = "6000.3.27f1"
DEFAULT_UNITY = Path(rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe")
NOT_SHIPPED = "_DoNotShip"
SECRET_WORDS = ("KEY", "SECRET", "TOKEN", "PASSWORD")


def read_env(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line and not line.startswith("#") and "=" in line:
            name, value = line.split("=", 1)
            values[name.strip()] = value.strip()
    return values


def main() -> int:
    if not ENV_FILE.exists():
        print(f"{ENV_FILE.name} does not exist: copying {ENV_EXAMPLE.name}, the local backend.")
        shutil.copyfile(ENV_EXAMPLE, ENV_FILE)

    values = read_env(ENV_FILE)
    secrets = [name for name in values if any(word in name.upper() for word in SECRET_WORDS)]
    if secrets:
        print(f"{ENV_FILE.name} would ship inside the zip: move {', '.join(secrets)} to server/.env.")
        return 1

    url = values.get("COLONQUEST_API_URL", "")
    if not url.startswith(("http://", "https://")):
        print(f"COLONQUEST_API_URL in {ENV_FILE.name} is not a URL: {url!r}")
        return 1
    # The zip carries its own backend; a build that talks to another one would skip the judge's key.
    if not url.startswith(("http://localhost", "http://127.0.0.1")) and "--remote" not in sys.argv:
        print(f"{ENV_FILE.name} points to {url}, not this computer. Pass --remote if that is on purpose.")
        return 1

    if "--package-only" in sys.argv:
        return package()

    unity = Path(os.environ.get("UNITY_PATH", DEFAULT_UNITY))
    if not unity.exists():
        print(f"Unity is not at {unity}. Install {UNITY_VERSION} or set UNITY_PATH.")
        return 1

    shutil.rmtree(BUILD, ignore_errors=True)
    BUILD.mkdir(parents=True)
    print(f"Building with {unity.name}, backend {url}. This takes some minutes.")
    result = subprocess.run(
        [
            str(unity), "-batchmode", "-quit",
            "-projectPath", str(ROOT / "unity"),
            "-executeMethod", "ColonQuest.EditorTools.BuildScript.Windows",
            "-buildPath", str(BUILD),
            "-logFile", str(LOG),
        ],
        check=False,
    )
    if result.returncode != 0 or not (BUILD / "ColonQuest.exe").exists():
        print(f"The build failed: see {LOG}. Is the Unity editor open on this project?")
        return 1

    for folder in BUILD.glob(f"*{NOT_SHIPPED}"):
        shutil.rmtree(folder)
    return package()


def package() -> int:
    """Put the build, the backend and the launcher together and zip them."""
    if not (BUILD / "ColonQuest.exe").exists():
        print(f"There is no build in {BUILD}: run this script without --package-only first.")
        return 1
    shutil.rmtree(PACKAGE, ignore_errors=True)
    shutil.copytree(BUILD, PACKAGE / "game", ignore=shutil.ignore_patterns(f"*{NOT_SHIPPED}"))
    shutil.copyfile(ENV_FILE, PACKAGE / "game" / ENV_FILE.name)
    for name in SERVER_FILES:
        source = ROOT / "server" / name
        target = PACKAGE / "server" / name
        if source.is_dir():
            shutil.copytree(source, target, ignore=SKIPPED)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
    (PACKAGE / "scripts").mkdir()
    shutil.copyfile(ROOT / "scripts" / "run_local.py", PACKAGE / "scripts" / "run_local.py")
    shutil.copyfile(LAUNCHER, PACKAGE / LAUNCHER.name)

    leaked = [path for path in PACKAGE.rglob("*") if path.name == ".env" or path.suffix == ".db"]
    if leaked:
        print(f"Refusing to zip local secrets or data: {', '.join(map(str, leaked))}")
        return 1
    archive = shutil.make_archive(str(ARCHIVE), "zip", DIST, PACKAGE.name)
    print(f"Ready: {archive}. Unzip it and double-click {LAUNCHER.name}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

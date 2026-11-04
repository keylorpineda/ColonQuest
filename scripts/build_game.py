"""Build the Windows simulator from colonquest.env and zip it for the judges.

Usage:
    cp colonquest.env.example colonquest.env
    python scripts/build_game.py

Leaves dist/ColonQuest-Windows.zip, with colonquest.env beside the exe so it can still be changed
without building again. UNITY_PATH overrides where Unity is. Close the Unity editor first. The file
only says which backend the simulator talks to: API keys belong in server/.env and never reach the build.
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
    shutil.copyfile(ENV_FILE, BUILD / ENV_FILE.name)
    archive = shutil.make_archive(str(BUILD), "zip", BUILD)
    print(f"Ready: {archive}")
    return 0


if __name__ == "__main__":
    sys.exit(main())

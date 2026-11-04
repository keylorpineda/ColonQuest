<p align="center"><b>English</b> | <a href="RUN_AND_BUILD.es.md">Español</a></p>

<p align="center">
  <a href="README.md"><img src="docs/assets/tab-readme-en.svg" alt="README"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
</p>

<p align="center">
  <img src="docs/assets/run-en.svg" width="100%" alt="How to run ColonQuest on your computer, start its local backend and build the executable from the env files. The full content of this card is available as text in the section below.">
</p>

<details open>
<summary><b>Read this page as text</b></summary>

## Run and build

ColonQuest runs entirely on your computer. There is no hosted service: nothing here talks to a server of ours.
There are three ways in, from the fastest to the most complete. Each one only adds to the last.

| You want to | You need | Time |
| --- | --- | --- |
| [Run the simulator on its own](#1-run-the-simulator-on-its-own) | Windows | 1 minute |
| [Add the local backend and the live AI](#3-start-the-local-backend) | Python, uv and, for the AI, your Meta key | 5 minutes |
| [Build the executable](#4-build-the-executable) | Unity 6000.3.27f1 and Python | 10 minutes |

### 1. Run the simulator on its own

Download `ColonQuest-Windows.zip` from the [latest release](https://github.com/keylorpineda/ColonQuest/releases/latest), unzip it, open `ColonQuest.exe` and choose **Guided simulation**.
With no backend running it uses its local content: the finance engine, the scripted events and the scripted
seller all work, with no internet and no keys. The live models need [the local backend](#3-start-the-local-backend).

### 2. The two env files

Everything you can change lives in two files, and neither is in the repository.

| File | Who reads it | What it holds | Secrets |
| --- | --- | --- | --- |
| `server/.env` | The backend | API keys, models, budgets, database | **Yes** |
| `colonquest.env` | The simulator | Which backend it talks to | No |

**The API keys never go inside the executable.** The simulator only knows a URL; the backend holds the keys and
calls the models.

The simulator looks for its backend in this order:

1. The environment variable `COLONQUEST_API_URL`.
2. The file `colonquest.env` beside `ColonQuest.exe`.
3. `http://localhost:8000/api`, this computer.

### 3. Start the local backend

You need Python 3.12 and [uv](https://docs.astral.sh/uv/) (`python -m pip install uv`). No database to install:
it keeps its data in a local SQLite file. From the root of the repository, double-click `run_local.bat` or run:

```bash
python scripts/run_local.py
```

The first run creates `server/.env` and installs the dependencies. Leave that window open and open
`ColonQuest.exe`. Check that it answers at `http://localhost:8000/api/health`.

Without keys the backend answers with the scripted events and records every decision. To turn on the live AI,
put your keys in `server/.env` and start it again:

| Variable | What it is |
| --- | --- |
| `MODEL_API_KEY` | Your key for the Meta Model API. Without it, the models do not run |
| `OPEN_WEIGHTS_API_KEY` | Optional: your Baseten key, for Muse Spark Open. Without it, the characters use Meta |

To use Postgres instead of SQLite, set `DATABASE_URL` in `server/.env`; the script applies the migrations.

Portraits and scenes are generated the first time they are asked for, which takes a while and needs your Meta key.
To have them all ready, run `uv run python scripts/pregenerate_images.py` once in `server/`; each one costs $0.01.

### 4. Build the executable

You need Unity 6000.3.27f1 and Python. From the root of the repository:

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

The script builds the simulator with Unity, puts `colonquest.env` beside the exe and leaves everything zipped in
`dist/ColonQuest-Windows.zip`. If Unity is not in its usual folder, set `UNITY_PATH` to its `Unity.exe`.

Close the Unity editor first: Unity does not open the same project twice.

### Check that it is all connected

In the simulator, the panel on the right shows every real call with its model, provider, time and cost. If it stays
empty and the simulator still answers, it could not reach the backend and is running its local rules: check
that `run_local.bat` is open and that `http://localhost:8000/api/health` answers. If the panel shows calls marked as failed, the
backend has no valid key. The simulator writes the backend it uses in its log, on a line that starts with
`[ColonQuest] Backend:`, at `%USERPROFILE%\AppData\LocalLow\DoWhile Team\ColonQuest\Player.log`.

</details>

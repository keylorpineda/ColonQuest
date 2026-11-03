<p align="center"><b>English</b> | <a href="RUN_AND_BUILD.es.md">Español</a></p>

<p align="center">
  <a href="README.md"><img src="docs/assets/tab-readme-en.svg" alt="README"></a>
  <a href="COSTA_RICA.md"><img src="docs/assets/tab-costa-rica-en.svg" alt="The situation in Costa Rica, with sources"></a>
</p>

<p align="center">
  <img src="docs/assets/run-en.svg" width="100%" alt="How to run ColonQuest, point it to another backend and build the executable from the env files. The full content of this card is available as text in the section below.">
</p>

<details open>
<summary><b>Read this page as text</b></summary>

## Run and build

There are three ways in, from the fastest to the most complete. Each one only adds to the last.

| You want to | You need | Time |
| --- | --- | --- |
| [Play it](#1-just-play-it) | Windows and internet | 1 minute |
| [Use your own backend and keys](#3-run-your-own-backend) | uv, Postgres and your keys | 10 minutes |
| [Build the executable](#4-build-the-executable) | Unity 6000.3.27f1 and Python | 10 minutes |

### 1. Just play it

Download `ColonQuest-Windows.zip` from the [latest release](https://github.com/keylorpineda/ColonQuest/releases/latest), unzip it, open `ColonQuest.exe` and choose **Guided simulation**. It talks to our
hosted backend, so it only needs internet. The first answer can take up to a minute if the server was
asleep; the game plays a scripted event if it does not arrive in time.

### 2. The two env files

Everything you can change lives in two files, and neither is in the repository.

| File | Who reads it | What it holds | Secrets |
| --- | --- | --- | --- |
| `server/.env` | The backend | API keys, models, budgets, database | **Yes** |
| `colonquest.env` | The game | Which backend it talks to | No |

**The API keys never go inside the executable.** The game only knows a URL; the backend holds the keys and
calls the models.

The game looks for its backend in this order:

1. The environment variable `COLONQUEST_API_URL`.
2. The file `colonquest.env` beside `ColonQuest.exe`.
3. Our hosted backend.

So you can move an existing build to another backend without building anything: edit `colonquest.env`
beside the exe and open the game again.

```
COLONQUEST_API_URL=http://localhost:8000/api
```

### 3. Run your own backend

You need [uv](https://docs.astral.sh/uv/), a Postgres database and your keys.

```bash
cd server
cp .env.example .env
```

Fill in `server/.env`. These three are the ones you must set; the rest of the file already holds the values
we run in production.

| Variable | What it is |
| --- | --- |
| `MODEL_API_KEY` | Your key for the Meta Model API |
| `OPEN_WEIGHTS_API_KEY` | Your Baseten key, for Muse Spark Open. Without it, set `NPC_PROVIDER=meta_api` and the characters use Meta |
| `DATABASE_URL` | Your Postgres database |

If you do not have a database at hand, this starts one, and its `DATABASE_URL` is
`postgresql://test:test@localhost:55432/colon_quest_test`:

```bash
docker run -d --name cq-pg -e POSTGRES_USER=test -e POSTGRES_PASSWORD=test -e POSTGRES_DB=colon_quest_test -p 55432:5432 postgres:17-alpine
```

Then start the server. Check that it answers at `http://localhost:8000/api/health`.

```bash
uv sync
uv run alembic upgrade head
uv run python -m app.dev
```

Portraits and scenes are generated the first time they are asked for, which takes a while. To have them all
ready, run `uv run python scripts/pregenerate_images.py` once; each one costs $0.01.

### 4. Build the executable

You need Unity 6000.3.27f1 and Python. From the root of the repository:

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

Edit `colonquest.env` first if the build should talk to your backend. The script builds the game with Unity,
puts that file beside the exe and leaves everything zipped in `dist/ColonQuest-Windows.zip`. If Unity is not in
its usual folder, set `UNITY_PATH` to its `Unity.exe`.

Close the Unity editor first: Unity does not open the same project twice.

### Check that it is all connected

In the game, the panel on the right shows every real call with its model, provider, time and cost. If it stays
empty and the game still answers, the game could not reach the backend and is playing its local rules: check
the URL in `colonquest.env` and `/api/health`. The game writes the backend it uses in its log, on a line that starts with
`[ColonQuest] Backend:`, at `%USERPROFILE%\AppData\LocalLow\DoWhile Team\ColonQuest\Player.log`.

</details>

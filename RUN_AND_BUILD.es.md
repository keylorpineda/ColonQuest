<p align="center"><a href="RUN_AND_BUILD.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="README.es.md"><img src="docs/assets/tab-readme-es.svg" alt="README"></a>
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
</p>

<p align="center">
  <img src="docs/assets/run-es.svg" width="100%" alt="Cómo correr ColonQuest, apuntarlo a otro backend y armar el ejecutable a partir de los archivos env. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
</p>

<details open>
<summary><b>Leer esta página como texto</b></summary>

## Correr y armar

Hay tres formas de entrar, de la más rápida a la más completa. Cada una solo suma a la anterior.

| Querés | Necesitás | Tiempo |
| --- | --- | --- |
| [Jugarlo](#1-solo-jugarlo) | Windows e internet | 1 minuto |
| [Usar tu propio backend y tus llaves](#3-levantar-tu-propio-backend) | uv, Postgres y tus llaves | 10 minutos |
| [Armar el ejecutable](#4-armar-el-ejecutable) | Unity 6000.3.27f1 y Python | 10 minutos |

### 1. Solo jugarlo

Bajá `ColonQuest-Windows.zip` del [último release](https://github.com/keylorpineda/ColonQuest/releases/latest), descomprimilo, abrí `ColonQuest.exe` y elegí **Simulación guiada**. Le habla a nuestro
backend en línea, así que solo ocupa internet. La primera respuesta puede tardar hasta un minuto si el servidor
estaba dormido; si no llega a tiempo, el juego usa un evento escrito a mano.

### 2. Los dos archivos env

Todo lo que se puede cambiar vive en dos archivos, y ninguno está en el repositorio.

| Archivo | Quién lo lee | Qué guarda | Secretos |
| --- | --- | --- | --- |
| `server/.env` | El backend | Llaves de API, modelos, presupuestos, base de datos | **Sí** |
| `colonquest.env` | El juego | A qué backend le habla | No |

**Las llaves de API nunca van dentro del ejecutable.** El juego solo conoce una URL; el backend guarda las llaves
y llama a los modelos.

El juego busca su backend en este orden:

1. La variable de entorno `COLONQUEST_API_URL`.
2. El archivo `colonquest.env` al lado de `ColonQuest.exe`.
3. Nuestro backend en línea.

Así podés pasar un build ya hecho a otro backend sin armar nada: editá `colonquest.env` al lado del exe y volvé a
abrir el juego.

```
COLONQUEST_API_URL=http://localhost:8000/api
```

### 3. Levantar tu propio backend

Necesitás [uv](https://docs.astral.sh/uv/), una base de datos Postgres y tus llaves.

```bash
cd server
cp .env.example .env
```

Llená `server/.env`. Estas tres son las obligatorias; el resto del archivo ya trae los valores que usamos en
producción.

| Variable | Qué es |
| --- | --- |
| `MODEL_API_KEY` | Tu llave de la API de modelos de Meta |
| `OPEN_WEIGHTS_API_KEY` | Tu llave de Baseten, para Muse Spark Open. Sin ella, poné `NPC_PROVIDER=meta_api` y los personajes usan Meta |
| `DATABASE_URL` | Tu base de datos Postgres |

Si no tenés una base a mano, esto levanta una, y su `DATABASE_URL` es
`postgresql://test:test@localhost:55432/colon_quest_test`:

```bash
docker run -d --name cq-pg -e POSTGRES_USER=test -e POSTGRES_PASSWORD=test -e POSTGRES_DB=colon_quest_test -p 55432:5432 postgres:17-alpine
```

Después levantá el servidor. Revisá que responda en `http://localhost:8000/api/health`.

```bash
uv sync
uv run alembic upgrade head
uv run python -m app.dev
```

Los retratos y las escenas se generan la primera vez que se piden, y eso tarda. Para tenerlos todos listos, corré una
vez `uv run python scripts/pregenerate_images.py`; cada uno cuesta $0,01.

### 4. Armar el ejecutable

Necesitás Unity 6000.3.27f1 y Python. Desde la raíz del repositorio:

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

Editá `colonquest.env` antes si el build le va a hablar a tu backend. El script arma el juego con Unity, pone ese
archivo al lado del exe y deja todo comprimido en `dist/ColonQuest-Windows.zip`. Si Unity no está en su carpeta de
siempre, poné en `UNITY_PATH` la ruta de su `Unity.exe`.

Cerrá antes el editor de Unity: Unity no abre el mismo proyecto dos veces.

### Revisar que todo esté conectado

En el juego, el panel de la derecha muestra cada llamada real con su modelo, proveedor, tiempo y costo. Si se queda
vacío y el juego igual responde, el juego no alcanzó el backend y está usando sus reglas locales: revisá la URL en
`colonquest.env` y `/api/health`. El juego escribe en su log el backend que usa, en una línea que empieza con
`[ColonQuest] Backend:`, en `%USERPROFILE%\AppData\LocalLow\DoWhile Team\ColonQuest\Player.log`.

</details>

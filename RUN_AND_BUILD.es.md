<p align="center"><a href="RUN_AND_BUILD.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="README.es.md"><img src="docs/assets/tab-readme-es.svg" alt="README"></a>
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
</p>

<p align="center">
  <img src="docs/assets/run-es.svg" width="100%" alt="Cómo correr ColonQuest en tu computadora, levantar su backend local y armar el ejecutable a partir de los archivos env. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
</p>

<details open>
<summary><b>Leer esta página como texto</b></summary>

## Correr y armar

ColonQuest es un simulador de finanzas personales que corre por completo en tu computadora, con **tus propias llaves
de API**. No hay ningún servicio en línea: nada de esto le habla a un servidor nuestro.

| Querés | Necesitás | Tiempo |
| --- | --- | --- |
| [Evaluarlo con la IA en vivo](#1-evaluarlo-con-tu-propia-llave) | Windows, internet y tu llave de la API de modelos de Meta | 3 minutos |
| [Correrlo sin llaves](#sin-llaves) | Windows | 1 minuto |
| [Armar el ejecutable](#4-armar-el-ejecutable) | Unity 6000.3.27f1 y Python | 10 minutos |

### 1. Evaluarlo con tu propia llave

1. Bajá `ColonQuest-Windows.zip` del [último release](https://github.com/keylorpineda/ColonQuest/releases/latest) y descomprimilo.
2. Hacé doble clic en `start_colonquest.bat`. Cuando te la pida, pegá tu llave de la API de modelos de Meta. La llave
   de Baseten para Muse Spark Open es opcional: apretá Enter para saltarla.
3. ColonQuest se abre solo. Elegí **Simulación guiada**. Al cerrar el simulador se apaga el backend.

Eso es todo. El lanzador hace el resto, sin permisos de administrador y sin instalar nada a mano:

- Si falta [uv](https://docs.astral.sh/uv/), lo instala en tu carpeta de usuario con su instalador oficial. uv trae
  su propio Python 3.12, así que no necesitás Python.
- Revisa tu llave contra la lista de modelos de Meta, que es gratis, y te avisa si la rechazaron.
- Instala el backend, lo levanta en `http://localhost:8000/api` con sus datos en un archivo SQLite local,
  espera a que responda y abre `game/ColonQuest.exe`.

Tus llaves se guardan solo en `server/.env`, dentro de la carpeta que descomprimiste, y nunca van dentro del
ejecutable. Para cambiar una llave, editá ese archivo o borrá la línea y arrancá de nuevo. Una partida de tres meses
cuesta unos **$0,04** de crédito de Meta; cada llamada y su costo aparecen en el panel de la derecha.

Los retratos, las escenas y los finales vienen dentro del zip, así que no se genera ni se paga ninguna imagen mientras
jugás.

<a id="sin-llaves"></a>**Sin llaves.** Apretá Enter cuando te pida la llave: el backend responde con los eventos escritos a mano y registra
cada decisión. También podés abrir solo `game/ColonQuest.exe`; sin backend corre el motor financiero, los eventos
escritos a mano y el vendedor con guion, sin internet.

**Desde el repositorio, en cualquier sistema.** Con Python 3.12 y uv, `python scripts/run_local.py` hace lo mismo
desde un clon, y abre el build de `dist/` si existe. `--no-open` solo levanta el backend y `--no-prompt` se salta
las preguntas.

### 2. Los dos archivos env

Todo lo que podés cambiar vive en dos archivos, y ninguno está en el repositorio.

| Archivo | Quién lo lee | Qué guarda | Secretos |
| --- | --- | --- | --- |
| `server/.env` | El backend | Llaves, modelos, presupuestos y base de datos | **Sí** |
| `colonquest.env` | El simulador | A qué backend le habla | No |

**Las llaves nunca van dentro del ejecutable.** El simulador solo conoce una URL; el backend guarda las llaves y
llama a los modelos.

El simulador busca su backend en este orden:

1. La variable de entorno `COLONQUEST_API_URL`.
2. El archivo `colonquest.env` al lado de `ColonQuest.exe`.
3. `http://localhost:8000/api`, esta computadora.

### 3. El backend local a mano

`start_colonquest.bat` es un atajo de `python scripts/run_local.py`, que podés correr vos con Python 3.12 y
[uv](https://docs.astral.sh/uv/) instalados. La primera vez crea `server/.env` a partir de `server/.env.example`.

| Variable | Qué es |
| --- | --- |
| `MODEL_API_KEY` | Tu llave de la API de modelos de Meta. Sin ella, los modelos no corren |
| `OPEN_WEIGHTS_API_KEY` | Opcional: tu llave de Baseten, para Muse Spark Open. Sin ella, los personajes usan Meta |

Para usar Postgres en vez de SQLite, poné `DATABASE_URL` en `server/.env`; el script aplica las migraciones.
Para incluir imágenes nuevas, corré `uv run python scripts/export_images.py` en `server/`: copia el set
pre-generado de la base de datos a `server/app/images/bundled`.

### 4. Armar el ejecutable

Necesitás Unity 6000.3.27f1 y Python. Desde la raíz del repositorio:

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

El script arma el simulador con Unity y deja `dist/ColonQuest-Windows.zip`: `start_colonquest.bat` arriba, el build
en `game/` con `colonquest.env` al lado del exe, y el backend en `server/` sin `.env`. `--package-only` vuelve a
comprimir el último build sin Unity. Si Unity no está en su carpeta de siempre, poné en `UNITY_PATH` la ruta de su `Unity.exe`.

Cerrá primero el editor de Unity: Unity no abre el mismo proyecto dos veces.

### Revisar que todo esté conectado

En el simulador, el panel de la derecha muestra cada llamada real con su modelo, proveedor, tiempo y costo. Si se queda
vacío y el simulador igual responde, no alcanzó el backend y está usando sus reglas locales: revisá
que la ventana de `start_colonquest.bat` esté abierta y que `http://localhost:8000/api/health` responda. Si el panel muestra llamadas fallidas, el
backend no tiene una llave válida. El simulador escribe el backend que usa en su log, en una línea que empieza con
`[ColonQuest] Backend:`, en `%USERPROFILE%\AppData\LocalLow\DoWhile Team\ColonQuest\Player.log`.

</details>

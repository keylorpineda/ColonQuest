# Prompt para la próxima sesión: roles de los modelos

Copiá todo lo que está debajo de la línea y pegalo como primer mensaje en una sesión nueva de
Claude Code, abierta en la carpeta del repositorio. Las decisiones y sus métricas están en
[`docs/adr/0004-model-roles.md`](../adr/0004-model-roles.md).

---

Trabajás en ColonQuest (lee `CLAUDE.md` y `docs/adr/0004-model-roles.md` antes de empezar). Soy
Keylor, dueño del backend. Implementá los cambios de abajo **en el orden dado**, uno por PR
pequeño hacia `develop`, con varios commits por PR, siguiendo `CONTRIBUTING.md`:

- Rama `<tipo>/SCRUM-<n>-<descripcion>` creada desde `develop` actualizado.
- Título del PR `tipo(scope): descripción [SCRUM-n]`.
- Nunca agregues coautores ni atribución a una IA en commits ni PRs.
- Antes de cada PR corré `uv run ruff check . && uv run mypy app && uv run pytest` en `server/`.
- No me preguntes cosas que puedas decidir con el ADR; avisame solo si algo bloquea.
- Hacé `git push` de cada rama **antes** de fusionar, porque el script de limpieza borra las ramas
  locales que no están en el remoto.

Las llaves ya están en `server/.env` (`MODEL_API_KEY`, `OPEN_WEIGHTS_API_KEY`). No las imprimas
nunca. Para probar con modelos reales usá la base de pruebas (rama de Neon `qa-local-tests`),
nunca la de producción, para no ensuciar las métricas del piloto. Cada prueba real debe registrar
su latencia y su costo en la descripción del PR.

## 1. Pre-generar retratos y escenas con Muse Image (SCRUM-49)

- Crear `server/scripts/pregenerate_images.py`, con el mismo estilo que `server/scripts/smoke_models.py`.
- Claves a generar:
  - Retratos: `npc_don_rafa_mecanico`, `npc_kevin_vendedor`, `npc_dona_marta_vendedora`,
    `npc_prestamista_gota_a_gota`, `npc_amigo_piramide`.
  - Escenas: los 12 `imageKey` de `server/app/game_master/fallback_events.json`
    (`scene_consultorio`, `scene_cooperativa`, `scene_esquina_barrio`, `scene_estado_cuenta`,
    `scene_gasolinera`, `scene_ins`, `scene_navidad`, `scene_oficina`, `scene_soda`,
    `scene_taller_mecanico`, `scene_tienda_celulares`, `scene_tienda_electro`).
  - Tres ilustraciones de la postal **sin texto**: `card_year_debt_free`, `card_year_some_debt`,
    `card_year_heavy_debt`.
- Usar `ImageService` y `build_image_prompt` de `app/images/service.py`, y guardar cada imagen en
  la tabla `images` (`CachedImage`), igual que lo hace `GET /api/images/{key}`.
- Saltar las claves que ya existen. Opción `--force` para regenerar.
- Usar `IMAGE_TIMEOUT_S` (30 s): cada imagen tarda de 14 a 17 s y cuesta $0,01.
- Agregar al prompt de estilo de la postal: "no text, no numbers, no letters".
- **Cómo verificar:** correrlo contra QA, después llamar `GET /api/images/npc_kevin_vendedor` y
  revisar que el encabezado `X-Image-Source` diga `cache`.

## 2. Postal "Tu año en ColonQuest" (sin cifras del modelo)

- Agregar el campo `cardImageKey` a la respuesta de `POST /api/sessions/{id}/summary`. Elige
  `card_year_debt_free`, `card_year_some_debt` o `card_year_heavy_debt` según la deuda final
  calculada en `app/summary/stats.py` (definir los umbrales como constantes, en colones enteros).
- Actualizar el esquema en `shared/` y el chequeo de deriva del CI.
- El texto con las cifras **no** sale de ningún modelo: Unity lo dibuja encima (ver la sección para Jefferson).

## 3. Resumen del año con Muse Wonder

- Nueva variable `SUMMARY_TIMEOUT_S` (25 s) usada solo por `SummaryWriter` en `app/summary/writer.py`.
- En Render, `SUMMARY_MODEL=muse-wonder-1.0`.
- Mantener `reasoning_effort` en `minimal` y `max_tokens` en 2000. Si falla o se pasa del tiempo,
  usa el resumen escrito a mano, como hoy.
- Pruebas: un `FakeProvider` lento confirma que espera más de 8 s y menos de 25 s.

## 4. Plan de temporada con Wonder en segundo plano

- Hoy `plan_month` en `app/api/events.py` llama al planificador **dentro** de la petición del
  primer evento. Con Wonder (6 a 18 s) eso pasaría el límite de 8 s de Unity.
- Nuevo flujo:
  1. En el primer evento, guardar `default_plan(state)` de inmediato y responder sin esperar.
  2. Lanzar el planificador con FastAPI `BackgroundTasks`, con su propia sesión de base de datos
     y un `DbLedger` nuevo.
  3. Si el plan de Wonder valida, reemplaza `sessions.season_plan` y rige desde el mes 2.
- Nueva variable `PLANNER_MODEL` (por defecto `muse-wonder-1.0`) y `PLANNER_TIMEOUT_S` (30 s).
- Pruebas: el primer evento responde sin esperar al planificador, y el plan guardado cambia después
  de que corre la tarea.

## 5. Banco de eventos escrito por Wonder (complemento)

- `server/scripts/generate_event_bank.py`: pide a Muse Wonder unos 30 eventos, cubriendo todos los
  `TeachingConcept` y tipos de evento, usando el `SYSTEM_PROMPT` del game master.
- Validar cada uno con `GameEvent` y `clamp_event`. Descartar los que cambien al recortar.
- Escribir el resultado en `server/app/game_master/fallback_events.json` sin borrar los eventos
  escritos a mano. Revisar ids repetidos y el tope de 280 caracteres de la narrativa.
- Copiarlo también a `unity/Assets/Resources/fallback_events.json` envuelto como
  `{ "events": [...] }`. Hay una prueba de Unity que lo valida.
- Costo esperado: menos de $1 en total.

## 6. Vendedores con tácticas y memoria en Muse Spark Open (Baseten)

- `OpenWeightsProvider` (`app/providers/open_weights.py`) implementa `chat()` (el protocolo
  `ToolCallingProvider`) reutilizando `chat_completion` de `app/providers/openai_compat.py`, con
  `reasoning_effort` `low` y el mismo margen de tokens.
- `FallbackProvider` (`app/providers/routing.py`) también implementa `chat()`: primero Baseten,
  después Meta si falla o se pasa del tiempo.
- En `app/main.py`, `SellerAgent` recibe el proveedor de los personajes (el `FallbackProvider`)
  en vez de `MetaProvider`.
- **Memoria de la partida:** `run_turn` en `app/api/negotiation.py` arma, además del historial de
  la oferta, un resumen de las preguntas clave que el jugador hizo en **otras** ofertas de la misma
  sesión (tabla `negotiations`, campo `key_questions_asked`). Se agrega al prompt como
  `playerHabits`, para que el vendedor cambie de táctica ("ya sé que vos preguntás por el seguro").
  El guardia de cifras no cambia.
- Probar en vivo contra QA con `SELLER_AGENT=true`. Si un turno pasa de 6 s, limitar el agente a
  una sola consulta de herramienta.
- Después, en Render, `SELLER_AGENT=true`.
- Actualizar `docs/adr/0003-agentic-game-master.md` y la tabla del ADR 0004.

## 7. Tope de gasto de Baseten y ping para UptimeRobot

- En `app/cost_guard.py`, contar también el costo de `open_weights` ($1,25 / $0,15 / $4,25 por
  millón de tokens) con su propio presupuesto diario `OPEN_WEIGHTS_DAILY_BUDGET_USD` (por defecto 3).
- Al llegar al tope, los personajes pasan a Meta automáticamente.
- Agregar `GET /api/ping`, que responde `{"status":"ok"}` sin tocar la base ni los modelos, para
  que UptimeRobot mantenga despierto Render sin despertar Neon.

## 8. Publicar el WebGL con pnpm (SCRUM-53, coordinado con Jefferson)

El repositorio no tiene nada de Node hoy. Agregar en la raíz:

- `package.json` con `"packageManager": "pnpm@<versión estable actual>"`, sin dependencias de
  ejecución, solo `wrangler` como dependencia de desarrollo.
- `pnpm-lock.yaml`.
- Scripts:
  - `"webgl:serve"`: sirve `unity/Builds/webgl` en local.
  - `"webgl:deploy"`: `wrangler pages deploy unity/Builds/webgl --project-name colon-quest`.
- `.gitignore`: ignorar `node_modules/`.
- CI: un job `Web / build check` que corra `pnpm install --frozen-lockfile`, sin desplegar.
- `docs/deployment.md`: cómo compilar el WebGL (menú "ColonQuest > Build > WebGL") y publicarlo con
  `pnpm webgl:deploy`.
- Agregar el dominio publicado a `ALLOWED_ORIGINS` en Render.

## Al terminar

- Actualizar la tabla de roles del `README.md`, el `README.es.md` y
  `scripts/build_readme_art.py`. Regenerar las tarjetas.
- Pasar a Finalizada los tickets de Jira que correspondan.
- Darme un resumen en español con lo que quedó listo, lo que no y el costo total de las pruebas.

---

## Para Jefferson (Unity), en otra sesión o a mano

1. **Retratos y escenas:** pedir `GET {BaseUrl}/images/{key}` y mostrar la textura en el gancho
   de retrato de `NegotiationPanel` y en la ventana del evento (el evento ya trae `imageKey`).
   Guardarlas en memoria para no pedirlas dos veces.
2. **Resumen del año:** subir el tiempo de espera de `RequestSummary` a 30 s y mostrar una
   animación de "armando tu año".
3. **Postal:** mostrar la imagen `cardImageKey` y dibujar encima, con texto de Unity, las cifras
   de `SessionSummary` (deuda, patrimonio, decisiones). El modelo nunca escribe números.
4. **URL de producción:** en builds WebGL, llamar
   `ApiConfig.Use("https://colon-quest-api.onrender.com/api")` antes de la primera llamada.
5. **Publicar el WebGL** con `pnpm webgl:deploy` (punto 8) y pasarme el link.

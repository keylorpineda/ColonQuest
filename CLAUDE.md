# Colón Quest — Guía de implementación para Claude Code

Juego 2D de educación financiera para estudiantes de Costa Rica, construido para el
**Meta Global AI Developer Hackathon** (track Indie). Un game master con IA convierte cada
decisión de plata en una historia con consecuencias; el jugador negocia por voz con
personajes que intentan engañarlo (cuotas con cargos escondidos, préstamos "gota a gota",
esquemas piramidales).

- **Equipo:** DoWhile Team. Keylor (backend, IA, motor financiero) y Jefferson (Unity, arte, flujo).
- **Entrega:** lunes 2 de noviembre de 2026. Todo debe estar terminado el domingo 1 o, a más
  tardar, el lunes 2. Hora exacta de cierre por confirmar.
- **Objetivo de alcance:** un *vertical slice* pulido. Un mes jugable de 2 a 3 minutos que
  funcione en vivo es mejor que un año completo a medias.

## Cómo nos evalúan (decide las prioridades)

| Criterio | Peso | Qué implica para el código |
| --- | --- | --- |
| Impacto | 30% | Registrar métricas reales de juego para la prueba piloto con estudiantes |
| Uso de herramientas de Meta | 25% | Wonder, Voice Transcribe, Image y Spark con roles claros, no un chatbot |
| Demo | 25% | Build web jugable con un link, sin errores, que aguante en vivo |
| Profundidad técnica | 20% | Contratos JSON validados, motor financiero exacto, control de costos |

## Premios extra ($50K) y qué entra en el plan

Además del track Indie, hay cinco premios extra. **Prioridad absoluta: el mes jugable con la
negociación funcionando.** Nada de esta tabla se hace a costa de eso.

| Premio | Monto | Estado en el plan | Qué exige |
| --- | --- | --- | --- |
| Best Creative Build | $5K | **Entra seguro.** Ya lo cubre el diseño | Proyecto original con Muse Image o Muse Wonder |
| Most Viral Project | $20K | **Entra seguro.** Es campaña, no código | Post del demo en X con más interacción hasta el 8 nov, 11:59 p. m. ET |
| Community Vote | $10K | **Entra seguro.** Es campaña, no código | Más votos en la galería pública (ganador el 16 nov) |
| Best Build on Meta Open Weights | $10K | **Condicional.** Solo si aprueban créditos de Baseten o Modal antes del 28 oct | Parte central del proyecto corriendo en Muse Glimmer o Muse Spark Open |
| Best Agentic Use-Case | $5K | **Solo si da tiempo.** Únicamente si el mes jugable está listo el 29 oct | Agentes que deciden y ejecutan tareas de varios pasos |

Pendiente de confirmar en Discord: si un proyecto puede ganar un premio de track y uno extra,
y si para Open Weights basta con que una parte central use el modelo abierto.

## Reglas que no se rompen

1. **La IA cuenta la historia; el motor financiero lleva las cuentas.** Ningún monto, interés
   ni saldo sale de un LLM. El modelo propone efectos con parámetros acotados y el motor
   los valida, los recorta a rangos permitidos y los aplica.
2. **Las llaves de API viven solo en el backend.** Nunca en Unity, nunca en el repositorio.
   Se leen de variables de entorno.
3. **Toda respuesta de un modelo se valida contra un esquema** (Pydantic en el backend).
   Si no valida, se reintenta una vez y luego se usa un evento de respaldo predefinido.
   El juego nunca se rompe por una respuesta mala del modelo.
4. **Cada llamada a un modelo pasa por el control de costos** (ver más abajo).
5. **El dinero se maneja en enteros** (colones, sin decimales). Nada de `float` para montos.
6. **Código en inglés** (nombres, comentarios). Textos del juego en español de Costa Rica.

## Arquitectura

```
Unity 2D (WebGL + escritorio)
  ├─ Motor financiero determinista (C#)
  ├─ Mapa cenital pixel art, estaciones de "tareas"
  └─ Paneles overlay (SINPE, estado de cuenta, negociación)
        │  HTTPS + JSON
        ▼
Backend FastAPI (Python 3.12, Pydantic v2)
  ├─ Game master, negociación, imágenes, resumen
  ├─ Control de costos y caché
  └─ Telemetría de partidas y Modo Clase
        │                         │
        ▼                         ▼
Meta Model API              Neon (PostgreSQL)
https://api.meta.ai/v1      sesiones, decisiones, clases
```

### Decisiones de stack

- **Backend: FastAPI + Pydantic v2** (no Flask). Keylor ya lo domina, valida esquemas de
  forma nativa y genera documentación OpenAPI en `/docs`, útil para que Unity consuma el contrato.
- **Cliente de la API de Meta:** el SDK oficial de OpenAI para Python apuntando a
  `base_url="https://api.meta.ai/v1"` y `api_key=os.environ["MODEL_API_KEY"]`. La API de Meta
  es compatible con el SDK de OpenAI. Envolverlo en `app/providers/meta.py` para que el resto
  del código no dependa del SDK.
- **Base de datos:** Neon (PostgreSQL) con SQLAlchemy 2 o psycopg 3 y migraciones con Alembic.
- **Despliegue:** backend en Render; build WebGL de Unity como sitio estático (itch.io,
  Cloudflare Pages o Vercel). Configurar CORS para el dominio del build.

### Antes de escribir código contra la API: verificar en la documentación

La documentación oficial está en `https://dev.meta.ai/docs`. Confirmar ahí, no adivinar:

- Formato exacto de salida estructurada (JSON schema) en la Responses API.
- Formatos de audio aceptados por `POST /v1/asr/transcribe` y cómo enviar el archivo.
- Parámetros de `POST /v1/images/generations` (tamaño, formato base64 o URL).
- Cómo se pasa `reasoning: { effort }` y qué valores acepta cada modelo.

## Modelos y responsabilidades

| Modelo | ID | Uso en el juego | Notas de costo |
| --- | --- | --- | --- |
| Muse Wonder | `muse-wonder-1.0` | Game master: genera el evento del mes y da voz a los NPCs en la negociación | $4 entrada / $20 salida por millón de tokens. `reasoning.effort` bajo |
| Muse Spark | `muse-spark-1.3` | Resumen anual, análisis del Modo Clase, y modelo de desarrollo para todo | $1.25 / $4.25. Usar `muse-spark-1.3-contributor` en desarrollo |
| Muse Voice Transcribe | `muse-voice-transcribe-1.0` | Transcribe lo que el jugador dice al negociar | $0.18 por hora de audio |
| Muse Image | `muse-image-1.0` | Retratos y escenas en pixel art | $0.01 por imagen. Pre-generar y cachear |

Variable de entorno `GAME_MASTER_MODEL` para cambiar entre Wonder y Spark sin tocar código.

### Modelos open-weight (CONDICIONAL: solo si aprueban créditos de Baseten o Modal)

**Puerta de decisión: miércoles 28 de octubre.** Si para entonces no hay créditos aprobados en
Baseten o Modal, esta sección se descarta y todo sigue en la API de Meta.

Si hay créditos:

| Parte del juego | Modelo | Dónde corre |
| --- | --- | --- |
| Personajes en la negociación (vendedor, prestamista, amigo del "negocio") | Muse Spark Open | Baseten o Modal |
| Diálogos secundarios y resumen del Modo Clase | Muse Glimmer | Baseten o Modal |
| Game master | Muse Wonder | API de Meta |
| Voz e imágenes | Voice Transcribe e Image | API de Meta |

- Implementar un **segundo proveedor** en `app/providers/open_weights.py` con la misma interfaz
  que `app/providers/meta.py`. Si el endpoint del socio es compatible con OpenAI, reutilizar
  el mismo SDK con otro `base_url` y otra llave.
- Variable `NPC_PROVIDER` con valores `meta_api` u `open_weights`, para cambiar sin tocar código.
- **Si el proveedor abierto falla, volver automáticamente a la API de Meta.** El juego no se cae.
- Registrar en `model_calls` qué proveedor respondió, para demostrarle al socio que el
  proyecto corre en un modelo abierto elegible.
- Ventaja extra: la parte de más volumen (la negociación) deja de gastar los $150 de la API
  de Meta, lo que protege la demo durante la evaluación, desde la entrega del 2 de noviembre.

## Contrato entre Unity y el backend

Unity solo habla con estos endpoints. Los modelos Pydantic son la fuente de verdad;
exportar el esquema OpenAPI y generar o escribir a mano las clases C# equivalentes.

| Método y ruta | Qué hace |
| --- | --- |
| `POST /api/sessions` | Crea una partida. Opcional: `classCode` para Modo Clase. Devuelve `sessionId` |
| `POST /api/sessions/{id}/event` | Recibe el estado financiero del mes y devuelve un `GameEvent` |
| `POST /api/sessions/{id}/decisions` | Registra la decisión tomada y el estado resultante (telemetría) |
| `POST /api/sessions/{id}/negotiation` | Recibe audio (o texto de respaldo) y devuelve un `NegotiationReply` |
| `GET /api/images/{key}` | Devuelve un retrato o escena cacheada; la genera solo si no existe |
| `POST /api/sessions/{id}/summary` | Genera el resumen final con Muse Spark |
| `GET /api/classes/{code}/dashboard` | Métricas agregadas del grupo para el panel del profesor |
| `GET /api/health` | Estado y presupuesto restante |

### `PlayerState` (lo envía Unity, lo calcula el motor C#)

```json
{
  "month": 3,
  "salaryNet": 450000,
  "cash": 120000,
  "savings": 50000,
  "emergencyFund": 0,
  "creditCard": { "balance": 180000, "limit": 300000, "monthlyRatePct": 3.5, "minPaymentPct": 10 },
  "installments": [{ "item": "Celular", "monthly": 25000, "monthsLeft": 33 }],
  "informalLoans": [],
  "history": ["paid_minimum_only", "skipped_savings"],
  "wellbeing": 62
}
```

### `GameEvent` (lo devuelve el game master, validado)

```json
{
  "eventId": "evt_moto_dañada",
  "type": "emergency",
  "title": "Se te dañó la moto",
  "narrative": "Texto corto en español tico, máximo 280 caracteres.",
  "npc": { "id": "mecanico_don_rafa", "name": "Don Rafa", "role": "mechanic" },
  "imageKey": "scene_taller_mecanico",
  "choices": [
    {
      "id": "pay_cash",
      "label": "Pagar con efectivo",
      "effects": [{ "kind": "expense", "amount": 85000, "source": "cash" }]
    },
    {
      "id": "card",
      "label": "Pasarlo a la tarjeta",
      "effects": [{ "kind": "expense", "amount": 85000, "source": "credit_card" }]
    }
  ],
  "teachingConcept": "emergency_fund"
}
```

- `type` es un enum cerrado: `income`, `expense`, `emergency`, `offer`, `scam`, `macro_news`, `bonus`.
- `effects[].kind` es un enum cerrado que el motor C# sabe aplicar: `expense`, `income`,
  `new_installment`, `new_informal_loan`, `savings_change`, `wellbeing_change`, `price_inflation`.
- **El backend recorta los montos a rangos permitidos por tipo de evento** antes de responder
  (por ejemplo, una emergencia entre ₡20 000 y ₡250 000). El motor C# vuelve a validar.
- `teachingConcept` es un enum: `budget`, `emergency_fund`, `compound_interest`,
  `minimum_payment`, `installments_total_cost`, `informal_lending`, `pyramid_scheme`,
  `inflation`, `exchange_rate`, `payroll_deductions`.

### `NegotiationReply`

```json
{
  "transcript": "¿Cuánto pago en total con los seguros?",
  "npcReply": "Mae, la cuota es nada... bueno, con el seguro son ₡31 500 al mes.",
  "revealedFacts": ["insurance_fee", "total_cost"],
  "keyQuestionsAsked": ["total_cost", "hidden_fees"],
  "dealStatus": "open"
}
```

- El vendedor tiene una **ficha oculta con la verdad** (precio de contado, cuota, plazo,
  seguros, comisiones) generada y calculada por el motor, no por el modelo. El modelo solo
  decide cómo revelarla según las preguntas del jugador.
- `keyQuestionsAsked` alimenta la métrica de impacto: ¿el jugador hizo las preguntas correctas?
- Siempre ofrecer **entrada de texto como respaldo** si el micrófono falla.

## Game master: diseño del prompt

- **Instrucciones fijas** (system prompt) que no cambian entre llamadas, para aprovechar la
  caché de entrada ($0.20 por millón en vez de $4): reglas del mundo, tono tico, enums
  permitidos, rangos de montos y el formato JSON exacto.
- **Entrada variable:** `PlayerState` resumido y los últimos eventos para no repetirlos.
- **Reglas narrativas:** reaccionar a la situación del jugador (tarjeta al límite →
  prestamista; ahorro alto → "negocio" piramidal; diciembre → aguinaldo y marchamo).
  Nunca sermonear: mostrar la consecuencia, no decir "estuvo mal".
- **Eventos de respaldo:** un archivo `fallback_events.json` con 15 a 20 eventos escritos a
  mano. Si el modelo falla, tarda más de 8 segundos o se acaba el presupuesto, se usa uno.

## Game master agéntico (SOLO SI DA TIEMPO, para Best Agentic Use-Case)

**Puerta de decisión: jueves 29 de octubre.** Solo se empieza si ese día ya hay un mes
jugable de punta a punta con la negociación por texto funcionando. Si no, se descarta.

La versión base del game master hace una llamada por mes. La versión agéntica:

1. **Usa herramientas (tool calling)** conectadas a funciones deterministas del backend:
   - `get_player_history(session_id)`: decisiones y eventos anteriores.
   - `simulate_purchase(price, months, rate, insurance)`: costo total real de una compra a cuotas.
   - `project_debt(balance, rate, payment, months)`: cómo evoluciona una deuda.
   - `get_concepts_not_yet_taught(session_id)`: qué conceptos faltan por enseñar.
2. **Planifica la temporada:** al empezar, arma un plan de eventos para el año según el perfil
   del jugador y lo ajusta mes a mes según sus decisiones. El plan se guarda en `sessions`.
3. **Vendedor con objetivo propio:** en la negociación, el vendedor es un agente que intenta
   maximizar su comisión dentro de reglas fijas (no puede mentir sobre cifras, solo omitirlas
   hasta que le pregunten). Usa `simulate_purchase` para responder con números exactos.

Reglas: máximo de pasos por llamada (5), timeout total de 10 s, y si algo falla se usa la
versión base de una sola llamada. Las herramientas nunca escriben el estado del jugador;
solo lo leen o calculan.

## Voz en Unity

- **El `Microphone` de Unity no funciona en builds WebGL.** En WebGL, grabar con un plugin
  `.jslib` que use `MediaRecorder` del navegador y devuelva el audio a C#. En escritorio,
  usar `Microphone`.
- Limitar la grabación a 15 segundos. Enviar el archivo al backend, nunca directo a Meta.
- Verificar en la documentación de Meta qué formatos de audio acepta la transcripción y
  convertir en el backend si hace falta.

## Imágenes

- **Pre-generar** un set base (retratos de los NPCs fijos y escenas comunes) con un script
  `scripts/pregenerate_images.py` y guardarlas como assets. Muse Image en tiempo real solo
  para eventos especiales, con caché por `imageKey`.
- Prompt de estilo fijo para mantener consistencia: pixel art, paleta limitada, mismo encuadre.

## Control de costos (obligatorio)

Créditos: $150 en la API de Meta por persona. Una partida completa con Wonder cuesta
alrededor de $0.75; con Spark, alrededor de $0.15.

- `app/cost_guard.py`: estima el costo de cada llamada con los precios oficiales y acumula el
  gasto diario en la base de datos.
- `DAILY_BUDGET_USD` (variable de entorno). Al llegar al 80%, el game master pasa
  automáticamente a Muse Spark. Al 100%, solo eventos de respaldo.
- Límite por sesión: máximo de llamadas por partida y por IP.
- Caché de respuestas idénticas.
- En desarrollo, `GAME_MASTER_MODEL=muse-spark-1.3-contributor`.

## Base de datos (Neon)

| Tabla | Campos principales |
| --- | --- |
| `classes` | `code`, `name`, `created_at` |
| `sessions` | `id`, `class_code`, `started_at`, `finished_at`, `final_state` (JSONB) |
| `decisions` | `session_id`, `month`, `event_id`, `teaching_concept`, `choice_id`, `state_after` (JSONB) |
| `negotiations` | `session_id`, `transcript`, `key_questions_asked`, `deal_status` |
| `model_calls` | `model`, `endpoint`, `input_tokens`, `output_tokens`, `estimated_cost_usd`, `latency_ms`, `ok` |
| `images` | `key`, `url`, `created_at` |

`model_calls` sirve para controlar el gasto y para mostrar en el video cuánto cuesta una partida.

## Modo Clase

- El profesor crea una clase y recibe un código corto. Los estudiantes lo ingresan al empezar.
- El panel muestra, por concepto: porcentaje que eligió la opción riesgosa (por ejemplo, pagar
  solo el mínimo), cuántos detectaron la trampa en la negociación y deuda promedio al cierre.
- Las cifras se calculan con SQL. **Muse Spark solo redacta** la explicación en lenguaje claro
  a partir de esas cifras; no inventa números.

## Motor financiero (C#, en Unity)

- Clase pura sin dependencias de Unity (`FinanceEngine`) para poder probarla con tests de
  EditMode.
- Cubre: salario neto (rebajo de CCSS y renta con los porcentajes vigentes, verificados en
  fuentes oficiales), intereses de tarjeta sobre saldo, pago mínimo, cuotas, aguinaldo en
  diciembre, marchamo, préstamos informales y fondo de emergencia.
- Aplica los `effects` del `GameEvent` y rechaza los que estén fuera de rango.
- Tests obligatorios: interés compuesto de tarjeta pagando solo el mínimo, costo total de una
  compra a cuotas con seguro, aguinaldo, saldo nunca negativo sin deuda asociada.

## Organización del repositorio

```
colon-quest/
  CLAUDE.md
  README.md
  server/            # Keylor: FastAPI, proveedores, motor de costos, migraciones, tests
  unity/             # Jefferson: proyecto Unity 2D
  shared/            # Esquemas JSON exportados del backend (contrato)
  scripts/           # Pre-generación de imágenes, seed de clases de prueba
  docs/              # Arquitectura, decisiones, resultados de la prueba piloto
```

- `main` solo tiene versiones estables; `develop` es la rama de integración y la rama por defecto.
- Ramas por ticket de Jira creadas desde `develop` (`feat/SCRUM-12-game-master`,
  `feat/SCRUM-30-negotiation-ui`), PR pequeños a `develop`. A `main` solo llegan PRs de release
  desde `develop` (`release: vX.Y.Z`) o `hotfix/*`. Convenciones completas en `CONTRIBUTING.md`.
- Cada uno trabaja en su carpeta para no pisarse. Cambios al contrato en `shared/` se avisan.
- Commits todos los días: el historial demuestra que el proyecto se hizo durante el hackathon.

## Plan por fases

Lo marcado como **[SI DA TIEMPO]** o **[CONDICIONAL]** se hace solo si se cumple su puerta de
decisión. Si no, ese tiempo va a pulir lo obligatorio.

Cada fase es un sprint de Jira (proyecto `SCRUM`). Detalle en `docs/workflow.md`.

| Sprint y fechas | Keylor (backend, IA, motor) | Jefferson (Unity, arte, flujo) | Listo cuando |
| --- | --- | --- | --- |
| **Sprint 1: Foundations** (lun 26 y mar 27 oct) | Repo, FastAPI, proveedor de Meta, prueba de cada modelo (Wonder JSON, transcripción en español tico, una imagen). Esquemas Pydantic y contrato en `shared/`. Neon y migración base. Pedir créditos de Baseten, Modal y Fireworks | Proyecto Unity 2D, mapa cenital con 3 estaciones (cajero, tienda, casa), movimiento y colisiones | Cada modelo responde desde el backend; el personaje camina entre estaciones |
| **Sprint 2: Playable Month** (mié 28 y jue 29 oct) | Game master con validación, recorte de montos y eventos de respaldo. Control de costos. Motor financiero C# con tests. **[CONDICIONAL, decidir el 28]** proveedor open-weight para los personajes | Paneles overlay: alerta SINPE, estado de cuenta, ventana de decisión. Conexión al endpoint de eventos | Un mes completo jugable de punta a punta |
| **Sprint 3: Negotiation & Deploy** (vie 30 y sáb 31 oct) | Negociación por texto y voz: transcripción, ficha oculta del vendedor, `keyQuestionsAsked`. Imágenes pre-generadas. Telemetría. Despliegue en Render. **[SI DA TIEMPO, decidir el 29]** game master con herramientas | Grabación de voz (jslib en WebGL), panel de negociación, retratos. Build WebGL desplegado | La negociación funciona en el build web desplegado (voz, o texto como respaldo) |
| **Sprint 4: Pilot & Submission** (dom 1 y lun 2 nov) | **Prueba piloto con estudiantes** (mañana del 1). Modo Clase y panel del profesor si da tiempo. Correcciones, métricas para el video, README, publicar en la galería | Pulido, sonido, pantalla final. Video de 3 min para los jueces y clip vertical de 30 a 45 s para X | **Proyecto entregado el 2 de noviembre** |
| Después de la entrega (hasta el 8 nov) | Vigilar gasto y errores de la demo en vivo | Campaña en X | Post en X con la mayor interacción posible |
| Hasta el 16 nov | Campaña de votos de la comunidad | Campaña de votos de la comunidad | Votos en la galería pública |

## Campaña para Most Viral y Community Vote (no es código)

- **Cuenta de X:** crearla o reactivarla ya. Publicar avances cortos durante el hackathon
  (un GIF del personaje, el vendedor tico, el panel SINPE) para llegar al 2 de noviembre con
  algo de audiencia.
- **Clip para X (30 a 45 s, vertical):** la negociación con el vendedor que intenta meter el
  seguro escondido. Español con subtítulos en inglés. Terminar con el link a la demo y a la
  galería. Mencionar las cuentas oficiales del hackathon.
- **Publicar el 2 o 3 de noviembre**, apenas se entregue, y moverlo en grupos de la UNA, comunidades tech de Costa
  Rica, y con los estudiantes de la prueba piloto.
- **Community Vote:** pedir votos con el link directo a la galería. Mensaje: un juego
  costarricense compitiendo en un hackathon mundial de Meta.
- **Fecha límite del viral:** 8 de noviembre, 11:59 p. m. hora del Este (10:59 p. m. en Costa Rica).

## Riesgos conocidos

- **Latencia del game master:** mostrar una animación mientras responde; timeout de 8 s con
  evento de respaldo.
- **Micrófono en WebGL:** probarlo el 30 de octubre como máximo (inicio del Sprint 3); el respaldo de texto debe existir siempre.
- **Unity WebGL pesado:** comprimir assets y probar la carga en una conexión lenta.
- **Alcance:** si algo se atrasa, se recorta en este orden:
  1. Game master agéntico **[SI DA TIEMPO]**.
  2. Proveedor open-weight **[CONDICIONAL]**.
  3. Modo Clase.
  4. Imágenes en tiempo real (quedan las pre-generadas).
  5. Voz en la versión web (queda el texto; la voz se muestra en el video desde escritorio).

  El mes jugable y la negociación por texto **no se recortan nunca**.
- **Créditos:** verificar en los Credit Terms cuándo vencen; la demo debe seguir funcionando
  desde la entrega del 2 de noviembre y durante toda la evaluación.

## Comandos

```bash
# Backend
cd server
uv sync
uv run alembic upgrade head
uv run uvicorn app.main:app --reload
uv run pytest
uv run ruff check . && uv run mypy app
```

Variables de entorno (`server/.env`, nunca en el repo): `MODEL_API_KEY`, `DATABASE_URL`,
`GAME_MASTER_MODEL`, `DAILY_BUDGET_USD`, `ALLOWED_ORIGINS`.

<p align="center"><a href="BENCHMARKS.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="README.es.md"><img src="docs/assets/tab-readme-es.svg" alt="README"></a>
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
  <a href="RUN_AND_BUILD.es.md"><img src="docs/assets/tab-run-es.svg" alt="Correr y armar, para los jueces"></a>
</p>

<p align="center">
  <img src="docs/assets/benchmarks-es.svg?v=2" width="100%" alt="La latencia y el costo de cada modelo de Meta en ColonQuest, medidos con llaves reales, y las decisiones que salieron de ahí. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
</p>

<details open>
<summary><b>Leer esta página como texto</b></summary>

## Benchmarks y decisiones sobre los modelos

Cómo le tocó su trabajo a cada modelo de Meta en ColonQuest. Medimos cada modelo con llaves reales el 28 y el
29 de octubre de 2026, con llamadas sueltas desde Costa Rica, cronometradas por el backend de la solicitud a la
respuesta. El registro completo, con cada muestra, está en la [ADR 0004](docs/adr/0004-model-roles.md) (en inglés).

### En un minuto

- **El simulador espera 8 segundos por el evento del mes**, y después usa uno escrito a mano. Un modelo más lento
  que eso nunca llega al usuario.
- **Solo Muse Spark responde siempre dentro de los 8 s** (3,0 a 4,4 s con esfuerzo `minimal`), así que es el
  director de la simulación en vivo.
- **Muse Wonder es el que mejor escribe, pero tarda 5,8 a 10,5 s**, así que solo corre donde nadie espera:
  planificando tu año en segundo plano y escribiendo tu final.
- **Muse Spark Open en Baseten es el más rápido** (1,9 a 2,8 s) y se paga con créditos de Baseten, así que le da
  voz a cada personaje con el que hablás.
- **Muse Image tarda 14 a 17 s por imagen**, así que cada imagen se dibuja una vez antes y viene en el zip.

### El evento del mes: qué modelo y qué esfuerzo

El prompt del director de la simulación, unos 1 600 tokens de entrada.

| Modelo | Esfuerzo de razonamiento | Muestras | Tokens de salida | Costo por evento |
| --- | --- | --- | --- | --- |
| Muse Wonder | `low` | 14,6 s, 18,3 s | unos 1 190 | unos $0,028 |
| Muse Wonder | `minimal` | 5,8 s, 6,7 s, 7,2 s, 10,5 s | 500 a 570 | unos $0,011 |
| Muse Spark 1.3 | `low` | 5,1 s, 6,5 s, 7,7 s | 1 260 a 2 030 | $0,008 a $0,011 |
| **Muse Spark 1.3** | **`minimal`** | **3,0 s, 3,4 s, 3,9 s, 4,4 s** | **530 a 770** | **unos $0,004** |
| Cualquiera | `none` | la API lo rechaza | | no existe |

**Decisión:** Muse Spark con esfuerzo `minimal` en cada llamada que el usuario espera (`LIVE_REASONING_EFFORT`).

### Cada función de punta a punta

Lo que tardó cada función pasando por el backend real, con el proveedor y el modelo que usa hoy.

| Función | Proveedor y modelo | Tiempo | Qué respondió |
| --- | --- | --- | --- |
| Evento del mes | API de Meta, Muse Spark `minimal` | 2,5 a 3,8 s | Un evento válido |
| Director de la simulación con agente | API de Meta, Muse Spark `minimal` | 1,4 a 4,2 s | Llamó `get_player_history` y `get_concepts_not_yet_taught` antes de escribir |
| Planificador del año | API de Meta, Muse Wonder `minimal`, en segundo plano | 6,2 a 16,7 s | Un plan de 12 meses; el primer evento nunca lo espera |
| Negociación | Baseten, Muse Spark Open | 1,9 a 2,8 s | Las cifras exactas de la ficha oculta (₡19 600, ₡3 500, ₡569 400) |
| Vendedor agente con su herramienta | Baseten, Muse Spark Open | 3,3 a 5,3 s | Volvió a cotizar con `quote_term` para empujar 60 meses |
| Voz | API de Meta, Muse Voice Transcribe | 0,7 a 1,5 s | "cuánto pago en total con el seguro y la comisión" |
| Historia del final | API de Meta, Muse Wonder `minimal` | 5,2 a 7,6 s | Escrita detrás de una pantalla de carga de 25 s |
| Reporte del Modo Clase | API de Meta, Muse Spark `minimal` | 2,2 a 2,7 s | Una explicación clara de cifras calculadas en SQL |
| Imagen de 512 × 512 | API de Meta, Muse Image | 13,8 a 17,4 s | Dibujada una vez y en el zip; nunca se genera mientras jugás |

### Lo que aprendimos a la mala

- **Todos los modelos de Meta razonan antes de responder**, y esos tokens cuentan contra `max_tokens`. Con 700
  tokens el resumen llegaba vacío, y el simulador usaba el respaldo sin avisar. Ahora cada llamada deja 2 000
  tokens o más.
- **Baseten gastó de 363 a 1 435 tokens razonando un prompt de una línea.** Con 1 500 tokens la negociación
  llegaba vacía siempre. Con `reasoning_effort: low` y 3 072 tokens más, responde en 1,9 a 2,8 s.
- **`reasoning_effort: none` no existe** en la API de Meta; `minimal` es el más bajo.
- **Muse Image ignora el tamaño pedido** y devuelve PNG de 1 600 × 1 600 de 3 a 4 MB, así que el backend achica
  cada uno a 512 píxeles y 256 colores antes de guardarlo.

### Lo que cuesta una simulación

Costos por llamada tomados de la tabla `model_calls` con los precios oficiales.

| Llamada | En una ruta guiada | Costo por llamada | Subtotal |
| --- | --- | --- | --- |
| Evento del mes, Muse Spark | 3 | unos $0,004 | $0,012 |
| Plan del año, Muse Wonder | 1 | unos $0,015 | $0,015 |
| Historia del final, Muse Wonder | 1 | unos $0,008 | $0,008 |
| Personajes, Muse Spark Open en Baseten | unos 8 turnos | unos $0,0025 | $0,020 |
| Voz, Muse Voice Transcribe | un minuto más o menos | $0,18 la hora | $0,003 |
| Imágenes, Muse Image | dibujadas una vez | $0,01 cada una | $0 por simulación |
| **Total** | | | **unos $0,06** |

| Modelo | Precio por millón de tokens (entrada / en caché / salida) |
| --- | --- |
| Muse Wonder | $4 / $0,20 / $20 |
| Muse Spark 1.3 | $1,25 / $0,15 / $4,25 |
| Muse Spark Open en Baseten | $1,25 / $0,15 / $4,25 |

Muse Image cuesta $0,01 por imagen y Muse Voice Transcribe $0,18 por hora de audio.

### Las decisiones, y lo que descartamos

| Decisión | Por qué | Lo que descartamos |
| --- | --- | --- |
| Muse Spark es el director de la simulación en vivo | El único siempre dentro de los 8 s | Muse Wonder en vivo: 5,8 a 10,5 s aun con `minimal` |
| Muse Wonder planifica el año y escribe el final | El que mejor escribe, donde nadie espera | Usarlo en cada evento |
| Los personajes corren en Muse Spark Open en Baseten | El más rápido, con presupuesto aparte, y un modelo abierto en el centro del simulador | Modal: había que desplegar y mantener el modelo, con arranques en frío |
| Cada imagen se dibuja antes | 14 a 17 s es demasiado para esperar | Dibujar en vivo, aun con caché |
| Escribir siempre sirve junto a la voz | Un micrófono puede fallar | La voz como única entrada |
| Cada modelo tiene respaldo | El simulador nunca se puede detener | Mostrar un error |

Si Baseten falla, tarda más de 5 s o llega a su propio presupuesto diario, los personajes pasan a Muse Spark en la
API de Meta, y `model_calls` registra qué proveedor respondió. Las otras decisiones, en inglés:
[ADR 0001](docs/adr/0001-meta-api-contracts.md) (los contratos con la API de Meta),
[ADR 0002](docs/adr/0002-open-weights-npcs.md) (modelos abiertos en Baseten),
[ADR 0003](docs/adr/0003-agentic-game-master.md) (los agentes).

### Medilo vos

Con tu llave en `server/.env`, desde `server/`:

```bash
uv run python scripts/smoke_models.py --audio pregunta.wav --image-out muse.png
```

Llama una vez a Muse Wonder, Muse Spark, Muse Voice Transcribe y Muse Image, e imprime el tiempo, los tokens y el
costo de cada llamada; cuesta unos centavos. `--audio` recibe cualquier clip WAV corto y se puede omitir. En el
simulador, **L** muestra cada llamada de tu simulación con su modelo, proveedor, tiempo y costo.

</details>

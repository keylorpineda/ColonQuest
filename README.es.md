<p align="center"><a href="README.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
  <a href="RUN_AND_BUILD.es.md"><img src="docs/assets/tab-run-es.svg" alt="Correr y armar, para los jueces"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-es.svg?v=2" width="100%" alt="ColonQuest, un simulador de finanzas personales que se juega como un juego, de Costa Rica para el mundo. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
</p>

<details>
<summary><b>Leer esta página como texto</b></summary>

## ColonQuest

**From Costa Rica to the World.** Un simulador de finanzas personales que se juega como un juego:
vivís tres meses con un salario mínimo de Costa Rica, mientras te tratan de vender cosas. Hecho por
DoWhile Team para el Meta Global AI Developer Hackathon 2026 (track Indie). Vos decidís, Muse Spark
narra, el motor en C# calcula los colones exactos y negociás con la voz. La IA cuenta la historia;
el motor lleva las cuentas.

### El problema

En Costa Rica el 87% de los adultos tiene alguna deuda y destina en promedio el 34% de su ingreso a
pagarla, el 58,3% entre quienes ganan menos de ₡500 000 al mes. El 80,88% de los encuestados por la
Universidad Nacional nunca recibió educación financiera. Nadie te enseña a hacer la pregunta correcta
mientras un vendedor te sonríe. Esa es la habilidad que entrena ColonQuest, contra cuatro trampas reales:

- **Cuotas:** solo una cuota al mes, y un seguro escondido.
- **Pago mínimo:** pagás poquito y la deuda sigue creciendo.
- **Gota a gota:** plata rápida hoy, y cobradores en la puerta. El OIJ contó 2 930 casos en 2025.
- **Pirámide:** traé tres amigos y te hacés rico. Seguro.

Cada cifra con su fuente: [La situación en Costa Rica](COSTA_RICA.es.md).

### Cómo se juega un mes

Te llega el salario con el rebajo de la CCSS y la renta. Decidís qué pagar, ahorrar o pedir
prestado. Muse Spark escribe un evento hecho para tu situación y el motor en C# lo aplica en
colones enteros. Cuando aparece un vendedor, negociás en voz alta: el precio, la tasa, el seguro y
la comisión reales solo salen si preguntás.

### Jugalo en unos tres minutos

Elegí **Simulación guiada**, lo primero del menú. Es el juego real en un tramo corto del pueblo,
durante tres meses, y vos solo tomás las decisiones:

1. **Mes 1.** Decís cómo llegás a fin de mes. Te cae el salario por SINPE, Muse Wonder escribe tu año
   en segundo plano y el game master trae un evento hecho para tu situación.
2. **Mes 2.** Una flecha te lleva a la tienda, donde el vendedor se calla lo que no le preguntás.
   Hablá o escribí.
3. **Mes 3.** Lo que sigue depende de tu plata: si te falta, el prestamista y luego los cobradores; si
   te sobra, una pirámide. Podés preguntarles antes de firmar.

El panel de la derecha muestra cada llamada real mientras pasa, leída del servidor: modelo, proveedor,
tiempo y costo. Se oculta con **L**. La partida termina con un informe hecho con los números del motor:
lo que tenés, lo que debés, tu plata mes a mes, tus notas y una imagen elegida por lo que pasó en tu partida. Los objetivos, la flecha y las marcas del
mapa funcionan igual en el **Modo historia** completo, donde la gente de la calle también te habla con
Muse Spark Open.

### Probá cada función en dos minutos

Elegí **Demo de funciones** en el menú para llegar a cada función por aparte, con el mismo panel de
llamadas reales.

| Estación | Qué probar | Modelos |
| --- | --- | --- |
| Game master | Elegí la situación de un jugador, luego una opción, y mirá cómo la aplica el motor | Muse Spark con herramientas |
| Negociación | Hablale o escribile a un vendedor y abrí la ficha oculta línea por línea | Muse Spark Open, Muse Voice Transcribe |
| Cobros y trampas | Hablá con el cobrador, el prestamista y el de la pirámide, y abrí lo que esconden | Muse Spark Open, Muse Voice Transcribe |
| Plan y cierre | Compará los planes de dos perfiles y leé cómo terminó un año | Muse Wonder |
| Motor y controles | Tratá de meter un monto fuera de rango y mirá los presupuestos en vivo | Ninguno, a propósito |
| Modo Clase | Corré una clase de prueba real con tres estudiantes simulados, marcados como tales | Muse Spark |

Ocupa internet, porque los modelos corren en el backend. Un recorrido completo cuesta unos $0,10.

### Criterios del jurado

| Criterio | Peso | Qué construimos |
| --- | --- | --- |
| Impacto | 30% | Cifras de Costa Rica con fuente, el salario mínimo y el tope de usura reales en el motor, telemetría por decisión, Modo Clase para profesores |
| Herramientas de Meta | 25% | Cinco modelos de Meta con un rol claro cada uno, más agentes con herramientas |
| Demo | 25% | Una ruta guiada de tres meses y una demo de funciones en el menú; cada llamada a un modelo tiene tiempo límite y respaldo |
| Profundidad técnica | 20% | Contratos validados, un motor exacto y más de 1 300 pruebas: NUnit del motor y pytest del servidor corren en CI, las de Unity en local |

### Modelos de Meta

| Modelo | Rol |
| --- | --- |
| Muse Spark | Game master en vivo: el evento de cada mes, con herramientas; reportes del Modo Clase |
| Muse Wonder | Planifica tu año en segundo plano y escribe el resumen final |
| Muse Spark Open (Baseten) | Los vendedores, con tácticas de comisión y memoria de lo que preguntaste; los cobradores, el prestamista, el de la pirámide y la gente del pueblo |
| Muse Voice Transcribe | Negociás en voz alta; escribir siempre sirve |
| Muse Image | Retratos, escenas de los eventos y la imagen de tu final, elegida por lo que pasó en tu partida; pre-generados y sin texto |

Cada rol sale de latencias medidas: solo Spark responde siempre dentro de los 8 s del juego, así
que Wonder, el que mejor escribe, solo corre donde no estás esperando. Ver
[ADR 0004](docs/adr/0004-model-roles.md).

### Agentes

Detrás de banderas de configuración, ver [ADR 0003](docs/adr/0003-agentic-game-master.md) (en
inglés).

- **Game master:** usa herramientas de solo lectura como `simulate_purchase` y `project_debt`, así
  las consecuencias que narra usan números reales.
- **Planificador:** arma el recorrido del año y repasa los conceptos en los que el jugador falló.
- **Vendedor:** empuja plazos más largos para ganar más comisión, pero solo puede decir cifras de
  su ficha oculta o de sus herramientas.

### Arquitectura

Un cliente Unity 2D (motor financiero en C#) se comunica por JSON con un backend FastAPI (contratos
Pydantic v2, control de costos, agentes). El backend llama a la API de modelos de Meta, guarda sesiones
y decisiones en Postgres y corre los personajes en Muse Spark Open en Baseten, con respaldo en Meta si
falla o se acaba su presupuesto diario.

- Ningún monto, tasa ni saldo sale de un modelo.
- La plata es un número entero de colones.
- Toda respuesta de un modelo se valida y tiene respaldo.
- Toda llamada pasa por un control de costos diario.
- Las llaves de API viven solo en el backend.

### Accesibilidad

| Área | Estado | Qué significa |
| --- | --- | --- |
| Voz opcional | Listo | Cada turno de negociación acepta texto |
| Preguntas de un clic | Listo | Las preguntas clave son botones; escribir o hablar es opcional |
| Lenguaje claro | Listo | Textos cortos en inglés sencillo o en español tico cotidiano |
| Sin bloqueos | Listo | Si un modelo falla, entra contenido escrito a mano |
| Página legible | Listo | Esta versión en texto, y la tarjeta se queda quieta si se pide menos movimiento |
| Teclado | En progreso | Movimiento y paneles sin usar el mouse |
| No solo color | En progreso | Cada color lleva también una etiqueta o un ícono |
| Texto ajustable | Pendiente | Letra más grande en paneles y diálogos |

### Correrlo y armarlo

El juego lee a qué backend hablarle de `colonquest.env`, y el backend lee sus llaves de `server/.env`.
Cambiálos y armá desde ahí; las llaves nunca van dentro del ejecutable. La guía completa:
[Correr y armar](RUN_AND_BUILD.es.md).

```bash
cd server
cp .env.example .env
uv sync
uv run alembic upgrade head
uv run python -m app.dev
```

```bash
cp colonquest.env.example colonquest.env
python scripts/build_game.py
```

Las pruebas se corren con `uv run pytest` en `server/`.

El trabajo se registra en Jira (proyecto `SCRUM`); leé [CONTRIBUTING.md](CONTRIBUTING.md) y
[SECURITY.md](SECURITY.md). Las tarjetas se generan con `python scripts/build_readme_art.py`.

### Equipo

DoWhile Team: Keylor Pineda y Jefferson Abarca, hecho en equipo para el Meta Global AI Developer
Hackathon 2026. From Costa Rica to the World. Publicado bajo la [licencia MIT](LICENSE).

</details>

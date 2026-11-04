<p align="center"><a href="README.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="https://github.com/keylorpineda/ColonQuest/releases/latest"><img src="docs/assets/tab-download-es.svg" alt="Descargar el simulador para Windows"></a>
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
  <a href="RUN_AND_BUILD.es.md"><img src="docs/assets/tab-run-es.svg" alt="Correr y armar, para los jueces"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-es.svg?v=6" width="100%" alt="ColonQuest, un simulador de finanzas personales, de Costa Rica para el mundo. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
</p>

<details>
<summary><b>Leer esta página como texto</b></summary>

## ColonQuest

**From Costa Rica to the World.** Un simulador de finanzas personales:
vivís tres meses con un salario mínimo de Costa Rica, mientras te tratan de vender cosas. Hecho por
DoWhile Team para el Meta Global AI Developer Hackathon 2026 (track Indie). Vos decidís, Muse Spark
narra, el motor en C# calcula los colones exactos y negociás con la voz. La IA cuenta la historia;
el motor lleva las cuentas.

### El problema

| Cifra | En Costa Rica | Fuente |
| --- | --- | --- |
| **34%** | del ingreso mensual se va en pagar deudas, en promedio | [OCF, 2025](https://delfino.cr/2025/10/aunque-los-costarricenses-comprometen-en-promedio-menos-dinero-para-el-pago-de-deudas-el-nivel-de-endeudamiento-es-alto-para-buena-parte-de-la-poblacion) |
| **58,3%** | para quienes ganan menos de ₡500 000 (unos $1 000) al mes | Misma encuesta |
| **80,88%** | de los encuestados nunca recibió educación financiera | [Universidad Nacional, 2024](https://www.elfinancierocr.com/economia-y-politica/una-cartera-muy-estrecha-menos-del-10-de-las/22FABMYC6ZE47JR73C2UYYMBJU/story) |
| **2 930** | casos de gota a gota contados por los investigadores en 2025 | [OIJ, 2026](https://semanariouniversidad.com/pais/oij-registra-mas-de-2-000-casos-vinculados-a-prestamos-gota-a-gota-en-lo-que-va-de-2026/) |

### Por qué educación financiera: la ley de usura y su vacío

1. **La gente debía más de lo que podía pagar.** Hasta 2020 Costa Rica no tenía un máximo legal de
   interés, y nada impedía que los rebajos por planilla dejaran a un trabajador debajo del mínimo para
   vivir. Ese año, los hogares destinaban el 52% de su ingreso a pagar deudas ([OCF](https://delfino.cr/2025/10/aunque-los-costarricenses-comprometen-en-promedio-menos-dinero-para-el-pago-de-deudas-el-nivel-de-endeudamiento-es-alto-para-buena-parte-de-la-poblacion)).
2. **La ley de usura respondió.** La Ley 9859, contra la usura crediticia, rige desde junio de 2020. Fija
   una tasa anual máxima para todo préstamo, tarjeta y microcrédito, que el Banco Central recalcula cada
   seis meses (36,48% en colones para el segundo semestre de 2026). Quien presta debe revisar la deuda
   total de la persona en el Centro de Información Crediticia de la SUGEF antes de prestar, y los rebajos
   por planilla ya no pueden tocar el salario mínimo inembargable ([CRHoy](https://crhoy.com/nacionales/que-es-la-ley-de-usura-y-por-que-genera-debate-en-la-campana-presidencial/),
   [Delfino](https://delfino.cr/2020/09/tesoreria-nacional-no-admitira-deducciones-por-planilla-a-salarios-inferiores-a-199-760)).
   Para 2025 la parte del ingreso que se iba en deudas había bajado al 34%.
3. **También dejó gente afuera.** Con un tope, los bancos le prestan menos a quien parece riesgoso. La
   OCDE dice que el tope está causando exclusión financiera que empuja al crédito informal, y las
   microfinancieras estiman unas 60 000 personas excluidas desde 2020 ([El Observador](https://observador.cr/tasa-de-usura-esta-causando-exclusion-financiera-en-costa-rica-y-aumenta-los-creditos-informales-advierte-la-ocde/)).
   El debate sigue abierto: en 2024 había más tarjetas de crédito que en 2019, y ningún estudio prueba que
   la exclusión alimentara a los prestamistas informales ([El Financiero](https://www.elfinancierocr.com/finanzas/ley-de-usura-levanta-posiciones-encontradas/UBJQZ373BJGQDBISGMWT25SCGA/story/)).
4. **El gota a gota llenó el vacío, fuera de la ley.** Los prestamistas informales cobran hasta 20% por
   semana o por mes, muchas veces el tope legal, y mezclan usura, extorsión y violencia ([ONU Costa Rica, 2025](https://observador.cr/onu-prestamos-gota-a-gota-combinan-usura-extorsion-y-violencia-en-costa-rica/)).
   Las denuncias por extorsión ligadas a préstamos informales pasaron de 47 en 2020 a 428 en 2024. De
   quienes acudieron a uno, el 40% necesitaba la plata con urgencia y el 32% supuso que el banco le diría
   que no; solo al 14% se lo habían negado de verdad ([OIJ, en El Financiero](https://www.elfinancierocr.com/finanzas/ley-de-usura-levanta-posiciones-encontradas/UBJQZ373BJGQDBISGMWT25SCGA/story/)).
5. **Una ley puede topar una tasa; no puede enseñar la pregunta.** El mercado legal tiene techo, el ilegal
   está a un mensaje de distancia, y 8 de cada 10 personas nunca recibieron educación financiera. Por eso
   el país aprobó una ley de educación financiera en 2025, y ese es el vacío que entrena ColonQuest: saber
   el costo total, reconocer una tasa por encima del tope y decir que no antes de firmar.

Nadie te enseña a hacer la pregunta correcta mientras un vendedor te sonríe. Esa es la habilidad que entrena
ColonQuest, contra cuatro trampas reales:

- **Cuotas:** solo una cuota al mes, y un seguro escondido.
- **Pago mínimo:** pagás poquito y la deuda sigue creciendo.
- **Gota a gota:** plata rápida hoy de un prestamista informal, y cobradores en la puerta.
- **Pirámide:** traé tres amigos y te hacés rico. Seguro.

Explicaciones claras, palabras locales y hasta dónde confiar en cada cifra: [La situación en Costa Rica](COSTA_RICA.es.md).

### Cómo se simula un mes

Te llega el salario con el rebajo de la CCSS y la renta. Decidís qué pagar, ahorrar o pedir
prestado. Muse Spark escribe un evento hecho para tu situación y el motor en C# lo aplica en
colones enteros. Cuando aparece un vendedor, negociás en voz alta: el precio, la tasa, el seguro y
la comisión reales solo salen si preguntás.

### Probalo en unos tres minutos

Corre en tu propia computadora con tu propia llave de Meta; no hay ningún servicio en línea. [Bajá el simulador para Windows](https://github.com/keylorpineda/ColonQuest/releases/latest),
descomprimilo, hacé doble clic en `start_colonquest.bat` y pegá tu llave cuando te la pida. Instala lo que necesita,
levanta el backend local y abre el simulador; el primer arranque tarda un minuto ([detalles](RUN_AND_BUILD.es.md#1-evaluarlo-con-tu-propia-llave),
[preguntas](RUN_AND_BUILD.es.md#preguntas-que-puede-tener-un-juez)). Elegí **Simulación guiada**,
lo primero del menú. Es el simulador real en un tramo corto del pueblo,
durante tres meses, y vos solo tomás las decisiones:

1. **Mes 1.** Decís cómo llegás a fin de mes. Te cae el salario por SINPE, Muse Wonder escribe tu año
   en segundo plano y el director de la simulación trae un evento hecho para tu situación.
2. **Mes 2.** Una flecha te lleva a la tienda, donde el vendedor se calla lo que no le preguntás.
   Hablá o escribí.
3. **Mes 3.** Lo que sigue depende de tu plata: si te falta, el prestamista y luego los cobradores; si
   te sobra, una pirámide. Podés preguntarles antes de firmar.

El panel de la derecha muestra cada llamada real mientras pasa, leída del servidor: modelo, proveedor,
tiempo y costo. Se oculta con **L**. La simulación termina con un informe hecho con los números del motor:
lo que tenés, lo que debés, tu plata mes a mes, tus notas y una imagen elegida por lo que pasó en tu simulación. Los objetivos, la flecha y las marcas del
mapa funcionan igual en el **Modo historia** completo, donde la gente de la calle también te habla con
Muse Spark Open.

### Probá cada función en dos minutos

Elegí **Demo de funciones** en el menú para llegar a cada función por aparte, con el mismo panel de
llamadas reales.

| Estación | Qué probar | Modelos |
| --- | --- | --- |
| Director de la simulación | Elegí la situación de un usuario, luego una opción, y mirá cómo la aplica el motor | Muse Spark con herramientas |
| Negociación | Hablale o escribile a un vendedor y abrí la ficha oculta línea por línea | Muse Spark Open, Muse Voice Transcribe |
| Cobros y trampas | Hablá con el cobrador, el prestamista y el de la pirámide, y abrí lo que esconden | Muse Spark Open, Muse Voice Transcribe |
| Plan y cierre | Compará los planes de dos perfiles y leé cómo terminó un año | Muse Wonder |
| Motor y controles | Tratá de meter un monto fuera de rango y mirá los presupuestos en vivo | Ninguno, a propósito |
| Modo Clase | Corré una clase de prueba real con tres estudiantes simulados, marcados como tales | Muse Spark |

Necesita el backend local con tu llave de Meta, porque los modelos corren ahí. Un recorrido completo cuesta unos $0,10.

### Criterios del jurado

| Criterio | Peso | Qué construimos |
| --- | --- | --- |
| Impacto | 30% | Cifras de Costa Rica con fuente, el salario mínimo y el tope de usura de la Ley 9859 reales en el motor, telemetría por decisión, Modo Clase para profesores |
| Herramientas de Meta | 25% | Cinco modelos de Meta con un rol claro cada uno, más agentes con herramientas |
| Demo | 25% | Una ruta guiada de tres meses y una demo de funciones en el menú; cada llamada a un modelo tiene tiempo límite y respaldo |
| Profundidad técnica | 20% | Contratos validados, un motor exacto y más de 1 300 pruebas: NUnit del motor, pytest del servidor y EditMode de Unity corren en CI con cada cambio |

### Categorías en las que participamos

| Categoría | Qué pide | Dónde está en ColonQuest |
| --- | --- | --- |
| **Track Indie** | Un proyecto evaluado por impacto, uso de herramientas de Meta, demo y profundidad técnica | Los [criterios del jurado](#criterios-del-jurado) de arriba, y una ruta guiada de tres meses que se prueba en tres minutos |
| **Best Build on Meta Open Weights** | Una parte central del proyecto corriendo en Muse Spark Open o Muse Glimmer | El centro del simulador es hablar con gente que quiere tu plata, y cada una corre en Muse Spark Open en Baseten: vendedores, cobradores, el prestamista, el de la pirámide y la gente del pueblo. Cada llamada registra su proveedor |
| **Best Agentic Use-Case** | Agentes que deciden y ejecutan tareas de varios pasos | Tres agentes: el director de la simulación consulta cuatro herramientas de solo lectura antes de escribir (hasta 5 pasos en 10 s), el planificador arma tu año y lo ajusta a tus decisiones, y el vendedor vuelve a cotizar con una herramienta para empujar el plazo que más le deja. Ver [Agentes](#agentes) |
| **Best Creative Build** | Un proyecto original hecho con Muse Image o Muse Wonder | Un pueblo y un elenco costarricenses originales. Muse Image dibuja los retratos, las escenas y diez finales distintos; Muse Wonder planifica tu año y escribe cómo terminó |
| **Most Viral Project** y **Community Vote** | La elección del público | El clip de la demo en X y los votos en la galería pública |

### Modelos de Meta: quién hace qué y dónde corre

Cinco modelos de Meta, un trabajo cada uno. Cuatro corren en la **API de modelos de Meta**
(`https://api.meta.ai/v1`). El modelo abierto, **Muse Spark Open, corre en Baseten**
(`https://inference.baseten.co/v1`), y ahí vive cada personaje con el que hablás. Ningún monto, tasa ni
saldo sale de ellos: el motor en C# y el backend calculan cada cifra, y los modelos solo eligen cómo
contarla.

| Modelo e ID | Dónde corre | Qué hace en el simulador | Cuándo se llama | Tiempo medido |
| --- | --- | --- | --- | --- |
| **Muse Spark** `muse-spark-1.3` | API de Meta | El director de la simulación en vivo: escribe el evento de cada mes para tu situación, después de consultar herramientas de solo lectura. También redacta el reporte del Modo Clase a partir de cifras de SQL | Cada mes, mientras esperás | 1,4 a 4,4 s |
| **Muse Wonder** `muse-wonder-1.0` | API de Meta | Planifica tu año (qué conceptos y en qué orden) y escribe la historia de tu reporte final. También puede escribir eventos de respaldo fuera de línea | En segundo plano después del primer evento y al final de la simulación; nunca en una llamada en vivo | 5 a 17 s |
| **Muse Spark Open** `meta-models/muse-spark-open-1026` | **Baseten** | Cada personaje con el que hablás: los vendedores que esconden el seguro y la comisión hasta que preguntás, los cobradores, el prestamista gota a gota, el de la pirámide y la gente del pueblo. El vendedor es un agente que vuelve a cotizar con una herramienta | Cada turno de una conversación | 1,9 a 5,3 s |
| **Muse Voice Transcribe** `muse-voice-transcribe-1.0` | API de Meta | Pasa a texto lo que decís, para que negocies en voz alta. Escribir siempre sirve | Cada turno hablado | 0,7 a 1,5 s |
| **Muse Image** `muse-image-1.0` | API de Meta | Retratos, escenas de los eventos y las diez imágenes del final, elegidas por los números del motor. Ninguna imagen lleva texto ni números | Pre-generadas una vez y dentro del zip; nunca mientras jugás | 14 a 17 s cada una |

Si Baseten falla, tarda más de 5 s o llega a su propio presupuesto diario, los personajes pasan a Muse
Spark en la API de Meta; si un modelo falla del todo, responde contenido escrito a mano. El panel de la
derecha del simulador y la tabla `model_calls` muestran qué modelo y qué proveedor respondió cada
llamada, con su tiempo y su costo. Cada rol sale de latencias medidas: solo Spark responde siempre
dentro de los 8 s del simulador, así que Wonder solo corre donde no estás esperando
([ADR 0004](docs/adr/0004-model-roles.md) y [ADR 0002](docs/adr/0002-open-weights-npcs.md), en inglés).

### Agentes

Encendidos por defecto en el backend local, cada uno con su propio interruptor en `server/.env`; si un agente
falla, responde la versión de una sola llamada, y si tarda demasiado, un evento escrito a mano. Ver [ADR 0003](docs/adr/0003-agentic-game-master.md) (en inglés).

| Agente | Modelo y proveedor | Qué decide | Sus herramientas |
| --- | --- | --- | --- |
| **Director de la simulación** | Muse Spark, API de Meta | Qué evento le sirve a tu mes y cómo contarlo, después de ver tu historial y el costo real de cada opción | `get_player_history`, `get_concepts_not_yet_taught`, `simulate_purchase`, `project_debt` |
| **Planificador** | Muse Wonder, API de Meta, en segundo plano | El recorrido de tu año: qué concepto va cuándo, y repasa los que te costaron | Tu perfil y tus decisiones hasta ahora |
| **Vendedor** | Muse Spark Open, Baseten | Qué plazo empujar para ganar más comisión, sin dejar de responder con la verdad cuando preguntás | `quote_term`, que vuelve a cotizar la oferta a otro plazo con la fórmula del motor |

Las herramientas solo leen o calculan; ninguna escribe tu estado. Cada monto que dice un agente sale de
la ficha oculta o de una herramienta, y el evento igual pasa por el mismo control de rangos que cualquier otro.

### Arquitectura

Un cliente Unity 2D (motor financiero en C#) se comunica por JSON con un backend FastAPI (contratos
Pydantic v2, control de costos, agentes). El backend llama a la API de modelos de Meta, guarda sesiones
y decisiones en SQLite o Postgres y corre los personajes en Muse Spark Open en Baseten, con respaldo en Meta si
falla o se acaba su presupuesto diario. El diagrama, el camino de un evento, las protecciones y el costo
de una simulación (unos $0,06) están en [Arquitectura](docs/architecture.md), en inglés.

- Ningún monto, tasa ni saldo sale de un modelo.
- La plata es un número entero de colones.
- Toda respuesta de un modelo se valida y tiene respaldo.
- Toda llamada pasa por un control de costos diario.
- Las llaves de API viven solo en el backend.

### Correrlo y armarlo

Todo corre en tu computadora con tus propias llaves. `start_colonquest.bat` instala uv si falta, te pide la llave
de Meta, levanta el backend local (SQLite, sin base de datos que instalar) y abre el simulador. Las llaves se quedan
en `server/.env` y nunca van dentro del ejecutable. En otros sistemas, `python scripts/run_local.py` hace lo mismo.
La guía completa: [Correr y armar](RUN_AND_BUILD.es.md).

```bash
start_colonquest.bat
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

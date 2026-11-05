<p align="center"><a href="README.md">English</a> | <b>Español</b></p>

<p align="center">
  <a href="https://github.com/keylorpineda/ColonQuest/releases/latest"><img src="docs/assets/tab-download-es.svg" alt="Descargar el simulador para Windows"></a>
  <a href="COSTA_RICA.es.md"><img src="docs/assets/tab-costa-rica-es.svg" alt="La situación en Costa Rica, con fuentes"></a>
  <a href="RUN_AND_BUILD.es.md"><img src="docs/assets/tab-run-es.svg" alt="Correr y armar, para los jueces"></a>
  <a href="BENCHMARKS.es.md"><img src="docs/assets/tab-benchmarks-es.svg" alt="Benchmarks y decisiones sobre los modelos"></a>
</p>

<p align="center">
  <img src="docs/assets/readme-es.svg?v=9" width="100%" alt="ColonQuest, un simulador de finanzas personales, de Costa Rica para el mundo. Todo el contenido de esta tarjeta está disponible como texto en la sección de abajo.">
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

### Probalo

Corre en tu propia computadora con tu propia llave de Meta; no hay ningún servicio en línea. [Bajá el simulador para Windows](https://github.com/keylorpineda/ColonQuest/releases/latest),
descomprimilo, hacé doble clic en `start_colonquest.bat` y pegá tu llave cuando te la pida. Instala lo que necesita,
levanta el backend local y abre el simulador; el primer arranque tarda un minuto ([detalles](RUN_AND_BUILD.es.md#1-evaluarlo-con-tu-propia-llave),
[preguntas](RUN_AND_BUILD.es.md#preguntas-que-puede-tener-un-juez)).

### Tres formas de entrar, y por qué existe cada una

El menú principal ofrece tres entradas al mismo simulador. Comparten el motor financiero, los modelos y el backend; lo
que cambia es cuánto del pueblo recorrés y cuánto tiempo necesitás.

| Entrada del menú | Qué es | Tiempo | Por qué la hicimos |
| --- | --- | --- | --- |
| **Simulación guiada** | El simulador real, tres meses, en un tramo corto del pueblo. Vos solo tomás las decisiones | Unos 3 minutos | Para que un juez o un estudiante vea una simulación completa, del primer salario al reporte final, sin aprenderse el pueblo antes |
| **Demo de funciones** | Cada función de IA en su propia pantalla, a un clic, con lo que hizo el modelo al lado | Unos 2 minutos | Para que un juez revise cada modelo, agente y control directamente, sin tener que llegar a él dentro de una simulación |
| **Modo historia** | El mundo abierto completo, con tres partidas guardadas | Lo que quieras | Como lo juega un estudiante de verdad: turnos de trabajo, alquiler, carro, finca, todo el pueblo |

Si tenés pocos minutos, empezá por **Simulación guiada** y después abrí **Demo de funciones**.

### Simulación guiada: una simulación completa en tres minutos

Elegís cómo llegás a fin de mes y el motor pone tu plata inicial. Los dos perfiles ganan el mismo salario: el mínimo de
2026 con el rebajo de la CCSS y la renta.

| Perfil | Efectivo | Ahorro | Saldo de tarjeta |
| --- | --- | --- | --- |
| "Llego justo y con la tarjeta cargada" | ₡40 000 | ₡0 | ₡240 000 |
| "Tengo algo ahorrado" | ₡120 000 | ₡400 000 | ₡40 000 |

1. **Mes 1.** Te cae el salario por SINPE. Muse Wonder empieza a escribir tu año en segundo plano, y el director de la
   simulación trae un evento hecho para tu situación, con opciones cuyo costo calcula el motor.
2. **Mes 2.** Una flecha te lleva a la tienda. El vendedor te dice una cuota bajita y se calla el seguro, la comisión
   y el total hasta que preguntás. Hablá o escribí, o usá los botones de preguntas.
3. **Mes 3.** Lo que viene depende de tu plata después de dos meses de tus propias decisiones, no del perfil que
   elegiste: si no te alcanza para un salario, el prestamista gota a gota, y los cobradores si firmás; con plata de
   sobra, un amigo con una pirámide. Les podés preguntar lo que quieras antes de decidir.

La simulación termina con un reporte hecho con los números del motor: lo que tenés, lo que debés, tu plata mes a mes,
tus notas, las preguntas clave que hiciste y una imagen elegida por lo que pasó.

Para que dure tres minutos, la ruta guiada detiene el reloj, quita el tránsito y la policía, abre solo las puertas de
su recorrido y nunca toca tus partidas guardadas. Todo lo demás es igual que en el modo historia.

**En qué fijarse:** el vendedor nunca miente en una cifra, pero se calla lo que no le preguntás; el costo total solo
aparece después de la pregunta correcta; el motor rechaza un monto fuera de rango; y el panel de la derecha muestra
cada llamada real mientras pasa, leída del servidor, con su modelo, su proveedor (API de Meta o Baseten), su tiempo y
su costo. Apretá **L** para esconderlo.

### Demo de funciones: cada pieza de IA, a un clic

Cada estación le manda una solicitud real al backend y muestra la respuesta al lado de por qué ese modelo tiene ese
trabajo.

| Estación | Qué hacés | Qué muestra | Modelo y proveedor |
| --- | --- | --- | --- |
| Director de la simulación | Elegís la situación de un usuario y después una opción | El evento que escribe el modelo, las herramientas que llamó y cómo el motor aplica la opción | Muse Spark con herramientas, API de Meta |
| Negociación | Le hablás o escribís a un vendedor y después abrís la ficha oculta | El vendedor solo dice el seguro y la comisión si le preguntás; la ficha se abre línea por línea | Muse Spark Open en Baseten, Muse Voice Transcribe |
| Cobros y trampas | Le hablás al cobrador, al prestamista y al de la pirámide | Cada uno tiene su personalidad y solo dice lo incómodo si le preguntás | Muse Spark Open en Baseten, Muse Voice Transcribe |
| Plan y cierre | Comparás los planes de dos perfiles y leés cómo terminó un año | El plan que Muse Wonder escribe en segundo plano y la historia de un final | Muse Wonder, API de Meta |
| Motor y controles | Intentás meter un monto fuera de rango | El motor lo rechaza o lo recorta, y los presupuestos diarios en vivo | Ninguno, a propósito |
| Modo Clase | Creás una clase de prueba real con tres estudiantes simulados, marcados como tales | El panel del profesor: cifras de SQL que el modelo explica en palabras claras | Muse Spark, API de Meta |

Necesita el backend local con tu llave de Meta, porque los modelos corren ahí. Un recorrido completo cuesta unos $0,10.

### Modo historia: todo el pueblo

La simulación libre: tres meses de cuatro días cada uno, con turnos de trabajo en la soda, alquiler, recibos, banco,
cooperativa, finca, agencia de carros, hospital y comisaría. La gente de la calle también te habla con Muse Spark Open.
Los mismos objetivos, la flecha y las marcas del mapa de la ruta guiada te dicen qué sigue.

### Todo lo que tiene el simulador

ColonQuest es una simulación gamificada: vivís los primeros meses por tu cuenta de un joven costarricense, en un pueblo
que recorrés caminando, y cada colón sigue las reglas reales. Esto es lo que trae.

**Tu plata, con las reglas reales.** El motor financiero en C# cuenta en colones enteros y aplica:

- El salario mínimo de 2026, con el rebajo de la CCSS (10,83%) y la renta, pagado por SINPE.
- Efectivo, una cuenta de ahorro en el banco y un fondo de emergencia.
- Una tarjeta de crédito al 3% mensual, con su pago mínimo, su estado de cuenta e intereses sobre el saldo.
- Compras a cuotas con el seguro y la comisión que el vendedor no menciona.
- Préstamos gota a gota con sus cobradores, y una pirámide que les paga a los primeros con la plata de los últimos.
- Recibos de luz, agua, internet y teléfono que se acumulan si no los pagás, hasta que te cortan la luz.
- Alquiler o hipoteca, un carro con prima y cuotas, una finca para invertir en café o ganado, y muebles de contado o a
  cuotas.
- El aguinaldo y el marchamo de diciembre, para las simulaciones que llegan a diciembre.

**Un pueblo para vivir.** Unos 17 lugares, cada uno una decisión de plata: la casa de tu mamá, tu cuarto alquilado,
la Soda El Muelle donde trabajás, el banco con sus cajeros y su ventanilla, la cooperativa, la finca, la tienda, la
agencia de carros, la inmobiliaria, el hospital, la comisaría, la pulpería, el taller de Don Rafa, los cursos del INA y
la UNA, un puesto de préstamos rápidos y un stand de "inversiones". Cada día es una semana, de 6:00 a 22:00, con
tránsito, gente caminando, un perro, botes, gaviotas, la playa, la noche con las luces de la calle y sonido hecho en
código.

**Trabajo y estudio.** Tu primer trabajo tiene reloj marcador: si llegás tarde, primero te avisan y después te
rebajan. En las tardes podés llevar cursos en el INA o la UNA para crecer. También podés cocinar en casa para ahorrar un
poco.

**Un celular (P).** Avisos de SINPE, tu banco, tus recibos, metas (un fondo de emergencia, bajar la tarjeta, amueblar
tu cuarto, ahorrar, comprar una moto), un buscador que explica cada idea de plata en palabras claras, una
calculadora de préstamos, un juego de bloques y un casino que apuesta tu efectivo de verdad.

**Gente que quiere tu plata.** Vendedores que esconden los costos hasta que preguntás, cobradores en la acera, un
prestamista gota a gota cuya tasa, cuando preguntás, es muchas veces el tope legal, un amigo con un negocio seguro,
vecinos que conversan y un desconocido que te pide que le mandés plata. Todos hablan con Muse Spark Open, por voz o
por escrito, y ninguno puede inventar una cifra.

**Consecuencias.** Si te metés en una casa ajena, el dueño llama a la policía: de uno a tres días en una celda, o la
fianza. Un carro te puede atropellar: el hospital y tu seguro de salud deciden cuánto cuesta. Si no pagás los recibos,
te cortan la luz. Si firmás con el prestamista, llegan los cobradores.

**Guía, para que nadie se pierda.** Una escena de inicio (cumplís 18, tu mamá te da un regalo, el celular te trae tu
primer trabajo), una lista de tareas (Tab), objetivos con flecha y marcas en el mapa, un minimapa y el mapa completo
(M), subtítulos, un HUD que se puede esconder (H), el panel de llamadas (L), pausa, tres partidas guardadas, inglés o
español, y ajustes de sonido.

**El cierre de una simulación.** Un reporte con los números del motor: lo que tenés y lo que debés, tu plata mes a
mes, notas de la A a la D en presupuesto, fondo de emergencia, deudas, cuotas e inversiones, las preguntas clave que
hiciste, lo que compraste y una de diez imágenes elegida por cómo te fue.

**Para profesores.** Modo Clase: el profesor le da un código al grupo y ve, por concepto y sin datos personales, quién
eligió la opción riesgosa, quién detectó la trampa en la negociación y la deuda promedio al cierre. Las cifras salen de
SQL; Muse Spark solo las explica.

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
| **Community Vote** | La elección del público | Los votos en la galería pública del hackathon |

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
dentro de los 8 s del simulador, así que Wonder solo corre donde no estás esperando. Cada medición, lo que cuesta
una simulación y lo que descartamos: **[Benchmarks y decisiones sobre los modelos](BENCHMARKS.es.md)**.

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

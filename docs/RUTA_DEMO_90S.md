# Ruta de la demo en 90 segundos

> **Actualizado el 1 de noviembre.** La ruta principal ya existe en el juego: es **Simulación guiada** (Guided simulation),
> la primera opción del menú, y dura unos 3 minutos jugando. Lo que sigue es el recorrido alterno por la Demo de funciones,
> que ahora tiene siete estaciones: se agregó **Cobros y trampas** (Debts and traps) después de la negociación.

Para el evaluador que ejecuta el juego en local. Cada paso dice qué tocar, qué debe ver y qué prueba.
Los nombres de botones y textos salen del código de `DemoLab` y `DemoScenarios` (revisado el 1 de nov). El juego arranca en
inglés; el texto entre comillas es la clave en español y, si aparece, su versión en inglés.
**No se ejecutó el juego para escribir esta guía**: los tiempos de los modelos son los medidos en el ADR 0004 y pueden variar.

## Antes de empezar (10 s)

1. Internet activo: los modelos corren en el backend.
2. Backend despierto: `GET https://colon-quest-api.onrender.com/api/ping` responde sin tocar la base ni los modelos.
3. Micrófono opcional: todo se puede escribir. Si no hay micrófono, el botón "Hablar" muestra "No pude usar el micrófono, escribilo."

## La ruta

| Tiempo | Dónde | Qué hacer | Qué debe ver | Qué prueba |
| --- | --- | --- | --- | --- |
| 0:00 a 0:05 | Menú principal | Elegir **"Demo de funciones"** (Feature demo) | Seis estaciones a la izquierda y un panel de evidencia a la derecha | Que no hay que jugar un año para ver cada función |
| 0:05 a 0:30 | Estación 1, **Game master** (Muse Spark con herramientas) | Elegir **"Tarjeta al límite"** (Card at the limit): mes 4, tarjeta casi llena (₡285 000 de ₡300 000), ₡38 000 en efectivo, sin ahorro. Esperar "Muse Spark está leyendo la situación" (3 a 4,4 s). Tocar una de las opciones | Un evento escrito para esa situación y una tabla **Antes / Después** con Efectivo, Ahorros, Saldo de la tarjeta, Cuotas por pagar, Préstamos informales y Bienestar | La IA cuenta la historia y el motor C# aplica los colones. El panel de la derecha muestra modelo, proveedor, latencia, tokens, costo y herramientas |
| 0:30 a 0:55 | Estación 2, **Negociación** (Muse Spark Open y Voice Transcribe) | Elegir el producto **"Celular"**. Tocar **"¿Tiene cargos escondidos?"** y luego **"¿Cuánto pago en total?"** (o pulsar "Hablar" y decirlo) | El vendedor suelta el seguro. Se abre la **"Ficha oculta del vendedor"** línea por línea: Precio de contado, Cuota mensual, Plazo, Tasa de interés, Seguro, Comisión, Costo total. Abajo, **"Preguntas clave que hiciste"** | El vendedor solo revela lo que se le pregunta. Las cifras vienen del motor, no del modelo |
| 0:55 a 1:10 | Estación 3, **Plan y cierre** (Muse Wonder) | Elegir **"Perfil endeudado"** y luego **"Perfil ahorrador"** | Un plan de meses con su concepto a enseñar y, abajo, **"Cómo terminó el año"**: "Año con deudas" frente a "Año sin deudas", con su patrimonio | La temporada se adapta al perfil. Muse Wonder puede tardar de 6 a 10 s: es normal, trabaja donde el jugador no espera |
| 1:10 a 1:20 | Estación 5, **Motor y controles** | Tocar **"Emergencia de ₡9 000 000"** y luego **"Emergencia de ₡85 000"** | El primero dice **Rechazado**, el segundo **Aceptado**. Más abajo: "En un año pagaste X, y Y fueron solo intereses" | El motor recorta y rechaza lo que se sale de las reglas, sin tocar modelos |
| 1:20 a 1:30 | Estación 6, **Modo Clase** | Tocar **"Simular una clase"** | Estudiantes, Partidas cerradas, "Detectaron la trampa", Deuda promedio y quiénes eligieron la opción riesgosa por concepto. Rotulado: "Datos de esta simulación, no de estudiantes reales." | Lo que ve un profesor, con cifras calculadas y explicadas por Muse Spark |

La estación 4, **Arte del juego** (Muse Image), queda fuera de los 90 s: se puede abrir al final para ver las imágenes pregeneradas, sin texto, sin costo en vivo.

## Qué esperar de los tiempos

- Si cada respuesta del modelo llega en menos de 8 s, la ruta cabe en unos 90 s de acciones del evaluador.
- Si un modelo tarda más, el juego usa su contenido de respaldo y sigue; el panel de la derecha indica quién respondió.
- Sin internet, el juego cae a contenido local: la ruta se completa, pero el panel no mostrará modelo, latencia ni costo reales.
- Una vuelta completa de las seis estaciones cuesta alrededor de $0,10.

## Si algo falla

| Síntoma | Qué hacer |
| --- | --- |
| La primera respuesta tarda mucho | El backend estaba dormido. Esperar y repetir; el `ping` anterior lo evita |
| El micrófono no responde | Escribir la pregunta en el cuadro "Escribí aquí..." y pulsar "Enviar" |
| La estación muestra respaldo | Los créditos de Meta pueden haber vencido (ventana de 10 días); la ruta sigue, con contenido local |
| "Rechazado" aparece en una opción normal | Es el motor aplicando un rango; elegir otra opción |

## Lo que falta confirmar

- Este recorrido sigue el **Demo de funciones**, que es lo que existe en el repositorio. La ruta guiada "Play in 90 seconds" que se mencionó no aparece en `develop` ni en las ramas remotas; si vive en otra parte, esta guía debe ajustarse a ella.
- Una versión realmente **autojugada** (que se reproduzca sola, sin que el evaluador toque nada) no existe todavía. Es un ticket aparte (el 17 de `TICKETS_DEMO_Y_VIDEO.md`).
- Los textos en inglés exactos de cada botón deben revisarse en el juego, porque aquí se citan las claves en español.

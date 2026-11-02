# Demo y video por perfil de evaluador

Decidido el 1 de noviembre de 2026. Perfiles elegidos: A (ingeniero o investigador de Meta),
C (impacto y educación), D (revisor masivo asíncrono) y E (jugador o diseñador creativo).
El perfil B (inversor) queda fuera: no se diseña nada para él, pero tampoco se le estorba.

## Honestidad sobre la base

Los jueces de este hackathon no están nombrados en ninguna fuente pública. Los perfiles son
inferidos de: la página del evento, las reglas (Reconscribe), hackatones previos de Meta y
Cerebral Valley, notas de jueces reales y guías de juzgamiento asíncrono. Confianza: D alta,
A, C y E media. El largo del video no está definido en las reglas leídas; el portal de envío
y las Official Rules completas piden login y no se leyeron.

Actualización: los patrones de los proyectos ganadores (usuario concreto, agente que trabaja,
wow de 10 s, funciona en vivo) están en [INVESTIGACION_EVALUADORES_Y_GANADORES.md](INVESTIGACION_EVALUADORES_Y_GANADORES.md).
De ahí sale el encuadre: "un agente de IA que entrena contra estafas financieras, en formato de juego".

## Quién prueba qué

Hay dos entregas separadas y cada perfil las usa distinto:

| Perfil | Ve primero | Prueba el app | Lo que decide su puntaje |
| --- | --- | --- | --- |
| D Revisor masivo | El video | Pocos minutos, y solo si el link abre | Que abra, que se entienda en 10 s, que no se rompa |
| A Meta | El video, luego el app | Busca debajo del capó | Roles por modelo, validación, costos, open weights |
| C Impacto | El video | Va al Modo Clase y al piloto | Evidencia con estudiantes, problema real |
| E Creativo | El video | Juega unos minutos | Que se sienta juego, personalidad, pulido |

## Demo: la "Judge Run" de 90 segundos

Un botón en el menú, "Play in 90 seconds", que no pide cuenta ni tutorial. Es el camino que
ve el 80% de los jueces. El Demo Lab queda como puerta aparte ("Under the hood") para A y C.

| Tiempo | Qué pasa | Perfil que sirve |
| --- | --- | --- |
| 0:00 | Botón en el menú, carga corta con una frase tica y un dato financiero | D |
| 0:05 | Apertura tipo película acortada (con opción de saltar) | E |
| 0:10 | Cae el salario, suena el SINPE, el contador de colones corre | E |
| 0:20 | El game master lanza un evento adaptado al jugador. Chip: modelo, segundos, costo | A |
| 0:35 | Llega el vendedor. Tres preguntas sugeridas en pantalla para que nadie se quede en blanco; voz o texto | D, E |
| 0:55 | Revelación: la ficha oculta se abre línea por línea, el precio de contado se tacha y la cuota real sube en una barra "lo prometido vs el costo real" | E, C |
| 1:10 | El motor aplica el efecto en colones enteros. Chip desplegable con el rastro de herramientas (`simulate_purchase`) | A |
| 1:20 | Tarjeta del mes: concepto aprendido, qué preguntas hizo bien | C |
| 1:30 | Dos botones: "How teachers see this" (Modo Clase) y "Under the hood" (Demo Lab) | C, A |

### Reglas que no se negocian (perfil D manda)

1. El link abre en incógnito en móvil y PC, sin login ni cuenta.
2. Indicador discreto "Live / Offline". Si el servidor tarda más de 8 s, entra el respaldo local sin pantalla de error.
3. Texto como respaldo siempre visible junto al botón de voz.
4. Keep-alive de Render activo para que no haya arranque en frío de 50 s.
5. Todo el contenido por defecto en inglés; el español queda en Ajustes.
6. Ninguna cifra sale de un modelo; el chip del perfil A lo demuestra.

### Qué ya existe y qué es nuevo

- Ya existe: Demo Lab con 6 estaciones y panel de evidencia, apertura tipo película, créditos, arranque en inglés, vendedor con ficha oculta, Modo Clase en el Demo Lab.
- Nuevo: botón y flujo Judge Run, preguntas sugeridas, animación de revelación con barra de costo real, chips de evidencia dentro de la escena, indicador Live/Offline, tarjeta de cierre del mes.

## Video

Largo no definido por las reglas. Propuesta: 2:30 en inglés (o español con subtítulos en inglés), 1080p.
Recortable a 90 s si el formulario pide menos.

| Tiempo | Contenido | Perfil |
| --- | --- | --- |
| 0:00 a 0:10 | Gancho completo: el vendedor suelta el seguro escondido, el jugador lo cacha y la cuota real sube en pantalla. Primera frase en voz: "ColonQuest teaches you to catch the hidden fee by talking to the seller" | D, E |
| 0:10 a 0:35 | Un mes jugado: salario, evento, decisión, consecuencia. Sin sermón | E |
| 0:35 a 1:05 | Negociación por voz real, con la transcripción y la respuesta | A, E |
| 1:05 a 1:35 | Arquitectura animada: 5 modelos, un trabajo cada uno por latencia, el motor C# que lleva las cuentas, el fallback a la API de Meta | A |
| 1:35 a 2:05 | Modo Clase y dato de la prueba piloto (N y porcentajes agregados, sin nombres ni rostros) | C |
| 2:05 a 2:30 | Postal del año, cierre y link a la demo | E, D |

Todo lo esencial va antes del segundo 30, aunque el juez vea el video completo.

Corte vertical de 30 a 45 s para X (negociación y revelación) se mantiene del plan original.

## Reparto

| Responsable | Tarea |
| --- | --- |
| Jefferson | Botón y flujo Judge Run, preguntas sugeridas, animación de revelación, contador de colones, tarjeta de cierre, indicador Live/Offline, chips en escena |
| Keylor | Datos de evidencia por llamada listos para el chip (modelo, latencia, costo, herramientas), keep-alive, presupuesto y créditos de Meta, datos del piloto para el Modo Clase |
| Ambos | Prueba en incógnito en móvil y PC, guion y grabación del video, README con la ruta de 90 s |

## Si falta tiempo, se recorta en este orden

1. Chips desplegables con rastro de herramientas (queda el chip simple).
2. Sacudida de cámara y sonidos extra.
3. Corte vertical para X.
4. Arquitectura animada (queda un diagrama estático).

No se recorta: link que abre, Judge Run con negociación, respaldo local, texto como alternativa a la voz.

## Pendientes que cambian el plan

- Largo máximo del video en el formulario de envío.
- Fecha de vencimiento de los créditos de Meta frente al juicio del 4 al 8 de noviembre.
- Si el micrófono por WebGL funciona en el build desplegado.
- Si hay datos reales del piloto (N y resultados) o se muestra la clase simulada rotulada.

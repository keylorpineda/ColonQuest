# Estimación: puntaje por rúbrica y probabilidad de ganar

Fecha: 1 de noviembre de 2026. Entrega acordada: link del repositorio; los evaluadores lo ejecutan en local.

## Aviso

Estos números son un **juicio del equipo, no un dato**. No hay puntajes publicados, los jueces no están nombrados,
no se conoce cuántos proyectos se enviarán y no se pudo leer el portal de envío. Sirven para priorizar trabajo, no para predecir.

## Hallazgo que condiciona todo

Ejecutar el simulador desde el repositorio exige hoy **abrir el proyecto con Unity 6000.3.27f1** (`unity/ProjectSettings/ProjectVersion.txt`).
El README solo explica cómo correr el backend; **no explica cómo correr el simulador**. No hay ninguna versión publicada (`gh release list` vacío).
Un juez que no tenga ese editor no puede probar nada. Esto aplasta el criterio de Demo si no se resuelve.

## Puntaje por criterio (0 a 100, cómo lo puntuaría un juez)

| Criterio | Peso | Hoy | Con las acciones | Razón de la nota de hoy |
| --- | --- | --- | --- | --- |
| Impacto | 30% | 72 (65 a 80) | 84 (80 a 88) | Problema documentado (OCF 2025), Modo Clase, accesibilidad. Resta: el piloto afirmado en el README no tiene datos en el repositorio |
| Herramientas de Meta | 25% | 84 (78 a 90) | 86 (82 a 90) | Cinco modelos con rol por latencia medida, Muse Spark Open en Baseten, agentes con herramientas, ADR. Resta: solo se aprecia si el juez logra correrlo |
| Demo | 25% | 55 (45 a 65) | 80 (75 a 85) | Respaldo local y Demo Lab. Resta: no hay build ni instrucciones, el video no está grabado, la ruta de 90 s no se ve en el repositorio |
| Profundidad técnica | 20% | 85 (80 a 90) | 88 (85 a 92) | Motor puro con cifras de 2026 correctas, 4 pruebas obligatorias, contratos Pydantic, CI. Resta: `double` en la cuota, conteo de pruebas impreciso |
| **Total ponderado** | 100% | **73 (66 a 81)** | **84 (80 a 89)** | |

Cálculo de hoy: 0,30 x 72 + 0,25 x 84 + 0,25 x 55 + 0,20 x 85 = 73,4.
Cálculo con acciones: 0,30 x 84 + 0,25 x 86 + 0,25 x 80 + 0,20 x 88 = 84,3.

### Qué es "las acciones"

1. Publicar un build de Windows y Mac como Release de GitHub, y un paso de ejecución en la primera pantalla del README.
2. Video de 2:30 grabado, con el gancho en los primeros 10 s, enlazado al principio del README.
3. Datos agregados del piloto en `docs/pilot/`, o quitar la frase del README.
4. Ruta de 90 s verificada y visible en el repositorio.
5. Correcciones del README, del motor y de CLAUDE.md (tickets 14 a 16).

## Probabilidad de ganar

Supuestos: la final en vivo la simulan 10 proyectos; el track Indie paga 5 premios (150, 100, 75, 50 y 25 mil dólares);
los ganadores documentados de eventos anteriores eran herramientas con usuario claro y demo que corre. El número de envíos
es desconocido: en eventos previos enviaron entre el 14% y el 45% de los asistentes (LlamaCon 44 de 238, Llama 3 51 de 354,
Opus 4.6 de Cerebral Valley 227 de 500). Con hasta 20 000 aceptados, la competencia podría ir de cientos a miles de envíos.

| Resultado | Hoy | Con las acciones |
| --- | --- | --- |
| Llegar a la final en vivo (top 10) | 5 a 10% | 20 a 30% |
| Premio del track Indie (top 5) | 2 a 5% | 8 a 15% |
| Primer lugar del track Indie | alrededor de 1% | 3 a 6% |

Cómo se lee: la diferencia entre "hoy" y "con las acciones" es el dato útil. Casi todo el salto viene de Demo (de 55 a 80) y de la
prueba del Impacto. Los rangos son anchos a propósito, porque dependen de cuántos proyectos compitan y de quién juzgue.

## Listado de acciones para subir las probabilidades

Las probabilidades no se "suben" solas: suben si se hacen estas acciones. Ordenadas por efecto en la rúbrica contra esfuerzo.
Los puntos son estimaciones nuestras.

| # | Acción | Responsable | Esfuerzo | Efecto en la rúbrica | Ticket |
| --- | --- | --- | --- | --- | --- |
| 1 | Publicar un build de Windows y Mac como Release de GitHub, con un paso de ejecución al inicio del README, y probarlo en una máquina limpia | Jefferson | Medio | Demo +12 a +18 | 18 |
| 2 | Grabar el video de 2:30 con el gancho en los primeros 10 s y enlazarlo arriba del README | Ambos | Medio | Demo +8 a +12 | 12 |
| 3 | Subir cifras agregadas del piloto a `docs/pilot/`, o quitar la frase "a student pilot" | Keylor | Bajo a medio | Impacto +8 a +12 | 11 |
| 4 | Publicar la ruta de 90 s (esta guía) y verificarla jugándola una vez | Ambos | Bajo | Demo +4 a +8 | 17 |
| 5 | Versión autojugada de la ruta, para el evaluador que no quiera tocar nada | Jefferson | Medio | Demo +3 a +6 | 17 |
| 6 | Corregir el README: encuadre de simulador, dato de la OCF (87% con deudas, 34% del ingreso), enlaces arriba | Ambos | Bajo | Impacto +2 a +4, Técnica +1 | 14 |
| 7 | Animación de revelación con barra de costo real | Jefferson | Medio | Demo e Impacto +2 a +5 | 7 |
| 8 | Chips de evidencia dentro de la escena (modelo, latencia, costo, herramientas) | Jefferson | Medio | Meta +2 a +4 | 10 |
| 9 | Declarar Muse Code, los cinco modelos y el costo por simulación (~$0,04) en el envío | Ambos | Bajo | Meta +2 | 13 |
| 10 | Quitar el `double` de la cuota y documentar los créditos fiscales | Keylor | Bajo | Técnica +2 a +4 | 15 |
| 11 | Actualizar CLAUDE.md (plazo del 4 de nov, premios sin confirmar, ventana de 10 días) | Keylor | Bajo | Credibilidad | 16 |
| 12 | Keep-alive de Render, créditos de Meta y prueba sin internet | Keylor | Bajo | Protege Demo | 2, 3, 4 |

### Probabilidades si se hacen

| Acciones hechas | Total estimado | Final en vivo (top 10) | Premio Indie (top 5) | Primer lugar |
| --- | --- | --- | --- | --- |
| Ninguna (hoy) | 73 | 5 a 10% | 2 a 5% | alrededor de 1% |
| 1 a 4 (lo esencial de Demo e Impacto) | 81 | 15 a 22% | 6 a 11% | 2 a 4% |
| 1 a 9 | 84 | 20 a 30% | 8 a 15% | 3 a 6% |
| 1 a 12 | 85 | 22 a 32% | 9 a 16% | 3 a 7% |

Lectura: las cuatro primeras acciones aportan casi todo el salto. Las demás pulen y protegen. Si el tiempo solo alcanza para una
cosa, es la 1: sin build que se pueda abrir, el resto no se ve.

## Sensibilidad: qué mueve más el resultado

| Palanca | Puntos de rúbrica que mueve | Esfuerzo |
| --- | --- | --- |
| Build descargable más README de un paso | Demo: +12 a +18 | Medio |
| Video de 2:30 con gancho en 10 s | Demo: +8 a +12 | Medio |
| Datos del piloto | Impacto: +8 a +12 | Bajo a medio (depende de que existan) |
| Ruta de 90 s verificada | Demo: +4 a +8 | Bajo si ya está hecha |
| Arreglos técnicos (`double`, README) | Técnica: +2 a +4 | Bajo |
| Animación de revelación | Demo e Impacto: +2 a +5 | Medio |

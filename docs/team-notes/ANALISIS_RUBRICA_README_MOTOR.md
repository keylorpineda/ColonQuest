# Análisis: rúbrica, motor financiero, README y CLAUDE.md

Fecha: 1 de noviembre de 2026. Lo que se revisó: [README.md](../../README.md), [CLAUDE.md](../../CLAUDE.md), el motor en
`unity/Assets/Scripts/Finance/`, sus pruebas, `server/tests/` y `.github/workflows/ci.yml`.
Contexto: [INVESTIGACION_EVALUADORES_Y_GANADORES.md](INVESTIGACION_EVALUADORES_Y_GANADORES.md) y
[DEMO_Y_VIDEO_POR_PERFIL.md](DEMO_Y_VIDEO_POR_PERFIL.md).

## 1. La rúbrica

Pesos del waiver (vía Reconscribe): Impacto 30, Herramientas de Meta 25, Demo 25, Profundidad técnica 20.
Lente del track Indie: creatividad, ambición técnica y si funciona. La demo se califica por "si funciona y aguanta
en vivo" y por la calidad del video. La profundidad técnica pregunta si la ingeniería es sólida, está bien refinada
y muestra "real craft". El largo del video no está definido.

## 2. Motor financiero

### Lo que está sólido

| Hallazgo | Evidencia |
| --- | --- |
| Biblioteca pura, sin Unity | 14 archivos, unas 2 570 líneas en `Finance/`, con su propio ensamblado |
| Dinero en enteros | `long` para colones; `decimal` solo para porcentajes (44 usos); redondeo explícito a medio hacia arriba (`RoundColones`) |
| Validación de rangos en cada entrada | `ContractLimits.MaxColones`, plazo 1 a 120, tasas 0 a 100; lanza excepción fuera de rango |
| Cargas sociales 2026 correctas | SEM 5,50, IVM 4,33, Banco Popular 1,00 = 10,83%. Coincide con las fuentes de 2026 que busqué (el IVM subió de 4,17 a 4,33 el 1 de enero de 2026) |
| Tramos de renta 2026 correctos | Exento hasta ₡918 000; 10% hasta ₡1 347 000; 15% hasta ₡2 364 000; 20% hasta ₡4 727 000; 25% arriba. Coincide con el decreto 45333-H citado por las fuentes |
| Aguinaldo según la Ley 2412 | Un doceavo de lo devengado, exento de cargas y de renta |
| Las 4 pruebas obligatorias de CLAUDE.md existen con nombre propio | `Mandatory1` interés compuesto pagando solo el mínimo a 12 meses; `Mandatory2` costo total de cuotas con seguro; `Mandatory3` aguinaldo; `Mandatory4` efectivo nunca negativo sin deuda asociada |
| Dos implementaciones cruzadas | `Mandatory2` compara el motor C# contra todas las ofertas de vendedor del backend (`seller_sheet.py`). Es un argumento fuerte de rigor para el perfil A |
| El CI prueba el motor sin licencia de Unity | El trabajo `Engine / NUnit tests` corre con `dotnet test` |

### Lo que conviene corregir o decir con precisión

| # | Hallazgo | Riesgo | Acción |
| --- | --- | --- | --- |
| M1 | La cuota francesa usa `double` y `Math.Pow` (`FinanceEngine.Obligations.cs`, líneas 74 y 75) y luego convierte a `decimal`. CLAUDE.md dice "nada de float para montos" | Bajo en la práctica, porque el resultado se redondea hacia arriba a 100 colones y está comparado con el backend. Un juez técnico que lea la regla y vea el `double` puede notarlo | Reemplazar por una potencia en `decimal` (bucle de multiplicación) y volver a correr `Mandatory2`. Es opcional, no bloquea |
| M2 | No hay créditos fiscales (hijos, cónyuge) en el impuesto al salario | Bajo; es una simplificación | Declararlo como simplificación en el README o el ADR |
| M3 | El CI de Unity (EditMode) se omite si no existe el secreto `UNITY_LICENSE` | El README dice "260+ tests in CI" y solo `dotnet` y `pytest` corren seguro | Decir exactamente qué corre en CI. Ver sección 3 |
| M4 | El motor incluye Granja, Vivienda, Vehículo, Mobiliario, Cartera e Incidentes, más allá del alcance original | Alcance amplio contra el consejo "angosto y terminado" | No agregar más; pulir lo que existe |

### Conteo de pruebas (sin ejecutar nada)

| Origen | Cantidad contada | Aviso |
| --- | --- | --- |
| C# (EditMode y PlayMode) | 841 atributos `[Test]`, `[TestCase]`, `[UnityTest]` | Cada `[TestCase]` cuenta como una; no es el número de ejecuciones |
| Python | 239 funciones `test_` en 43 archivos | La parametrización puede multiplicar las ejecuciones |

El README dice "260+". Es verdadero, pero subestima. No cambiarlo a otro número sin ejecutar y leer el resultado real.

## 3. README contra la rúbrica

| Pasaje del README | Problema | Criterio afectado | Acción |
| --- | --- | --- | --- |
| Primera frase del README | El equipo decidió que es un simulador de finanzas personales | Impacto, Demo | Hecho: el encuadre es de simulador |
| "Many young Costa Ricans make their first big money decisions alone" | Sin ninguna cifra | Impacto (30%) | Añadir la OCF 2025: 87% tiene alguna deuda y destina en promedio 34% de sus ingresos; CINPE y UNA: más del 80% sin educación financiera. Aclarar que el 87% mide cualquier deuda |
| Tabla de criterios: "a student pilot" | **No hay evidencia en el repositorio.** `docs/README.md` apunta a `docs/pilot/`, y esa carpeta no existe | Impacto, credibilidad | Subir cifras agregadas del piloto a `docs/pilot/`, o quitar la frase hasta tenerlas. Una afirmación sin respaldo daña a todo el documento |
| No hay enlace a la demo, al video ni a la galería al inicio | El perfil D busca el link primero | Demo (25%) | Poner "Play now" y el video en las primeras líneas |
| "Try every feature in two minutes" (Demo Lab) | Buena para los perfiles A y C, pero no es la ruta del revisor masivo | Demo | Añadir antes la ruta "Play in 90 seconds" cuando exista |
| "260+ tests in CI" | Impreciso (sección 2, M3) | Técnica | Escribir: motor con NUnit y backend con pytest en CI, más pruebas de Unity en local |
| Class Mode "a real test class of three simulated students, labelled as such" | Está bien: es honesto | Impacto | Mantener |
| Accesibilidad con estados "Done / In progress / Planned" | Está bien, pero "Keyboard use" y "Not by color alone" siguen en progreso | Impacto | Mantener o terminar si da tiempo |
| No se menciona Muse Code ni el costo por simulación | Los jueces de Meta lo buscan | Meta (25%) | Decirlo en el envío (ya está en el ticket 13) y poner el costo por simulación (~$0,04) |

## 4. CLAUDE.md: lo desactualizado o riesgoso

| Pasaje | Problema | Acción |
| --- | --- | --- |
| "Entrega: lunes 2 de noviembre... hora exacta por confirmar" | El plazo oficial es el 4 de nov, 9:00 PM ET (8:00 PM en Costa Rica); el 2 es solo el corte interno | Corregir el encabezado y las fases |
| Tabla de "Premios extra" con cinco premios nombrados | El texto oficial solo habla de "hasta $50K en conjunto" sin nombrar los cinco | Marcarlos como "sin confirmar" |
| Primera línea de CLAUDE.md | Coincide con el encuadre decidido | Hecho |
| Créditos de Meta: "$150 por persona" | No menciona la ventana de 10 días para el desarrollo, que es un riesgo durante el juicio | Añadir el riesgo y su mitigación |
| Regla 6 y "el simulador arranca en inglés" | Coincide con lo implementado | Sin cambio |
| Pruebas obligatorias del motor | Cumplidas (sección 2) | Sin cambio |

## 5. Rúbrica contra evidencia (estimación nuestra, no puntaje oficial)

| Criterio | Evidencia fuerte | Hueco | Lectura |
| --- | --- | --- | --- |
| Impacto (30%) | Problema documentado con cifra oficial, Modo Clase, telemetría, accesibilidad, piloto planeado | El piloto no tiene datos en el repositorio y el README ya lo afirma | Fuerte en el problema, **débil en la prueba**. Es el mayor riesgo del 30% |
| Herramientas de Meta (25%) | 5 modelos con rol por latencia medida, Muse Spark Open en Baseten, agentes con herramientas, ADR 0003 y 0004 | Muse Code sin declarar; falta mostrar los agentes dentro de la escena | Fuerte; falta visibilidad |
| Demo (25%) | Demo Lab, respaldo local, tiempos límite por llamada | Link público sin verificar, ruta de 90 s sin construir, video sin grabar | **Hueco más grande**: la puerta que condiciona todo |
| Técnica (20%) | Motor puro, cifras de 2026 correctas, 4 pruebas obligatorias, contratos Pydantic, doble implementación comparada, CI | `double` en la cuota, conteo de pruebas impreciso en el README | Fuerte; dos arreglos baratos |

## 6. Qué cambia en el plan

Se agregan tres tickets a [TICKETS_DEMO_Y_VIDEO.md](TICKETS_DEMO_Y_VIDEO.md):

- 14. Corregir README: encuadre de simulador, dato de la OCF, enlaces al principio y la frase del piloto.
- 15. Motor: quitar el `double` de la cuota (opcional) y documentar los créditos fiscales como simplificación.
- 16. Actualizar CLAUDE.md: plazo del 4 de nov, premios sin confirmar, ventana de 10 días, encuadre.

El ticket 11 (datos del piloto) sube de prioridad: el README ya afirma un piloto que el repositorio no respalda.

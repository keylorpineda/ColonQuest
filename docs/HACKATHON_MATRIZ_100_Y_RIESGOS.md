# MATRIZ MAESTRA: CUMPLIMIENTO AL 100% Y GESTIÓN DE RIESGOS LATENTES
## Meta Global AI Developer Hackathon 2026 (Track Indie — DoWhile Team / ColonQuest)

> **Fecha límite oficial:** 4 de noviembre de 2026, 8:00 PM CST (Costa Rica) / 9:00 PM EST.  
> **Meta interna (Code Freeze):** 2 de noviembre de 2026, 11:59 PM CST.  
> **Premios objetivo:** 1º Lugar Track Indie ($150,000 USD) + Bolsa de Premios Bonus ($50,000 USD).  
> **Equipo:** Keylor Pineda (Representante oficial) y Jefferson Abarca (Desarrollador y coautor). Reparto: 50% - 50%.

---

## PARTE 1: LISTADO DE RIESGOS LATENTES (MATRIZ FORENSE)

A continuación se detallan los riesgos operativos, técnicos y legales no evidentes ("NA" o trampas del reglamento) que podrían comprometer la victoria si no se gestionan con anticipación.

| ID | Riesgo Latente | Severidad | Probabilidad | Impacto en Evaluación | Medida de Mitigación Obligatoria (Solución) | Estado |
| :--- | :--- | :---: | :---: | :--- | :--- | :---: |
| **R-01** | **Vencimiento de Créditos de Meta (10 días)**<br>Los $150 de Meta duran "una ventana de 10 días" (desde el 26 oct expirarían el 5 nov). Los jueces evalúan del 4 al 8 de nov. | **CRÍTICA**<br>(10/10) | Media-Alta | **Demo (25%) / Meta AI (25%)**<br>Si un juez entra el 6 o 7 de nov, la API rechaza las llamadas con error 401/403/Quota y no ve la IA. | **Doble API Key en Backend + Baseten:**<br>1. Jefferson ya tiene sus propios $150 de Meta. Configurar en `server/` soporte de llave secundaria (`MODEL_API_KEY_FALLBACK`).<br>2. Vendedores corren en Baseten ($100 sin ventana de 10 días).<br>3. Fallback gracioso a contenido local (`LocalSeller`) garantizado. | 🟡 Pendiente config de fallback key |
| **R-02** | **Desconexión / Cold Start del Backend en Render**<br>Los servidores gratuitos o de baja demanda de Render entran en suspensión ("spin down") tras 15 min sin tráfico. Tardan 50s en reiniciar. | **CRÍTICA**<br>(10/10) | Alta | **Demo (25%)**<br>Un juez no espera 50 segundos en WebGL; asumirá que el juego no sirve y cerrará la pestaña. | **Keep-Alive Automatizado (Monitor Ping):**<br>Configurar un monitor gratuito (UptimeRobot o cron de GitHub Actions) haciendo `GET /api/health` cada 8 minutos desde el 4 hasta el 9 de noviembre. | 🟡 Pendiente activar ping |
| **R-03** | **Enlace WebGL Inaccesible (`colon-quest.pages.dev` no resuelve)**<br>Actualmente el dominio de Cloudflare Pages da error de DNS en pruebas externas. | **CRÍTICA**<br>(10/10) | Alta | **Demo (25%)**<br>Sin un enlace que funcione en un clic, el criterio de Demo se califica con 0 automático. | **Despliegue y Verificación en Navegadores Limpios:**<br>Vincular Cloudflare Pages al directorio de build WebGL o usar GitHub Pages / Vercel como espejo secundario. Probar en modo incógnito desde móvil y PC. | 🔴 En curso (Jefferson / WebGL) |
| **R-04** | **Juez Impaciente / Falta de Tiempo para Explorar**<br>Los jueces tienen decenas de proyectos. Si deben jugar 10 minutos de tutorial para ver la IA de Meta, podrían calificar sin verla. | **ALTA**<br>(9/10) | Muy Alta | **Demo (25%) / Meta AI (25%)**<br>Pérdida de puntos por no presenciar la negociación por voz ni la narración con herramientas. | **Botón Fast-Track ("Demo Rápida Jurado"):**<br>Colocar en la pantalla de inicio un botón directo que cargue al jugador frente al vendedor, con dinero disponible y el micrófono listo para negociar con Muse Spark Open. | 🟡 Pendiente botón en UI |
| **R-05** | **Video Demostrativo Débil o No Enfocado en los Criterios**<br>Hacer un video largo o puramente visual sin evidenciar la arquitectura ni el uso de Meta AI. | **ALTA**<br>(9/10) | Media | **Demo (25%)**<br>El reglamento indica literalmente que se evalúa "the quality of the demo video". | **Guion Estructurado de 2:15 min (Inglés + Subtítulos):**<br>0:00-0:25 Problema social -> 0:25-1:15 Negociación por voz en vivo -> 1:15-1:45 Arquitectura Multi-Modelo y C# -> 1:45-2:15 Modo Clase y conclusión. | 🟡 Pendiente grabación (3 nov) |
| **R-06** | **Riesgo de Privacidad en Datos de la Prueba Piloto**<br>El reglamento descalifica violaciones a derechos de privacidad ("privacy rights"). | **MEDIA**<br>(8/10) | Baja | **Descalificación Legal**<br>Exponer nombres, rostros o datos personales sin consentimiento formal. | **Datos 100% Agregados y Anonimizados:**<br>No mostrar nombres reales de estudiantes ni capturas de chats privados. Presentar solo estadísticas consolidadas (ej. "82% evitó el gota a gota tras la ronda 2"). | 🟢 Mitigado (Participantes 18+) |
| **R-07** | **Omisión de Herramientas Destacadas en el Portal**<br>Cerebral Valley menciona: Muse Spark, Muse Wonder, Muse Image, SAM 3.1, Muse Code y Open Weights. | **MEDIA**<br>(7/10) | Media | **Uso de Herramientas Meta (25%)**<br>No aprovechar todo el arsenal citado por los organizadores resta ventaja frente a otros finalistas. | **Cobertura y Declaración Documentada:**<br>1. Generar con `Muse Image` los retratos y la postal anual.<br>2. Declarar en el formulario que `Muse Code` asistió en la creación de contratos Pydantic y suites de pruebas.<br>3. Resaltar la integración híbrida Cloud + Open Weights en Baseten. | 🟡 Pendiente Muse Image en ADR 0004 |
| **R-08** | **Discrepancias en el Desembolso y Formularios Fiscales**<br>Cerebral Valley paga en USD mediante Tremendous y requiere W-8BEN individual para no residentes. | **MEDIA**<br>(6/10) | Baja | **Cobro del Premio**<br>Retraso en el pago o conflicto si no se formaliza el acuerdo de reparto 50-50 antes. | **Carta Bilateral de Representación:**<br>Firmar la carta de coautoría antes del 4 de noviembre y seleccionar pago dividido para que cada uno reciba su transferencia y tramite su propio W-8BEN. | 🟢 Estrategia definida |

---

## PARTE 2: MATRIZ DE CUMPLIMIENTO AL 100% DE LOS CRITERIOS DEL JURADO

Para garantizar el puntaje perfecto, cada uno de los cuatro criterios evaluados por el jurado debe cumplirse de manera irrefutable.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        CRITERIOS OFICIALES DE EVALUACIÓN                               │
├─────────────────────┬──────────────────────┬─────────────────────┬─────────────────────┤
│ 1. IMPACTO (30%)    │ 2. META AI (25%)     │ 3. DEMO (25%)       │ 4. TÉCNICA (20%)    │
│ Potencial real,     │ Más allá de lo       │ Funciona en vivo,   │ Ingeniería sólida,  │
│ quién se beneficia, │ básico, inesperado,  │ impresiona, calidad │ contratos, tests,   │
│ problema del mundo. │ roles por latencia.  │ del video demo.     │ "real craft".       │
└─────────────────────┴──────────────────────┴─────────────────────┴─────────────────────┘
```

---

### CRITERIO 1: IMPACTO EN EL MUNDO REAL (Ponderación: 30%)

> **Pregunta del Jurado:** *¿Cuál es el potencial en el mundo real? ¿Quién se beneficia? ¿Podría la gente usarlo? ¿Encaja en una de las temáticas ("Build for Your World")?*

* [x] **Ajuste Perfecto a la Temática ("Build for Your World")**:
  - Plantea un problema vivido en carne propia: el analfabetismo financiero juvenil en Costa Rica y América Latina frente a cobradores abusivos, "gota a gota", tarjetas de crédito y esquemas piramidales.
* [x] **Mecánica Educativa Práctica (No Teórica)**:
  - En lugar de cuestionarios aburridos, el jugador se enfrenta a vendedores que cobran comisiones ocultas y aprende a interrogar con su propia voz sobre tasas reales, seguros escondidos y Costo Anual Total.
* [ ] **Plataforma Educativa para Docentes: Modo Clase (Class Mode)**:
  - **Requisito para 100%:** Mostrar el panel donde los profesores visualizan la telemetría agregada de su grupo: qué porcentaje cayó en la trampa del pago mínimo, quién negoció con éxito y cómo evolucionó su toma de decisiones.
* [ ] **Evidencia Cuantitativa de la Prueba Piloto**:
  - **Requisito para 100%:** Incluir en el README y en el video una tarjeta gráfica con los resultados de la prueba piloto realizada con mayores de edad (ejemplo: *"N=25 jugadores: 76% redujo su endeudamiento tras el mes 3; 100% aprendió a exigir la tasa efectiva"*).

---

### CRITERIO 2: USO DE HERRAMIENTAS DE META AI (Ponderación: 25%)

> **Pregunta del Jurado:** *¿Qué tan creativamente se usaron las herramientas de Meta? ¿Fueron más allá de una integración básica? ¿Demostraron capacidades inesperadas?*

* [x] **Especialización Multi-Modelo Justificada por Latencia ([ADR 0004](docs/adr/0004-model-roles.md))**:
  - **Muse Spark 1.3** (`minimal reasoning`): Game Master en vivo que narra eventos mensuales adaptados al historial del jugador en menos de 4 segundos.
  - **Muse Wonder**: Planificador de temporada (12 meses) y resumen de fin de año en segundo plano (calidad narrativa superior sin bloquear la UI).
  - **Muse Spark Open en Baseten**: Modelos abiertos de Meta corriendo en infraestructura de inferencia asociada (aprovechando los créditos y límites de 2M TPM).
  - **Voice Transcribe**: Interacción natural por voz donde el jugador negocia cara a cara con los personajes.
* [ ] **Completar `Muse Image` en la Pipeline**:
  - **Requisito para 100%:** Cerrar la tarea pendiente del ADR 0004. Pre-generar y cachear retratos de NPCs, fondos de eventos y la ilustración de la tarjeta anual de balance financiero.
* [x] **Agentes con Herramientas Deterministas ([ADR 0003](docs/adr/0003-agentic-game-master.md))**:
  - Los modelos no inventan números; tienen herramientas (`simulate_purchase`, `project_debt`). Si el vendedor quiere engañar al jugador, sus cifras provienen de su hoja de cálculo oculta validada en backend.
* [x] **Arquitectura de Inferencia Híbrida (Cloud + Open Weights)**:
  - Combinación pionera entre la API administrada de Meta y el modelo de pesos abiertos en Baseten con balanceo de carga y fallback automático.
* [ ] **Mención de Muse Code en el Proceso de Ingeniería**:
  - **Requisito para 100%:** Mencionar explícitamente en el formulario de entrega cómo Muse Code aceleró el desarrollo de la arquitectura y la cobertura de pruebas.

---

### CRITERIO 3: DEMO Y EXPERIENCIA EN VIVO (Ponderación: 25%)

> **Pregunta del Jurado:** *¿Funciona la demo? ¿Es impresionante? ¿Aguanta en vivo sin romperse? ¿Cuál es la calidad del video demostrativo?*

* [ ] **Un Solo Enlace Público WebGL Operativo**:
  - **Requisito para 100%:** Resolver el DNS de Cloudflare Pages (`colon-quest.pages.dev`). El juego debe cargar en menos de 8 segundos, soportar teclado/mouse y solicitar permisos de micrófono limpiamente.
* [ ] **Botón Fast-Track para el Jurado**:
  - **Requisito para 100%:** Agregar en el menú principal la opción `"Demo Rápida (Evaluación Meta AI)"` para que el juez experimente la negociación por voz y la narración inteligente en sus primeros 60 segundos.
* [x] **Resiliencia Extrema ("Holds up live")**:
  - Si el backend se satura, falla el internet o se acaba la cuota de la API, el juego pasa a modo local sin mostrar pantallas de error ni congelarse.
* [ ] **Video Demostrativo de Alta Factura (2:15 minutos)**:
  - **Requisito para 100%:** Video en 1080p/4K grabado en inglés (o español con subtítulos en inglés perfectos), mostrando la interacción por voz real, la respuesta de la IA en tiempo real y la pantalla de resumen.
* [ ] **Keep-Alive Activo en Render**:
  - **Requisito para 100%:** Evitar que el evaluador sufra el cold start de 50 segundos mediante un ping recurrente a `/api/health`.

---

### CRITERIO 4: PROFUNDIDAD TÉCNICA Y EJECUCIÓN (Ponderación: 20%)

> **Pregunta del Jurado:** *¿Es una ingeniería sólida y bien refinada? ¿Demuestra maestría artesanal ("real craft")?*

* [x] **Motor Financiero Determinista en C# (`FinanceEngine`)**:
  - Matemática exacta: sin errores de redondeo, dinero en colones enteros, cálculo real de cargas sociales de la CCSS y retenciones fiscales.
* [x] **Contratos de Datos Estrictos**:
  - Validación de esquemas bidireccional entre C# (Unity) y FastAPI (Pydantic v2).
* [x] **Control de Costos y Gobernanza de IA (`cost_guard.py`)**:
  - Presupuesto diario parametrizable (`DAILY_BUDGET_USD`), control estricto de tokens de razonamiento (`reasoning_effort: minimal`), degradación automática preventiva al alcanzar el 80% del gasto.
* [x] **Calidad de Software y CI/CD Exhaustivo**:
  - Más de 260 tests automatizados en GitHub Actions (EditMode, PlayMode, NUnit en .NET, Pytest con cobertura, Ruff linter y Mypy type-checking).
* [x] **Base de Datos de Producción**:
  - Neon PostgreSQL con control de migraciones mediante Alembic.

---

## PARTE 3: ESTRATEGIA PARA CAPTURAR LOS PREMIOS BONUS ($50,000 USD)

Cerebral Valley contempla hasta $50,000 acumulados en categorías discrecionales:

1. **Premio "Most Viral Project" (Proyecto Más Viral)**:
   - **Mecánica:** Al completar el año financiero, el juego genera una postal gráfica tipo *Spotify Wrapped*: *"Sobreviví 1 año en Costa Rica sin caer en el gota a gota. Ahorros: ₡450,000. Negociaciones ganadas: 4/4."*
   - **Botón de compartir:** Genera un tweet / post de LinkedIn listo con hashtags `#MetaAIHackathon #ColonQuest @Meta @CerebralValley`.
2. **Premio "Community Vote" (Voto de la Comunidad)**:
   - **Mecánica:** Publicar un hilo en X (Twitter) y LinkedIn el día de la entrega con el video y el enlace WebGL, etiquetando a los organizadores y activando a la comunidad de desarrolladores y educadores de Costa Rica y LATAM.
3. **Mención de Honor en Modelos Abiertos (Open Weights)**:
   - Documentar con orgullo el uso de **Muse Spark Open en Baseten**, posicionando a ColonQuest como el mejor caso de uso de open-weights del hackathon.

---

## PARTE 4: CRONOGRAMA DE CIERRE Y CHECKLIST OPERATIVO

```
Sábado 31 Oct               Domingo 1 Nov               Lunes 2 Nov                 Martes 3 Nov                Miércoles 4 Nov
┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐   ┌───────────────────────┐
│ • Corregir Tanda 3    │   │ • Integrar Voz        │   │ • CODE FREEZE         │   │ • Grabar Video Demo   │   │ • Revisión Final      │
│ • Ajustar Colisiones  │   │ • Generar Muse Image  │   │ • Desplegar WebGL     │   │ • Redactar Pitch      │   │ • SUBMIT FORMAL       │
│ • Firmar Acuerdo 50/50│   │ • Probar Fast-Track   │   │ • Configurar DNS      │   │ • Montar Keep-Alive   │   │   (2:00 PM CST)       │
└───────────────────────┘   └───────────────────────┘   └───────────────────────┘   └───────────────────────┘   └───────────────────────┘
```

### Checklist Final para la Entrega (Día 4 de Noviembre)
- [ ] Enlace público de la demo WebGL abre y corre en Chrome, Edge y Firefox sin errores de consola.
- [ ] Backend en Render activo con monitor keep-alive configurado.
- [ ] Fallback de API Key secundaria de Meta configurado en las variables de entorno de Render.
- [ ] Video de demostración de 2:15 min subido a YouTube (modo Oculto o Público) en 1080p con audio limpio.
- [ ] Repositorio GitHub con README en inglés y español impecable, sin credenciales expuestas ni archivos de configuración locales.
- [ ] Formulario de Cerebral Valley completado por Keylor Pineda, identificando a Jefferson Abarca como coautor y miembro del equipo.
- [ ] Envío realizado al menos 4 horas antes del límite formal de las 8:00 PM CST para evitar sobrecargas de red.

---

## PARTE 5: CONFIRMACIONES Y DECISIONES TÉCNICAS INMEDIATAS (CHECKLIST DE DECISIÓN)

Esta sección consolida las decisiones críticas que el equipo debe rubricar para ejecutar la fase final sin vacilaciones:

| # | Decisión / Confirmación Técnica | Responsable | Acción Específica Requerida | Estado de Confirmación |
| :-: | :--- | :---: | :--- | :---: |
| **C-01** | **Implementar Llave Secundaria (`MODEL_API_KEY_FALLBACK`)** | Keylor (Server) | Configurar en `server/app/config.py` y `providers/routing.py` la lectura de la API Key de Jefferson para conmutar en caso de error 401/403/Quota en la llave de Keylor durante la semana del jurado. | `[ ] APROBADO` |
| **C-02** | **Botón Fast-Track ("Demo del Jurado") en `Menu.unity`** | Jefferson (Unity) | Agregar un botón visible en el menú principal que cargue una escena directa frente al vendedor o prestamista, con dinero precargado y micrófono listo para evaluar la IA en 60 segundos. | `[ ] APROBADO` |
| **C-03** | **Redundancia de Hosting WebGL (Cloudflare + Mirror)** | Jefferson / Keylor | Levantar `colon-quest.pages.dev` en Cloudflare Pages. En caso de retraso con el dominio, desplegar un espejo en GitHub Pages (`colonquest.github.io`) o Vercel como respaldo inmediato. | `[ ] APROBADO` |
| **C-04** | **Monitor Keep-Alive en Render (Evitar Cold Start)** | Keylor (DevOps) | Activar una tarea programada gratuita (cron o UptimeRobot) que envíe una petición `GET /api/health` cada 8-10 minutos desde el 4 hasta el 9 de noviembre para mantener caliente la instancia. | `[ ] APROBADO` |
| **C-05** | **Pre-generación y Caché con `Muse Image`** | Keylor (Server) | Ejecutar el script que genera con Muse Image los retratos y fondos de eventos, almacenándolos en disco/estáticos para cumplir la presencia del modelo en el pipeline. | `[ ] APROBADO` |
| **C-06** | **Firma y Reserva de la Carta Bilateral 50-50** | Keylor / Jefferson | Redactar y firmar el acuerdo de coautoría y división de premios 50-50 para presentarlo inmediatamente a Cerebral Valley al ser contactados para el payout. | `[ ] APROBADO` |
| **C-07** | **Grabación del Video Demostrativo (3 de Noviembre)** | Jefferson / Keylor | Grabar 2:15 min siguiendo el guion estructurado: Problema -> Demo de Voz en vivo con Baseten -> Diagrama de arquitectura -> Modo Clase. Subir a YouTube en 1080p con subtítulos. | `[ ] APROBADO` |

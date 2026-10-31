# Errores del dictado (30 de octubre) y verificación total del juego

Probado por Jefferson con `windows_r18` (commit `5b265ff`, rama `feat/SCRUM-94-graphical-leap-textures-physics-typography`).
La parte A es para arreglar. La parte B es la lista para verificar absolutamente todo el juego, edificio por edificio.

## Estado al 31 de octubre (rama `fix/SCRUM-58-dictation-fixes`)

| Error | Estado |
| --- | --- |
| A1 Presentación DoWhile Team | Arreglado en Unity: pantalla propia después del logo de Unity, se salta con clic o tecla, una vez por arranque |
| A2 Ventanas del menú | Arreglado: misma banda de título y aparición suave en Créditos, Ajustes, Jugar online, partidas y Nueva vida; Salir pide confirmación |
| A3 Salida de la casa | Arreglado: suena la puerta, el personaje sigue caminando mientras oscurece y sale caminando del otro lado |
| A4 Tiempo | Arreglado: 30 s por hora en normal (8 min por día) y 10 s en rápida |
| A5 8 tareas por mes | Arreglado: 2 por día, también el día 4; un lugar se repite en otro día, no el mismo |
| A6 Inglés | Arreglado del lado de Unity: en inglés se usan los eventos, el vendedor y el resumen locales, ya traducidos. **Pendiente de Keylor:** que el servidor reciba el idioma para que en inglés también escriba la IA (puntos 1 a 4) |
| A7 Escenas faltantes | Arreglado: mascota, doctores, moto, accesorios, carros, taller, pizarra del banco y la gente que conversa en la calle |
| A8 Puentes | Arreglado: el paso va de baranda a baranda; escenas regeneradas |

Nada de esto se ha visto todavía en pantalla: lo verifica la RONDA 10 de Antigravity y la prueba manual.

---

## A. Errores para arreglar

### A1. Falta la pantalla "Desarrollado por DoWhile Team" al iniciar
- **Qué pasa:** al abrir el juego sale el logo de Unity y después entra directo al menú.
- **Qué se quiere:** después del logo de Unity, una pantalla propia: "Desarrollado por DoWhile Team", con quién lo diseñó (Jefferson Abarca y Keylor Pineda) y el nombre del hackathon.
- **Dónde está:** `unity/ProjectSettings/ProjectSettings.asset` tiene `m_SplashScreenLogos: []` (no hay logo propio). Se puede agregar el logo del equipo ahí, o hacer una escena de presentación de 2 a 3 segundos antes del menú (con fundido, y que se pueda saltar con clic o Espacio).

### A2. Profesionalizar todas las ventanas del menú
Aplica a **todas** las opciones del menú principal, sin excepción:
- **Créditos:** que se vea como una pantalla de créditos de verdad (roles, nombres, herramientas de Meta usadas, hackathon), no como un aviso.
- **Ajustes (configuraciones):** mismo estilo de tarjeta, títulos, separación y botones que el resto.
- **Jugar online:** el aviso tiene que verse profesional, no como una ventana de error.
- **Salir:** hoy cierra directo. Tiene que pedir confirmación ("¿Seguro que quieres salir?" con Salir / Cancelar).
- **Los demás botones** (Modo historia, partidas, etc.): mismo estilo, mismo tamaño de letra, mismas animaciones al pasar el mouse.
- Una sola línea visual para todas las pantallas: la fuente nueva (`Resources/Fonts`), los colores, los bordes, las sombras y las transiciones.

### A3. Al salir de la casa parece que el personaje se teletransporta
- **Qué pasa:** al caminar hacia la puerta para salir, la animación se ve fatal: el personaje desaparece y aparece en la calle sin transición creíble.
- **Dónde está:** `unity/Assets/Scripts/World/SceneTransition.cs` hace un fundido (`FadeThenLoad`), pero se siente como teletransporte.
- **Qué se quiere:** que el personaje camine hasta la puerta, la puerta se abra (con sonido), funda a negro de forma suave, y en la calle aparezca saliendo por la puerta (unos pasos hacia afuera), no parado de golpe. Revisar lo mismo en **todas** las puertas (ver B1).

### A4. El tiempo corre demasiado rápido
- **Dónde está:** `unity/Assets/Scripts/Game/GameRules.cs`, `NormalSecondsPerGameHour = 15f` (un día de 16 horas dura unos 4 minutos) y `FastSecondsPerGameHour = 5f`. También `GameSettings.SecondsPerGameHour`.
- **Qué se quiere:** más lento. Propuesta: 30 s por hora de juego en normal (un día dura unos 8 minutos) y dejar el modo rápido para la demo. Que alcance para hacer las 2 tareas del día sin correr.

### A5. Dos tareas por semana: 8 tareas por mes (la partida sigue en 3 meses)
- **Decisión:** la demo se queda en **3 meses** (`MonthsToPlay = 3`). Cada mes tiene 4 días y cada día es una semana. Se quieren **al menos 2 tareas por semana, 8 por mes**.
- **Cómo está hoy (en `GameRules.cs` y `GameLoop.cs`):**
  - `TasksPerMonth = 3`.
  - `CanDoTask` exige `TasksToday < 1` (1 tarea por día) y `!Clock.IsLastDay` (el último día no hay tareas, es día de banco).
  - Cada lugar de tareas se puede usar **una sola vez por mes** (`doneStations`).
- **Problema adicional encontrado:** en el pueblo hay solo **7 lugares de tareas** (tienda, taller de Don Rafa, pulpería, prestamista, pirámide, soda y cooperativa). `StationType.Kiosk` existe en el código pero no está construido en ningún mapa. Con 7 lugares y "una vez por mes" nunca se llega a 8.
- **Qué hay que cambiar:**
  1. `TasksPerMonth = 8` y 2 tareas por día (`TasksToday < 2`).
  2. Permitir tareas el día 4 (o decidir que el banco se cobra después de la segunda tarea del día 4).
  3. Llegar a 8 lugares: construir el kiosko (`Kiosk`) o permitir repetir un lugar en otra semana con un evento distinto.
  4. Ajustar las pruebas que cuentan tareas (`GameLoop` y las de PlayMode) y el texto del HUD que dice cuántas faltan.

### A6. En inglés, los diálogos y las opciones siguen en español (error grave)
- **Qué pasa:** al poner el juego en inglés, los menús cambian, pero los diálogos de los personajes, los eventos del game master y las opciones de las decisiones salen en español.
- **Causa encontrada:** Unity **no le dice al servidor en qué idioma escribir**. No hay ningún campo de idioma en las llamadas de `unity/Assets/Scripts/Api/` (el propio `LocalSeller.cs` lo dice: "The API layer does not know the language"). Por eso el game master, los vendedores, el resumen y la transcripción de voz (`languageBias: ["Spanish"]` en `server/app/providers/meta.py`) trabajan siempre en español.
- **Qué hay que cambiar (Keylor, toca `server/` y `shared/`):**
  1. Agregar `language` (`es` / `en`) al contrato: creación de partida, evento, negociación y resumen.
  2. Que el game master, los vendedores y el resumen escriban en ese idioma (los ids y enums no cambian).
  3. Que la transcripción use inglés como idioma preferido cuando el juego está en inglés.
  4. Que los eventos de respaldo (`fallback_events.json`) tengan versión en inglés.
  5. Que todo el texto escrito en Unity (diálogos de NPC, opciones, pensamientos, subtítulos, carteles) pase por `Translations.cs`.
- **Cómo verificarlo:** Ajustes > Language: English, y jugar un mes completo. No puede aparecer **ni una palabra** en español: diálogos, opciones, botones, subtítulos, carteles, retratos con nombre, estado de cuenta, reporte final.

### A7. Interacciones que no tienen escena animada
La regla del juego es que **cada interacción tiene animación del personaje, del objeto y sonido**. Estas cosas existen en las escenas pero no tienen escena animada propia en `GameUi.Scenes.cs` (hoy abren solo un aviso o una ventana):
`Thing_Accessories`, `Thing_Motorbike`, `Thing_Screens`, `Thing_Washer` (estos de la Tienda), `Thing_Car` (Agencia y calle), `Thing_Workshop` (Agencia), `Thing_RatesBoard`, `Thing_Teller3` (Banco), `Thing_Doctor1`, `Thing_Doctor2`, `Thing_Doctor3` (Hospital arriba), `Thing_Pet` (Casa, Cuarto y Vecino), y los carteles `Station_Listing_*` de las casas en venta.
Verificar cada uno en el juego (B) y darle escena a los que solo abren una ventana.

### A8. Caminando por la acera junto al río, el juego cree que quieres nadar y no te deja pasar
- **Qué pasa:** al cruzar un puente caminando por la acera (no por la calle), el personaje se frena, sale el pensamiento del bañador y lo empuja hacia atrás, como si hubiera querido meterse a nadar.
- **Causa encontrada:** el puente y el paso libre no miden lo mismo.
  - El puente **se dibuja** de baranda a baranda, cubriendo la calle y las dos aceras: `Bridge(parent, x, 6f, -2f)`, `Bridge(parent, x, -8f, -16f)` y `Bridge(parent, x, NorthSidewalkY, 24f)` en `unity/Assets/Editor/MapBuilder.World.cs` (líneas 189 a 192).
  - Pero las **paredes invisibles del río** (`RiverWall`, líneas 359 a 363 del mismo archivo) solo dejan abierta la franja de la calle: de y 0 a 4, de -14 a -10 y de 26 a 30. Las aceras miden 1,2 a cada lado de la calle (`Surfaces.cs`, línea 55), así que sobre el puente la acera queda encima de "agua".
  - Y `WorldHints.OnDeck` en `unity/Assets/Scripts/World/Surfaces.cs` solo cuenta como puente de -0,4 a 4,4, de -14,4 a -9,6 y de 25,6 a 30,4. Fuera de eso, `WalkWatch.cs` (línea 77) lanza el pensamiento del agua y empuja al jugador con `Nudge`.
- **Qué hay que cambiar:** que la parte abierta del río y `OnDeck` vayan de baranda a baranda (calle más las dos aceras), igual que el dibujo del puente: aproximadamente de y -1,5 a 5,5, de -15,5 a -8,5 y de 24,5 a `NorthSidewalkY - 0,5`. Revisar también el puente del camino a la playa (-19,15 a -17,45). Actualizar `SurfacesTests` y `JailAndSwimTests` si cuentan esas franjas.
- **Cómo verificarlo:** cruzar los 4 puentes caminando por la acera de arriba, por la de abajo y por la calle, ida y vuelta. No debe salir ningún pensamiento ni frenar. Y pegado a la orilla del río, lejos de los puentes, el pensamiento del bañador sí debe salir.

---

## B. Verificación total: cada edificio, cada acción, cada interacción

Cómo marcar: **OK / FALLA / NO PROBADO**, y una línea de lo que se vio.
Para **cada** interacción se revisan estas 8 cosas:
1. El personaje camina hasta el lugar sin atravesar muebles.
2. El personaje hace una pose (se sienta, firma, come, etc.).
3. El objeto o la otra persona reacciona (se abre, saluda, cuenta billetes...).
4. Suena algo.
5. Espacio salta la escena sin dejar al personaje congelado.
6. Al terminar, el personaje queda libre, de pie, fuera del mueble y con la pantalla clara.
7. La plata o el ánimo cambian **al final**, y el HUD lo muestra.
8. En inglés, todo sale en inglés.

### B0. Arranque y menú
- [ ] Logo de Unity y luego "Desarrollado por DoWhile Team" (A1).
- [ ] Menú: fondo con capas que se mueve con el mouse; versión arriba a la izquierda; crédito abajo.
- [ ] Botón Modo historia.
- [ ] Botón Partidas: 3 espacios, crear, cargar, borrar (con confirmación), partida terminada se ve como terminada.
- [ ] Botón Ajustes: idioma, volumen, velocidad del tiempo, pantalla completa. Se guardan al cerrar y abrir.
- [ ] Botón Créditos (A2).
- [ ] Botón Jugar online (A2).
- [ ] Botón Salir con confirmación (A2).
- [ ] Pantalla de carga: nombre, barra, "Cargando...", versión.
- [ ] Apertura de la historia: nombre y género del personaje, primer trabajo.

### B1. Todas las puertas (entrar y salir de cada una, 21 puertas)
Para cada una: camina a la puerta, fundido suave, apareces en el lugar correcto, al salir apareces **frente a esa misma puerta** y no te teletransportas (A3).
- [ ] Casa (`Door_Casa`) y Cuarto alquilado (`Door_Cuarto`).
- [ ] Banco, Tienda, Soda, Agencia de carros, Inmobiliaria, Finca, Hospital, Cárcel (comisaría).
- [ ] Hospital: elevador abajo y arriba (`Door_Elevator`), ida y vuelta.
- [ ] Casa del vecino (`Door_Casa_Vecina_A`): aviso con cuenta de 9 s; si te quedas, arresto.
- [ ] Casas `casa_vecina_b`, `casa_vecina_d`, `casa_almendros_1` a `5`: qué pasa al pulsar E en cada una.

### B2. Pueblo (Town)
- [ ] Caminar por toda la calle principal, las calles del este, la plaza, el parque, el puente, el estanque, el río y la playa.
- [ ] Límites: oeste (subtítulo "No debería alejarme tanto"), río, estanque, mar fuera del muelle (pide bañador).
- [ ] Los 4 puentes: cruzar por la acera de arriba, la de abajo y la calle, ida y vuelta, sin frenazo ni pensamiento del bañador (A8).
- [ ] Arte nuevo: texturas de calle, acera, plaza, techos y ladrillo; árboles y palmeras que se mueven al pasar y se calman; el personaje queda detrás del árbol cuando está arriba y delante cuando está abajo.
- [ ] Carros: respetan semáforos, no se montan, no desaparecen delante; con conductor al moverse.
- [ ] Semáforos y oficial de tránsito: cruzar en rojo trae multa solo si el oficial te ve.
- [ ] Atropello: destello, sale despedido, gira, estrellas, hospital; una vez por mes; con seguro cuesta menos.
- [ ] Carro de cobradores: solo para si debes; Pagar / Negociar / Ignorar.
- [ ] Peatones: saludan con el codo doblado; Estudiante y Repartidor.
- [ ] Noche: el pueblo oscurece desde las 6 p. m.; a las 10 p. m. hay que dormir.
- [ ] `Thing_Bench` (banca): se sienta y se levanta libre.
- [ ] `Thing_Fountain` (fuente): moneda y deseo.
- [ ] `Thing_Car` (carro en la calle).
- [ ] `Thing_Talk`, `Thing_Talk_Cajera`, `Thing_Talk_DonMario`, `Thing_Talk_MartaPlaza`, `Thing_Talk_Vendedor`: conversación con subtítulo y nombre; el diálogo se cierra al alejarse.
- [ ] `Station_Beach` (playa): con bañador nada y sube el ánimo; segunda vez en el mes, solo un pensamiento.
- [ ] `Station_Courses` (cursos): inscribirse, pagar, efecto en el ascenso.
- [ ] Carteles `Station_Listing_*` (4 casas en venta o alquiler): pensamiento "tendría que ir a la inmobiliaria".

### B3. Los 7 lugares de tareas del mes (cada uno abre el evento del game master)
- [ ] `Station_Store` en la Tienda: Kevin y la negociación (B5).
- [ ] `Station_Garage`: taller de Don Rafa.
- [ ] `Station_Grocery`: pulpería.
- [ ] `Station_Lender`: prestamista gota a gota (billetes).
- [ ] `Station_Pyramid`: Brayan, "INVIERTE YA" (folleto).
- [ ] `Station_Soda`: soda (plato).
- [ ] `Station_Coop`: cooperativa (firma).
- [ ] Por cada uno: el otro personaje saluda, hay un gesto antes de la ventana, el evento llega en menos de 8 s o sale el de respaldo, las opciones cambian la plata correcta, y el lugar no se puede repetir en el mes (o sí, según lo que se decida en A5).
- [ ] Conteo: cuántas tareas por día y por mes deja hacer (debe ser 2 y 8, A5).

### B4. Casa propia (Casa) y cuarto alquilado (Cuarto)
- [ ] `Thing_Bed`: camina, se acuesta, Z, oscurece, despertador, amanece, "Dormiste", ánimo +3; segunda vez en el mes solo habla; queda libre.
- [ ] `Thing_Stove`: sartén, vapor, plato, come; ahorra efectivo.
- [ ] `Thing_Shower`: vapor y gotas.
- [ ] `Thing_Sink`.
- [ ] `Thing_Tv`: se sienta, la pantalla parpadea.
- [ ] `Thing_Fridge`: abre, luz.
- [ ] `Thing_Sofa`: se sienta.
- [ ] `Thing_DiningTable`: come.
- [ ] `Thing_Pet` (zaguatico): acariciar, mueve la cola, sube el ánimo (A7).
- [ ] `Station_Home`: estado de cuenta y recibos.
- [ ] `Station_Wardrobe`: ropero abre, compra de bañador (₡18 000), cambio de ropa.
- [ ] Mejoras de la casa por etapas: la casa fea mejora al comprar muebles.
- [ ] Repetir lo que exista en el Cuarto (cama, mesa, refri, tele, mascota).

### B5. Tienda (Tienda): núcleo de la demo
- [ ] `Station_Store`: Kevin saluda y desliza un papel.
- [ ] Negociación por texto: preguntar por el precio de contado, el seguro y la comisión; el vendedor solo revela cuando le preguntan.
- [ ] Negociación por voz (micrófono): graba hasta 15 s, transcribe, responde; si falla, queda el texto.
- [ ] "Me lo llevo": firma del contrato (papel, firma, sello, caja) **antes** de cobrar; la cuota nueva aparece en el HUD.
- [ ] `Thing_Cashier`, `Thing_Screens` (televisores), `Thing_Washer`, `Thing_Fridge`, `Thing_Motorbike`, `Thing_Accessories` (A7).
- [ ] `Station_Furniture`: comprar muebles para la casa.

### B6. Banco (Banco)
- [ ] `Station_Bank` / ventanilla: billetes vuelan a la cajera, cuenta, impresora, recibo, sello, y luego la ventana de cerrar el mes.
- [ ] Cajero automático: tarjeta, teclas, billetes, recibo.
- [ ] `Thing_TicketMachine`: papel del turno.
- [ ] `Thing_Receptionist`, `Thing_Guard`, `Thing_RatesBoard`, `Thing_Teller3` (A7).
- [ ] Quincena: el salario llega en dos pagos; rebajos de CCSS y renta correctos.
- [ ] Cierre de mes: interés de la tarjeta, pago mínimo, cuotas, alquiler, gastos de vida; nada negativo sin deuda.

### B7. Soda (Soda): primer trabajo
- [ ] `Station_Work`: turno de 7 a. m. a 12 m.; llegar tarde rebaja el salario; animación de trabajar.
- [ ] `Station_Soda`: comer (plato).
- [ ] Ascenso a administrador según cursos y puntualidad.

### B8. Hospital (Hospital y HospitalArriba)
- [ ] `Station_HealthDesk`: hablar de un seguro de salud, contratarlo.
- [ ] `Thing_EmergencyWard`: camas de urgencias.
- [ ] Elevador a la planta de arriba y de vuelta.
- [ ] `Thing_Doctor1`, `Thing_Doctor2`, `Thing_Doctor3`: consulta (A7).
- [ ] Llegar tras un atropello: despierta en una cama, cuenta del hospital, un día menos.

### B9. Cárcel (Carcel)
- [ ] Arresto: manos arriba, sirena, celda, "Condena", "Pagar fianza".
- [ ] `Thing_CellCot`: catre de la celda.
- [ ] `Thing_Counter`: mostrador, el oficial firma y sella.
- [ ] `Thing_Judge`: juez, golpes de mazo.
- [ ] La condena corre en días de juego; al salir quedas libre en la calle.

### B10. Agencia de carros (Agencia)
- [ ] `Station_CarDealer`: Don Chepe, lista de carros, firma.
- [ ] `Thing_Car` (3 carros con precio), `Thing_Workshop` (taller del fondo), `Thing_FinePrint` (letra pequeña), `Thing_Coffee` (cafetera).
- [ ] Meta de la moto: se puede comprar y aparece.

### B11. Inmobiliaria y Finca
- [ ] `Station_RealEstate`: la agente Marcela, fotos de casas, alquilar o comprar, firma.
- [ ] `Station_Farm`: Don Beto, vacas, firma.

### B12. Casa del vecino (Vecino)
- [ ] Entrar: aviso rosado de 9 s; quedarse lleva al arresto; salir a tiempo no pasa nada.
- [ ] `Thing_Pet` del vecino.

### B13. Celular (tecla P)
- [ ] El personaje teclea y el celular se abre con movimiento.
- [ ] Cada app del celular: SINPE, estado de cuenta, metas, cursos y lo que tenga. Cada una abre, funciona y cierra.

### B14. Dinámicas ticas, eventos y fin de la partida
- [ ] Aguinaldo y marchamo si caen en los 3 meses; noticias de economía; inflación.
- [ ] Facturas (luz, agua, internet) y sus fechas.
- [ ] Fin de los 3 meses: reporte final, postal del año, resumen narrado, volver al menú, la partida queda terminada.

### B15. Pausa, sonido, idioma y rendimiento
- [ ] Esc: pausa, el tiempo se detiene, Reanudar, Ajustes, Volver al menú.
- [ ] Sonido: música del pueblo y de cada interior, efectos de cada interacción, volumen de Ajustes.
- [ ] Inglés: todo el juego en inglés, incluidos diálogos y opciones (A6).
- [ ] FPS en el pueblo, en la plaza y en la playa; sin tirones al entrar o salir de un edificio.
- [ ] Registro sin excepciones: `%USERPROFILE%\AppData\LocalLow\<compañía>\ColonQuest\Player.log`.

### Lo que no está hecho (no es falla)
Año de 12 meses (la demo queda en 3, decidido), interiores de pulpería y taller, animales nuevos, cama con suero en el hospital, cuidado de la mascota (solo se acaricia).

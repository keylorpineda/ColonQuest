# Colón Quest: lista completa de pruebas (2026-10-30)

Build a probar: `C:\Users\USUARIO\ColonQuestBuild\windows_r17\ColonQuest.exe` (develop, commit `b583f2d`).
Marca cada línea con OK, FALLA (y qué viste) o NO PROBADO. Si algo falla, anota cómo llegaste ahí, para poder repetirlo.

**Controles:** flechas o WASD para caminar, **E** para interactuar, **P** para el celular, **Espacio** para saltar una
escena animada, **Esc** para pausa o volver, **F11** o **Alt+Enter** para pantalla completa, y los números **1 a 9**
para elegir en las ventanas.

**Regla de las escenas animadas:** cada interacción debe tener
- el personaje caminando al sitio y girándose hacia el objeto;
- un gesto o pose propio;
- algo que reacciona (un objeto o una persona);
- un sonido;
- el cambio de plata **al final** de la escena.

Además, Espacio debe saltar la escena sin dejar al jugador congelado.

---

## 0. Arranque y ventana

- [ ] El .exe abre sin errores, en pantalla completa.
- [ ] F11 y Alt+Enter cambian entre ventana y pantalla completa, y el cambio se recuerda al reiniciar.
- [ ] En una ventana pequeña (arrastra el borde) nada del menú ni del HUD se corta.
- [ ] Cerrar el juego con la X de la ventana no deja el proceso abierto (revisa el Administrador de tareas).

## 1. Menú principal

- [ ] Se ve la ilustración del tucán de fondo, con capas que se mueven un poco con el mouse.
- [ ] Hay música propia del menú.
- [ ] Botones: Modo historia, Jugar online, Ajustes, Créditos y Salir. Ninguno se monta sobre otro ni sobre el título.
- [ ] Al pasar el mouse cada botón crece, cambia el borde y suena un toque suave. Al hacer clic suena el clic.
- [ ] Las flechas y Enter sirven igual que el mouse.
- [ ] Arriba a la izquierda dice la versión (Alpha 0.1.x); abajo, "Design by DoWhile Team". Los dos son legibles.
- [ ] **Créditos:** se abren, se leen completos (nada se sale de la tarjeta) y se cierran con Volver y con Esc.
- [ ] **Jugar online:** sale el aviso de la versión completa, se cierra bien y su texto cabe.
- [ ] **Salir:** cierra el juego.

## 2. Ajustes

- [ ] Sonido activado o desactivado, y la barra de volumen funciona en el acto.
- [ ] **Idioma Español / English:** el menú cambia en el acto. Al reiniciar, se mantiene el idioma elegido.
- [ ] La velocidad del tiempo (normal o rápida) se ve y se guarda.
- [ ] Pantalla completa desde Ajustes.
- [ ] Volver y Esc cierran Ajustes sin cambiar nada más.

## 3. Partidas guardadas (Modo historia)

- [ ] Modo historia abre la lista de **3 espacios**. Los vacíos dicen que están libres.
- [ ] **Nueva partida:** pide el nombre y el género, más el nombre de la partida.
- [ ] No deja empezar sin nombre.
- [ ] Nombres con tilde y ñ ("Sofía", "Begoña") se ven bien.
- [ ] Un nombre muy largo no rompe la ventana.
- [ ] Con mujer y con hombre, el personaje del juego sale distinto (pelo, ropa).
- [ ] Cada espacio ocupado muestra nombre, mes y semana, plata, ánimo y fecha de la última vez.
- [ ] **Continuar** carga la partida exactamente donde quedó (lugar, plata, muebles, puesto, metas).
- [ ] **Borrar** pide confirmación y deja el espacio libre.
- [ ] Llenar los 3 espacios: no deja crear un cuarto sin borrar uno.
- [ ] Se guarda al dormir y al salir al menú. Cierra el .exe con la X a mitad del día y vuelve a abrir: la partida está en el último punto guardado, no corrupta.
- [ ] Una partida terminada (fin del año) se ve como terminada y no se puede seguir jugando.

## 4. Pantalla de carga y apertura

- [ ] Al entrar sale la pantalla de carga (logo, barra, "Cargando...", versión y crédito) y desaparece sola.
- [ ] **Escena de apertura:** el personaje cumple 18, le llega el mensaje del trabajo al celular y sale de la casa.
- [ ] La apertura se puede saltar con Espacio.
- [ ] En la apertura, el nombre y el género salen bien escritos ("un joven" o "una joven").
- [ ] Una flecha o guía indica a dónde ir a trabajar.

## 5. HUD y reloj

- [ ] El HUD muestra el mes, la semana y la hora, más efectivo, ahorros, tarjeta, cuotas por mes y ánimo.
- [ ] Nada del HUD se pisa, y ningún texto está cortado.
- [ ] El reloj avanza solo. En velocidad normal y en rápida, la hora corre más lenta o más rápida.
- [ ] La luz del pueblo cambia por hora: mañana, mediodía, atardecer y noche.
- [ ] De noche las farolas se encienden, los locales tienen letrero CERRADO y no se pueden usar, y la gente se va.
- [ ] A las 22:00 el personaje cae dormido si no fue a la cama, y baja el ánimo.
- [ ] Un mes = 4 semanas. Al final de la semana 4 se cierra el mes.

## 6. Personajes (v2)

- [ ] **Jugador:** mochila, camisa naranja (o la versión femenina), con los brazos pegados al cuerpo al caminar.
- [ ] Codos sin picos, barbilla y cuello sin hueco, y cintura sin hueco entre las piernas.
- [ ] Mira de cerca a cada personaje con nombre: Kevin (tienda), Don Rafa, Doña Marta, el prestamista, Brayan, Don Beto, la cajera, el oficial, la jueza, el enfermero, la doctora, la asesora de la coope, Doña Lucía, Don Chepe, Don Mario, la financiera, la agente inmobiliaria, la estudiante y el repartidor.
  Para cada uno revisa:
  - [ ] el pelo no atraviesa el sombrero;
  - [ ] no tiene un moño encima de la gorra;
  - [ ] la sonrisa se ve en reposo y se mueve al hablar;
  - [ ] la pupila no se sale del blanco del ojo al seguirte.
- [ ] Al acercarte, el NPC saluda con el codo doblado y te sigue con la mirada.
- [ ] Los que engañan (el prestamista, la financiera) se ven distintos de los que ayudan (la coope, Doña Marta).
- [ ] Ningún personaje queda enorme, diminuto o con piezas sueltas.

## 7. Pueblo: caminar y límites

- [ ] Camina todo el pueblo 5 minutos: nunca quedas trabado en un árbol, una casa, una banca o un carro estacionado.
- [ ] **Bordes del mapa:** el pensamiento "No debería alejarme tanto del pueblo" y un pasito atrás.
- [ ] **Aceras junto al río y alrededor del estanque:** se camina sin avisos ni empujones.
- [ ] Meterse al río o al estanque sale el pensamiento del río o del estanque, no el del bañador.
- [ ] Los puentes se cruzan.
- [ ] Los pasos suenan distinto en el asfalto, el pasto, la madera y la arena.
- [ ] Sonidos de fondo: tráfico cerca de las calles, mar en la playa, río al este, pájaros.

## 8. Carros y semáforos

- [ ] Quédate en cada cruce 2 ciclos completos (unos 40 s):
  - [ ] con el muñequito **verde**, ningún carro pisa la cebra;
  - [ ] con **rojo**, pasan.
- [ ] Los carros frenan en fila y no se montan unos sobre otros.
- [ ] Los carros no desaparecen delante de ti; solo salen del mapa por los extremos.
- [ ] **Cruzar con verde:** nunca te atropellan.
- [ ] **Cruzar con rojo:** si un oficial te ve, te multa (ventana "Lo que pasó"), con la escena del oficial.
- [ ] **Carro de cobradores** (negro, música):
  - [ ] solo se detiene si debes (préstamo gota a gota o tarjeta casi llena);
  - [ ] ventana con Pagar, Negociar e Ignorar;
  - [ ] cada opción cambia la plata como dice.

## 9. Atropello y hospital

- [ ] Párate en la calle hasta que te atropelle un carro: destello, sales despedido, giras, estrellas, y luego el hospital.
- [ ] La cuenta del hospital es menor con seguro de salud que sin él, y se pierde un día.
- [ ] **Recepción:** contratar el seguro de salud con firma animada. El seguro se cobra cada mes.
- [ ] **Ascensor:** pulsar el botón, timbre, se abren las puertas, sube, se abren arriba. Bajar igual, dos veces.
- [ ] Ascensor: siempre apareces fuera del ascensor y nunca dentro de un mueble.
- [ ] Sala de urgencias (mirar las camas) con escena.

## 10. Policía, arresto y cárcel

- [ ] Entra a una casa ajena: aviso rosado con cuenta regresiva de 9 s.
- [ ] Sal a tiempo: no pasa nada.
- [ ] **Quédate:** sirena, el oficial llega, levantas las manos, esposas, fundido y celda.
- [ ] En la celda la **reja está cerrada**: no puedes salir por el frente ni por los lados.
- [ ] La condena se cuenta en días de juego.
- [ ] Con el botón de **fianza** pagas y la reja se abre con animación y sonido.
- [ ] **Cumplir la condena** también abre la reja.
- [ ] Sin plata para la fianza, el botón no deja pagar y lo dice.
- [ ] **Mostrador:** el oficial firma y sella.
- [ ] **Despacho del juez:** tiene paredes y puerta. Hablarle muestra firma y golpes del mazo.
- [ ] Durante la condena no se llega al despacho.

## 11. Primer trabajo (soda)

- [ ] La guía lleva a la soda. Adentro se ve el interior y la jefa (Doña Lucía).
- [ ] **Marcar entrada** en el reloj de tarjetas: escena con la tarjeta y el timbre.
- [ ] Trabajar: escena de barrer, atender o lavar, y la mañana pasa.
- [ ] Llegar antes de las 07:15: no pasa nada.
- [ ] Llegar tarde: llamada de atención. Dos veces: rebajo en la quincena. Tres en el mes: advertencia.
- [ ] Faltar un día: rebajo y baja el ánimo.
- [ ] **Quincena:** pago al final de la semana 2 y de la semana 4, con aviso de SINPE en el celular. El monto coincide con el salario mínimo neto menos rebajos.
- [ ] Solo se puede trabajar una vez por día (límite diario).

## 12. Crecer: cursos y ascensos

- [ ] **Tablero de cursos** en la acera: se ven el INA y la UNA, con precio y duración.
- [ ] Inscribirse cobra lo que dice y ocupa las tardes.
- [ ] Sin plata, no deja.
- [ ] Al cumplir los días sin faltar y el curso, el jefe anuncia el **ascenso** con escena y celebración.
- [ ] Sube el salario en la siguiente quincena.
- [ ] La escalera de puestos llega hasta administrador.

## 13. Casa: cuarto alquilado y mejoras

- [ ] Empieza en el **cuarto feo**: colchón en el piso, sin tele ni refri.
- [ ] El alquiler se cobra al cierre del mes.
- [ ] Comprar muebles (cama, refri, tele, mesa) de contado o a cuotas.
- [ ] Cada mueble aparece en el cuarto al comprarlo.
- [ ] Las cuotas aparecen en el HUD y en el estado de cuenta.
- [ ] Mudarse a una vivienda mejor desde la inmobiliaria (alquilar o comprar). La nueva casa tiene su interior y el alquiler cambia.
- [ ] **Recibos** (luz, agua, internet, teléfono) en la semana 4, en el celular. Pagarlos desde el celular.
- [ ] **Sin pagar:** recargo. Al segundo mes, corte: la casa se oscurece y la tele no funciona. Al pagar, vuelve.

## 14. Escenas animadas de la casa

Para cada una, revisa:
- llegas sin atravesar muebles;
- haces la pose;
- algo reacciona;
- suena;
- se puede saltar con Espacio;
- al final no quedas trabado ni dentro del mueble.

- [ ] **Cama:** se acuesta, salen Z, la pantalla se oscurece, suena el despertador y amanece (el día siguiente a las 06:00). Sube el ánimo.
- [ ] **Estufa:** sartén, vapor, plato y come. Ahorro de efectivo, una vez por mes.
- [ ] **Ducha:** vapor y gotas.
- [ ] **Tele:** se sienta y la pantalla parpadea.
- [ ] **Refri:** se abre la puerta y sale la luz.
- [ ] **Sillón:** se sienta de verdad sobre el asiento.
- [ ] **Mesa del comedor:** se sienta, plato y come.
- [ ] **Escritorio de recibos:** papeles, firma y luego el estado de cuenta.
- [ ] **Ropero:**
  - se abre desde el lado del cuarto y las puertas se abren;
  - sin bañador, ventana para comprarlo;
  - con bañador, el bañador colgado y un pensamiento.
- [ ] **Lavamanos y lavadora.**

## 15. Banco

- [ ] **Ventanilla (Caja 2):** billetes a la cajera, ella cuenta, impresora, recibo y sello. Después, la ventana de cerrar el mes.
- [ ] **Cerrar el mes:** el reporte muestra salario, cuotas, tarjeta, intereses y gastos, y cuadra con el HUD.
- [ ] Elegir "Todavía no" no cierra nada.
- [ ] **Cajero automático** (en el banco y en la calle): tarjeta, teclas, billetes, recibo y estado de cuenta.
- [ ] **Máquina de turnos:** sale el papel.
- [ ] Los demás cajeros y el guarda saludan.

## 16. Tienda y negociación (núcleo de la demo)

- [ ] Hablar con Kevin: saluda y desliza un papel. Se abre la negociación.
- [ ] **Por texto:** pregunta "¿cuánto pago en total con el seguro?".
  - [ ] Kevin revela el seguro y el costo total.
  - [ ] Se marcan las preguntas clave.
- [ ] **Por voz:** grabar con el micrófono hasta 15 s; transcribe y responde. Sin micrófono, el texto sigue funcionando.
- [ ] **Me lo llevo:** firma del contrato (papel, firma, sello, caja) **antes** de cobrar. La cuota aparece en el HUD.
- [ ] **Mejor me voy:** no cobra nada.
- [ ] Pantallas y moto de la tienda tienen escena. La caja registradora de contado: pitido y bolsa.
- [ ] Sin conexión (desconecta el internet): el vendedor local responde igual.

## 17. Lugares de tareas del mes

Para cada uno revisa: la persona saluda, hay un gesto, el evento del game master llega y la decisión cambia la plata.

- [ ] Taller de Don Rafa.
- [ ] Pulpería de Doña Marta.
- [ ] Prestamista "gota a gota": billetes; el préstamo aparece y crece.
- [ ] Brayan, el de la pirámide: folleto.
- [ ] Cooperativa: firma.
- [ ] Soda: plato.
- [ ] El aviso "El game master está pensando" es chico, no tapa el centro y dura poco.
- [ ] Ya hecha la tarea del mes, el lugar dice "Ya pasaste por aquí este mes".
- [ ] Con internet: evento distinto cada vez. Sin internet: evento de respaldo, sin errores.
- [ ] Los eventos nunca hablan de un carro, una moto, una refri o una lavadora que el jugador no tiene.

## 18. Agencias, finca y vecino

- [ ] **Agencia de carros** (Don Chepe):
  - [ ] catálogo;
  - [ ] negociación;
  - [ ] firma y llaves;
  - [ ] el carro cobra marchamo, seguro y gasolina cada mes.
- [ ] **Inmobiliaria:** fotos de las casas; alquilar o comprar con firma y llaves.
- [ ] **Finca** (Don Beto): vacas y heno; invertir con firma; el resultado llega con el tiempo.
- [ ] **Casa de Don Mario** (vecino) y las casas en venta o alquiler: el letrero dice "tendría que ir a la inmobiliaria".
- [ ] Entrar por la puerta de una casa y salir: vuelves frente a esa misma puerta.

## 19. Calle y parque

- [ ] **Banca** (las 5): se sienta sobre el asiento, delante del respaldo; al levantarse, queda frente a la banca.
- [ ] **Fuente:** moneda, salpicón y el pensamiento del deseo. No te quedas dentro de la fuente.
- [ ] **Peatones** (incluidos la estudiante y el repartidor):
  - [ ] saludas y te saludan;
  - [ ] el subtítulo se va **al alejarte**.
- [ ] Tras hablar con alguien, la E no repite hasta que te alejes y vuelvas.
- [ ] **Puesto "INVIERTE YA 300%":** habla con subtítulos.

## 20. Playa

- [ ] Sin bañador, al acercarte al mar sale "Mejor me compro un bañador".
- [ ] Con bañador:
  - [ ] la orilla (E) muestra la escena de nadar con salpicaduras;
  - [ ] sube el ánimo;
  - [ ] al terminar quedas en la arena sin aviso.
- [ ] Segunda vez en el mes: "ya me di mi chapuzón".
- [ ] El muelle se camina sin avisos.

## 21. Celular (P)

- [ ] Al pulsar P, el personaje saca el celular del bolsillo, teclea y se abre. Al cerrarlo, lo guarda.
- [ ] Fondo de pantalla, reloj e íconos dibujados.
- [ ] **SINPE:**
  - [ ] enviar plata (escribe monto);
  - [ ] la notificación al recibir la quincena;
  - [ ] el estafador por SINPE llama y el teléfono vibra.
- [ ] **App de metas:** 3 metas con barra de progreso y celebración al cumplir una.
- [ ] **Recibos:** pagar desde el celular.
- [ ] Mensajes y avisos se leen completos; el teclado no mueve al personaje mientras escribes.
- [ ] Esc vuelve atrás y P cierra.

## 22. Dinámicas ticas y eventos

- [ ] **Tanda quincenal en la soda:** entrar, pagar cada quincena, cobrar tu turno, y el riesgo de que alguien no pague.
- [ ] **Rifa del barrio:** comprar un número.
- [ ] **Black Friday:** la pantalla "sin prima" a cuotas.
- [ ] **Diciembre:** el aguinaldo entra y llegan las ofertas.
- [ ] **Marchamo** (si tienes vehículo).
- [ ] **Horas extra:** aceptar todo, la mitad o nada.
- [ ] Emergencias (tubo reventado, médica) con sus opciones.

## 23. Fin del año y reporte

- [ ] Al cerrar el último mes: "Armando tu año", con el resumen del año (Muse Wonder) o el de respaldo sin internet.
- [ ] La postal del año con las cifras dibujadas por el juego (la imagen sin texto).
- [ ] **Boleta de calificaciones** de la A a la D por concepto, que cuadra con lo jugado.
- [ ] Metas cumplidas y no cumplidas.
- [ ] Volver al menú. La partida queda como terminada.

## 24. Pausa

- [ ] Esc en el pueblo: pausa. El tiempo y el reloj se detienen.
- [ ] Reanudar, Ajustes y Volver al menú (guarda y pasa por la pantalla de carga).
- [ ] Desde el menú, Continuar retoma la misma partida.

## 25. Inglés

Cambia a English y repite:
- [ ] menú, créditos, nueva partida, apertura y HUD;
- [ ] una escena animada de la casa y una del banco;
- [ ] la negociación (Kevin responde en inglés);
- [ ] el celular y el reporte final.

Anota cada texto que siga en español (incluidos los letreros del pueblo y los nombres "Teller" y "The lender").

## 26. Sonido

- [ ] Música del menú, del pueblo, de la tienda, del banco y de la casa, sin cortes bruscos al cambiar de escena.
- [ ] Efectos de las escenas: billetes, impresora, sello, sartén, ducha, pitido, teclas, timbre, despertador, moneda, lapicero, golpes, salpicón, comer.
- [ ] Voces de los NPC al hablar, suaves.
- [ ] Con el sonido desactivado no suena nada.

## 27. Rendimiento y estabilidad

- [ ] FPS en el pueblo con mucha gente y carros: sin tirones. Anota el número aproximado.
- [ ] Juega 30 minutos seguidos: sin cierres ni lentitud creciente.
- [ ] Entra y sal de 10 interiores seguidos: sin errores.
- [ ] Revisa el log del jugador (`%USERPROFILE%\AppData\LocalLow\<empresa>\ColonQuest\Player.log`) y busca `Exception`, `NullReference` y `error`.

## 28. Lo que NO está hecho (no es falla, no lo busques)

- El año de 12 meses (hoy la partida dura 3).
- La mascota.
- Los interiores de la pulpería y del taller.
- Los animales nuevos.
- La cama con suero en el hospital tras el atropello.

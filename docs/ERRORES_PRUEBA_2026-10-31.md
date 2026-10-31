# Errores de la prueba en el motor (31 de octubre)

Jefferson los encontró jugando en el motor con windows_r19. Cada punto lleva su estado.

## Tanda 1

### Pantallas iniciales y HUD
- [x] T1-01 La pantalla "Desarrollado por DoWhile Team" dura más que el logo de Unity.
  Hecho: El splash de DoWhile Team dura 4 s (Antigravity).
- [x] T1-02 El dinero entra al HUD solo cuando la madre lo entrega.
  Hecho: El HUD muestra ₡0 durante la intro hasta que la caja cambia de manos; la mamá dice la cifra desde el estado.

### Casa de la madre
- [x] T1-03 Al decirle la madre que salga, el personaje se teletransporta a la puerta: debe caminar.
  Hecho: `Interaction.Walk` ya no salta: si el personaje se traba, apaga sus pies y sigue caminando.
- [x] T1-04 Si sale y vuelve a entrar, la madre desaparece.
  Hecho: La mamá vive en la sala con un Thing de consejos; la intro la lleva al cuarto. Se borró `OpeningActor`.
- [x] T1-05 El zaguatico parece una alfombra: rediseñarlo como perro.
  Hecho: El zaguatico se dibuja de perfil, acostado en su cama (Antigravity).
- [x] T1-06 "Acostarte un rato" no muestra al personaje acostándose.
  Hecho: De día la cama es una siesta corta con el personaje acostado y las Z.
- [x] T1-07 Ropero: al menos tres mudadas para elegir (bañador y dos más).
  Hecho: Ropero con ropa de diario, de domingo, deportiva y el bañador; se guarda en la partida y se aplica en cada escena.
- [x] T1-08 Baño: falta la opción de usar el servicio.
  Hecho: Thing `Toilet` "Usar el servicio" con su escena (se oscurece y suena la cadena).
- [x] T1-09 Ducha: se baña con ropa. Simular que se la quita, con censura en el cuerpo.
  Hecho: La ducha quita la ropa con una cuadrícula de censura y la devuelve aunque se salte la escena.
- [x] T1-10 Se interactúa a través de las paredes (baño desde la cocina, cuartos hacia la cocina).
  Hecho: Una estación detrás de una pared interior (`WallCollider`) ya no se ofrece.
- [x] T1-11 Cocinar: animación incoherente y explicar que suma porque se ahorra la comida de afuera.
  Hecho: Cocinar pasa por la refri, la tabla y el fogón, y el resultado explica que suma la comida de afuera que no se pagó.
- [x] T1-12 Se teletransporta al sentarse a la mesa y en el sillón.
  Hecho: Mismo arreglo de T1-03; test de PlayMode que mide el salto máximo por frame en el sillón, la tele y la mesa.

### Banco
- [x] T1-13 Cajeros automáticos: opción real de sacar dinero.
  Hecho: El cajero automático saca ₡10 000, ₡25 000 o ₡50 000 de los ahorros y enseña el estado de cuenta.
- [x] T1-14 No se puede hablar con la encargada de créditos.
  Hecho: Thing para el gerente y la asesora de créditos, con escena y consejos.
- [x] T1-15 Arbustos con fallas: a la derecha al entrar a créditos y a la izquierda de la puerta de gerencia.
  Hecho: Se quitaron las plantas de las oficinas y las dos del vestíbulo junto a sus puertas.
- [x] T1-16 Caja 4: opción de depositar efectivo.
  Hecho: La caja 4 recibe depósitos de efectivo a los ahorros.
- [x] T1-17 Las cajeras parecen paradas sobre los carteles; un cartel tiene información incoherente.
  Hecho: El cartel CAJA n bajó al frente del mostrador; la pizarra muestra el tipo de cambio.
- [x] T1-18 Banco vacío: clientes con rutinas variables y el jugador espera su turno unos segundos.
  Hecho: Cuatro clientes con diligencias al azar que no cruzan la soga ni el mostrador; el jugador espera su turno (máximo 8 s).

### Ciudad
- [x] T1-19 La valla del mirador no tiene colisión.
  Hecho: La valla del mirador tiene collider (Antigravity).
- [x] T1-20 Fuente del banco: al caminar al lado, el juego cree que el personaje se quiere meter.
  Hecho: La fuente solo se ofrece de frente y la escena se queda delante del borde.
- [x] T1-21 El cartel de los cursos no tiene colisión.
  Hecho: El cartel de los cursos tiene collider (Antigravity).

## Tanda 2

### Entorno y exteriores
- [x] T2-01 El letrero de la casa inicial dice "Tu casa": debe decir que es la casa de la madre.
  Hecho: El letrero dice CASA DE MAMÁ (Antigravity).
- [x] T2-02 Poste de luz mal puesto frente a la entrada de la cooperativa.
  Hecho: El poste se movió de x = -33 a -34.5, lejos de la puerta (Antigravity).
- [x] T2-03 Muro incoherente en el extremo derecho del mapa.
  Hecho: El muro del borde este se cambió por una franja de árboles (Antigravity).
- [x] T2-04 Matas de café de la finca: rediseñarlas para que parezcan café real.
  Hecho: Matas de café con granos y surcos (Antigravity).
- [x] T2-05 Fachadas que parecen casas: inmobiliaria (y sus fotos de casas), Auto Don Chepe, hospital y estación de policía.
  Hecho: Fachadas propias para la inmobiliaria, la agencia, el hospital y la policía (Antigravity).

### Interiores
- [x] T2-06 La cooperativa no tiene interior: crearlo con trámites.
  Hecho: Nueva escena `Cooperativa` con la asesora, la tarea, el ahorro a plazo, sillas, afiche y planta.
- [x] T2-07 Casas de alquiler: se usan como propias al entrar. Bloquear si no son del jugador.
  Hecho: Las casas en venta o alquiler no llevan a la mamá y sus cosas solo dan un pensamiento hasta alquilarlas o comprarlas.
- [x] T2-08 Enriquecer el detalle de los interiores.
  Hecho: Relojes, extintores, dispensadores y más en el cuarto, la tienda, la finca, la inmobiliaria, el hospital y la agencia (Antigravity).
- [x] T2-09 Auto Don Chepe: no se puede hablar con los vendedores; agregar al encargado de financiamiento para negociar el carro.
  Hecho: Los vendedores y Don Chepe tienen Thing; la negociación del carro se abre en el escritorio de financiamiento.
- [x] T2-10 Ascensor del hospital: el personaje es más grande que el ascensor y en el segundo piso sale por el lado contrario.
  Hecho: Cabina y marco más grandes; al llegar el personaje sale caminando desde adentro hacia el pasillo, en los dos pisos.
- [x] T2-11 Celdas de la policía vacías: agregar reos.
  Hecho: Tres reos en las celdas que no son del jugador (Antigravity).
- [x] T2-12 Sodas vacías: agregar clientes.
  Hecho: Cuatro clientes sentados en la soda (Antigravity).

### NPC
- [x] T2-13 Todas las casas de vecinos tienen perro: que unas sí y otras no.
  Hecho: Solo Casa_Vecina_A y Casa_Almendros_4 tienen perro; el de la mamá no aparece en las casas en venta. La mamá ya no sale en la casa del vecino.
- [x] T2-14 De noche los NPC se quedan parados diciendo "ya está cerrado": que se vayan a dormir.
  Hecho: Componente `NightShift`: los de tiendas, soda y puestos caminan hacia atrás y desaparecen de noche.

### Animación, lógica y colisiones
- [x] T2-15 Trabajo en la soda: animación contextual (pila, refri, fogones, atender clientes).
  Hecho: Rutina por puesto con 2 vueltas: ayudante en la mesa, la refri y el fogón; cajero y jefes atienden mesas y la caja.
- [x] T2-16 Soga del muelle: el retroceso al no entrar al mar es demasiado largo y lo mete otra vez a la zona de la soga.
  Hecho: El empujón dura lo justo para salir de la zona (máximo 0,35 s), hacia la arena.

## Tanda 3

Lista de la tercera prueba, tal cual la pasó Jefferson. Rama `fix/SCRUM-58-playtest-oct31` (base `54fe0cd`).
Responsable: S = senior (Claude), A = Antigravity, J = solo se ve jugando.

### Interfaz y experiencia de usuario
- [x] T3-01 (A) Créditos de desarrolladores: en la pantalla que dice "Diseñado por DoWhile Team" se deben ocultar o eliminar los nombres individuales de los desarrolladores.
  Hecho (Antigravity): créditos solo con "DoWhile Team".
- [x] T3-02 (A) Transparencia del HUD: aplicar un poco de opacidad/transparencia al HUD general, ya que su opacidad total tapa información importante del juego.
  Hecho (Antigravity): fondo de la tarjeta del HUD semitransparente.
- [x] T3-03 (S) Sistema de misiones/tareas: implementar un menú o apartado visual en pantalla donde el jugador pueda revisar las tareas pendientes, ya que el flujo actual es poco intuitivo.
  Hecho: lista "Tareas de hoy" bajo el HUD (Tab la esconde), leída de la partida: día de llegada, trabajo, recibos, deudas, cierre del mes y dormir.
- [x] T3-04 (S) Gestión del tiempo muerto: como las tareas se completan rápido, implementar una mecánica para adelantar el tiempo o irse a dormir sin tener que esperar de forma pasiva a que termine el día después de trabajar.
  Hecho: en la cama de día, menú con "Descansar un par de horas" (+2 h) y, si ya trabajaste o pasó la mañana, "Dormir hasta mañana".

### Mecánicas y progresión
- [x] T3-05 (S) Animación de salida de la casa: eliminar por completo la animación al salir de la casa de la madre. Al despedirse, el personaje simplemente debe quedarse parado sin ejecutar ninguna acción automática de salida.
  Hecho: al despedirse el personaje se queda parado en el cuarto; ya no camina solo ni pasa al pueblo.
- [x] T3-06 (S) Paso del tiempo laboral: el tiempo del juego debe avanzar de manera progresiva y realista mientras el personaje está trabajando, no dar un salto repentino de horas únicamente al finalizar el turno.
  Hecho: el reloj avanza poco a poco durante la escena del turno, de la entrada a las 12:00.
- [x] T3-07 (A) Multas de tránsito: ajustar el valor exacto de la multa por saltarse un semáforo en rojo a 5,000 colones.
  Hecho (Antigravity): multa del semáforo en rojo de ₡5 000.
- [x] T3-08 (S) Día de introducción/rutina previa: lógica obligatoria en el día a día donde el jugador deba cumplir ciertos requisitos previos antes de iniciar su jornada laboral (por ejemplo: pagar el seguro, ir a visitar su nuevo cuarto de alquiler, etc.).
  Hecho: día de llegada en una partida nueva: el primer turno espera a conocer el cuarto y sacar el seguro de salud; ese turno nunca cuenta como tarde.
- [x] T3-09 (A) Sistema de cursos: rediseñar la lógica para llevar los cursos, haciéndola más fácil de entender. Crear o asignar un espacio/edificio físico específico para ir a recibirlos.
  Hecho (Antigravity): edificio "INA · CURSOS" en la acera sur con su estación en la puerta; modal más claro.
- [x] T3-10 (J→A) Puertas bloqueadas: revisar las casas del mapa, ya que algunas puertas están bloqueadas y no permiten la entrada.
  Hecho (Antigravity): puertas de casas desbloqueadas y triggers ajustados.

### Entorno, objetos y colisiones
- [x] T3-11 (S) Error crítico de colisión (barco): un barco pequeño está atravesando el muelle por encima. Corregir de inmediato ajustando las rutas o colisiones del barco.
  Hecho: los barcos van y vuelven, dos al oeste y uno al este del muelle, y dan la vuelta antes de llegar a él.
- [x] T3-12 (A) Poste en la Cooperativa: el poste de luz sigue apareciendo justo frente a la cooperativa y su colisión bloquea completamente la entrada al jugador.
  Hecho (Antigravity): el poste se movió fuera de la puerta de la Cooperativa.
- [x] T3-13 (A) Muro del límite derecho: el muro que delimita el lado derecho del mapa sigue visible y mal posicionado, cortando de forma poco natural áreas como la calle.
  Hecho (Antigravity): las calles del este terminan antes del acantilado.
- [x] T3-14 (A) Posicionamiento de las mascotas: los perros en los interiores de las casas aparecen encima de las mesas. Deben spawnear en el suelo o en camas para perros.
  Hecho (Antigravity): el perro descansa en el piso, fuera de la mesa.

### NPC e inteligencia artificial
- [x] T3-15 (A) Desaparición de NPC: al terminar sus turnos laborales, los NPC desaparecen de golpe. Programar que caminen físicamente hasta su casa asignada.
  Hecho (Antigravity): los peatones caminan a la puerta de su casa al anochecer y vuelven en la mañana.
- [x] T3-16 (S) Interacciones y negociaciones: sistema de diálogo para negociar con NPC clave del pueblo.
  Hecho: en el prestamista y en Invierte Ya sale "Hablar y negociar (texto o voz)" antes de escuchar la oferta.
  - [x] T3-16a (S) Invertir Ya (300%): actualmente no hay opción de negociar con ellos.
    Hecho: Invierte Ya responde por reglas: admite de dónde sale la plata, que no hay contrato ni SUGEF, solo si le preguntan.
  - [x] T3-16b (S) Prestamista de dinero rápido: habilitar opciones de negociación o pago para el NPC que presta con intereses excesivos.
    Hecho: el prestamista dice la tasa (20% al mes) y el total real (₡100 000 se vuelven ₡172 800 en 3 meses) cuando le preguntan.
- [x] T3-17 (A) Vida en el mapa:
  Hecho (Antigravity).
  - [x] T3-17a (A) Playa y cancha: NPC pasando el rato en la playa y otros jugando en la cancha de fútbol.
    Hecho (Antigravity): gente en la playa y jugadores en la cancha.
  - [x] T3-17b (A) Torre de salvavidas: el NPC salvavidas no está en su puesto, la torre está vacía.
    Hecho (Antigravity): salvavidas en la torre.
- [x] T3-18 (A) Animación de las gaviotas: las gaviotas de la playa están estáticas y parecen piedras. Agregarles animaciones de movimiento o vuelo.
  Hecho (Antigravity): gaviotas que picotean y vuelan cuando te acercás.

### Evento de cobradores y sistema de negociación
- [x] T3-19 (S) Animación y sincronización con el prólogo: el evento de los cobradores debe ejecutarse literalmente como lo describe el texto: un carro negro polarizado frena físicamente frente al jugador, se bajan dos hombres y caminan hacia el personaje para realizar el cobro.
  Hecho: el carro frena, se bajan dos cobradores, caminan hasta el jugador, hablan y después sale el menú; al elegir vuelven al carro.
- [x] T3-20 (S) Opción interactiva de negociación: al ser abordado por los cobradores, el menú de interacción debe incluir obligatoriamente una opción de "Negociar".
  Hecho: el menú de los cobradores tiene "Negociar con tus palabras"; el arreglo (mitad hoy, resto con recargo) aparece solo si lo pedís.
- [x] T3-21 (S) Soporte de input por texto y voz: la negociación debe soportar input escrito (teclado) y reconocimiento de voz (micrófono), para que el jugador negocie con sus propias palabras.
  Hecho: ventana de conversación con caja de texto y botón Hablar (micrófono). La voz se transcribe en el backend; sin backend pide escribir.

### Sistema de guardado y salida del juego
- [x] T3-22 (S) Mensaje de confirmación exacto: al intentar salir de la partida, mostrar obligatoriamente una alerta con el texto exacto: "¿Seguro? Se pierde la partida, ¿quieres guardar?".
  Hecho: "Volver al menú" abre el cuadro con el texto exacto.
- [x] T3-23 (S) Opciones de interacción: el cuadro debe tener únicamente dos botones, "Sí" y "No": Sí guarda y cierra, No solo cierra.
  Hecho: solo "Sí" (guarda y sale) y "No" (sale sin guardar).

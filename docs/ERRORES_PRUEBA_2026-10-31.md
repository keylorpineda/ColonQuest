# Errores de la prueba en el motor (31 de octubre)

Jefferson los encontró jugando en el motor con windows_r19. Cada punto lleva su estado.

## Tanda 1

### Pantallas iniciales y HUD
- [ ] T1-01 La pantalla "Desarrollado por DoWhile Team" dura más que el logo de Unity.
- [x] T1-02 El dinero entra al HUD solo cuando la madre lo entrega.
  Hecho: El HUD muestra ₡0 durante la intro hasta que la caja cambia de manos; la mamá dice la cifra desde el estado.

### Casa de la madre
- [x] T1-03 Al decirle la madre que salga, el personaje se teletransporta a la puerta: debe caminar.
  Hecho: `Interaction.Walk` ya no salta: si el personaje se traba, apaga sus pies y sigue caminando.
- [x] T1-04 Si sale y vuelve a entrar, la madre desaparece.
  Hecho: La mamá vive en la sala con un Thing de consejos; la intro la lleva al cuarto. Se borró `OpeningActor`.
- [ ] T1-05 El zaguatico parece una alfombra: rediseñarlo como perro.
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
- [ ] T1-19 La valla del mirador no tiene colisión.
- [x] T1-20 Fuente del banco: al caminar al lado, el juego cree que el personaje se quiere meter.
  Hecho: La fuente solo se ofrece de frente y la escena se queda delante del borde.
- [ ] T1-21 El cartel de los cursos no tiene colisión.

## Tanda 2

### Entorno y exteriores
- [ ] T2-01 El letrero de la casa inicial dice "Tu casa": debe decir que es la casa de la madre.
- [ ] T2-02 Poste de luz mal puesto frente a la entrada de la cooperativa.
- [ ] T2-03 Muro incoherente en el extremo derecho del mapa.
- [ ] T2-04 Matas de café de la finca: rediseñarlas para que parezcan café real.
- [ ] T2-05 Fachadas que parecen casas: inmobiliaria (y sus fotos de casas), Auto Don Chepe, hospital y estación de policía.

### Interiores
- [x] T2-06 La cooperativa no tiene interior: crearlo con trámites.
  Hecho: Nueva escena `Cooperativa` con la asesora, la tarea, el ahorro a plazo, sillas, afiche y planta.
- [x] T2-07 Casas de alquiler: se usan como propias al entrar. Bloquear si no son del jugador.
  Hecho: Las casas en venta o alquiler no llevan a la mamá y sus cosas solo dan un pensamiento hasta alquilarlas o comprarlas.
- [ ] T2-08 Enriquecer el detalle de los interiores.
- [x] T2-09 Auto Don Chepe: no se puede hablar con los vendedores; agregar al encargado de financiamiento para negociar el carro.
  Hecho: Los vendedores y Don Chepe tienen Thing; la negociación del carro se abre en el escritorio de financiamiento.
- [x] T2-10 Ascensor del hospital: el personaje es más grande que el ascensor y en el segundo piso sale por el lado contrario.
  Hecho: Cabina y marco más grandes; al llegar el personaje sale caminando desde adentro hacia el pasillo, en los dos pisos.
- [ ] T2-11 Celdas de la policía vacías: agregar reos.
- [ ] T2-12 Sodas vacías: agregar clientes.

### NPC
- [ ] T2-13 Todas las casas de vecinos tienen perro: que unas sí y otras no.
- [x] T2-14 De noche los NPC se quedan parados diciendo "ya está cerrado": que se vayan a dormir.
  Hecho: Componente `NightShift`: los de tiendas, soda y puestos caminan hacia atrás y desaparecen de noche.

### Animación, lógica y colisiones
- [x] T2-15 Trabajo en la soda: animación contextual (pila, refri, fogones, atender clientes).
  Hecho: Rutina por puesto con 2 vueltas: ayudante en la mesa, la refri y el fogón; cajero y jefes atienden mesas y la caja.
- [x] T2-16 Soga del muelle: el retroceso al no entrar al mar es demasiado largo y lo mete otra vez a la zona de la soga.
  Hecho: El empujón dura lo justo para salir de la zona (máximo 0,35 s), hacia la arena.

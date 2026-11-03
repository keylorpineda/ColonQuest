# ColonQuest: bugs de la alpha y diseño del modo historia (2026-10-29)

Parte 1: 15 bugs encontrados al jugar. Parte 2 (al final): el modo historia definido con Jefferson.

Rama: `feat/SCRUM-58-alpha-fixes` (PR 58), commit `feef017`. Rutas relativas a `unity/Assets/`.
Cada punto dice: qué se ve, la causa encontrada en el código (o "probable" si hay que confirmarla en pantalla), qué
cambiar, cómo verificarlo y qué otros lugares tienen el mismo problema.

Reglas del repo para quien arregle: código y comentarios en inglés, textos del juego en español con su entrada en inglés
en `Scripts/World/Translations.cs`, el dinero solo lo mueve `FinanceEngine`, escenas regeneradas en commit aparte,
y no tocar `server/` ni `shared/`.

Antes de los 13 puntos, hay **tres causas comunes** que explican varios a la vez. Conviene arreglarlas primero:

- **A. Caminar al centro de un mueble sólido.** Las escenas animadas (`Scripts/UI/GameUi.Scenes.cs`) hacen
  `WalkTo(Here(station))`, y el centro de muchas estaciones es el centro del mueble (`BuildThing` en
  `Editor/MapBuilder.Things.cs` usa la posición del objeto). El jugador choca, camina 4 s contra el mueble y
  `Interaction.Walk` (`Scripts/World/Interaction.cs`) lo **teletransporta dentro** del mueble.
- **B. El colisionador se reactiva dentro del mueble.** `WalkInto` apaga el colisionador para subirse a la cama, al
  sillón o a la banca, y el bloque `finally` de `InteractionRunner.Run` lo vuelve a encender **sin sacar antes al
  jugador**. La física lo empuja hacia un lado (saltos, quedar trabado).
- **C. Manos detrás del pecho.** En `Editor/MapBuilder.People.cs` la mano (`Hand`, orden `OrderPlayer + 5`) y el
  antebrazo (+3) se dibujan **debajo** del torso (+6). Las poses que llevan la mano al pecho o a la boca (`Eat`, `Read`,
  `Type`, `Count`, `Sign`, `Wash`, `Cook`) esconden la mano detrás del cuerpo y parecen rotas.

---

## 1. El diseño de los personajes se corrompió en la barbilla, los codos y la cintura

**Causas (en `Editor/MapBuilder.People.cs`):**

- **Barbilla:**
  - `JawShade` (círculo de 0.32 × 0.08 en `y = 0`, orden +13) se dibuja **encima** del contorno de la cara (`Face_Outline`, orden +11).
  - La cara termina en `y ≈ -0.03`, así que la sombra tapa el borde oscuro del mentón y lo "rompe".
  - Con `SquareJaw` (Don Rafa) se suman `Jaw` y `JawCover` y quedan costuras.
- **Cuello y barbilla al moverse:** `Neck` es hijo de `Visual`, pero `Collar` es hijo de `Torso`. `PlayerAnimator` rota
  el torso al caminar (2.5°) y en las poses (`Lean` hasta 7°), y escala el pecho al respirar. El cuello y la cabeza no
  siguen al torso, así que se abre un hueco o un escalón bajo la barbilla.
- **Codos:**
  - El antebrazo nace 0.03 por encima del pivote del codo y se dibuja detrás de la manga.
  - Al doblar el codo más de 40° (poses `Type`, `Eat`, `Count`), la esquina del antebrazo y su contorno salen por un costado de la manga.
  - El óvalo `SleeveEdge` no tapa la unión.
- **Cintura:** probable. Al quitar la faja, el borde inferior redondeado del torso queda directo sobre las dos piernas
  separadas (hueco de 0.04 entre ellas, y más con `Width` < 1). En las poses con `Drop` y `LegL/LegR` abiertas (`Sit`,
  `Eat`, `Read`) las piernas se abren y el hueco crece. Confirmar en pantalla.

**Qué cambiar:**
- Mover `JawShade` dentro de la cara (centro `y ≈ 0.04`, alto ≤ 0.06) o dibujarlo debajo del contorno.
- Hacer `Neck` hijo de `Torso` y que `PlayerAnimator` aplique a `Head` la misma inclinación que al torso, o desplazar la cabeza con el torso.
- Agregar una "rótula" (círculo del color de la manga o de la piel, Ø ≈ 0.11) en el pivote del codo, encima de manga y antebrazo, o alargar el solape de la manga.
- Agregar una pieza de cadera (rectángulo redondeado del color del pantalón, ancho `0.42·W`, alto 0.12) entre torso y piernas, debajo del torso.
- Causa C: dibujar mano y antebrazo **encima** del torso cuando la pose los lleva al frente (un `sortingOrder` alterno que active el animador), o siempre encima con el hombro escondido.

**Cómo verificar:** pararse frente a Kevin, Don Rafa y el jugador y comparar con `docs/art/characters-v2/elenco_v2.png`.
Caminar 10 s y mirar barbilla y cuello. Sentarse a la mesa, usar el cajero y abrir el celular (P): codos sin picos,
manos visibles frente al pecho.

**Similares:** todos los NPC con ficha (`Editor/MapBuilder.Cast.cs`) y todos los peatones, porque comparten el mismo esqueleto.

---

## 2. En la cárcel, al encerrarlo, uno no se queda dentro de una celda

**Causa (verificada):**
- `BuildJailLayout` (`Editor/MapBuilder.Interiors.Places.cs`) arma cada celda con barras de `left` a `x - 0.7` y de
  `x + 0.7` a `right`. Queda un **hueco de 1.4 sin reja ni puerta** (el comentario dice "a gate that is open").
- El jugador aparece en `SpawnIds.JailCell` (-7.6, 3.0), pero puede salir caminando.
- Lo único que se bloquea es la puerta a la calle (`Door.Locked` en `Scripts/UI/GameUi.Cell.cs`).

**Qué cambiar:**
- Agregar una reja-puerta en el hueco de la celda 0: colisionador más barras, componente `CellGate` en `Scripts/World`.
- Cerrarla al empezar la condena (en el lugar donde hoy se pone `Door.Locked = true`) y abrirla con la fianza o al cumplir la condena.
- Animar la reja deslizándose, con el sonido `Slam` al cerrar.
- Las otras 3 celdas pueden quedar cerradas siempre.

**Cómo verificar:** entrar a una casa ajena, esperar 9 s y, ya en la celda, intentar salir por el frente: no se puede.
Pagar la fianza: la reja se abre y se puede salir. Esperar la condena: lo mismo.

**Similares:** el arrestado hoy puede caminar hasta el juez o el mostrador durante la condena (ver punto 8).

---

## 3. La animación del arresto no existe, es demasiado simple

**Causa (verificada):** `ArrestSequence` (`Scripts/UI/GameUi.Incidents.cs`) solo congela al jugador, pinta la sirena
roja y azul sobre la pantalla 2.2 s y cambia de escena. No hay oficial, ni esposas, ni gesto del personaje.

**Qué cambiar:** reescribirlo con el marco `Interaction` (`Scripts/World/Interaction.cs`), en este orden:
1. Sirena (sonido y luces).
2. El oficial más cercano (`PoliceOfficer`) camina hasta el jugador.
3. El jugador levanta las manos: falta una pose nueva `HandsUp` en `Scripts/World/PersonPose.cs`.
4. Esposas: prop nuevo en `InteractionProps`, sonido metálico.
5. Caminan juntos 2 o 3 pasos.
6. Fundido a negro.
7. En la celda, la reja se cierra con `Slam` (punto 2).

Espacio debe saltarlo, y el cambio de juego (`session.Arrest()`) se aplica al final, como hoy.

**Cómo verificar:** entrar a una casa ajena y esperar 9 s. Se ve al oficial llegar, el gesto de las manos, las esposas,
y luego la celda. Probar también saltando con Espacio.

**Similares:**
- La multa por cruzar en rojo (`PoliceOfficer.StoppedPlayer`) tampoco tiene gesto del oficial ni del jugador.
- El atropello sí tiene animación (`HitAnimation`), pero la llegada al hospital no: falta la cama con suero.

---

## 4. Las aceras cerca de los ríos no dejan caminar y dicen que necesita un bañador

**Causa (verificada):**
- `WorldHints.Check` (`Scripts/World/Surfaces.cs`) marca como agua **toda la franja x entre `RiverXMin - 0.9` y `RiverXMax + 0.9`**
  (27.1 a 31.9) para cualquier `y` sobre la playa, salvo los tableros de los puentes (`OnDeck`).
- Ese margen de 0.9 cae sobre las aceras que corren junto al río. `WalkWatch` empuja al jugador hacia atrás (`Nudge`) y dice `WaterThought`.
- El texto es el del bañador, aunque en el río ni en el estanque se puede nadar.

**Qué cambiar:**
- Quitar el margen en las zonas caminables: usar el borde real del agua o restar las aceras del río.
- Hacer lo mismo con el estanque del parque (`Pond`, mismo margen de 0.9) y revisar el muelle.
- Usar un pensamiento distinto para río y estanque ("Mejor no me meto al río."), con su traducción.

**Cómo verificar:** caminar por las dos aceras junto al río de punta a punta y por los senderos alrededor del estanque
(19.5, -5.9): ningún empujón ni pensamiento. Meterse al río por fuera de la acera: sí sale el aviso.

**Similares:** el estanque y la playa (punto 13).

---

## 5. Al sentarse en la banca no se sienta realmente en la banca

**Causa (verificada):**
- La banca (`Bench` en `Editor/MapBuilder.Town.Props.cs`) tiene la estación en la raíz `(x, y)` y el asiento en `y + 0.28`, con un colisionador sólido de `y` a `y + 0.6`.
- La escena hace `WalkInto(me)` al punto `(x, y)` (debajo del asiento) y luego la pose `Sit` **baja** el cuerpo 0.2 (`Drop = -0.2` en `PersonPose.cs`). En vista cenital, el personaje queda delante de la banca, no sobre el asiento.
- Al terminar, el colisionador se enciende dentro de la banca y la física lo empuja (causa B).

**Qué cambiar:**
- Agregar en `Interaction` un paso `SitOn(asiento, frente)`:
  1. camina con colisión hasta el frente de la banca `(x, y - 0.6)`;
  2. sube sin colisión al asiento `(x, y + 0.3)`;
  3. se sienta con `Drop` cercano a 0 y las piernas hacia abajo;
  4. al levantarse vuelve al punto del frente **antes** de encender el colisionador.
- Usarlo en banca, sillón, mesa y sala de espera.

**Cómo verificar:** sentarse en las 5 bancas del pueblo: el cuerpo queda sobre el asiento, delante del respaldo. Al
levantarse, el jugador queda frente a la banca, sin saltos.

**Similares:** sillón de la casa (`Thing_Sofa`), mesa del comedor (`Thing_DiningTable`), sala de urgencias del hospital (`Thing_EmergencyWard`, se sienta en el aire).

---

## 6. Las animaciones de sentarse en general están muy mal y a veces se buguean

**Causas:**
- La 5 en todos los asientos.
- Causa A: si el centro de la estación es el mueble, el jugador camina 4 s contra él y aparece dentro.
- Causa B: la cama (`BedScene`), la ducha, el sillón y la mesa reactivan el colisionador dentro del mueble. Ese es el "a veces se buguea": depende de por dónde lo empuje la física.
- Causa C: en `Eat` y `Read` la mano queda detrás del pecho.
- La pose `Sit` abre las piernas ±26° y baja el cuerpo, en lugar de doblarlas. No hay rodillas en el esqueleto.

**Qué cambiar:**
- Aplicar la solución de la 5 (punto de frente más punto de asiento, colisionador encendido solo tras volver al frente).
- Para `Sit`: piernas casi rectas hacia abajo (`LegL/LegR` ±6°) y cuerpo con `Drop` de 0 a -0.05.
- Ver la causa C para las manos.

**Cómo verificar:** cama, sillón, mesa, banca y sala de espera del hospital, 3 veces seguidas cada uno, entrando desde
lados distintos: nunca queda trabado, nunca aparece dentro del mueble, nunca salta al terminar.

**Similares (todas en `GameUi.Scenes.cs` con `Here(station)` sin desplazamiento):**
- **Causa A:** `Thing_Fridge` (refri sólida), `Thing_Fountain` (fuente sólida de 3.7 × 3.0: el jugador termina dentro del agua), `Thing_Sink`, `Thing_Washer`, `Thing_Coffee`, `Thing_TicketMachine`, `Thing_FinePrint`.
- **Causa B:** `Thing_Shower` (`WalkInto`).
- Revisar cada una en pantalla y darles un punto de frente.

---

## 7. En el hospital, en vez de escaleras, que sea un ascensor

**Situación actual:**
- `BuildHospitalLayout` (`Editor/MapBuilder.Interiors.Places.cs`, "The stairs to the consulting rooms") dibuja escalones y una puerta `Door_Upstairs` que lleva a la escena `HospitalArriba`.
- Arriba hay otro letrero "ESCALERAS" con la puerta de bajada.

**Qué cambiar:**
- Cambiar el dibujo de los escalones por un ascensor en los dos pisos: marco metálico, dos hojas de puerta, botonera y la placa "ASCENSOR" con su traducción.
- Antes de cambiar de escena, una escena de `Interaction`:
  1. el jugador pulsa el botón (pose `Reach`);
  2. suena `Chime`;
  3. las hojas se abren deslizándose;
  4. entra;
  5. fundido;
  6. en el otro piso, `Chime` y las puertas se abren.
- La lógica de puertas (`Door`, `SpawnIds` de los dos extremos en `Scripts/World/MapLayout.cs`) se reutiliza tal cual; solo cambian el dibujo y la animación.

**Cómo verificar:** subir y bajar dos veces: el jugador aparece frente al ascensor del otro piso, sin quedar dentro de un mueble. Los textos salen en inglés con el idioma cambiado.

**Similares:** ninguno (es la única escalera del juego).

---

## 8. En la cárcel, el juez debería tener al menos una oficina privada

**Situación actual:** el juez está detrás de un estrado (`JudgeBench`) en la misma sala que el mostrador y las celdas, a la vista de todo.

**Qué cambiar:** en `BuildJailLayout`, cerrar con paredes interiores (`InnerWall`) un despacho en la esquina derecha:
- escritorio, sillas y bandera o balanza;
- puerta con placa "JUZGADO" o "DESPACHO DEL JUEZ";
- el juez y la estación `Thing_Judge` adentro.

Revisar que la escena del juez (`Thing_Judge` en `GameUi.Scenes.cs`) camine hasta un punto **dentro** del despacho y
que el pasillo no quede bloqueado.

**Cómo verificar:** entrar a la comisaría, ir al despacho, hablar con el juez (firma más golpes del mazo), salir. Ningún objeto tapa la puerta del despacho ni la salida a la calle.

**Similares:** con la reja del punto 2 cerrada, el arrestado no debe poder llegar al despacho durante la condena.

---

## 9. Cuando uno habla con un NPC y se aleja, el diálogo no se quita de inmediato

**Causa (verificada):**
- `SubtitleBar.Show` (`Scripts/UI/SubtitleBar.cs`) pone un tiempo fijo de 3 a 10 s según el largo del texto.
- Nada lo oculta cuando el jugador sale de la zona. `OnStationExited` en `Scripts/UI/GameUi.cs` solo quita la estación de la lista.

**Qué cambiar:**
- Guardar en `GameUi` qué estación originó el subtítulo (en `ShowThing` y en las escenas).
- En `OnStationExited` llamar a `subtitles.Hide()` si es esa estación.
- Como respaldo, ocultarlo si el jugador se aleja más de 3.5 unidades del NPC.
- Los pensamientos del jugador (sin nombre) pueden seguir con su tiempo.

**Cómo verificar:** hablar con un peatón, con Brayan y con Doña Marta y alejarse caminando: el subtítulo desaparece al salir de la zona. Quedarse quieto: el subtítulo dura lo de siempre.

**Similares:**
- Las líneas que dicen los personajes dentro de las escenas animadas (`Interaction.Spoke`) se quedan igual si el jugador sale de la escena.
- El vendedor de la negociación habla con `AudioDirector.Talk` además del subtítulo.

---

## 10. Después de interactuar con E, que haya que alejarse y volver para poder interactuar otra vez

**Causa (verificada):** en `GameUi.Update` (`Scripts/UI/GameUi.cs`) cada pulsación de E con una estación cerca vuelve a
llamar a `Interact`. Las cosas cotidianas (`ShowThing`) rotan sus líneas en cada uso, así que se puede repetir sin fin.

**Qué cambiar:**
- Agregar un conjunto `usedStations` en `GameUi`: al interactuar se agrega la estación.
- `PromptFor` no muestra el aviso ni acepta E mientras esté en el conjunto.
- `OnStationExited` la saca del conjunto.
- Las ventanas que se cierran solas (el banco con "Todavía no") cuentan como uso.

**Cómo verificar:** pulsar E en la refri: sale la escena. Pulsar E otra vez sin moverse: no pasa nada y no hay aviso. Salir de la zona y volver: aparece el aviso y funciona. Probar con un peatón, la banca y el cajero automático.

**Similares:** la tecla P del celular se puede repetir mientras el jugador está quieto (eso está bien, es un menú, pero que no se apile con la escena del celular).

---

## 11. El aviso "El game master está pensando" tapa el medio de la pantalla y es lento

**Causa (verificada):**
- `BuildLoading` en `Scripts/UI/GameUi.cs` crea una tarjeta de 640 × 120 anclada al **centro** de la pantalla.
- La espera es la del servidor (`EventTimeoutSeconds = 8f` en `Scripts/Api/ApiConfig.cs`).
- Además, con las escenas animadas, la petición del evento (`GameSession.BeginTask`) **empieza recién cuando termina la animación**. La animación y la espera se suman, en vez de solaparse.

**Qué cambiar:**
- **Aviso:** tarjeta chica (unos 360 × 60) en una esquina (arriba a la derecha, bajo el HUD) o como pastilla junto al
  HUD, con los puntos animados. Que no bloquee la vista del pueblo.
- **Velocidad:** pedir el evento al **inicio** de la escena del lugar de la tarea (`TalkScene` en `GameUi.Scenes.cs`).
  1. Agregar a `GameSession` un método que solo pida y guarde el evento (`PrefetchTask`).
  2. Hacer que `BeginTask` use el evento guardado si ya llegó.
  3. Así la animación de 2 o 3 s esconde casi toda la espera.
- No tocar el servidor (es de Keylor). Si se quiere bajar `EventTimeoutSeconds`, coordinarlo con él, porque el respaldo (`FallbackEvents`) entra cuando se agota.

**Cómo verificar:** con el backend de Render, ir al taller de Don Rafa y medir con reloj desde E hasta que abre el
evento. Antes y después del cambio, 3 veces cada uno. El aviso nunca tapa al jugador ni el centro.

**Similares:** el resumen del año ("Armando tu año", 25 s) usa la misma tarjeta central.

---

## 12. El ropero está bugueado al intentar abrirlo

**Causa probable (confirmar en pantalla):**
- El ropero sólido está en (-7.6, 2.3) con alto 2.2 (de `y` 1.2 a 3.4). La estación `Station_Wardrobe` está en (-7.6, 1.0), pegada a su borde.
- La escena del ropero (`case StationType.Wardrobe` en `GameUi.Scenes.cs`) hace:
  1. `WalkTo(-7.6, 1.0)`: el pie del jugador (radio 0.3, desplazado -0.1) toca el mueble antes de llegar, así que puede quedarse empujando 4 s y luego teletransportarse (causa A);
  2. `Face(Here(station, -1f, 0.5f))`: mira hacia la izquierda (la pared) en lugar del ropero, que está arriba;
  3. luego abre la ventana.
- Si ya tiene bañador, `ShowWardrobe` solo dice un pensamiento: la escena entera (ruido, papel) termina en un pensamiento y parece fallar.

**Qué cambiar:**
- Punto de frente en (-7.6, 0.4).
- `Face` hacia arriba (el ropero).
- Que la puerta del ropero se abra de verdad (una hoja que gira o se desliza) en lugar del papel volando.
- Con bañador, mostrar el bañador colgado y el pensamiento.

**Cómo verificar:** abrir el ropero desde abajo, desde la derecha y desde la cama, sin bañador y con bañador.
Nunca se queda trabado. La ventana de compra aparece después del gesto. Con bañador sale el pensamiento sin ventana.

**Similares:** causa A en la refri y la fuente (punto 6).

---

## 13. El tema del bañador también está bugueado

**Causas (verificadas):**
- **Después de nadar sale "Mejor me compro un bañador".** `BeachScene` deja al jugador en `y = -26.5` (`shore`), dentro
  del margen de agua de `WorldHints` (hasta `ShoreY + 0.9 = -26.3`). Al terminar la escena, `WalkWatch` dice el
  pensamiento del bañador y lo empuja hacia arriba.
- **El aviso no sabe si ya tiene bañador.** `WalkWatch` (`Scripts/World/WalkWatch.cs`) siempre dice `WaterThought`,
  aunque `GameLoop.HasSwimsuit` sea verdadero, porque `World` no ve `Game`.
- **El mismo texto sale en el río y el estanque**, donde no se nada (punto 4).

**Qué cambiar:**
- Al terminar `BeachScene`, devolver al jugador a la arena (`y ≈ -25.4`) antes de soltarlo.
- Agregar a `WalkWatch` una bandera estática que ponga `GameUi`: `WalkWatch.HasSwimsuit = session.Loop.HasSwimsuit` en cada `Refresh`. Con bañador, la orilla del mar dice "Puedo darme un chapuzón en la playa (E)" o nada.
- Separar los textos de mar, río y estanque, con sus traducciones.

**Cómo verificar:**
1. Sin bañador, ir al mar: sale el pensamiento del bañador.
2. Comprarlo en el ropero, volver: ya no sale; pulsar E en la orilla: nada, salpica y sube el ánimo.
3. Al terminar, el jugador queda en la arena, sin empujón ni pensamiento.
4. Segunda vez en el mes: pensamiento de "ya me di mi chapuzón".
5. Cerrar el mes y repetir: vuelve a funcionar.

**Similares:** la escena de la ducha y la de la fuente sueltan al jugador donde el aviso de agua no aplica (no es el
pueblo o no hay margen), pero conviene revisar la fuente por la causa A.

---

## 14. Los carros no respetan los semáforos

**Causa (verificada):**
- Los carros del pueblo se mueven con `Drift.ConfigureLane` (`Scripts/World/Drift.cs`, creado en `Editor/MapBuilder.City.cs`): una velocidad constante en x que da la vuelta al salir del mapa.
- `Drift` no sabe nada de los cruces. `CrossingSignal` (`Scripts/World/CrossingSignal.cs`) solo controla la luz del peatón (`WalkLight`) y la multa por cruzar en rojo.
- `TrafficCar` solo detecta el atropello.
- Por eso los carros pasan con la luz del peatón en verde, y también se montan unos sobre otros: no frenan ante el carro de adelante.

**Qué cambiar:**
- Crear `CarDriver` en `Scripts/World` (o ampliar el modo carril de `Drift`) con una función pura y testeable, del estilo `CarRules.ShouldStop(carX, dir, stopLineX, walkLight, carAheadX)`.
- Cada cruce expone su línea de parada a cada lado. Cuando `WalkLight` está en verde (que para el carro es rojo), el carro frena con suavidad antes de la línea y arranca al cambiar.
- Si el cruce cambia a rojo con el carro ya encima de la línea, sigue de largo, como en la vida real.
- Mantener una distancia mínima con el carro de adelante en el mismo carril: frenan en fila.
- Pruebas de EditMode para la función pura: frena antes de la línea, no frena si ya la pasó, respeta la distancia.
- Mirar también `CollectorCar.cs` (el carro de los cobradores): debe respetar lo mismo, salvo cuando se detiene frente al jugador.

**Cómo verificar:** pararse en cada cruce del pueblo 2 ciclos completos (unos 40 s): con el muñequito verde ningún
carro pisa la cebra, con rojo pasan. Los carros forman fila y no se montan. Cruzar con verde nunca termina en atropello.

**Similares:** el atropello (`HitAnimation`) hoy puede pasar cruzando con verde, lo que contradice la multa por cruzar en rojo.

---

## 15. Hay que arreglar la ventana de inicio (menú principal)

**Qué se ve:** Jefferson la reporta rota o mal resuelta. Falta que él detalle qué falla exactamente. Anotarlo aquí
antes de empezar: _______________________________________________

**Situación actual:**
- El menú se rehízo en el bloque A (`Scripts/UI/MainMenu.cs`, `Editor/MapBuilder.Menu.cs`, `Scripts/UI/LoadingScreen.cs`, `Scripts/Game/MenuEntries.cs`, `Scripts/Game/MenuMotion.cs`, `Scripts/UI/ButtonHover.cs`, `Scripts/Game/BuildInfo.cs`):
  - fondo en capas con parallax;
  - hover con escala y borde;
  - sonido de clic;
  - "Alpha 0.1.16" arriba a la izquierda y "Design by DoWhile Team" abajo;
  - pantalla de carga;
  - créditos y aviso de jugar online nuevos.
- **Nada de eso se ha visto en pantalla todavía:** la RONDA 8 de Antigravity dejó el humo del menú como "no probado". La lista original de lo pedido está en `C:\Users\USUARIO\ColonQuestBuild\PENDIENTES_MENU_Y_CARGA.md`.

**Qué revisar y arreglar (abrir el build `windows_r16` o el editor en la escena `Menu`):**
- **Botones:** que no se pisen entre sí ni con el título, en 16:9, en 4:3 y en ventana chica, y en WebGL.
- **Hover:** visible con mouse y con teclado (flechas y Enter).
- **Textos:**
  - "Alpha 0.1.16" y "Design by DoWhile Team" legibles y fuera de los botones;
  - créditos y aviso de jugar online sin texto que se salga de su tarjeta;
  - todo en inglés al cambiar el idioma.
- **Pantalla de carga:** aparece al entrar a Modo historia y al volver al menú desde Pausa, y desaparece sola.
- **Música:** la del menú no sigue sonando en el pueblo.
- **Integración con el modo historia:** "Modo historia" debe abrir la lista de 3 partidas guardadas (idea 3) y la pantalla de nueva partida (idea 1). Diseñar la ventana de inicio ya pensando en eso, para no rehacerla dos veces.

**Cómo verificar:**
- Captura del menú en 16:9 y en 4:3, en español y en inglés.
- Pasar el mouse y el teclado por cada botón.
- Abrir y cerrar créditos y el aviso online.
- Entrar a Modo historia y volver al menú dos veces.
- Cero excepciones en la consola.

**Similares:** el menú de pausa (`Scripts/UI/PauseMenu.cs`) y Ajustes (`Scripts/UI/SettingsPanel.cs`) usan los mismos
componentes de UI (`UiKit`): revisar que no tengan los mismos solapes.

---

# MODO HISTORIA (definido con Jefferson el 2026-10-29: entran TODAS las ideas)

Hoy el juego dura 3 meses fijos (`GameRules.MonthsToPlay = 3`, `TasksPerMonth = 3`) y no guarda nada. El día y la
noche (`Scripts/World/DayCycle.cs`) avanzan con las tareas hechas, no con un reloj. Todo lo de abajo cambia eso.

Reglas que no cambian: el dinero lo mueve `FinanceEngine` (en Unity, `Scripts/Finance`), el game master solo cuenta la
historia, y `server/` y `shared/` son de Keylor. Si algo necesita un endpoint nuevo, se le pide a él.

## Tiempo: el mes dura 4 días y cada día es una semana

- **Calendario:** 1 mes = 4 días de juego, y cada día representa una semana (semana 1 a 4). El HUD muestra "Mes 3 ·
  Semana 2 · 14:30".
- **Reloj del día:** de 06:00 a 22:00. Propuesta: 1 hora de juego = 15 s reales, así un día dura unos 4 minutos y un mes
  unos 16. Ajustable en `GameRules` y con una opción "velocidad del tiempo" en Ajustes (normal y rápida), para la demo
  de 3 minutos y para los jueces.
- **Franjas:**
  - mañana (06:00 a 12:00): trabajo;
  - tarde (12:00 a 18:00): libre, trámites, negocios;
  - noche (18:00 a 22:00): los locales cierran con letrero CERRADO, las farolas se encienden y los NPC se van a casa.
  - A las 22:00 hay que dormir.
- **Dormir pasa el día:** la cama (ya existe la escena) adelanta a las 06:00 del día siguiente. Si no duerme, a las
  22:00 cae dormido donde esté (escena corta), amanece cansado y baja el ánimo. La regla "sin dormir baja el ánimo"
  ya está en `GameLoop.Rest` y `CloseMonth`; hay que pasarla de mensual a diaria.
- **Pago:** quincena, como en Costa Rica: la mitad del salario neto al final del día 2 y la otra mitad al final del día
  4, con el aviso de SINPE en el celular. El cierre del mes (cuotas, tarjeta, intereses, gastos) se hace al final del
  día 4. El motor hoy paga una vez al mes: hay que partir `RunMonthlyTick` en dos pagos.
- **Tareas:** las 3 tareas del mes pasan a ser eventos repartidos en los 4 días: como máximo uno por día, y el día de
  pago no hay evento.
- **Cárcel:** la condena de 5 a 15 minutos reales (`JailSentence`) choca con el reloj. Se pasa a días de juego:
  1 a 3 días sin trabajar, con fianza igual.
- **Duración:** un año completo = 12 meses = 48 días. El final del año es el resumen.

## Inicio y partidas guardadas

1. **Nueva partida:**
   - Al tocar "Modo historia" se abre la lista de partidas (idea 3).
   - "Nueva partida" pide el nombre del personaje, el género (idea 2) y el nombre de la partida (por defecto, el del personaje).
   - El nombre se usa en los diálogos y el game master lo recibe en el estado.
2. **Mujer u hombre:**
   - Cambia la ficha del jugador en `Editor/MapBuilder.Cast.cs`: dos fichas, `PlayerLook` y `PlayerLookF`, con pelo y ropa distintos. Como el jugador se construye en el editor, en tiempo de juego se elige cuál de los dos objetos activar (o se arman los dos y se oculta uno).
   - Los textos con género ("un joven" o "una joven", "contratado" o "contratada") se escriben en las dos formas en español y en inglés (`Translations.cs`).
3. **3 espacios de guardado:**
   - Cada uno muestra nombre, mes y semana, efectivo, ahorros, ánimo y la fecha real del último juego; botones Continuar, Nueva y Borrar (con confirmación).
   - Se guarda en JSON con `PlayerPrefs`, que en WebGL queda en IndexedDB.
   - Se guarda solo al dormir y al salir al menú. Incluye `PlayerState`, `Holdings`, el día, la hora, las metas, el trabajo, el puesto y el `sessionId` del servidor (si el servidor ya no tiene esa sesión, se crea otra sin romper la partida).
   - Pruebas de EditMode para guardar y leer.
4. **Escena de apertura (30 a 45 s, se salta con Espacio):**
   1. El personaje cumple 18 en el cuarto de su mamá.
   2. Le suena el celular: "¡Te contrataron! Empezás el lunes a las 7:00 en [lugar]".
   3. Se despide en la puerta y sale al pueblo con una flecha hacia el trabajo.
   - Sin texto largo: subtítulos cortos.

## El primer trabajo

5. **Salario mínimo:** el salario base es el mínimo del decreto vigente del Ministerio de Trabajo (MTSS) para el puesto
   (trabajador no calificado). **No inventar la cifra: copiarla del decreto oficial** y ponerla en `GameRules` con un
   comentario de la fuente. El motor ya rebaja la CCSS y la renta.
6. **Lugar de trabajo en el mapa:**
   - Un local nuevo o uno existente (soda El Muelle o pulpería) con un interior.
   - El jugador va en persona, marca entrada en un reloj de tarjetas (escena: mete la tarjeta, suena el timbre) y trabaja con una escena animada de unos 10 s (barrer, atender, lavar platos) que cubre la mañana; luego queda libre.
7. **Llegar tarde:**
   - Hora de entrada 07:00 y 15 minutos de gracia.
   - Tarde una vez: llamada de atención (diálogo con el jefe).
   - Dos veces: rebajo del día en la quincena.
   - Tres veces en el mes: advertencia de despido.
   - Faltar sin aviso: rebajo y baja el ánimo. Todo lo mueve el motor en la quincena.

## Crecer

8. **Ascensos:** puestos ayudante, cajero y encargado (y si da, administrador), con salarios crecientes en `GameRules`.
   Para subir: N días trabajados sin llegar tarde más el curso de la idea 9. El jefe lo anuncia con una escena (apretón de manos, sonido).
9. **Cursos:**
   - En el INA (barato, de tarde, varios días) o en la UNA (más caro, abre puestos mejores).
   - Cuestan plata o tiempo de la tarde.
   - Enseñan "invertir en uno mismo". Concepto nuevo de enseñanza: hay que agregarlo al enum `teachingConcept` en el servidor (pedírselo a Keylor) o manejarlo solo en Unity.
10. **Reloj:** ver la sección "Tiempo".
11. **Dormir pasa el día:** ver la sección "Tiempo".

## Vivienda y estilo de vida

12. **Empezar en un cuarto feo y alquilado:**
    - Nuevo interior "Cuarto": paredes manchadas, colchón en el piso, un bombillo sin lámpara, una silla plástica, sin tele ni refri.
    - Alquiler barato, que sale del salario en el cierre del mes.
    - La casa actual (`Casa`) pasa a ser la segunda vivienda.
13. **Mejorar por etapas:**
    - Primero los muebles del cuarto: cama, refri, tele, mesa. Cada compra aparece en el interior: hay que armar los muebles como objetos que se activan según `Holdings`.
    - Después, mudarse a una casa mejor desde la inmobiliaria (ya existe `RentHome` y `BuyHome`).
    - Cada vivienda tiene su interior.
14. **Muebles a cuotas o de contado:** la tienda vende los muebles. A cuotas, entra la negociación con el vendedor y el
    seguro escondido (ya existe). De contado, la escena de la caja (pitido, bolsa).
15. **Recibos reales:**
    - Luz, agua, internet y teléfono llegan al celular la semana 4, con montos que dependen de la vivienda y de los muebles.
    - Si no se pagan: recargo, y al segundo mes, corte (la luz se apaga en el interior y la tele no funciona).
    - El motor los cobra.

## Metas y final

16. **Metas del año:**
    - Al empezar, el celular propone 3 metas según la situación: juntar el fondo de emergencia, terminar el año sin deudas, comprar la moto.
    - Se ven en una app "Metas" con barra de progreso y se celebran al cumplirlas (sonido y confeti).
17. **Final:**
    - Al cerrar el mes 12, la postal del año (ya existe) con lo logrado.
    - Una nota de A a D por concepto financiero: presupuesto, fondo de emergencia, deudas, cuotas, inversiones.
    - El resumen con Muse Wonder (ya existe).
    - La partida guardada queda como "terminada" y se puede ver.
18. **Año completo:**
    - `MonthsToPlay` pasa a 12.
    - Balancear la economía (salario mínimo contra alquiler, recibos y comida) para que ahorrar sea posible pero cueste.
    - La demo de 3 minutos muestra solo el inicio y el primer día.

## Extras

19. **Eventos de la vida:**
    - Cumpleaños de un amigo, un familiar que pide plata prestada, el marchamo en diciembre, el aguinaldo en diciembre (el motor ya lo calcula), una emergencia médica.
    - El game master los cuenta; el motor aplica el efecto.
20. **Dinámicas ticas:**
    - Black Friday con el celular a cuotas.
    - La tanda entre compañeros del trabajo (cada uno pone cada quincena y uno se lleva todo por turno: enseñar el riesgo de que alguien no pague).
    - Rifas del barrio.
    - El prestamista "gota a gota" (ya existe).
21. **Mascota:**
    - Un perro o un gato que se adopta en la feria o lo regala un vecino.
    - Gastos de comida cada semana y veterinario como evento.
    - Sube el ánimo.
    - Se ve en la vivienda, caricatura estilo Among Us como el resto, nunca pixel art.

## Qué hay que cambiar en el código (resumen técnico)

- **`Scripts/Game/GameRules.cs`:** constantes nuevas.
  - `DaysPerMonth = 4`, `MonthsToPlay = 12`, `DayStartHour = 6`, `DayEndHour = 22`, `SecondsPerGameHour`.
  - Salario mínimo con su fuente; salarios por puesto; alquiler del cuarto.
- **`Scripts/Game/GameClock.cs` (nuevo, puro y con pruebas):** mes, día, hora y minuto. Avanza con el tiempo real, la cama lo salta a la mañana, emite `DayStarted`, `PayDay` y `MonthEnded`.
- **`Scripts/World/DayCycle.cs`:** el tinte sigue a la hora del reloj, no a las tareas.
- **`Scripts/Game/GameLoop.cs`:**
  - pagos quincenales;
  - días trabajados, tardanzas y puesto;
  - recibos;
  - metas;
  - el descanso pasa a ser diario.
- **`Scripts/Game/SaveSlots.cs` (nuevo, con pruebas):** 3 espacios de guardado, JSON en `PlayerPrefs`.
- **`Scripts/UI/MainMenu.cs`:** la lista de partidas y la pantalla de nueva partida (nombre, género, nombre de la partida).
- **`Editor/MapBuilder.*`:**
  - interior del cuarto y del trabajo;
  - muebles que se activan según `Holdings`;
  - letreros CERRADO de noche;
  - segunda ficha del jugador.
- **HUD:** reloj y semana; app de metas en el celular.
- **Servidor (pedírselo a Keylor):**
  - que `PlayerState` acepte el nombre, la semana y el puesto si el game master los va a usar;
  - el concepto de enseñanza nuevo ("invertir en uno mismo").

## Orden sugerido

1. Causas A, B y C, y los puntos 5, 6, 12 y 13. Son los que más se notan en la demo y comparten el arreglo del paso `SitOn` y del punto de frente.
2. Puntos 9, 10, 11, 14 (semáforos) y 15 (ventana de inicio). Cambios acotados, con mucho efecto.
3. Puntos 4 y 1.
4. Puntos 2, 3 y 8 (cárcel y arresto).
5. Punto 7 (ascensor).
6. Modo historia, en este orden:
   1. reloj y calendario de 4 días;
   2. guardado de 3 partidas;
   3. nueva partida (nombre, género) y escena de apertura;
   4. trabajo con salario mínimo y llegar tarde;
   5. cuarto feo y mejoras;
   6. recibos;
   7. metas y final;
   8. ascensos y cursos;
   9. extras.

   Si el tiempo no alcanza para la entrega del lunes 2 de noviembre, se recorta desde el final de esta lista.

Después de cada bloque: regenerar escenas si se tocó `Editor/MapBuilder*`, correr EditMode y PlayMode, y probar en
pantalla lo de la sección "Cómo verificar". Ninguna prueba automática cubre lo visual.

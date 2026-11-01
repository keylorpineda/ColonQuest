# Plan de implementación visual: cambio drástico

Instrucciones paso a paso para implementar, en la rama `feat/SCRUM-94-lighting-water-characters`, el salto
visual del juego: árboles que reaccionan, agua viva, personajes y animaciones que se notan, y edificios con
texturas e interiores distintos. Está escrito para un agente de código (Gemini) que **no** vio la
conversación donde se diseñó. Seguí las fases en orden. Cada fase termina con un commit.

> Lee primero `CLAUDE.md` y `docs/graficos-luces-animacion.md` (lo que ya está hecho en esta rama).

---

## 0. Por qué todavía no se ve casi nada (leer antes de tocar código)

Las escenas de Unity (`unity/Assets/Scenes/*.unity`) **no se escriben a mano**: las genera el código de
`unity/Assets/Editor/MapBuilder*.cs` con el menú **ColonQuest > Build All Scenes**. Los 6 commits que ya
tiene esta rama cambiaron ese código, pero **las escenas nunca se regeneraron**. Por eso:

- El agua nueva (espuma, oleaje, corriente, anillos), el perro animado, las luces 2D y el polvo **no existen**
  en las escenas guardadas.
- Los árboles usan el script nuevo, pero con los valores viejos guardados en la escena (`useGlobalWind = 0`,
  `pivotDrop = 0`). Encima, el rebote al pasar quedó más amortiguado (de 5,5 a 9), así que se mueven **menos**
  que antes.

**Regla de oro:** después de cada fase hay que correr **Build All Scenes** en Unity y hacer commit de las
escenas regeneradas. Sin ese paso ningún cambio de `MapBuilder` se ve en el juego.

Además, varios valores de la primera pasada quedaron demasiado sutiles. La fase 1 y la fase 2 los suben.

---

## 1. Reglas que no se rompen (todas las fases)

Estas reglas existen porque en este proyecto ya se arreglaron muchos bugs causados por romperlas.

### Código

1. **Código en inglés** (nombres, comentarios). Textos visibles del juego en español de Costa Rica.
2. **Un `MonoBehaviour` por archivo**, con el archivo llamado **exactamente** como la clase
   (`ShoreLap` va en `ShoreLap.cs`). Si no, Unity no puede guardar el componente en la escena. Las clases
   `static` de lógica pura pueden ir en cualquier archivo.
3. **Cada archivo nuevo dentro de `unity/Assets` lleva su `.meta`** con un GUID nuevo (32 caracteres hex en
   minúscula, por ejemplo con `python3 -c "import uuid;print(uuid.uuid4().hex)"`). Formato usado en el repo
   para scripts (sin salto de línea final):
   ```
   fileFormatVersion: 2
   guid: <32 hex>
   ```
   Para carpetas nuevas usar el formato de `unity/Assets/Scripts/Lighting.meta` (con `folderAsset: yes`).
4. **La lógica va en funciones `static` puras** (clases tipo `WaterMath`, `PersonMotion`, `DogMotion`) y se
   prueba con tests de EditMode en `unity/Assets/Tests/EditMode/World/`. El `MonoBehaviour` solo lee el tiempo
   y aplica el resultado. Ver como ejemplo `WaterMath.cs` + `WaterMathTests.cs`.
5. **No usar `Mathf.PerlinNoise` dentro de las funciones puras**: pasar el ruido como parámetro (ver
   `FoliagePhysics.GlobalWindStrength`), así los tests no dependen del motor.
6. **Nada de `float` para dinero.** Estas fases no deberían tocar dinero. Si un cambio visual necesita un
   monto, se lee del estado, nunca se calcula.
7. **No tocar** `unity/Assets/Scripts/Finance/`, `server/`, `shared/`, ni el contrato con el backend.

### Construcción de escenas (`MapBuilder`)

8. **Todo dibujo se crea con los helpers** de `MapBuilder.cs`: `Shape(...)`, `TiledShape(...)`, `Glow(...)`,
   `Flickering(...)`, `Part2(...)` (con contorno) o `Deco(...)`/`Decal(...)` en interiores. No crear
   `SpriteRenderer` a mano en el builder: los helpers ponen la capa, el orden y el material de luz
   (`ApplyLighting`).
9. **Semilla propia para cada cosa nueva aleatoria.** `BuildTown` comparte `new System.Random(11)` entre todas
   las funciones. Si una función nueva saca números de ese `random`, **se mueven de lugar todos los objetos
   colocados después** (árboles, gente, props), y pueden terminar tapando puertas o caminos. Toda función
   nueva crea su propio generador: `var random = new System.Random(<número fijo distinto>);`. Ver
   `BuildSeaSwells` en `MapBuilder.World.cs`.
10. **Lo decorativo no lleva colisión.** No agregar `BoxCollider2D`/`CircleCollider2D` a ningún adorno nuevo.
    `Prop(...)` con orden `<= OrderProp` **sí** agrega colisión: para adornos usar `Shape`, o `Prop` con un
    orden mayor que `OrderProp` (5), o `Deco`.
11. **Nunca dibujar delante de puertas, estaciones ni zonas de interacción.** En el pueblo, antes de colocar un
    adorno en el suelo, comprobar `!OnPavement(x, y, margen)` cuando corresponda y que no caiga a menos de
    **1,5 unidades** de `b.DoorX` en la acera de un edificio. En interiores, mantener libre un rectángulo de
    **1,6 × 1,6** alrededor de cada `BuildThing`, `BuildStation` y del felpudo (`MapLayout.*Mat`).
12. **No renombrar objetos que el código busca por nombre**: `"Bob"`, `"Visual"`, `"Head"`, `"Torso"`,
    `"Hand"`, `"Pet"`, `"Dog"`, `"Thing_*"`, `"Station_*"`, `"Spawn_*"`, `"Talk"`, `"Wing"`, `"DayTint"`.
13. **Órdenes de dibujo:** en el pueblo los edificios usan órdenes 0–12 dentro de su `SortingGroup`; las
    personas, `OrderPlayer = 20` y hasta +27. Un adorno de fachada va entre 2 y 11. Un adorno en el suelo del
    pueblo va en la capa `Floor` (`currentLayer = LayerFloor`) con orden menor que `TownShadow` (50).
14. **Rendimiento WebGL:** nada de crear y destruir objetos cada frame. Usar *pools* fijos (ver
    `RippleField` y `FootDust`, de 8 a 12 sprites reutilizados). Máximo **12 luces 2D nuevas** en total por
    fase.

### Verificación y commits

15. Antes de cada commit, en Unity:
    1. **ColonQuest > Build All Scenes**, sin errores en la consola.
    2. **Window > General > Test Runner**: correr EditMode y PlayMode. Todo en verde.
    3. Jugar el pueblo: caminar entre árboles, ir a la playa y al río, entrar a la casa y a 3 locales.
16. **Commits a nombre de Keylor**, sin líneas `Co-Authored-By` ni menciones a una IA:
    ```bash
    git config user.name "keylorpineda"
    ```
    ```bash
    git config user.email "keylorpineda729@gmail.com"
    ```
    Formato del mensaje (igual que el historial): `feat(unity): <qué cambia, en inglés> [SCRUM-94]`.
17. Incluir en el commit las escenas regeneradas (`unity/Assets/Scenes/*.unity`), las texturas nuevas
    (`unity/Assets/Art/Sprites/*.png` y sus `.meta`) y, la primera vez, `unity/Assets/Settings/` y
    `unity/ProjectSettings/`.

---

## Fase 1. Árboles, arbustos y pasto que reaccionan al jugador

**Objetivo visible:** al pasar junto a un árbol, la copa se sacude claramente y se recupera; caen 2 o 3 hojas.
Los arbustos y el pasto alto se apartan al paso del jugador. Con viento, se ve la ráfaga cruzar el pueblo.

### 1.1 Rebote más fuerte y que se dispare siempre (`unity/Assets/Scripts/World/FoliagePhysics.cs`)

Hoy el rebote solo se dispara si el jugador **entra** al radio viniendo de lejos
(`lastRustleDistance >= reactRadius * 0.85f`). Si camina pegado a una fila de árboles, casi nunca se dispara.

1. Cambiar las constantes:
   - `RustleStiffness = 30f`
   - `RustleDamping = 4.2f` (rebota 2 o 3 veces, más visible).
2. Agregar campos privados: `private float nextRustleTime;` y `private Vector2 lastPlayerPos;`.
3. Reescribir `UpdatePlayerRustle()` así:
   - Calcular `dist` como hoy.
   - Calcular cuánto se movió el jugador desde el frame anterior: `float moved = Vector2.Distance(playerPos, lastPlayerPos); lastPlayerPos = playerPos;`.
   - Disparar si `dist < reactRadius && moved > 0.01f && Time.time >= nextRustleTime`.
   - Al disparar: `springVelocity += dir * reactImpulseDegrees * (3.5f + strength * 3.5f);`, poner
     `nextRustleTime = Time.time + 0.45f`, y avisar a `LeafBurst` si existe (ver 1.3).
   - Borrar el campo `lastRustleDistance` si queda sin uso.
4. Agregar el método público `public void Rustle(float direction, float strength01)` que hace el mismo
   impulso. Se usa en 1.4.
5. Actualizar `FoliagePhysicsTests.StepSpring_DampsImpulseBackToRest`: llama a `StepSpring` con 42 y 5,5
   explícitos, así que **no cambia**. Agregar un test nuevo que llame a `StepSpring` con `RustleStiffness` y
   `RustleDamping`, con velocidad inicial 40, y compruebe un pico mayor a 4° y un regreso a menos de 0,1° en
   240 pasos de 1/60 s.

### 1.2 Valores de los árboles (`unity/Assets/Editor/MapBuilder.Town.Props.cs`, función `Tree`)

Reemplazar la llamada a `Configure` del `CanopyRig` por:
```csharp
canopyRig.gameObject.AddComponent<FoliagePhysics>().Configure(4.5f, 1.45f, x * 0.53f + y * 0.71f, 0.008f, true, 2.4f, 16f, true, 1.58f * scale);
```
- En `Palm` (`MapBuilder.World.cs`): `Configure(4.8f, 1.55f, x * 0.6f + y * 0.9f, 0.008f, true, 2.3f, 14f, true, 1.0f * scale)`.
- En `Bush` (`MapBuilder.Town.Props.cs`, alrededor de la línea 1258): `Configure(5.5f, 1.75f, x * 0.83f + y * 0.47f, 0.02f, true, 1.7f, 22f, true, 0.15f)`.
- En `CoffeeBush` (`MapBuilder.World.cs`): `Configure(4.5f, 2.0f, x * 1.1f + y * 0.7f, 0.012f, true, 1.6f, 18f, true, 0.1f)`.

### 1.3 Hojas que caen al sacudir (`LeafBurst.cs`, nuevo)

Crear `unity/Assets/Scripts/World/LeafBurst.cs` (+ `.meta`):
- `public sealed class LeafBurst : MonoBehaviour`, con un *pool* de **6** `SpriteRenderer` creados en
  `Awake` como hijos de un `GameObject` aparte en la raíz de la escena (igual que `FootDust`), para que las
  hojas no se muevan con el árbol.
- Campos serializados: `Sprite leafSprite`, `Color leafColor`, `string sortingLayer`, `int sortingOrder`,
  y `Configure(Sprite, Color, string, int)`.
- Método público `Burst(int count)`: activa `count` hojas (de 2 a 3) en posiciones aleatorias dentro de un
  radio de 0,6 alrededor de `transform.position`.
- Cada hoja vive **1,6 s**: cae 1,2 unidades, se balancea de lado con `sin(t * 6) * 0.25`, gira y se
  desvanece en el último 30 % de su vida.
- Lógica pura en `public static class LeafMath` (en el mismo archivo):
  `public static Vector2 Offset(float t01, float side)` y `public static float Alpha(float t01)`, con tests
  en `LeafMathTests.cs`.
- En `FoliagePhysics`, al disparar el rebote: `GetComponent<LeafBurst>()?.Burst(Random.Range(2, 4));`.
  Guardar la referencia en `Awake` para no buscarla cada vez.
- En el builder (`Tree`, `Palm`, `Bush`), sobre el mismo objeto que tiene `FoliagePhysics`:
  `rig.gameObject.AddComponent<LeafBurst>().Configure(palmFrondSprite != null ? palmFrondSprite : circle, <color de la hoja de esa planta>, LayerWorld, 900);`.
  **No usar `??` con objetos de Unity** (un `Sprite` destruido no es `null` para C#). En `Tree` el color es
  `GamePalette.Hex(leaf)`; en `Palm` y `Bush`, el color verde principal que ya usa cada función. El orden 900
  deja las hojas delante de todo en el pueblo.

### 1.4 Pasto alto y flores que se apartan (`GrassBend.cs`, nuevo)

`GrassClump` y `FlowerClump` (en `MapBuilder.Depth.cs`) hoy son estáticos.
- Crear `unity/Assets/Scripts/World/GrassBend.cs`: un `MonoBehaviour` que, si el jugador está a menos de
  **0,9** unidades, inclina su `transform` hasta **±18°** alejándose del jugador (signo según
  `player.x > x`), con un resorte (`FoliagePhysics.StepSpringToward`, rigidez 60, amortiguación 8).
  Lejos del jugador, se mece con la brisa global con amplitud 3°
  (`FoliagePhysics.WindLean(FoliagePhysics.GlobalWindStrength(...), 3f)`).
- Buscar al jugador con `GameObject.FindWithTag("Player")` como máximo cada 0,5 s, y guardarlo.
- **Rendimiento:** hay unos 330 objetos de cobertura. Si el jugador está a más de 8 unidades, no hacer nada
  (`return` antes de cualquier cálculo).
- En `GrassClump` y `FlowerClump`: crear un hijo `"BendRig"` en `(0, 0)`, dibujar las hojas y flores dentro de
  él y agregarle `GrassBend`. La raíz con `SortingGroup` no se toca.

### 1.5 Criterios de aceptación

- [ ] Caminar a lo largo de la fila de árboles de la calle principal: cada árbol se sacude al pasar y suelta
      hojas.
- [ ] El pasto alto se aparta al pisarlo y vuelve.
- [ ] Ningún objeto cambió de lugar respecto de `develop` (comparar una captura antes y después).
- [ ] Tests en verde. Commit: `feat(unity): make trees, bushes and grass react to the player [SCRUM-94]`.

---

## Fase 2. Agua que se nota

**Objetivo visible:** el mar se mueve todo el tiempo, la orilla tiene olas claras, el río corre, el jugador
salpica y deja huellas en la arena mojada, y la cascada echa espuma.

### 2.1 Subir los valores (`unity/Assets/Editor/MapBuilder.World.cs`)

En `BuildSouthGround`:
- `Shore_Foam`: tamaño `(118f, 0.55f)`, alfa 0,85, `ShoreLap.Configure(0.38f, 5.5f, 0f, 0.35f)`.
- `Shore_Foam_Back`: tamaño `(116f, 0.32f)`, alfa 0,55, `ShoreLap.Configure(0.3f, 5.5f, 0.3f, 0.6f)`.
- Agregar una tercera línea `"WetSand_Lap"` color `GamePalette.Shade(SandColor, 0.82f)` alfa 0,5, tamaño
  `(116f, 0.6f)`, en `y = -27.0f`, orden `WorldSand + 1`, con
  `ShoreLap.Configure(0.3f, 5.5f, 0.1f, 0.8f)`: es la arena mojada que deja la ola.

En `BuildSeaSwells`: 40 líneas, alto 0,16, alfa 0,5, ancho de 5 a 10, `travel` de 5,6 y período de 6 a 9 s.

En `RippleField` (`unity/Assets/Scripts/World/RippleField.cs`): `every = 0.7f`, `PoolSize = 16`, alfa máximo
0,8 (hoy 0,55), `PlayerRippleEvery = 0.3f`.

En `BuildRiver`: 36 estelas, alfa 0,42, ancho 0,09, velocidad de 0,9 a 1,4.

### 2.2 Brillo que se desplaza sobre el agua (`WaterShimmer.cs`, nuevo)

- Textura nueva `WaterCausticTexture.png` en `SpriteFactory` (ver la sección 4.1 para cómo se agrega una
  textura): 128×128, `pixelsPerUnit` 64, `repeat` verdadero. Blanco con alfa: líneas onduladas tipo cáustica,
  alfa entre 0 y 0,35, transparente en el resto.
- `WaterShimmer : MonoBehaviour` sobre un `TiledShape` del tamaño del mar: mueve `transform.localPosition` en
  `x` a 0,25 u/s y en `y` con `sin(t * 0.4) * 0.15`. Envuelve cada 2 unidades (el tamaño del tile), para que
  nunca se vea el borde.
- Dos capas sobre el mar, en direcciones opuestas (x a +0,25 y a −0,18), con orden `WorldFoam` y alfa 0,6
  y 0,4. Una capa igual sobre el río, moviéndose en `y` a −0,8 u/s.

### 2.3 Salpicaduras y huellas

- `RippleField.FollowPlayer`: además del anillo, generar 3 gotas (círculos de 0,08, blanco alfa 0,8) que
  saltan en arco 0,3 hacia arriba y caen en 0,4 s. Usar un segundo *pool* de 12.
- **Huellas en la arena** (`FootDust.cs`): cuando `Surfaces.At(...) == SurfaceKind.Sand`, además del polvo,
  dejar una huella (elipse de 0,12 × 0,07, color `#C9B37E`, alfa 0,5) alternando el pie (±0,08 en x). Dura
  6 s y se desvanece en los últimos 2 s. *Pool* de 20, capa `Floor`, orden 199.
- **Cascada** (`MapBuilder.World.cs`, cerca de la línea 303, `"Waterfall"`): agregar un `WaterFlow` vertical
  a 3 franjas claras dentro de la cascada (velocidad 2,5) y un `RippleField` pequeño centrado en la poza
  (`PoolY`), con `area` del tamaño de la poza, `every = 0.35f` y `followPlayer = false`.

### 2.4 Criterios de aceptación

- [ ] Desde la playa se ve el mar moverse en todo momento sin acercarse a la orilla.
- [ ] Las olas suben y bajan claramente y dejan la arena oscura.
- [ ] Al caminar por la orilla salen anillos y gotas, y quedan huellas en la arena.
- [ ] El río corre visiblemente bajo los puentes.
- [ ] Commit: `feat(unity): make the sea, river and waterfall feel alive [SCRUM-94]`.

---

## Fase 3. Personajes y animaciones generales

**Objetivo visible:** cada personaje se mueve con peso y personalidad. Todo movimiento tiene anticipación,
acción y asentamiento, y se nota a la distancia de la cámara (tamaño ortográfico 4,6).

Archivos: `unity/Assets/Scripts/World/PlayerAnimator.cs`, `PersonMotion.cs`, `PersonPose.cs` y
`unity/Assets/Editor/MapBuilder.People.cs`. Tests: `PersonMotionTests.cs`.

### 3.1 Subir lo que ya existe

En `PlayerAnimator` y `PersonMotion`:
- `bobHeight = 0.12f` (hoy 0,09).
- `PersonMotion.StepSquash`: factor 0,09 en vez de 0,05.
- `PersonMotion.WalkLean`: −5° en vez de −3°.
- Rebote al frenar: `settleVelocity -= 1.6f` (hoy 0,9).
- `PersonMotion.LegAngle`: 30° (hoy 26°).
- Actualizar los rangos en los tests existentes que dependan de esos números.

### 3.2 Codos al caminar

Hoy `elbowLeft` y `elbowRight` valen 0 al caminar. Agregar
`PersonMotion.ElbowSwing(float phase, float moving, bool left)`: el codo se dobla de 10° a 35° y llega 0,6
radianes después del hombro: `-(left ? -1 : 1) * (10f + 25f * Mathf.Max(0f, Mathf.Sin(phase - 0.6f * (left ? 1 : -1)))) * moving`.
Usarlo en `AnimateRig` cuando no hay saludo, ni habla, ni pose. Agregar un test: 0 con `moving = 0` y un
valor absoluto de entre 10 y 35 al caminar.

### 3.3 Movimiento secundario (pelo, corbata, falda)

1. En `MapBuilder.People.cs`, `DrawHairBehind`: para `Long`, `Ponytail`, `Bun` y `Bob`, crear un pivote
   `"HairSway"` en la parte alta de la cabeza (`(0, 0.38)`) y dibujar ahí el pelo de atrás.
   Para `Ponytail`, el pivote va en la raíz de la cola.
2. En `DrawExtras`: la corbata (`Tie`, `Knot`) y el `Scarf` cuelgan de un pivote `"TieSway"` en el cuello.
3. Agregar a `PersonRig`: `public Transform HairSway; public Transform TieSway;`, asignados en el builder.
4. En `PlayerAnimator`, un resorte por pivote
   (`FoliagePhysics.StepSpringToward(ref angle, ref vel, target, 90f, 7f, dt)`) con
   `target = -walkVelocity.x * 2.2f` (en grados, limitado a ±14°) más `torsoLean * 0.5f`. Al frenar, el
   pelo sigue un instante y vuelve.

### 3.4 Giro con transición

En `PlayerController.Update` (jugador) y en `Patrol`/`BankCustomer` (NPC), hoy
`visual.localScale = new Vector3(facing, 1f, 1f)` cambia de golpe. Crear `FacingTurn.cs` (`MonoBehaviour`
en `Visual`) con `public void TurnTo(int sign)`: durante 0,12 s la escala x pasa de `±1` a `0.15` y luego
a `∓1`, con la forma `1 - |cos(t·π)|`, conservando el signo final. El jugador y los NPC llaman `TurnTo` en
lugar de escribir la escala. **Cuidado:** `PlayerController.Face(int)` y `Interaction` también voltean;
deben seguir siendo instantáneos (las escenas animadas cuentan con eso). Usar el giro animado **solo** en el
caminar normal.

### 3.5 Anticipación al arrancar

En `PlayerAnimator.Update`: cuando `target > 0.2f` y `moving < 0.05f` (arranque), aplicar un impulso de
`settleVelocity += 0.8f` (se estira) y una inclinación hacia atrás de +4° durante 0,1 s sobre `torsoLean`.

### 3.6 Esperas variadas (solo el jugador)

1. Agregar a `PersonPose` las poses `Stretch`, `ScratchHead`, `LookAround`, `CheckPhone` y `ShiftWeight`,
   con sus ángulos en `PersonPoses.Evaluate` (usar como referencia las poses existentes `Wave`, `Read` y
   `Pocket`). Por ejemplo, `ScratchHead`: `ArmR = 150f + sin(t*14)*8f`, `ElbowR = 70f`, `Lean = 3f`.
2. Crear `IdleActions.cs` (`MonoBehaviour` en el `Bob` del jugador, agregado por `BuildPlayer` solo en el
   pueblo): si el jugador está quieto, sin `PlayerController.Blocked`/`Frozen` y sin pose, después de
   **6 a 9 s** elige una pose al azar (sin repetir la anterior) y la mantiene de 1,5 a 2,5 s con
   `PlayerAnimator.SetPose(pose, seconds)`. Cualquier movimiento la cancela con `SetPose(PersonPose.None)`.
3. Lógica pura: `public static PersonPose IdleActions.Pick(float random01, PersonPose last)`, con test que
   asegure que nunca repite `last`.

### 3.7 Ojos y cara vivos

- **Mirada que se mueve sola:** en `AnimateFace`, si no hay jugador cerca, cada 1,5 a 3,5 s mover
  `pupilGoal` a un punto al azar de ±0,012 en x y ±0,006 en y.
- **Doble parpadeo:** con probabilidad 0,2, programar un segundo parpadeo 0,18 s después del primero.
- **Cara según el ánimo** (solo el jugador): agregar `public void SetMood(float wellbeing01)` a
  `PlayerAnimator`. Con menos de 0,35, cejas 10° hacia adentro y boca recta (`MouthRest` 0,25); con más de
  0,75, sonrisa. `GameUi` (o `GameSession.Update`) llama `SetMood(state.Wellbeing / 100f)` como máximo una
  vez por segundo. `Wellbeing` es un entero de 0 a 100 en `PlayerState`.

### 3.8 Sombra que respira con el paso

El `"Shadow"` (hermano del `"Bob"`, creado en `BuildPersonVisual`) escala en x de 1 a 0,85 cuando el cuerpo
sube en el paso. Pasarle la referencia al animador (campo nuevo en `PersonRig`: `public Transform Shadow;`)
y escalarla en `Update` con `1f - step * 0.15f * moving`.

### 3.9 Globos de reacción

Crear `ReactionBubble.cs` en World: muestra sobre la cabeza del jugador un círculo blanco con contorno y un
símbolo dibujado con formas (no texto): `!` (dos rectángulos), `$` (círculo amarillo con barra), corazón
(dos círculos y un rombo), enojo (dos rayas rojas cruzadas). Aparece con un rebote de escala 0 → 1,15 → 1
en 0,25 s, dura 1,6 s y se desvanece.
- API: `public static void Show(ReactionKind kind)`, con
  `enum ReactionKind { Alert, Money, Love, Angry }`. Busca al jugador por la etiqueta `Player`.
- Engancharlo **sin tocar la lógica de dinero**, solo suscribiéndose a eventos existentes:
  - `TrafficCar.PlayerHit` → `Angry`.
  - `PoliceOfficer.StoppedPlayer` → `Alert`.
  - Al acariciar al perro (en `DogAnimator.Cheer`) → `Love`.
  - En la negociación, cuando `revealedFacts` trae un cargo nuevo (`insurance_fee` o `hidden_fees`) →
    `Alert`. Hacerlo en `NegotiationPanel`, solo visual.
- Suscribirse en `OnEnable` y desuscribirse en `OnDisable`. Si no, se duplica al cambiar de escena (ya pasó
  con otros eventos del juego).

### 3.10 NPC con oficio

En `MapBuilder.Characters.cs`/`MapBuilder.Cast.cs`, para los NPC fijos agregar una pose en bucle según su
rol, con un componente `WorkLoop` que llama `SetPose(pose, 0)` y la cambia cada 4 a 7 s entre la pose de
trabajo y `None`:
- Cajero, asesora y financiera: `Type`.
- Doña de la soda: `Wash` o `Cook`.
- Mecánico (Don Rafa): `Reach`.
- Vendedor: `Wave` ocasional.

**No** aplicar a NPC que tienen escena de diálogo activa: `WorkLoop` se pausa si
`PlayerAnimator.Talking` es verdadero.

### 3.11 Criterios de aceptación

- [ ] Caminar de lado: el cuerpo se inclina, los codos se doblan, el pelo largo y la corbata se quedan atrás.
- [ ] Cambiar de dirección: el giro se ve (no salta).
- [ ] Quedarse quieto 10 s: el jugador hace una acción de espera.
- [ ] Con bienestar bajo, la cara se ve triste.
- [ ] Atropello, multa, perro y cargo escondido muestran su globo.
- [ ] Los tests de PlayMode de interacción y de escenas (`InteractionTests`, `PlacesPlayTests`) siguen en
      verde.
- [ ] Commit: `feat(unity): give characters secondary motion, idles, turns and reactions [SCRUM-94]`.

---

## Fase 4. Exteriores: cada edificio con su material

**Objetivo visible:** que ningún edificio se parezca a otro. Hoy todos usan la misma textura de ladrillo
`FacadeBrickTexture` teñida (`MapBuilder.Town.Props.cs`, función `Building`, línea ~110) y la misma teja.

### 4.1 Cómo se agrega una textura (`unity/Assets/Editor/SpriteFactory.cs`)

Todas las texturas se dibujan por código, píxel a píxel. Para cada textura nueva:
1. Constante de ruta: `public const string StuccoTexturePath = Folder + "/StuccoTexture.png";`.
2. En `Generate()`: `WriteColor(StuccoTexturePath, 128, 128, 64, 0, StuccoPixel, true);` (ancho, alto,
   píxeles por unidad, borde 0, función, `repeat = true` para que se repita sin costuras).
3. Función `private static Color StuccoPixel(float x, float y)` que devuelve el color del píxel. Para que se
   repita sin costura, usar funciones periódicas en el tamaño del tile (`Mathf.Sin(x / 128f * 2π * n)`) o
   ruido calculado con `x % ancho`. Seguir el estilo de `SidewalkSlabPixel` y `BrickWainscotPixel`.
4. Agregar `|| !File.Exists(StuccoTexturePath)` en `EnsureGenerated()`, para que se generen si faltan.
5. En `MapBuilder.cs`: campo `private static Sprite stuccoTex;` y su carga en `LoadSprites()` con
   `AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.StuccoTexturePath)`.
6. **Las texturas son en escala de grises claros** (o blanco con detalle), así el color de cada edificio
   se aplica como tinte (`TiledShape(..., color, order)`), igual que hoy con el ladrillo.

### 4.2 Texturas nuevas de exterior

| Archivo | Tamaño / ppu | Qué dibuja |
| --- | --- | --- |
| `StuccoTexture.png` | 128 / 64 | Repello: base blanca con grano fino irregular (ruido de 2 escalas, ±6 % de brillo) y 2 o 3 manchas suaves más oscuras |
| `ConcreteBlockTexture.png` | 128 / 64 | Bloques de 40×20 px en hilada trabada, junta de 2 px un 25 % más oscura, leve variación por bloque |
| `WoodSidingTexture.png` | 128 / 64 | Tablilla horizontal de 16 px de alto, sombra de 3 px bajo cada tabla, veta suave |
| `ZincWallTexture.png` | 128 / 64 | Lámina corrugada vertical: bandas de brillo `sin(x · 2π / 16)`, un par de remaches |
| `TileWhiteTexture.png` | 128 / 64 | Azulejo de 32 px con junta gris clara (hospital) |
| `ZincRoofTexture.png` | 256 / 64 | Techo de zinc ondulado en diagonal leve, brillo metálico y 1 o 2 manchas de óxido naranja (alfa bajo) |
| `ClayRoofTexture.png` | 256 / 64 | Teja de barro distinta de la actual: filas más anchas y bordes irregulares |
| `ConcreteRoofTexture.png` | 128 / 64 | Losa plana gris con manchas de humedad y una junta |
| `GrimeTexture.png` | 128 / 64 | Solo alfa: chorreaduras verticales oscuras que se desvanecen hacia abajo (se superpone a cualquier pared) |

### 4.3 Material por edificio (`Building` en `MapBuilder.Town.Props.cs`)

Crear la función `private static (Sprite wall, Sprite roof) MaterialsFor(BuildingSpec b)` y usarla en lugar de
`facadeBrickTex` y `roofTileTex`:

| Edificio (`b.Name`) | Pared | Techo |
| --- | --- | --- |
| `Casa` (la del jugador) | `stuccoTex` | `clayRoofTex` |
| `Casa_*` (vecinos) | por semilla del nombre: `stucco`, `concreteBlock`, `woodSiding` | por semilla: `zincRoof` (60 %), `clayRoof` (40 %) |
| `Banco` | sin textura de pared (mármol liso actual) + franja de `tileWhite` en el zócalo | `concreteRoofTex` |
| `Tienda`, `Pulperia`, `SodaElMuelle` | `woodSidingTex` | `zincRoofTex` |
| `Taller` | `zincWallTex` | `zincRoofTex` |
| `Hospital` | `tileWhiteTex` | `concreteRoofTex` |
| `Comisaria` | `concreteBlockTex` | `concreteRoofTex` |
| `Cooperativa`, `InaCursos`, `Inmobiliaria`, `AgenciaDeCarros` | `stuccoTex` | `concreteRoofTex` |
| `Establo`, `FincaSanIsidro` | `woodSidingTex` | `zincRoofTex` |
| cualquier otro | `facadeBrickTex` (actual) | `roofTileTex` (actual) |

**Semilla por nombre:** `int seed = b.Name.GetHashCode()` **no sirve** (cambia entre ejecuciones de .NET).
Usar un hash estable, por ejemplo
`int seed = 0; foreach (char c in b.Name) seed = seed * 31 + c; seed = Mathf.Abs(seed);`.

Los techos de zinc no llevan `RoofMossPatch` ni `Roof_EaveScallops`: en su lugar va un borde recto
(`Eave_Outline` + `Eave` del `else` actual).

### 4.4 Desgaste y elementos ticos (nueva función `DecorateFacade(Transform root, BuildingSpec b, float w, float fh, float dx)`)

Llamarla al final de `Building`, antes de `return`. Usa su propio `System.Random(seed)`. Todo con `Shape`, sin
colisiones, con orden de 2 a 11, y **nunca** a menos de 0,9 unidades de `dx` (la puerta) en x por debajo de
y = 2,2.

1. **Chorreaduras**: `TiledShape(root, "Grime", grimeTex, ...)` bajo cada ventana (ancho 0,9, alto 1,2) y en
   las esquinas, alfa 0,35, orden 2.
2. **Franja de suciedad** al pie de la pared: alto 0,25, color `#5A4A3A`, alfa 0,25, orden 2.
3. **Rejas** en las ventanas de las casas (70 % de las casas, por semilla): 5 barras verticales de 0,04 de
   ancho, color `#2A2F45`, sobre la ventana, orden 9.
4. **Medidor de luz** (caja de 0,22 × 0,3, gris, con círculo blanco) a un lado de la puerta, a 1,2 de altura.
5. **Cables**: 2 líneas finas (0,03 de alto, color `#1D2238`, alfa 0,8) desde el borde del techo hacia el lado
   más cercano, inclinadas ±8°.
6. **Canoa y bajante**: franja horizontal bajo el alero (`fh - 0.1`) y tubo vertical en una esquina, color
   `#8E93A8`.
7. **Antena** de TV en el techo de 50 % de las casas: un poste y 3 barras.
8. **Rótulo pintado a mano** en negocios sin rótulo propio: rectángulo de color con 2 franjas, sin texto.
9. **Variación de casas**: por semilla, el color de pared se elige de una paleta tica: `#F2D7B6`, `#BFE3D0`,
   `#F7C6C7`, `#FFE8A3`, `#C9D8F0`, `#E8D5F2`, `#F4F1E8`. Solo para `Casa_*`, **no** para la casa del jugador
   ni para los negocios (su color es parte de su identidad).

### 4.5 Criterios de aceptación

- [ ] Recorrer la calle principal y el distrito norte: no hay dos edificios vecinos con la misma pared y el
      mismo techo.
- [ ] Ninguna puerta, ventana de atención ni estación quedó tapada (probar entrar a todas).
- [ ] Generar todas las texturas no deja advertencias en la consola.
- [ ] Commit: `feat(unity): give every building its own materials, weathering and details [SCRUM-94]`.

---

## Fase 5. Interiores realistas y distintos

**Objetivo visible:** cada interior tiene piso, paredes y objetos propios. Entrar a dos casas del pueblo da
dos casas distintas.

Archivos: `unity/Assets/Editor/MapBuilder.Interiors*.cs` (una por lugar: `Banco`, `Casa`, `Coop`, `Cuarto`,
`Places`, `Rural`, `Soda`, `Tienda`, `Extra`) y `MapBuilder.Interiors.cs` (`BuildFloors`, `WallFace`,
`ZoneFloor`, `InnerWall`).

### 5.1 Texturas de piso y pared

Agregar en `SpriteFactory` (procedimiento de 4.1):

| Archivo | Qué dibuja | Dónde |
| --- | --- | --- |
| `FloorCeramicTexture.png` (128/64) | Cerámica de 32 px con junta, cada baldosa ±4 % de brillo | Casa, Vecino, Soda |
| `FloorTerrazzoTexture.png` (128/64) | Fondo claro con piedritas de 1 a 3 px de 3 tonos | Banco, Cooperativa |
| `FloorMosaicTexture.png` (128/64) | Mosaico de 16 px en damero de 2 tonos | Hospital, HospitalArriba |
| `FloorWoodTexture.png` (128/64) | Duela de madera de 16 px con uniones alternadas | Finca, Inmobiliaria, Cuarto |
| `FloorCementTexture.png` (128/64) | Cemento lujado con manchas suaves y una grieta | Cárcel, Agencia (taller) |
| `WallPaintTexture.png` (128/64) | Pared pintada con grano muy fino | todas las paredes |

En `BuildFloors`: después del `Shape(... room.Name + "_Floor" ...)`, agregar
`TiledShape(parent, room.Name + "_FloorTexture", FloorTexFor(room.Name), room.Center, room.Size - new Vector2(0.1f, 0.1f), room.Color, order)`.
`FloorTexFor` devuelve la textura según la tabla; si es `null`, no hace nada (así no se rompe ningún lugar
nuevo). En `WallFace`, lo mismo con `WallPaintTexture`.

### 5.2 Detalle de pared (función nueva `DecorateWalls(Transform walls, Room room, int seed)`)

En cada interior, sobre la franja de pared superior:
- **Zócalo**: franja de 0,3 de alto, un 15 % más oscura que la pared.
- **Rodapié**: línea de 0,06 al pie, color madera.
- **Enchufes**: 2 o 3 rectángulos blancos de 0,12 × 0,16.
- **Cuadros**: de 1 a 3, cada uno con marco y un "paisaje" de 3 franjas de color (cielo, monte, pasto).
- **Reloj** en Banco, Soda, Hospital y Cooperativa; **calendario** en Casa y Vecino.
- **Grietas y manchas** (alfa 0,2) solo en Cárcel y en la variante "humilde" del vecino.

Todo con `Deco`, que nunca bloquea. Respetar la regla 11 (zonas libres).

### 5.3 Identidad por lugar (agregar dentro de cada `Build*Layout`)

| Lugar | Elementos nuevos |
| --- | --- |
| Soda (`BuildSodaLayout`) | Pizarra con "menú" (rayas), vitrina con empanadas (círculos dorados), abanico de techo con `FoliagePhysics` en modo clásico girando (o un componente `Spin` nuevo a 180°/s), botellas de salsa en las mesas |
| Banco | Cintas de fila (postes y franja roja), dispensador de fichas, pantalla de turnos con números que **parpadean** (`Flicker`), planta grande en la esquina |
| Hospital | Sillas de espera en fila, camilla, afiches de salud, dispensador de alcohol en gel, cortinas |
| Cooperativa | Afiches, escritorios con monitores encendidos (`Flicker` leve), archivero |
| Tienda | Estantes llenos de cajas de colores distintos, cajas registradoras, etiquetas de precio (sin texto) |
| Cárcel | Barrotes con sombra en el piso, banca de concreto, manchas |
| Finca | Sacos de café, herramientas colgadas, gallinas (círculos) que caminan con `Patrol` corto |
| Casa del jugador | Más objetos personales: libros, ropa en una silla, cargador del celular, plantas |

### 5.4 Casas del vecino distintas (la parte más delicada)

Hoy todas las casas ajenas llevan a la misma escena `Vecino`, construida por `BuildNeighborLayout`. Mantener
**una sola escena** y elegir la decoración al entrar:

1. En `unity/Assets/Scripts/World/HouseVisit.cs`, agregar
   `public static int VariantFor(string doorReturn)`, que devuelve de 0 a 3 con el mismo hash estable de
   4.3 aplicado a `doorReturn`. Con `doorReturn` vacío devuelve 0. Tests en `HouseVisitTests.cs`: es
   estable y cubre las 4 variantes con las puertas reales (`SpawnIds.TownFrom*` y los ids de las casas).
2. En `BuildNeighborLayout`, construir la base común (paredes, puertas, estaciones, perro) **igual que hoy**.
   Luego construir **4 grupos de decoración**, `"Variant_0"` a `"Variant_3"`, con muebles y colores
   distintos:
   - 0: familia con niños (juguetes, dibujos en la pared, sillón de color).
   - 1: abuelita (mecedora, carpetas tejidas, santos, plantas).
   - 2: estudiante (escritorio con laptop, afiches, colchón en el piso).
   - 3: casa humilde (pocos muebles, piso de cemento, manchas).
3. Componente nuevo `NeighborVariant.cs` en la raíz de los 4 grupos: en `Awake` activa solo el grupo
   `HouseVisit.VariantFor(Door.RememberedReturn)` y desactiva los demás. Debe correr **antes** de que algo
   busque objetos por nombre: `[DefaultExecutionOrder(-150)]`.
4. **Los grupos de variante solo tienen decoración, sin colisiones.** Las colisiones, las estaciones
   (`Thing_*`), el perro y el felpudo quedan en la base común, para que los tests de `PlacesPlayTests`
   (que buscan `Thing_Pet` según la casa) sigan funcionando.

### 5.5 Luz interior (opcional, requiere URP ya activo)

En interiores, hoy los sprites no tienen luz y se ven planos. Para agregar luz:
1. En `BuildInterior` llamar `BeginLitScene()` (ya existe en `MapBuilder.Lighting.cs`) y al final
   `EndLitScene()`, con `try/finally`, igual que en `BuildTown`.
2. Una luz global de intensidad **0,85**, color `#FFF4E0`, sin `DayLight` (la hora no afecta los interiores).
3. Una luz puntual por lámpara del techo (radio 4, intensidad 0,5, color `#FFE2B0`) y luz desde las ventanas
   (radio 3, `#DDEBFF`, 0,35). Máximo 4 luces por interior.
4. **Riesgo:** si un interior queda sin luz global, todo se ve negro. Agregar un test de PlayMode que cargue
   cada escena de interior y verifique que existe exactamente un `Light2D` de tipo `Global`.

### 5.6 Criterios de aceptación

- [ ] Cada interior tiene piso con textura y paredes con detalle.
- [ ] Entrar a 4 casas distintas del pueblo muestra al menos 3 decoraciones distintas.
- [ ] `PlacesPlayTests` y `InteractionTests` en verde.
- [ ] En ningún interior se tapó una estación, una puerta ni el felpudo.
- [ ] Commit: `feat(unity): give interiors their own floors, walls, props and neighbor variants [SCRUM-94]`.

---

## Fase 6. Cierre

1. Actualizar `docs/graficos-luces-animacion.md` con lo que se hizo en cada fase.
2. Build All Scenes, todos los tests en verde y una partida completa de 3 meses sin errores en la consola.
3. Build WebGL (menú `ColonQuest > Build > WebGL`) y probarla en el navegador:
   debe cargar y mantener al menos 30 fps al caminar por el pueblo de noche.
4. Commit final: `chore(unity): regenerate scenes with the visual overhaul [SCRUM-94]`.
5. Push a `origin feat/SCRUM-94-lighting-water-characters`. No abrir PR a `main`; el PR va a `develop`.

---

## Apéndice A. Mapa rápido de archivos

| Qué | Dónde |
| --- | --- |
| Helpers de dibujo (`Shape`, `TiledShape`, `Glow`, `Flickering`, órdenes, capas) | `unity/Assets/Editor/MapBuilder.cs` |
| Luces 2D (`ApplyLighting`, `KeepUnlit`, `AddNightLight`, `BeginLitScene`) | `unity/Assets/Editor/MapBuilder.Lighting.cs` |
| Edificios del pueblo (`Building`, `DrawEntrance`, `DrawFacadeWindows`, `Tree`, `Lamp`, `Bush`) | `unity/Assets/Editor/MapBuilder.Town.Props.cs` |
| Lista de edificios (`BuildingSpec`: nombre, colores, medidas, puerta) | `unity/Assets/Editor/MapBuilder.Town.cs`, `World.cs`, `Districts.Props.cs` |
| Detalles de fachada, cables, rótulos, porches | `unity/Assets/Editor/MapBuilder.City.cs` |
| Mar, río, palmeras, cascada | `unity/Assets/Editor/MapBuilder.World.cs` |
| Pasto, flores y rocas de cobertura | `unity/Assets/Editor/MapBuilder.Depth.cs` |
| Personas (cuerpo, cabeza, pelo, ropa, vista de espaldas) | `unity/Assets/Editor/MapBuilder.People.cs` |
| Jugador y NPC (`BuildPlayer`, `BuildNpc`) | `unity/Assets/Editor/MapBuilder.Characters.cs` |
| Interiores | `unity/Assets/Editor/MapBuilder.Interiors*.cs` |
| Texturas generadas por código | `unity/Assets/Editor/SpriteFactory.cs` |
| Animación de personas | `unity/Assets/Scripts/World/PlayerAnimator.cs`, `PersonMotion.cs`, `PersonPose.cs` |
| Viento y rebote de plantas | `unity/Assets/Scripts/World/FoliagePhysics.cs` |
| Agua | `WaterMath.cs`, `ShoreLap.cs`, `Swell.cs`, `WaterFlow.cs`, `RippleField.cs`, `BoatRock.cs` |
| Perro | `unity/Assets/Scripts/World/DogAnimator.cs`, `unity/Assets/Editor/MapBuilder.Dog.cs` |
| Hora del día y luz | `DayCycle.cs`, `DayTint.cs`, `unity/Assets/Scripts/Lighting/` |
| Tests de lógica | `unity/Assets/Tests/EditMode/World/` |
| Tests jugando escenas | `unity/Assets/Tests/PlayMode/` |

## Apéndice B. Errores que ya pasaron en este proyecto (no repetirlos)

- Objetos que tapan puertas o desplazan estaciones al cambiar el orden de los números aleatorios (regla 9).
- Colisiones en adornos que dejan al jugador atascado (regla 10).
- Componentes en un archivo con otro nombre que desaparecen de la escena (regla 2).
- Eventos `static` sin desuscribir que se duplican al cambiar de escena (fase 3.9).
- `SetActive` llamado dentro de `OnDisable` mientras el objeto se destruye: da error. Ver el comentario en
  `PlayerAnimator.OnDisable`.
- Escenas sin regenerar: el código cambia pero el juego no (sección 0).

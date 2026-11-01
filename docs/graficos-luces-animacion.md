# Gráficos: luces 2D, agua, viento, perro y personajes

Rama `feat/SCRUM-94-lighting-water-characters`. Todo el cambio vive en el código que arma las escenas
(`MapBuilder`) y en componentes de `Scripts/World` y `Scripts/Lighting`. No toca el motor financiero ni el backend.

## Qué hay que hacer en Unity para verlo (obligatorio)

1. Abrir el proyecto. Unity instala solo el paquete `com.unity.render-pipelines.universal` 17.3.0 (ya está en
   `Packages/manifest.json`) y actualiza `packages-lock.json`.
2. Menú **Colon Quest > Build All Scenes**. La primera vez crea `Assets/Settings/ColonQuest_URP2D.asset` y su
   renderer 2D, y los pone como pipeline del proyecto (Project Settings > Graphics). También se puede hacer aparte con
   **Colon Quest > Setup URP 2D Lighting**.
3. Revisar la consola: no debe haber errores. Si aparece "Building the town without 2D lights", el pueblo se armó con
   el aspecto viejo (sin luces) y hay que revisar el paso 2.
4. Correr los tests de EditMode y PlayMode (Window > General > Test Runner).
5. Hacer commit de las escenas regeneradas, de `Assets/Settings/` y de `ProjectSettings/` (GraphicsSettings cambia).

Si algo sale mal, basta con quitar el pipeline en Project Settings > Graphics y volver a correr Build All Scenes:
el pueblo se arma con el aspecto anterior (tinte de color y sprites sin luz).

## Qué cambia

| Parte | Qué se ve | Dónde |
| --- | --- | --- |
| Luz del día | Una luz global 2D cambia de color e intensidad con la hora: mañana cálida, mediodía blanco, atardecer dorado a las 18:00 y noche azul. Nunca baja de 0,55 para que todo se lea | `DayCycle.LightFor`, `Lighting/DayLight.cs` |
| Luces de noche | Postes, faroles coloniales, luces de porche y una luz suave alrededor del jugador se encienden de noche (con un parpadeo leve) y se apagan de día | `Lighting/NightLight.cs`, `MapBuilder.Lighting.cs` |
| Ventanas | De noche, tres de cada cuatro ventanas de las casas se iluminan en tono cálido | `WindowNightGlow` en `MapBuilder.Town.Props.cs` |
| Árboles y palmeras | Una sola brisa cruza el mapa de oeste a este; cada copa se dobla desde el pie del tronco, la sigue con un resorte suave y se asienta en uno o dos vaivenes cuando el jugador pasa | `FoliagePhysics` (modo `UseGlobalWind`, `PivotDrop`) |
| Mar | Espuma que sube por la arena y se retira, líneas de oleaje que llegan a la orilla, anillos en el agua y botes que se mecen | `ShoreLap`, `Swell`, `RippleField`, `BoatRock` |
| Río | Corriente con estelas que bajan bajo los puentes | `WaterFlow` |
| Jugador junto al agua | Anillos a los pies al caminar por la orilla del mar, el río o la laguna | `RippleField` |
| Perro | Respira, mueve la oreja, parpadea, se duerme si nadie se acerca, levanta la cabeza y mueve la cola al acercarse el jugador, y al acariciarlo salta, saca la lengua y suelta un corazón | `DogAnimator`, `MapBuilder.Dog.cs` |
| Personajes | De espaldas al caminar hacia arriba (nuca de pelo; se ocultan cara, flequillo, corbata y gafetes), paso con peso, pequeño rebote al frenar y leve inclinación al caminar de lado | `PlayerAnimator`, `BuildBackView` en `MapBuilder.People.cs` |
| Polvo | Nubecitas de arena o polvo al caminar por la playa y la calle | `FootDust` |

Las interiores y el menú no tienen luces: el renderer 2D dibuja un sprite sin luz igual que antes.

## Verificación hecha y pendiente

- Hecho: todo el código compila contra las DLL de referencia de Unity (con stubs de la API de URP 17.3 tomados de su
  código fuente de la rama 6000.3); la lógica nueva tiene tests de EditMode (`WaterMathTests`, `DogMotionTests` y
  casos nuevos en `PersonMotionTests`, `FoliagePhysicsTests`, `DayCycleTests` y `SurfacesTests`).
- Pendiente: abrir Unity, regenerar escenas, correr los tests y mirar el pueblo de día y de noche, y probar la build
  WebGL (las luces 2D funcionan en WebGL 2).
- En la vista de escena del editor (sin Play) las ventanas se ven encendidas: su script las apaga de día solo en Play,
  igual que las luces de porche que ya existían.

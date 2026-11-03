# Gráficos: luces 2D, agua, viento, perro y personajes

Rama `feat/SCRUM-94-lighting-water-characters`. Todo el cambio vive en el código que arma las escenas
(`MapBuilder`) y en componentes de `Scripts/World` y `Scripts/Lighting`. No toca el motor financiero ni el backend.

## Qué hay que hacer en Unity para verlo (obligatorio)

1. Abrir el proyecto. Unity instala solo el paquete `com.unity.render-pipelines.universal` 17.3.0 (ya está en
   `Packages/manifest.json`) y actualiza `packages-lock.json`.
2. Menú **ColonQuest > Build All Scenes**. La primera vez crea `Assets/Settings/ColonQuest_URP2D.asset` y su
   renderer 2D, y los pone como pipeline del proyecto (Project Settings > Graphics). También se puede hacer aparte con
   **ColonQuest > Setup URP 2D Lighting**.
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
| Vegetación reactiva | Sacudida elástica al contacto del jugador, ráfagas de hojas con física y pasto que se aparta al caminar | `FoliagePhysics`, `LeafBurst`, `MapBuilder.Town.Props.cs` |
| Agua y cascada viva | Oleaje cáustico multicapa, resaca en arena húmeda, corriente de río con estelas en puentes y cascada con niebla, salpicaduras y arcoíris | `MapBuilder.World.cs`, `WaterFlow`, `Swell` |
| Reacciones de personajes | Giros con inclinación elástica, parpadeo e idles con cambio de peso, y burbujas de reacción contextual | `PersonBob`, `ReactionBubble`, `MapBuilder.People.cs` |
| Fachadas y envejecimiento | Texturas procedurales (estuco, zinc, madera, ladrillo, tejas), manchas de óxido/humedad, verjas coloniales, medidor de luz, bajantes y antenas | `SpriteFactory.cs`, `BuildingMath.cs`, `MapBuilder.Town.Props.cs` |
| Interiores realistas y luces | Pisos cerámicos/terrazo/mosaico, zócalos, cuadros, abanico de techo, objetos personales, 4 variantes de vecinos y luz 2D cenital/ventana | `MapBuilder.Interiors*.cs`, `NeighborVariant.cs`, `Spin.cs`, `MapBuilder.Lighting.cs` |

## Implementación por fases del salto visual (SCRUM-94)

1. **Fase 1: Vegetación y follaje reactivo**:
   - `LeafBurst` suelta hojas que caen y se dispersan al rozar árboles y arbustos.
   - `FoliagePhysics` reacciona tanto al viento global como a la presencia y velocidad del jugador.
2. **Fase 2: Agua, río y cascada**:
   - Mar con espuma volumétrica, resaca dinámica en arena mojada y vaivén de barcos.
   - Río animado con corriente y espuma alrededor de los pilotes de los puentes.
   - Cascada con partículas de caída de agua, arcoíris tenue refractado y niebla ascendente en la base.
3. **Fase 3: Personajes vivos y expresivos**:
   - Animación de respiro y cambio de apoyo de peso en reposo.
   - Pestañeo natural y micro-movimiento de cabeza.
   - Inclinación sutil de 4-6° al girar o cambiar de dirección bruscamente.
   - Burbujas de reacción contextual ante eventos y compras.
4. **Fase 4: Materiales exteriores y envejecimiento**:
   - 9 texturas de sprites procedurales (estuco, bloques de concreto, tinglado de madera, zinc corrugado, teja de barro, suciedad/moho).
   - `BuildingMath` determinista sin aleatoriedad por frame.
   - Desgaste realista: manchas de lluvia bajo repisas de ventana, salpicaduras de barro en el zócalo exterior, rejas de hierro forjado, medidores eléctricos a 1.2 m, bajantes pluviales y cables de tendido eléctrico aéreo.
5. **Fase 5: Interiores detallados, variantes de vecinos y luces 2D**:
   - 6 texturas procedurales para interiores: cerámica moderna, mosaico hidráulico, terrazo vintage, cemento afinado, madera y pintura satinada.
   - Zócalos protectores, rodapiés, molduras perimetrales y tomacorrientes a 30 cm del piso.
   - Cuadros de paisaje con marcos biselados, relojes de pared en negocios y calendarios en viviendas.
   - Abanicos de techo con script de rotación continua `Spin.cs`.
   - `NeighborVariant.cs` con 4 ambientaciones únicas (soltero, familia numerosa, abuelos, artesano) que se eligen determinísticamente según la casa visitada.
   - Objetos personales del protagonista en su cuarto (libros, cargador de teléfono, ropa doblada).
   - Iluminación URP 2D interior: luz global tenue cálida, lámpara colgante cenital y foco suave desde la ventana.

## Verificación hecha

- **Tests EditMode**: 632 / 632 aprobados (100% green).
- **Tests PlayMode**: 150 / 150 aprobados (100% green).
- Todas las escenas regeneradas limpiamente mediante `MapBuilder.BuildAll` sin errores en consola.

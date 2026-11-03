# Personajes v2: contexto y pasos de implementación

Guía para llevar al juego el rediseño de los personajes: cabeza, pelo, brazos, proporciones y
un elenco de 20 personajes con identidad propia. Está escrita para quien lo implemente en Unity
(Jefferson, Keylor o una sesión de Claude Code) sin haber visto la conversación donde se diseñó.

- **Ticket:** SCRUM-58 (pulido).
- **Rama base:** `feat/SCRUM-58-alpha-fixes`. Ahí está el código de personajes más reciente; en
  `develop` es una versión anterior.
- **Referencias visuales:** `docs/art/characters-v2/`. Las imágenes son una aproximación dibujada
  con PIL usando solo las formas que tiene Unity. No son capturas del juego.

| Archivo | Qué muestra |
| --- | --- |
| `elenco_v2.png` | Los 20 personajes propuestos |
| `elenco_v2_siluetas.png` | Prueba de siluetas del elenco |
| `comparacion.png` | Personajes de hoy (arriba) contra la v2 (abajo) |
| `comparacion_siluetas.png` | Las mismas siluetas, hoy contra v2 |
| `brazos_antes_despues.png` | Solo el cambio de brazos |
| `mockup_v2.py` | El script que dibuja el elenco. **Las medidas de esta guía salen de aquí** |

Para regenerar las imágenes: `python docs/art/characters-v2/mockup_v2.py out.png [color|sil]`.

---

## 1. Cómo funcionan los personajes hoy

### Archivos

| Archivo | Rol |
| --- | --- |
| `unity/Assets/Editor/MapBuilder.Characters.cs` | `Look`, `Accessory`, `BuildPlayer`, `BuildNpc`, `BuildPedestrian` |
| `unity/Assets/Editor/MapBuilder.People.cs` | `BuildPersonVisual`, `Leg`, `Arm`, `BuildHead`, `Eye`, `DrawHair`, `DrawHat`, `DrawTorsoAccessory`, `Part2` |
| `unity/Assets/Editor/MapBuilder.cs` | `Shape(...)` (devuelve un `SpriteRenderer`), sprites `circle`, `rounded`, `roundedSmall`, `square`, `softCircle`; `OrderPlayer = 20` |
| `unity/Assets/Scripts/World/PlayerAnimator.cs` | `PersonRig` y la animación: caminar, respirar, parpadear, mirar, saludar, hablar |
| `unity/Assets/Scripts/World/PersonMotion.cs` | Funciones puras de movimiento (`LegAngle`, `ArmAngle`, `WaveAngle`, `MouthOpen`, …) con tests en `Tests/EditMode/World/PersonMotionTests.cs` |
| `MapBuilder.Town.cs`, `Interiors.*.cs`, `Crossings.cs`, `Districts.Props.cs` | Donde se crea cada NPC con `new Look { ... }`. Hay unas 40 llamadas a `BuildNpc`/`BuildPedestrian` |

Las escenas **se generan por código**: después de cualquier cambio hay que correr
`ColonQuest > Build All Scenes` y commitear las escenas regeneradas.

### El modelo actual

```csharp
private struct Look { public string Skin; public Accessory Extra; public string ExtraColor; }
```

- `Look.Skin` **no es la piel**: es el color de la camisa.
- Piel, pelo, pantalón y peinado se sacan de `LookSeed(look)`, un hash del color de camisa.
  Nadie los elige.
- Todos usan la misma plantilla: torso de 0.52 × 0.52, cabeza de 0.47, mismos ojos, misma boca,
  faja con hebilla dorada y tres botones.
- `Accessory` tiene 6 valores: `None`, `Cap`, `Hood`, `Helmet`, `Tie`, `Vest`.

### Problemas detectados

| Zona | Problema | Causa en el código |
| --- | --- | --- |
| Brazos | Parecen tubos pegados al costado, con doble contorno | `Arm()` cuelga de `x = ±0.3` y el torso termina en ±0.26; cada parte tiene su contorno (`Part2`) |
| Brazos | Nacen a media altura del pecho | El pivote está en `y = 0.17` y el torso llega a ~0.24 |
| Brazos | Rígidos, sin codo, cortos (la mano queda en la faja) | Una sola pieza de 0.14 × 0.34 que gira entera desde el hombro |
| Brazos | Se despegan al respirar o al inclinarse | Son hijos de `Visual`, no de `Torso`, y `PlayerAnimator` escala y rota `Torso` |
| Ojos | Mirada de susto | Ojos de 0.17 con aro blanco grueso |
| Boca | Igual en todos; sin expresión | Una barra que `PlayerAnimator` aplasta a 0.25 en reposo (`PersonMotion.MouthOpen`) |
| Pelo | Peluca de Lego que enmarca la cara | Tabla de 0.5 × 0.18 encima más rectángulos a los lados |
| Pelo | **Error:** el moño sale encima del casco o la gorra | `DrawHairStyle` caso 2 dibuja `Bun` sin mirar si hay sombrero (le pasa a Don Rafa) |
| Siluetas | Todos son la misma figura | Mismas proporciones para todos |
| Roles | El prestamista y el juez usan la misma capucha; el oficial y la enfermera, la misma gorra | Solo hay 6 accesorios |
| Retratos | No combinan con el juego | `STYLE_PROMPT` de `server/app/images/service.py` pide "16-bit pixel art" y el juego es vectorial |

---

## 2. Qué cambia en la v2

1. **Apariencia explícita** para los personajes con nombre. El hash queda solo para peatones.
2. **Proporciones por personaje:** ancho, alto y tamaño de cabeza.
3. **Cabeza nueva:** ojos más chicos con casi todo pupila, cejas con ángulo, boca con expresión,
   mandíbula, bigote, chiva, lentes y párpado caído.
4. **Pelo como cúpula:** una forma detrás de la cabeza que da el cráneo, más un flequillo según el
   estilo. El sombrero tapa el pelo de arriba.
5. **Brazos con codo:** el hombro dentro del torso, el brazo detrás de él, manga corta por defecto,
   la mano a la cadera, y los brazos hijos del torso.
6. **Ropa con identidad:** se quitan la faja con hebilla y los botones por defecto; cada
   personaje lleva sus detalles (gafete, overol, delantal, cadena, estetoscopio…).

Regla de diseño: **quien engaña se ve distinto de quien ayuda**. El prestamista y la financiera
tienen sonrisa de lado y párpado caído; la asesora de la coope, Doña Marta y Don Beto tienen
expresión cálida. Se muestra, no se dice.

---

## 3. Medidas (copiar de aquí o de `mockup_v2.py`)

Unidades de mundo de Unity, `y` hacia arriba, relativas al root del personaje. `W` es el ancho del
personaje, `Hh` el alto y `headW` el tamaño de cabeza (1.0 = como hoy). "Contorno" es `Part2`
(una forma oscura 0.045 más grande detrás). El orden es relativo a `OrderPlayer`.

### Cuerpo

| Parte | Centro | Tamaño | Orden | Notas |
| --- | --- | --- | --- | --- |
| Sombra | (0, −0.52) | (0.9·W, 0.3) | −1 | `softCircle` |
| Pierna L/R | (±0.1·W, −0.34·Hh) | (0.16, 0.34·Hh) | +1 | Con contorno. Con falda: ancho 0.10 y color piel |
| Zapato | (±0.1·W + 0.02, −0.52·Hh) | (0.21, 0.10) | +3 | Con contorno; color por personaje (por defecto `#2A2F45`) |
| Falda (opcional) | (0, −0.26·Hh) | (0.42·W, 0.26) | +1.6 | `rounded`, con contorno |
| Torso | (0, −0.02) | (0.5·W, 0.5·Hh) | +6 | `rounded`, con contorno |
| Sombra del torso | (0.03, −0.02 − 0.15·Hh) | (0.5·W − 0.08, 0.16) | +7 | `Shade(shirt, 0.84)` |
| Cuello de camisa | (0, −0.02 + 0.215·Hh) | (0.2, 0.07) | +8 | `Shade(shirt, 0.78)` |
| Cuello (piel) | (0, −0.02 + 0.25·Hh) | (0.12, 0.09) | +7.6 | `Shade(skin, 0.84)` |

**Se quitan** `Belt`, `Buckle` y los tres `Button` por defecto.

### Brazos (detrás del torso)

| Parte | Valor |
| --- | --- |
| Pivote del hombro | (±0.235·W, 0.13), es decir `0.15 + ty` con `ty = −0.02` |
| Ángulo en reposo | 22° hacia afuera |
| Brazo (manga) | 0.13 de ancho, 0.2·Hh de largo (+0.08 de solape), orden +4, con contorno |
| Pivote del codo | Al final del brazo |
| Antebrazo | 0.10 de ancho, 0.2·Hh de largo (+0.06), orden +3, con contorno; doblado 14° hacia adentro; color piel (o camisa si `LongSleeve`) |
| Borde de la manga corta | Óvalo (0.13, 0.06) en el codo, `Shade(shirt, 0.82)`, orden +4.8 |
| Mano | Círculo de 0.12 en la muñeca, orden +5, con contorno |

### Cabeza (pivote `Head` en `y = 0.27·Hh − 0.02 + 0.02`)

Todas las `x` se multiplican por `headW`.

| Parte | Centro | Tamaño | Orden | Notas |
| --- | --- | --- | --- | --- |
| Pelo de atrás (cúpula) | (0, 0.27) | (0.54·headW, 0.40) | +9 | Con contorno. **No se dibuja** si hay sombrero o si es calvo |
| Pelo largo (estilo `long`) | (0, 0.12) | (0.6·headW, 0.62) | +9 | Reemplaza la cúpula |
| Oreja L/R | (±0.235, 0.17) | (0.09, 0.12) | +10 | Con contorno |
| Cara | (0, 0.20) | (0.5·headW, 0.46) | +11 | Con contorno |
| Sombra de mandíbula | (0, 0.06) | (0.4·headW, 0.16) | +11.8 | `Shade(skin, 0.9)`, tapada por la cara interior |
| Cara interior | (0, 0.20) | (0.45·headW, 0.39) | +11.9 | Color piel |
| Mandíbula cuadrada (opcional) | (0, 0.06) | (0.43·headW, 0.20) | +11.95 | `rounded`, sin contorno |
| Ojo (blanco) L/R | (±0.10, 0.21) | (0.095, 0.115) | +14 | Contorno fino (0.025) |
| Pupila | ojo + (0.012, −0.005) | (0.065, 0.08) | +15 | Es la que mueve `PlayerAnimator`; `x` de reposo por personaje (mirar de lado) |
| Brillo | pupila + (0.012, 0.017) | (0.022, 0.022) | +16 | Blanco |
| Párpado caído (opcional) | ojo + (0, 0.045) | (0.12, 0.05) | +16.5 | Color piel, fijo. Aparte del párpado que parpadea |
| Lentes (opcional) | ojo | aro (0.15, 0.14) y vidrio (0.12, 0.11) | +13.5 / +13.7 | Puente (0.06, 0.02) en (0, 0.215), orden +16.6 |
| Ceja L/R | (±0.10, 0.305) | (0.12, 0.035) | +17 | Rotación según expresión (tabla abajo) |
| Nariz | (0, 0.13) | (0.065, 0.05) | +17 | `Shade(skin, 0.82)` |
| Boca | (mouthX, 0.065) | ver expresiones | +18 | Es el transform `Mouth` que anima `PlayerAnimator` |
| Cachetes (opcional) | (±0.15, 0.10) | (0.08, 0.045) | +17 | Piel mezclada 35 % con `#FF828C` |
| Bigote (opcional) | (0, 0.095) | (0.16, 0.05) | +18.8 | `rounded`, color de cejas |
| Chiva (opcional) | (mouthX, 0.0) | (0.16, 0.08) | +19 | Con contorno, color de pelo |

### Expresiones

| Expresión | Cejas (rot L, rot R) | Boca |
| --- | --- | --- |
| `Smile` | (0°, 0°) | Media luna: círculo oscuro `#7A2E3A` (0.13, 0.08) más un círculo color piel (0.14, 0.07) corrido 0.025 hacia arriba, orden +18.5 |
| `Warm` | (−8°, 8°) | Igual que `Smile` |
| `Smirk` | (−12°, 14°), la derecha 0.015 más arriba | Media luna corrida 0.03 a la derecha y rotada 12° |
| `Frown` | (14°, −14°) | Media luna invertida (el círculo de piel corrido 0.025 hacia abajo) |
| `Neutral` | (0°, 0°) | Barra (0.10, 0.025) |

La media luna sigue funcionando con la animación de hablar, porque `PlayerAnimator` escala
`Mouth` en `y` (0.25 en reposo, hasta 1 al hablar). **Hay que revisar en pantalla** que en reposo
no desaparezca. Si pasa, subir el mínimo de `MouthOpen` para las expresiones con media luna o no
escalar la boca en reposo.

### Pelo (flequillo, orden +20, con contorno)

| Estilo | Formas |
| --- | --- |
| `Side` | Óvalo (0.5·headW, 0.2) en (0, 0.38) + mechón `rounded` (0.3, 0.11) en (−0.07, 0.36) rotado −18° |
| `Long` | Dos óvalos (0.3, 0.17) en (±0.1, 0.37) rotados ∓14° (raya al medio) |
| `Spiky` | Óvalo (0.48·headW, 0.16) en (0, 0.39) + 4 `rounded` (0.1, 0.17) en x = −0.15, −0.05, 0.06, 0.16, y = 0.48, rotados 25°, 8°, −8°, −25° (orden +19.5) |
| `Bun` | Óvalo (0.5·headW, 0.18) en (0, 0.39) + moño, círculo de 0.17 en (0, 0.55), orden +8.5. **Solo sin sombrero** |
| `Ponytail` | Óvalo como `Bun` + cola `rounded` (0.11, 0.3) en (0.27, 0.18) rotada −12°, orden +8.5 |
| `Curly` | 5 círculos (0.16, 0.15) en (−0.18, 0.33), (−0.1, 0.42), (0, 0.45), (0.1, 0.42), (0.18, 0.33) |
| `Afro` | Óvalo grande (0.68·headW, 0.42) en (0, 0.36) detrás (orden +8.4) + óvalo (0.5·headW, 0.16) en (0, 0.41) |
| `Bob` | `rounded` (0.58·headW, 0.42) en (0, 0.2) detrás (orden +8.6) + óvalo (0.52·headW, 0.18) en (0.04, 0.39) |
| `Buzz` | Óvalo (0.48·headW, 0.13) en (0, 0.40) |
| `Bald` | Solo los lados: óvalos (0.08, 0.14) en (±0.2, 0.27) |

Con sombrero solo se dibuja el pelo de los lados o de atrás (cola, pelo largo); **nunca** el moño
ni las puntas.

### Sombreros (orden +21, con contorno)

| Sombrero | Formas |
| --- | --- |
| `Cap` | Copa: óvalo (0.54·headW, 0.24) en (0, 0.40). Visera: óvalo (0.34, 0.08) en (±0.12, 0.31), `Shade(c, 0.75)`, orden +21.5. El lado de la visera es por personaje |
| `FlatCap` | `rounded` (0.56·headW, 0.18) en (0, 0.39) + visera `rounded` (0.5, 0.06) en (0, 0.31) |
| `Helmet` | Óvalo (0.58·headW, 0.30) en (0, 0.38) + borde `rounded` (0.62·headW, 0.06) en (0, 0.28) |
| `Police` | `rounded` (0.56·headW, 0.2) en (0, 0.42) + visera negra (0.5, 0.07) en (0, 0.31) + escudo dorado de 0.08 en (0, 0.43) |
| `Nurse` | `rounded` (0.3, 0.1) en (0, 0.45) + cruz o franja azul |
| `Beanie` | Óvalo (0.56·headW, 0.30) en (0, 0.38) + borde doblado `rounded` (0.58·headW, 0.07) en (0, 0.28) |
| `Chonete` | Ala: óvalo (0.78, 0.13) en (0, 0.31). Copa: óvalo (0.44, 0.2) en (0, 0.40), orden +21.6. Cinta (0.44, 0.035) en (0, 0.33) |
| `Hood` | Se mantiene la de hoy, pero ya no la usa el prestamista |

### Detalles de torso (orden +8.4 a +9.8)

`Tie(color)`, `Badge` (gafete blanco), `Lanyard(color)`, `Phone` (en la mano derecha), `Watch`,
`Pen`, `Chain` (cadena dorada), `Pouch` (riñonera), `Overall(color)`, `Apron` (delantal),
`Coat` (gabacha blanca abierta), `Stethoscope`, `Robe` (cuello blanco de toga), `Scarf(color)`,
`Stripe(color)` (franja horizontal), `Backpack` (tirantes), `Cardigan(color)`, `Bag` (bolso),
`PoliceBadge` (placa y faja negra). Las medidas exactas de cada uno están en `mockup_v2.py`.

---

## 4. El elenco de 20

Colores en hex. Los nombres de la columna "En el código" son los `name` que ya existen en las
llamadas a `BuildNpc`/`BuildPedestrian`; los que dicen *nuevo* no existen todavía.

| # | Personaje | En el código | Piel | Pelo (color, estilo) | Camisa / pantalón | Proporción (W, Hh, headW) | Sombrero | Expresión | Extras |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Jugador | `Player` | `#D9A47A` | `#3A2418`, Side | `#E07A3F` / `#2F3E66` | 1, 1, 1 | — | Smile, mira al frente | Backpack |
| 2 | Kevin, vendedor de cuotas | `Vendedor` (Tienda) | `#EBC29A` | `#2A1F1A`, Spiky | `#2E86C1` / `#3A3F52` | 0.92, 1.06, 1 | — | Smile, mira de lado (0.018) | Tie `#E8505B`, Badge, Phone |
| 3 | Don Rafa, mecánico | `DonRafa`, `Mecanico` | `#8A5A3C` | `#2A1F1A`, Buzz | `#5B6E8C` / `#3E4A63` | 1.14, 0.94, 1.05 | Cap `#C0392B`, visera a la izquierda | Frown | Mandíbula cuadrada, bigote, Overall `#3E5A86` |
| 4 | Doña Marta, pulpería | `DonaMarta` | `#C98E66` | `#9A9A9A`, Bun | `#D9707F` / `#5B4B4B` | 1.1, 0.9, 1 | — | Warm | Lentes, cachetes, Apron |
| 5 | Prestamista "gota a gota" | `Prestamista` | `#B5835A` | `#1A1A1A`, Buzz | `#2D2D2D` / `#1F2433` | 1.12, 1, 1 | FlatCap `#3A3A3A` | Smirk (mouthX 0.02) | Chiva, párpado caído, Chain, Pouch |
| 6 | Brayan, el de la pirámide | `Promotor` | `#E2B48C` | `#4A2F20`, Side | `#F4F1E8` / `#2B3550` | 0.95, 1.04, 1 | — | Smile, mira de lado (0.02) | Tie `#7D3C98`, Watch, Phone |
| 7 | Don Beto, finca | `DonBeto` | `#A06A45` | `#6A6A6A`, Bald | `#8FAF6A` / `#5A4A3A` | 1, 0.98, 1 | Chonete `#E8D9A8` | Warm | Bigote, zapatos `#1E1E1E` (botas) |
| 8 | Cajera del banco | `Cajera`, `Cajero2` | `#C68A62` | `#2A1F1A`, Ponytail | `#EAF0F8` / `#1F3A5F` | 1, 1, 1 | — | Neutral | Falda, Scarf `#1D4F8C`, Lanyard `#1D4F8C` |
| 9 | Oficial de la Fuerza Pública | `Oficial` (interiores y cruces) | `#9C6B47` | `#1A1A1A`, Buzz | `#22324F` / `#1A2438` | 1.08, 1, 1 | Police `#22324F` | Neutral | Manga larga, PoliceBadge |
| 10 | Jueza | `Juez` | `#D7A27E` | `#5A3A28`, Bob | `#1E1E26` / `#1E1E26` | 1, 1, 1 | — | Neutral | Lentes, manga larga, Robe |
| 11 | Enfermero de la CCSS | `Enfermera` | `#B07A52` | `#2A1F1A`, Curly | `#7FC8B8` / `#7FC8B8` | 1, 1, 1 | — | Smile | Pen |
| 12 | Doctora | `Doctora`, `Doctor1..` | `#F0CDB0` | `#B0452D`, Ponytail | `#6FA8DC` / `#3A3F52` | 1, 1, 1 | — | Warm | Coat, Stethoscope |
| 13 | Asesora de la coope | `AsesoraCoop` | `#8A5A3C` | `#1A1A1A`, Afro | `#2E8B57` / `#2F3E66` | 1, 1, 1 | — | Smile | Cardigan `#F1E4C8`, Lanyard `#2E8B57` |
| 14 | Doña Lucía, soda | `DonaLucia` | `#C98E66` | `#3A2418`, Bun | `#F2C14E` / `#7A4E3A` | 1.12, 0.92, 1 | Beanie `#E8505B` | Warm | Cachetes, Apron |
| 15 | Don Chepe, tienda | `DonChepe` | `#E0B08A` | `#BDBDBD`, Bald | `#9E3D3D` / `#3A3F52` | 1.08, 1, 1 | — | Smile | Lentes, bigote, Cardigan `#6B5B50` |
| 16 | Don Mario, vecino | `DonMario` | `#B5835A` | `#555A66`, Buzz | `#E9E2D0` / `#4A5A3A` | 1.05, 0.97, 1 | — | Neutral | Chiva, Stripe `#C0392B` |
| 17 | Financiera | `Financiera` | `#E7B994` | `#D9A441`, Bob | `#F7F7F7` / `#2B2B3A` | 1, 1.03, 1 | — | Smirk (mouthX 0.02) | Falda, párpado caído, Cardigan `#2A7F8C`, Bag |
| 18 | Agente inmobiliario | `AgenteInmobiliaria` | `#9C6B47` | `#2A1F1A`, Side | `#34495E` / `#2C3E50` | 1, 1.05, 1 | — | Smile | Manga larga, Tie `#F2C14E`, Pen |
| 19 | Estudiante de la UNA | *nuevo* (peatón) | `#D9A47A` | `#1A1A1A`, Long | `#C0392B` / `#2F3E66` | 0.92, 0.97, 1 | — | Smile | Backpack, Stripe `#F4F1E8` |
| 20 | Repartidor | *nuevo* (peatón) | `#80553A` | `#1A1A1A`, Buzz | `#F39C12` / `#2D2D2D` | 1, 1, 1 | Helmet `#2D2D2D` | Neutral | Manga larga, Stripe `#2D2D2D` |

Los NPC que no están en la tabla (`Vendedor1`, `Vendedor2`, `Cajero` de la tienda,
`Laboratorista`, `Estafador`, los `Peaton_*`) siguen con el hash, pero con la cabeza, el pelo y
los brazos nuevos. `Estafador` (Town) puede reutilizar la ficha del prestamista o de Brayan.

Detalles que quedaron flojos en el mockup y conviene corregir al implementar:
- El pelo largo de la estudiante tapa mucho el torso: bajar el alto de 0.62 a ~0.5.
- Las canas a los lados de Don Chepe parecen audífonos: hacerlas más chicas o pegarlas a la oreja.
- El gorro de Doña Lucía se confunde con una gorra: subir el doblez o usar un pañuelo.

---

## 5. Pasos de implementación

Trabajar en una rama nueva desde `feat/SCRUM-58-alpha-fixes`, por ejemplo
`feat/SCRUM-58-characters-v2`. Commits pequeños con el formato del repo:
`feat(unity): ... [SCRUM-58]`.

### Paso 1. Ampliar `Look` sin romper las llamadas existentes

En `MapBuilder.Characters.cs`. Las ~40 llamadas usan `new Look { Skin, Extra, ExtraColor }`, así
que los campos nuevos tienen que tener un valor por defecto que signifique "usar lo de hoy":

```csharp
private enum HairStyle { Auto, Side, Long, Spiky, Bun, Ponytail, Curly, Afro, Bob, Buzz, Bald }
private enum Hat { None, Cap, FlatCap, Helmet, Police, Nurse, Beanie, Chonete, Hood }
private enum Expression { Smile, Warm, Smirk, Frown, Neutral }
[System.Flags] private enum Extras { None = 0, Tie = 1, Badge = 2, Lanyard = 4, Phone = 8, Watch = 16, Pen = 32,
    Chain = 64, Pouch = 128, Overall = 256, Apron = 512, Coat = 1024, Stethoscope = 2048, Robe = 4096,
    Scarf = 8192, Stripe = 16384, Backpack = 32768, Cardigan = 65536, Bag = 131072, PoliceBadge = 262144 }

private struct Look
{
    public string Skin;          // shirt color (name kept so existing calls compile)
    public Accessory Extra;      // legacy accessory, mapped to Hat/Extras when those are empty
    public string ExtraColor;

    public string SkinTone;      // null = from LookSeed
    public string HairColor;     // null = from LookSeed
    public HairStyle Hair;       // Auto = from LookSeed
    public string Pants;         // null = from LookSeed
    public string Shoes;         // null = #2A2F45
    public Hat Hat;              // None + Extra = mapped from the legacy accessory
    public string HatColor;
    public Expression Face;      // Smile by default
    public Extras Wear;
    public string WearColor;     // tie, lanyard, cardigan, overall…
    public float Width, Height, Head;   // 0 = 1
    public bool Glasses, Mustache, Goatee, HeavyLids, SquareJaw, Blush, LongSleeve, Skirt;
    public float LookAside;      // resting pupil offset
    public float MouthX;
    public int BrimSide;         // -1 left, 1 right
}
```

Agregar un helper `Look Resolve(Look look, bool isPlayer)` que rellene lo que viene vacío con el
hash de hoy y con el mapeo del accesorio viejo (`Tie` → `Extras.Tie`, `Vest` → `Extras.Cardigan`,
`Cap` → `Hat.Cap`, etc.). Así un peatón sin ficha se ve como hoy, pero con la cabeza y los brazos
nuevos.

### Paso 2. Fichas del elenco

Un archivo nuevo, `MapBuilder.Cast.cs`, con un diccionario de nombre de NPC a `Look`, con los
valores de la tabla de la sección 4:

```csharp
private static readonly Dictionary<string, Look> Cast = new Dictionary<string, Look>
{
    ["DonRafa"] = new Look { Skin = "#5B6E8C", SkinTone = "#8A5A3C", HairColor = "#2A1F1A", Hair = HairStyle.Buzz,
        Pants = "#3E4A63", Hat = Hat.Cap, HatColor = "#C0392B", BrimSide = -1, Face = Expression.Frown,
        Width = 1.14f, Height = 0.94f, Head = 1.05f, SquareJaw = true, Mustache = true,
        Wear = Extras.Overall, WearColor = "#3E5A86" },
    // …
};
```

En `BuildNpc` y `BuildPedestrian`, si `Cast` tiene el `name`, usar esa ficha en lugar del `look`
que llega. Así no hay que tocar las 40 llamadas, y los NPC que comparten nombre (`Oficial`,
`Doctor1..`) reciben la misma ficha. Para el jugador, cambiar `PlayerLook`.

### Paso 3. Cuerpo y brazos

En `MapBuilder.People.cs`:
1. `BuildPersonVisual`: aplicar `Width`, `Height` y `Head` a todas las posiciones y tamaños
   (sección 3). Quitar `Belt`, `Buckle` y `Button`.
2. Crear `torso` **antes** que los brazos y colgar los brazos de `torso`, no de `visual`. Así
   siguen la respiración y la inclinación de `PlayerAnimator`.
3. Reescribir `Arm()` con dos pivotes:
   ```csharp
   // Shoulder pivot inside the torso; the sleeve sits behind the torso so the joint is hidden.
   private static (Transform shoulder, Transform elbow) Arm(Transform torso, string name, int side, Look look, Color shirt, Color skin)
   ```
   - Pivote del hombro en `(side * 0.235f * W, 0.15f)` relativo al torso, con
     `localRotation = Z(side * 22f)`.
   - Manga: `roundedSmall` de (0.13, 0.2·Hh + 0.08), centrada a media altura del brazo, orden
     `OrderPlayer + 4`, con contorno.
   - Pivote del codo al final del brazo, con `localRotation = Z(-side * 14f)`.
   - Antebrazo (0.10, 0.2·Hh + 0.06), orden `OrderPlayer + 3`, color piel o camisa según
     `LongSleeve`. Mano (0.12) al final, orden `OrderPlayer + 5`.
   - Si no es manga larga, borde de manga en el codo.
4. Falda opcional y color de zapatos por personaje en `Leg()`.

### Paso 4. Animación de brazos

En `PlayerAnimator.cs` y `PersonRig`:
1. Agregar `ElbowL` y `ElbowR` a `PersonRig`. Guardar la rotación de reposo de hombros y codos en
   `Awake` (igual que `legLRest`).
2. En `AnimateRig`, sumar el ángulo animado al de reposo en vez de reemplazarlo:
   `rig.ArmL.localRotation = Z(armLRest + armLeft)`.
3. Al saludar, doblar el codo de la mano levantada (por ejemplo, 40° extra) para que el saludo no
   sea un palo. Al hablar, mover un poco el codo además del hombro.
4. Si se agrega una función nueva a `PersonMotion` (por ejemplo `ElbowAngle`), agregar sus tests
   en `PersonMotionTests.cs`.

### Paso 5. Cabeza

Reescribir `BuildHead` y `Eye` con la tabla de la sección 3:
1. Cara con mandíbula, orejas más abajo, nariz.
2. Ojos nuevos. El transform `Pupil` tiene que seguir existiendo (lo mueve `PlayerAnimator`) y su
   posición de reposo incluye `LookAside`.
3. `Lid` (el que parpadea) se mantiene: un círculo color piel del tamaño del ojo, con
   `localScale.y = 0` en reposo. El párpado caído es una forma **aparte** y fija.
4. Cejas con rotación según `Face`.
5. Boca según `Face`, dentro del transform `Mouth` que ya anima `PlayerAnimator`. Quitar la lengua.
6. Lentes, bigote, chiva y cachetes según la ficha.

`Shape(...)` devuelve el `SpriteRenderer`; para rotar una forma:
`Shape(...).transform.localRotation = Quaternion.Euler(0f, 0f, grados);`. Conviene un helper
`ShapeRot` y un `Part2Rot` (contorno más forma, los dos rotados).

### Paso 6. Pelo y sombreros

1. Reescribir `DrawHair`/`DrawHairStyle` con los estilos de la sección 3: cúpula detrás de la cabeza
   más el flequillo.
2. Reescribir `DrawHat` con los 8 sombreros.
3. **Corregir el error del moño:** si hay sombrero, no dibujar la cúpula, el moño ni las puntas;
   solo los lados y lo que cae atrás (cola, pelo largo).
4. `DrawTorsoAccessory` pasa a dibujar los `Extras` de la ficha.

### Paso 7. Regenerar y revisar

1. Abrir Unity y correr `ColonQuest > Build All Scenes`.
2. Correr los tests de EditMode y PlayMode. Hoy pasan 146 y 51; tienen que seguir pasando.
3. **Mirarlo en pantalla** (esto no lo cubre ningún test):
   - Caminar con el jugador: que los brazos se balanceen sin despegarse y el codo no se vea raro.
   - Acercarse a un NPC: saludo con el codo doblado, ojos que siguen al jugador, parpadeo.
   - Hablar con el vendedor: la boca de media luna se mueve y no desaparece en reposo.
   - Que la sombra, el orden por altura (`YSort`) y las colisiones sigan bien con los personajes más
     anchos o más altos.
   - Revisar el pueblo, la tienda, el banco y los interiores con el elenco completo.
4. Tomar capturas nuevas para `docs/art/` y actualizar `docs/unity-alpha-status.md`.
5. Commitear las escenas regeneradas por separado
   (`chore(unity): regenerate scenes with the v2 cast [SCRUM-58]`), porque los diffs son grandes.

### Paso 8 (opcional). Retratos que combinen

Los paneles usan `UiKit.Portrait`, que carga `Resources/Portraits/<clave>` y, si no hay imagen,
dibuja una tarjeta. Dos caminos:

- **A (recomendado):** generar el retrato con el mismo esqueleto de Unity, en busto y grande, desde
  un script de editor que guarde un PNG por personaje en `Resources/Portraits/`. Combina perfecto
  con el mapa y no cuesta nada.
- **B (Muse Image, cuenta para Best Creative Build):** en `server/app/images/service.py`, cambiar
  `STYLE_PROMPT` a algo como *"flat vector cartoon, thick dark navy outline, simple cel shading,
  muted Costa Rican palette, bust portrait, plain background"* y agregar un `CHARACTER_SHEETS` con
  la descripción de cada ficha de la sección 4. Luego regenerar con
  `uv run python scripts/pregenerate_images.py --only npc_kevin_vendedor ... --force` contra la base
  de producción y elegir a mano.

---

## 6. Riesgos y cosas a vigilar

- **Tiempo.** La entrega es el 2 de noviembre. Si no da para todo, el orden es: brazos (pasos 3 y
  4) → cabeza y pelo (pasos 5 y 6) para Kevin, el prestamista y Brayan, que salen en la
  negociación y en el clip para X → el resto del elenco.
- **Rendimiento en WebGL.** En el mockup, un personaje de hoy tiene unas 62 formas y uno de la v2
  entre 51 y 68 (unas 60 en promedio), así que no debería pesar más. Igual hay que medir los FPS en
  el navegador, porque el pueblo ya tiene miles de `SpriteRenderer`. Si baja, quitar formas de los
  peatones (lentes, cachetes, detalles de torso).
- **Escenas enormes.** `Town.unity` ya pesa unos 21 MB de texto. Coordinar con Jefferson para no
  regenerar escenas al mismo tiempo y evitar conflictos.
- **Las imágenes son una aproximación.** Los números se copian de `mockup_v2.py`, pero el
  resultado final se juzga en Unity.

## 7. Lista de verificación

- [ ] `Look` ampliado; las llamadas viejas compilan sin cambios
- [ ] `MapBuilder.Cast.cs` con las 20 fichas
- [ ] Brazos con codo, hijos del torso, detrás del torso
- [ ] `PersonRig` con `ElbowL/R`; `PlayerAnimator` usa rotaciones de reposo
- [ ] Cabeza nueva con expresiones; `Pupil`, `Lid` y `Mouth` siguen animándose
- [ ] Pelo en cúpula, sombreros nuevos, moño corregido
- [ ] Faja, hebilla y botones por defecto eliminados
- [ ] Escenas regeneradas y commiteadas aparte
- [ ] EditMode y PlayMode en verde
- [ ] Revisado en pantalla: caminar, saludar, hablar, interiores
- [ ] FPS medido en el build WebGL
- [ ] Capturas y `docs/unity-alpha-status.md` actualizados

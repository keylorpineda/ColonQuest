# Unity client: alpha status

Last updated 28 October 2026. This is what the Unity side does today, how it was checked and what is
still open. Everything under "Verified" was run; everything under "Not verified" was not.

## The main menu

The game opens on the `Menu` scene, the first one in Build Settings. It is built from code like the rest of the
interface (`MainMenu`, `UiKit`): a cartoon sky with drifting clouds and bobbing coins, the title and the buttons.

| Button | What it does |
| --- | --- |
| Modo historia | Starts a new life (`GameSession.StartNewGame`, or the first run when no session exists yet) and loads `Town` |
| Jugar online | Only shows "Disponible en la versión completa" and what the online mode does and does not send; nothing else happens |
| Ajustes | Sound on or off and the volume in steps of 10 %. Applied to `AudioDirector` at once and kept in PlayerPrefs (`GameSettings`) |
| Créditos | DoWhile Team (Keylor and Jefferson, the same credit) and the Meta tools the client uses: Muse Wonder, Muse Spark, Muse Voice Transcribe |
| Salir | `Application.Quit`. Not shown in the browser build |

The menu is drawn in layers that move a little with the mouse; every button grows and lights its border under the cursor and taps when pressed. The title, loading and credit lines show `BuildInfo.Label` ("Alpha 0.1.16") and "Design by DoWhile Team". Starting a story and coming back to the menu go through `LoadingScreen`.

In the town, Esc opens the pause window (not while the game master is answering, a window is open or the phone
is up): Reanudar, Ajustes and Volver al menú. Time stops while it is up. Going back to the menu takes two
presses, because the run is lost. The town overlay hides itself in the menu scene.

Pure logic lives in `GameSettings` (World), `MenuEntries` and `MenuMotion` (Game) and has EditMode tests; the
flow has PlayMode tests in `MenuPlayTests.cs` (`MenuPlayTests` and `PausePlayTests`).

## Places and things that happen to the player

| Place or event | What it does and what it teaches |
| --- | --- |
| Car agency (`Agencia`, north avenue) | A showroom with three cars and a price tag each, two salesmen's desks, a glass financing office, a waiting room with free coffee and a workshop at the back. Don Chepe welcomes the player inside and the car negotiation opens at his desk. Teaches reading the conditions before signing |
| Neighbor's house (`Vecino`, the house next to the rental sign) | Not the player's house. The owner counts down (9 s) and, if the player is still inside, calls the police: an officer comes in and goes to the player. A fine exists only when an officer reaches the player and stops them (no officer, no fine; leaving before he arrives costs nothing). Teaches that a fine costs money and time |
| Police station (`Carcel`, east end of the avenue) | Counter, judge and four cells. After an arrest: a fine (cash, then card), two days of the month lost and a lower mood |
| Street crosswalks (plaza, south street, east street) | Each has a pedestrian light (9 s green, 11 s red) and a police officer standing nearby. Crossing on red only raises the offense; a fine (illustrative, in `MarketAssumptions`) exists only when the officer reaches the player. No jail and no lost days. Teaches that a small fine is still money |
| Real estate agency (`Inmobiliaria`) and the farm (`Finca`) | Both have an inside now. The agency has a wall of photographs and an agent who talks (a face and typed text); the farm has stalls, hay and Don Beto. The windows are the ones the stations used to open outside. A house with a sign is not sold at its door: the player thinks of going to the agency |
| Every other house | Enterable. They share the neighbor's interior, and the door the player came in by is the one they leave through (`Door.RememberedReturn`) |
| Cell and bail | An arrest is a real-time term in the cell (5, 10 or 15 minutes by how many arrests so far). The door stays locked; the bail (each minute left costs `BailPerMinute`) opens it. Pure rules in `JailSentence` |
| Swimsuit and the sea | The wardrobe at home sells a swimsuit; with it, the beach gives one swim a month that lifts the mood. Without it the water only brings a thought |
| Car hit and arrest | The hit throws the player, spins and leaves stars over them (`HitAnimation`); the arrest flashes the police lights with a siren (`ScreenEffects`) |
| Subtitles and thoughts | Everyday objects and people speak as a line at the bottom (`SubtitleBar`); the player's own thoughts (map edge, water, houses on the market, a locked door) use it too |
| Hospital (`Hospital` and `HospitalArriba`, north avenue between the car agency and the river) | Two floors: reception and emergency ward below, consulting rooms and lab above. A car that runs the player over (once a month at most) sends them here: a bill, one day lost and a lower mood. The emergency fund pays first, then cash, then the card |
| Health insurance | Taken out at the hospital reception, generic (no institution is named). A monthly premium; with it the patient pays a share of the bill instead of all of it |
| Collectors' car | A black car with tinted windows and loud music patrols the main street now and then. When the player is in arrears (an informal loan, or a card at 90 % of its limit) and ends up in front of it, it stops and offers three choices: pay, negotiate (half now, the rest as a new informal loan with a surcharge) or ignore (late fee and a lower mood) |
| Car glass | Every other car has see-through glass: a driver shows in a moving car and the seat is empty in a parked one |

All money here moves in `FinanceEngine` (`FinanceEngine.Incidents.cs`) and `GameLoop`, in whole colones. Every amount and
percentage is in `MarketAssumptions.cs`, marked as illustrative: check them against a real source (courts, the health
insurer, the financial regulator) before stating any of them as fact.

## What plays

One run is three months. Each month has three tasks, then the bank closes the month.

| Place | What happens |
| --- | --- |
| Garage, grocery, lender, pyramid, coop, soda | One event from the game master, or a local one when the backend fails or takes more than 8 s |
| Store | Negotiation with the seller (text). Buying adds the installment with insurance and charges the opening commission |
| Bank | Closes the month: salary, installments, card cycle, interest, living expenses |
| Home | Account statement |
| Real estate agency (north district) and the house signs | Four houses to rent or buy. Rent is a 12 month installment with a deposit; buying needs a down payment, closing costs and a mortgage at the longest term the installment contract allows (120 months). The window shows the real total and the share of the salary. Not a task of the month |
| Car agency (north district) | Negotiation with the seller about a car with a down payment (`prima`). The seller is the local one: the offers are client only and never reach the backend. Buying charges the first marchamo; fuel and upkeep are charged every month. Not a task |
| Farm (east district) | Coffee or cattle. The stake leaves the cash now and does not come back during the run. The end of the run shows a projection at real time with a bad, a normal and a good scenario. Not a task |
| ATMs | Account balance |
| Everyday objects and townspeople | A line of dialogue, rotating; they never move money |

The store rotates four offers (phone, TV, fridge, motorbike). The seller keeps a hidden sheet with the true
figures; the player only learns them by asking. Asking about total cost, hidden fees, interest, term, cash
price and insurance feeds the `keyQuestionsAsked` impact metric, and the backend stores every turn.

If the backend answers, its seller replies. If it does not, `LocalSeller` answers with the same scripted
rules and the same figures (`SellerOffer` mirrors `server/app/negotiation/seller_sheet.py`).

## The districts

The playable area is about twice the original town. The streets of the town run on to the east, a dirt path
leads from the first street to a new avenue in the north, and both districts end in rock faces like the rest of
the map. Scene sizes and positions are in `MapLayout`; the districts are built by `MapBuilder.Districts*.cs`.

All prices, rates and percentages of homes, cars and the farm are in one file, `MarketAssumptions.cs`. They are
illustrative and must be checked against a real source before any of them is stated as a fact; the game shows
them with a warning. Money for these purchases moves only in `FinanceEngine` (`Holdings`, `FinanceEngine.Housing`,
`FinanceEngine.Vehicle`, `FinanceEngine.Farm`).

## The look

- Characters are vector people with animated parts, not pixel art.
- The town has about 100 visual layers, three interiors with their own layers, parallax and ambient motion.
- Interiors follow real floor plans: the house has a bedroom, bathroom, kitchen and living room behind
  walls with doorways; the bank has four numbered windows, a closed staff side with the vault, a queue,
  waiting chairs, offices and ATMs; the store sells exactly the four offers of the seller.
- Town buildings get details by use (homes: water tank, flower boxes, door number, no shop sign; shops
  and offices: cameras where money changes hands). Five homes can be reached; two carry
  "SE VENDE" and "SE ALQUILA" signs, ready for renting, buying and mortgages later.
- Stations are invisible zones with an "[E]" prompt: no rings or glow. SINPE is not a place; it will be a
  phone alert.
- Also: soft ground shadows under the buildings, a fine tiling grain over the colors and a wash of light
  that goes from warm morning to blue dusk as the run advances (`DayCycle`).

## Verified

- EditMode 146 of 146 and PlayMode 51 of 51 pass (28 October, branch `feat/SCRUM-48-voice-on-tip`).
- Windows build succeeds with 0 errors, 91 MB, and starts without exceptions; with the backend off it
  plays offline on the local events and seller.
- Scenes rebuild from code with `Colon Quest > Build All Scenes`.

## Not verified

- A full month played by a person on the Windows build, and the north and east districts on screen (the scenes are generated and the tests pass, nobody has judged them visually).

- Nobody has looked at the new lighting, shadows or the negotiation window on screen. Batch captures hang,
  so a person has to open `Assets/Scenes/Town.unity` and judge them. The amounts are constants in
  `MapBuilder.Realism.cs` and `DayCycle.cs`.
- The `₡` sign in the built-in UI font and the text layout of the windows.
- Everything of the last round (menu, loading screen, subtitles, interiors, cell, hit and arrest animations, step sounds, the system fonts, the backend images and the year postcard): written without compiling or running anything.
- The pictures: the portraits (`npc_don_rafa_mecanico`, `npc_kevin_vendedor`, `npc_dona_marta_vendedora`, `npc_prestamista_gota_a_gota`, `npc_amigo_piramide`), the scenes of the events (`imageKey`) and the three postcards (`card_year_*`, chosen by the debt at the end, behind the end window) are asked for with the names that `server/scripts/pregenerate_images.py` produces. Nobody has seen them on screen.
- Builds now point to `https://colon-quest-api.onrender.com/api`; the editor and the tests keep the local backend. The browser build needs its host in `ALLOWED_ORIGINS`.
- Frame rate in a browser. The town has thousands of sprite renderers; the download is small but the draw
  cost is unmeasured.
- The deployed backend URL. `ApiConfig` still points to `http://localhost:8000/api`.
- Voice in the browser. The `.jslib` recorder and the upload exist, but nobody has tried them in a real browser with a microphone over HTTPS.
- Pregenerated portraits: windows draw a colored face until a picture named like the character (`don_rafa.png`) is placed in `Resources/Portraits`.
- The loading indicator.

## Commands

```bash
# builds go outside the repository
Unity.exe -batchmode -projectPath unity -executeMethod ColonQuest.EditorTools.BuildScript.WebGL -buildPath <folder>
Unity.exe -batchmode -projectPath unity -executeMethod ColonQuest.EditorTools.BuildScript.Windows -buildPath <folder>
```

## Risks

- A browser build with this many renderers may run slowly on weak laptops. First fix: a sprite atlas and
  fewer decorative layers (motes, leaves, clouds).
- `Town.unity` is 21 MB of text. It is generated, but it makes diffs large.
- Microphone access needs HTTPS in the browser; text input stays as the fallback.

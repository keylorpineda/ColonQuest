# Unity client: alpha status

Last updated 28 October 2026. This is what the Unity side does today, how it was checked and what is
still open. Everything under "Verified" was run; everything under "Not verified" was not.

## What plays

One run is three months. Each month has three tasks, then the bank closes the month.

| Place | What happens |
| --- | --- |
| Garage, grocery, lender, pyramid, coop, soda | One event from the game master, or a local one when the backend fails or takes more than 8 s |
| Store | Negotiation with the seller (text). Buying adds the installment with insurance and charges the opening commission |
| Bank | Closes the month: salary, installments, card cycle, interest, living expenses |
| Home | Account statement |
| ATMs | Account balance |
| Everyday objects and townspeople | A line of dialogue, rotating; they never move money |

The store rotates four offers (phone, TV, fridge, motorbike). The seller keeps a hidden sheet with the true
figures; the player only learns them by asking. Asking about total cost, hidden fees, interest, term, cash
price and insurance feeds the `keyQuestionsAsked` impact metric, and the backend stores every turn.

If the backend answers, its seller replies. If it does not, `LocalSeller` answers with the same scripted
rules and the same figures (`SellerOffer` mirrors `server/app/negotiation/seller_sheet.py`).

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

- A full month played by a person on the Windows build.

- Nobody has looked at the new lighting, shadows or the negotiation window on screen. Batch captures hang,
  so a person has to open `Assets/Scenes/Town.unity` and judge them. The amounts are constants in
  `MapBuilder.Realism.cs` and `DayCycle.cs`.
- The `₡` sign in the built-in UI font and the text layout of the windows.
- Frame rate in a browser. The town has thousands of sprite renderers; the download is small but the draw
  cost is unmeasured.
- The deployed backend URL. `ApiConfig` still points to `http://localhost:8000/api`.
- Voice with a real microphone and the real backend. The recorder, `.jslib` and upload are written and
  covered by tests, nothing more.
- The phone (key P) and the hip-line fix live in `feat/SCRUM-58-phone-and-people`, not merged yet.
- Portraits, sound and the loading indicator.

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

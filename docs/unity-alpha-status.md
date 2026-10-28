# Unity client: alpha status

Last updated 28 October 2026. This is what the Unity side does today, how it was checked and what is
still open. Everything under "Verified" was run; everything under "Not verified" was not.

## What plays

One run is three months. Each month has three tasks, then the bank closes the month.

| Place | What happens |
| --- | --- |
| Garage, grocery, lender, pyramid, kiosk, coop, soda | One event from the game master, or a local one when the backend fails or takes more than 8 s |
| Store | Negotiation with the seller (text). Buying adds the installment with insurance and charges the opening commission |
| Bank | Closes the month: salary, installments, card cycle, interest, living expenses |
| Home | Account statement |
| Real estate agency (north district) and the house signs | Four houses to rent or buy. Rent is a 12 month installment with a deposit; buying needs a down payment, closing costs and a mortgage at the longest term the installment contract allows (120 months). The window shows the real total and the share of the salary. Not a task of the month |
| Car agency (north district) | Negotiation with the seller about a car with a down payment (`prima`). The seller is the local one: the offers are client only and never reach the backend. Buying charges the first marchamo; fuel and upkeep are charged every month. Not a task |
| Farm (east district) | Coffee or cattle. The stake leaves the cash now and does not come back during the run. The end of the run shows a projection at real time with a bad, a normal and a good scenario. Not a task |

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
  and offices: roof AC units, cameras where money changes hands). Five homes can be reached; two carry
  "SE VENDE" and "SE ALQUILA" signs, ready for renting, buying and mortgages later.
- Also: cast shadows to the lower right of every building, a fine tiling grain over the
  colors and a wash of light that goes from warm morning to blue dusk as the run advances (`DayCycle`).

## Verified

- EditMode 124 of 124 and PlayMode 45 of 45 pass (28 October).
- WebGL build succeeds with 0 errors in about 6 minutes. Output is 9.9 MB (wasm 7.7 MB, data 2.1 MB), gzip
  with the fallback loader, so any static host works.
- Scenes rebuild from code with `Colon Quest > Build All Scenes`.

## Not verified

- The north and east districts: the code was written without compiling or running anything, and the scenes
  have not been regenerated yet. Nobody has looked at the new windows (agencies, farm, end report) or at the
  districts on screen.
- Nobody has looked at the new lighting, shadows or the negotiation window on screen. Batch captures hang,
  so a person has to open `Assets/Scenes/Town.unity` and judge them. The amounts are constants in
  `MapBuilder.Realism.cs` and `DayCycle.cs`.
- The `₡` sign in the built-in UI font and the text layout of the windows.
- Frame rate in a browser. The town has thousands of sprite renderers; the download is small but the draw
  cost is unmeasured.
- The deployed backend URL. `ApiConfig` still points to `http://localhost:8000/api`.
- Voice. The `.jslib` recorder and the multipart upload to `/negotiation/voice` are not written yet.
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

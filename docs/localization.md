# Localization: Spanish and English

The game text exists in Spanish (Costa Rica) and English. The player changes it in Ajustes (Settings), the button
"Idioma: Español" / "Language: English". The choice is kept with the other settings (`GameSettings`, text form
`1;1.00;en`; Spanish adds nothing).

## How it works

- `Loc.T("texto en español")` returns the English text when the language is English and the Spanish text otherwise.
  The Spanish text is the key. A text with no entry stays in Spanish: a missing translation never breaks the game.
- `Loc.F("Mes {0} de {1}", a, b)` is for texts with holes: it translates the template and then fills the holes.
  The `{n}` holes must be the same in both languages (a test checks it).
- Every label made with `UiKit.Label` or `UiKit.MakeButton` is translated when it is created and writes itself again
  when the language changes (`LocalizedText`). Labels the code fills in later start empty and are left alone.
- All English text is in `Scripts/World/Translations.cs`.

## Glossary: Costa Rican words in colloquial English

| Spanish | English | Note |
| --- | --- | --- |
| plata | money, cash | colloquial, never "silver" |
| mae | dude | |
| cuota | installment | "Cuotas al mes" is "Installments / month" |
| prima | down payment | |
| marchamo | marchamo (the yearly car registration fee) | explain it the first time it appears |
| RTV | RTV (mandatory vehicle inspection) | |
| SINPE | SINPE (instant bank transfer) | |
| aguinaldo | aguinaldo (year-end bonus) | |
| pulpería | corner store (pulpería) | |
| soda | diner (soda) | a small local restaurant |
| gota a gota | loan sharks | "Deuda con prestamista" is "Loan-shark debt" |
| tarjeta | card | |
| ánimo | mood | |
| cobradores | collectors | |

Rules: no emojis and no long dashes; colones keep the ₡ sign and the thousands separated by spaces; names of people and
places (Don Rafa, El Muelle) stay as they are.

## What is translated today

The main menu and its credits, the settings, the pause window, the HUD, the prompts and station names, the result
window, the health insurance desk, the collectors and the trespass warning, the events of the local fallback catalog
(title, story and choices), the month report and the end screen, the statement, the phone (SINPE, bank, search,
calculator, casino), the seller of the store and of the car agency (the local seller also understands questions typed
in English), the house, car and farm windows with the end report of what was bought, and the lines of the everyday
objects. A test (`TranslationCoverageTests`) fails when one of those data driven texts has no English entry.

## What is not translated yet

- Text that comes from the backend (the game master events, the seller replies and the final summary when the server
  answers). The backend answers in Spanish and `server/` is not touched from here, so it needs its own translation step.
- The letters painted on signs inside the scenes (for example "SE VENDE", "BANCO DEL PUEBLO"): they are part of the
  generated scenes, not labels.
- The names of installments that the finance engine keeps in its state ("Alquiler: ...", "Hipoteca: ...", "Carro: ...")
  have entries for the current houses and cars only; a new house or car needs its own entry.

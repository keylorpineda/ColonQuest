using System.Collections;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>The game master station: a situation goes to the model, an event comes back, and the choice goes through the engine.</summary>
    public sealed partial class DemoLab
    {
        private const float ResultTop = -300f;

        private const int TriesPerSession = 7;

        private ApiService gmService;
        private int gmTries;
        private int gmScenario = -1;
        private GameEvent gmEvent;
        private string gmOrigin = string.Empty;
        private string gmError;
        private PlayerState gmBefore;
        private PlayerState gmAfter;
        private string gmRejected;

        private void DrawGameMaster()
        {
            DrawHeading("Game master", "Muse Spark", "Le mandamos el estado financiero de un jugador, como en el juego. El modelo decide qué le pasa este mes y propone las opciones; las cuentas no son cosa suya.");

            float width = (Width - 36f) / 4f;
            for (int i = 0; i < DemoScenarios.All.Count; i++)
            {
                int scenario = i;
                Action(DemoScenarios.All[i].Name, i == gmScenario ? Beak : GameUiColors.Blue, new Vector2(width, 72f), new Vector2(Left + i * (width + 12f), -170f), () => TryScenario(scenario));
            }

            Note(gmScenario >= 0 ? DemoScenarios.All[gmScenario].Summary : "Elegí una situación para empezar.", 24, GameUiColors.Muted, new Vector2(Width, 36f), new Vector2(Left, -250f));

            if (Busy)
            {
                DrawWait(new Vector2(Width, 90f), new Vector2(Left, ResultTop - 120f));
                return;
            }

            if (!string.IsNullOrEmpty(gmError))
            {
                Note(gmError, 28, GameUiColors.Loss, new Vector2(Width, 90f), new Vector2(Left, ResultTop - 120f), TextAnchor.UpperCenter);
                return;
            }

            if (gmEvent != null)
            {
                DrawEvent();
            }
        }

        public void TryScenario(int scenario)
        {
            if (!Busy)
            {
                StartCoroutine(RunScenario(scenario));
            }
        }

        private IEnumerator RunScenario(int scenario)
        {
            gmScenario = scenario;
            gmEvent = null;
            gmAfter = null;
            gmRejected = null;
            gmError = null;
            gmBefore = DemoScenarios.All[scenario].Build();
            BeginWait("Muse Spark está leyendo la situación");
            Redraw();

            // The backend allows 40 calls per session and an event can take five: a judge who keeps trying still gets the model.
            if (++gmTries % TriesPerSession == 0)
            {
                gmService = NewService();
            }

            bool connected = false;
            yield return EnsureSession(gmService, null, ok => connected = ok);
            if (!connected)
            {
                gmError = NoServer;
                EndWait();
                Redraw();
                yield break;
            }

            ApiResult<GameEvent> result = null;
            yield return gmService.RequestEvent(gmBefore, r => result = r);
            gmEvent = result.Value;
            gmOrigin = string.IsNullOrEmpty(result.Origin) ? "local" : result.Origin;
            EndWait();
            Redraw();
            yield return RefreshEvidence();
        }

        private void DrawEvent()
        {
            float y = ResultTop;
            if (!string.IsNullOrEmpty(gmEvent.ImageKey))
            {
                Picture(workspace, gmEvent.ImageKey, new Vector2(340f, 226f), TopLeft, new Vector2(Left, y));
            }

            float x = Left + 356f;
            float textWidth = Width - 356f;
            var title = Plain(Loc.T(gmEvent.Title), 34, UiKit.Ink, new Vector2(textWidth, 46f), new Vector2(x, y), TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            OriginBadge(gmOrigin, new Vector2(textWidth, 44f), new Vector2(x, y - 50f));
            Plain(Loc.T(gmEvent.Narrative), 24, UiKit.Ink, new Vector2(textWidth, 124f), new Vector2(x, y - 102f));

            y -= 238f;
            if (gmAfter != null || gmRejected != null)
            {
                DrawOutcome(y);
                return;
            }

            Note("Cada opción la propuso el modelo. Elegí una: la valida y la aplica el motor del juego.", 22, GameUiColors.Muted, new Vector2(Width, 32f), new Vector2(Left, y));
            y -= 36f;
            foreach (var choice in gmEvent.Choices)
            {
                string id = choice.Id;
                var button = Action(choice.Label, GameUiColors.Yellow, new Vector2(Width, 76f), new Vector2(Left, y), () => Pick(id));
                var label = button.transform.Find("Body/Text") as RectTransform;
                label.anchoredPosition = new Vector2(0f, 12f);
                label.sizeDelta = new Vector2(Width - 40f, 40f);
                UiKit.PlainLabel(button.transform.Find("Body"), "Effects", DemoReadout.DescribeAll(choice), 19, GameUiColors.Muted, new Vector2(Width - 40f, 26f), Center, new Vector2(0f, -18f), TextAnchor.MiddleCenter);
                y -= 82f;
            }
        }

        /// <summary>The real engine of the game applies the choice, or rejects it whole if an amount is out of range.</summary>
        private void Pick(string choiceId)
        {
            var after = gmBefore.Clone();
            try
            {
                FinanceEngine.ApplyChoice(after, gmEvent, choiceId);
                gmAfter = after;
                if (gmService.HasSession)
                {
                    StartCoroutine(gmService.RecordDecision(gmBefore.Month, gmEvent, choiceId, gmAfter, _ => { }));
                }
            }
            catch (InvalidGameEventEffectException error)
            {
                gmRejected = error.Message;
            }

            Redraw();
        }

        private void DrawOutcome(float y)
        {
            if (gmRejected != null)
            {
                Note("El motor rechazó esta opción, entera y sin tocar la plata:", 26, GameUiColors.Loss, new Vector2(Width, 40f), new Vector2(Left, y));
                Plain(gmRejected, 22, UiKit.Ink, new Vector2(Width, 90f), new Vector2(Left, y - 44f));
                return;
            }

            Note("La IA solo propuso. El motor en C# validó y aplicó, y estas son las cuentas:", 24, GameUiColors.Gain, new Vector2(Width, 36f), new Vector2(Left, y));
            y -= 40f;
            Note("Antes", 22, GameUiColors.Muted, new Vector2(200f, 30f), new Vector2(Left + 420f, y), TextAnchor.MiddleRight);
            Note("Después", 22, GameUiColors.Muted, new Vector2(200f, 30f), new Vector2(Left + 640f, y), TextAnchor.MiddleRight);
            y -= 32f;
            y = Row("Efectivo", gmBefore.Cash, gmAfter.Cash, false, y);
            y = Row("Ahorros", gmBefore.Savings, gmAfter.Savings, false, y);
            y = Row("Saldo de la tarjeta", gmBefore.CreditCard.Balance, gmAfter.CreditCard.Balance, true, y);
            y = Row("Cuotas por pagar", Installments(gmBefore), Installments(gmAfter), true, y);
            y = Row("Préstamos informales", Loans(gmBefore), Loans(gmAfter), true, y);

            Note("Bienestar", 25, UiKit.Ink, new Vector2(400f, 36f), new Vector2(Left, y), TextAnchor.MiddleLeft);
            Plain(gmBefore.Wellbeing.ToString(), 25, UiKit.Ink, new Vector2(200f, 36f), new Vector2(Left + 420f, y), TextAnchor.MiddleRight);
            Plain(gmAfter.Wellbeing.ToString(), 25, Delta(gmAfter.Wellbeing - gmBefore.Wellbeing, false), new Vector2(200f, 36f), new Vector2(Left + 640f, y), TextAnchor.MiddleRight);
        }

        private float Row(string name, long before, long after, bool isDebt, float y)
        {
            Note(name, 25, UiKit.Ink, new Vector2(400f, 36f), new Vector2(Left, y), TextAnchor.MiddleLeft);
            Plain(Money.Format(before), 25, UiKit.Ink, new Vector2(200f, 36f), new Vector2(Left + 420f, y), TextAnchor.MiddleRight);
            Plain(Money.Format(after), 25, Delta(after - before, isDebt), new Vector2(200f, 36f), new Vector2(Left + 640f, y), TextAnchor.MiddleRight);
            return y - 38f;
        }

        private static Color Delta(long change, bool isDebt)
        {
            if (change == 0L)
            {
                return UiKit.Ink;
            }

            return change > 0L != isDebt ? GameUiColors.Gain : GameUiColors.Loss;
        }

        private static long Installments(PlayerState state)
        {
            long total = 0L;
            foreach (var installment in state.Installments)
            {
                total += installment.Monthly * installment.MonthsLeft;
            }

            return total;
        }

        private static long Loans(PlayerState state)
        {
            long total = 0L;
            foreach (var loan in state.InformalLoans)
            {
                total += loan.Balance;
            }

            return total;
        }
    }
}

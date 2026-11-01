using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// "The AI tells the story, the engine keeps the accounts", shown instead of said: the game's own engine rejects an
    /// amount out of range, makes the sums of the minimum payment and the installments, and hands out a local event when
    /// the model does not answer. None of this calls a model.
    /// </summary>
    public sealed partial class DemoLab
    {
        private const int MinimumPaymentMonths = 12;

        private readonly HashSet<string> seenFallbacks = new HashSet<string>();
        private string engineVerdict;
        private bool engineAccepted;
        private GameEvent localEvent;

        private void DrawEngine()
        {
            DrawHeading("Motor y controles", "FinanceEngine (C#)", "La IA propone y este motor decide. Cuenta en colones enteros, sin decimales, y revisa todo lo que manda un modelo. También corre en el servidor, antes de que la respuesta llegue al juego.");

            float y = -170f;
            Note("Probá a colarle un monto que se sale de las reglas:", 25, UiKit.Ink, new Vector2(Width, 34f), new Vector2(Left, y));
            y -= 38f;
            float width = (Width - 24f) / 3f;
            Action("Emergencia de ₡85 000", GameUiColors.Green, new Vector2(width, 60f), new Vector2(Left, y), () => TryEffect(ColonQuest.Finance.EventType.Emergency, new Effect(EffectKind.Expense, 85_000L, MoneySource.Cash)));
            Action("Emergencia de ₡9 000 000", GameUiColors.Pink, new Vector2(width, 60f), new Vector2(Left + width + 12f, y), () => TryEffect(ColonQuest.Finance.EventType.Emergency, new Effect(EffectKind.Expense, 9_000_000L, MoneySource.Cash)));
            Action("Cuotas por 200 meses", GameUiColors.Pink, new Vector2(width, 60f), new Vector2(Left + (width + 12f) * 2f, y), () => TryEffect(ColonQuest.Finance.EventType.Offer, new Effect(EffectKind.NewInstallment, 25_000L, null, 200)));
            y -= 66f;
            if (engineVerdict != null)
            {
                var verdict = Plain(engineVerdict, 21, engineAccepted ? GameUiColors.Gain : GameUiColors.Loss, new Vector2(Width, 56f), new Vector2(Left, y));
                verdict.fontStyle = FontStyle.Bold;
            }

            y -= 64f;
            DrawMinimumPayment(y);
            y -= 208f;
            DrawInstallments(y);
            y -= 186f;
            DrawFallback(y);
        }

        private void TryEffect(ColonQuest.Finance.EventType type, Effect effect)
        {
            engineAccepted = EffectLimits.TryValidateEffect(type, effect, out string message);
            engineVerdict = engineAccepted ? Loc.F("Aceptado: {0}", DemoReadout.Describe(effect)) : Loc.F("Rechazado: {0}", message);
            Redraw();
        }

        /// <summary>The real calculation of the card, month by month, drawn as bars.</summary>
        private void DrawMinimumPayment(float y)
        {
            var state = new PlayerState { CreditCard = new CreditCard(180_000L, 300_000L, 3.5m, 10m), Cash = ContractLimits.MaxColones };
            long start = state.CreditCard.Balance;
            long paid = 0L;
            long interest = 0L;
            var balances = new List<long>();
            for (int month = 0; month < MinimumPaymentMonths; month++)
            {
                var cycle = FinanceEngine.PayCreditCardMinimum(state);
                paid += cycle.AmountPaid;
                interest += cycle.InterestCharged;
                balances.Add(state.CreditCard.Balance);
            }

            Plain(Loc.F("Tarjeta de {0} al {1}% mensual, pagando solo el mínimo", Money.Format(start), Loc.Current == GameLanguage.English ? "3.5" : "3,5"), 25, UiKit.Ink, new Vector2(Width, 34f), new Vector2(Left, y));
            y -= 40f;
            const float chartHeight = 150f;
            const float barWidth = 34f;
            for (int i = 0; i < balances.Count; i++)
            {
                float height = Mathf.Max(6f, chartHeight * balances[i] / start);
                UiKit.Fill(workspace, "Bar_" + i, GameUiColors.Loss, new Vector2(barWidth, height), new Vector2(0f, 1f), new Vector2(Left + i * (barWidth + 6f), y - (chartHeight - height)));
            }

            float x = Left + balances.Count * (barWidth + 6f) + 20f;
            float textWidth = Width - (x - Left);
            Plain(Loc.F("En un año pagaste {0}, y {1} fueron solo intereses.", Money.Format(paid), Money.Format(interest)), 23, UiKit.Ink, new Vector2(textWidth, 76f), new Vector2(x, y));
            Plain(Loc.F("Todavía debés {0}.", Money.Format(state.CreditCard.Balance)), 26, GameUiColors.Loss, new Vector2(textWidth, 60f), new Vector2(x, y - 84f)).fontStyle = FontStyle.Bold;
        }

        private void DrawInstallments(float y)
        {
            Note("El costo real de comprar a cuotas", 25, UiKit.Ink, new Vector2(Width, 34f), new Vector2(Left, y));
            y -= 36f;
            foreach (var item in SellerOffer.Catalog)
            {
                Plain(Loc.T(item.Item), 22, UiKit.Ink, new Vector2(400f, 30f), new Vector2(Left, y), TextAnchor.MiddleLeft);
                Plain(Loc.F("{0} de contado", Money.Format(item.CashPrice)), 22, GameUiColors.Muted, new Vector2(270f, 30f), new Vector2(Left + 400f, y), TextAnchor.MiddleRight);
                Plain(Loc.F("{0} en cuotas", Money.Format(item.TotalCost)), 22, GameUiColors.Loss, new Vector2(290f, 30f), new Vector2(Left + 680f, y), TextAnchor.MiddleRight);
                y -= 34f;
            }
        }

        private void DrawFallback(float y)
        {
            Note("Si el modelo no responde, el juego sigue:", 25, UiKit.Ink, new Vector2(560f, 34f), new Vector2(Left, y - 4f), TextAnchor.MiddleLeft);
            Action("Sacar un evento de respaldo", GameUiColors.Yellow, new Vector2(400f, 56f), new Vector2(Left + Width - 400f, y), ShowFallback);
            if (localEvent != null)
            {
                Plain(Loc.T(localEvent.Title) + ": " + Loc.T(localEvent.Narrative), 20, GameUiColors.Muted, new Vector2(Width, 66f), new Vector2(Left, y - 62f));
            }
        }

        private void ShowFallback()
        {
            localEvent = FallbackEvents.Next(DemoScenarios.FirstMonth(), seenFallbacks);
            seenFallbacks.Add(localEvent.EventId);
            Redraw();
        }
    }
}

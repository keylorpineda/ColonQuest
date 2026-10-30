using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>The goals of the run on the phone, and the two moves of the bank app that get the player closer to them.</summary>
    public sealed partial class PhoneUi
    {
        private const long BankStep = 10_000L;

        private static readonly Color GoalsColor = GamePalette.Hex("#D9A400");

        public static string TitleOf(GoalKind kind)
        {
            switch (kind)
            {
                case GoalKind.EmergencyFund: return "Juntar el fondo de emergencia";
                case GoalKind.PayDownCard: return "Bajar la tarjeta a la mitad";
                case GoalKind.Savings: return "Juntar un ahorro";
                default: return "Amueblar tu cuarto";
            }
        }

        private string DetailOf(Goal goal)
        {
            var state = session.State;
            switch (goal.Kind)
            {
                case GoalKind.EmergencyFund:
                    return Loc.F("{0} de {1}", Money.Format(state.EmergencyFund), Money.Format(goal.Target));
                case GoalKind.Savings:
                    return Loc.F("{0} de {1}", Money.Format(state.Savings), Money.Format(goal.Target));
                case GoalKind.PayDownCard:
                    return Loc.F("La tarjeta debe {0}. La meta es {1}.", Money.Format(state.CreditCard.Balance), Money.Format(goal.Target));
                default:
                    int pieces = 0;
                    foreach (var unused in session.Holdings.Furniture)
                    {
                        pieces++;
                    }

                    return Loc.F("{0} de {1} muebles", pieces, goal.Target);
            }
        }

        private void BuildGoals()
        {
            Heading("Metas");
            float y = -66f;
            foreach (var goal in session.Loop.Goals.Goals)
            {
                float progress = GoalBook.Progress(goal, session.State, session.Holdings);
                var card = UiKit.Fill(content, "Goal_" + goal.Kind, Color.white, new Vector2(InnerWidth, 148f), new Vector2(0f, 1f), new Vector2(Margin, y));
                UiKit.Label(card.transform, TitleOf(goal.Kind), 25, UiKit.Ink, new Vector2(InnerWidth - 32f, 36f), new Vector2(0f, 1f), new Vector2(16f, -10f));
                var detail = UiKit.Label(card.transform, goal.Done ? Loc.T("¡Meta cumplida!") : DetailOf(goal), 21, goal.Done ? GameUiColors.Gain : GameUiColors.Muted, new Vector2(InnerWidth - 32f, 32f), new Vector2(0f, 1f), new Vector2(16f, -50f));
                detail.fontStyle = FontStyle.Normal;

                float width = InnerWidth - 32f;
                UiKit.Fill(card.transform, "BarBack", GamePalette.Hex("#E2E5EC"), new Vector2(width, 20f), new Vector2(0f, 0f), new Vector2(16f, 18f));
                var bar = UiKit.Fill(card.transform, "Bar", goal.Done ? BankColor : GoalsColor, new Vector2(Mathf.Max(20f, width * progress), 20f), new Vector2(0f, 0f), new Vector2(16f, 18f));
                bar.rectTransform.sizeDelta = new Vector2(20f, 20f);
                float full = Mathf.Max(20f, width * progress);
                motion.Run(0.1f, 0.45f, t =>
                {
                    if (bar != null)
                    {
                        bar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(20f, full, PhoneMotion.Settle(t)), 20f);
                    }
                });
                y -= 160f;
            }
        }

        /// <summary>The two moves of the bank app, under the numbers.</summary>
        private void AddBankMoves()
        {
            string fund = Loc.F("Apartar {0} al fondo", Money.Format(BankStep));
            string card = Loc.F("Abonar {0} a la tarjeta", Money.Format(BankStep));
            AddButton(fund, GameUiColors.Green, new Vector2(InnerWidth, 56f), new Vector2(0f, -664f), () => { session.PutInFund(BankStep); Show(App.Bank); });
            AddButton(card, GameUiColors.Yellow, new Vector2(InnerWidth, 56f), new Vector2(0f, -728f), () => { session.PayTowardCard(BankStep); Show(App.Bank); });
        }
    }
}

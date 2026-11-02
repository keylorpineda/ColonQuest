using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>The bills of the month on the phone. It only shows them and asks; the finance engine takes the money.</summary>
    public sealed partial class PhoneUi
    {
        private static readonly MoneySource[] BillSources = { MoneySource.Cash, MoneySource.Savings, MoneySource.CreditCard };

        private string billsResult = string.Empty;
        private MoneySource billSource = MoneySource.Cash;

        private static string NameOf(BillKind kind)
        {
            switch (kind)
            {
                case BillKind.Electricity: return "Luz";
                case BillKind.Water: return "Agua";
                case BillKind.Internet: return "Internet";
                default: return "Teléfono";
            }
        }

        private static string SourceName(MoneySource source)
        {
            switch (source)
            {
                case MoneySource.Savings: return "Cuenta";
                case MoneySource.CreditCard: return "Tarjeta";
                default: return "Efectivo";
            }
        }

        /// <summary>What the chosen source has, on the line under the title. Only cash is kept up to date by itself.</summary>
        private void SourceLine(float y)
        {
            var state = session.State;
            switch (billSource)
            {
                case MoneySource.Savings:
                    Label(Loc.F("En la cuenta  {0}", Money.Format(state.Savings)), 30, GameUiColors.Muted, InnerWidth, new Vector2(0f, y));
                    break;
                case MoneySource.CreditCard:
                    Label(Loc.F("Crédito disponible  {0}  ·  {1}% al mes", Money.Format(state.CreditCard.AvailableCredit), state.CreditCard.MonthlyRatePct), 26, GameUiColors.Muted, InnerWidth, new Vector2(0f, y));
                    break;
                default:
                    CashLine(y);
                    break;
            }
        }

        private string NotEnough()
        {
            switch (billSource)
            {
                case MoneySource.Savings: return Loc.T("No te alcanza lo que hay en la cuenta para pagar ese recibo.");
                case MoneySource.CreditCard: return Loc.T("No te alcanza el crédito de la tarjeta para pagar ese recibo.");
                default: return Loc.T("No te alcanza el efectivo para pagar ese recibo.");
            }
        }

        private void BuildBills()
        {
            Heading("Recibos");
            SourceLine(-54f);
            var book = session.Loop.Bills;
            if (book.Due.Count == 0)
            {
                Label(book.ArrivedThisMonth ? "Estás al día. Este mes ya no debés ningún recibo." : "Todavía no hay recibos. Llegan en la última semana del mes.", 26, GameUiColors.Muted, InnerWidth, new Vector2(0f, -110f));
                return;
            }

            float gap = 8f;
            float width = (InnerWidth - 2f * gap) / BillSources.Length;
            for (int i = 0; i < BillSources.Length; i++)
            {
                var source = BillSources[i];
                AddButton(SourceName(source), source == billSource ? GameUiColors.Yellow : Color.white, new Vector2(width, 54f), new Vector2(i * (width + gap), -100f), () =>
                {
                    billSource = source;
                    billsResult = string.Empty;
                    Show(App.Bills);
                });
            }

            float y = -164f;
            foreach (var due in book.Due)
            {
                var bill = due;
                bool cut = book.IsCut(bill.Kind);
                var row = UiKit.Fill(content, "Bill_" + bill.Kind, Color.white, new Vector2(InnerWidth, 104f), new Vector2(0f, 1f), new Vector2(Margin, y));
                UiKit.Label(row.transform, NameOf(bill.Kind), 26, UiKit.Ink, new Vector2(240f, 34f), new Vector2(0f, 1f), new Vector2(16f, -10f));
                string state = cut ? Loc.T("Servicio cortado") : (bill.MonthsLate > 0 ? Loc.T("Atrasado, con recargo") : Loc.T("Vence al cerrar el mes"));
                var note = UiKit.Label(row.transform, state, 20, bill.MonthsLate > 0 ? GameUiColors.Loss : GameUiColors.Muted, new Vector2(290f, 30f), new Vector2(0f, 0f), new Vector2(16f, 10f));
                note.fontStyle = FontStyle.Normal;
                UiKit.Label(row.transform, Money.Format(bill.Amount), 26, UiKit.Ink, new Vector2(150f, 34f), new Vector2(1f, 1f), new Vector2(-16f, -10f), TextAnchor.MiddleRight);
                AddButton("Pagar", GameUiColors.Green, new Vector2(110f, 44f), new Vector2(InnerWidth - 122f, y - 52f), () => Pay(bill.Kind));
                y -= 114f;
            }

            AddButton(Loc.F("Pagar todo  {0}", Money.Format(book.TotalDue)), GameUiColors.Blue, new Vector2(InnerWidth, 60f), new Vector2(0f, y - 4f), PayAll);
            if (billsResult.Length > 0)
            {
                Label(billsResult, 22, GameUiColors.Loss, InnerWidth, new Vector2(0f, y - 72f));
            }
        }

        private void Pay(BillKind kind)
        {
            bool paid = session.PayBill(kind, billSource);
            billsResult = paid ? string.Empty : NotEnough();
            if (paid)
            {
                AudioDirector.Effect(SoundEffectKind.Coin);
            }

            Show(App.Bills);
        }

        private void PayAll()
        {
            int before = session.Loop.Bills.Due.Count;
            int paid = session.PayAllBills(billSource);
            billsResult = paid < before ? Loc.T("No te alcanzó para pagarlos todos.") : string.Empty;
            if (paid > 0)
            {
                AudioDirector.Effect(SoundEffectKind.Coin);
            }

            Show(App.Bills);
        }

        /// <summary>The line on the home screen when something is owed; otherwise the usual SINPE advice.</summary>
        private string HomeNotice()
        {
            var book = session.Loop.Bills;
            return book.Due.Count > 0
                ? Loc.F("Tenés {0} recibos por {1}. Lo que no pagués antes de cerrar el mes lleva recargo.", book.Due.Count, Money.Format(book.TotalDue))
                : PhoneMessages.SinpeNotice(openings);
        }

        /// <summary>A service that was cut: the app only says so. True when it is cut.</summary>
        private bool ShowCut(BillKind kind, string message)
        {
            if (!session.Loop.Bills.IsCut(kind))
            {
                return false;
            }

            Label(message, 26, GameUiColors.Loss, InnerWidth, new Vector2(0f, -70f));
            return true;
        }
    }
}

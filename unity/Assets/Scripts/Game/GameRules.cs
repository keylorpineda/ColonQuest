using System.Globalization;
using System.Text;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>The numbers that shape one run: how long it is and where the player starts.</summary>
    public static class GameRules
    {
        /// <summary>Months in a run. The vertical slice is short on purpose; the engine supports 12.</summary>
        public const int MonthsToPlay = 3;

        /// <summary>Tasks (events) the player does each month before collecting the salary at the bank.</summary>
        public const int TasksPerMonth = 3;

        /// <summary>Rent, food and transport, charged every month.</summary>
        public const long LivingExpenses = 200_000L;

        /// <summary>The starting situation of the contract example: a salary, a card with a balance and a phone in installments.</summary>
        public static PlayerState NewPlayer()
        {
            var state = new PlayerState
            {
                Month = 1,
                SalaryNet = 450_000L,
                Cash = 120_000L,
                Savings = 50_000L,
                EmergencyFund = 0L,
                CreditCard = new CreditCard(180_000L, 300_000L, 3.5m, 10m),
                Wellbeing = 62
            };
            state.Installments.Add(new Installment("Celular", 25_000L, 33));
            return state;
        }

        /// <summary>What the player owes right now on the card and to informal lenders.</summary>
        public static long DebtBalance(PlayerState state)
        {
            long total = state.CreditCard.Balance;
            foreach (var loan in state.InformalLoans)
            {
                total += loan.Balance;
            }

            return total;
        }

        public static long MonthlyInstallments(PlayerState state)
        {
            long total = 0L;
            foreach (var installment in state.Installments)
            {
                if (installment.MonthsLeft > 0)
                {
                    total += installment.Monthly;
                }
            }

            return total;
        }

        /// <summary>Everything owned minus the card, the lenders and the installments still to pay.</summary>
        public static long NetWorth(PlayerState state)
        {
            long installmentsLeft = 0L;
            foreach (var installment in state.Installments)
            {
                installmentsLeft += installment.Monthly * installment.MonthsLeft;
            }

            return state.Cash + state.Savings + state.EmergencyFund - DebtBalance(state) - installmentsLeft;
        }
    }

    /// <summary>Colones the way Costa Rica writes them: ₡85 000.</summary>
    public static class Money
    {
        public const string Symbol = "₡";

        public static string Format(long amount)
        {
            string digits = (amount < 0 ? -amount : amount).ToString(CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                {
                    builder.Append(' ');
                }

                builder.Append(digits[i]);
            }

            return (amount < 0 ? "-" : string.Empty) + Symbol + builder;
        }

        /// <summary>With a plus sign for gains, so a change reads as a change.</summary>
        public static string FormatSigned(long amount)
        {
            return amount > 0 ? "+" + Format(amount) : Format(amount);
        }
    }
}

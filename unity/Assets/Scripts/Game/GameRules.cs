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

        /// <summary>Tasks (events) the player does each month before collecting the salary at the bank: two a week.</summary>
        public const int TasksPerMonth = TasksPerDay * DaysPerMonth;

        /// <summary>Tasks a day allows. A day stands for a week, so this is two errands a week.</summary>
        public const int TasksPerDay = 2;

        /// <summary>Days of play in a month. Each one stands for a week, so the month has four.</summary>
        public const int DaysPerMonth = 4;

        /// <summary>The day starts at this hour and ends at <see cref="DayEndHour"/>, when the player has to sleep.</summary>
        public const int DayStartHour = 6;

        public const int DayEndHour = 22;

        /// <summary>From this hour the evening begins: the town gets darker.</summary>
        public const int NightHour = 18;

        /// <summary>Real seconds for one hour of the game: a day of 16 hours lasts 8 minutes in normal speed.</summary>
        public const float NormalSecondsPerGameHour = 30f;

        /// <summary>Real seconds for one hour in fast speed mode (for demos and judges, ~2.7 min per day).</summary>
        public const float FastSecondsPerGameHour = 10f;

        public const float SecondsPerGameHour = NormalSecondsPerGameHour;

        /// <summary>The shift of the first job: it starts at this hour, with some minutes of grace, and lasts the morning.</summary>
        public const int WorkStartHour = 7;

        public const int WorkGraceMinutes = 15;

        public const int WorkEndHour = 12;

        /// <summary>About how long the shift scene lasts, in real seconds: the clock goes from clocking in to the end of the shift over it.</summary>
        public const float ShiftRealSeconds = 26f;

        /// <summary>A rest in bed during the day moves the clock on by this many minutes, so the free afternoon is not spent waiting.</summary>
        public const float NapMinutes = 120f;

        /// <summary>
        /// The legal minimum for a worker in an unskilled occupation (generic), in force since January 1, 2026: Executive Decree
        /// 45303-MTSS sets ₡12 436.41 per ordinary day, ₡373 092.30 per month. Rounded to whole colones: money here has no cents.
        /// </summary>
        public const long MinimumWageGross = 373_092L;

        /// <summary>Food and transport, charged every month. The rent of the room goes apart (see <see cref="MarketAssumptions.RoomRent"/>).</summary>
        public const long LivingExpenses = 110_000L;

        /// <summary>The starting situation: the minimum wage after its legal deductions, a card with a balance and a phone in installments.</summary>
        public static PlayerState NewPlayer()
        {
            var state = new PlayerState
            {
                Month = 1,
                SalaryNet = FinanceEngine.CalculateNetSalary(MinimumWageGross),
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

        /// <summary>The same, plus what the player bought: an owned home, the resale value of the car and the money in the farm.</summary>
        public static long NetWorth(PlayerState state, Holdings holdings)
        {
            return NetWorth(state) + (holdings != null ? holdings.AssetValue : 0L);
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

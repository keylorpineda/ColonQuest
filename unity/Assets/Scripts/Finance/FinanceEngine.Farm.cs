using System;

namespace ColonQuest.Finance
{
    /// <summary>
    /// What an investment in the farm could be worth when it pays off, in three scenarios. The percentages
    /// come from the caller; this struct only does the arithmetic, in whole colones.
    /// </summary>
    public readonly struct FarmProjection
    {
        public FarmKind Kind { get; }
        public long Invested { get; }

        /// <summary>Months from the purchase until the money comes back (the harvest or the sale of the cattle).</summary>
        public int MonthsToPayoff { get; }

        public int LowPct { get; }
        public int BasePct { get; }
        public int HighPct { get; }
        public long LowValue { get; }
        public long BaseValue { get; }
        public long HighValue { get; }

        public FarmProjection(FarmKind kind, long invested, int monthsToPayoff, int lowPct, int basePct, int highPct, long lowValue, long baseValue, long highValue)
        {
            Kind = kind;
            Invested = invested;
            MonthsToPayoff = monthsToPayoff;
            LowPct = lowPct;
            BasePct = basePct;
            HighPct = highPct;
            LowValue = lowValue;
            BaseValue = baseValue;
            HighValue = highValue;
        }

        /// <summary>Months still to wait after the run, given how long the money has been in.</summary>
        public int MonthsLeftAfter(int monthsHeld)
        {
            return Math.Max(0, MonthsToPayoff - Math.Max(0, monthsHeld));
        }
    }

    public static partial class FinanceEngine
    {
        public const long MinFarmInvestment = 1L;

        public static FarmProjection ProjectFarm(FarmKind kind, long invested, int monthsToPayoff, int lowPct, int basePct, int highPct)
        {
            if (invested <= 0L || invested > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(invested), "Invested amount must be positive and within MaxColones.");
            if (monthsToPayoff < 1 || monthsToPayoff > 120)
                throw new ArgumentOutOfRangeException(nameof(monthsToPayoff), "Months to payoff must be between 1 and 120.");
            if (lowPct < -100 || highPct > 1_000 || lowPct > basePct || basePct > highPct)
                throw new ArgumentOutOfRangeException(nameof(basePct), "Percentages must be ordered low, base, high and stay between -100 and 1000.");

            return new FarmProjection(
                kind,
                invested,
                monthsToPayoff,
                lowPct,
                basePct,
                highPct,
                ValueAfter(invested, lowPct),
                ValueAfter(invested, basePct),
                ValueAfter(invested, highPct));
        }

        private static long ValueAfter(long invested, int pct)
        {
            return ContractLimits.ClampColones(invested + RoundColones(invested * (decimal) pct / 100m));
        }

        /// <summary>
        /// Puts cash into the farm. The money leaves now and cannot be taken back during the run: nothing pays
        /// off until the harvest or the sale. Nothing changes when the cash is not enough.
        /// </summary>
        public static PurchaseResult TryInvestInFarm(PlayerState state, Holdings holdings, FarmKind kind, long amount)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));
            if (amount < MinFarmInvestment || amount > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive and within MaxColones.");

            if (state.Cash < amount)
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);
            TrySpendCash(state, amount);
            holdings.AddFarm(kind, amount, state.Month - 1);
            state.RecordHistory(kind == FarmKind.Coffee ? "invested_coffee" : "invested_cattle");
            return PurchaseResult.Success();
        }
    }
}

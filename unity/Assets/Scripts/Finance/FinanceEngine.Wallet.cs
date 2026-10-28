using System;

namespace ColonQuest.Finance
{
    public static partial class FinanceEngine
    {
        /// <summary>
        /// Takes money out of the player's cash for something that cannot be put on credit, such as a SINPE
        /// transfer or a casino bet. Returns false and changes nothing when the cash is not enough.
        /// </summary>
        public static bool TrySpendCash(PlayerState state, long amountColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (amountColones <= 0L || amountColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amountColones), "Amount must be between 1 and MaxColones.");
            if (state.Cash < amountColones)
                return false;

            state.Cash -= amountColones;
            return true;
        }

        /// <summary>Adds money to the player's cash, never past the contract limit.</summary>
        public static void CreditCash(PlayerState state, long amountColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (amountColones < 0L || amountColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amountColones), "Amount must be between 0 and MaxColones.");

            state.Cash = ContractLimits.ClampColones(state.Cash + amountColones);
        }
    }

    public enum CasinoOutcome
    {
        Lose,
        SmallWin,
        Jackpot
    }

    public readonly struct CasinoResult
    {
        public CasinoOutcome Outcome { get; }

        /// <summary>What the player gains (positive) or loses (negative) on top of the stake.</summary>
        public long Net { get; }

        public CasinoResult(CasinoOutcome outcome, long net)
        {
            Outcome = outcome;
            Net = net;
        }
    }

    /// <summary>
    /// The virtual casino: it pays small wins often and large losses more often still, so the house always
    /// wins in the long run. The roll comes from outside (0 to 99), which keeps this exact and testable.
    /// </summary>
    public static class CasinoGame
    {
        public const long MinStake = 1_000L;
        public const long MaxStake = 50_000L;

        // Rolls 0-3 pay double, 4-37 pay half, the other 62 lose the stake: the player gets back 63 colones
        // on average for every 100 bet.
        private const int JackpotBelow = 4;
        private const int SmallWinBelow = 38;

        public static bool IsValidStake(long stake) => stake >= MinStake && stake <= MaxStake;

        public static CasinoResult Play(long stake, int roll)
        {
            if (!IsValidStake(stake))
                throw new ArgumentOutOfRangeException(nameof(stake), "Stake is outside the casino limits.");
            if (roll < 0 || roll > 99)
                throw new ArgumentOutOfRangeException(nameof(roll), "Roll must be between 0 and 99.");

            if (roll < JackpotBelow)
                return new CasinoResult(CasinoOutcome.Jackpot, stake * 2L);
            if (roll < SmallWinBelow)
                return new CasinoResult(CasinoOutcome.SmallWin, stake / 2L);
            return new CasinoResult(CasinoOutcome.Lose, -stake);
        }

        /// <summary>The share of the money bet that comes back on average, in percent, over all 100 rolls.</summary>
        public static long ReturnPerHundred(long stake)
        {
            long net = 0L;
            for (int roll = 0; roll < 100; roll++)
            {
                net += Play(stake, roll).Net;
            }

            return 100L + net * 100L / (stake * 100L);
        }
    }

    /// <summary>Installment math in whole colones, using decimal so no cent is lost to rounding noise.</summary>
    public static class LoanMath
    {
        /// <summary>The fixed monthly payment of a loan with a monthly rate, rounded to the nearest colon.</summary>
        public static long MonthlyPayment(long principal, decimal monthlyRatePct, int months)
        {
            if (principal <= 0L)
                throw new ArgumentOutOfRangeException(nameof(principal), "Principal must be positive.");
            if (months < 1 || months > ContractLimits.MaxInstallmentMonths)
                throw new ArgumentOutOfRangeException(nameof(months), "Months must be between 1 and the contract limit.");
            if (monthlyRatePct < 0m || monthlyRatePct > 100m)
                throw new ArgumentOutOfRangeException(nameof(monthlyRatePct), "Rate must be between 0 and 100.");

            if (monthlyRatePct == 0m)
                return (long) Math.Round((decimal) principal / months, MidpointRounding.AwayFromZero);

            decimal rate = monthlyRatePct / 100m;
            decimal growth = 1m;
            for (int i = 0; i < months; i++)
            {
                growth *= 1m + rate;
            }

            decimal payment = principal * rate * growth / (growth - 1m);
            return (long) Math.Round(payment, MidpointRounding.AwayFromZero);
        }

        /// <summary>Everything paid by the end: the payment times the months.</summary>
        public static long TotalCost(long principal, decimal monthlyRatePct, int months)
        {
            return MonthlyPayment(principal, monthlyRatePct, months) * months;
        }
    }
}

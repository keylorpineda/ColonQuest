using System;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>
    /// When the collectors come and what they ask for. The numbers are in <see cref="MarketAssumptions"/>;
    /// this only decides, from the player's state, whether they are owed and how much.
    /// </summary>
    public static class CollectionRules
    {
        /// <summary>
        /// The player is in arrears when an informal loan is open (those are never paid on time by themselves) or
        /// when the card is almost at its limit.
        /// </summary>
        public static bool InArrears(PlayerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.InformalLoans.Count > 0)
            {
                return true;
            }

            return state.CreditCard.Limit > 0L
                && state.CreditCard.Balance * 100L >= state.CreditCard.Limit * MarketAssumptions.ArrearsCardUsagePct;
        }

        /// <summary>What the collectors ask for: a share of everything owed, inside the minimum and the maximum.</summary>
        public static long Demand(PlayerState state)
        {
            long owed = FinanceEngine.DebtOwed(state);
            long ask = owed * MarketAssumptions.CollectionDemandPct / 100L;
            return Math.Max(MarketAssumptions.CollectionMinDemand, Math.Min(MarketAssumptions.CollectionMaxDemand, ask));
        }
    }
}

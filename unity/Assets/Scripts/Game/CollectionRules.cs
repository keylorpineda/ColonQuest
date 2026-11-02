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
        /// The collectors work for the informal lenders, so they come only for an open informal loan. A full card costs
        /// interest and leaves no credit, but nobody comes to the door for it.
        /// </summary>
        public static bool InArrears(PlayerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.InformalLoans.Count > 0;
        }

        /// <summary>What the collectors ask for: a share of the informal debt, inside the limits, and never more than is owed.</summary>
        public static long Demand(PlayerState state)
        {
            long owed = FinanceEngine.DebtOwed(state);
            long ask = owed * MarketAssumptions.CollectionDemandPct / 100L;
            long demand = Math.Max(MarketAssumptions.CollectionMinDemand, Math.Min(MarketAssumptions.CollectionMaxDemand, ask));
            return Math.Min(owed, demand);
        }
    }
}

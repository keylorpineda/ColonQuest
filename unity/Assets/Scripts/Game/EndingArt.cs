using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>
    /// Which picture of Muse Image closes a run: the trap it is remembered for and whether it ended in the red. The keys are
    /// those of server/app/images/keys.py. The engine's figures choose it, never a model. Pure, so it can be tested.
    /// </summary>
    public static class EndingArt
    {
        public static string KeyFor(PlayerState state, long netWorth, int dealsAccepted)
        {
            return "end_" + Trap(state, dealsAccepted) + (netWorth < 0L ? "_hard" : "_ok");
        }

        private static string Trap(PlayerState state, int dealsAccepted)
        {
            if (state.InformalLoans.Count > 0 || Took(state, TalkPartner.Lender))
            {
                return "lender";
            }

            if (Took(state, TalkPartner.Pyramid))
            {
                return "pyramid";
            }

            if (dealsAccepted > 0)
            {
                return "installments";
            }

            return state.CreditCard.Balance * 2L >= state.CreditCard.Limit ? "card" : "calm";
        }

        private static bool Took(PlayerState state, TalkPartner partner)
        {
            string offer = DebtTalk.OfferEvent(partner).EventId + ":" + DebtTalk.TakeOffer;
            return state.History.Exists(entry => entry.StartsWith(offer, System.StringComparison.Ordinal));
        }
    }
}

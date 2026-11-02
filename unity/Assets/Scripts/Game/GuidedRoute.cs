using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>How the player says they get to the end of the month, at the start of the guided run.</summary>
    public enum GuidedProfile
    {
        Tight,
        Saver
    }

    /// <summary>
    /// The rules of the guided run: three real months of the game on a short stretch of the map, where only the decisions are
    /// left to the player. Pure, so it can be tested. The money moves in the finance engine, as in any other run.
    /// </summary>
    public static class GuidedRoute
    {
        /// <summary>How long the run waits for the plan of Muse Wonder before going on with the base plan, in seconds.</summary>
        public const float PlanWaitSeconds = 20f;

        /// <summary>The seconds between the loan and the black car: long enough to read what just happened.</summary>
        public const float CollectorsDelaySeconds = 1.5f;

        /// <summary>The starting money of each profile. Both have the same salary: the minimum wage after its deductions.</summary>
        public static void Apply(PlayerState state, GuidedProfile profile)
        {
            if (profile == GuidedProfile.Tight)
            {
                state.Cash = 40_000L;
                state.Savings = 0L;
                state.CreditCard.Balance = 240_000L;
                return;
            }

            state.Cash = 120_000L;
            state.Savings = 400_000L;
            state.CreditCard.Balance = 40_000L;
        }

        /// <summary>
        /// Who comes in the third month: money to spare attracts the pyramid, and being short attracts the lender. It depends on
        /// the money after two months of decisions, not on the profile chosen at the start.
        /// </summary>
        public static TalkPartner Consequence(PlayerState state)
        {
            return state.Cash + state.Savings >= state.SalaryNet ? TalkPartner.Pyramid : TalkPartner.Lender;
        }
    }
}

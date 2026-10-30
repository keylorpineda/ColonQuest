using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// How long the player stays in the cell and what the bail costs. A sentence lasts 1 to 3 game days,
    /// served by sleeping on the cell cot or paying the bail to leave early. Pure rules, tested in EditMode.
    /// </summary>
    public static class JailSentence
    {
        public const int ShortDays = 1;
        public const int MediumDays = 2;
        public const int LongDays = 3;

        public const int ShortSeconds = 5 * 60;
        public const int MediumSeconds = 10 * 60;
        public const int LongSeconds = 15 * 60;

        /// <summary>The term in game days: 1 day for 1st arrest, 2 days for 2nd, 3 days for 3rd and beyond.</summary>
        public static int TermDays(int priorArrests)
        {
            if (priorArrests <= 0)
            {
                return ShortDays;
            }

            return priorArrests == 1 ? MediumDays : LongDays;
        }

        /// <summary>The bail for what is left of the term in game days: ₡40 000 per remaining day.</summary>
        public static long BailCostDays(int remainingDays)
        {
            if (remainingDays <= 0)
            {
                return 0L;
            }

            return remainingDays * MarketAssumptions.BailPerDay;
        }

        /// <summary>The term for an arrest, from how many times the player was arrested before in this run.</summary>
        public static int TermSeconds(int priorArrests)
        {
            if (priorArrests <= 0)
            {
                return ShortSeconds;
            }

            return priorArrests == 1 ? MediumSeconds : LongSeconds;
        }

        /// <summary>The bail for what is left of the term: each started minute costs the same. Nothing left, nothing to pay.</summary>
        public static long BailCost(int remainingSeconds)
        {
            if (remainingSeconds <= 0)
            {
                return 0L;
            }

            int minutes = (remainingSeconds + 59) / 60;
            return minutes * MarketAssumptions.BailPerMinute;
        }

        /// <summary>The time left as minutes and seconds, like 04:59.</summary>
        public static string Clock(int seconds)
        {
            int clamped = Math.Max(0, seconds);
            return (clamped / 60).ToString("00") + ":" + (clamped % 60).ToString("00");
        }
    }
}

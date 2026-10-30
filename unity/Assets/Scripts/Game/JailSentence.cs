using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// How long the player stays in the cell and what the bail costs. The term is real time, not game time: the player
    /// sits in the cell and decides between paying the bail and waiting. A first offense is short, and each new arrest in
    /// the same run is a longer stay. Pure rules, tested in EditMode.
    /// </summary>
    public static class JailSentence
    {
        public const int ShortSeconds = 5 * 60;
        public const int MediumSeconds = 10 * 60;
        public const int LongSeconds = 15 * 60;

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

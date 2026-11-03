using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// The time of the story: a month has <see cref="GameRules.DaysPerMonth"/> days and each day stands for a week. A day
    /// runs from <see cref="GameRules.DayStartHour"/> to <see cref="GameRules.DayEndHour"/> with the real time, sleeping
    /// skips to the next morning and the bank's month close starts the first week again. Pure, so it can be tested.
    /// </summary>
    public sealed class GameClock
    {
        private const float MinutesPerHour = 60f;

        private bool endReported;

        /// <summary>The day of the month, from 1 to <see cref="GameRules.DaysPerMonth"/>. Each one is a week.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Minutes since midnight.</summary>
        public float Minutes { get; private set; } = StartMinutes;

        public int Hour => (int)(Minutes / MinutesPerHour);

        public int Minute => (int)Minutes % 60;

        /// <summary>The time as the HUD shows it, 24 hours: "14:30".</summary>
        public string Time => $"{Hour:00}:{Minute:00}";

        /// <summary>True in the last week of the month: the salary and the bills wait at the bank.</summary>
        public bool IsLastDay => Day >= GameRules.DaysPerMonth;

        public bool IsNight => Hour >= GameRules.NightHour;

        /// <summary>How far through the day it is, from 0 at the first hour to 1 when it is time to sleep.</summary>
        public float DayProgress => Clamp01((Minutes - StartMinutes) / (EndMinutes - StartMinutes));

        private static float StartMinutes => GameRules.DayStartHour * MinutesPerHour;

        private static float EndMinutes => GameRules.DayEndHour * MinutesPerHour;

        /// <summary>
        /// Moves the time on by some real seconds. Returns true once, the moment the day reaches its end: the player has to
        /// sleep. The time stops there until the player sleeps.
        /// </summary>
        public bool Advance(float realSeconds, float secondsPerGameHour)
        {
            if (realSeconds <= 0f || secondsPerGameHour <= 0f)
            {
                return false;
            }

            Minutes = Math.Min(EndMinutes, Minutes + realSeconds / secondsPerGameHour * MinutesPerHour);
            if (Minutes >= EndMinutes && !endReported)
            {
                endReported = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sleeps until the next morning. Returns false on the last week: the month only moves on at the bank, so the player
        /// wakes up again in that week.
        /// </summary>
        public bool SleepToMorning()
        {
            Minutes = StartMinutes;
            endReported = false;
            if (IsLastDay)
            {
                return false;
            }

            Day++;
            return true;
        }

        /// <summary>A scene took the time up to this minute of the day, say a morning at work. The clock never goes back.</summary>
        public void SkipTo(float minutes)
        {
            Minutes = Math.Max(Minutes, Math.Min(EndMinutes, minutes));
        }

        /// <summary>The month was closed: first week, first hour.</summary>
        public void StartMonth()
        {
            Day = 1;
            Minutes = StartMinutes;
            endReported = false;
        }

        /// <summary>Puts the clock back where a saved game left it, inside the limits of a day and a month.</summary>
        public void Restore(int day, float minutes)
        {
            Day = Math.Max(1, Math.Min(GameRules.DaysPerMonth, day));
            Minutes = Math.Max(StartMinutes, Math.Min(EndMinutes, minutes));

            // A game saved right at the end of the day reports it again, or the clock would stay stuck there.
            endReported = false;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}

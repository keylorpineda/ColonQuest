using System;

namespace ColonQuest.Game
{
    /// <summary>What happened when the player tried to clock in.</summary>
    public enum ClockInResult
    {
        OnTime,

        /// <summary>The first time late this month: a word from the boss.</summary>
        LateWarned,

        /// <summary>The second time late this month: a day of pay is docked.</summary>
        LateDocked,

        /// <summary>Three times or more late this month: a day of pay is docked and the job is at risk.</summary>
        LateLastWarning,

        /// <summary>The morning is over: the shift is gone and it counts as a missed day.</summary>
        AfterMorning,

        AlreadyWorked,

        /// <summary>Before the first shift ever, the room was not visited or the health insurance not taken out yet.</summary>
        NotReady
    }

    /// <summary>
    /// The attendance at the first job: days worked, times late and days missed. Pure, so it can be tested. It only counts;
    /// what that costs is worked out by the finance engine when the salary is paid.
    /// </summary>
    public sealed class JobRecord
    {
        public int DaysWorked { get; private set; }

        /// <summary>The days clocked in on time. These are the ones a promotion looks at.</summary>
        public int PunctualDays { get; private set; }

        public JobPosition Position { get; private set; } = JobPosition.Helper;

        public int LatesThisMonth { get; private set; }

        public int MissedThisMonth { get; private set; }

        public bool WorkedToday { get; private set; }

        /// <summary>True from clocking in until the work of the morning is done.</summary>
        public bool OnShift { get; private set; }

        /// <summary>The first time late is only a warning; every other one costs a day of pay.</summary>
        public int DockedLates => Math.Max(0, LatesThisMonth - 1);

        private static float StartMinutes => GameRules.WorkStartHour * 60f;

        private static float EndMinutes => GameRules.WorkEndHour * 60f;

        public static bool IsLate(float minutes)
        {
            return minutes > StartMinutes + GameRules.WorkGraceMinutes;
        }

        /// <summary>True while the player can still make it to work today.</summary>
        public bool ExpectedAt(float minutes)
        {
            return !WorkedToday && minutes < EndMinutes;
        }

        public ClockInResult ClockIn(float minutes)
        {
            if (WorkedToday)
            {
                return ClockInResult.AlreadyWorked;
            }

            if (minutes >= EndMinutes)
            {
                return ClockInResult.AfterMorning;
            }

            WorkedToday = true;
            OnShift = true;
            DaysWorked++;
            if (!IsLate(minutes))
            {
                PunctualDays++;
                return ClockInResult.OnTime;
            }

            LatesThisMonth++;
            return LatesThisMonth == 1 ? ClockInResult.LateWarned : (LatesThisMonth == 2 ? ClockInResult.LateDocked : ClockInResult.LateLastWarning);
        }

        /// <summary>The work of the morning is done. Returns false when there was no shift to finish.</summary>
        public bool FinishShift()
        {
            bool wasOnShift = OnShift;
            OnShift = false;
            return wasOnShift;
        }

        /// <summary>Closes the working day. Returns true when the player never came: it counts as a missed day.</summary>
        public bool EndDay()
        {
            bool missed = !WorkedToday;
            if (missed)
            {
                MissedThisMonth++;
            }

            WorkedToday = false;
            OnShift = false;
            return missed;
        }

        /// <summary>The pay of the month is settled: the warnings and the absences start from zero, the days worked stay.</summary>
        public void StartMonth()
        {
            LatesThisMonth = 0;
            MissedThisMonth = 0;
            WorkedToday = false;
            OnShift = false;
        }

        public void MoveUp()
        {
            Position++;
        }

        /// <summary>Puts the record back where a saved game left it.</summary>
        public void Restore(int daysWorked, int latesThisMonth, int missedThisMonth, bool workedToday, int punctualDays, JobPosition position)
        {
            PunctualDays = Math.Max(0, punctualDays);
            Position = position;
            DaysWorked = Math.Max(0, daysWorked);
            LatesThisMonth = Math.Max(0, latesThisMonth);
            MissedThisMonth = Math.Max(0, missedThisMonth);
            WorkedToday = workedToday;
            OnShift = false;
        }
    }
}

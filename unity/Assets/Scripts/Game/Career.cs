using System.Collections.Generic;

namespace ColonQuest.Game
{
    public enum JobPosition
    {
        Helper,
        Cashier,
        Manager
    }

    public enum CourseKind
    {
        Ina,
        Una
    }

    public enum ClassRefusal
    {
        None,
        AlreadyDone,
        OutsideTheAfternoon,
        AlreadyToday,
        NotEnoughCash
    }

    /// <summary>A course: what it costs once, when enrolling, and how many afternoons it takes.</summary>
    public sealed class CourseInfo
    {
        public CourseKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
        public long Price { get; }
        public int Afternoons { get; }

        public CourseInfo(CourseKind kind, string name, string description, long price, int afternoons)
        {
            Kind = kind;
            Name = name;
            Description = description;
            Price = price;
            Afternoons = afternoons;
        }
    }

    /// <summary>
    /// The ladder of the first job. ILLUSTRATIVE VALUES, like the ones of <see cref="MarketAssumptions"/>: only the first salary
    /// is the legal minimum; the other salaries and the prices of the courses were made up to show the idea that studying pays.
    /// </summary>
    public static class Career
    {
        public const JobPosition TopPosition = JobPosition.Manager;

        /// <summary>A class takes this many minutes of the afternoon.</summary>
        public const float ClassMinutes = 240f;

        public const int AfternoonStartHour = 12;

        public const int AfternoonEndHour = 18;

        public static readonly IReadOnlyList<CourseInfo> Courses = new List<CourseInfo>
        {
            new CourseInfo(CourseKind.Ina, "Curso del INA", "Atención al cliente y caja. Barato, y abre el puesto de cajero.", 15_000L, 2),
            new CourseInfo(CourseKind.Una, "Curso de la UNA", "Administración de negocios. Más caro, y abre el puesto de encargado.", 60_000L, 3)
        };

        public static CourseInfo Course(CourseKind kind)
        {
            return Courses[(int)kind];
        }

        public static long GrossSalary(JobPosition position)
        {
            switch (position)
            {
                case JobPosition.Cashier:
                    return 430_000L;
                case JobPosition.Manager:
                    return 560_000L;
                default:
                    return GameRules.MinimumWageGross;
            }
        }

        /// <summary>The days on time that the next position asks for.</summary>
        public static int PunctualDaysFor(JobPosition next)
        {
            return next == JobPosition.Manager ? 7 : 3;
        }

        /// <summary>The course that the next position asks for.</summary>
        public static CourseKind CourseFor(JobPosition next)
        {
            return next == JobPosition.Manager ? CourseKind.Una : CourseKind.Ina;
        }
    }

    /// <summary>The afternoons attended of each course. Pure, so it can be tested.</summary>
    public sealed class CourseBook
    {
        private readonly int[] attended = new int[Career.Courses.Count];

        /// <summary>True once the player went to a class today: one a day.</summary>
        public bool AttendedToday { get; set; }

        public int Attended(CourseKind kind)
        {
            return attended[(int)kind];
        }

        public bool Enrolled(CourseKind kind)
        {
            return attended[(int)kind] > 0;
        }

        public bool Done(CourseKind kind)
        {
            return attended[(int)kind] >= Career.Course(kind).Afternoons;
        }

        public void Attend(CourseKind kind)
        {
            attended[(int)kind]++;
            AttendedToday = true;
        }

        public void Restore(CourseKind kind, int afternoons)
        {
            attended[(int)kind] = afternoons < 0 ? 0 : afternoons;
        }
    }
}

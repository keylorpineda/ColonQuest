using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>What the simulator wants the user to do next and, when it is a place, where it is.</summary>
    public sealed class Objective
    {
        /// <summary>What to do, in Spanish; the screen translates it.</summary>
        public string Text { get; }

        /// <summary>The scene the place is in, or null when the objective has no place.</summary>
        public string Scene { get; }

        /// <summary>The object to reach in that scene. Empty when reaching the scene is enough.</summary>
        public string Target { get; }

        public Objective(string text, string scene = null, string target = "")
        {
            Text = text;
            Scene = scene;
            Target = target ?? string.Empty;
        }
    }

    /// <summary>Works out the current objective and where to point. Pure, so it can be tested; the marks on the screen are the overlay's.</summary>
    public static class Objectives
    {
        /// <summary>Instead of an object's name: the way out of the building, back to the town.</summary>
        public const string WayOut = "<way out>";

        /// <summary>
        /// The object to point at from this scene: the place itself, the door of its building from the town, or the way out
        /// from any other building. Null when there is nothing to point at.
        /// </summary>
        public static string PointAt(Objective objective, string activeScene)
        {
            if (objective == null || objective.Scene == null)
            {
                return null;
            }

            if (activeScene == objective.Scene)
            {
                return objective.Target.Length > 0 ? objective.Target : null;
            }

            return activeScene == SceneNames.Town ? "Door_" + objective.Scene : WayOut;
        }

        /// <summary>The next thing to do in a normal run, or null when the afternoon is free.</summary>
        public static Objective ForGame(GameLoop loop, string activeScene)
        {
            if (loop == null || loop.IsFinished)
            {
                return null;
            }

            if (loop.ArrivalDay && loop.Job.DaysWorked == 0)
            {
                return Arrival(loop);
            }

            float minutes = loop.Clock.Minutes;
            if (loop.Job.ExpectedAt(minutes))
            {
                return ToWork(JobRecord.IsLate(minutes), activeScene);
            }

            if (loop.Clock.IsNight)
            {
                return ToBed(loop, activeScene);
            }

            if (loop.Bills.Due.Count > 0 && (loop.Bills.ArrivedThisMonth || HasLateBill(loop)))
            {
                return new Objective("Pagá los recibos: abrí el teléfono con la P");
            }

            if (loop.InArrears)
            {
                return new Objective(TaskList.CatchUp, SceneNames.Town, "Station_Lender");
            }

            return loop.Clock.IsLastDay ? new Objective(TaskList.CloseMonth, SceneNames.Banco, "Station_Bank") : null;
        }

        private static Objective Arrival(GameLoop loop)
        {
            if (!loop.VisitedRoom)
            {
                return new Objective(TaskList.VisitRoom, SceneNames.Cuarto);
            }

            return !loop.Holdings.HasHealthInsurance
                ? new Objective(TaskList.TakeInsurance, SceneNames.Hospital, "Station_HealthDesk")
                : new Objective(TaskList.FirstShift, SceneNames.Soda, "Station_Work");
        }

        private static Objective ToWork(bool late, string activeScene)
        {
            string text;
            if (activeScene == SceneNames.Town || activeScene == SceneNames.Soda)
            {
                text = late ? "¡Vas tarde al trabajo!" : "Al trabajo: entrás a las 7:00";
            }
            else
            {
                text = late ? "¡Vas tarde! Salí para la soda" : "Salí para la soda: entrás a las 7:00";
            }

            return new Objective(text, SceneNames.Soda, "Station_Work");
        }

        private static Objective ToBed(GameLoop loop, string activeScene)
        {
            string home = loop.Holdings.Home != null ? SceneNames.Casa : SceneNames.Cuarto;
            if (activeScene == home)
            {
                return new Objective("Ya es tarde: acostate a dormir", home);
            }

            return new Objective(home == SceneNames.Casa ? "Ya es tarde: andá a tu casa" : "Ya es tarde: andá a tu cuarto", home);
        }

        private static bool HasLateBill(GameLoop loop)
        {
            foreach (var bill in loop.Bills.Due)
            {
                if (bill.MonthsLate > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

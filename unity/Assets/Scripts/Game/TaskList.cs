using System.Collections.Generic;

namespace ColonQuest.Game
{
    /// <summary>One line of the list of things to do: what to do and whether it is done today.</summary>
    public sealed class TaskItem
    {
        public string Text { get; }
        public bool Done { get; }

        public TaskItem(string text, bool done)
        {
            Text = text;
            Done = done;
        }
    }

    /// <summary>
    /// What the player has to do now, read from the run: the day of arrival first (room, insurance, first shift), then the
    /// work of every day, the bills, the debts and the bank at the end of the month. Pure, so it can be tested.
    /// </summary>
    public static class TaskList
    {
        public const string VisitRoom = "Conocer tu cuarto de alquiler";
        public const string TakeInsurance = "Sacar el seguro de salud en el hospital";
        public const string FirstShift = "Tu primer turno en la soda El Muelle";
        public const string Work = "Trabajar en la soda (7:00 a 12:00)";
        public const string PayBills = "Pagar los recibos en el banco";
        public const string CatchUp = "Ponerte al día con lo que debés";
        public const string CloseMonth = "Cerrar el mes en el banco";
        public const string Sleep = "Dormir en tu cuarto";

        public static IReadOnlyList<TaskItem> For(GameLoop loop)
        {
            var list = new List<TaskItem>();
            if (loop == null || loop.IsFinished)
            {
                return list;
            }

            if (loop.ArrivalDay && loop.Job.DaysWorked == 0)
            {
                list.Add(new TaskItem(VisitRoom, loop.VisitedRoom));
                list.Add(new TaskItem(TakeInsurance, loop.Holdings.HasHealthInsurance));
                list.Add(new TaskItem(FirstShift, loop.Job.WorkedToday));
                return list;
            }

            list.Add(new TaskItem(Work, loop.Job.WorkedToday && !loop.Job.OnShift));
            if (loop.Bills.ArrivedThisMonth)
            {
                list.Add(new TaskItem(PayBills, loop.Bills.Due.Count == 0));
            }

            if (loop.InArrears)
            {
                list.Add(new TaskItem(CatchUp, false));
            }

            if (loop.Clock.IsLastDay)
            {
                list.Add(new TaskItem(CloseMonth, false));
            }

            list.Add(new TaskItem(Sleep, false));
            return list;
        }
    }
}

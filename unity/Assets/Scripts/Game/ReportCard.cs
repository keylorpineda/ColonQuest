using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>One line of the report card at the end of a run.</summary>
    public readonly struct ReportLine
    {
        public string Concept { get; }
        public char Grade { get; }

        public ReportLine(string concept, char grade)
        {
            Concept = concept;
            Grade = grade;
        }
    }

    /// <summary>
    /// The grades of a run, from A to D, worked out from the player's numbers. Never from a model. The thresholds are compared
    /// against the salary, so they mean the same at any pay.
    /// </summary>
    public static class ReportCard
    {
        public static List<ReportLine> Build(PlayerState state, Holdings holdings, BillBook bills)
        {
            return new List<ReportLine>
            {
                new ReportLine("Presupuesto", Budget(state, bills)),
                new ReportLine("Fondo de emergencia", AtLeast(state.EmergencyFund, state.SalaryNet, 50, 25, 1)),
                new ReportLine("Deudas", Debts(state)),
                new ReportLine("Cuotas", AtMost(GameRules.MonthlyInstallments(state), state.SalaryNet, 10, 20, 35)),
                new ReportLine("Inversiones", AtLeast(state.Savings + holdings.FarmInvested, state.SalaryNet, 100, 50, 1))
            };
        }

        private static char Budget(PlayerState state, BillBook bills)
        {
            int late = 0;
            foreach (var bill in bills.Due)
            {
                late = System.Math.Max(late, bill.MonthsLate);
            }

            if (late >= BillBook.MonthsUntilCut)
            {
                return 'D';
            }

            if (late > 0)
            {
                return 'C';
            }

            return state.Cash * 4L >= state.SalaryNet ? 'A' : 'B';
        }

        private static char Debts(PlayerState state)
        {
            char grade = AtMost(GameRules.DebtBalance(state), state.SalaryNet, 25, 50, 100);
            return state.InformalLoans.Count > 0 && grade < 'C' ? 'C' : grade;
        }

        /// <summary>More is better: the value as a percent of the salary reaches the mark of A, B or C, or it is a D.</summary>
        private static char AtLeast(long value, long salary, int a, int b, int c)
        {
            long percent = Percent(value, salary);
            return percent >= a ? 'A' : (percent >= b ? 'B' : (percent >= c ? 'C' : 'D'));
        }

        /// <summary>Less is better: the value as a percent of the salary stays under the mark of A, B or C, or it is a D.</summary>
        private static char AtMost(long value, long salary, int a, int b, int c)
        {
            long percent = Percent(value, salary);
            return percent <= a ? 'A' : (percent <= b ? 'B' : (percent <= c ? 'C' : 'D'));
        }

        private static long Percent(long value, long salary)
        {
            return salary > 0L ? value * 100L / salary : 0L;
        }
    }
}

using System;
using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    /// <summary>One finished situation for the game master of the demo: who the player is this month, ready to send.</summary>
    public sealed class DemoScenario
    {
        public readonly string Name;
        public readonly string Summary;
        public readonly Func<PlayerState> Build;

        public DemoScenario(string name, string summary, Func<PlayerState> build)
        {
            Name = name;
            Summary = summary;
            Build = build;
        }
    }

    /// <summary>
    /// The situations a judge can try without playing months to get there. Each one is an ordinary
    /// <see cref="PlayerState"/>: the backend cannot tell it from a real game.
    /// </summary>
    public static class DemoScenarios
    {
        public static readonly IReadOnlyList<DemoScenario> All = new List<DemoScenario>
        {
            new DemoScenario("Tarjeta al límite", "Mes 4: la tarjeta casi llena, ₡38 000 en efectivo, sin ahorro y pagando solo el mínimo.", CardAtTheLimit),
            new DemoScenario("Mucho ahorro", "Mes 5: ₡900 000 ahorrados, ₡220 000 en efectivo y ninguna deuda.", HighSavings),
            new DemoScenario("Diciembre", "Mes 12: ₡180 000 en efectivo, ₡120 000 ahorrados y un poco en la tarjeta.", December),
            new DemoScenario("Primer mes", "Mes 1: acaba de caer el salario, sin ahorro y sin deudas.", FirstMonth)
        };

        public static PlayerState CardAtTheLimit()
        {
            var state = Base(4, cash: 38_000L, savings: 0L, wellbeing: 48);
            state.CreditCard = new CreditCard(285_000L, 300_000L, 3.5m, 10m);
            state.Installments.Add(new Installment("Celular", 25_000L, 21));
            state.History.Add("paid_minimum_only");
            state.History.Add("skipped_savings");
            return state;
        }

        public static PlayerState HighSavings()
        {
            var state = Base(5, cash: 220_000L, savings: 900_000L, wellbeing: 78);
            state.EmergencyFund = 150_000L;
            state.History.Add("saved_every_month");
            return state;
        }

        public static PlayerState December()
        {
            var state = Base(12, cash: 180_000L, savings: 120_000L, wellbeing: 66);
            state.CreditCard = new CreditCard(60_000L, 300_000L, 3.5m, 10m);
            return state;
        }

        public static PlayerState FirstMonth()
        {
            return Base(1, cash: 300_000L, savings: 0L, wellbeing: 70);
        }

        /// <summary>How the year ends for the summary: with heavy debts, or with none.</summary>
        public static PlayerState YearEnd(bool withDebts)
        {
            var state = Base(12, cash: withDebts ? 40_000L : 260_000L, savings: withDebts ? 0L : 640_000L, wellbeing: withDebts ? 41 : 82);
            if (withDebts)
            {
                state.CreditCard = new CreditCard(290_000L, 300_000L, 3.5m, 10m);
                state.Installments.Add(new Installment("Moto", 48_000L, 40));
                state.InformalLoans.Add(new InformalLoan("Prestamista", 650_000L, 15m));
            }
            else
            {
                state.EmergencyFund = 300_000L;
            }

            return state;
        }

        /// <summary>The debts the figures of the end of the year count: the card, the loans and the installments still to pay.</summary>
        public static long TotalDebt(PlayerState state)
        {
            long debt = state.CreditCard.Balance;
            foreach (var loan in state.InformalLoans)
            {
                debt += loan.Balance;
            }

            foreach (var installment in state.Installments)
            {
                debt += installment.Monthly * installment.MonthsLeft;
            }

            return debt;
        }

        private static PlayerState Base(int month, long cash, long savings, int wellbeing)
        {
            return new PlayerState
            {
                CharacterName = "Demo",
                Month = month,
                SalaryNet = 450_000L,
                Cash = cash,
                Savings = savings,
                Wellbeing = wellbeing
            };
        }
    }
}

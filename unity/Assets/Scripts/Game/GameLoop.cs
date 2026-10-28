using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>What one decision did to the player's numbers, to show right after choosing.</summary>
    public sealed class ChoiceOutcome
    {
        public string ChoiceLabel = string.Empty;
        public long CashDelta;
        public long SavingsDelta;
        public long EmergencyFundDelta;
        public long CardDelta;
        public long InstallmentsPerMonthDelta;
        public long InformalDebtDelta;
        public int WellbeingDelta;

        public bool ChangedNothing =>
            CashDelta == 0L && SavingsDelta == 0L && EmergencyFundDelta == 0L && CardDelta == 0L
            && InstallmentsPerMonthDelta == 0L && InformalDebtDelta == 0L && WellbeingDelta == 0;
    }

    /// <summary>
    /// The rules of a run without any Unity or network code: the player's state, which tasks were
    /// done this month, applying a decision and closing the month. All money math is the finance
    /// engine's; this only decides when it runs.
    /// </summary>
    public sealed class GameLoop
    {
        private readonly HashSet<StationType> doneStations = new HashSet<StationType>();

        public PlayerState State { get; private set; }
        public int MonthsPlayed { get; private set; }
        public int TasksDone { get; private set; }
        public int Decisions { get; private set; }

        public bool IsFinished => MonthsPlayed >= GameRules.MonthsToPlay;

        public GameLoop()
        {
            NewGame();
        }

        public void NewGame()
        {
            State = GameRules.NewPlayer();
            MonthsPlayed = 0;
            TasksDone = 0;
            Decisions = 0;
            doneStations.Clear();
        }

        /// <summary>The bank and the house are not tasks; every other place offers one event per month.</summary>
        public static bool IsTaskStation(StationType type)
        {
            return type != StationType.Bank && type != StationType.Home && type != StationType.Thing && type != StationType.Atm;
        }

        public bool CanDoTask(StationType type)
        {
            return IsTaskStation(type) && !IsFinished && TasksDone < GameRules.TasksPerMonth && !doneStations.Contains(type);
        }

        public bool AlreadyDid(StationType type)
        {
            return doneStations.Contains(type);
        }

        public bool AllTasksDone => TasksDone >= GameRules.TasksPerMonth;

        /// <summary>Applies the chosen option through the finance engine and counts the task.</summary>
        /// <exception cref="InvalidGameEventEffectException">The choice breaks the contract ranges; nothing changed.</exception>
        public ChoiceOutcome Choose(GameEvent gameEvent, string choiceId, StationType station)
        {
            var before = State.Clone();
            FinanceEngine.ApplyChoice(State, gameEvent, choiceId);

            Choice chosen = gameEvent.Choices.Find(c => c.Id == choiceId);
            var outcome = new ChoiceOutcome
            {
                ChoiceLabel = chosen?.Label ?? string.Empty,
                CashDelta = State.Cash - before.Cash,
                SavingsDelta = State.Savings - before.Savings,
                EmergencyFundDelta = State.EmergencyFund - before.EmergencyFund,
                CardDelta = State.CreditCard.Balance - before.CreditCard.Balance,
                InstallmentsPerMonthDelta = GameRules.MonthlyInstallments(State) - GameRules.MonthlyInstallments(before),
                InformalDebtDelta = SumLoans(State) - SumLoans(before),
                WellbeingDelta = State.Wellbeing - before.Wellbeing
            };

            if (IsTaskStation(station))
            {
                doneStations.Add(station);
                TasksDone++;
            }

            Decisions++;
            return outcome;
        }

        /// <summary>Pays the salary and runs the month: installments, the card cycle, interest and living expenses.</summary>
        public MonthlyTickReport CloseMonth()
        {
            var report = FinanceEngine.RunMonthlyTick(State, new MonthlyTickOptions
            {
                FixedLivingExpensesColones = GameRules.LivingExpenses
            });

            MonthsPlayed++;
            TasksDone = 0;
            doneStations.Clear();
            return report;
        }

        private static long SumLoans(PlayerState state)
        {
            long total = 0L;
            foreach (var loan in state.InformalLoans)
            {
                total += loan.Balance;
            }

            return total;
        }
    }
}

using System;
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

    /// <summary>The result of buying something big (a home, a car, a farm stake): how it ended and what it changed.</summary>
    public sealed class AssetPurchase
    {
        public PurchaseResult Result { get; }
        public ChoiceOutcome Outcome { get; }

        public AssetPurchase(PurchaseResult result, ChoiceOutcome outcome)
        {
            Result = result;
            Outcome = outcome;
        }
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

        /// <summary>The home, the car and the farm stakes the player bought in this run.</summary>
        public Holdings Holdings { get; private set; } = new Holdings();

        /// <summary>The fuel and upkeep that went out with the last closed month, to show in its report.</summary>
        public long LastRunningCosts { get; private set; }

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
            Holdings = new Holdings();
            LastRunningCosts = 0L;
            HurtThisMonth = false;
            Arrests = 0;
            HasSwimsuit = false;
            SwamThisMonth = false;
            SleptThisMonth = false;
            CookedThisMonth = false;
            doneStations.Clear();
        }

        /// <summary>The bank, the house and the market places are not tasks; every other place offers one event per month.</summary>
        public static bool IsTaskStation(StationType type)
        {
            return type != StationType.Bank && type != StationType.Home && type != StationType.Thing && type != StationType.Atm
                && type != StationType.RealEstate && type != StationType.CarDealer && type != StationType.Farm
                && type != StationType.HealthDesk && type != StationType.Wardrobe && type != StationType.Beach;
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

        /// <summary>True once the player has been run over this month; it happens once a month at most.</summary>
        public bool HurtThisMonth { get; private set; }

        /// <summary>How many times the police took the player away in this run. Each one is a longer stay in the cell.</summary>
        public int Arrests { get; private set; }

        public bool HasSwimsuit { get; private set; }

        public bool SwamThisMonth { get; private set; }

        /// <summary>True once the player slept in the bed this month. Skipping it costs a little mood when the month closes.</summary>
        public bool SleptThisMonth { get; private set; }

        public bool CookedThisMonth { get; private set; }

        /// <summary>Days of the month that are gone (a night in jail, a day in the hospital): they count as tasks not done.</summary>
        public void LoseTasks(int days)
        {
            TasksDone = Math.Min(GameRules.TasksPerMonth, TasksDone + Math.Max(0, days));
        }

        // ---------------------------------------------------------------- things that happen to the player

        public ChoiceOutcome ArrestPlayer()
        {
            var before = State.Clone();
            FinanceEngine.ChargeFine(State, MarketAssumptions.TrespassFine);
            FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.JailWellbeingLoss);
            LoseTasks(MarketAssumptions.JailDaysLost);
            Arrests++;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Te arrestaron por entrar a una casa ajena"));
        }

        /// <summary>The bail for what is left of the term: it is paid from cash, or from the card when there is not enough.</summary>
        public ChoiceOutcome PayBail(long cost)
        {
            var before = State.Clone();
            FinanceEngine.ChargeFine(State, cost);
            Decisions++;
            return OutcomeFrom(before, Loc.T("Pagaste la fianza"));
        }

        /// <summary>The swimsuit is paid in cash. Null when the cash is not enough.</summary>
        public ChoiceOutcome BuySwimsuit()
        {
            if (HasSwimsuit)
            {
                return null;
            }

            var before = State.Clone();
            if (!FinanceEngine.TrySpendCash(State, MarketAssumptions.SwimsuitPrice))
            {
                return null;
            }

            HasSwimsuit = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Compraste un bañador"));
        }

        /// <summary>A swim in the sea: free, and good for the mood. Once a month.</summary>
        public ChoiceOutcome Swim()
        {
            if (!HasSwimsuit || SwamThisMonth)
            {
                return null;
            }

            var before = State.Clone();
            FinanceEngine.ChangeWellbeing(State, MarketAssumptions.SwimWellbeing);
            SwamThisMonth = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Te diste un chapuzón en el mar"));
        }

        /// <summary>A night in the own bed: free, a little rest for the mood. Once a month.</summary>
        public ChoiceOutcome Rest()
        {
            if (SleptThisMonth)
            {
                return null;
            }

            var before = State.Clone();
            FinanceEngine.ChangeWellbeing(State, MarketAssumptions.RestWellbeing);
            SleptThisMonth = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Dormiste en tu cama"));
        }

        /// <summary>Cooking at home instead of eating out: a small saving and a little better mood. Once a month.</summary>
        public ChoiceOutcome Cook()
        {
            if (CookedThisMonth)
            {
                return null;
            }

            var before = State.Clone();
            FinanceEngine.CreditCash(State, MarketAssumptions.HomeCookingSaving);
            FinanceEngine.ChangeWellbeing(State, MarketAssumptions.CookWellbeing);
            CookedThisMonth = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Cocinaste en casa"));
        }

        /// <summary>An officer saw the player cross on red: a fine and a little stress, no jail and no lost days.</summary>
        public ChoiceOutcome FineRedLight()
        {
            var before = State.Clone();
            FinanceEngine.ChargeFine(State, MarketAssumptions.RedLightFine);
            FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.RedLightWellbeingLoss);
            Decisions++;
            return OutcomeFrom(before, Loc.T("Te multaron por cruzar en rojo"));
        }

        /// <summary>The player was run over: the bill depends on having health insurance and on the emergency fund.</summary>
        public ChoiceOutcome HospitalizePlayer()
        {
            var before = State.Clone();
            long bill = Holdings.HasHealthInsurance
                ? FinanceEngine.PatientShare(MarketAssumptions.HospitalBill, MarketAssumptions.InsuredPatientSharePct)
                : MarketAssumptions.HospitalBill;
            FinanceEngine.ChargeMedicalBill(State, bill);
            FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.HospitalWellbeingLoss);
            LoseTasks(MarketAssumptions.HospitalDaysLost);
            HurtThisMonth = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T(Holdings.HasHealthInsurance ? "Te atropelló un carro (con seguro de salud)" : "Te atropelló un carro (sin seguro de salud)"));
        }

        public bool BuyHealthInsurance()
        {
            if (Holdings.HasHealthInsurance || IsFinished)
            {
                return false;
            }

            Holdings.SetHealthInsurance(MarketAssumptions.HealthInsurancePremium);
            State.RecordHistory("bought_health_insurance");
            return true;
        }

        /// <summary>True when the player owes enough, and late enough, that the collectors come: an informal loan, or a card that is almost full.</summary>
        public bool InArrears => CollectionRules.InArrears(State);

        public long CollectionDemand => CollectionRules.Demand(State);

        public ChoiceOutcome PayCollectors()
        {
            var before = State.Clone();
            FinanceEngine.PayCollectors(State, CollectionRules.Demand(State));
            Decisions++;
            return OutcomeFrom(before, Loc.T("Pagar a los cobradores"));
        }

        public ChoiceOutcome NegotiateCollectors()
        {
            var before = State.Clone();
            FinanceEngine.NegotiateCollectors(State, CollectionRules.Demand(State), MarketAssumptions.CollectionNegotiateSurchargePct);
            Decisions++;
            return OutcomeFrom(before, Loc.T("Negociar con los cobradores"));
        }

        public ChoiceOutcome IgnoreCollectors()
        {
            var before = State.Clone();
            FinanceEngine.IgnoreCollectors(State, CollectionRules.Demand(State), MarketAssumptions.CollectionIgnoreFeePct, MarketAssumptions.CollectionIgnoreWellbeingLoss);
            Decisions++;
            return OutcomeFrom(before, Loc.T("Ignorar a los cobradores"));
        }

        /// <summary>Applies the chosen option through the finance engine and counts the task.</summary>
        /// <exception cref="InvalidGameEventEffectException">The choice breaks the contract ranges; nothing changed.</exception>
        public ChoiceOutcome Choose(GameEvent gameEvent, string choiceId, StationType station)
        {
            var before = State.Clone();
            FinanceEngine.ApplyChoice(State, gameEvent, choiceId);

            Choice chosen = gameEvent.Choices.Find(c => c.Id == choiceId);
            var outcome = OutcomeFrom(before, chosen?.Label ?? string.Empty);

            if (IsTaskStation(station))
            {
                doneStations.Add(station);
                TasksDone++;
            }

            Decisions++;
            return outcome;
        }

        private ChoiceOutcome OutcomeFrom(PlayerState before, string label)
        {
            return new ChoiceOutcome
            {
                ChoiceLabel = label,
                CashDelta = State.Cash - before.Cash,
                SavingsDelta = State.Savings - before.Savings,
                EmergencyFundDelta = State.EmergencyFund - before.EmergencyFund,
                CardDelta = State.CreditCard.Balance - before.CreditCard.Balance,
                InstallmentsPerMonthDelta = GameRules.MonthlyInstallments(State) - GameRules.MonthlyInstallments(before),
                InformalDebtDelta = SumLoans(State) - SumLoans(before),
                WellbeingDelta = State.Wellbeing - before.Wellbeing
            };
        }

        // ---------------------------------------------------------------- homes, cars and the farm

        // None of these is a task of the month: the player may visit the agencies whenever there is cash.

        public AssetPurchase RentHome(HomeListing home)
        {
            return Buy(Loc.F("Alquilar {0}", Loc.T(home.Name)), () => FinanceEngine.TryRentHome(State, Holdings, home.Name, MarketAssumptions.QuoteRent(home)));
        }

        public AssetPurchase BuyHome(HomeListing home)
        {
            return Buy(Loc.F("Comprar {0}", Loc.T(home.Name)), () => FinanceEngine.TryBuyHome(State, Holdings, home.Name, MarketAssumptions.QuoteMortgage(home)));
        }

        public AssetPurchase BuyCar(CarModel car)
        {
            return Buy(Loc.F("Comprar {0}", Loc.T(car.Name)), () => FinanceEngine.TryBuyCar(State, Holdings, car.ToPlan()));
        }

        public AssetPurchase InvestInFarm(FarmOption option, long amount)
        {
            return Buy(Loc.F("Invertir en {0}", Loc.T(option.Name).ToLowerInvariant()), () => FinanceEngine.TryInvestInFarm(State, Holdings, option.Kind, amount));
        }

        /// <summary>Why a rental cannot be signed right now, or None. Nothing changes.</summary>
        public PurchaseFailure CheckRent(HomeListing home)
        {
            return Probe((state, holdings) => FinanceEngine.TryRentHome(state, holdings, home.Name, MarketAssumptions.QuoteRent(home)));
        }

        public PurchaseFailure CheckBuyHome(HomeListing home)
        {
            return Probe((state, holdings) => FinanceEngine.TryBuyHome(state, holdings, home.Name, MarketAssumptions.QuoteMortgage(home)));
        }

        public PurchaseFailure CheckBuyCar(CarModel car)
        {
            return Probe((state, holdings) => FinanceEngine.TryBuyCar(state, holdings, car.ToPlan()));
        }

        private AssetPurchase Buy(string label, Func<PurchaseResult> purchase)
        {
            var before = State.Clone();
            var result = purchase();
            if (!result.Bought)
            {
                return new AssetPurchase(result, new ChoiceOutcome { ChoiceLabel = label });
            }

            Decisions++;
            return new AssetPurchase(result, OutcomeFrom(before, label));
        }

        /// <summary>Runs a purchase on copies, so the real state and holdings stay as they are.</summary>
        private PurchaseFailure Probe(Func<PlayerState, Holdings, PurchaseResult> purchase)
        {
            var probe = new Holdings();
            if (Holdings.Home != null)
            {
                probe.SetHome(Holdings.Home);
            }

            if (Holdings.Car != null)
            {
                probe.SetCar(Holdings.Car);
            }

            return purchase(State.Clone(), probe).Failure;
        }

        /// <summary>Pays the salary and runs the month: installments, the card cycle, interest and living expenses.</summary>
        public MonthlyTickReport CloseMonth()
        {
            LastRunningCosts = Holdings.MonthlyRunningCosts;
            if (!SleptThisMonth)
            {
                FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.SkippedSleepWellbeingLoss);
            }

            var report = FinanceEngine.RunMonthlyTick(State, new MonthlyTickOptions
            {
                FixedLivingExpensesColones = GameRules.LivingExpenses + LastRunningCosts
            });

            MonthsPlayed++;
            TasksDone = 0;
            HurtThisMonth = false;
            SwamThisMonth = false;
            SleptThisMonth = false;
            CookedThisMonth = false;
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

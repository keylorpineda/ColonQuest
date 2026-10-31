using System;
using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.World;

namespace ColonQuest.Game
{
    /// <summary>What one decision did to the player's numbers, to show right after choosing.</summary>
    public sealed class ChoiceOutcome
    {
        public string ChoiceLabel = string.Empty;

        /// <summary>Why the numbers moved, when the label alone does not say it. Empty for most outcomes.</summary>
        public string Note = string.Empty;
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

        /// <summary>Tasks completed today: at most <see cref="GameRules.TasksPerDay"/> per day.</summary>
        public int TasksToday { get; private set; }

        /// <summary>Remaining days of jail sentence to serve.</summary>
        public int JailDaysRemaining { get; private set; }

        /// <summary>The week of the month and the hour of the day.</summary>
        public GameClock Clock { get; private set; } = new GameClock();

        /// <summary>The home, the car and the farm stakes the player bought in this run.</summary>
        public Holdings Holdings { get; private set; } = new Holdings();

        /// <summary>The attendance at the first job.</summary>
        public JobRecord Job { get; private set; } = new JobRecord();

        /// <summary>The bills of the home that are waiting to be paid.</summary>
        public BillBook Bills { get; private set; } = new BillBook();

        /// <summary>The afternoons attended of each course.</summary>
        public CourseBook Courses { get; private set; } = new CourseBook();

        /// <summary>The goals the phone proposed at the start of the run.</summary>
        public GoalBook Goals { get; private set; } = new GoalBook();

        /// <summary>The fuel and upkeep that went out with the last closed month, to show in its report.</summary>
        public long LastRunningCosts { get; private set; }

        /// <summary>The rent of the room that went out with the last closed month: none after moving to another home.</summary>
        public long LastRoomRent { get; private set; }

        public long QuincenaPaidThisMonth { get; private set; }

        public event Action<long> QuincenaPaid;

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
            TasksToday = 0;
            JailDaysRemaining = 0;
            Decisions = 0;
            Holdings = new Holdings();
            LastRunningCosts = 0L;
            LastRoomRent = 0L;
            HurtThisMonth = false;
            Arrests = 0;
            HasSwimsuit = false;
            Outfit = 0;
            SwamThisMonth = false;
            SleptThisMonth = false;
            CookedThisMonth = false;
            Clock = new GameClock();
            Job = new JobRecord();
            Bills = new BillBook();
            Courses = new CourseBook();
            Goals = GoalBook.Propose(State);
            doneStations.Clear();
        }

        /// <summary>The run as a saved game writes it (see <see cref="SaveSlots"/>).</summary>
        public RunSnapshot Snapshot()
        {
            var snapshot = new RunSnapshot
            {
                state = ApiMapper.ToDto(State),
                holdings = HoldingsSaving.ToSnapshot(Holdings),
                monthsPlayed = MonthsPlayed,
                tasksDone = TasksDone,
                tasksToday = TasksToday,
                jailDaysRemaining = JailDaysRemaining,
                decisions = Decisions,
                arrests = Arrests,
                hurtThisMonth = HurtThisMonth,
                hasSwimsuit = HasSwimsuit,
                outfit = Outfit,
                swamThisMonth = SwamThisMonth,
                sleptThisMonth = SleptThisMonth,
                cookedThisMonth = CookedThisMonth,
                day = Clock.Day,
                minutes = Clock.Minutes,
                daysWorked = Job.DaysWorked,
                latesThisMonth = Job.LatesThisMonth,
                missedThisMonth = Job.MissedThisMonth,
                workedToday = Job.WorkedToday,
                billsArrived = Bills.ArrivedThisMonth,
                punctualDays = Job.PunctualDays,
                jobPosition = (int)Job.Position,
                attendedToday = Courses.AttendedToday,
                quincenaPaidThisMonth = QuincenaPaidThisMonth
            };

            foreach (var course in Career.Courses)
            {
                snapshot.courseAfternoons.Add(Courses.Attended(course.Kind));
            }

            foreach (var goal in Goals.Goals)
            {
                snapshot.goalKinds.Add((int)goal.Kind);
                snapshot.goalTargets.Add(goal.Target);
                snapshot.goalStarts.Add(goal.Start);
                snapshot.goalsDone.Add(goal.Done);
            }

            foreach (var bill in Bills.Due)
            {
                snapshot.billKinds.Add((int)bill.Kind);
                snapshot.billAmounts.Add(bill.Amount);
                snapshot.billMonthsLate.Add(bill.MonthsLate);
            }

            foreach (var station in doneStations)
            {
                snapshot.doneStations.Add((int)station);
            }

            return snapshot;
        }

        /// <summary>Goes on with a saved run: the same finances, belongings, month, week and hour.</summary>
        public void Restore(RunSnapshot snapshot)
        {
            NewGame();
            if (snapshot == null)
            {
                return;
            }

            State = snapshot.state != null ? ApiMapper.FromDto(snapshot.state) : GameRules.NewPlayer();
            Holdings = HoldingsSaving.FromSnapshot(snapshot.holdings);
            MonthsPlayed = Math.Max(0, snapshot.monthsPlayed);
            TasksDone = Math.Max(0, Math.Min(GameRules.TasksPerMonth, snapshot.tasksDone));
            TasksToday = Math.Max(0, snapshot.tasksToday);
            JailDaysRemaining = Math.Max(0, snapshot.jailDaysRemaining);
            Decisions = Math.Max(0, snapshot.decisions);
            Arrests = Math.Max(0, snapshot.arrests);
            HurtThisMonth = snapshot.hurtThisMonth;
            HasSwimsuit = snapshot.hasSwimsuit;
            Outfit = Math.Max(0, snapshot.outfit);
            SwamThisMonth = snapshot.swamThisMonth;
            SleptThisMonth = snapshot.sleptThisMonth;
            CookedThisMonth = snapshot.cookedThisMonth;
            QuincenaPaidThisMonth = Math.Max(0L, snapshot.quincenaPaidThisMonth);
            Clock.Restore(snapshot.day, snapshot.minutes);
            Job.Restore(snapshot.daysWorked, snapshot.latesThisMonth, snapshot.missedThisMonth, snapshot.workedToday, snapshot.punctualDays, (JobPosition)snapshot.jobPosition);
            Courses.AttendedToday = snapshot.attendedToday;
            int courses = Math.Min(Career.Courses.Count, snapshot.courseAfternoons?.Count ?? 0);
            for (int i = 0; i < courses; i++)
            {
                Courses.Restore((CourseKind)i, snapshot.courseAfternoons[i]);
            }
            RestoreGoals(snapshot);
            Bills.RestoreArrived(snapshot.billsArrived);
            int bills = Math.Min(snapshot.billKinds?.Count ?? 0, Math.Min(snapshot.billAmounts?.Count ?? 0, snapshot.billMonthsLate?.Count ?? 0));
            for (int i = 0; i < bills; i++)
            {
                Bills.Restore((BillKind)snapshot.billKinds[i], snapshot.billAmounts[i], snapshot.billMonthsLate[i]);
            }
            foreach (int station in snapshot.doneStations ?? new List<int>())
            {
                doneStations.Add((StationType)station);
            }
        }

        /// <summary>A save from before the goals existed gets them proposed from where the run is now.</summary>
        private void RestoreGoals(RunSnapshot snapshot)
        {
            int count = snapshot.goalKinds?.Count ?? 0;
            if (count == 0 || snapshot.goalTargets == null || snapshot.goalStarts == null || snapshot.goalsDone == null)
            {
                Goals = GoalBook.Propose(State);
                return;
            }

            Goals = new GoalBook();
            int stored = Math.Min(count, Math.Min(snapshot.goalTargets.Count, Math.Min(snapshot.goalStarts.Count, snapshot.goalsDone.Count)));
            for (int i = 0; i < stored; i++)
            {
                Goals.Restore(new Goal((GoalKind)snapshot.goalKinds[i], snapshot.goalTargets[i], snapshot.goalStarts[i], snapshot.goalsDone[i]));
            }
        }

        /// <summary>The bank, the house and the market places are not tasks; every other place offers one event per day.</summary>
        public static bool IsTaskStation(StationType type)
        {
            return type != StationType.Bank && type != StationType.Home && type != StationType.Thing && type != StationType.Atm
                && type != StationType.RealEstate && type != StationType.CarDealer && type != StationType.Farm
                && type != StationType.HealthDesk && type != StationType.Wardrobe && type != StationType.Beach
                && type != StationType.Work && type != StationType.Furniture && type != StationType.Courses;
        }

        public bool CanDoTask(StationType type)
        {
            return IsTaskStation(type) && !IsFinished && TasksDone < GameRules.TasksPerMonth && TasksToday < GameRules.TasksPerDay && !doneStations.Contains(type);
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

        /// <summary>The clothes the player wears, by number (see PlayerOutfit): 0 is the everyday clothes, 3 the swimsuit.</summary>
        public int Outfit { get; private set; }

        /// <summary>Changes the clothes. The swimsuit can only be worn once it was bought.</summary>
        public bool Wear(int outfit, int swimsuit)
        {
            if (outfit < 0 || (outfit == swimsuit && !HasSwimsuit))
            {
                return false;
            }

            Outfit = outfit;
            return true;
        }

        public bool SwamThisMonth { get; private set; }

        /// <summary>True once the player slept in the bed this month. Skipping it costs a little mood when the month closes.</summary>
        public bool SleptThisMonth { get; private set; }

        public bool CookedThisMonth { get; private set; }

        /// <summary>Days of the month that are gone (a night in jail, a day in the hospital): their tasks count as not done.</summary>
        public void LoseTasks(int days)
        {
            TasksDone = Math.Min(GameRules.TasksPerMonth, TasksDone + Math.Max(0, days) * GameRules.TasksPerDay);
        }

        // ---------------------------------------------------------------- things that happen to the player

        public ChoiceOutcome ArrestPlayer()
        {
            var before = State.Clone();
            FinanceEngine.ChargeFine(State, MarketAssumptions.TrespassFine);
            FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.JailWellbeingLoss);
            JailDaysRemaining = JailSentence.TermDays(Arrests);
            LoseTasks(JailDaysRemaining);
            Arrests++;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Te arrestaron por entrar a una casa ajena"));
        }

        /// <summary>The bail for what is left of the term: it is paid from cash, or from the card when there is not enough.</summary>
        public ChoiceOutcome PayBail(long cost)
        {
            var before = State.Clone();
            FinanceEngine.ChargeFine(State, cost);
            JailDaysRemaining = 0;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Pagaste la fianza"));
        }

        /// <summary>Serves one day of the sentence by sleeping on the cell cot.</summary>
        public ChoiceOutcome SleepInJail()
        {
            var before = State.Clone();
            if (JailDaysRemaining > 0)
            {
                JailDaysRemaining--;
            }

            NextMorning();
            Decisions++;
            return OutcomeFrom(before, Loc.T("Cumpliste un día de condena en la celda"));
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
            int rest = MarketAssumptions.RestWellbeing + (Holdings.Has(FurnitureKind.Bed) ? MarketAssumptions.BedRestBonus : 0);
            FinanceEngine.ChangeWellbeing(State, rest);
            SleptThisMonth = true;
            Decisions++;
            return OutcomeFrom(before, Loc.T("Dormiste en tu cama"));
        }

        /// <summary>
        /// A night in the own bed: the clock skips to the next morning. The rest for the mood comes once a month (see
        /// <see cref="Rest"/>), so it returns null on the other nights.
        /// </summary>
        public ChoiceOutcome Sleep()
        {
            var outcome = Rest();
            NextMorning();
            return outcome;
        }

        /// <summary>The day ended away from the bed: the player falls asleep where they are and wakes up tired.</summary>
        public ChoiceOutcome FallAsleep()
        {
            var before = State.Clone();
            FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.SkippedSleepWellbeingLoss);
            NextMorning();
            Decisions++;
            return OutcomeFrom(before, Loc.T(PlayerProfile.Pick("Te quedaste dormido fuera de la cama", "Te quedaste dormida fuera de la cama")));
        }

        private float shiftStartMinutes;

        /// <summary>The player clocks in now. The time of the morning goes by while the shift plays (see <see cref="TickShift"/>).</summary>
        public ClockInResult ClockIn()
        {
            shiftStartMinutes = Clock.Minutes;
            return Job.ClockIn(Clock.Minutes);
        }

        /// <summary>
        /// While the player works, the clock moves through the morning little by little: from clocking in to the end of the
        /// shift over <see cref="GameRules.ShiftRealSeconds"/>. It never passes the end of the shift.
        /// </summary>
        public void TickShift(float realSeconds)
        {
            if (!Job.OnShift || realSeconds <= 0f)
            {
                return;
            }

            float end = GameRules.WorkEndHour * 60f;
            float perSecond = Math.Max(0f, end - shiftStartMinutes) / GameRules.ShiftRealSeconds;
            Clock.SkipTo(Math.Min(end, Clock.Minutes + perSecond * realSeconds));
        }

        /// <summary>The work of the morning is done: the clock moves on to the end of the shift.</summary>
        public void FinishShift()
        {
            if (Job.FinishShift())
            {
                Clock.SkipTo(GameRules.WorkEndHour * 60f);
            }
        }

        public void PayMidMonthQuincena()
        {
            if (QuincenaPaidThisMonth > 0L)
            {
                return;
            }

            long half = State.SalaryNet / 2L;
            if (half > 0L)
            {
                FinanceEngine.CreditCash(State, half);
                QuincenaPaidThisMonth = half;
                State.RecordHistory("sinpe_quincena");
                QuincenaPaid?.Invoke(half);
            }
        }

        /// <summary>Only a night that starts a new week closes the working day: the last week waits at the bank.</summary>
        private void NextMorning()
        {
            Courses.AttendedToday = false;
            int previousDay = Clock.Day;
            if (Clock.SleepToMorning())
            {
                // A new week: two more errands, and the places of last week can be visited again.
                TasksToday = 0;
                doneStations.Clear();
                EndWorkingDay();
                if (previousDay == 2)
                {
                    PayMidMonthQuincena();
                }

                if (Clock.IsLastDay)
                {
                    Bills.Arrive(BillAmount);
                }
            }
        }

        /// <summary>Why this class cannot be taken now, or None.</summary>
        public ClassRefusal CheckClass(CourseKind kind)
        {
            if (Courses.Done(kind))
            {
                return ClassRefusal.AlreadyDone;
            }

            if (Clock.Hour < Career.AfternoonStartHour || Clock.Hour >= Career.AfternoonEndHour)
            {
                return ClassRefusal.OutsideTheAfternoon;
            }

            if (Courses.AttendedToday)
            {
                return ClassRefusal.AlreadyToday;
            }

            return !Courses.Enrolled(kind) && State.Cash < Career.Course(kind).Price ? ClassRefusal.NotEnoughCash : ClassRefusal.None;
        }

        /// <summary>Goes to a class: the course is paid when enrolling, and the class takes the afternoon.</summary>
        public bool TakeClass(CourseKind kind)
        {
            if (CheckClass(kind) != ClassRefusal.None)
            {
                return false;
            }

            if (!Courses.Enrolled(kind) && !FinanceEngine.TrySpendCash(State, Career.Course(kind).Price))
            {
                return false;
            }

            Courses.Attend(kind);
            Clock.SkipTo(Clock.Minutes + Career.ClassMinutes);
            State.RecordHistory("took_course");
            Decisions++;
            return true;
        }

        /// <summary>True when the days on time and the course of the next position are done.</summary>
        public bool CanBePromoted
        {
            get
            {
                if (Job.Position >= Career.TopPosition)
                {
                    return false;
                }

                var next = Job.Position + 1;
                return Job.PunctualDays >= Career.PunctualDaysFor(next) && Courses.Done(Career.CourseFor(next));
            }
        }

        /// <summary>Moves up one position: the new net salary comes from the finance engine, with its legal deductions.</summary>
        public bool Promote()
        {
            if (!CanBePromoted)
            {
                return false;
            }

            Job.MoveUp();
            State.SalaryNet = FinanceEngine.CalculateNetSalary(Career.GrossSalary(Job.Position));
            FinanceEngine.ChangeWellbeing(State, MarketAssumptions.PromotionWellbeing);
            State.RecordHistory("promoted");
            return true;
        }

        private long BillAmount(BillKind kind)
        {
            return MarketAssumptions.BillAmount(kind, Holdings);
        }

        /// <summary>Moves cash to the emergency fund. Returns what moved.</summary>
        public long PutInFund(long amount)
        {
            return FinanceEngine.ContributeToEmergencyFund(State, amount, false);
        }

        /// <summary>Pays down the card outside the monthly cycle. Returns what was paid.</summary>
        public long PayTowardCard(long amount)
        {
            return FinanceEngine.PayTowardCard(State, amount);
        }

        /// <summary>Pays one bill out of cash. False when nothing is owed for it or the cash is not enough.</summary>
        public bool PayBill(BillKind kind)
        {
            var bill = Bills.Find(kind);
            if (bill == null || !FinanceEngine.TryPayBill(State, bill.Amount))
            {
                return false;
            }

            Bills.Settle(kind);
            Decisions++;
            return true;
        }

        /// <summary>Pays every bill the cash reaches, in order. Returns how many were paid.</summary>
        public int PayAllBills()
        {
            int paid = 0;
            foreach (var bill in new List<BillDue>(Bills.Due))
            {
                if (PayBill(bill.Kind))
                {
                    paid++;
                }
            }

            return paid;
        }

        private void EndWorkingDay()
        {
            if (Job.EndDay())
            {
                FinanceEngine.ChangeWellbeing(State, -MarketAssumptions.MissedWorkWellbeingLoss);
            }
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
            var outcome = OutcomeFrom(before, Loc.T("Cocinaste en casa"));
            outcome.Note = Loc.F("Comer afuera te iba a costar unos {0}. Como cocinaste, esa plata no salió de tu bolsa: por eso aparece como efectivo de más.", Money.Format(MarketAssumptions.HomeCookingSaving));
            return outcome;
        }

        /// <summary>The cash machine: money from the savings account to cash. Null when the account is empty.</summary>
        public ChoiceOutcome WithdrawSavings(long amount)
        {
            if (amount <= 0L || State.Savings <= 0L)
            {
                return null;
            }

            var before = State.Clone();
            long moved = FinanceEngine.WithdrawSavings(State, amount);
            var outcome = OutcomeFrom(before, Loc.F("Sacaste {0} del cajero", Money.Format(moved)));
            outcome.Note = Loc.T("La plata pasó de tu cuenta de ahorro a tu bolsa. Lo que tenés no cambió, solo dónde está.");
            return outcome;
        }

        /// <summary>The deposit window: cash into the savings account. Null when there is no cash.</summary>
        public ChoiceOutcome DepositCash(long amount)
        {
            if (amount <= 0L || State.Cash <= 0L)
            {
                return null;
            }

            var before = State.Clone();
            long moved = FinanceEngine.DepositCash(State, amount);
            var outcome = OutcomeFrom(before, Loc.F("Depositaste {0} en tu cuenta", Money.Format(moved)));
            outcome.Note = Loc.T("En la cuenta la plata no se gasta por impulso y está segura si perdés la billetera.");
            return outcome;
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
                TasksToday++;
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

        public AssetPurchase BuyFurniture(FurnitureModel item)
        {
            return Buy(Loc.F("Comprar {0}", Loc.T(item.Name)), () => FinanceEngine.TryBuyFurniture(State, Holdings, item.Kind, item.Price));
        }

        public AssetPurchase BuyFurnitureInInstallments(FurnitureModel item)
        {
            var offer = item.ToSellerOffer();
            return Buy(Loc.F("Comprar {0}", Loc.T(item.Name)), () => FinanceEngine.TryBuyFurnitureInInstallments(State, Holdings, item.Kind, item.Name, offer.MonthlyCharge, offer.TermMonths, offer.Commission));
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

        /// <summary>Why this piece cannot be paid cash down right now, or None. Nothing changes.</summary>
        public PurchaseFailure CheckBuyFurniture(FurnitureModel item)
        {
            return Probe((state, holdings) => FinanceEngine.TryBuyFurniture(state, holdings, item.Kind, item.Price));
        }

        /// <summary>Why this piece cannot be signed in installments right now, or None. Nothing changes.</summary>
        public PurchaseFailure CheckBuyFurnitureInInstallments(FurnitureModel item)
        {
            var offer = item.ToSellerOffer();
            return Probe((state, holdings) => FinanceEngine.TryBuyFurnitureInInstallments(state, holdings, item.Kind, item.Name, offer.MonthlyCharge, offer.TermMonths, offer.Commission));
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

            foreach (var kind in Holdings.Furniture)
            {
                probe.AddFurniture(kind);
            }

            return purchase(State.Clone(), probe).Failure;
        }

        /// <summary>Pays the salary and runs the month: installments, the card cycle, interest and living expenses.</summary>
        public MonthlyTickReport CloseMonth(bool dockRemainingShifts = false)
        {
            LastRunningCosts = Holdings.MonthlyRunningCosts;
            LastRoomRent = Holdings.Home == null ? MarketAssumptions.RoomRent : 0L;

            // The week in course counts once its morning is over: closing the month early in the morning is not an absence for today.
            if (Clock.Hour >= GameRules.WorkEndHour)
            {
                EndWorkingDay();
            }

            int missedShifts = Job.MissedThisMonth;
            if (dockRemainingShifts && Clock.Day < GameRules.DaysPerMonth)
            {
                int unplayedDays = GameRules.DaysPerMonth - Clock.Day;
                int todayUnworked = !Job.WorkedToday ? 1 : 0;
                missedShifts += unplayedDays + todayUnworked;
            }

            var report = FinanceEngine.RunMonthlyTick(State, new MonthlyTickOptions
            {
                FixedLivingExpensesColones = GameRules.LivingExpenses + LastRoomRent + LastRunningCosts,
                SalaryDockedColones = FinanceEngine.CalculateDockedPay(State.SalaryNet, Job.DockedLates, missedShifts),
                SalaryAlreadyPaidColones = QuincenaPaidThisMonth
            });

            QuincenaPaidThisMonth = 0L;
            MonthsPlayed++;
            TasksDone = 0;
            TasksToday = 0;
            HurtThisMonth = false;
            SwamThisMonth = false;
            SleptThisMonth = false;
            CookedThisMonth = false;
            if (Bills.CloseMonth(BillAmount) > 0L)
            {
                State.RecordHistory("bills_unpaid");
            }

            Job.StartMonth();
            Courses.AttendedToday = false;
            Clock.StartMonth();
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

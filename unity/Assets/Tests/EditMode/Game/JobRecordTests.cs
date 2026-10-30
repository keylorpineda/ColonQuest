using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class JobRecordTests
    {
        private const float OnTime = 7f * 60f;
        private const float InGrace = 7f * 60f + 15f;
        private const float Late = 7f * 60f + 16f;
        private const float Noon = 12f * 60f;

        private static void LateDay(JobRecord job)
        {
            job.ClockIn(Late);
            job.FinishShift();
            job.EndDay();
        }

        [Test]
        public void ClockIn_InsideTheGraceIsOnTime()
        {
            Assert.AreEqual(ClockInResult.OnTime, new JobRecord().ClockIn(OnTime));
            Assert.AreEqual(ClockInResult.OnTime, new JobRecord().ClockIn(InGrace));
        }

        [Test]
        public void ClockIn_TheLateArrivalsGetWorse()
        {
            var job = new JobRecord();
            Assert.AreEqual(ClockInResult.LateWarned, job.ClockIn(Late));
            job.EndDay();
            Assert.AreEqual(0, job.DockedLates, "the first one is only a warning");

            Assert.AreEqual(ClockInResult.LateDocked, job.ClockIn(Late));
            job.EndDay();
            Assert.AreEqual(1, job.DockedLates);

            Assert.AreEqual(ClockInResult.LateLastWarning, job.ClockIn(Late));
            Assert.AreEqual(2, job.DockedLates);
        }

        [Test]
        public void ClockIn_OnlyOnceADayAndOnlyInTheMorning()
        {
            var job = new JobRecord();
            Assert.AreEqual(ClockInResult.AfterMorning, job.ClockIn(Noon));
            Assert.IsFalse(job.WorkedToday);

            job.ClockIn(OnTime);
            Assert.AreEqual(ClockInResult.AlreadyWorked, job.ClockIn(OnTime + 1f));
            Assert.AreEqual(1, job.DaysWorked);
        }

        [Test]
        public void EndDay_WithoutComingCountsAsMissed()
        {
            var job = new JobRecord();
            Assert.IsTrue(job.EndDay());
            Assert.AreEqual(1, job.MissedThisMonth);

            job.ClockIn(OnTime);
            Assert.IsFalse(job.EndDay());
            Assert.AreEqual(1, job.MissedThisMonth);
        }

        [Test]
        public void StartMonth_ClearsTheWarningsAndKeepsTheDaysWorked()
        {
            var job = new JobRecord();
            LateDay(job);
            LateDay(job);
            job.EndDay();
            job.StartMonth();

            Assert.AreEqual(0, job.LatesThisMonth);
            Assert.AreEqual(0, job.MissedThisMonth);
            Assert.AreEqual(2, job.DaysWorked);
        }

        [Test]
        public void Salary_IsTheNetOfTheLegalMinimum()
        {
            Assert.AreEqual(373_092L, GameRules.MinimumWageGross);
            Assert.AreEqual(FinanceEngine.CalculateNetSalary(GameRules.MinimumWageGross), GameRules.NewPlayer().SalaryNet);
        }

        [Test]
        public void DockedPay_IsADayForALateArrivalAndAWeekForAnAbsence()
        {
            Assert.AreEqual(0L, FinanceEngine.CalculateDockedPay(300_000L, 0, 0));
            Assert.AreEqual(10_000L, FinanceEngine.CalculateDockedPay(300_000L, 1, 0));
            Assert.AreEqual(75_000L, FinanceEngine.CalculateDockedPay(300_000L, 0, 1));
            Assert.AreEqual(300_000L, FinanceEngine.CalculateDockedPay(300_000L, 3, 5), "never more than the salary");
        }

        [Test]
        public void Tick_PaysTheSalaryLessWhatIsDocked()
        {
            var state = new PlayerState { Month = 1, SalaryNet = 300_000L, Cash = 0L };
            var report = FinanceEngine.RunMonthlyTick(state, new MonthlyTickOptions { SalaryDockedColones = 10_000L });

            Assert.AreEqual(290_000L, report.SalaryCredited);
            Assert.AreEqual(10_000L, report.SalaryDocked);
            Assert.AreEqual(290_000L, state.Cash);
        }

        [Test]
        public void Loop_WorkingTheShiftMovesTheClockToNoon()
        {
            var loop = new GameLoop();
            Assert.AreEqual(ClockInResult.OnTime, loop.ClockIn());
            loop.FinishShift();

            Assert.AreEqual(12, loop.Clock.Hour);
            Assert.IsFalse(loop.Job.OnShift);

            loop.FinishShift();
            Assert.AreEqual(12, loop.Clock.Hour, "without a shift nothing moves");
        }

        [Test]
        public void Loop_ANightWithoutWorkingLowersTheMoodAndDocksAWeek()
        {
            var loop = new GameLoop();
            int mood = loop.State.Wellbeing;
            long salary = loop.State.SalaryNet;

            loop.Sleep();
            Assert.AreEqual(1, loop.Job.MissedThisMonth);
            Assert.AreEqual(mood + MarketAssumptions.RestWellbeing - MarketAssumptions.MissedWorkWellbeingLoss, loop.State.Wellbeing);

            var report = loop.CloseMonth();
            Assert.AreEqual(FinanceEngine.CalculateDockedPay(salary, 0, 1), report.SalaryDocked);
            Assert.AreEqual(0, loop.Job.MissedThisMonth);
        }

        [Test]
        public void Loop_ClosingTheMonthEarlyInTheMorningIsNotAnAbsence()
        {
            var loop = new GameLoop();
            var report = loop.CloseMonth();

            Assert.AreEqual(0L, report.SalaryDocked);
            Assert.AreEqual(loop.State.SalaryNet, report.SalaryCredited);
        }

        [Test]
        public void Loop_TheLastWeekDoesNotCountEachNight()
        {
            var loop = new GameLoop();
            for (int i = 0; i < GameRules.DaysPerMonth + 2; i++)
            {
                loop.Sleep();
            }

            Assert.AreEqual(GameRules.DaysPerMonth - 1, loop.Job.MissedThisMonth);
        }

        [Test]
        public void Loop_TheJobIsSavedWithTheRun()
        {
            var loop = new GameLoop();
            loop.ClockIn();
            loop.FinishShift();

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.AreEqual(1, restored.Job.DaysWorked);
            Assert.IsTrue(restored.Job.WorkedToday);
            Assert.IsFalse(restored.Job.ExpectedAt(restored.Clock.Minutes));
        }

        [Test]
        public void TheTimeClock_IsNotATask()
        {
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.Work));
        }

        [TestCase("Reloj marcador de la soda")]
        [TestCase("¡Vas tarde al trabajo!")]
        [TestCase("Ayer no fui a trabajar. Eso me lo van a rebajar del salario.")]
        public void TheTextsOfTheJob_AreTranslated(string spanish)
        {
            Assert.AreNotEqual(spanish, Loc.Translate(spanish, GameLanguage.English));
        }
    }
}

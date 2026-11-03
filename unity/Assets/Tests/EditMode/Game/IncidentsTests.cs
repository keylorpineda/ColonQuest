using System;
using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class IncidentsTests
    {
        private static GameLoop Loop(long cash = 500_000L)
        {
            var loop = new GameLoop();
            loop.State.Cash = cash;
            loop.State.EmergencyFund = 0L;
            return loop;
        }

        // ---------------------------------------------------------------- the law

        [Test]
        public void Arrest_ChargesTheFineLosesDaysAndLowersTheMood()
        {
            var loop = Loop(500_000L);
            int mood = loop.State.Wellbeing;

            var outcome = loop.ArrestPlayer();

            Assert.AreEqual(500_000L - MarketAssumptions.TrespassFine, loop.State.Cash);
            Assert.AreEqual(-MarketAssumptions.TrespassFine, outcome.CashDelta);
            Assert.AreEqual(1, loop.JailDaysRemaining);
            Assert.AreEqual(Math.Min(GameRules.TasksPerMonth, loop.JailDaysRemaining * GameRules.TasksPerDay), loop.TasksDone);
            Assert.AreEqual(mood - MarketAssumptions.JailWellbeingLoss, loop.State.Wellbeing);
        }

        [Test]
        public void Arrest_WithoutCashTheFineGoesOnTheCard()
        {
            var loop = Loop(30_000L);
            long card = loop.State.CreditCard.Balance;

            loop.ArrestPlayer();

            Assert.AreEqual(0L, loop.State.Cash);
            Assert.AreEqual(card + MarketAssumptions.TrespassFine - 30_000L, loop.State.CreditCard.Balance);
        }

        [Test]
        public void LostDays_NeverPassTheTasksOfTheMonth()
        {
            var loop = Loop();
            loop.LoseTasks(2);
            loop.LoseTasks(5);

            Assert.AreEqual(GameRules.TasksPerMonth, loop.TasksDone);
            Assert.IsTrue(loop.AllTasksDone);
            Assert.IsFalse(loop.CanDoTask(StationType.Garage), "no task left to do this month");
        }

        [Test]
        public void LostDays_ANegativeNumberChangesNothing()
        {
            var loop = Loop();
            loop.LoseTasks(-3);
            Assert.AreEqual(0, loop.TasksDone);
        }

        // ---------------------------------------------------------------- hospital

        [Test]
        public void Hospital_WithoutInsuranceTheWholeBillIsPaid()
        {
            var loop = Loop(500_000L);

            loop.HospitalizePlayer();

            Assert.AreEqual(500_000L - MarketAssumptions.HospitalBill, loop.State.Cash);
            Assert.AreEqual(MarketAssumptions.HospitalDaysLost * GameRules.TasksPerDay, loop.TasksDone);
            Assert.IsTrue(loop.HurtThisMonth);
        }

        [Test]
        public void Hospital_WithInsuranceOnlyTheShareIsPaid()
        {
            var loop = Loop(500_000L);
            Assert.IsTrue(loop.BuyHealthInsurance());

            loop.HospitalizePlayer();

            long share = FinanceEngine.PatientShare(MarketAssumptions.HospitalBill, MarketAssumptions.InsuredPatientSharePct);
            Assert.AreEqual(500_000L - share, loop.State.Cash);
            Assert.Less(share, MarketAssumptions.HospitalBill);
        }

        [Test]
        public void Hospital_AnEmergencyFundTakesTheBlowInsteadOfCash()
        {
            var loop = Loop(100_000L);
            loop.State.EmergencyFund = 450_000L;

            var outcome = loop.HospitalizePlayer();

            Assert.AreEqual(100_000L, loop.State.Cash);
            Assert.AreEqual(450_000L - MarketAssumptions.HospitalBill, loop.State.EmergencyFund);
            Assert.AreEqual(-MarketAssumptions.HospitalBill, outcome.EmergencyFundDelta);
        }

        [Test]
        public void Hospital_TheCountOfHurtMonthsResetsWhenTheMonthCloses()
        {
            var loop = Loop();
            loop.HospitalizePlayer();
            Assert.IsTrue(loop.HurtThisMonth);

            loop.CloseMonth();

            Assert.IsFalse(loop.HurtThisMonth);
        }

        // ---------------------------------------------------------------- health insurance

        [Test]
        public void HealthInsurance_CanBeTakenOnlyOnceAndChargesEveryMonth()
        {
            var loop = Loop();
            Assert.IsTrue(loop.BuyHealthInsurance());
            Assert.IsFalse(loop.BuyHealthInsurance(), "one policy is enough");
            Assert.AreEqual(MarketAssumptions.HealthInsurancePremium, loop.Holdings.MonthlyRunningCosts);

            loop.State.Cash = 1_000_000L;
            loop.State.Installments.Clear();
            loop.State.CreditCard.Balance = 0L;
            var withoutPolicy = Loop(1_000_000L);
            withoutPolicy.State.Installments.Clear();
            withoutPolicy.State.CreditCard.Balance = 0L;
            loop.CloseMonth();
            withoutPolicy.CloseMonth();

            Assert.AreEqual(MarketAssumptions.HealthInsurancePremium, withoutPolicy.State.Cash - loop.State.Cash, "the policy takes its premium out of the month");
        }

        [Test]
        public void Holdings_AreNoLongerEmptyWithAPolicy()
        {
            var holdings = new Holdings();
            Assert.IsTrue(holdings.IsEmpty);

            holdings.SetHealthInsurance(16_000L);

            Assert.IsFalse(holdings.IsEmpty);
            Assert.IsTrue(holdings.HasHealthInsurance);
        }

        [Test]
        public void HealthDesk_IsNotATask()
        {
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.HealthDesk));
        }

        // ---------------------------------------------------------------- collectors

        private static GameLoop Owing(long cash, long loan)
        {
            var loop = Loop(cash);
            loop.State.InformalLoans.Add(new InformalLoan("Prestamista", loan, 15m));
            return loop;
        }

        [Test]
        public void Arrears_OnlyAnInformalLoanCounts()
        {
            Assert.IsFalse(CollectionRules.InArrears(GameRules.NewPlayer()));

            var full = GameRules.NewPlayer();
            full.CreditCard.Balance = full.CreditCard.Limit;
            Assert.IsFalse(CollectionRules.InArrears(full), "a full card brings interest, not the black car");

            var loan = GameRules.NewPlayer();
            loan.InformalLoans.Add(new InformalLoan("Prestamista", 50_000L, 15m));
            Assert.IsTrue(CollectionRules.InArrears(loan));
        }

        [Test]
        public void Demand_IsAShareOfTheLoansInsideTheLimitsAndNeverMoreThanOwed()
        {
            var small = Owing(0L, 5_000L);
            Assert.AreEqual(5_000L, CollectionRules.Demand(small.State), "the minimum never passes what is owed");

            var low = Owing(0L, 40_000L);
            Assert.AreEqual(MarketAssumptions.CollectionMinDemand, CollectionRules.Demand(low.State));

            var middle = Owing(0L, 400_000L);
            middle.State.CreditCard.Balance = 3_000_000L;
            Assert.AreEqual(100_000L, CollectionRules.Demand(middle.State), "the card does not count");

            var huge = Owing(0L, 3_000_000L);
            Assert.AreEqual(MarketAssumptions.CollectionMaxDemand, CollectionRules.Demand(huge.State));
        }

        [Test]
        public void Collectors_PayingLowersTheDebtAndIgnoringRaisesIt()
        {
            var pay = Owing(500_000L, 280_000L);
            pay.PayCollectors();
            Assert.Less(FinanceEngine.DebtOwed(pay.State), 280_000L);

            var ignore = Owing(500_000L, 280_000L);
            ignore.IgnoreCollectors();
            Assert.Greater(FinanceEngine.DebtOwed(ignore.State), 280_000L);
            Assert.AreEqual(500_000L, ignore.State.Cash);
        }

        [Test]
        public void Collectors_NegotiatingLowersTheDebtWithCashAndLeavesANewLoan()
        {
            var loop = Owing(500_000L, 280_000L);

            var outcome = loop.NegotiateCollectors();

            Assert.Less(outcome.InformalDebtDelta, 0L);
            Assert.IsTrue(loop.State.InformalLoans.Exists(l => l.Lender == "Cobradores"));
        }

        [Test]
        public void Collectors_ComeOnceAMonth()
        {
            var loop = Owing(500_000L, 280_000L);
            Assert.IsTrue(loop.CollectorsDue);

            loop.IgnoreCollectors();
            Assert.IsFalse(loop.CollectorsDue, "not again this month");
            Assert.IsTrue(loop.InArrears, "the debt is still there");

            loop.CloseMonth();
            Assert.IsTrue(loop.CollectorsDue, "next month they are back");
        }

        [Test]
        public void Collectors_TheirVisitOfTheMonthSurvivesASavedGame()
        {
            var loop = Owing(500_000L, 280_000L);
            loop.PayCollectors();

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.IsFalse(restored.CollectorsDue);
        }

        [Test]
        public void Lender_TakesAPaymentByChoice()
        {
            var loop = Owing(50_000L, 80_000L);

            var outcome = loop.PayLender();

            Assert.AreEqual(-50_000L, outcome.CashDelta);
            Assert.AreEqual(-50_000L, outcome.InformalDebtDelta);
            Assert.IsNull(Owing(0L, 80_000L).PayLender(), "no cash, no payment");
            Assert.IsNull(Loop(50_000L).PayLender(), "nothing owed, nothing to pay");
        }

        [Test]
        public void Lender_QuotesTheRatesTheEngineCharges()
        {
            var state = Loop(0L).State;
            FinanceEngine.ApplyEffect(state, EventType.Offer, new Effect(EffectKind.NewInformalLoan, 50_000L, null, DebtTalk.ExampleMonths));
            FinanceEngine.ApplyEffect(state, EventType.Offer, new Effect(EffectKind.NewInformalLoan, 50_000L, null, DebtTalk.ExampleMonths + 1));

            Assert.AreEqual((decimal)DebtTalk.LenderRatePct, state.InformalLoans[0].MonthlyRatePct);
            Assert.AreEqual((decimal)DebtTalk.LenderLongRatePct, state.InformalLoans[1].MonthlyRatePct);
            var request = new DebtTalk(TalkPartner.Lender).ToRequest("rate", "en");
            CollectionAssert.Contains(request.percents, DebtTalk.LenderLongRatePct);
        }
    }
}

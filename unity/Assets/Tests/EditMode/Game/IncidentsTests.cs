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
        public void RedLightFine_ChargesTheFineAndALittleMoodButNoJailAndNoLostDays()
        {
            var loop = Loop(500_000L);
            int mood = loop.State.Wellbeing;

            var outcome = loop.FineRedLight();

            Assert.AreEqual(500_000L - MarketAssumptions.RedLightFine, loop.State.Cash);
            Assert.AreEqual(-MarketAssumptions.RedLightFine, outcome.CashDelta);
            Assert.AreEqual(0, loop.TasksDone);
            Assert.AreEqual(mood - MarketAssumptions.RedLightWellbeingLoss, loop.State.Wellbeing);
        }

        [Test]
        public void RedLightFine_WithoutCashGoesOnTheCard()
        {
            var loop = Loop(10_000L);
            long card = loop.State.CreditCard.Balance;

            loop.FineRedLight();

            Assert.AreEqual(0L, loop.State.Cash);
            Assert.AreEqual(card + MarketAssumptions.RedLightFine - 10_000L, loop.State.CreditCard.Balance);
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

        [Test]
        public void Arrears_ANewPlayerWithACardAtSixtyPercentIsNotInArrears()
        {
            Assert.IsFalse(CollectionRules.InArrears(GameRules.NewPlayer()));
        }

        [Test]
        public void Arrears_AnInformalLoanOrAFullCardIs()
        {
            var loan = GameRules.NewPlayer();
            loan.InformalLoans.Add(new InformalLoan("Prestamista", 50_000L, 15m));
            Assert.IsTrue(CollectionRules.InArrears(loan));

            var full = GameRules.NewPlayer();
            full.CreditCard.Balance = 270_000L;
            Assert.IsTrue(CollectionRules.InArrears(full), "90 percent of the limit");

            full.CreditCard.Balance = 269_999L;
            Assert.IsFalse(CollectionRules.InArrears(full));
        }

        [Test]
        public void Demand_IsAShareOfTheDebtInsideTheLimits()
        {
            var small = GameRules.NewPlayer();
            small.CreditCard.Balance = 10_000L;
            Assert.AreEqual(MarketAssumptions.CollectionMinDemand, CollectionRules.Demand(small));

            var middle = GameRules.NewPlayer();
            middle.CreditCard.Balance = 400_000L;
            Assert.AreEqual(100_000L, CollectionRules.Demand(middle));

            var huge = GameRules.NewPlayer();
            huge.CreditCard.Balance = 3_000_000L;
            Assert.AreEqual(MarketAssumptions.CollectionMaxDemand, CollectionRules.Demand(huge));
        }

        [Test]
        public void Collectors_PayingLowersTheDebtAndIgnoringRaisesIt()
        {
            var pay = Loop(500_000L);
            pay.State.CreditCard.Balance = 280_000L;
            long before = FinanceEngine.DebtOwed(pay.State);
            pay.PayCollectors();
            Assert.Less(FinanceEngine.DebtOwed(pay.State), before);

            var ignore = Loop(500_000L);
            ignore.State.CreditCard.Balance = 280_000L;
            long start = FinanceEngine.DebtOwed(ignore.State);
            ignore.IgnoreCollectors();
            Assert.Greater(FinanceEngine.DebtOwed(ignore.State), start);
            Assert.AreEqual(500_000L, ignore.State.Cash);
        }

        [Test]
        public void Collectors_NegotiatingLeavesANewInformalLoan()
        {
            var loop = Loop(500_000L);
            loop.State.CreditCard.Balance = 280_000L;

            var outcome = loop.NegotiateCollectors();

            Assert.Greater(outcome.InformalDebtDelta, 0L);
            Assert.IsTrue(loop.State.InformalLoans.Exists(l => l.Lender == "Cobradores"));
        }
    }
}

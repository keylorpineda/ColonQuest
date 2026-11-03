using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceFarmTests
    {
        [Test]
        public void ProjectFarm_AppliesTheThreeScenariosToTheInvestedMoney()
        {
            var projection = FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 36, -30, 25, 60);

            Assert.AreEqual(70_000L, projection.LowValue);
            Assert.AreEqual(125_000L, projection.BaseValue);
            Assert.AreEqual(160_000L, projection.HighValue);
            Assert.AreEqual(36, projection.MonthsToPayoff);
        }

        [Test]
        public void ProjectFarm_RoundsToTheNearestColon()
        {
            var projection = FinanceEngine.ProjectFarm(FarmKind.Cattle, 33_333L, 21, 0, 25, 25);

            Assert.AreEqual(41_666L, projection.BaseValue, "33 333 plus 8 333.25");
        }

        [Test]
        public void ProjectFarm_ALossOfEverythingLeavesZeroNotANegativeValue()
        {
            var projection = FinanceEngine.ProjectFarm(FarmKind.Cattle, 80_000L, 21, -100, 0, 10);

            Assert.AreEqual(0L, projection.LowValue);
        }

        [Test]
        public void ProjectFarm_RejectsBadInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.ProjectFarm(FarmKind.Coffee, 0L, 36, -30, 25, 60));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 0, -30, 25, 60));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 36, -101, 25, 60));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 36, 30, 25, 60), "low above base");
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 36, -30, 70, 60), "base above high");
        }

        [Test]
        public void MonthsLeftAfter_CountsDownFromThePurchaseAndStopsAtZero()
        {
            var projection = FinanceEngine.ProjectFarm(FarmKind.Coffee, 100_000L, 36, -30, 25, 60);

            Assert.AreEqual(33, projection.MonthsLeftAfter(3));
            Assert.AreEqual(36, projection.MonthsLeftAfter(-2));
            Assert.AreEqual(0, projection.MonthsLeftAfter(40));
        }

        [Test]
        public void TryInvestInFarm_TakesTheCashNowAndKeepsTheMoneyAsAStake()
        {
            var state = new PlayerState { Cash = 150_000L, Month = 2 };
            var holdings = new Holdings();

            var result = FinanceEngine.TryInvestInFarm(state, holdings, FarmKind.Coffee, 100_000L);

            Assert.IsTrue(result.Bought);
            Assert.AreEqual(50_000L, state.Cash);
            Assert.AreEqual(1, holdings.Farm.Count);
            Assert.AreEqual(FarmKind.Coffee, holdings.Farm[0].Kind);
            Assert.AreEqual(100_000L, holdings.Farm[0].Invested);
            Assert.AreEqual(1, holdings.Farm[0].MonthsPlayedWhenBought, "the second month, so one month was already closed");
            Assert.AreEqual(100_000L, holdings.AssetValue, "the farm is counted at cost until it pays off");
            Assert.Contains("invested_coffee", state.History);
        }

        [Test]
        public void TryInvestInFarm_AddsToTheStakeOfTheSameKindAndKeepsOtherKindsApart()
        {
            var state = new PlayerState { Cash = 400_000L };
            var holdings = new Holdings();

            FinanceEngine.TryInvestInFarm(state, holdings, FarmKind.Coffee, 50_000L);
            FinanceEngine.TryInvestInFarm(state, holdings, FarmKind.Coffee, 100_000L);
            FinanceEngine.TryInvestInFarm(state, holdings, FarmKind.Cattle, 100_000L);

            Assert.AreEqual(2, holdings.Farm.Count);
            Assert.AreEqual(150_000L, holdings.Farm[0].Invested);
            Assert.AreEqual(100_000L, holdings.Farm[1].Invested);
            Assert.AreEqual(250_000L, holdings.FarmInvested);
            Assert.AreEqual(150_000L, state.Cash);
        }

        [Test]
        public void TryInvestInFarm_ChangesNothingWhenCashIsNotEnough()
        {
            var state = new PlayerState { Cash = 99_999L };
            var holdings = new Holdings();

            var result = FinanceEngine.TryInvestInFarm(state, holdings, FarmKind.Cattle, 100_000L);

            Assert.AreEqual(PurchaseFailure.NotEnoughCash, result.Failure);
            Assert.AreEqual(99_999L, state.Cash);
            Assert.IsTrue(holdings.IsEmpty);
            Assert.AreEqual(0L, state.CreditCard.Balance, "an investment never goes on the card");
        }

        [Test]
        public void TryInvestInFarm_RejectsAmountsThatAreNotPositive()
        {
            var state = new PlayerState { Cash = 100_000L };

            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.TryInvestInFarm(state, new Holdings(), FarmKind.Coffee, 0L));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.TryInvestInFarm(state, new Holdings(), FarmKind.Coffee, -5L));
        }

        [Test]
        public void Holdings_AssetValueAddsOwnedHomeCarAndFarmButNotARental()
        {
            var holdings = new Holdings();
            Assert.IsTrue(holdings.IsEmpty);

            holdings.SetHome(new HomeHolding("Casa", true, 30_000_000L, 300_000L, 120));
            holdings.SetCar(new CarHolding("Carro", 4_000_000L, 50_000L));
            holdings.AddFarm(FarmKind.Coffee, 100_000L, 0);

            Assert.AreEqual(34_100_000L, holdings.AssetValue);

            var renter = new Holdings();
            renter.SetHome(new HomeHolding("Casa", false, 0L, 250_000L, 12));
            Assert.AreEqual(0L, renter.AssetValue);
            Assert.IsFalse(renter.IsEmpty);
        }
    }
}

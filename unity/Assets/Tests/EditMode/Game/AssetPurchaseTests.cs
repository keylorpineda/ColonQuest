using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class AssetPurchaseTests
    {
        private GameLoop loop;

        [SetUp]
        public void SetUp()
        {
            loop = new GameLoop();
        }

        private static CarModel Compact => MarketAssumptions.FindCar("compacto");

        [Test]
        public void MarketPlacesAreNotTasks()
        {
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.RealEstate));
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.CarDealer));
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.Farm));
            Assert.IsFalse(loop.CanDoTask(StationType.Farm));
        }

        [Test]
        public void RentHome_TakesTheDepositStartsTheRentAndKeepsTheTasksAsTheyWere()
        {
            loop.State.Cash = 1_000_000L;

            var purchase = loop.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(-260_000L, purchase.Outcome.CashDelta);
            Assert.AreEqual(260_000L, purchase.Outcome.InstallmentsPerMonthDelta);
            Assert.AreEqual("Alquilar Casa Vecina D", purchase.Outcome.ChoiceLabel);
            Assert.AreEqual(1, loop.Decisions);
            Assert.AreEqual(0, loop.TasksDone);
            Assert.IsNotNull(loop.Holdings.Home);
        }

        [Test]
        public void RentHome_WithoutTheDepositChangesNothing()
        {
            var purchase = loop.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));

            Assert.IsFalse(purchase.Result.Bought);
            Assert.AreEqual(PurchaseFailure.NotEnoughCash, purchase.Result.Failure);
            Assert.IsTrue(purchase.Outcome.ChangedNothing);
            Assert.AreEqual(0, loop.Decisions);
            Assert.AreEqual(120_000L, loop.State.Cash);
        }

        [Test]
        public void BuyHome_NeedsAFortuneAtTheStartAndWorksWithOne()
        {
            var house = MarketAssumptions.FindHome("casa_vecina_b");
            Assert.AreEqual(PurchaseFailure.NotEnoughCash, loop.CheckBuyHome(house), "the player starts with ₡120 000");

            loop.State.Cash = 10_000_000L;
            var quote = MarketAssumptions.QuoteMortgage(house);
            var purchase = loop.BuyHome(house);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(10_000_000L - quote.UpfrontCost, loop.State.Cash);
            Assert.AreEqual(quote.MonthlyPayment, purchase.Outcome.InstallmentsPerMonthDelta);
            Assert.AreEqual(MarketAssumptions.MortgageTermMonths, loop.State.Installments[loop.State.Installments.Count - 1].MonthsLeft);
        }

        [Test]
        public void NetWorth_CountsTheOwnedHomeButTheMortgageStillWeighs()
        {
            loop.State.Cash = 10_000_000L;
            var house = MarketAssumptions.FindHome("casa_vecina_b");
            long before = GameRules.NetWorth(loop.State, loop.Holdings);

            loop.BuyHome(house);

            long after = GameRules.NetWorth(loop.State, loop.Holdings);
            var quote = MarketAssumptions.QuoteMortgage(house);
            Assert.AreEqual(before - quote.UpfrontCost - quote.MonthlyPayment * quote.TermMonths + house.Price, after,
                "the house adds its price, the cash and the whole mortgage leave");
            Assert.Less(after, before, "owning on credit costs more than the house is worth: the bank's interest and the closing costs");
        }

        [Test]
        public void BuyCar_PaysDownPaymentAndCommissionAndPutsTheMarchamoOnCashWhenItFits()
        {
            loop.State.Cash = 1_000_000L;
            var car = Compact;

            var purchase = loop.BuyCar(car);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(1_000_000L - car.DownPayment - car.Commission - car.MarchamoFirstYear, loop.State.Cash);
            Assert.AreEqual(0L, purchase.Outcome.CardDelta);
            Assert.AreEqual(FinanceEngine.QuoteCar(car.ToPlan()).MonthlyInstallment, purchase.Outcome.InstallmentsPerMonthDelta);
            Assert.AreEqual(car.RunningCostsMonthly, loop.Holdings.MonthlyRunningCosts);
        }

        [Test]
        public void BuyCar_TheMarchamoThatDoesNotFitGoesOnTheCardAndIsReported()
        {
            var car = Compact;
            long onlyTheUpfront = car.DownPayment + car.Commission + 34_000L;
            loop.State.Cash = onlyTheUpfront;

            var purchase = loop.BuyCar(car);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(car.MarchamoFirstYear - 34_000L, purchase.Result.FinancedOnCard);
            Assert.AreEqual(purchase.Result.FinancedOnCard, purchase.Outcome.CardDelta);
            Assert.AreEqual(0L, loop.State.Cash);
        }

        [Test]
        public void BuyCar_ASecondCarIsRefused()
        {
            loop.State.Cash = 3_000_000L;
            loop.BuyCar(Compact);

            Assert.AreEqual(PurchaseFailure.AlreadyHasCar, loop.CheckBuyCar(MarketAssumptions.FindCar("sedan")));
        }

        [Test]
        public void Check_NeverChangesTheStateOrTheHoldings()
        {
            loop.State.Cash = 1_000_000L;
            int installments = loop.State.Installments.Count;
            int history = loop.State.History.Count;

            Assert.AreEqual(PurchaseFailure.None, loop.CheckBuyCar(Compact));
            Assert.AreEqual(PurchaseFailure.None, loop.CheckRent(MarketAssumptions.FindHome("casa_vecina_d")));

            Assert.AreEqual(1_000_000L, loop.State.Cash);
            Assert.AreEqual(installments, loop.State.Installments.Count);
            Assert.AreEqual(history, loop.State.History.Count);
            Assert.IsTrue(loop.Holdings.IsEmpty);
        }

        [Test]
        public void Check_ReportsAHomeAlreadyHeld()
        {
            loop.State.Cash = 1_000_000L;
            var rental = MarketAssumptions.FindHome("casa_vecina_d");
            loop.RentHome(rental);

            Assert.AreEqual(PurchaseFailure.AlreadyHasHome, loop.CheckRent(MarketAssumptions.FindHome("casa_almendros_2")));
        }

        [Test]
        public void InvestInFarm_TakesTheCashAndLeavesNoWayBackDuringTheRun()
        {
            loop.State.Cash = 300_000L;
            var coffee = MarketAssumptions.FindFarm(FarmKind.Coffee);

            var purchase = loop.InvestInFarm(coffee, 100_000L);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(-100_000L, purchase.Outcome.CashDelta);
            Assert.AreEqual(100_000L, loop.Holdings.FarmInvested);
            Assert.AreEqual("Invertir en café", purchase.Outcome.ChoiceLabel);
        }

        [Test]
        public void InvestInFarm_WithoutCashChangesNothing()
        {
            var purchase = loop.InvestInFarm(MarketAssumptions.FindFarm(FarmKind.Cattle), 200_000L);

            Assert.IsFalse(purchase.Result.Bought);
            Assert.AreEqual(120_000L, loop.State.Cash);
            Assert.IsTrue(loop.Holdings.IsEmpty);
        }

        [Test]
        public void CloseMonth_ChargesTheCarsFuelAndUpkeepWithTheLivingExpenses()
        {
            var withCar = new GameLoop();
            var without = new GameLoop();
            withCar.State.Cash = 1_000_000L;
            without.State.Cash = 1_000_000L;
            withCar.Holdings.SetCar(new CarHolding("Carro", 2_000_000L, 50_000L));

            withCar.CloseMonth();
            without.CloseMonth();

            Assert.AreEqual(50_000L, withCar.LastRunningCosts);
            Assert.AreEqual(0L, without.LastRunningCosts);
            Assert.AreEqual(without.State.Cash - 50_000L, withCar.State.Cash);
        }

        [Test]
        public void NewGame_ForgetsWhatWasBought()
        {
            loop.State.Cash = 1_000_000L;
            loop.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));

            loop.NewGame();

            Assert.IsTrue(loop.Holdings.IsEmpty);
            Assert.AreEqual(0L, loop.LastRunningCosts);
            Assert.AreEqual(120_000L, loop.State.Cash);
        }
    }
}

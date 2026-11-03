using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceVehicleTests
    {
        /// <summary>No interest, so every figure can be checked by hand: 4 000 000 financed over 40 months.</summary>
        private static CarPlan Plan()
        {
            return new CarPlan("Carro de prueba", 4_800_000L, 800_000L, 40, 0m, 10_000L, 50_000L, 100_000L, 55_000L, 4_200_000L);
        }

        private static PlayerState WithCash(long cash)
        {
            return new PlayerState { Cash = cash };
        }

        [Test]
        public void QuoteCar_SplitsTheDownPaymentFromTheFinancedPart()
        {
            var quote = FinanceEngine.QuoteCar(Plan());

            Assert.AreEqual(4_000_000L, quote.Financing.CashPrice);
            Assert.AreEqual(100_000L, quote.Financing.BaseMonthlyPayment);
            Assert.AreEqual(110_000L, quote.MonthlyInstallment, "the payment plus the insurance");
            Assert.AreEqual(165_000L, quote.MonthlyAllIn, "the installment plus fuel and upkeep");
            Assert.AreEqual(850_000L, quote.UpfrontCash, "the down payment plus the commission");
        }

        [Test]
        public void QuoteCar_ShowsWhatCreditAddedOnTopOfThePrice()
        {
            var quote = FinanceEngine.QuoteCar(Plan());

            Assert.AreEqual(5_250_000L, quote.TotalPaidForTheCar);
            Assert.AreEqual(450_000L, quote.ExtraOverPrice, "400 000 of insurance and 50 000 of commission");
        }

        [Test]
        public void QuoteCar_WithInterestCostsMoreThanThePrice()
        {
            var plan = new CarPlan("Carro con tasa", 6_500_000L, 975_000L, 60, 1.4m, 20_000L, 130_000L, 150_000L, 60_000L, 5_700_000L);

            var quote = FinanceEngine.QuoteCar(plan);

            Assert.AreEqual(quote.Financing.BaseMonthlyPayment + 20_000L, quote.MonthlyInstallment);
            Assert.Greater(quote.ExtraOverPrice, 20_000L * 60L + 130_000L, "interest comes on top of insurance and commission");
        }

        [Test]
        public void CarPlan_RejectsADownPaymentThatCoversThePrice()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarPlan("Carro", 1_000_000L, 1_000_000L, 12, 0m, 0L, 0L, 0L, 0L, 0L));
            Assert.Throws<ArgumentException>(() => new CarPlan(" ", 1_000_000L, 100_000L, 12, 0m, 0L, 0L, 0L, 0L, 0L));
        }

        [Test]
        public void TryBuyCar_PaysDownPaymentCommissionAndMarchamoFromCash()
        {
            var state = WithCash(1_000_000L);
            var holdings = new Holdings();
            int wellbeing = state.Wellbeing;

            var result = FinanceEngine.TryBuyCar(state, holdings, Plan());

            Assert.IsTrue(result.Bought);
            Assert.AreEqual(0L, result.FinancedOnCard);
            Assert.AreEqual(50_000L, state.Cash, "1 000 000 minus 850 000 minus the 100 000 marchamo");
            Assert.AreEqual(0L, state.CreditCard.Balance);
            Assert.AreEqual(1, state.Installments.Count);
            Assert.AreEqual("Carro: Carro de prueba", state.Installments[0].Item);
            Assert.AreEqual(110_000L, state.Installments[0].Monthly);
            Assert.AreEqual(40, state.Installments[0].MonthsLeft);
            Assert.AreEqual(wellbeing + FinanceEngine.CarWellbeingBoost, state.Wellbeing);
            Assert.AreEqual(55_000L, holdings.MonthlyRunningCosts);
            Assert.AreEqual(4_200_000L, holdings.AssetValue, "the car is worth its resale value, not what was paid");
            Assert.Contains("bought_car", state.History);
        }

        [Test]
        public void TryBuyCar_PutsTheMarchamoOnTheCardWhenCashRunsOut()
        {
            var state = WithCash(900_000L);

            var result = FinanceEngine.TryBuyCar(state, new Holdings(), Plan());

            Assert.IsTrue(result.Bought);
            Assert.AreEqual(50_000L, result.FinancedOnCard);
            Assert.AreEqual(0L, state.Cash, "cash never goes negative");
            Assert.AreEqual(50_000L, state.CreditCard.Balance);
        }

        [Test]
        public void TryBuyCar_ChangesNothingWithoutTheCashForTheDownPaymentAndCommission()
        {
            var state = WithCash(849_999L);
            var holdings = new Holdings();

            var result = FinanceEngine.TryBuyCar(state, holdings, Plan());

            Assert.AreEqual(PurchaseFailure.NotEnoughCash, result.Failure);
            Assert.AreEqual(849_999L, state.Cash);
            Assert.AreEqual(0, state.Installments.Count);
            Assert.AreEqual(0L, state.CreditCard.Balance);
            Assert.IsTrue(holdings.IsEmpty);
        }

        [Test]
        public void TryBuyCar_RefusesASecondCar()
        {
            var state = WithCash(5_000_000L);
            var holdings = new Holdings();
            FinanceEngine.TryBuyCar(state, holdings, Plan());
            long cashAfterFirst = state.Cash;

            var second = FinanceEngine.TryBuyCar(state, holdings, Plan());

            Assert.AreEqual(PurchaseFailure.AlreadyHasCar, second.Failure);
            Assert.AreEqual(cashAfterFirst, state.Cash);
            Assert.AreEqual(1, state.Installments.Count);
        }

        [Test]
        public void RunningCosts_ComeOutOfTheMonthWhenPassedAsLivingExpenses()
        {
            var state = new PlayerState { Cash = 1_000_000L, SalaryNet = 450_000L };
            var holdings = new Holdings();
            FinanceEngine.TryBuyCar(state, holdings, Plan());
            long cashBefore = state.Cash;

            var report = FinanceEngine.RunMonthlyTick(state, new MonthlyTickOptions
            {
                FixedLivingExpensesColones = 200_000L + holdings.MonthlyRunningCosts
            });

            long expected = cashBefore + 450_000L - 255_000L - report.InstallmentsCharged - report.CardCycle.AmountPaid;
            Assert.AreEqual(110_000L, report.InstallmentsCharged);
            Assert.AreEqual(expected, state.Cash);
        }
    }
}

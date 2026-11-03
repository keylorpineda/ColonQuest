using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceHousingTests
    {
        private static PlayerState WithCash(long cash)
        {
            return new PlayerState { Cash = cash, SalaryNet = 450_000L };
        }

        [Test]
        public void QuoteRent_AddsTheDepositAndTheWholeLease()
        {
            var quote = FinanceEngine.QuoteRent(260_000L, 12, 1);

            Assert.AreEqual(260_000L, quote.Deposit);
            Assert.AreEqual(260_000L, quote.UpfrontCost);
            Assert.AreEqual(3_120_000L, quote.LeaseCost);
        }

        [Test]
        public void QuoteRent_RejectsValuesOutsideTheContract()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.QuoteRent(0L, 12, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.QuoteRent(260_000L, 121, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.QuoteRent(260_000L, 12, 4));
        }

        [Test]
        public void TryRentHome_TakesTheDepositAndStartsAMonthlyInstallmentForTheWholeLease()
        {
            var state = WithCash(500_000L);
            var holdings = new Holdings();
            int wellbeing = state.Wellbeing;

            var result = FinanceEngine.TryRentHome(state, holdings, "Casa Vecina D", FinanceEngine.QuoteRent(260_000L, 12, 1));

            Assert.IsTrue(result.Bought);
            Assert.AreEqual(240_000L, state.Cash);
            Assert.AreEqual(1, state.Installments.Count);
            Assert.AreEqual("Alquiler: Casa Vecina D", state.Installments[0].Item);
            Assert.AreEqual(260_000L, state.Installments[0].Monthly);
            Assert.AreEqual(12, state.Installments[0].MonthsLeft);
            Assert.IsFalse(holdings.Home!.Owned);
            Assert.AreEqual(0L, holdings.AssetValue, "a rented home is not worth anything to the renter");
            Assert.AreEqual(wellbeing + FinanceEngine.HomeWellbeingBoost, state.Wellbeing);
            Assert.Contains("rented_home", state.History);
        }

        [Test]
        public void TryRentHome_ChargesTheRentEveryMonthLikeAnyInstallment()
        {
            var state = WithCash(1_000_000L);
            FinanceEngine.TryRentHome(state, new Holdings(), "Casa Vecina D", FinanceEngine.QuoteRent(260_000L, 12, 1));

            long charged = FinanceEngine.ProcessMonthlyInstallments(state);

            Assert.AreEqual(260_000L, charged);
            Assert.AreEqual(1_000_000L - 260_000L - 260_000L, state.Cash);
            Assert.AreEqual(11, state.Installments[0].MonthsLeft);
        }

        [Test]
        public void TryRentHome_ChangesNothingWhenTheDepositIsMissing()
        {
            var state = WithCash(100_000L);
            var holdings = new Holdings();

            var result = FinanceEngine.TryRentHome(state, holdings, "Casa Vecina D", FinanceEngine.QuoteRent(260_000L, 12, 1));

            Assert.IsFalse(result.Bought);
            Assert.AreEqual(PurchaseFailure.NotEnoughCash, result.Failure);
            Assert.AreEqual(100_000L, state.Cash);
            Assert.AreEqual(0, state.Installments.Count);
            Assert.IsNull(holdings.Home);
        }

        [Test]
        public void TryRentHome_RefusesASecondHome()
        {
            var state = WithCash(2_000_000L);
            var holdings = new Holdings();
            FinanceEngine.TryRentHome(state, holdings, "Casa Vecina D", FinanceEngine.QuoteRent(260_000L, 12, 1));

            var second = FinanceEngine.TryRentHome(state, holdings, "Casa Los Almendros 2", FinanceEngine.QuoteRent(310_000L, 12, 1));

            Assert.AreEqual(PurchaseFailure.AlreadyHasHome, second.Failure);
            Assert.AreEqual(1, state.Installments.Count);
        }

        [Test]
        public void QuoteMortgage_WithoutInterestIsTheFinancedAmountOverTheTerm()
        {
            var quote = FinanceEngine.QuoteMortgage(12_000_000L, 25m, 0m, 0m, 100);

            Assert.AreEqual(3_000_000L, quote.DownPayment);
            Assert.AreEqual(9_000_000L, quote.Financed);
            Assert.AreEqual(90_000L, quote.MonthlyPayment);
            Assert.AreEqual(12_000_000L, quote.TotalPaid);
            Assert.AreEqual(0L, quote.TotalInterest);
        }

        [Test]
        public void QuoteMortgage_WithInterestMatchesTheKnownAmortizationFigure()
        {
            // 10 000 000 at 1% a month for 12 months is 888 487.89 a month.
            var quote = FinanceEngine.QuoteMortgage(10_000_000L, 0m, 0m, 1m, 12);

            Assert.AreEqual(888_488L, quote.MonthlyPayment);
            Assert.AreEqual(661_856L, quote.TotalInterest);
        }

        [Test]
        public void QuoteMortgage_AddsClosingCostsToWhatMustBePaidUpFront()
        {
            var quote = FinanceEngine.QuoteMortgage(40_000_000L, 20m, 4m, 0.8m, 120);

            Assert.AreEqual(8_000_000L, quote.DownPayment);
            Assert.AreEqual(1_600_000L, quote.ClosingCosts);
            Assert.AreEqual(9_600_000L, quote.UpfrontCost);
            Assert.AreEqual(32_000_000L, quote.Financed);
            Assert.AreEqual(LoanMath.MonthlyPayment(32_000_000L, 0.8m, 120), quote.MonthlyPayment);
            Assert.That(quote.MonthlyPayment, Is.InRange(400_000L, 430_000L));
            Assert.AreEqual(quote.UpfrontCost + quote.MonthlyPayment * 120L, quote.TotalPaid);
            Assert.Greater(quote.TotalPaid, quote.Price, "the bank's interest makes the house cost more than its price");
        }

        [Test]
        public void QuoteMortgage_CannotPassTheInstallmentLimitOfTheContract()
        {
            Assert.DoesNotThrow(() => FinanceEngine.QuoteMortgage(10_000_000L, 20m, 0m, 1m, ContractLimits.MaxInstallmentMonths));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.QuoteMortgage(10_000_000L, 20m, 0m, 1m, ContractLimits.MaxInstallmentMonths + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.QuoteMortgage(10_000_000L, 100m, 0m, 1m, 12));
        }

        [Test]
        public void ShareOfSalaryPct_RoundsDownAndSurvivesNoSalary()
        {
            Assert.AreEqual(50L, FinanceEngine.ShareOfSalaryPct(225_000L, 450_000L));
            Assert.AreEqual(92L, FinanceEngine.ShareOfSalaryPct(415_880L, 450_000L));
            Assert.AreEqual(0L, FinanceEngine.ShareOfSalaryPct(100_000L, 0L));
        }

        [Test]
        public void TryBuyHome_PaysTheDownPaymentNowAndStartsTheMortgage()
        {
            var state = WithCash(3_500_000L);
            var holdings = new Holdings();
            var quote = FinanceEngine.QuoteMortgage(12_000_000L, 25m, 0m, 0m, 100);

            var result = FinanceEngine.TryBuyHome(state, holdings, "Casa Vecina B", quote);

            Assert.IsTrue(result.Bought);
            Assert.AreEqual(500_000L, state.Cash);
            Assert.AreEqual("Hipoteca: Casa Vecina B", state.Installments[0].Item);
            Assert.AreEqual(90_000L, state.Installments[0].Monthly);
            Assert.AreEqual(100, state.Installments[0].MonthsLeft);
            Assert.IsTrue(holdings.Home!.Owned);
            Assert.AreEqual(12_000_000L, holdings.AssetValue);
            Assert.Contains("bought_home", state.History);
        }

        [Test]
        public void TryBuyHome_ChangesNothingWhenOneColonIsMissing()
        {
            var state = WithCash(2_999_999L);
            var holdings = new Holdings();

            var result = FinanceEngine.TryBuyHome(state, holdings, "Casa Vecina B", FinanceEngine.QuoteMortgage(12_000_000L, 25m, 0m, 0m, 100));

            Assert.AreEqual(PurchaseFailure.NotEnoughCash, result.Failure);
            Assert.AreEqual(2_999_999L, state.Cash);
            Assert.AreEqual(0, state.Installments.Count);
            Assert.IsTrue(holdings.IsEmpty);
        }

        [Test]
        public void TryBuyHome_RefusesWhenTheInstallmentListIsFull()
        {
            var state = WithCash(10_000_000L);
            for (int i = 0; i < ContractLimits.MaxInstallments; i++)
            {
                state.Installments.Add(new Installment("Cuota " + i, 1_000L, 5));
            }

            var result = FinanceEngine.TryBuyHome(state, new Holdings(), "Casa Vecina B", FinanceEngine.QuoteMortgage(12_000_000L, 25m, 0m, 0m, 100));

            Assert.AreEqual(PurchaseFailure.TooManyInstallments, result.Failure);
            Assert.AreEqual(10_000_000L, state.Cash);
        }
    }
}

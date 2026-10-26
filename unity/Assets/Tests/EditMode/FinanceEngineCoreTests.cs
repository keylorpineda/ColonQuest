using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceEngineCoreTests
    {
        [Test]
        public void CalculatePayroll_BelowExemptThreshold_DeductsOnlyCcssAndBancoPopular()
        {
            // ₡500,000 gross is below the ₡918,000 income tax exempt threshold.
            // SEM (5.50%) = ₡27,500; IVM (4.33%) = ₡21,650; Banco Popular (1.00%) = ₡5,000.
            // Total worker social charges (10.83%) = ₡54,150. Net salary = ₡445,850.
            PayrollBreakdown breakdown = FinanceEngine.CalculatePayroll(500_000L);

            Assert.That(breakdown.GrossSalary, Is.EqualTo(500_000L));
            Assert.That(breakdown.SemDeduction, Is.EqualTo(27_500L));
            Assert.That(breakdown.IvmDeduction, Is.EqualTo(21_650L));
            Assert.That(breakdown.BancoPopularDeduction, Is.EqualTo(5_000L));
            Assert.That(breakdown.TotalSocialCharges, Is.EqualTo(54_150L));
            Assert.That(breakdown.IncomeTax, Is.EqualTo(0L));
            Assert.That(breakdown.NetSalary, Is.EqualTo(445_850L));
            Assert.That(FinanceEngine.CalculateNetSalary(500_000L), Is.EqualTo(445_850L));
        }

        [Test]
        public void CalculatePayroll_AcrossProgressiveTaxBrackets_ComputesExactDeductions()
        {
            // ₡1,500,000 gross:
            // - Social charges (10.83%): SEM ₡82,500 + IVM ₡64,950 + BP ₡15,000 = ₡162,450
            // - Income tax:
            //   - 0 to ₡918,000: 0
            //   - ₡918,000 to ₡1,347,000 (₡429,000 @ 10%): ₡42,900
            //   - ₡1,347,000 to ₡1,500,000 (₡153,000 @ 15%): ₡22,950
            //   Total income tax = ₡65,850
            // - Net salary = 1,500,000 - 162,450 - 65,850 = ₡1,271,700
            PayrollBreakdown breakdown = FinanceEngine.CalculatePayroll(1_500_000L);

            Assert.That(breakdown.TotalSocialCharges, Is.EqualTo(162_450L));
            Assert.That(breakdown.IncomeTax, Is.EqualTo(65_850L));
            Assert.That(breakdown.TotalDeductions, Is.EqualTo(228_300L));
            Assert.That(breakdown.NetSalary, Is.EqualTo(1_271_700L));
        }

        [Test]
        public void CalculateSalaryIncomeTax_TopBrackets_Applies20And25PercentTiers()
        {
            // ₡5,000,000 gross:
            // - Tier 1 (918k..1,347k = 429k @ 10%) = 42,900
            // - Tier 2 (1,347k..2,364k = 1,017k @ 15%) = 152,550
            // - Tier 3 (2,364k..4,727k = 2,363k @ 20%) = 472,600
            // - Tier 4 (4,727k..5,000k = 273k @ 25%) = 68,250
            // Total = ₡736,300
            long tax = FinanceEngine.CalculateSalaryIncomeTax(5_000_000L);
            Assert.That(tax, Is.EqualTo(736_300L));
        }

        [Test]
        public void CalculateCardInterest_AndMinimumPayment_RespectFloorAndBalanceCap()
        {
            var card = new CreditCard(180_000L, 300_000L, 3.5m, 10m);

            Assert.That(FinanceEngine.CalculateMinimumPayment(card), Is.EqualTo(18_000L));
            Assert.That(FinanceEngine.CalculateCardInterest(card.Balance, card.MonthlyRatePct), Is.EqualTo(6_300L));

            // Small balance below ₡5,000 floor is capped at the balance itself
            var smallCard = new CreditCard(3_200L, 300_000L, 3.5m, 10m);
            Assert.That(FinanceEngine.CalculateMinimumPayment(smallCard), Is.EqualTo(3_200L));

            // Balance where 10% (₡4,000) is below the ₡5,000 floor uses the floor
            var floorCard = new CreditCard(40_000L, 300_000L, 3.5m, 10m);
            Assert.That(FinanceEngine.CalculateMinimumPayment(floorCard), Is.EqualTo(5_000L));
        }

        [Test]
        public void PayCreditCardMinimum_CompoundsRemainingBalanceAndRecordsHistory()
        {
            var state = new PlayerState
            {
                Month = 1,
                SalaryNet = 450_000L,
                Cash = 120_000L,
                CreditCard = new CreditCard(180_000L, 300_000L, 3.5m, 10m),
            };

            // Month 1: min payment is 10% of 180,000 = 18,000.
            // Unpaid = 162,000. Interest (3.5% of 162,000) = 5,670. Ending balance = 167,670.
            CreditCardCycleResult m1 = FinanceEngine.PayCreditCardMinimum(state);

            Assert.That(m1.MinimumPaymentDue, Is.EqualTo(18_000L));
            Assert.That(m1.AmountPaid, Is.EqualTo(18_000L));
            Assert.That(m1.UnpaidBalanceBeforeInterest, Is.EqualTo(162_000L));
            Assert.That(m1.InterestCharged, Is.EqualTo(5_670L));
            Assert.That(m1.EndingBalance, Is.EqualTo(167_670L));
            Assert.That(state.Cash, Is.EqualTo(102_000L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(167_670L));
            Assert.That(state.History, Does.Contain("paid_minimum_only"));

            // Month 2: min payment is 10% of 167,670 = 16,767.
            // Unpaid = 150,903. Interest (3.5% of 150,903 = 5,281.605 -> 5,282). Ending = 156,185.
            CreditCardCycleResult m2 = FinanceEngine.PayCreditCardMinimum(state);

            Assert.That(m2.MinimumPaymentDue, Is.EqualTo(16_767L));
            Assert.That(m2.InterestCharged, Is.EqualTo(5_282L));
            Assert.That(m2.EndingBalance, Is.EqualTo(156_185L));
        }

        [Test]
        public void ProcessCreditCardCycle_NeverLetsCashGoNegative()
        {
            var state = new PlayerState
            {
                Cash = 7_000L,
                CreditCard = new CreditCard(100_000L, 300_000L, 4.0m, 10m),
            };

            CreditCardCycleResult result = FinanceEngine.ProcessCreditCardCycle(state, 50_000L);

            Assert.That(result.AmountPaid, Is.EqualTo(7_000L));
            Assert.That(state.Cash, Is.EqualTo(0L));
            Assert.That(result.UnpaidBalanceBeforeInterest, Is.EqualTo(93_000L));
            Assert.That(result.InterestCharged, Is.EqualTo(3_720L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(96_720L));
        }

        [Test]
        public void Validation_RejectsNegativeOrOutOfRangeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.CalculatePayroll(-1L));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard(-5L, 100_000L, 3m, 10m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CreditCard(0L, 100_000L, 120m, 10m));
        }
    }
}

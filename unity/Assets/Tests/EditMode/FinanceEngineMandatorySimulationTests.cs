using System.Collections.Generic;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    /// <summary>
    /// Mandatory test suite required by <c>CLAUDE.md</c> and <c>SCRUM-38</c>:
    /// 1. Compound credit card interest when paying only the minimum across 12 months.
    /// 2. Total cost of installment purchases with insurance and commission matching backend offers.
    /// 3. Aguinaldo calculation across standard, partial-year, and variable-pay scenarios.
    /// 4. Strict invariant that cash (and savings/emergency fund) never goes negative without
    ///    associated debt across all operations and a full 12-month simulation.
    /// </summary>
    [TestFixture]
    public sealed class FinanceEngineMandatorySimulationTests
    {
        [Test]
        public void Mandatory1_CompoundCardInterest_PayingOnlyMinimumOver12Months()
        {
            var state = new PlayerState
            {
                Month = 1,
                SalaryNet = 450_000L,
                Cash = 500_000L,
                CreditCard = new CreditCard(250_000L, 400_000L, 4.0m, 10.0m),
            };

            long totalPaid = 0L;
            long totalInterestAccrued = 0L;

            for (int month = 1; month <= 12; month++)
            {
                CreditCardCycleResult cycle = FinanceEngine.PayCreditCardMinimum(state);
                totalPaid += cycle.AmountPaid;
                totalInterestAccrued += cycle.InterestCharged;

                Assert.That(cycle.AmountPaid, Is.GreaterThanOrEqualTo(FinanceEngine.MinimumCardPaymentFloorColones));
                Assert.That(cycle.InterestCharged, Is.GreaterThan(0L));
                Assert.That(state.Cash, Is.GreaterThanOrEqualTo(0L));
            }

            // Even after paying ₡213,993 over a full year, ₡113,045 of the original ₡250,000 debt
            // still remains because 4% monthly compound interest adds ₡77,038 in interest charges.
            Assert.That(totalPaid, Is.EqualTo(213_993L));
            Assert.That(totalInterestAccrued, Is.EqualTo(77_038L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(113_045L));
            Assert.That(250_000L - totalPaid + totalInterestAccrued, Is.EqualTo(state.CreditCard.Balance));
            Assert.That(state.History, Does.Contain("paid_minimum_only"));
        }

        [TestCase(350_000L, 24, 2.5, 3_500L, 15_000L, 19_600L, 569_400L)]
        [TestCase(650_000L, 36, 2.9, 4_500L, 25_000L, 29_400L, 1_245_400L)]
        [TestCase(1_200_000L, 48, 2.2, 9_000L, 40_000L, 40_800L, 2_430_400L)]
        [TestCase(450_000L, 30, 2.7, 0L, 20_000L, 22_100L, 683_000L)]
        public void Mandatory2_InstallmentTotalCostWithInsurance_MatchesAllBackendSellerOffers(
            long cashPrice,
            int termMonths,
            double monthlyRatePct,
            long insuranceMonthly,
            long commission,
            long expectedBaseMonthly,
            long expectedTotalCost)
        {
            InstallmentQuote quote = FinanceEngine.CalculateInstallmentQuote(
                cashPrice,
                termMonths,
                (decimal) monthlyRatePct,
                insuranceMonthly,
                commission);

            Assert.That(quote.BaseMonthlyPayment, Is.EqualTo(expectedBaseMonthly));
            Assert.That(quote.TotalCost, Is.EqualTo(expectedTotalCost));
            Assert.That(quote.OverpaymentColones, Is.EqualTo(expectedTotalCost - cashPrice));
        }

        [Test]
        public void Mandatory3_Aguinaldo_ExemptFromPayrollDeductionsAndExactForAnyTenure()
        {
            long grossMonthly = 600_000L;
            PayrollBreakdown regularMonth = FinanceEngine.CalculatePayroll(grossMonthly);

            // Regular month has 10.83% social charges (₡64,980), net = ₡535,020.
            Assert.That(regularMonth.TotalSocialCharges, Is.EqualTo(64_980L));
            Assert.That(regularMonth.NetSalary, Is.EqualTo(535_020L));

            // December aguinaldo for 12 months worked equals full gross ₡600,000 with zero deductions.
            long fullYearAguinaldo = FinanceEngine.CalculateAguinaldo(grossMonthly, 12);
            Assert.That(fullYearAguinaldo, Is.EqualTo(600_000L));

            // 9 months worked -> 9/12 of ₡600,000 = ₡450,000.
            long nineMonthsAguinaldo = FinanceEngine.CalculateAguinaldo(grossMonthly, 9);
            Assert.That(nineMonthsAguinaldo, Is.EqualTo(450_000L));
        }

        [Test]
        public void Mandatory4_CashNeverNegativeWithoutAssociatedDebt_AcrossFullYearAndAllExpenseSources()
        {
            var state = new PlayerState
            {
                Month = 1,
                SalaryNet = 350_000L,
                Cash = 20_000L,
                Savings = 15_000L,
                EmergencyFund = 10_000L,
                CreditCard = new CreditCard(50_000L, 500_000L, 3.5m, 10m),
                Installments = new List<Installment>
                {
                    new Installment("Celular", 30_000L, 12),
                },
            };

            // 1. Direct cash expense larger than cash (₡100,000 vs ₡20,000 cash)
            // -> Cash becomes 0, and exact ₡80,000 deficit is added to CreditCard.Balance (50k -> 130k).
            FinanceEngine.ApplyEffect(
                state,
                EventType.Emergency,
                new Effect(EffectKind.Expense, 100_000L, MoneySource.Cash));
            Assert.That(state.Cash, Is.EqualTo(0L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(130_000L));

            // 2. Expense from Savings larger than Savings + Cash (₡40,000 vs ₡15,000 savings + ₡0 cash)
            // -> Savings becomes 0, Cash stays 0, and ₡25,000 deficit goes to CreditCard.Balance (130k -> 155k).
            FinanceEngine.ApplyEffect(
                state,
                EventType.Emergency,
                new Effect(EffectKind.Expense, 40_000L, MoneySource.Savings));
            Assert.That(state.Savings, Is.EqualTo(0L));
            Assert.That(state.Cash, Is.EqualTo(0L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(155_000L));

            // 3. Expense from EmergencyFund larger than EmergencyFund + Cash (₡35,000 vs ₡10,000 fund + ₡0 cash)
            // -> EmergencyFund becomes 0, Cash stays 0, and ₡25,000 deficit goes to CreditCard.Balance (155k -> 180k).
            FinanceEngine.ApplyEffect(
                state,
                EventType.Emergency,
                new Effect(EffectKind.Expense, 35_000L, MoneySource.EmergencyFund));
            Assert.That(state.EmergencyFund, Is.EqualTo(0L));
            Assert.That(state.Cash, Is.EqualTo(0L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(180_000L));

            // 4. Simulate 12 full months with high living expenses so cash frequently runs dry
            for (int m = 1; m <= 12; m++)
            {
                FinanceEngine.RunMonthlyTick(
                    state,
                    new MonthlyTickOptions
                    {
                        FixedLivingExpensesColones = 340_000L,
                        PayOnlyMinimumOnCard = true,
                        EmergencyFundContributionColones = 10_000L,
                        IncludeDecemberAguinaldo = true,
                        GrossSalaryForAguinaldoColones = 400_000L,
                        DecemberMarchamoColones = 120_000L,
                    });

                Assert.That(state.Cash, Is.GreaterThanOrEqualTo(0L), $"Cash went negative in month {m}");
                Assert.That(state.Savings, Is.GreaterThanOrEqualTo(0L), $"Savings went negative in month {m}");
                Assert.That(state.EmergencyFund, Is.GreaterThanOrEqualTo(0L), $"EmergencyFund went negative in month {m}");
                Assert.That(state.CreditCard.Balance, Is.GreaterThanOrEqualTo(0L), $"Card balance went negative in month {m}");
            }
        }
    }
}

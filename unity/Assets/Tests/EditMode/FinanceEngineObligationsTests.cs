using System.Collections.Generic;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceEngineObligationsTests
    {
        [Test]
        public void CalculateInstallmentQuote_WithHiddenInsuranceAndCommission_MatchesBackendSellerSheet()
        {
            // Mirrors "celular_gama_alta" in server/app/negotiation/seller_sheet.py:
            // Cash price = ₡350,000, 24 months @ 2.5% monthly, ₡3,500/mo insurance, ₡15,000 commission.
            // French amortization of 350,000 over 24m @ 2.5% = 19,569.49 -> rounded up to next 100 = ₡19,600.
            // Total monthly = 19,600 + 3,500 = ₡23,100.
            // Total cost = 23,100 * 24 + 15,000 = ₡569,400 (overpayment of ₡219,400).
            InstallmentQuote quote = FinanceEngine.CalculateInstallmentQuote(
                cashPriceColones: 350_000L,
                termMonths: 24,
                monthlyRatePct: 2.5m,
                insuranceMonthlyColones: 3_500L,
                commissionColones: 15_000L);

            Assert.That(quote.BaseMonthlyPayment, Is.EqualTo(19_600L));
            Assert.That(quote.TotalMonthlyPayment, Is.EqualTo(23_100L));
            Assert.That(quote.TotalCost, Is.EqualTo(569_400L));
            Assert.That(quote.OverpaymentColones, Is.EqualTo(219_400L));
        }

        [Test]
        public void CalculateAguinaldo_FullAndPartialYear_ComputesExactTwelfth()
        {
            // Full year at ₡450,000/month -> ₡450,000 aguinaldo
            Assert.That(FinanceEngine.CalculateAguinaldo(450_000L, 12), Is.EqualTo(450_000L));

            // 6 months worked at ₡450,000/month -> ₡225,000 aguinaldo
            Assert.That(FinanceEngine.CalculateAguinaldo(450_000L, 6), Is.EqualTo(225_000L));

            // Variable salaries across 12 months (6 months @ 400k + 6 months @ 500k = 5,400,000 / 12 = 450,000)
            var salaries = new List<long>
            {
                400_000L, 400_000L, 400_000L, 400_000L, 400_000L, 400_000L,
                500_000L, 500_000L, 500_000L, 500_000L, 500_000L, 500_000L,
            };
            Assert.That(FinanceEngine.CalculateAguinaldo(salaries), Is.EqualTo(450_000L));
        }

        [Test]
        public void ProcessMonthlyInstallments_DecrementsMonthsRemovesFinishedAndNeverMakesCashNegative()
        {
            var state = new PlayerState
            {
                Cash = 30_000L,
                CreditCard = new CreditCard(0L, 300_000L, 3.5m, 10m),
                Installments = new List<Installment>
                {
                    new Installment("Celular", 25_000L, 2),
                    new Installment("Licuadora", 15_000L, 1),
                },
            };

            // Total installments = 40,000; Cash = 30,000 -> 10,000 deficit goes to CreditCard.Balance
            long totalCharged = FinanceEngine.ProcessMonthlyInstallments(state);

            Assert.That(totalCharged, Is.EqualTo(40_000L));
            Assert.That(state.Cash, Is.EqualTo(0L));
            Assert.That(state.CreditCard.Balance, Is.EqualTo(10_000L));
            Assert.That(state.Installments.Count, Is.EqualTo(1));
            Assert.That(state.Installments[0].Item, Is.EqualTo("Celular"));
            Assert.That(state.Installments[0].MonthsLeft, Is.EqualTo(1));
        }

        [Test]
        public void InformalLoans_AccrueMonthlyInterestAndSupportPaydown()
        {
            var state = new PlayerState
            {
                Cash = 100_000L,
                InformalLoans = new List<InformalLoan>
                {
                    new InformalLoan("Prestamista Don Beto", 80_000L, 20.0m),
                },
            };

            // 20% monthly interest on ₡80,000 = ₡16,000 -> new balance ₡96,000
            long interest = FinanceEngine.AccrueInformalLoansInterest(state);
            Assert.That(interest, Is.EqualTo(16_000L));
            Assert.That(state.InformalLoans[0].Balance, Is.EqualTo(96_000L));

            // Pay off the entire ₡96,000 loan from cash -> loan is removed
            long paid = FinanceEngine.PayInformalLoan(state, 0, 96_000L);
            Assert.That(paid, Is.EqualTo(96_000L));
            Assert.That(state.Cash, Is.EqualTo(4_000L));
            Assert.That(state.InformalLoans, Is.Empty);
        }

        [Test]
        public void EmergencyFund_TransfersAndTracksSkippedSavings()
        {
            var state = new PlayerState { Cash = 50_000L, EmergencyFund = 10_000L };

            long contributed = FinanceEngine.ContributeToEmergencyFund(state, 20_000L);
            Assert.That(contributed, Is.EqualTo(20_000L));
            Assert.That(state.Cash, Is.EqualTo(30_000L));
            Assert.That(state.EmergencyFund, Is.EqualTo(30_000L));
            Assert.That(state.History, Does.Not.Contain("skipped_savings"));

            long zeroContribution = FinanceEngine.ContributeToEmergencyFund(state, 0L);
            Assert.That(zeroContribution, Is.EqualTo(0L));
            Assert.That(state.History, Does.Contain("skipped_savings"));

            long withdrawn = FinanceEngine.WithdrawFromEmergencyFund(state, 15_000L);
            Assert.That(withdrawn, Is.EqualTo(15_000L));
            Assert.That(state.EmergencyFund, Is.EqualTo(15_000L));
            Assert.That(state.Cash, Is.EqualTo(45_000L));
        }

        [Test]
        public void RunMonthlyTick_DecemberIncludesAguinaldoMarchamoInstallmentsAndWrapsMonth()
        {
            var state = new PlayerState
            {
                Month = 12,
                SalaryNet = 445_850L,
                Cash = 50_000L,
                CreditCard = new CreditCard(100_000L, 300_000L, 3.5m, 10m),
                Installments = new List<Installment>
                {
                    new Installment("Celular", 25_000L, 12),
                },
                InformalLoans = new List<InformalLoan>
                {
                    new InformalLoan("Gota a gota", 50_000L, 15.0m),
                },
            };

            MonthlyTickReport report = FinanceEngine.RunMonthlyTick(
                state,
                new MonthlyTickOptions
                {
                    IncludeDecemberAguinaldo = true,
                    GrossSalaryForAguinaldoColones = 500_000L,
                    MonthsWorkedInYear = 12,
                    DecemberMarchamoColones = 110_000L,
                    FixedLivingExpensesColones = 200_000L,
                    PayOnlyMinimumOnCard = true,
                    EmergencyFundContributionColones = 50_000L,
                });

            Assert.That(report.MonthProcessed, Is.EqualTo(12));
            Assert.That(report.SalaryCredited, Is.EqualTo(445_850L));
            Assert.That(report.AguinaldoCredited, Is.EqualTo(500_000L));
            Assert.That(report.InstallmentsCharged, Is.EqualTo(25_000L));
            Assert.That(report.MarchamoCharged, Is.EqualTo(110_000L));
            Assert.That(report.CardCycle.AmountPaid, Is.EqualTo(10_000L));
            Assert.That(report.CardCycle.InterestCharged, Is.EqualTo(3_150L));
            Assert.That(report.InformalLoanInterestAccrued, Is.EqualTo(7_500L));
            Assert.That(report.EmergencyFundContributed, Is.EqualTo(50_000L));
            Assert.That(state.Cash, Is.EqualTo(600_850L));
            Assert.That(state.Month, Is.EqualTo(1));
        }
    }
}

using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceIncidentsTests
    {
        private static PlayerState State(long cash, long fund = 0L, long card = 0L, long limit = 300_000L)
        {
            return new PlayerState { Cash = cash, EmergencyFund = fund, CreditCard = new CreditCard(card, limit, 3.5m, 10m), Wellbeing = 60 };
        }

        // ---------------------------------------------------------------- fines

        [Test]
        public void ChargeFine_TakesItFromCash()
        {
            var state = State(200_000L);

            long onCard = FinanceEngine.ChargeFine(state, 120_000L);

            Assert.AreEqual(80_000L, state.Cash);
            Assert.AreEqual(0L, onCard);
            Assert.AreEqual(0L, state.CreditCard.Balance);
        }

        [Test]
        public void ChargeFine_WhatCashCannotPayGoesToTheCardAndCashNeverGoesNegative()
        {
            var state = State(50_000L, card: 10_000L);

            long onCard = FinanceEngine.ChargeFine(state, 120_000L);

            Assert.AreEqual(0L, state.Cash);
            Assert.AreEqual(70_000L, onCard);
            Assert.AreEqual(80_000L, state.CreditCard.Balance);
        }

        // ---------------------------------------------------------------- hospital

        [Test]
        public void ChargeMedicalBill_TheEmergencyFundPaysFirst()
        {
            var state = State(100_000L, fund: 400_000L);

            long onCard = FinanceEngine.ChargeMedicalBill(state, 380_000L);

            Assert.AreEqual(20_000L, state.EmergencyFund);
            Assert.AreEqual(100_000L, state.Cash, "cash is untouched while the fund covers it");
            Assert.AreEqual(0L, onCard);
            CollectionAssert.Contains(state.History, "hospital_paid_from_fund");
        }

        [Test]
        public void ChargeMedicalBill_WithoutAFundItTakesCashThenTheCard()
        {
            var state = State(120_000L, fund: 0L, card: 0L);

            long onCard = FinanceEngine.ChargeMedicalBill(state, 380_000L);

            Assert.AreEqual(0L, state.Cash);
            Assert.AreEqual(260_000L, onCard);
            Assert.AreEqual(260_000L, state.CreditCard.Balance);
            CollectionAssert.Contains(state.History, "hospital_bill");
        }

        [Test]
        public void ChargeMedicalBill_AHalfFundPaysWhatItHasAndTheRestGoesOn()
        {
            var state = State(50_000L, fund: 100_000L);

            FinanceEngine.ChargeMedicalBill(state, 300_000L);

            Assert.AreEqual(0L, state.EmergencyFund);
            Assert.AreEqual(0L, state.Cash);
            Assert.AreEqual(150_000L, state.CreditCard.Balance);
        }

        [TestCase(380_000L, 15, 57_000L)]
        [TestCase(100L, 15, 15L)]
        [TestCase(101L, 15, 16L)]
        [TestCase(380_000L, 100, 380_000L)]
        [TestCase(380_000L, 0, 0L)]
        public void PatientShare_IsRoundedUpToTheColon(long bill, int pct, long expected)
        {
            Assert.AreEqual(expected, FinanceEngine.PatientShare(bill, pct));
        }

        [Test]
        public void PatientShare_RejectsABadPercentage()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.PatientShare(1000L, 101));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.PatientShare(-1L, 10));
        }

        // ---------------------------------------------------------------- collectors

        [Test]
        public void DebtOwed_AddsTheCardAndEveryInformalLoan()
        {
            var state = State(0L, card: 100_000L);
            state.InformalLoans.Add(new InformalLoan("Don Beto", 60_000L, 15m));
            state.InformalLoans.Add(new InformalLoan("Prestamista", 40_000L, 15m));

            Assert.AreEqual(200_000L, FinanceEngine.DebtOwed(state));
        }

        [Test]
        public void PayCollectors_LowersTheInformalLoanBeforeTheCard()
        {
            var state = State(100_000L, card: 90_000L);
            state.InformalLoans.Add(new InformalLoan("Prestamista", 60_000L, 15m));

            long paid = FinanceEngine.PayCollectors(state, 80_000L);

            Assert.AreEqual(80_000L, paid);
            Assert.AreEqual(20_000L, state.Cash);
            Assert.AreEqual(0, state.InformalLoans.Count, "the loan is gone");
            Assert.AreEqual(70_000L, state.CreditCard.Balance, "the rest of the payment went to the card");
        }

        [Test]
        public void PayCollectors_NeverPaysMoreThanTheCashThePlayerHas()
        {
            var state = State(15_000L, card: 90_000L);

            long paid = FinanceEngine.PayCollectors(state, 80_000L);

            Assert.AreEqual(15_000L, paid);
            Assert.AreEqual(0L, state.Cash);
            Assert.AreEqual(75_000L, state.CreditCard.Balance);
        }

        [Test]
        public void NegotiateCollectors_PaysHalfNowAndTheRestComesBackWithASurcharge()
        {
            var state = State(200_000L, card: 100_000L);

            var result = FinanceEngine.NegotiateCollectors(state, 100_000L, 10);

            Assert.AreEqual(50_000L, result.Paid);
            Assert.AreEqual(55_000L, result.NewDebt, "the unpaid half plus 10 percent");
            Assert.AreEqual(150_000L, state.Cash);
            Assert.AreEqual(1, state.InformalLoans.Count);
            Assert.AreEqual(55_000L, state.InformalLoans[0].Balance);
            Assert.AreEqual("Cobradores", state.InformalLoans[0].Lender);
        }

        [Test]
        public void NegotiateCollectors_WithoutCashTheWholeDemandBecomesDebt()
        {
            var state = State(0L, card: 100_000L);

            var result = FinanceEngine.NegotiateCollectors(state, 100_000L, 10);

            Assert.AreEqual(0L, result.Paid);
            Assert.AreEqual(110_000L, result.NewDebt);
        }

        [Test]
        public void IgnoreCollectors_AddsALateFeeToTheCardAndLowersTheMood()
        {
            var state = State(100_000L, card: 100_000L);

            long fee = FinanceEngine.IgnoreCollectors(state, 100_000L, 12, 10);

            Assert.AreEqual(12_000L, fee);
            Assert.AreEqual(112_000L, state.CreditCard.Balance);
            Assert.AreEqual(100_000L, state.Cash, "ignoring them costs no cash today");
            Assert.AreEqual(50, state.Wellbeing);
        }

        [Test]
        public void ChangeWellbeing_StaysBetweenZeroAndOneHundred()
        {
            var state = State(0L);
            FinanceEngine.ChangeWellbeing(state, -500);
            Assert.AreEqual(0, state.Wellbeing);
            FinanceEngine.ChangeWellbeing(state, 500);
            Assert.AreEqual(100, state.Wellbeing);
        }
    }
}

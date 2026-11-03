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

        private static PlayerState Owing(long cash, long loan, long card = 0L)
        {
            var state = State(cash, card: card);
            state.InformalLoans.Add(new InformalLoan("Prestamista", loan, 15m));
            return state;
        }

        [Test]
        public void DebtOwed_IsTheInformalLoansAndNotTheCard()
        {
            var state = Owing(0L, 60_000L, card: 100_000L);
            state.InformalLoans.Add(new InformalLoan("Don Beto", 40_000L, 15m));

            Assert.AreEqual(100_000L, FinanceEngine.DebtOwed(state));
        }

        [Test]
        public void PayCollectors_LowersTheLoansAndLeavesTheCardAlone()
        {
            var state = Owing(100_000L, 60_000L, card: 90_000L);
            state.InformalLoans.Add(new InformalLoan("Don Beto", 40_000L, 15m));

            long paid = FinanceEngine.PayCollectors(state, 80_000L);

            Assert.AreEqual(80_000L, paid);
            Assert.AreEqual(20_000L, state.Cash);
            Assert.AreEqual(1, state.InformalLoans.Count, "the first loan is gone");
            Assert.AreEqual(20_000L, state.InformalLoans[0].Balance);
            Assert.AreEqual(90_000L, state.CreditCard.Balance);
        }

        [Test]
        public void PayCollectors_NeverPaysMoreThanTheCashThePlayerHas()
        {
            var state = Owing(15_000L, 90_000L);

            long paid = FinanceEngine.PayCollectors(state, 80_000L);

            Assert.AreEqual(15_000L, paid);
            Assert.AreEqual(0L, state.Cash);
            Assert.AreEqual(75_000L, state.InformalLoans[0].Balance);
        }

        [Test]
        public void PayCollectors_NeverTakesMoreThanIsOwed()
        {
            var state = Owing(100_000L, 5_000L);

            long paid = FinanceEngine.PayCollectors(state, 20_000L);

            Assert.AreEqual(5_000L, paid);
            Assert.AreEqual(95_000L, state.Cash, "the rest of the money stays with the player");
            Assert.AreEqual(0, state.InformalLoans.Count);
        }

        [Test]
        public void NegotiateCollectors_MovesTheRestInsteadOfAddingIt()
        {
            var state = Owing(200_000L, 300_000L);

            var result = FinanceEngine.NegotiateCollectors(state, 75_000L, 10);

            Assert.AreEqual(37_500L, result.Paid);
            Assert.AreEqual(41_250L, result.NewDebt, "the unpaid half plus 10 percent");
            Assert.AreEqual(162_500L, state.Cash);
            Assert.AreEqual(266_250L, FinanceEngine.DebtOwed(state), "only the surcharge is new debt");
            Assert.AreEqual("Cobradores", state.InformalLoans[1].Lender);
            Assert.AreEqual(41_250L, state.InformalLoans[1].Balance);
        }

        [Test]
        public void NegotiateCollectors_WithoutCashTheWholeDemandMovesWithItsSurcharge()
        {
            var state = Owing(0L, 300_000L);

            var result = FinanceEngine.NegotiateCollectors(state, 100_000L, 10);

            Assert.AreEqual(0L, result.Paid);
            Assert.AreEqual(110_000L, result.NewDebt);
            Assert.AreEqual(310_000L, FinanceEngine.DebtOwed(state));
        }

        [Test]
        public void NegotiateCollectors_AtTheLimitOfLoansTheDebtIsNotLost()
        {
            var state = Owing(0L, 300_000L);
            while (state.InformalLoans.Count < ContractLimits.MaxInformalLoans)
            {
                state.InformalLoans.Add(new InformalLoan("Otro", 10_000L, 15m));
            }

            long before = FinanceEngine.DebtOwed(state);
            FinanceEngine.NegotiateCollectors(state, 100_000L, 10);

            Assert.AreEqual(before + 10_000L, FinanceEngine.DebtOwed(state));
            Assert.AreEqual(ContractLimits.MaxInformalLoans, state.InformalLoans.Count);
        }

        [Test]
        public void IgnoreCollectors_AddsALateFeeToTheLoanAndLowersTheMood()
        {
            var state = Owing(100_000L, 200_000L, card: 100_000L);

            long fee = FinanceEngine.IgnoreCollectors(state, 100_000L, 12, 10);

            Assert.AreEqual(12_000L, fee);
            Assert.AreEqual(212_000L, FinanceEngine.DebtOwed(state));
            Assert.AreEqual(100_000L, state.CreditCard.Balance);
            Assert.AreEqual(100_000L, state.Cash, "ignoring them costs no cash today");
            Assert.AreEqual(50, state.Wellbeing);
        }

        [Test]
        public void PayInformalDebt_PaysWhatTheCashReaches()
        {
            var some = Owing(30_000L, 100_000L);
            Assert.AreEqual(30_000L, FinanceEngine.PayInformalDebt(some));
            Assert.AreEqual(70_000L, FinanceEngine.DebtOwed(some));

            var all = Owing(150_000L, 100_000L, card: 50_000L);
            Assert.AreEqual(100_000L, FinanceEngine.PayInformalDebt(all));
            Assert.AreEqual(0, all.InformalLoans.Count);
            Assert.AreEqual(50_000L, all.Cash);
            Assert.AreEqual(50_000L, all.CreditCard.Balance, "the card is another debt");

            Assert.AreEqual(0L, FinanceEngine.PayInformalDebt(State(50_000L)));
        }

        // ---------------------------------------------------------------- where the money leaves from

        [Test]
        public void TryPayBill_LeavesFromTheChosenSource()
        {
            var state = State(10_000L, card: 100_000L);
            state.Savings = 40_000L;

            Assert.IsTrue(FinanceEngine.TryPayBill(state, 8_000L, MoneySource.Cash));
            Assert.AreEqual(2_000L, state.Cash);
            Assert.IsTrue(FinanceEngine.TryPayBill(state, 30_000L, MoneySource.Savings));
            Assert.AreEqual(10_000L, state.Savings);
            Assert.IsTrue(FinanceEngine.TryPayBill(state, 20_000L, MoneySource.CreditCard));
            Assert.AreEqual(120_000L, state.CreditCard.Balance);
        }

        [Test]
        public void TryPayBill_ChangesNothingWhenTheSourceDoesNotReach()
        {
            var state = State(1_000L, card: 100_000L);
            state.Savings = 1_000L;
            state.CreditCard.Limit = 100_500L;

            Assert.IsFalse(FinanceEngine.TryPayBill(state, 5_000L, MoneySource.Cash));
            Assert.IsFalse(FinanceEngine.TryPayBill(state, 5_000L, MoneySource.Savings));
            Assert.IsFalse(FinanceEngine.TryPayBill(state, 5_000L, MoneySource.CreditCard), "the card keeps its limit");
            Assert.AreEqual(1_000L, state.Cash);
            Assert.AreEqual(1_000L, state.Savings);
            Assert.AreEqual(100_000L, state.CreditCard.Balance);
        }

        [Test]
        public void TrySpendFromAccount_TouchesOnlyTheAccount()
        {
            var state = State(50_000L);
            state.Savings = 20_000L;

            Assert.IsFalse(FinanceEngine.TrySpendFromAccount(state, 30_000L), "the cash in the pocket does not help");
            Assert.IsTrue(FinanceEngine.TrySpendFromAccount(state, 20_000L));
            Assert.AreEqual(0L, state.Savings);
            Assert.AreEqual(50_000L, state.Cash);
        }

        private static GameEvent CardEvent(long onCard)
        {
            return new GameEvent
            {
                EventId = "evt_test",
                Type = EventType.Emergency,
                Title = "Test",
                Narrative = "Test",
                TeachingConcept = TeachingConcept.Budget,
                Choices = new System.Collections.Generic.List<Choice>
                {
                    new Choice("card", "Card", new System.Collections.Generic.List<Effect> { new Effect(EffectKind.Expense, onCard, MoneySource.CreditCard) }),
                    new Choice("cash", "Cash", new System.Collections.Generic.List<Effect> { new Effect(EffectKind.Expense, onCard, MoneySource.Cash) })
                }
            };
        }

        [Test]
        public void ChoiceIsOpen_ClosesWhatDoesNotFitOnTheCard()
        {
            var state = State(0L, card: 100_000L);
            state.CreditCard.Limit = 120_000L;

            var fits = CardEvent(20_000L);
            Assert.IsTrue(FinanceEngine.ChoiceIsOpen(state, fits, fits.Choices[0]));

            var tooMuch = CardEvent(20_001L);
            Assert.IsFalse(FinanceEngine.ChoiceIsOpen(state, tooMuch, tooMuch.Choices[0]));
            Assert.IsTrue(FinanceEngine.ChoiceIsOpen(state, tooMuch, tooMuch.Choices[1]));
        }

        [Test]
        public void ChoiceIsOpen_AnEventWhereNothingFitsStaysOpen()
        {
            var state = State(0L, card: 100_000L);
            state.CreditCard.Limit = 100_000L;
            var onlyCard = CardEvent(20_000L);
            onlyCard.Choices[1] = new Choice("card2", "Card", new System.Collections.Generic.List<Effect> { new Effect(EffectKind.Expense, 30_000L, MoneySource.CreditCard) });

            Assert.IsTrue(FinanceEngine.ChoiceIsOpen(state, onlyCard, onlyCard.Choices[0]));
        }

        [Test]
        public void CardCycle_MissingTheMinimumAddsTheLateFee()
        {
            var state = State(0L, card: 100_000L);

            var result = FinanceEngine.PayCreditCardMinimum(state);

            Assert.AreEqual(FinanceEngine.CardLateFeeColones, result.LateFee);
            Assert.AreEqual(100_000L + result.InterestCharged + FinanceEngine.CardLateFeeColones, state.CreditCard.Balance);
            CollectionAssert.Contains(state.History, "missed_card_minimum");
        }

        [Test]
        public void CardCycle_ReachingTheMinimumHasNoLateFee()
        {
            var state = State(50_000L, card: 100_000L);

            var result = FinanceEngine.PayCreditCardMinimum(state);

            Assert.AreEqual(0L, result.LateFee);
            CollectionAssert.DoesNotContain(state.History, "missed_card_minimum");
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

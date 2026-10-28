using System;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceWalletTests
    {
        private static PlayerState WithCash(long cash)
        {
            return new PlayerState { Cash = cash };
        }

        [Test]
        public void TrySpendCash_TakesTheMoneyOnlyWhenThereIsEnough()
        {
            var state = WithCash(30_000L);
            Assert.IsTrue(FinanceEngine.TrySpendCash(state, 10_000L));
            Assert.AreEqual(20_000L, state.Cash);

            Assert.IsFalse(FinanceEngine.TrySpendCash(state, 25_000L));
            Assert.AreEqual(20_000L, state.Cash, "a failed spend changes nothing");
        }

        [Test]
        public void TrySpendCash_NeverTouchesTheCard()
        {
            var state = WithCash(1_000L);
            long card = state.CreditCard.Balance;
            FinanceEngine.TrySpendCash(state, 5_000L);
            Assert.AreEqual(card, state.CreditCard.Balance);
        }

        [Test]
        public void TrySpendCash_RejectsNonPositiveAmounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.TrySpendCash(WithCash(5_000L), 0L));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceEngine.TrySpendCash(WithCash(5_000L), -1L));
        }

        [Test]
        public void CreditCash_AddsAndNeverPassesTheLimit()
        {
            var state = WithCash(ContractLimits.MaxColones - 10L);
            FinanceEngine.CreditCash(state, 1_000L);
            Assert.AreEqual(ContractLimits.MaxColones, state.Cash);
        }

        [Test]
        public void Casino_TheHouseAlwaysWinsInTheLongRun()
        {
            Assert.AreEqual(63L, CasinoGame.ReturnPerHundred(10_000L), "63 colones come back for every 100 bet");

            long wins = 0L;
            long losses = 0L;
            int winRolls = 0;
            for (int roll = 0; roll < 100; roll++)
            {
                var result = CasinoGame.Play(10_000L, roll);
                if (result.Net > 0L)
                {
                    wins += result.Net;
                    winRolls++;
                }
                else
                {
                    losses += -result.Net;
                }
            }

            Assert.Greater(winRolls, 30, "small wins come often, to keep the player betting");
            Assert.Greater(losses, wins, "but the losses add up to more");
        }

        [Test]
        public void Casino_PaysTheExpectedAmounts()
        {
            Assert.AreEqual(CasinoOutcome.Jackpot, CasinoGame.Play(10_000L, 0).Outcome);
            Assert.AreEqual(20_000L, CasinoGame.Play(10_000L, 0).Net);
            Assert.AreEqual(5_000L, CasinoGame.Play(10_000L, 10).Net);
            Assert.AreEqual(-10_000L, CasinoGame.Play(10_000L, 99).Net);
        }

        [Test]
        public void Casino_RejectsStakesOutsideTheLimits()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CasinoGame.Play(500L, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => CasinoGame.Play(CasinoGame.MaxStake + 1L, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => CasinoGame.Play(10_000L, 100));
        }

        [Test]
        public void LoanMath_MatchesTheKnownFormula()
        {
            // 1 000 000 at 2% a month over 12 months is 94 560 a month.
            Assert.AreEqual(94_560L, LoanMath.MonthlyPayment(1_000_000L, 2m, 12));
            Assert.AreEqual(94_560L * 12L, LoanMath.TotalCost(1_000_000L, 2m, 12));
        }

        [Test]
        public void LoanMath_WithoutInterestJustDividesThePrice()
        {
            Assert.AreEqual(25_000L, LoanMath.MonthlyPayment(300_000L, 0m, 12));
        }

        [Test]
        public void LoanMath_InterestMakesTheTotalCostMore()
        {
            Assert.Greater(LoanMath.TotalCost(500_000L, 3m, 24), 500_000L);
        }
    }
}

using ColonQuest.Game;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class RestAndCookTests
    {
        [Test]
        public void Rest_RaisesTheMoodOnceAMonth()
        {
            var loop = new GameLoop();
            int mood = loop.State.Wellbeing;

            var outcome = loop.Rest();

            Assert.IsNotNull(outcome);
            Assert.IsTrue(loop.SleptThisMonth);
            Assert.AreEqual(System.Math.Min(100, mood + MarketAssumptions.RestWellbeing), loop.State.Wellbeing);
            Assert.IsNull(loop.Rest(), "a second night in the same month changes nothing");
        }

        [Test]
        public void Cook_PaysTheSavingThroughTheEngineOnceAMonth()
        {
            var loop = new GameLoop();
            long cash = loop.State.Cash;

            var outcome = loop.Cook();

            Assert.IsNotNull(outcome);
            Assert.AreEqual(cash + MarketAssumptions.HomeCookingSaving, loop.State.Cash);
            Assert.AreEqual(MarketAssumptions.HomeCookingSaving, outcome.CashDelta);
            Assert.IsFalse(string.IsNullOrEmpty(outcome.Note), "the outcome says why cooking adds cash");
            Assert.IsNull(loop.Cook());
        }

        [Test]
        public void WithdrawSavings_MovesTheAccountToCashWithANote()
        {
            var loop = new GameLoop();
            loop.State.Savings = 20_000L;
            long cash = loop.State.Cash;

            var outcome = loop.WithdrawSavings(25_000L);

            Assert.IsNotNull(outcome);
            Assert.AreEqual(cash + 20_000L, loop.State.Cash, "only what the account holds");
            Assert.AreEqual(0L, loop.State.Savings);
            Assert.AreEqual(20_000L, outcome.CashDelta);
            Assert.AreEqual(-20_000L, outcome.SavingsDelta);
            Assert.IsFalse(string.IsNullOrEmpty(outcome.Note));
            Assert.IsNull(loop.WithdrawSavings(10_000L), "an empty account");
            Assert.IsNull(loop.WithdrawSavings(0L));
        }

        [Test]
        public void DepositCash_MovesCashToTheAccountWithANote()
        {
            var loop = new GameLoop();
            loop.State.Cash = 15_000L;
            long savings = loop.State.Savings;

            var outcome = loop.DepositCash(10_000L);

            Assert.IsNotNull(outcome);
            Assert.AreEqual(5_000L, loop.State.Cash);
            Assert.AreEqual(savings + 10_000L, loop.State.Savings);
            Assert.AreEqual(10_000L, outcome.SavingsDelta);
            Assert.IsFalse(string.IsNullOrEmpty(outcome.Note));

            loop.State.Cash = 0L;
            Assert.IsNull(loop.DepositCash(10_000L), "no cash");
        }

        [Test]
        public void Wear_TheSwimsuitOnlyOnceItWasBought()
        {
            const int swimsuit = 3;
            var loop = new GameLoop();
            Assert.AreEqual(0, loop.Outfit);

            Assert.IsTrue(loop.Wear(1, swimsuit));
            Assert.AreEqual(1, loop.Outfit);
            Assert.IsFalse(loop.Wear(swimsuit, swimsuit), "no swimsuit yet");
            Assert.AreEqual(1, loop.Outfit);
            Assert.IsFalse(loop.Wear(-1, swimsuit));

            loop.BuySwimsuit();
            Assert.IsTrue(loop.Wear(swimsuit, swimsuit));
            Assert.AreEqual(swimsuit, loop.Outfit);

            loop.NewGame();
            Assert.AreEqual(0, loop.Outfit, "a new game starts in everyday clothes");
        }

        [Test]
        public void ClosingTheMonthWithoutSleep_CostsALittleMoodAndTheFlagsReset()
        {
            var awake = new GameLoop();
            var rested = new GameLoop();
            rested.Rest();

            awake.CloseMonth();
            rested.CloseMonth();

            Assert.Less(awake.State.Wellbeing, rested.State.Wellbeing);
            Assert.IsFalse(rested.SleptThisMonth);
            Assert.IsFalse(awake.CookedThisMonth);
        }
    }
}

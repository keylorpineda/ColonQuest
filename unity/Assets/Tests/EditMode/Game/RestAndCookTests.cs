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
            Assert.IsNull(loop.Cook());
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

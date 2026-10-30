using ColonQuest.Game;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class JailAndSwimTests
    {
        [Test]
        public void Term_GrowsWithEachArrestAndStopsAtFifteenMinutes()
        {
            Assert.AreEqual(300, JailSentence.TermSeconds(0));
            Assert.AreEqual(600, JailSentence.TermSeconds(1));
            Assert.AreEqual(900, JailSentence.TermSeconds(2));
            Assert.AreEqual(900, JailSentence.TermSeconds(7));
        }

        [Test]
        public void Bail_CostsEachStartedMinuteThatIsLeft()
        {
            Assert.AreEqual(5 * MarketAssumptions.BailPerMinute, JailSentence.BailCost(300));
            Assert.AreEqual(2 * MarketAssumptions.BailPerMinute, JailSentence.BailCost(61));
            Assert.AreEqual(0L, JailSentence.BailCost(0));
            Assert.AreEqual(0L, JailSentence.BailCost(-10));
        }

        [Test]
        public void Clock_ShowsMinutesAndSeconds()
        {
            Assert.AreEqual("04:59", JailSentence.Clock(299));
            Assert.AreEqual("15:00", JailSentence.Clock(900));
            Assert.AreEqual("00:00", JailSentence.Clock(-5));
        }

        [Test]
        public void TermDays_GrowsFromOneToThreeGameDays()
        {
            Assert.AreEqual(1, JailSentence.TermDays(0));
            Assert.AreEqual(2, JailSentence.TermDays(1));
            Assert.AreEqual(3, JailSentence.TermDays(2));
            Assert.AreEqual(3, JailSentence.TermDays(5));
        }

        [Test]
        public void BailCostDays_ChargesFortyThousandPerRemainingDay()
        {
            Assert.AreEqual(40_000L, JailSentence.BailCostDays(1));
            Assert.AreEqual(80_000L, JailSentence.BailCostDays(2));
            Assert.AreEqual(120_000L, JailSentence.BailCostDays(3));
            Assert.AreEqual(0L, JailSentence.BailCostDays(0));
            Assert.AreEqual(0L, JailSentence.BailCostDays(-1));
        }

        [Test]
        public void Arrest_CountsAndBailCharges()
        {
            var loop = new GameLoop();
            loop.ArrestPlayer();
            Assert.AreEqual(1, loop.Arrests);
            Assert.AreEqual(1, loop.JailDaysRemaining);

            long before = loop.State.Cash + loop.State.CreditCard.Balance * -1L;
            loop.PayBail(40_000L);
            long after = loop.State.Cash + loop.State.CreditCard.Balance * -1L;
            Assert.AreEqual(40_000L, before - after);
            Assert.AreEqual(0, loop.JailDaysRemaining);
        }

        [Test]
        public void Arrest_SleepingOnCotServesDayAndAdvancesMorning()
        {
            var loop = new GameLoop();
            loop.ArrestPlayer(); // 1st arrest = 1 day
            loop.ArrestPlayer(); // 2nd arrest = 2 days
            Assert.AreEqual(2, loop.JailDaysRemaining);

            int day = loop.Clock.Day;
            loop.SleepInJail();
            Assert.AreEqual(1, loop.JailDaysRemaining);
            Assert.AreEqual(day + 1, loop.Clock.Day);

            loop.SleepInJail();
            Assert.AreEqual(0, loop.JailDaysRemaining);
            Assert.AreEqual(day + 2, loop.Clock.Day);
        }

        [Test]
        public void Swimsuit_IsBoughtOnceAndPaidInCash()
        {
            var loop = new GameLoop();
            long cash = loop.State.Cash;

            Assert.IsNotNull(loop.BuySwimsuit());
            Assert.IsTrue(loop.HasSwimsuit);
            Assert.AreEqual(cash - MarketAssumptions.SwimsuitPrice, loop.State.Cash);
            Assert.IsNull(loop.BuySwimsuit(), "the second one is not sold");
        }

        [Test]
        public void Swim_NeedsTheSwimsuitAndHappensOncePerMonth()
        {
            var loop = new GameLoop();
            Assert.IsNull(loop.Swim(), "no swimsuit, no swim");

            loop.BuySwimsuit();
            int mood = loop.State.Wellbeing;
            Assert.IsNotNull(loop.Swim());
            Assert.Greater(loop.State.Wellbeing, mood - 1);
            Assert.IsNull(loop.Swim(), "once a month");
        }

        [Test]
        public void NewGame_ForgetsTheSwimsuitAndTheArrests()
        {
            var loop = new GameLoop();
            loop.BuySwimsuit();
            loop.ArrestPlayer();

            loop.NewGame();

            Assert.IsFalse(loop.HasSwimsuit);
            Assert.AreEqual(0, loop.Arrests);
        }
    }
}

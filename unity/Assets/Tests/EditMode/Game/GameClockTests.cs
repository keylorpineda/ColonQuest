using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class GameClockTests
    {
        private const float Hour = GameRules.SecondsPerGameHour;

        [Test]
        public void NewClock_StartsTheFirstWeekAtTheFirstHour()
        {
            var clock = new GameClock();
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(GameRules.DayStartHour, clock.Hour);
            Assert.AreEqual("06:00", clock.Time);
            Assert.AreEqual(0f, clock.DayProgress, 0.0001f);
        }

        [Test]
        public void Advance_MovesOneGameHourPerSecondsPerGameHour()
        {
            var clock = new GameClock();
            clock.Advance(Hour * 8.5f, Hour);
            Assert.AreEqual("14:30", clock.Time);
            Assert.IsFalse(clock.IsNight);

            clock.Advance(Hour * 4f, Hour);
            Assert.IsTrue(clock.IsNight, "18:30 is evening");
        }

        [Test]
        public void Advance_ReportsTheEndOfTheDayOnceAndStopsThere()
        {
            var clock = new GameClock();
            Assert.IsFalse(clock.Advance(Hour * 10f, Hour));
            Assert.IsTrue(clock.Advance(Hour * 10f, Hour), "22:00 reached: time to sleep");
            Assert.AreEqual("22:00", clock.Time);
            Assert.AreEqual(1f, clock.DayProgress, 0.0001f);
            Assert.IsFalse(clock.Advance(Hour, Hour), "it is reported only once");
            Assert.AreEqual("22:00", clock.Time);
        }

        [Test]
        public void Sleep_SkipsToTheNextWeekMorning()
        {
            var clock = new GameClock();
            clock.Advance(Hour * 13f, Hour);
            Assert.IsTrue(clock.SleepToMorning());
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual("06:00", clock.Time);
        }

        [Test]
        public void LastWeek_WaitsForTheBankToCloseTheMonth()
        {
            var clock = new GameClock();
            for (int i = 1; i < GameRules.DaysPerMonth; i++)
            {
                clock.SleepToMorning();
            }

            Assert.IsTrue(clock.IsLastDay);
            Assert.IsFalse(clock.SleepToMorning(), "the month moves on only at the bank");
            Assert.AreEqual(GameRules.DaysPerMonth, clock.Day);

            clock.StartMonth();
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual("06:00", clock.Time);
        }

        [Test]
        public void AfterSleeping_TheEndOfTheDayIsReportedAgain()
        {
            var clock = new GameClock();
            clock.Advance(Hour * 16f, Hour);
            clock.SleepToMorning();
            Assert.IsTrue(clock.Advance(Hour * 16f, Hour));
        }

        [Test]
        public void Loop_SleepMovesTheClockAndRestsOnceAMonth()
        {
            var loop = new GameLoop();
            int mood = loop.State.Wellbeing;

            Assert.IsNotNull(loop.Sleep(), "the first night rests");
            Assert.AreEqual(2, loop.Clock.Day);
            Assert.Greater(loop.State.Wellbeing, mood);

            Assert.IsNull(loop.Sleep(), "the other nights only move the clock");
            Assert.AreEqual(3, loop.Clock.Day);
        }

        [Test]
        public void Loop_FallingAsleepOutsideCostsMoodAndMovesTheClock()
        {
            var loop = new GameLoop();
            int mood = loop.State.Wellbeing;
            var outcome = loop.FallAsleep();

            Assert.Less(loop.State.Wellbeing, mood);
            Assert.Less(outcome.WellbeingDelta, 0);
            Assert.AreEqual(2, loop.Clock.Day);
        }

        [Test]
        public void Loop_ClosingTheMonthStartsTheFirstWeek()
        {
            var loop = new GameLoop();
            loop.Sleep();
            loop.Sleep();
            loop.CloseMonth();
            Assert.AreEqual(1, loop.Clock.Day);
        }
    }
}

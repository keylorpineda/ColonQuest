using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class DayCycleTests
    {
        [Test]
        public void ProgressOf_GoesFromTheFirstMorningToTheLastEvening()
        {
            Assert.AreEqual(0f, DayCycle.ProgressOf(0, 0, 3, 3), 0.0001f);
            Assert.AreEqual(1f / 3f, DayCycle.ProgressOf(1, 0, 3, 3), 0.0001f);
            Assert.AreEqual(2f / 3f, DayCycle.ProgressOf(1, 3, 3, 3), 0.0001f, "all tasks done is the end of the month");
            Assert.AreEqual(1f, DayCycle.ProgressOf(3, 0, 3, 3), 0.0001f);
        }

        [Test]
        public void ProgressOf_NeverLeavesTheRange()
        {
            Assert.AreEqual(1f, DayCycle.ProgressOf(9, 9, 3, 3), 0.0001f);
            Assert.AreEqual(0f, DayCycle.ProgressOf(0, 0, 0, 0), 0.0001f);
        }

        [Test]
        public void TintFor_IsClearAtMiddayAndHeavierAtTheEnds()
        {
            float midday = DayCycle.TintFor(0.35f).a;

            Assert.AreEqual(0f, midday, 0.0001f);
            Assert.Greater(DayCycle.TintFor(0f).a, midday);
            Assert.Greater(DayCycle.TintFor(1f).a, DayCycle.TintFor(0f).a, "dusk is the darkest");
        }

        [Test]
        public void TintFor_DuskIsBluerThanMorning()
        {
            var morning = DayCycle.TintFor(0f);
            var dusk = DayCycle.TintFor(1f);

            Assert.Greater(morning.r, morning.b);
            Assert.Greater(dusk.b, dusk.r);
        }

        [Test]
        public void TintFor_ChangesSmoothly()
        {
            var previous = DayCycle.TintFor(0f);
            for (int i = 1; i <= 100; i++)
            {
                var tint = DayCycle.TintFor(i / 100f);
                Assert.Less(Mathf.Abs(tint.a - previous.a), 0.02f);
                previous = tint;
            }
        }
    }
}

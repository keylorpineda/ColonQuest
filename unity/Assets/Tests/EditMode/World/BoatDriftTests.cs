using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class BoatDriftTests
    {
        [Test]
        public void Bounce_SailsOnInsideTheSpan()
        {
            float speed = 2f;
            Assert.AreEqual(1f, Drift.Bounce(0f, ref speed, 0.5f, -5f, 5f), 0.0001f);
            Assert.AreEqual(2f, speed);
        }

        [Test]
        public void Bounce_TurnsAroundAtTheEndInsteadOfCrossingIt()
        {
            float speed = 2f;
            Assert.AreEqual(4f, Drift.Bounce(4.5f, ref speed, 0.75f, -5f, 5f), 0.0001f);
            Assert.AreEqual(-2f, speed, "it heads back");

            Assert.AreEqual(-4f, Drift.Bounce(-4.5f, ref speed, 0.75f, -5f, 5f), 0.0001f);
            Assert.AreEqual(2f, speed, "and turns again at the other end");
        }

        [Test]
        public void Bounce_TheBoatsNeverReachThePier()
        {
            float west = 0.35f;
            float east = 0.55f;
            float westX = -10f;
            float eastX = 30f;
            for (int frame = 0; frame < 20000; frame++)
            {
                westX = Drift.Bounce(westX, ref west, 0.05f, MapLayout.TownBounds.xMin + 2f, MapLayout.PierXMin - 2.5f);
                eastX = Drift.Bounce(eastX, ref east, 0.05f, MapLayout.PierXMax + 3f, MapLayout.PlayEastX);
                Assert.Less(westX + 1.2f, MapLayout.PierXMin, "the west boat stays west of the pier");
                Assert.Greater(eastX - 1.2f, MapLayout.PierXMax, "the east boat stays east of the pier");
            }
        }
    }
}

using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class GrassBendTests
    {
        [Test]
        public void TargetAngle_FarFromPlayerFollowsTheBreeze()
        {
            Assert.AreEqual(-1.5f, GrassBend.TargetAngle(2f, true, -1.5f), 0.0001f);
        }

        [Test]
        public void TargetAngle_LeansAwayFromPlayerAndStaysWithinLimit()
        {
            float playerEast = GrassBend.TargetAngle(0.1f, true, 0f);
            float playerWest = GrassBend.TargetAngle(0.1f, false, 0f);
            Assert.Greater(playerEast, 0f);
            Assert.Less(playerWest, 0f);
            Assert.LessOrEqual(System.Math.Abs(playerEast), 18f);
            Assert.LessOrEqual(System.Math.Abs(GrassBend.TargetAngle(0f, true, 0f)), 18f);
        }
    }
}
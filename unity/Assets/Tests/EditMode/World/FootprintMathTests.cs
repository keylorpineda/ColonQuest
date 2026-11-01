using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class FootprintMathTests
    {
        [Test]
        public void Alpha_IsFullThenFadesOverTheLastTwoSecondsAndEnds()
        {
            Assert.AreEqual(0.5f, FootprintMath.Alpha(0f, 0.5f), 0.0001f);
            Assert.AreEqual(0.5f, FootprintMath.Alpha(4f, 0.5f), 0.0001f, "full until the last two seconds");
            Assert.AreEqual(0.25f, FootprintMath.Alpha(5f, 0.5f), 0.0001f, "half way through the fade");
            Assert.AreEqual(0f, FootprintMath.Alpha(6f, 0.5f), 0.0001f);
            Assert.AreEqual(0f, FootprintMath.Alpha(9f, 0.5f), 0.0001f);
            Assert.AreEqual(0f, FootprintMath.Alpha(-1f, 0.5f), 0.0001f);
        }

        [Test]
        public void Side_AlternatesBetweenFeet()
        {
            Assert.AreEqual(-1f, FootprintMath.Side(0));
            Assert.AreEqual(1f, FootprintMath.Side(1));
            Assert.AreEqual(-1f, FootprintMath.Side(2));
            Assert.AreEqual(1f, FootprintMath.Side(1001));
        }
    }
}
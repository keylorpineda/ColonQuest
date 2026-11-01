using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class WaterMathTests
    {
        [Test]
        public void Lap_StartsPulledBackReachesTheTopAndStaysInRange()
        {
            Assert.AreEqual(-1f, WaterMath.Lap(0f, 6f, 0f), 0.001f, "a wave starts pulled back");
            Assert.AreEqual(1f, WaterMath.Lap(2f, 6f, 0f), 0.001f, "it reaches the top a third of the way through");
            for (float t = 0f; t < 12f; t += 0.07f)
            {
                float w = WaterMath.Lap(t, 6f, 0.3f);
                Assert.That(w, Is.InRange(-1.0001f, 1.0001f));
            }
        }

        [Test]
        public void Lap_RushesUpFasterThanItSlidesBack()
        {
            // Halfway up takes less time than halfway down.
            float upHalf = WaterMath.Lap(1f, 6f, 0f);
            float downHalf = WaterMath.Lap(2f + 2f, 6f, 0f);
            Assert.Greater(upHalf, 0.4f, "after a sixth of the cycle the water is already well up");
            Assert.That(downHalf, Is.InRange(-0.2f, 0.2f), "it is back around the middle only two sixths after the top");
        }

        [Test]
        public void Lap_WithoutAPeriodStaysStill()
        {
            Assert.AreEqual(0f, WaterMath.Lap(3f, 0f, 0f));
        }

        [Test]
        public void Cycle_StaysBetweenZeroAndOne()
        {
            for (float t = 0f; t < 30f; t += 0.37f)
            {
                Assert.That(WaterMath.Cycle(t, 8f, 0.6f), Is.InRange(0f, 1f));
            }

            Assert.AreEqual(0f, WaterMath.Cycle(5f, 0f, 0.2f));
        }

        [Test]
        public void FadeInOut_IsClearAtBothEndsAndFullInTheMiddle()
        {
            Assert.AreEqual(0f, WaterMath.FadeInOut(0f), 0.001f);
            Assert.AreEqual(1f, WaterMath.FadeInOut(0.5f), 0.001f);
            Assert.AreEqual(0f, WaterMath.FadeInOut(1f), 0.001f);
        }

        [Test]
        public void EdgeFade_FadesNearBothEndsOfTheRiver()
        {
            Assert.AreEqual(0f, WaterMath.EdgeFade(0f, 0f, 10f, 2f), 0.001f);
            Assert.AreEqual(0.5f, WaterMath.EdgeFade(1f, 0f, 10f, 2f), 0.001f);
            Assert.AreEqual(1f, WaterMath.EdgeFade(5f, 0f, 10f, 2f), 0.001f);
            Assert.AreEqual(0.5f, WaterMath.EdgeFade(9f, 0f, 10f, 2f), 0.001f);
            Assert.AreEqual(1f, WaterMath.EdgeFade(5f, 0f, 10f, 0f), "no fade length: fully visible inside");
            Assert.AreEqual(0f, WaterMath.EdgeFade(11f, 0f, 10f, 0f), "and hidden outside");
        }

        [Test]
        public void Ripple_GrowsAndFades()
        {
            WaterMath.Ripple(0f, 0.2f, 1.2f, out float startSize, out float startAlpha);
            WaterMath.Ripple(1f, 0.2f, 1.2f, out float endSize, out float endAlpha);

            Assert.AreEqual(0.2f, startSize, 0.001f);
            Assert.AreEqual(1f, startAlpha, 0.001f);
            Assert.AreEqual(1.2f, endSize, 0.001f);
            Assert.AreEqual(0f, endAlpha, 0.001f);
        }

        [Test]
        public void Rock_StaysWithinItsAngle()
        {
            for (float t = 0f; t < 20f; t += 0.11f)
            {
                Assert.LessOrEqual(Mathf.Abs(WaterMath.Rock(t, 3.5f, 3.4f, 1.2f)), 3.5f + 0.001f);
            }

            Assert.AreEqual(0f, WaterMath.Rock(4f, 3f, 0f, 0f));
        }
    }
}

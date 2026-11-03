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

        [Test]
        public void Wrap_StaysWithinHalfATileAndRepeatsEveryTile()
        {
            for (float v = -9f; v < 9f; v += 0.13f)
            {
                float w = WaterMath.Wrap(v, 2f);
                Assert.That(w, Is.InRange(-1f, 1f));
                Assert.AreEqual(w, WaterMath.Wrap(v + 2f, 2f), 0.001f, "one tile later it is in the same place");
            }

            Assert.AreEqual(0f, WaterMath.Wrap(5f, 0f));
        }

        [Test]
        public void ShimmerDrift_MovesWithTheSignOfItsSpeedAndNeverLeavesATile()
        {
            Assert.AreEqual(0.5f, WaterMath.ShimmerDrift(2f, 0.25f, 2f), 0.001f);
            Assert.AreEqual(-0.36f, WaterMath.ShimmerDrift(2f, -0.18f, 2f), 0.001f);
            for (float t = 0f; t < 60f; t += 0.37f)
            {
                Assert.That(WaterMath.ShimmerDrift(t, -0.8f, 2f), Is.InRange(-1f, 1f));
            }
        }

        [Test]
        public void ShimmerBob_IsBoundedByItsAmplitude()
        {
            Assert.AreEqual(0f, WaterMath.ShimmerBob(0f, 0.4f, 0.15f), 0.0001f);
            for (float t = 0f; t < 40f; t += 0.21f)
            {
                Assert.LessOrEqual(Mathf.Abs(WaterMath.ShimmerBob(t, 0.4f, 0.15f)), 0.15f + 0.0001f);
            }
        }

        [Test]
        public void SplashOffset_RisesInAnArcAndLandsWhereItsSideTakesIt()
        {
            Assert.AreEqual(0f, WaterMath.SplashOffset(0f, 1f, 0.3f, 0.2f).y, 0.0001f);
            Assert.AreEqual(0.3f, WaterMath.SplashOffset(0.5f, 1f, 0.3f, 0.2f).y, 0.0001f, "the arc peaks halfway");
            var end = WaterMath.SplashOffset(1f, -1f, 0.3f, 0.2f);
            Assert.AreEqual(0f, end.y, 0.0001f);
            Assert.AreEqual(-0.2f, end.x, 0.0001f);
        }

        [Test]
        public void SplashAlpha_IsSolidThenFadesToNothing()
        {
            Assert.AreEqual(1f, WaterMath.SplashAlpha(0.3f), 0.0001f);
            Assert.AreEqual(0f, WaterMath.SplashAlpha(1f), 0.0001f);
            Assert.That(WaterMath.SplashAlpha(0.85f), Is.InRange(0.1f, 0.9f));
        }

        [Test]
        public void Push_MovesThingsAwayFromTheWalkerOnlyWhenClose()
        {
            var push = WaterMath.Push(new Vector2(1f, 0f), Vector2.zero, 2f, 4f);
            Assert.Greater(push.x, 0f, "pushed away from the walker");
            Assert.AreEqual(2f, push.magnitude, 0.0001f, "half way into the radius, half the strength");
            Assert.AreEqual(Vector2.zero, WaterMath.Push(new Vector2(3f, 0f), Vector2.zero, 2f, 4f), "too far to feel it");
            Assert.Greater(WaterMath.Push(Vector2.zero, Vector2.zero, 2f, 4f).magnitude, 0f, "right on top still gets a direction");
        }

        [Test]
        public void KeepInside_NeverLeavesTheWater()
        {
            var pond = new Rect(0f, 0f, 4f, 2f);
            Assert.AreEqual(new Vector2(2f, 1f), WaterMath.KeepInside(new Vector2(2f, 1f), pond, 0.3f));
            var clamped = WaterMath.KeepInside(new Vector2(9f, -5f), pond, 0.3f);
            Assert.AreEqual(3.7f, clamped.x, 0.0001f);
            Assert.AreEqual(0.3f, clamped.y, 0.0001f);
            var tiny = WaterMath.KeepInside(new Vector2(5f, 5f), new Rect(0f, 0f, 0.2f, 0.2f), 0.3f);
            Assert.AreEqual(0.3f, tiny.x, 0.0001f, "a margin bigger than the pond still gives a point");
        }

        [Test]
        public void JumpHeight_IsAnArcThatStartsAndEndsInTheWater()
        {
            Assert.AreEqual(0f, WaterMath.JumpHeight(0f, 0.5f), 0.0001f);
            Assert.AreEqual(0.5f, WaterMath.JumpHeight(0.5f, 0.5f), 0.0001f);
            Assert.AreEqual(0f, WaterMath.JumpHeight(1f, 0.5f), 0.0001f);
        }
    }
}

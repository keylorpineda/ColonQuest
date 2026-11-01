using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class LeafMathTests
    {
        [Test]
        public void Offset_StartsAtOriginAndFallsTheFullDistance()
        {
            Assert.AreEqual(Vector2.zero, LeafMath.Offset(0f, 1f));
            Assert.AreEqual(-LeafMath.FallDistance, LeafMath.Offset(1f, 1f).y, 0.0001f);
        }

        [Test]
        public void Offset_SwayStaysInBoundsAndFollowsSide()
        {
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                Vector2 right = LeafMath.Offset(t, 1f);
                Vector2 left = LeafMath.Offset(t, -1f);
                Assert.LessOrEqual(Mathf.Abs(right.x), LeafMath.SwayAmplitude + 0.0001f);
                Assert.AreEqual(-right.x, left.x, 0.0001f);
            }
        }

        [Test]
        public void Alpha_IsOpaqueThenFadesToZero()
        {
            Assert.AreEqual(1f, LeafMath.Alpha(0f), 0.0001f);
            Assert.AreEqual(1f, LeafMath.Alpha(0.7f), 0.0001f);
            Assert.AreEqual(0.5f, LeafMath.Alpha(0.85f), 0.001f);
            Assert.AreEqual(0f, LeafMath.Alpha(1f), 0.0001f);
        }
    }
}
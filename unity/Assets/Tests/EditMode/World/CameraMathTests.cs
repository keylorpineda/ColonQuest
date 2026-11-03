using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class CameraMathTests
    {
        private static readonly Rect Bounds = new Rect(-10f, -5f, 20f, 10f);
        private static readonly Vector2 HalfView = new Vector2(4f, 2f);

        [Test]
        public void ClampCenter_InsideTheBounds_IsUnchanged()
        {
            Assert.AreEqual(new Vector2(1f, 1f), CameraMath.ClampCenter(new Vector2(1f, 1f), HalfView, Bounds));
        }

        [Test]
        public void ClampCenter_NearAnEdge_StopsWhereTheViewTouchesTheEdge()
        {
            var clamped = CameraMath.ClampCenter(new Vector2(20f, -20f), HalfView, Bounds);

            Assert.AreEqual(6f, clamped.x, 0.0001f);
            Assert.AreEqual(-3f, clamped.y, 0.0001f);
        }

        [Test]
        public void ClampCenter_WhenTheViewIsLargerThanTheBounds_CentersOnThem()
        {
            var clamped = CameraMath.ClampCenter(new Vector2(7f, 7f), new Vector2(30f, 20f), Bounds);

            Assert.AreEqual(Vector2.zero, clamped);
        }
    }
}

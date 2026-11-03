using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class MovementMathTests
    {
        [Test]
        public void Velocity_WithNoInput_IsZero()
        {
            Assert.AreEqual(Vector2.zero, MovementMath.Velocity(Vector2.zero, 5f));
        }

        [Test]
        public void Velocity_MovingStraight_EqualsSpeed()
        {
            Assert.AreEqual(5f, MovementMath.Velocity(Vector2.right, 5f).magnitude, 0.0001f);
        }

        [Test]
        public void Velocity_MovingDiagonally_IsNotFasterThanStraight()
        {
            var diagonal = MovementMath.Velocity(new Vector2(1f, 1f), 5f);

            Assert.AreEqual(5f, diagonal.magnitude, 0.0001f);
        }

        [Test]
        public void Velocity_WithPartialInput_ScalesWithTheInput()
        {
            Assert.AreEqual(2.5f, MovementMath.Velocity(new Vector2(0.5f, 0f), 5f).magnitude, 0.0001f);
        }

        [Test]
        public void FacingSign_WhenIdle_KeepsTheCurrentSide()
        {
            Assert.AreEqual(-1, MovementMath.FacingSign(0f, -1));
            Assert.AreEqual(1, MovementMath.FacingSign(0f, 1));
        }

        [Test]
        public void FacingSign_FollowsTheHorizontalInput()
        {
            Assert.AreEqual(1, MovementMath.FacingSign(0.8f, -1));
            Assert.AreEqual(-1, MovementMath.FacingSign(-0.8f, 1));
        }
    }
}

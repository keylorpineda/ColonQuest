using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class PersonMotionTests
    {
        [Test]
        public void Legs_SwingInOppositeDirections()
        {
            float left = PersonMotion.LegAngle(1f, 1f, true);
            float right = PersonMotion.LegAngle(1f, 1f, false);

            Assert.AreEqual(-left, right, 0.0001f);
            Assert.AreNotEqual(0f, left);
        }

        [Test]
        public void Legs_StayStillWhenStanding()
        {
            Assert.AreEqual(0f, PersonMotion.LegAngle(2f, 0f, true), 0.0001f);
            Assert.AreEqual(0f, PersonMotion.FootLift(2f, 0f, false), 0.0001f);
        }

        [Test]
        public void Arms_SwingAgainstTheLegOnTheSameSide()
        {
            float leg = PersonMotion.LegAngle(1f, 1f, true);
            float arm = PersonMotion.ArmAngle(1f, 1f, true);

            Assert.IsTrue(leg * arm < 0f);
        }

        [Test]
        public void Foot_OnlyLiftsWhileStepping()
        {
            Assert.GreaterOrEqual(PersonMotion.FootLift(1f, 1f, true), 0f);
            Assert.AreEqual(0f, PersonMotion.FootLift(1f, 1f, false), 0.0001f);
        }

        [Test]
        public void Wave_RaisesTheArmShakesItAndLowersIt()
        {
            Assert.AreEqual(0f, PersonMotion.WaveAngle(-0.1f), 0.0001f);
            Assert.Greater(PersonMotion.WaveAngle(0.15f), 60f, "halfway through the lift");
            Assert.Greater(PersonMotion.WaveAngle(1f), 120f, "raised while shaking");
            Assert.AreEqual(0f, PersonMotion.WaveAngle(PersonMotion.WaveDuration + 0.1f), 0.0001f);
        }

        [Test]
        public void Wave_ShakesFromSideToSide()
        {
            float a = PersonMotion.WaveAngle(0.55f);
            float b = PersonMotion.WaveAngle(0.66f);

            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Blink_ClosesAndOpensAgain()
        {
            Assert.AreEqual(0f, PersonMotion.BlinkClosed(-1f), 0.0001f);
            Assert.AreEqual(1f, PersonMotion.BlinkClosed(0.09f), 0.01f);
            Assert.AreEqual(0f, PersonMotion.BlinkClosed(0.5f), 0.0001f);
        }

        [Test]
        public void NextBlink_IsAFewSecondsAway()
        {
            Assert.AreEqual(1.8f, PersonMotion.NextBlinkDelay(0f), 0.0001f);
            Assert.AreEqual(5.2f, PersonMotion.NextBlinkDelay(1f), 0.0001f);
        }

        [Test]
        public void Pupils_LookTowardTheTargetButNeverFarther_ThanTheLimit()
        {
            var offset = PersonMotion.PupilOffset(new Vector2(30f, 0f), 0.03f);

            Assert.AreEqual(0.03f, offset.magnitude, 0.0001f);
            Assert.Greater(offset.x, 0f);
        }

        [Test]
        public void Pupils_StayCentered_OnATargetOnTopOfThePerson()
        {
            Assert.AreEqual(Vector2.zero, PersonMotion.PupilOffset(Vector2.zero, 0.03f));
        }

        [Test]
        public void Mouth_IsClosedUnlessTalking()
        {
            Assert.AreEqual(0.25f, PersonMotion.MouthOpen(3f, false), 0.0001f);
            Assert.GreaterOrEqual(PersonMotion.MouthOpen(0.17f, true), 0.25f);
            Assert.LessOrEqual(PersonMotion.MouthOpen(0.17f, true), 1f);
        }

        [Test]
        public void HeadTilt_TurnsTowardTheSideAndIsLimited()
        {
            Assert.Less(PersonMotion.HeadTilt(2f), 0f);
            Assert.Greater(PersonMotion.HeadTilt(-2f), 0f);
            Assert.AreEqual(9f, PersonMotion.HeadTilt(-50f), 0.0001f);
        }
    }
}

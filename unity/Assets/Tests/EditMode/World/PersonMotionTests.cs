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

        [Test]
        public void ElbowAngle_BendsWhileWavingAndIsZeroOutside()
        {
            Assert.AreEqual(0f, PersonMotion.ElbowAngle(-0.1f), 0.0001f);
            Assert.AreEqual(0f, PersonMotion.ElbowAngle(PersonMotion.WaveDuration + 0.1f), 0.0001f);
            Assert.Greater(PersonMotion.ElbowAngle(1f), 25f, "bent while the arm is raised");
            Assert.LessOrEqual(PersonMotion.ElbowAngle(1f), 48.01f);
            Assert.Less(PersonMotion.ElbowAngle(0.05f), PersonMotion.ElbowAngle(1f), "it bends as the arm lifts");
        }
    

        [Test]
        public void FacingAway_NeedsAClearWalkUpAndHoldsOnDiagonals()
        {
            Assert.IsTrue(PersonMotion.FacingAway(new Vector2(0f, 4f), false), "walking straight up shows the back");
            Assert.IsFalse(PersonMotion.FacingAway(new Vector2(0f, -4f), false), "walking down shows the face");
            Assert.IsFalse(PersonMotion.FacingAway(new Vector2(4f, 3f), false), "mostly sideways keeps the face");
            Assert.IsFalse(PersonMotion.FacingAway(new Vector2(0f, 0.3f), false), "a drift is not a walk");
            Assert.IsTrue(PersonMotion.FacingAway(new Vector2(3f, 3f), true), "once turned, a diagonal keeps the back");
            Assert.IsFalse(PersonMotion.FacingAway(new Vector2(4f, 1f), true), "a clearly sideways walk turns back to the face");
            Assert.IsFalse(PersonMotion.FacingAway(Vector2.zero, true), "standing still shows the face");
        }

        [Test]
        public void StepSquash_IsGentleAndOnlyWhileWalking()
        {
            Assert.AreEqual(1f, PersonMotion.StepSquash(0f, 0f), 0.0001f);
            Assert.AreEqual(1f, PersonMotion.StepSquash(1f, 0f), 0.0001f);
            Assert.Less(PersonMotion.StepSquash(0f, 1f), 1f, "a little shorter as the foot lands");
            Assert.Greater(PersonMotion.StepSquash(1f, 1f), 1f, "a little taller between steps");
            Assert.That(PersonMotion.StepSquash(1f, 1f), Is.InRange(0.94f, 1.08f));
            Assert.That(PersonMotion.StepSquash(0f, 1f), Is.InRange(0.94f, 1.08f));
        }

        [Test]
        public void WalkLean_LeansForwardOnlyWhenWalkingSideways()
        {
            Assert.AreEqual(0f, PersonMotion.WalkLean(1f, 0f), 0.0001f, "walking up or down stays upright");
            Assert.AreEqual(0f, PersonMotion.WalkLean(0f, 1f), 0.0001f, "standing stays upright");
            Assert.Less(PersonMotion.WalkLean(1f, 1f), 0f, "negative leans toward the way the person faces");
            Assert.GreaterOrEqual(PersonMotion.WalkLean(1f, 1f), -5f);
        }

        [Test]
        public void ElbowSwing_ZeroWhenStanding_BendsBetweenTenAndThirtyFiveWhenWalking()
        {
            Assert.AreEqual(0f, PersonMotion.ElbowSwing(1f, 0f, true), 0.0001f);
            Assert.AreEqual(0f, PersonMotion.ElbowSwing(1f, 0f, false), 0.0001f);

            float left = Mathf.Abs(PersonMotion.ElbowSwing(1f, 1f, true));
            float right = Mathf.Abs(PersonMotion.ElbowSwing(1f, 1f, false));
            Assert.That(left, Is.InRange(10f, 35f));
            Assert.That(right, Is.InRange(10f, 35f));
        }

        [Test]
        public void SwayTarget_LimitsAngleAndAddsTorsoLean()
        {
            float normal = PersonMotion.SwayTarget(2f, 4f);
            Assert.AreEqual(-2f * 2.2f + 2f, normal, 0.001f);

            float extremeFast = PersonMotion.SwayTarget(100f, 0f);
            Assert.AreEqual(-14f, extremeFast, 0.001f);

            float extremeSlow = PersonMotion.SwayTarget(-100f, 0f);
            Assert.AreEqual(14f, extremeSlow, 0.001f);
        }

        [Test]
        public void ShadowScale_NarrowsToEightyFivePercentWhileWalking()
        {
            Assert.AreEqual(1f, PersonMotion.ShadowScale(0f, 0f), 0.001f);
            Assert.AreEqual(1f, PersonMotion.ShadowScale(1f, 0f), 0.001f);
            Assert.AreEqual(0.85f, PersonMotion.ShadowScale(1f, 1f), 0.001f);
        }

        [Test]
        public void Mood_AltersBrowsAndMouthRest()
        {
            Assert.AreEqual(10f, PersonMotion.BrowTilt(0.2f));
            Assert.AreEqual(0f, PersonMotion.BrowTilt(0.5f));

            Assert.AreEqual(0.25f, PersonMotion.MoodMouthRest(0.2f, 0.4f));
            Assert.GreaterOrEqual(PersonMotion.MoodMouthRest(0.9f, 0.25f), 0.55f);
            Assert.AreEqual(0.3f, PersonMotion.MoodMouthRest(0.5f, 0.3f));
        }

        [Test]
        public void TurnWidth_FormsCosineCurveBetweenFullAndPointFifteen()
        {
            Assert.AreEqual(1f, PersonMotion.TurnWidth(0f), 0.001f);
            Assert.AreEqual(1f, PersonMotion.TurnWidth(1f), 0.001f);
            Assert.AreEqual(0.15f, PersonMotion.TurnWidth(0.5f), 0.001f);
        }
    }
}

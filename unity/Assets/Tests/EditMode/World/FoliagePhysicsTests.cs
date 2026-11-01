using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class FoliagePhysicsTests
    {
        [Test]
        public void WindAngle_StaysWithinConfiguredBoundsAndVariesOverTime()
        {
            float a = FoliagePhysics.ComputeWindAngle(0.2f, 1.5f, 4f, 0.5f);
            float b = FoliagePhysics.ComputeWindAngle(1.4f, 1.5f, 4f, 0.5f);

            Assert.LessOrEqual(Mathf.Abs(a), 5f);
            Assert.LessOrEqual(Mathf.Abs(b), 5f);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void WindScale_PreservesInitialScaleWhenBreathIsZero()
        {
            var initial = new Vector3(1.5f, 2.0f, 1f);
            var scaled = FoliagePhysics.ComputeWindScale(initial, 3.2f, 1.4f, 0f, 1.1f);

            Assert.AreEqual(initial, scaled);
        }

        [Test]
        public void StepSpring_DampsImpulseBackToRest()
        {
            float displacement = 0f;
            float velocity = 25f;

            float peak = 0f;
            for (int i = 0; i < 240; i++)
            {
                FoliagePhysics.StepSpring(ref displacement, ref velocity, 42f, 5.5f, 1f / 60f);
                peak = Mathf.Max(peak, Mathf.Abs(displacement));
            }

            Assert.Greater(peak, 0.5f, "spring deflects after an impulse");
            Assert.Less(Mathf.Abs(displacement), 0.05f, "spring settles back near rest");
        }
    

        [Test]
        public void StepSpring_RustleConstantsBounceVisiblyThenSettle()
        {
            float displacement = 0f;
            float velocity = 40f;
            float peak = 0f;
            for (int i = 0; i < 240; i++)
            {
                FoliagePhysics.StepSpring(ref displacement, ref velocity, FoliagePhysics.RustleStiffness, FoliagePhysics.RustleDamping, 1f / 60f);
                peak = Mathf.Max(peak, Mathf.Abs(displacement));
            }

            Assert.Greater(peak, 4f, "the shake is clearly visible");
            Assert.Less(Mathf.Abs(displacement), 0.1f, "it settles back to rest");
        }

        [Test]
        public void GlobalWind_StaysInRangeAndRollsAcrossTheTown()
        {
            for (float t = 0f; t < 60f; t += 0.7f)
            {
                Assert.That(FoliagePhysics.GlobalWindStrength(t, 3f, 0.5f), Is.InRange(0f, 1f));
                Assert.That(FoliagePhysics.GlobalWindStrength(t, -40f, 0f), Is.InRange(0f, 1f));
                Assert.That(FoliagePhysics.GlobalWindStrength(t, 60f, 1f), Is.InRange(0f, 1f));
            }

            // A tree 12 units further east feels the same gust 12 / GustSpeed seconds later.
            float west = FoliagePhysics.GlobalWindStrength(5f, 0f, 0.4f);
            float east = FoliagePhysics.GlobalWindStrength(5f + 12f / FoliagePhysics.GustSpeed, 12f, 0.4f);
            Assert.AreEqual(west, east, 0.0001f);
        }

        [Test]
        public void WindLean_BendsEastMoreInAGust()
        {
            float calm = FoliagePhysics.WindLean(0f, 3f);
            float gust = FoliagePhysics.WindLean(1f, 3f);

            Assert.Less(calm, 0f, "even a calm breeze leans the crown a little to the east");
            Assert.Less(gust, calm);
            Assert.AreEqual(-3f, gust, 0.0001f, "a full gust reaches the configured angle and no more");
        }

        [Test]
        public void PivotPosition_SwingsTheCrownOnItsTrunk()
        {
            var rest = new Vector3(0f, 1.6f, 0f);

            Assert.AreEqual(rest, FoliagePhysics.PivotPosition(rest, 0f, 10f), "no pivot: it turns in place");
            Assert.AreEqual(rest, FoliagePhysics.PivotPosition(rest, 1.6f, 0f), "no angle: it stays put");

            var leanRight = FoliagePhysics.PivotPosition(rest, 1.6f, -5f);
            Assert.Greater(leanRight.x, rest.x, "bending clockwise moves the crown to the right");
            Assert.Less(leanRight.y, rest.y, "and a little down, around the foot of the trunk");
            Assert.AreEqual(1.6f, Vector2.Distance(leanRight, Vector2.zero), 0.0001f, "it keeps its distance to the pivot");
        }

        [Test]
        public void StepSpringToward_SettlesOnItsTarget()
        {
            float bend = 0f;
            float velocity = 0f;
            for (int i = 0; i < 600; i++)
            {
                FoliagePhysics.StepSpringToward(ref bend, ref velocity, -2f, 8.4f, 2f, 1f / 60f);
            }

            Assert.AreEqual(-2f, bend, 0.01f);
        }
    }
}

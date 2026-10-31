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
    }
}

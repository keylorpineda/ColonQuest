using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class IdleActionsTests
    {
        [Test]
        public void Pick_NeverRepeatsLastPose()
        {
            for (int i = 0; i < IdleActions.Poses.Length; i++)
            {
                var last = IdleActions.Poses[i];
                for (float r = 0f; r <= 1f; r += 0.05f)
                {
                    var picked = IdleActions.Pick(r, last);
                    Assert.AreNotEqual(last, picked);
                }
            }
        }

        [Test]
        public void Delays_AreWithinExpectedRanges()
        {
            Assert.That(IdleActions.NextDelay(0f), Is.EqualTo(6f));
            Assert.That(IdleActions.NextDelay(1f), Is.EqualTo(9f));

            Assert.That(IdleActions.HoldDuration(0f), Is.EqualTo(1.5f));
            Assert.That(IdleActions.HoldDuration(1f), Is.EqualTo(2.5f));
        }

        [Test]
        public void Poses_ContainsAllFivePoses()
        {
            Assert.AreEqual(5, IdleActions.Poses.Length);
            CollectionAssert.Contains(IdleActions.Poses, PersonPose.Stretch);
            CollectionAssert.Contains(IdleActions.Poses, PersonPose.ScratchHead);
            CollectionAssert.Contains(IdleActions.Poses, PersonPose.LookAround);
            CollectionAssert.Contains(IdleActions.Poses, PersonPose.CheckPhone);
            CollectionAssert.Contains(IdleActions.Poses, PersonPose.ShiftWeight);
        }
    }
}

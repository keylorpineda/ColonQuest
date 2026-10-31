using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class DoorTests
    {
        [Test]
        public void DoorWithoutDirection_TakesAnyStep()
        {
            Assert.IsTrue(Door.AllowsEntry(Vector2.zero, Vector2.zero));
        }

        [Test]
        public void WalkingTowardTheDoor_Enters()
        {
            Assert.IsTrue(Door.AllowsEntry(Vector2.up, new Vector2(0f, 4.5f)));
            Assert.IsTrue(Door.AllowsEntry(Vector2.up, new Vector2(2f, 4f)));
        }

        [Test]
        public void WalkingAlongTheSidewalkOrStandingStill_DoesNotEnter()
        {
            Assert.IsFalse(Door.AllowsEntry(Vector2.up, new Vector2(4.5f, 0f)));
            Assert.IsFalse(Door.AllowsEntry(Vector2.up, new Vector2(0f, -4.5f)));
            Assert.IsFalse(Door.AllowsEntry(Vector2.up, Vector2.zero));
        }

        [Test]
        public void WalkThroughADoor_KeepsTheStraightWayOfTheStep()
        {
            Assert.AreEqual(Vector2.down, SceneTransition.WalkDirection(new Vector2(0.8f, -4.4f)));
            Assert.AreEqual(Vector2.right, SceneTransition.WalkDirection(new Vector2(4.5f, 1f)));
            Assert.AreEqual(Vector2.zero, SceneTransition.WalkDirection(Vector2.zero), "a teleport walks nowhere");
        }
    }
}

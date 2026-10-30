using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class CrossingTests
    {
        [Test]
        public void Light_IsGreenForTheWalkTimeAndRedForTheWaitTime()
        {
            Assert.IsTrue(CrossingClock.IsWalk(0f, 0f));
            Assert.IsTrue(CrossingClock.IsWalk(CrossingClock.WalkSeconds - 0.1f, 0f));
            Assert.IsFalse(CrossingClock.IsWalk(CrossingClock.WalkSeconds + 0.1f, 0f));
            Assert.IsFalse(CrossingClock.IsWalk(CrossingClock.Cycle - 0.1f, 0f));
        }

        [Test]
        public void Light_RepeatsEveryCycle()
        {
            Assert.AreEqual(CrossingClock.IsWalk(3f, 0f), CrossingClock.IsWalk(3f + CrossingClock.Cycle * 4f, 0f));
            Assert.AreEqual(CrossingClock.IsWalk(15f, 0f), CrossingClock.IsWalk(15f + CrossingClock.Cycle * 4f, 0f));
        }

        [Test]
        public void Phase_MovesOneCrosswalkAgainstAnother()
        {
            Assert.IsTrue(CrossingClock.IsWalk(0f, 0f));
            Assert.IsFalse(CrossingClock.IsWalk(0f, CrossingClock.WalkSeconds + 1f));
        }

        [Test]
        public void Phase_NegativeTimesStillWork()
        {
            Assert.IsFalse(CrossingClock.IsWalk(-1f, 0f));
        }

        [Test]
        public void Offense_OnlyOnTheCrosswalkWhileTheLightSaysWait()
        {
            Assert.IsTrue(CrossingRules.IsRedLightOffense(true, false, false));
            Assert.IsFalse(CrossingRules.IsRedLightOffense(true, true, false), "green light");
            Assert.IsFalse(CrossingRules.IsRedLightOffense(false, false, false), "not on the crosswalk");
            Assert.IsFalse(CrossingRules.IsRedLightOffense(true, false, true), "already fined at this crossing");
        }

        [Test]
        public void Officer_StopsAPlayerWhoCrossesOnRedAndIgnoresOneWhoWaits()
        {
            Assert.AreEqual(OfficerAction.Pursue, OfficerLogic.Decide(UnityEngine.Vector2.zero, new UnityEngine.Vector2(4f, 0f), OffenseKind.RedLight, false));
            Assert.AreEqual(OfficerAction.Idle, OfficerLogic.Decide(UnityEngine.Vector2.zero, new UnityEngine.Vector2(1f, 0f), OffenseKind.None, false));
        }
    }
}

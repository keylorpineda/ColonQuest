using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class FurnitureTests
    {
        [Test]
        public void FreeSpot_KeepsAPointThatIsAlreadyFree()
        {
            var point = new Vector2(3f, 4f);
            Assert.AreEqual(point, FreeSpot.Search(point, _ => true));
        }

        [Test]
        public void FreeSpot_StopsInFrontOfFurnitureInsteadOfInsideIt()
        {
            // A bench: solid from y 0 to 0.6, two units wide. The scene aims at its seat.
            var bench = Rect.MinMaxRect(-1f, 0f, 1f, 0.6f);
            var seat = new Vector2(0f, 0.3f);
            Vector2 spot = FreeSpot.Search(seat, p => !bench.Overlaps(Rect.MinMaxRect(p.x - 0.3f, p.y - 0.3f, p.x + 0.3f, p.y + 0.3f)));

            Assert.AreEqual(0f, spot.x, 0.0001f, "straight below the seat");
            Assert.Less(spot.y, -0.3f + 0.0001f, "out of the bench");
            Assert.Greater(spot.y, -0.5f, "and right in front of it");
        }

        [Test]
        public void FreeSpot_TriesBelowFirstInEveryRing()
        {
            var first = FreeSpot.Candidates(Vector2.zero).First();
            Assert.AreEqual(0f, first.x, 0.0001f);
            Assert.AreEqual(-FreeSpot.RingStep, first.y, 0.0001f);

            var ring = FreeSpot.Candidates(Vector2.zero).Take(FreeSpot.Directions).ToList();
            Assert.AreEqual(FreeSpot.RingStep, ring.Last().y, 0.0001f, "above is the last resort of the ring");
        }

        [Test]
        public void FreeSpot_ReturnsThePointWhenNothingIsFree()
        {
            var point = new Vector2(1f, 1f);
            Assert.AreEqual(point, FreeSpot.Search(point, _ => false));
        }

        [Test]
        public void Sit_KeepsTheLegsDownAndBarelyDrops()
        {
            foreach (var pose in new[] { PersonPose.Sit, PersonPose.SitEat })
            {
                var angles = PersonPoses.Evaluate(pose, 1f);
                Assert.LessOrEqual(Mathf.Abs(angles.LegL), 8f, pose + " left leg");
                Assert.LessOrEqual(Mathf.Abs(angles.LegR), 8f, pose + " right leg");
                Assert.GreaterOrEqual(angles.Drop, -0.05f, pose + " drop");
                Assert.IsTrue(PersonPoses.MovesBody(pose));
            }
        }

        [Test]
        public void StandingPoses_DoNotBendTheLegs()
        {
            foreach (var pose in new[] { PersonPose.Read, PersonPose.Eat })
            {
                var angles = PersonPoses.Evaluate(pose, 1f);
                Assert.AreEqual(0f, angles.LegL, pose.ToString());
                Assert.AreEqual(0f, angles.Drop, pose.ToString());
                Assert.IsFalse(PersonPoses.MovesBody(pose), pose.ToString());
            }
        }

        [Test]
        public void HandsInFront_OnlyForPosesThatBringThemToTheChest()
        {
            Assert.IsTrue(PersonPoses.HandsInFront(PersonPose.Eat));
            Assert.IsTrue(PersonPoses.HandsInFront(PersonPose.Type));
            Assert.IsTrue(PersonPoses.HandsInFront(PersonPose.Sign));
            Assert.IsFalse(PersonPoses.HandsInFront(PersonPose.None));
            Assert.IsFalse(PersonPoses.HandsInFront(PersonPose.Wave));
            Assert.IsFalse(PersonPoses.HandsInFront(PersonPose.Handover));
        }

        [Test]
        public void WaterThought_DependsOnTheWaterAndTheSwimsuit()
        {
            Assert.AreEqual(WalkWatch.WaterThought, WalkWatch.ThoughtFor(WaterBody.Sea, false));
            Assert.AreEqual(WalkWatch.SeaThought, WalkWatch.ThoughtFor(WaterBody.Sea, true));
            Assert.AreEqual(WalkWatch.RiverThought, WalkWatch.ThoughtFor(WaterBody.River, true));
            Assert.AreEqual(WalkWatch.PondThought, WalkWatch.ThoughtFor(WaterBody.Pond, false));
        }

        [Test]
        public void WaterHints_SayWhichWaterItIs()
        {
            Assert.AreEqual(WaterBody.River, WorldHints.Check(SceneNames.Town, new Vector2(29.5f, 15f)).Water);
            Assert.AreEqual(WaterBody.Sea, WorldHints.Check(SceneNames.Town, new Vector2(-10f, MapLayout.ShoreY + 0.3f)).Water);
            Assert.AreEqual(WaterBody.Pond, WorldHints.Check(SceneNames.Town, new Vector2(19.5f, -5.9f)).Water);
        }

        [Test]
        public void TheSandAboveTheBeachStation_IsNotWater()
        {
            // The swim ends on the sand above the station at (-12, -25.6): no swimsuit warning there.
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, new Vector2(-12f, -25.4f)).Kind);
        }

        [Test]
        public void SittingScene_GoesBackInFrontBeforeTheEnd()
        {
            var scene = new Interaction().SitOn(new Vector2(0f, 0.3f), 2f).Apply(() => { });
            Assert.AreEqual(3, scene.Count, "walk onto the seat, sit, apply");
            Assert.AreEqual(2f, scene.WaitSeconds, 0.0001f);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class InteractionTests
    {
        [Test]
        public void Steps_AreCountedInOrderAndOnlyApplyChangesTheGame()
        {
            int applied = 0;
            var scene = new Interaction()
                .WalkTo(Vector2.zero)
                .Face(Vector2.one)
                .Pose(PersonPose.Handover, 0.7f)
                .Sound(SoundEffectKind.Bills)
                .Fly("bills", Vector2.zero, Vector2.up, 0.8f)
                .Wait(0.5f)
                .Apply(() => applied++);

            Assert.AreEqual(7, scene.Count);
            Assert.AreEqual(1, scene.ApplyCount);
            Assert.AreEqual(0, applied, "building a scene never runs the action");
        }

        [Test]
        public void WaitSeconds_AddsPosesWaitsAndFades()
        {
            var scene = new Interaction()
                .Pose(PersonPose.Sleep, 0.8f)
                .Fade(Color.black, 1.4f)
                .Wait(1.4f)
                .Sound(SoundEffectKind.Alarm)
                .Say(string.Empty, "x");

            Assert.AreEqual(3.6f, scene.WaitSeconds, 0.0001f);
        }

        [Test]
        public void EmptyScene_DoesNotPlay()
        {
            Assert.IsFalse(new Interaction().Play());
            Assert.IsFalse(Interaction.Playing);
        }

        [Test]
        public void Poses_StartAtRestAndNoneChangesNothing()
        {
            var none = PersonPoses.Evaluate(PersonPose.None, 3f);
            Assert.AreEqual(0f, none.ArmL);
            Assert.AreEqual(0f, none.ArmR);
            Assert.AreEqual(0f, none.Roll);
            Assert.AreEqual(0f, none.Drop);
        }

        [Test]
        public void Sleep_LiesDownAndLowersTheBody()
        {
            var sleep = PersonPoses.Evaluate(PersonPose.Sleep, 1f);
            Assert.AreEqual(90f, sleep.Roll, 0.0001f);
            Assert.Less(sleep.Drop, 0f);
            Assert.IsTrue(PersonPoses.MovesBody(PersonPose.Sleep));
            Assert.IsFalse(PersonPoses.MovesBody(PersonPose.Handover));
        }

        [Test]
        public void Handover_ReachesOutWithTheRightArm()
        {
            Assert.AreEqual(0f, PersonPoses.Evaluate(PersonPose.Handover, 0f).ArmR, 0.0001f);
            Assert.Greater(PersonPoses.Evaluate(PersonPose.Handover, 1f).ArmR, 55f);
        }

        [Test]
        public void EveryPose_StaysInsideTheLimitsOfTheRig()
        {
            foreach (PersonPose pose in System.Enum.GetValues(typeof(PersonPose)))
            {
                for (float t = 0f; t < 4f; t += 0.13f)
                {
                    var a = PersonPoses.Evaluate(pose, t);
                    Assert.LessOrEqual(Mathf.Abs(a.ArmL), 185f, pose + " left arm");
                    Assert.LessOrEqual(Mathf.Abs(a.ArmR), 185f, pose + " right arm");
                    Assert.LessOrEqual(Mathf.Abs(a.ElbowL), 150f, pose + " left elbow");
                    Assert.LessOrEqual(Mathf.Abs(a.ElbowR), 150f, pose + " right elbow");
                    Assert.LessOrEqual(Mathf.Abs(a.Drop), 0.5f, pose + " drop");
                }
            }
        }

        [Test]
        public void EveryEffect_MakesAClipOfItsLength()
        {
            foreach (SoundEffectKind kind in System.Enum.GetValues(typeof(SoundEffectKind)))
            {
                var clip = ProceduralSound.Effect(kind);
                Assert.IsNotNull(clip, kind.ToString());
                Assert.AreEqual(Mathf.RoundToInt(ProceduralSound.EffectSeconds(kind) * ProceduralSound.Rate), clip.samples, kind.ToString());
                Object.DestroyImmediate(clip);
            }
        }
    }
}

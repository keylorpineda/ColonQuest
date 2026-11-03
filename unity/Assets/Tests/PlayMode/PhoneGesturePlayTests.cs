using System.Collections;
using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>The phone comes out of the pocket, stays in the hand while it is open and goes back when it closes.</summary>
    public sealed class PhoneGesturePlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static GameObject Held => GameObject.Find("Prop_phone");

        private static IEnumerator WaitWhile(System.Func<bool> condition, float seconds)
        {
            for (float t = 0f; t < seconds && condition(); t += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Phone_StaysInTheHandUntilTheWindowCloses()
        {
            yield return null;
            bool open = true;
            bool played = new Interaction().Pose(PersonPose.Pocket, 0.05f).HoldProp("phone").Pose(PersonPose.Type, 0.05f)
                .WaitUntil(() => !open).ReleaseProp().Play();
            Assert.IsTrue(played);

            yield return WaitWhile(() => Held == null, 2f);
            Assert.IsNotNull(Held, "the phone is in the hand");

            var hand = FindRightHand();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(Interaction.Playing, "the scene waits for the window");
            Assert.IsNotNull(Held);
            Assert.Less(Vector2.Distance(Held.transform.position, hand.position), 0.3f, "it follows the hand");

            open = false;
            yield return WaitWhile(() => Interaction.Playing, 2f);
            yield return null;
            Assert.IsFalse(Interaction.Playing);
            Assert.IsNull(Held, "it went back to the pocket");
        }

        [UnityTest]
        public IEnumerator Phone_DoesNotStayBehindWhenTheSceneEnds()
        {
            yield return null;
            Assert.IsTrue(new Interaction().HoldProp("phone").Wait(0.05f).Play());
            yield return WaitWhile(() => Interaction.Playing, 2f);
            yield return null;
            Assert.IsNull(Held);
        }

        private static Transform FindRightHand()
        {
            foreach (var part in GameObject.FindWithTag(Station.PlayerTag).GetComponentsInChildren<Transform>())
            {
                if (part.name == "Hand" && part.parent.name == "ArmRElbow")
                {
                    return part;
                }
            }

            Assert.Fail("the player has no right hand");
            return null;
        }
    }
}

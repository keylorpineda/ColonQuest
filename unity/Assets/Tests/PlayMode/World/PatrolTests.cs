using System.Collections;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    public sealed class PatrolTests
    {
        [UnityTearDown]
        public void TearDown()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Loop.Clock.Restore(1, GameRules.DayStartHour * 60f);
            }
            DayTint.IsNight = false;
        }

        [UnityTest]
        public IEnumerator Patrol_WalksHomeAtNight_AndHidesWhenArrived()
        {
            var go = new GameObject("TestPatrol");
            go.transform.position = new Vector3(0f, 0f, 0f);
            var bob = new GameObject("Bob");
            bob.transform.SetParent(go.transform, false);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(bob.transform, false);

            var patrol = go.AddComponent<Patrol>();
            Vector2 home = new Vector2(0.5f, 0f);
            patrol.Configure(new Vector2(0f, 0f), new Vector2(5f, 0f), 4.0f, 0.5f, home);

            yield return null;
            Assert.IsFalse(patrol.IsHome);
            Assert.IsTrue(patrol.HasHome);
            Assert.AreEqual(home, patrol.HomePoint);

            // Turn night on: NPC should walk to home and retire
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Loop.Clock.Advance(GameRules.SecondsPerGameHour * 12f, GameRules.SecondsPerGameHour);
            }
            DayTint.IsNight = true;

            float timeout = 2.0f;
            while (!patrol.IsHome && timeout > 0f)
            {
                DayTint.IsNight = true;
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(patrol.IsHome, "NPC should arrive home. Current pos: " + go.transform.position);
            Assert.IsFalse(visual.activeSelf);

            // Turn day on: NPC should emerge at home and resume route
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Loop.Clock.Restore(1, GameRules.DayStartHour * 60f);
            }
            DayTint.IsNight = false;
            yield return null;

            Assert.IsFalse(patrol.IsHome);
            Assert.IsTrue(visual.activeSelf);

            Object.Destroy(go);
        }
    }
}

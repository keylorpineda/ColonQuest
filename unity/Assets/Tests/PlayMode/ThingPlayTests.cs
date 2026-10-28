using System.Collections;
using System.Linq;
using ColonQuest.Api;
using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Everyday objects in the town: walking up shows what you can do, and they are never tasks.</summary>
    public sealed class ThingPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        [UnityTest]
        public IEnumerator Town_HasThingsWithSomethingToSay()
        {
            yield return null;

            var things = Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Where(s => s.Type == StationType.Thing).ToList();

            Assert.Greater(things.Count, 5, "benches, the fountain, cars and passersby");
            foreach (var thing in things)
            {
                Assert.IsNotEmpty(thing.DisplayName, thing.name);
                Assert.IsNotEmpty(thing.Lines, thing.name);
                Assert.IsTrue(thing.GetComponent<BoxCollider2D>().isTrigger, thing.name);
            }
        }

        [UnityTest]
        public IEnumerator Bench_ShowsItsPromptAndIsNotATask()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            GameSession.Instance.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;

            var bench = Object.FindObjectsByType<Station>(FindObjectsSortMode.None).First(s => s.name == "Thing_Bench");
            Teleport(bench.transform.position + new Vector3(0f, -0.6f, 0f));
            yield return new WaitForSeconds(0.3f);

            var prompt = Ui.Find("Prompt").gameObject;
            Assert.IsTrue(prompt.activeSelf, "the prompt appears next to the bench");
            StringAssert.Contains("Sentarte en la banca", prompt.GetComponentInChildren<Text>().text);
            Assert.AreEqual(0, GameSession.Instance.Loop.TasksDone);
        }
    }
}

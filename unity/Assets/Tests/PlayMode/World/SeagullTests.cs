using System.Collections;
using ColonQuest.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    public sealed class SeagullTests
    {
        [UnityTest]
        public IEnumerator Seagull_IdleBobsAndPecks()
        {
            var go = new GameObject("TestSeagull");
            go.transform.position = new Vector3(500f, 500f, 0f);
            var seagull = go.AddComponent<Seagull>();
            Vector3 initialPos = go.transform.position;

            yield return null;
            Assert.AreEqual(Seagull.SeagullState.Idle, seagull.State);
            Assert.AreEqual(initialPos, seagull.HomePosition);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Seagull_FliesAwayWhenPlayerApproaches_AndReturnsLater()
        {
            var seagullObj = new GameObject("TestSeagull");
            seagullObj.transform.position = new Vector3(600f, 600f, 0f);
            var seagull = seagullObj.AddComponent<Seagull>();
            seagull.ConfigureTimers(0.15f, 0.15f, 0.15f);

            var playerObj = new GameObject("Player");
            playerObj.tag = Station.PlayerTag;
            playerObj.transform.position = new Vector3(650f, 600f, 0f);
            seagull.PlayerTarget = playerObj.transform;

            yield return null;
            Assert.AreEqual(Seagull.SeagullState.Idle, seagull.State);

            // Move player close to seagull (within 2.5 units)
            playerObj.transform.position = seagullObj.transform.position + new Vector3(1.5f, 0f, 0f);
            yield return null;
            yield return null;

            Assert.AreEqual(Seagull.SeagullState.Flying, seagull.State);

            // Let it fly and fade out
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(Seagull.SeagullState.Away, seagull.State);

            // Move player far away
            playerObj.transform.position = new Vector3(800f, 800f, 0f);

            // Wait for away timer to finish and return
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(Seagull.SeagullState.Idle, seagull.State);
            Assert.AreEqual(seagull.HomePosition, seagullObj.transform.position);

            Object.Destroy(seagullObj);
            Object.Destroy(playerObj);
        }
    }
}

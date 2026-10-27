using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the real Town scene: edges, buildings, doors, stations, sorting and camera.</summary>
    public sealed class TownPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        [UnityTest]
        public IEnumerator Player_StartsInTheMiddleOfTheStreet()
        {
            yield return null;

            Assert.AreEqual(0f, Body.position.x, 0.5f);
            Assert.AreEqual(2f, Body.position.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheHedgeAtTheTownEdge()
        {
            yield return Walk(new Vector2(-23f, 2f), Vector2.left, 1.5f);

            Assert.Greater(Body.position.x, -25.3f);
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByABuildingFacade()
        {
            // x = -17.5 is on the Casa facade, away from its door at -19.5.
            yield return Walk(new Vector2(-17.5f, 4.6f), Vector2.up, 1.5f);

            Assert.Less(Body.position.y, 6f);
        }

        /// <summary>The tree standing in the open gap between Casa and Banco, with free ground around it.</summary>
        private static Transform GapTree()
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var group in Object.FindObjectsByType<SortingGroup>(FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(group.transform.position, new Vector2(-14.5f, 8.6f));
                if (group.name == "Tree" && distance < bestDistance)
                {
                    best = group.transform;
                    bestDistance = distance;
                }
            }

            return best;
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByATree()
        {
            var tree = GapTree();
            Vector2 below = (Vector2)tree.position + new Vector2(0f, -2f);

            yield return Walk(below, Vector2.up, 1.2f);

            Assert.Less(Body.position.y, tree.position.y + 0.1f);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksIn()
        {
            var station = GameObject.Find("Station_Garage").GetComponent<Station>();
            Station entered = null;
            station.PlayerEntered += s => entered = s;

            Teleport(station.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, entered);
            Assert.AreEqual(StationType.Garage, station.Type);
        }

        [UnityTest]
        public IEnumerator Town_HasAllEightInteractionPlaces()
        {
            yield return null;

            Assert.AreEqual(5, Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Length, "five street stations");
            Assert.AreEqual(3, Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length, "three enterable buildings");
        }

        [UnityTest]
        public IEnumerator Player_DrawsInFrontOfATreeWhenSouthOfIt()
        {
            var tree = GapTree();
            var treeGroup = tree.GetComponent<SortingGroup>();
            var playerGroup = Body.GetComponent<SortingGroup>();

            Teleport((Vector2)tree.position + new Vector2(0f, -1.5f));
            yield return null;
            yield return null;
            Assert.Greater(playerGroup.sortingOrder, treeGroup.sortingOrder, "south of the tree: in front");

            Teleport((Vector2)tree.position + new Vector2(0f, 1.5f));
            yield return null;
            yield return null;
            Assert.Less(playerGroup.sortingOrder, treeGroup.sortingOrder, "north of the tree: behind");
        }

        [UnityTest]
        public IEnumerator Door_TakesThePlayerInsideAndPlacesItAtTheDoormat()
        {
            // Walk up into the Casa door.
            yield return Walk(new Vector2(-19.5f, 4.4f), Vector2.up, 2.0f);
            yield return WaitForScene(SceneNames.Casa, 3f);
            FindPlayer();

            Assert.AreEqual(SceneNames.Casa, SceneManager.GetActiveScene().name);
            Assert.AreEqual(-7f, Body.position.x, 0.6f);
            Assert.AreEqual(2.3f, Body.position.y, 0.6f);
        }

        [UnityTest]
        public IEnumerator Camera_FollowsThePlayer()
        {
            // Open ground on the avenue, so the player is not pushed by a collider.
            Teleport(new Vector2(0f, -6f));
            yield return new WaitForSeconds(1.5f);

            float halfHeight = Cam.orthographicSize;
            var halfView = new Vector2(halfHeight * Cam.aspect, halfHeight);
            Vector2 expected = CameraMath.ClampCenter(Body.position, halfView, MapLayout.TownBounds);

            Assert.AreEqual(expected.x, Cam.transform.position.x, 0.1f);
            Assert.AreEqual(expected.y, Cam.transform.position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Camera_NeverShowsPastTheTownEdge()
        {
            Teleport(new Vector2(24f, 2f));
            yield return new WaitForSeconds(1.5f);

            float halfWidth = Cam.orthographicSize * Cam.aspect;
            Assert.LessOrEqual(Cam.transform.position.x + halfWidth, MapLayout.TownBounds.xMax + 0.01f);
        }

        [UnityTest]
        public IEnumerator CloudShadows_SlideAgainstTheCamera()
        {
            Teleport(new Vector2(-12f, -6f));
            yield return new WaitForSeconds(1.5f);

            var clouds = GameObject.Find("CloudShadows").transform;
            Assert.AreEqual(Cam.transform.position.x * -0.1f, clouds.position.x, 0.05f);
        }
    }
}

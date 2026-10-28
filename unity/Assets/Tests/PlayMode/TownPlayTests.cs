using System.Collections;
using System.Linq;
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
        public IEnumerator Player_IsStoppedByTheInvisibleWallInTheWest()
        {
            yield return Walk(new Vector2(-42f, 2f), Vector2.left, 2f);

            Assert.Greater(Body.position.x, MapLayout.PlayWestX - 0.1f);
        }

        [UnityTest]
        public IEnumerator Neighborhood_HasHomesTheyCanWalkTo()
        {
            yield return null;

            int homes = 0;
            foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (root.name.StartsWith("Casa_Vecina_"))
                {
                    homes++;
                    Assert.Greater(root.position.x, MapLayout.PlayWestX, root.name + " stands inside the walls");
                }
            }

            Assert.GreaterOrEqual(homes, 4, "the neighborhood has homes to rent or buy later");
            Assert.AreEqual(2, GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "YardSign"));
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheInvisibleWallInTheEast()
        {
            yield return Walk(new Vector2(38f, 2f), Vector2.right, 2f);

            Assert.Less(Body.position.x, MapLayout.PlayEastX + 0.1f);
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheInvisibleWallInTheNorth()
        {
            // Up the trail into the forest, which ends at the invisible wall.
            yield return Walk(new Vector2(0.2f, 15f), Vector2.up, 3f);

            Assert.Less(Body.position.y, MapLayout.PlayNorthY + 0.1f);
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheSeaAtTheShore()
        {
            yield return Walk(new Vector2(-12f, -22f), Vector2.down, 2f);

            Assert.Greater(Body.position.y, MapLayout.ShoreY - 0.1f);
        }

        [UnityTest]
        public IEnumerator Player_CanWalkOutOnThePier()
        {
            float pierX = (MapLayout.PierXMin + MapLayout.PierXMax) * 0.5f;
            yield return Walk(new Vector2(pierX, -25f), Vector2.down, 1.2f);

            Assert.Less(Body.position.y, MapLayout.ShoreY - 1f, "the pier leads over the water");
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheRiverAwayFromTheBridges()
        {
            yield return Walk(new Vector2(25.5f, 10f), Vector2.right, 2f);

            Assert.Less(Body.position.x, MapLayout.RiverXMin);
        }

        [UnityTest]
        public IEnumerator Player_CrossesTheRiverOnTheBridge()
        {
            yield return Walk(new Vector2(25f, 2f), Vector2.right, 3f);

            Assert.Greater(Body.position.x, MapLayout.RiverXMax + 1f);
        }

        [UnityTest]
        public IEnumerator Player_LegsSwingWhileWalking()
        {
            var animator = Body.GetComponentInChildren<PlayerAnimator>();
            Assert.IsTrue(animator.HasRig, "the player is a person with moving parts");
            var leg = animator.transform.Find("Visual/LegL");

            // Open ground on the avenue, so nothing stops the walk.
            Teleport(new Vector2(0f, -6f));
            yield return new WaitForFixedUpdate();
            Body.linearVelocity = MovementMath.Velocity(Vector2.right, 4.5f);

            float widest = 0f;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                widest = Mathf.Max(widest, Quaternion.Angle(leg.localRotation, Quaternion.identity));
                yield return null;
            }

            Body.linearVelocity = Vector2.zero;
            Assert.Greater(widest, 10f);
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
        public IEnumerator Town_HasAllTenInteractionPlaces()
        {
            yield return null;

            Assert.AreEqual(7, Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Length, "seven street stations");
            Assert.AreEqual(3, Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length, "three enterable buildings");
            Assert.AreEqual(StationType.Coop, GameObject.Find("Station_Coop").GetComponent<Station>().Type);
            Assert.AreEqual(StationType.Soda, GameObject.Find("Station_Soda").GetComponent<Station>().Type);
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
            Assert.AreEqual(0f, Body.position.x, 0.6f);
            Assert.AreEqual(MapLayout.CasaMat.y + 1.3f, Body.position.y, 0.6f);
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
            Teleport(new Vector2(43f, -21f));
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

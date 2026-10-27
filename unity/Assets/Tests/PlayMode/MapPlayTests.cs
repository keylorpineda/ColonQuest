using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the real Main scene: walls and furniture must block the player and stations must fire.</summary>
    public sealed class MapPlayTests
    {
        private Rigidbody2D body;

        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            yield return SceneManager.LoadSceneAsync("Main");

            var player = GameObject.FindWithTag(Station.PlayerTag);
            body = player.GetComponent<Rigidbody2D>();

            // The tests drive the body directly instead of the keyboard.
            player.GetComponent<PlayerController>().enabled = false;
        }

        private IEnumerator Walk(Vector2 start, Vector2 direction, float seconds)
        {
            body.position = start;
            body.transform.position = start;
            yield return new WaitForFixedUpdate();

            body.linearVelocity = MovementMath.Velocity(direction, 4.5f);
            yield return new WaitForSeconds(seconds);
            body.linearVelocity = Vector2.zero;
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheOuterWall()
        {
            yield return Walk(Vector2.zero, Vector2.left, 3f);

            Assert.Greater(body.position.x, -11f);
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByAWallBesideTheDoorway()
        {
            yield return Walk(new Vector2(-9.5f, 0f), Vector2.up, 1.2f);

            Assert.Less(body.position.y, 1.2f);
        }

        [UnityTest]
        public IEnumerator Player_WalksThroughTheDoorway()
        {
            yield return Walk(new Vector2(-7f, 0f), Vector2.up, 1.2f);

            Assert.Greater(body.position.y, 1.5f);
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByFurniture()
        {
            yield return Walk(new Vector2(-9.3f, 3f), Vector2.up, 1.5f);

            Assert.Less(body.position.y, 4f);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksIn()
        {
            var station = GameObject.Find("Station_Bank").GetComponent<Station>();
            Station entered = null;
            station.PlayerEntered += s => entered = s;

            body.position = station.transform.position;
            body.transform.position = station.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, entered);
            Assert.IsTrue(station.IsPlayerInside);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksOut()
        {
            var station = GameObject.Find("Station_Home").GetComponent<Station>();
            Station exited = null;
            station.PlayerExited += s => exited = s;

            body.position = station.transform.position;
            body.transform.position = station.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            body.position = Vector2.zero;
            body.transform.position = Vector2.zero;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, exited);
            Assert.IsFalse(station.IsPlayerInside);
        }
    }
}

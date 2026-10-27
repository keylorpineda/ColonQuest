using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the real Main scene: the camera follows the player and layers get their parallax.</summary>
    public sealed class CameraPlayTests
    {
        // Same bounds MapBuilder passes to CameraFollow.
        private static readonly Rect MapBounds = new Rect(-11.5f, -6.7f, 23f, 13.4f);

        private Rigidbody2D body;
        private Camera cam;

        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            yield return SceneManager.LoadSceneAsync("Main");

            body = GameObject.FindWithTag(Station.PlayerTag).GetComponent<Rigidbody2D>();
            cam = Camera.main;
        }

        private IEnumerator TeleportAndSettle(Vector2 position)
        {
            body.position = position;
            body.transform.position = position;
            yield return new WaitForSeconds(1.5f);
        }

        private Vector2 HalfView()
        {
            float halfHeight = cam.orthographicSize;
            return new Vector2(halfHeight * cam.aspect, halfHeight);
        }

        [UnityTest]
        public IEnumerator Camera_FollowsThePlayer()
        {
            var target = new Vector2(-5f, -3f);
            yield return TeleportAndSettle(target);

            Vector2 expected = CameraMath.ClampCenter(target, HalfView(), MapBounds);
            Assert.AreEqual(expected.x, cam.transform.position.x, 0.1f);
            Assert.AreEqual(expected.y, cam.transform.position.y, 0.1f);
        }

        [UnityTest]
        public IEnumerator Camera_NeverShowsOutsideTheMap_AtTheEdges()
        {
            yield return TeleportAndSettle(new Vector2(10.5f, 0f));

            Vector2 half = HalfView();
            Vector3 position = cam.transform.position;
            if (half.x * 2f < MapBounds.width)
            {
                Assert.LessOrEqual(position.x + half.x, MapBounds.xMax + 0.01f);
            }

            if (half.y * 2f < MapBounds.height)
            {
                Assert.GreaterOrEqual(position.y - half.y, MapBounds.yMin - 0.01f);
            }
        }

        [UnityTest]
        public IEnumerator FarStars_MoveLessThanTheCamera()
        {
            yield return TeleportAndSettle(new Vector2(-8f, -4f));

            var farStars = GameObject.Find("Stars_Far").transform;
            var camPosition = cam.transform.position;

            // Factor 0.88: the layer follows the camera by 88 percent of its travel.
            Assert.AreEqual(camPosition.x * 0.88f, farStars.position.x, 0.05f);
            Assert.AreEqual(camPosition.y * 0.88f, farStars.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator Dust_MovesMoreThanTheWorld()
        {
            yield return TeleportAndSettle(new Vector2(-8f, -4f));

            var dust = GameObject.Find("Dust").transform;
            var camPosition = cam.transform.position;

            // Factor -0.25: the foreground slides the opposite way, faster than the world.
            Assert.AreEqual(camPosition.x * -0.25f, dust.position.x, 0.05f);
        }
    }
}

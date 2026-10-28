using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Loads one scene, finds the player and offers helpers to teleport and walk it.</summary>
    public abstract class PlayTestBase
    {
        protected Rigidbody2D Body;
        protected Camera Cam;

        protected abstract string SceneToLoad { get; }

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneToLoad);
            yield return null;
            FindPlayer();

            // The tests drive the body directly instead of the keyboard.
            Body.GetComponent<PlayerController>().enabled = false;
        }

        protected void FindPlayer()
        {
            Body = GameObject.FindWithTag(Station.PlayerTag).GetComponent<Rigidbody2D>();
            Cam = Camera.main;
        }

        protected void Teleport(Vector2 position)
        {
            Body.position = position;
            Body.transform.position = position;
        }

        protected IEnumerator Walk(Vector2 start, Vector2 direction, float seconds)
        {
            Teleport(start);
            yield return new WaitForFixedUpdate();

            Body.linearVelocity = MovementMath.Velocity(direction, 4.5f);
            yield return new WaitForSeconds(seconds);

            // A door may have loaded another scene meanwhile, which destroys this body.
            if (Body != null)
            {
                Body.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>Waits until the named scene is the active one, or the time runs out.</summary>
        protected static IEnumerator WaitForScene(string sceneName, float timeout)
        {
            for (float waited = 0f; waited < timeout; waited += Time.unscaledDeltaTime)
            {
                if (SceneManager.GetActiveScene().name == sceneName)
                {
                    // One more frame so Awake and Start of the new scene have run.
                    yield return null;
                    yield return null;
                    yield break;
                }

                yield return null;
            }
        }
    }
}

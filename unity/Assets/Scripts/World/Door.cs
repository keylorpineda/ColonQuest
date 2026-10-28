using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A doorway or doormat. When the player steps on it, the next scene is loaded
    /// and the player appears on the given spawn point. A door with an entry direction only opens
    /// when the player walks that way, so passing along the sidewalk does not go inside.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawn;
        [SerializeField] private Vector2 entryDirection;

        /// <summary>How close to the entry direction the walk must be (1 is straight on, 0 is sideways).</summary>
        private const float MinimumAlignment = 0.5f;

        public string TargetScene => targetScene;
        public string TargetSpawn => targetSpawn;

        public void Configure(string scene, string spawn)
        {
            targetScene = scene;
            targetSpawn = spawn;
        }

        public void Configure(string scene, string spawn, Vector2 direction)
        {
            Configure(scene, spawn);
            entryDirection = direction.normalized;
        }

        /// <summary>True when walking with this velocity goes through the door. A door with no direction takes any step.</summary>
        public static bool AllowsEntry(Vector2 entryDirection, Vector2 velocity)
        {
            if (entryDirection == Vector2.zero)
            {
                return true;
            }

            return velocity.sqrMagnitude > 0.01f && Vector2.Dot(velocity.normalized, entryDirection.normalized) >= MinimumAlignment;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag(Station.PlayerTag))
            {
                return;
            }

            var body = other.attachedRigidbody;
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            if (AllowsEntry(entryDirection, velocity))
            {
                SceneTransition.Go(targetScene, targetSpawn);
            }
        }
    }
}

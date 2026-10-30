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
        [SerializeField] private string returnSpawn;
        [SerializeField] private bool useRememberedReturn;

        /// <summary>Where the last house door of the town left the player, so the shared house interior leads back to the right door.</summary>
        public static string RememberedReturn { get; private set; } = string.Empty;

        /// <summary>True while the player serves a sentence: no door of the cell opens.</summary>
        public static bool Locked { get; set; }

        public static string LockedThought { get; set; } = string.Empty;

        private static float nextLockedThought;

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

        /// <summary>A door of the town that goes inside a house: it remembers the spot in front of it for the way out.</summary>
        public void ConfigureHouse(string scene, string spawn, Vector2 direction, string outsideSpawn)
        {
            Configure(scene, spawn, direction);
            returnSpawn = outsideSpawn;
        }

        /// <summary>The mat of the shared house: it leads to the door the player came in by.</summary>
        public void RememberReturn()
        {
            useRememberedReturn = true;
        }

        /// <summary>The spawn a door leads to. A mat that remembers uses the last house door; without one it uses its own.</summary>
        public static string ResolveSpawn(bool remember, string remembered, string own)
        {
            return remember && !string.IsNullOrEmpty(remembered) ? remembered : own;
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

            if (Locked)
            {
                if (Time.time >= nextLockedThought && !string.IsNullOrEmpty(LockedThought))
                {
                    nextLockedThought = Time.time + 6f;
                    Thoughts.Say(LockedThought);
                }

                return;
            }

            var body = other.attachedRigidbody;
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            if (AllowsEntry(entryDirection, velocity))
            {
                if (!string.IsNullOrEmpty(returnSpawn))
                {
                    RememberedReturn = returnSpawn;
                }

                SceneTransition.Go(targetScene, ResolveSpawn(useRememberedReturn, RememberedReturn, targetSpawn));
            }
        }
    }
}

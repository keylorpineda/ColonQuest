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
        [SerializeField] private bool closesAtNight;
        [SerializeField] private GameObject closedSign;
        [SerializeField] private string listingId;

        /// <summary>Where the last house door of the town left the player, so the shared house interior leads back to the right door.</summary>
        public static string RememberedReturn { get; set; } = string.Empty;

        public static void SetRememberedReturn(string spawn)
        {
            RememberedReturn = spawn ?? string.Empty;
        }

        public bool ClosesAtNight => closesAtNight;

        public void SetClosesAtNight(bool value)
        {
            closesAtNight = value;
        }

        public void AttachClosedSign(GameObject sign)
        {
            closedSign = sign;
            if (closedSign != null)
            {
                closedSign.SetActive(false);
            }
        }

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

        /// <summary>The door of a house for sale or rent: inside, the house is not the player's until it is rented or bought.</summary>
        public void MarkListing(string id)
        {
            listingId = id;
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

        private void Update()
        {
            if (closedSign != null)
            {
                bool night = closesAtNight && DayTint.IsNight;
                if (closedSign.activeSelf != night)
                {
                    closedSign.SetActive(night);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag(Station.PlayerTag))
            {
                return;
            }

            if (closesAtNight && DayTint.IsNight)
            {
                if (Time.time >= nextLockedThought)
                {
                    nextLockedThought = Time.time + 3f;
                    Thoughts.Say(Loc.T("Está cerrado. Abrimos a las 06:00."));
                }

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

                // Every door says which house it opens: a listing, or none (the mother's house and every other place).
                HouseVisit.Enter(listingId);

                string spawn = ResolveSpawn(useRememberedReturn, RememberedReturn, targetSpawn);
                var elevator = GetComponent<Elevator>();
                if (elevator != null)
                {
                    // The elevator plays its ride first and changes the floor at the end of it.
                    elevator.Ride(targetScene, spawn);
                    return;
                }

                SceneTransition.Go(targetScene, spawn, velocity);
            }
        }
    }
}

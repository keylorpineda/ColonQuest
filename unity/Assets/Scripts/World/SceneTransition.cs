using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Moves the player to another scene with a fade, and remembers which spawn point
    /// the next scene should put the player on. Through a door the player keeps walking while the screen
    /// goes dark and comes out walking the same way on the other side, so it reads as a walk, not a jump.
    /// </summary>
    public static class SceneTransition
    {
        /// <summary>How long the player keeps walking into the door while the screen goes dark.</summary>
        public const float WalkInSeconds = 0.45f;

        /// <summary>How long the player walks out of the door on the other side, while the screen clears.</summary>
        public const float WalkOutSeconds = 0.25f;

        private static string pendingSpawnId;

        /// <summary>True from the moment a door is used until the next scene has faded in.</summary>
        public static bool IsBusy { get; private set; }

        /// <summary>The way the player walked through the last door, to walk out the same way. Zero when nobody walked.</summary>
        public static Vector2 ArrivalDirection { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            pendingSpawnId = null;
            IsBusy = false;
            ArrivalDirection = Vector2.zero;
        }

        public static void Go(string sceneName, string spawnId)
        {
            Go(sceneName, spawnId, Vector2.zero);
        }

        /// <summary>Goes through a door walking this way: the player keeps the step into the dark and out on the other side.</summary>
        public static void Go(string sceneName, string spawnId, Vector2 walking)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            pendingSpawnId = spawnId;
            ArrivalDirection = WalkDirection(walking);

            var player = Object.FindFirstObjectByType<PlayerController>();
            var fader = Object.FindFirstObjectByType<ScreenFader>();
            if (fader == null)
            {
                if (player != null)
                {
                    player.InputEnabled = false;
                }

                SceneManager.LoadScene(sceneName);
                return;
            }

            if (player != null && ArrivalDirection != Vector2.zero)
            {
                AudioDirector.Effect(SoundEffectKind.DoorOpen);
                player.Nudge(ArrivalDirection, WalkInSeconds);
            }
            else if (player != null)
            {
                player.InputEnabled = false;
            }

            fader.StartCoroutine(FadeThenLoad(fader, sceneName, player));
        }

        /// <summary>The straight way of a walk (up, down, left or right), or zero when the player was standing still.</summary>
        public static Vector2 WalkDirection(Vector2 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f)
            {
                return Vector2.zero;
            }

            return Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)
                ? new Vector2(Mathf.Sign(velocity.x), 0f)
                : new Vector2(0f, Mathf.Sign(velocity.y));
        }

        /// <summary>The player of the new scene takes the last steps out of the door, if they came in walking.</summary>
        public static void WalkOut(PlayerController player)
        {
            if (player != null && ArrivalDirection != Vector2.zero)
            {
                player.Nudge(ArrivalDirection, WalkOutSeconds);
            }

            ArrivalDirection = Vector2.zero;
        }

        /// <summary>Returns the spawn id a door asked for, once.</summary>
        public static string ConsumeSpawnId()
        {
            string id = pendingSpawnId;
            pendingSpawnId = null;
            return id;
        }

        internal static void Finished()
        {
            IsBusy = false;
        }

        private static IEnumerator FadeThenLoad(ScreenFader fader, string sceneName, PlayerController player)
        {
            yield return fader.FadeTo(1f);
            if (player != null)
            {
                player.InputEnabled = false;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}

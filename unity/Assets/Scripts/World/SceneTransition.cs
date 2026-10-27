using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Moves the player to another scene with a fade, and remembers which spawn point
    /// the next scene should put the player on.
    /// </summary>
    public static class SceneTransition
    {
        private static string pendingSpawnId;

        /// <summary>True from the moment a door is used until the next scene has faded in.</summary>
        public static bool IsBusy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            pendingSpawnId = null;
            IsBusy = false;
        }

        public static void Go(string sceneName, string spawnId)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            pendingSpawnId = spawnId;

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.InputEnabled = false;
            }

            var fader = Object.FindFirstObjectByType<ScreenFader>();
            if (fader == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            fader.StartCoroutine(FadeThenLoad(fader, sceneName));
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

        private static IEnumerator FadeThenLoad(ScreenFader fader, string sceneName)
        {
            yield return fader.FadeTo(1f);
            SceneManager.LoadScene(sceneName);
        }
    }
}

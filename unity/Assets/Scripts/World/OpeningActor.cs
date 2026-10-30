using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A person who only appears in the opening scene, such as the player's mother. Any other time they are not there.</summary>
    public sealed class OpeningActor : MonoBehaviour
    {
        /// <summary>Set while the opening is about to play or playing.</summary>
        public static bool Show { get; set; }

        private void Awake()
        {
            if (!Show)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

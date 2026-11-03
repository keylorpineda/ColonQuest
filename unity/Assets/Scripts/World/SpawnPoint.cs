using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A named place where the player can appear after walking through a door.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;

        public void Configure(string spawnId)
        {
            id = spawnId;
        }
    }
}

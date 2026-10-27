using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A doorway or doormat. When the player steps on it, the next scene is loaded
    /// and the player appears on the given spawn point.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Door : MonoBehaviour
    {
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawn;

        public string TargetScene => targetScene;
        public string TargetSpawn => targetSpawn;

        public void Configure(string scene, string spawn)
        {
            targetScene = scene;
            targetSpawn = spawn;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(Station.PlayerTag))
            {
                SceneTransition.Go(targetScene, targetSpawn);
            }
        }
    }
}

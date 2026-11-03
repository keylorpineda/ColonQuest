using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Puts the player on the spawn point a door asked for. Runs in Awake, before the
    /// camera's Start, so the camera begins on the player instead of sliding in.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerSpawner : MonoBehaviour
    {
        private void Awake()
        {
            string id = SceneTransition.ConsumeSpawnId();
            if (id == null)
            {
                return;
            }

            foreach (var spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if (spawn.Id != id)
                {
                    continue;
                }

                Vector2 position = spawn.transform.position;
                transform.position = position;
                GetComponent<Rigidbody2D>().position = position;
                SceneTransition.WalkOut(GetComponent<PlayerController>());
                return;
            }

            Debug.LogWarning($"No spawn point with id '{id}' in this scene; the player stays at its start.");
        }
    }
}

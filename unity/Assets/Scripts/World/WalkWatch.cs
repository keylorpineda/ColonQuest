using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Follows the player's feet: a step sound that fits the ground every stride, and a thought with a few steps
    /// back when the player tries to leave the map or enter the water. It is added by <see cref="PlayerController"/>.
    /// </summary>
    public sealed class WalkWatch : MonoBehaviour
    {
        private const float Stride = 0.85f;
        private const float HintCooldown = 7f;

        public const string EdgeThought = "No debería alejarme tanto del pueblo.";
        public const string WaterThought = "Mejor me compro un bañador antes de meterme al agua.";

        private Vector2 last;
        private float travelled;
        private float nextHint;
        private int variant;

        private void Start()
        {
            last = transform.position;
        }

        private void Update()
        {
            Vector2 now = transform.position;
            float moved = Vector2.Distance(now, last);
            last = now;
            if (moved > 1.5f || SceneTransition.IsBusy)
            {
                // A jump (the door of a building) is not a step.
                travelled = 0f;
                return;
            }

            string scene = SceneManager.GetActiveScene().name;
            if (!PlayerController.Blocked)
            {
                travelled += moved;
                if (travelled >= Stride)
                {
                    travelled = 0f;
                    AudioDirector.Step(Surfaces.At(scene, now), variant++);
                }
            }

            var hint = WorldHints.Check(scene, now);
            if (hint.Kind != HintKind.None && Time.time >= nextHint)
            {
                nextHint = Time.time + HintCooldown;
                Thoughts.Say(hint.Kind == HintKind.Water ? WaterThought : EdgeThought);
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Nudge(hint.Away, 0.7f);
                }
            }
        }
    }
}

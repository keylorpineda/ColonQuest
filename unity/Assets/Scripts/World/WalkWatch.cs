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
        public const string SeaThought = "Ya tengo bañador. El chapuzón me lo doy en la orilla de la playa (E).";
        public const string RiverThought = "Mejor no me meto al río.";
        public const string PondThought = "Mejor no me meto al estanque.";

        /// <summary>Set by the game from the player's things: with a swimsuit the sea no longer asks for one.</summary>
        public static bool HasSwimsuit { get; set; }

        /// <summary>The walking speed of the player, the same as the controller's.</summary>
        public const float WalkSpeed = 4.5f;

        /// <summary>
        /// How long the step back lasts: just enough to leave the warning zone and a little more. A long step pushed the
        /// player across the beach by the pier and back into the next warning.
        /// </summary>
        public static float NudgeSeconds(float depth)
        {
            return Mathf.Clamp((Mathf.Max(0f, depth) + 0.25f) / WalkSpeed, 0.1f, 0.35f);
        }

        /// <summary>What the player thinks at the edge of the water.</summary>
        public static string ThoughtFor(WaterBody water, bool hasSwimsuit)
        {
            switch (water)
            {
                case WaterBody.River:
                    return RiverThought;
                case WaterBody.Pond:
                    return PondThought;
                default:
                    return hasSwimsuit ? SeaThought : WaterThought;
            }
        }

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

            // A scene (the swim) moves the player on purpose; it must not be interrupted by a warning.
            if (Interaction.Playing)
            {
                return;
            }

            var hint = WorldHints.Check(scene, now);
            if (hint.Kind != HintKind.None && Time.time >= nextHint)
            {
                nextHint = Time.time + HintCooldown;
                Thoughts.Say(hint.Kind == HintKind.Water ? ThoughtFor(hint.Water, HasSwimsuit) : EdgeThought);
                var controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Nudge(hint.Away, NudgeSeconds(hint.Depth));
                }
            }
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A full-screen wash of color that follows <see cref="DayCycle"/>, eased so the change is gentle.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DayTint : MonoBehaviour
    {
        /// <summary>Set by the game session; the town reads it when it loads or while it is open.</summary>
        public static float Progress;

        /// <summary>True during night hours (18:00 to 06:00), updated by the active session.</summary>
        public static bool IsNight;

        [SerializeField] private float strength = 1f;

        private SpriteRenderer spriteRenderer;

        /// <summary>
        /// How much of the wash shows, from 0 to 1. A town lit by 2D lights keeps only a light haze of it: the sun light
        /// already colors the day, and the full wash on top would tint it twice.
        /// </summary>
        public float Strength => strength;

        public void SetStrength(float value)
        {
            strength = Mathf.Clamp01(value);
        }

        /// <summary>The wash for a moment of the day at a strength: the same color, with its opacity scaled.</summary>
        public static Color TintAt(float progress, float strength)
        {
            var color = DayCycle.TintFor(progress);
            color.a *= Mathf.Clamp01(strength);
            return color;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = TintAt(Progress, strength);
        }

        private void Update()
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, TintAt(Progress, strength), 1f - Mathf.Exp(-1.5f * Time.deltaTime));
        }
    }
}

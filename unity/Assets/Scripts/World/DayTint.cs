using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A full-screen wash of color that follows <see cref="DayCycle"/>, eased so the change is gentle.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DayTint : MonoBehaviour
    {
        /// <summary>Set by the game session; the town reads it when it loads or while it is open.</summary>
        public static float Progress;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = DayCycle.TintFor(Progress);
        }

        private void Update()
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, DayCycle.TintFor(Progress), 1f - Mathf.Exp(-1.5f * Time.deltaTime));
        }
    }
}

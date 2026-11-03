using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// A window that opens with a small pop: it grows from a little smaller to its size and fades in, in a fifth of a second.
    /// Runs on unscaled time, so it also works in the pause menu.
    /// </summary>
    public sealed class PopIn : MonoBehaviour
    {
        public const float Seconds = 0.2f;
        private const float StartScale = 0.92f;

        private CanvasGroup group;
        private float openedAt;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            openedAt = Time.unscaledTime;
            Apply(0f);
        }

        private void Update()
        {
            Apply(Mathf.Clamp01((Time.unscaledTime - openedAt) / Seconds));
        }

        /// <summary>The scale of the window at a point of the pop, from 0 (just opened) to 1 (done). Eases out, with no overshoot.</summary>
        public static float ScaleAt(float progress)
        {
            float t = Mathf.Clamp01(progress);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            return Mathf.Lerp(StartScale, 1f, ease);
        }

        private void Apply(float progress)
        {
            float scale = ScaleAt(progress);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (group != null)
            {
                group.alpha = Mathf.Clamp01(progress * 1.6f);
            }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A black sprite over the whole camera view. Fades out before a scene change
    /// and fades back in when the next scene starts after a door.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ScreenFader : MonoBehaviour
    {
        /// <summary>Long enough to see the player walk into the dark and out of it, short enough not to wait.</summary>
        public const float Seconds = 0.45f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            SetAlpha(0f);
        }

        private void Start()
        {
            if (SceneTransition.IsBusy)
            {
                SetAlpha(1f);
                StartCoroutine(FadeIn());
            }
        }

        public IEnumerator FadeTo(float target)
        {
            float start = spriteRenderer.color.a;
            for (float elapsed = 0f; elapsed < Seconds; elapsed += Time.unscaledDeltaTime)
            {
                // Eased, so the dark settles in and lifts softly instead of snapping.
                SetAlpha(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / Seconds)));
                yield return null;
            }

            SetAlpha(target);
        }

        private IEnumerator FadeIn()
        {
            yield return FadeTo(0f);
            SceneTransition.Finished();
        }

        private void SetAlpha(float alpha)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}

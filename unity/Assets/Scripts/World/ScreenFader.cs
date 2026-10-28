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
        [SerializeField] private float duration = 0.25f;

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
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, elapsed / duration));
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

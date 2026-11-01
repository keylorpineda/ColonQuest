using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The smoke of the volcano in the main menu: puffs drawn in the style of the key art (pale grey with an ink outline,
    /// see <c>Resources/Menu/Smoke_*</c>) leave the crater one after the other, rise, sway, grow, bend with the wind and
    /// thin away. The key art plate has no smoke of its own, so this column is the volcano's smoke, not a layer on top.
    /// A fixed pool of images is reused; nothing is created after the start.
    /// </summary>
    public sealed class MenuVolcanoSmoke : MonoBehaviour
    {
        private const float SourceWidth = 2048f;
        private const float SourceHeight = 1152f;
        private static readonly Vector2 CraterPx = new Vector2(676f, 158f);
        private const int PoolSize = 12;
        private const float Life = 7.5f;
        private const float PuffSizePx = 256f;

        private Image[] puffs;
        private float[] born;
        private float[] sway;
        private float[] spin;
        private float scale;
        private Vector2 crater;
        private float nextPuff;

        public static MenuVolcanoSmoke Create(RectTransform art, float artWidth)
        {
            var sprites = new[] { Resources.Load<Sprite>("Menu/Smoke_0"), Resources.Load<Sprite>("Menu/Smoke_1"), Resources.Load<Sprite>("Menu/Smoke_2") };
            if (sprites[0] == null)
            {
                return null;
            }

            var go = new GameObject("VolcanoSmoke", typeof(RectTransform));
            go.transform.SetParent(art, false);
            var smoke = go.AddComponent<MenuVolcanoSmoke>();
            smoke.Build(artWidth / SourceWidth, sprites);
            return smoke;
        }

        private void Build(float pixelScale, Sprite[] sprites)
        {
            scale = pixelScale;
            var root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            crater = new Vector2((CraterPx.x - SourceWidth * 0.5f) * scale, (SourceHeight * 0.5f - CraterPx.y) * scale);

            puffs = new Image[PoolSize];
            born = new float[PoolSize];
            sway = new float[PoolSize];
            spin = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var rect = UiKit.Rect(root, "Puff" + i, Vector2.one * PuffSizePx * scale, new Vector2(0.5f, 0.5f), crater);
                var image = rect.gameObject.AddComponent<Image>();
                var sprite = sprites[i % sprites.Length];
                image.sprite = sprite != null ? sprite : sprites[0];
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0f);
                puffs[i] = image;
                sway[i] = Random.Range(0f, 6.28f);
                spin[i] = Random.Range(-25f, 25f);
            }

            // Start with a column already standing, as in the drawing: the puffs are spread over their lives, the oldest
            // in front.
            float now = Time.unscaledTime;
            for (int i = 0; i < PoolSize; i++)
            {
                born[i] = now - Life * (i / (float)PoolSize);
                puffs[i].transform.SetAsLastSibling();
            }

            nextPuff = now + Life / PoolSize;
        }

        private void Update()
        {
            if (puffs == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextPuff)
            {
                // The oldest puff starts again at the crater, behind the others: each older (higher, wider) puff covers
                // the top of the one under it, so the column reads as one billowing shape with only its edges inked.
                nextPuff = now + Life / PoolSize;
                int index = OldestPuff();
                born[index] = now;
                sway[index] = Random.Range(0f, 6.28f);
                spin[index] = Random.Range(-25f, 25f);
                puffs[index].transform.SetAsFirstSibling();
            }

            for (int i = 0; i < PoolSize; i++)
            {
                float t = (now - born[i]) / Life;
                var rect = puffs[i].rectTransform;
                rect.anchoredPosition = crater + MenuMotion.SmokeOffset(t, sway[i]) * scale;
                float size = MenuMotion.SmokeScale(t);
                rect.localScale = new Vector3(size, size, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin[i] * t);
                puffs[i].color = new Color(1f, 1f, 1f, MenuMotion.SmokeAlpha(t));
            }
        }

        private int OldestPuff()
        {
            int oldest = 0;
            for (int i = 1; i < PoolSize; i++)
            {
                if (born[i] < born[oldest])
                {
                    oldest = i;
                }
            }

            return oldest;
        }
    }
}

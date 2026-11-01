using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The smoke of the volcano in the main menu. It is the column of the key art itself (<c>Resources/Menu/Smoke_Column</c>,
    /// cut from the picture and erased from <c>KeyArt_Plate</c>): a wave climbs it from the crater while a few pale wisps in
    /// its own colors rise along its axis, so it moves and still looks drawn with the rest of the picture.
    /// A fixed pool of images is reused; nothing is created after the start.
    /// </summary>
    public sealed class MenuVolcanoSmoke : MonoBehaviour
    {
        private const float SourceWidth = 2048f;
        private const float SourceHeight = 1152f;
        private static readonly Rect ColumnPx = new Rect(652f, 2f, 198f, 169f);
        private static readonly Vector2 BasePx = new Vector2(674f, 171f);
        private const int WispCount = 6;
        private const float WispLife = 5f;
        private const float WispSizePx = 256f;

        private SmokeColumnGraphic column;
        private Image[] wisps;
        private float[] offset;
        private float[] spin;
        private float scale;
        private Vector2 crater;

        public static MenuVolcanoSmoke Create(RectTransform art, float artWidth)
        {
            var columnSprite = Resources.Load<Sprite>("Menu/Smoke_Column");
            var sprites = new[] { Resources.Load<Sprite>("Menu/Smoke_0"), Resources.Load<Sprite>("Menu/Smoke_1"), Resources.Load<Sprite>("Menu/Smoke_2") };
            if (columnSprite == null || sprites[0] == null)
            {
                return null;
            }

            var go = new GameObject("VolcanoSmoke", typeof(RectTransform));
            go.transform.SetParent(art, false);
            var smoke = go.AddComponent<MenuVolcanoSmoke>();
            smoke.Build(artWidth / SourceWidth, columnSprite, sprites);
            return smoke;
        }

        /// <summary>A pixel of the key art (top left origin) in the art rectangle (center origin, y up).</summary>
        private Vector2 ArtPoint(Vector2 px)
        {
            return new Vector2((px.x - SourceWidth * 0.5f) * scale, (SourceHeight * 0.5f - px.y) * scale);
        }

        private void Build(float pixelScale, Sprite columnSprite, Sprite[] sprites)
        {
            scale = pixelScale;
            var root = (RectTransform)transform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            crater = ArtPoint(BasePx);

            var center = new Vector2(ColumnPx.x + ColumnPx.width * 0.5f, ColumnPx.y + ColumnPx.height * 0.5f);
            var columnRect = UiKit.Rect(root, "Column", new Vector2(ColumnPx.width, ColumnPx.height) * scale, new Vector2(0.5f, 0.5f), ArtPoint(center));
            column = columnRect.gameObject.AddComponent<SmokeColumnGraphic>();
            column.Configure(columnSprite.texture, scale, ColumnPx.height);

            wisps = new Image[WispCount];
            offset = new float[WispCount];
            spin = new float[WispCount];
            for (int i = 0; i < WispCount; i++)
            {
                var rect = UiKit.Rect(root, "Wisp" + i, Vector2.one * WispSizePx * scale, new Vector2(0.5f, 0.5f), crater);
                var image = rect.gameObject.AddComponent<Image>();
                var sprite = sprites[i % sprites.Length];
                image.sprite = sprite != null ? sprite : sprites[0];
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0f);
                wisps[i] = image;

                // Spread over the life, so the column is already moving when the menu opens.
                offset[i] = WispLife * (i / (float)WispCount);
                spin[i] = Random.Range(-40f, 40f);
            }

            Apply(Time.unscaledTime);
        }

        private void Update()
        {
            if (column != null)
            {
                Apply(Time.unscaledTime);
            }
        }

        private void Apply(float now)
        {
            column.SetTime(now);
            for (int i = 0; i < WispCount; i++)
            {
                float t = Mathf.Repeat(now + offset[i], WispLife) / WispLife;
                float height = t * ColumnPx.height;
                var rect = wisps[i].rectTransform;
                rect.anchoredPosition = crater + new Vector2(MenuMotion.SmokeAxis(height) + MenuMotion.SmokeSway(height, now), height) * scale;
                float size = MenuMotion.WispScale(t);
                rect.localScale = new Vector3(size, size, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin[i] * t);
                wisps[i].color = new Color(1f, 1f, 1f, MenuMotion.WispAlpha(t));
            }
        }
    }
}

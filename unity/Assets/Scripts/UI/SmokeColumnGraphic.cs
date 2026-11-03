using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The smoke column cut from the key art, drawn as a strip of rows that bend with <see cref="MenuMotion.SmokeSway"/>:
    /// the base stays on the crater and a wave climbs the column, so the ink line of the drawing moves without breaking.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SmokeColumnGraphic : MaskableGraphic
    {
        private const int Rows = 48;

        private Texture texture;
        private float pixelScale;
        private float heightPx;
        private float seconds;

        public override Texture mainTexture => texture != null ? texture : s_WhiteTexture;

        public void Configure(Texture columnTexture, float artPixelScale, float columnHeightPx)
        {
            texture = columnTexture;
            pixelScale = artPixelScale;
            heightPx = columnHeightPx;
            raycastTarget = false;
            SetAllDirty();
        }

        public void SetTime(float now)
        {
            seconds = now;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var box = rectTransform.rect;
            Color32 tint = color;
            for (int row = 0; row <= Rows; row++)
            {
                float v = row / (float)Rows;
                float y = Mathf.Lerp(box.yMin, box.yMax, v);
                float bend = MenuMotion.SmokeSway(v * heightPx, seconds) * pixelScale;
                vh.AddVert(new Vector3(box.xMin + bend, y), tint, new Vector4(0f, v));
                vh.AddVert(new Vector3(box.xMax + bend, y), tint, new Vector4(1f, v));
                if (row > 0)
                {
                    int top = row * 2;
                    vh.AddTriangle(top - 2, top, top + 1);
                    vh.AddTriangle(top + 1, top - 1, top - 2);
                }
            }
        }
    }
}

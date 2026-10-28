using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// What makes the flat town feel lit by a real sun: cast shadows from every building toward the
    /// lower right, a fine grain over the colors, and a wash of light that changes from morning to dusk
    /// as the run advances.
    /// </summary>
    public static partial class MapBuilder
    {
        private static Sprite noise;

        private static void BuildRealism(Transform parent, Transform shadows)
        {
            BuildCastShadows(shadows);
            BuildGrain(parent);
            BuildDayTint(parent);
        }

        /// <summary>The sun is high to the upper left, so each building throws a long soft shadow to its lower right.</summary>
        private static void BuildCastShadows(Transform shadows)
        {
            foreach (var footprint in footprints)
            {
                float baseY = footprint.yMin + 1.5f;
                float width = footprint.width - 2f;
                float centerX = footprint.center.x;

                var core = GamePalette.Outline;
                core.a = 0.2f;
                OnGround(shadows, "Cast", rounded, new Vector2(centerX + 1.4f, baseY - 0.6f), new Vector2(width + 1.2f, 1.5f), core, TownShadow + 2);

                var penumbra = GamePalette.Outline;
                penumbra.a = 0.12f;
                OnGround(shadows, "CastSoft", softCircle, new Vector2(centerX + 2.2f, baseY - 0.9f), new Vector2(width * 1.35f, 3.4f), penumbra, TownShadow + 2);
            }
        }

        /// <summary>A very light speckle over everything, like the texture of paper under flat paint.</summary>
        private static void BuildGrain(Transform parent)
        {
            var go = new GameObject("Grain");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = MapLayout.TownBounds.center;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = noise;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = MapLayout.TownBounds.size + Vector2.one * 24f;
            var color = GamePalette.Hex("#1C2233");
            color.a = 0.075f;
            renderer.color = color;
            renderer.sortingLayerName = LayerForeground;
            renderer.sortingOrder = 2;
        }

        /// <summary>The light of the day: it starts as a warm morning and ends as a blue dusk.</summary>
        private static void BuildDayTint(Transform parent)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var tint = Shape(parent, "DayTint", square, MapLayout.TownBounds.center, MapLayout.TownBounds.size + Vector2.one * 60f, DayCycle.TintFor(0f), 6);
            tint.gameObject.AddComponent<DayTint>();

            currentLayer = previous;
        }
    }
}

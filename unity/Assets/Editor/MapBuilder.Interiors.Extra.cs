using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Small helpers the interiors share. Each kind of detail goes in its own named group (a layer), so it
    /// can be found, hidden or tuned on its own.
    /// </summary>
    public static partial class MapBuilder
    {
        private static int interiorLayers;

        private static Transform IL(Transform parent, string name)
        {
            interiorLayers++;
            return Group(parent, name);
        }

        /// <summary>Furniture: it has an outline, a shadow and a collider, so the player walks around it.</summary>
        private static void Solid(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex)
        {
            Prop(g, name, sprite, new Vector2(x, y), new Vector2(w, h), GamePalette.Hex(hex));
        }

        /// <summary>A detail drawn on top of furniture or on a wall; it never blocks the player.</summary>
        private static SpriteRenderer Deco(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex, float alpha = 1f, int order = 7)
        {
            return Shape(g, name, sprite, new Vector2(x, y), new Vector2(w, h), Hex(hex, alpha), order);
        }

        /// <summary>Something flat on the floor, such as a rug or tape.</summary>
        private static SpriteRenderer Decal(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex, float alpha = 1f)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            var renderer = Shape(g, name, sprite, new Vector2(x, y), new Vector2(w, h), Hex(hex, alpha), OrderRug + 2);
            currentLayer = previous;
            return renderer;
        }

        /// <summary>A rug with a darker border, so it reads as cloth and not as a colored zone.</summary>
        private static void Rug(Transform g, float x, float y, float w, float h, string hex, string borderHex)
        {
            Decal(g, "Rug_Border", rounded, x, y, w, h, borderHex);
            Decal(g, "Rug", rounded, x, y, w - 0.24f, h - 0.24f, hex);
        }

        private static void Plant(Transform g, float x, float y, float scale = 1f)
        {
            Prop(g, "Pot", roundedSmall, new Vector2(x, y), new Vector2(0.6f, 0.5f) * scale, GamePalette.Hex("#C26A3A"));
            Deco(g, "Plant", circle, x, y + 0.35f * scale, 0.9f * scale, 0.9f * scale, "#3FA66A");
            Deco(g, "Leaf", circle, x - 0.15f * scale, y + 0.5f * scale, 0.4f * scale, 0.4f * scale, "#6BBF59");
        }

        private static void DustMotes(Transform g, System.Random random, Room room, int count)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;
            var half = room.Size * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var mote = Shape(g, "Mote", softCircle, new Vector2((float)random.NextDouble() * room.Size.x - half.x, (float)random.NextDouble() * room.Size.y - half.y), Vector2.one * 0.2f, Hex("#FFF6B8", 0.3f), 2);
                mote.gameObject.AddComponent<Drift>().Configure(new Vector2(0.1f + (float)random.NextDouble() * 0.12f, 0.06f), half);
            }

            currentLayer = previous;
        }

        /// <summary>A soft pool of light under a ceiling lamp.</summary>
        private static void CeilingLight(Transform lights, Vector2 at, float size, string hex, float alpha)
        {
            Flickering(Glow(lights, "Ceiling", at, size, hex, alpha, OrderLight), 0.04f, 0.3f, at.x * 3f + at.y);
        }

        private static void FloorChecks(Transform g, Rect area, float tile, string hex, float alpha)
        {
            int row = 0;
            for (float y = area.yMin + tile * 0.5f; y < area.yMax; y += tile, row++)
            {
                int col = 0;
                for (float x = area.xMin + tile * 0.5f; x < area.xMax; x += tile, col++)
                {
                    if ((row + col) % 2 == 0)
                    {
                        Decal(g, "Check", square, x, y, tile, tile, hex, alpha);
                    }
                }
            }
        }

        private static void SecurityCamera(Transform g, Vector2 at)
        {
            Deco(g, "Dome", circle, at.x, at.y, 0.34f, 0.34f, "#4A5166");
            Flickering(Deco(g, "Light", circle, at.x, at.y, 0.08f, 0.08f, "#E8505B", 1f, 8), 0.8f, 1.2f, at.x);
        }
    }
}

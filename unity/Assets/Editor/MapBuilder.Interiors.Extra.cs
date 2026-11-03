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
            Decal(g, "Rug_Outline", rounded, x, y, w + 0.1f, h + 0.1f, "#2A3345", 0.85f);
            Decal(g, "Rug_Border", rounded, x, y, w, h, borderHex);
            Decal(g, "Rug", rounded, x, y, w - 0.24f, h - 0.24f, hex);
            if (w > 1.4f && h > 0.8f)
            {
                Decal(g, "Rug_InnerTrim", rounded, x, y, w - 0.46f, h - 0.42f, borderHex, 0.55f);
                Decal(g, "Rug_InnerCore", rounded, x, y, w - 0.56f, h - 0.52f, hex);
            }
        }

        private static void Plant(Transform g, float x, float y, float scale = 1f)
        {
            Prop(g, "Pot", roundedSmall, new Vector2(x, y), new Vector2(0.6f, 0.5f) * scale, GamePalette.Hex("#C26A3A"));
            Deco(g, "Pot_Rim", roundedSmall, x, y + 0.2f * scale, 0.68f * scale, 0.14f * scale, "#D67B47", 1f, OrderProp + 2);
            Deco(g, "Pot_Soil", circle, x, y + 0.22f * scale, 0.52f * scale, 0.14f * scale, "#4A3222", 1f, OrderProp + 2);

            var rig = new GameObject("PlantRig").transform;
            rig.SetParent(g, false);
            rig.localPosition = new Vector2(x, y + 0.22f * scale);
            rig.gameObject.AddComponent<FoliagePhysics>().Configure(3.2f, 1.5f, x * 1.3f + y * 0.9f, 0.02f, true, 1.45f, 11f);

            Deco(rig, "Plant_Outline", circle, 0f, 0.18f * scale, 1.02f * scale, 0.98f * scale, "#1C2233", 1f, 6);
            Deco(rig, "Plant_Deep", circle, 0.06f * scale, 0.14f * scale, 0.88f * scale, 0.84f * scale, "#2E8553", 1f, 7);
            Deco(rig, "Plant", circle, -0.02f * scale, 0.2f * scale, 0.82f * scale, 0.8f * scale, "#3FA66A", 1f, 7);
            Deco(rig, "Leaf", circle, -0.15f * scale, 0.32f * scale, 0.44f * scale, 0.4f * scale, "#6BBF59", 1f, 8);
            Deco(rig, "Leaf_Right", circle, 0.18f * scale, 0.26f * scale, 0.36f * scale, 0.32f * scale, "#6BBF59", 1f, 8);
            Deco(rig, "Bloom", circle, 0.08f * scale, 0.36f * scale, 0.16f * scale, 0.16f * scale, "#FFD23F", 1f, 9);
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

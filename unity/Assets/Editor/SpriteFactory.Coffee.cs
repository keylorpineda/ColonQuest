using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// A coffee plant drawn leaf by leaf: a dense rounded crown of glossy leaves, and separate boughs with opposite leaves and
    /// cherries on their nodes, which is what makes a coffee shrub look like one. The boughs are their own sprites so each can
    /// move by itself. The seeds are fixed, so a rebuild draws the same plant.
    /// </summary>
    public static partial class SpriteFactory
    {
        public const string CoffeeCrownPath = Folder + "/CoffeeCrown.png";
        public const string CoffeeBoughPath = Folder + "/CoffeeBough.png";
        public const string CoffeeBoughGreenPath = Folder + "/CoffeeBoughGreen.png";

        private const float LeafInk = 2.6f;
        private const float CherryRadius = 7.5f;

        private static readonly Color CoffeeInk = new Color(0.07f, 0.13f, 0.09f, 1f);
        private static readonly Color Twig = new Color(0.36f, 0.25f, 0.16f, 1f);
        private static readonly Color CherryRed = new Color(0.78f, 0.13f, 0.14f, 1f);
        private static readonly Color CherryCrimson = new Color(0.60f, 0.08f, 0.14f, 1f);
        private static readonly Color CherryGreen = new Color(0.56f, 0.72f, 0.26f, 1f);
        private static readonly Color CherryYellow = new Color(0.93f, 0.71f, 0.16f, 1f);

        private struct CoffeeLeaf
        {
            public Vector2 Base;
            public Vector2 Axis;
            public float Length;
            public float HalfWidth;
            public Color Light;
            public Color Dark;
        }

        private struct Cherry
        {
            public Vector2 Center;
            public Color Color;
        }

        /// <summary>Draws the plant if a file is missing. Part of what the map builder asks for before it builds.</summary>
        private static void EnsureCoffee()
        {
            if (!System.IO.File.Exists(CoffeeCrownPath) || !System.IO.File.Exists(CoffeeBoughPath) || !System.IO.File.Exists(CoffeeBoughGreenPath))
            {
                WriteCoffee();
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        private static void WriteCoffee()
        {
            WriteCoffeeSprite(CoffeeCrownPath, 256, 192, CrownLeaves(), new List<Cherry>(), null);
            BoughParts(true, out var ripeLeaves, out var ripeCherries);
            WriteCoffeeSprite(CoffeeBoughPath, 256, 128, ripeLeaves, ripeCherries, BoughTwig);
            BoughParts(false, out var greenLeaves, out var greenCherries);
            WriteCoffeeSprite(CoffeeBoughGreenPath, 256, 128, greenLeaves, greenCherries, BoughTwig);
        }

        private static void WriteCoffeeSprite(string path, int width, int height, List<CoffeeLeaf> leaves, List<Cherry> cherries, Func<float, float> twig)
        {
            WriteColor(path, width, height, 192, 0, (x, y) => CoffeePixel(new Vector2(x, y), leaves, cherries, twig));
        }

        private static float BoughTwig(float x)
        {
            float t = x / 256f;
            return 72f + 10f * t - 34f * t * t;
        }

        private static Color CoffeePixel(Vector2 p, List<CoffeeLeaf> leaves, List<Cherry> cherries, Func<float, float> twig)
        {
            var color = Color.clear;
            if (twig != null && p.x > 2f && p.x < 232f)
            {
                float distance = Mathf.Abs(p.y - twig(p.x));
                float half = Mathf.Lerp(4.2f, 1.6f, p.x / 232f);
                if (distance < half + 1.8f)
                {
                    color = distance < half ? Twig : CoffeeInk;
                }
            }

            foreach (var leaf in leaves)
            {
                if (LeafColor(p, leaf, out var leafColor))
                {
                    color = leafColor;
                }
            }

            foreach (var cherry in cherries)
            {
                float distance = (p - cherry.Center).magnitude;
                if (distance < CherryRadius)
                {
                    color = distance > CherryRadius - 2f ? CoffeeInk : cherry.Color;
                    if ((p - cherry.Center - new Vector2(-2f, 2.4f)).magnitude < 1.8f)
                    {
                        color = Color.Lerp(cherry.Color, Color.white, 0.75f);
                    }
                }
            }

            return color;
        }

        /// <summary>A pointed oval, split along its midrib: a shiny side, a shaded side and a darker vein down the middle.</summary>
        private static bool LeafColor(Vector2 p, CoffeeLeaf leaf, out Color color)
        {
            color = Color.clear;
            var local = p - leaf.Base;
            float u = Vector2.Dot(local, leaf.Axis);
            if (u < 0f || u > leaf.Length)
            {
                return false;
            }

            float v = Vector2.Dot(local, new Vector2(-leaf.Axis.y, leaf.Axis.x));
            float along = u / leaf.Length;
            float width = leaf.HalfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(along, 0.75f)), 0.8f);
            float abs = Mathf.Abs(v);
            if (abs > width)
            {
                return false;
            }

            if (abs > width - LeafInk)
            {
                color = CoffeeInk;
                return true;
            }

            if (abs < 0.9f)
            {
                color = Color.Lerp(leaf.Dark, CoffeeInk, 0.55f);
                return true;
            }

            color = v > 0f ? leaf.Light : leaf.Dark;

            // The waxy shine of a coffee leaf: one soft streak on the lit side.
            if (v > width * 0.3f && v < width * 0.62f && along > 0.25f && along < 0.7f)
            {
                color = Color.Lerp(color, Color.white, 0.28f);
            }

            // Veins, faint, leaving the midrib towards the tip.
            float vein = Mathf.Repeat(u - abs * 1.1f, 11f);
            if (vein < 1f)
            {
                color = Color.Lerp(color, leaf.Dark, 0.5f);
            }

            return true;
        }

        private static CoffeeLeaf MakeLeaf(Vector2 start, float degrees, float length, float halfWidth, Color light, Color dark)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new CoffeeLeaf
            {
                Base = start,
                Axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)),
                Length = length,
                HalfWidth = halfWidth,
                Light = light,
                Dark = dark
            };
        }

        /// <summary>The body of the shrub: leaves that grow out from the foot, the darkest at the back and low, the brightest on top.</summary>
        private static List<CoffeeLeaf> CrownLeaves()
        {
            var random = new System.Random(4127);
            var leaves = new List<CoffeeLeaf>();
            var foot = new Vector2(128f, 18f);
            const int Count = 96;
            for (int i = 0; i < Count; i++)
            {
                float progress = i / (float)(Count - 1);
                float angle = (float)random.NextDouble() * Mathf.PI;
                float reach = Mathf.Sqrt((float)random.NextDouble());
                var start = new Vector2(128f + Mathf.Cos(angle) * reach * 92f, 40f + Mathf.Sin(angle) * reach * 104f);

                // They point out from the foot of the plant and droop a little at the sides.
                var outward = start - foot;
                float degrees = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg + ((float)random.NextDouble() - 0.5f) * 56f;
                degrees -= Mathf.Cos(degrees * Mathf.Deg2Rad) * 22f;
                float height = Mathf.Clamp01(start.y / 150f);
                float brightness = Mathf.Clamp01(progress * 0.7f + height * 0.4f);
                var light = Color.Lerp(HexColor(0.14f, 0.40f, 0.22f), HexColor(0.34f, 0.68f, 0.40f), brightness);
                var dark = Color.Lerp(HexColor(0.09f, 0.29f, 0.17f), HexColor(0.19f, 0.50f, 0.29f), brightness);
                leaves.Add(MakeLeaf(start, degrees, 38f + (float)random.NextDouble() * 16f, 11f + (float)random.NextDouble() * 4f, light, dark));
            }

            return leaves;
        }

        /// <summary>One bough: pairs of opposite leaves along the twig and cherries packed on the nodes between them.</summary>
        private static void BoughParts(bool ripe, out List<CoffeeLeaf> leaves, out List<Cherry> cherries)
        {
            var random = new System.Random(ripe ? 913 : 557);
            leaves = new List<CoffeeLeaf>();
            cherries = new List<Cherry>();
            var light = HexColor(0.33f, 0.70f, 0.40f);
            var dark = HexColor(0.17f, 0.48f, 0.27f);
            float[] nodes = { 32f, 78f, 122f, 164f, 202f };
            for (int n = 0; n < nodes.Length; n++)
            {
                float x = nodes[n];
                float y = BoughTwig(x);
                float slope = (BoughTwig(x + 2f) - BoughTwig(x - 2f)) / 4f;
                float along = Mathf.Atan(slope) * Mathf.Rad2Deg;
                float shrink = 1f - n * 0.09f;
                leaves.Add(MakeLeaf(new Vector2(x, y + 1f), along + 44f, 56f * shrink, 15f * shrink, light, dark));
                leaves.Add(MakeLeaf(new Vector2(x, y - 1f), along - 40f, 54f * shrink, 15f * shrink, light, dark));

                int count = n == nodes.Length - 1 ? 1 : 3 + random.Next(2);
                for (int c = 0; c < count; c++)
                {
                    float roll = (float)random.NextDouble();
                    Color color = ripe
                        ? (roll < 0.62f ? CherryRed : roll < 0.84f ? CherryCrimson : roll < 0.94f ? CherryYellow : CherryGreen)
                        : (roll < 0.72f ? CherryGreen : roll < 0.9f ? CherryYellow : CherryRed);
                    cherries.Add(new Cherry
                    {
                        Center = new Vector2(x + 9f + (c - count * 0.5f) * 9f, y - 7f + (float)random.NextDouble() * 12f),
                        Color = color
                    });
                }
            }

            leaves.Add(MakeLeaf(new Vector2(214f, BoughTwig(214f)), -8f, 36f, 11f, light, dark));
        }

        private static Color HexColor(float r, float g, float b)
        {
            return new Color(r, g, b, 1f);
        }
    }
}

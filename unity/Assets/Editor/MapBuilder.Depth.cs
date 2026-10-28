using ColonQuest.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Extra layers that give the town depth: haze toward the far north, ground cover that the player
    /// walks behind and in front of, and floating motes and light beams that slide at their own speed.
    /// </summary>
    public static partial class MapBuilder
    {
        private static void BuildDepth(Transform parent, Transform shadows, System.Random random)
        {
            // Flat on the ground, back to front.
            BuildSunPatches(parent, random);
            BuildContactShadows(shadows);
            BuildShells(parent, random);
            BuildWaterSparkles(parent, random);
            BuildMist(parent, random);

            // Standing things, sorted by their feet.
            BuildGroundCover(parent, shadows, random);
            BuildReeds(parent);

            // In the air, from the nearest layer to the farthest.
            BuildDistanceHaze(parent);
            BuildColorGrade(parent);
            BuildLeafShadows(parent, random);
            BuildMotes(parent, random);
            BuildLightBeams(parent, random);
            BuildFallingLeaves(parent, random);
            BuildBirds(parent, random);
            BuildHighClouds(parent, random);
        }

        /// <summary>The farther north, the paler and bluer, as far things look through the air.</summary>
        private static void BuildDistanceHaze(Transform parent)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var haze = GamePalette.Hex("#BFD9EE");
            const float bottom = -4f;
            const float top = 34f;
            const int bands = 38;
            float step = (top - bottom) / bands;
            for (int i = 0; i < bands; i++)
            {
                float t = (i + 1f) / bands;
                var color = haze;
                color.a = 0.24f * t * t;
                Shape(parent, "Haze", square, new Vector2(0f, bottom + step * (i + 0.5f)), new Vector2(100f, step + 0.02f), color, 3);
            }

            currentLayer = previous;
        }

        private static bool FreeGround(Transform map, float x, float y)
        {
            if (!OnOpenGrass(x, y))
            {
                return false;
            }

            // The streets, their sidewalks and the lots between them.
            if (y > -17.8f && y < 6.8f)
            {
                return false;
            }

            // The football field.
            if (x < -27f && y < -17f)
            {
                return false;
            }

            foreach (var footprint in footprints)
            {
                if (footprint.Contains(new Vector2(x, y)))
                {
                    return false;
                }
            }

            foreach (var group in map.GetComponentsInChildren<SortingGroup>())
            {
                if ((Vector2)group.transform.position != Vector2.zero && Vector2.Distance(group.transform.position, new Vector2(x, y)) < 1.8f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Tall grass, flowers, bushes and rocks sorted by their feet, so the player passes behind some and in front of others.</summary>
        private static void BuildGroundCover(Transform parent, Transform shadows, System.Random random)
        {
            var map = parent.parent;
            int placed = 0;
            for (int attempt = 0; attempt < 4200 && placed < 330; attempt++)
            {
                float x = (float)(random.NextDouble() * 88.0 - 44.0);
                float y = (float)(random.NextDouble() * 54.0 - 27.0);
                if (!FreeGround(map, x, y))
                {
                    continue;
                }

                double roll = random.NextDouble();
                if (roll < 0.52)
                {
                    GrassClump(parent, x, y, 0.8f + (float)random.NextDouble() * 0.5f);
                }
                else if (roll < 0.74)
                {
                    FlowerClump(parent, x, y, random);
                }
                else if (roll < 0.9)
                {
                    Bush(parent, shadows, x, y, 0.8f + (float)random.NextDouble() * 0.5f);
                }
                else
                {
                    Rock(parent, shadows, x, y, 0.55f + (float)random.NextDouble() * 0.4f);
                }

                placed++;
            }
        }

        private static void GrassClump(Transform parent, float x, float y, float scale)
        {
            var root = Root(parent, "Grass", new Vector2(x, y), y);
            var dark = GamePalette.Hex("#3E9457");
            var light = GamePalette.Hex("#5BBE74");
            foreach (var blade in new[] { (-0.22f, 14f), (0f, 0f), (0.22f, -14f), (-0.1f, 7f), (0.12f, -7f) })
            {
                var shape = Shape(root, "Blade", roundedSmall, new Vector2(blade.Item1, 0.3f) * scale, new Vector2(0.14f, 0.62f) * scale, blade.Item1 == 0f ? light : dark, 0);
                Tilt(shape, blade.Item2);
            }
        }

        private static void FlowerClump(Transform parent, float x, float y, System.Random random)
        {
            var root = Root(parent, "Flowers", new Vector2(x, y), y);
            string[] colors = { "#FF7A8A", "#FFD23F", "#F8F5E8", "#B28DFF" };
            string petal = colors[random.Next(colors.Length)];
            foreach (var offset in new[] { new Vector2(-0.2f, 0.35f), new Vector2(0.15f, 0.5f), new Vector2(0.02f, 0.2f) })
            {
                Shape(root, "Stem", roundedSmall, offset * 0.8f + new Vector2(0f, -0.12f), new Vector2(0.06f, 0.4f), GamePalette.Hex("#3E9457"), 0);
                Shape(root, "Petal", circle, offset, Vector2.one * 0.22f, GamePalette.Hex(petal), 1);
                Shape(root, "Heart", circle, offset, Vector2.one * 0.09f, GamePalette.Hex("#FFD23F"), 2);
            }
        }

        /// <summary>Pollen and dust that float past in front of the world and slide faster than it.</summary>
        private static void BuildMotes(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "Motes", -0.3f);
            var color = GamePalette.Hex("#FFF6B8");
            for (int i = 0; i < 330; i++)
            {
                float x = (float)(random.NextDouble() * 180.0 - 90.0);
                float y = (float)(random.NextDouble() * 112.0 - 56.0);
                float size = 0.1f + (float)random.NextDouble() * 0.16f;
                color.a = 0.3f + (float)random.NextDouble() * 0.35f;
                var mote = Shape(layer, "Mote", softCircle, new Vector2(x, y), Vector2.one * size * 2.2f, color, 1);
                mote.gameObject.AddComponent<Drift>().Configure(new Vector2(0.12f + (float)random.NextDouble() * 0.18f, 0.1f), new Vector2(90f, 56f));
                if (i % 4 == 0)
                {
                    Flickering(mote, 0.25f, 0.8f + (float)random.NextDouble(), i);
                }
            }

            currentLayer = previous;
        }

        /// <summary>Wide soft slanted beams of sunlight that move at a different speed than the ground.</summary>
        private static void BuildLightBeams(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "LightBeams", -0.15f);
            var color = GamePalette.Hex("#FFF3C4");
            for (int i = 0; i < 9; i++)
            {
                color.a = 0.07f + (float)random.NextDouble() * 0.04f;
                float x = -70f + i * 17f + (float)random.NextDouble() * 6f;
                var beam = Shape(layer, "Beam", softCircle, new Vector2(x, (float)random.NextDouble() * 20f - 10f), new Vector2(3.4f + (float)random.NextDouble() * 2f, 34f), color, 2);
                Tilt(beam, 28f);
                Flickering(beam, 0.05f, 0.25f + (float)random.NextDouble() * 0.2f, i * 2f);
            }

            currentLayer = previous;
        }
    }
}

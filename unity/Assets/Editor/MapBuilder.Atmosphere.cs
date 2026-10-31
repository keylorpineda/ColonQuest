using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// More layers for the town: light and shade on the ground, water details, and several layers of
    /// things in the air that each slide at their own speed against the ground.
    /// </summary>
    public static partial class MapBuilder
    {
        /// <summary>Warm patches of sunlight on the grass.</summary>
        private static void BuildSunPatches(Transform parent, System.Random random)
        {
            var sun = GamePalette.Hex("#FFF0A8");
            for (int i = 0; i < 34; i++)
            {
                float x = (float)(random.NextDouble() * 88.0 - 44.0);
                float y = (float)(random.NextDouble() * 54.0 - 27.0);
                if (!OnOpenGrass(x, y))
                {
                    continue;
                }

                sun.a = 0.14f + (float)random.NextDouble() * 0.08f;
                float size = 4f + (float)random.NextDouble() * 4f;
                OnGround(parent, "Sun", softCircle, new Vector2(x, y), new Vector2(size * 1.3f, size), sun, TownGrassPatch);
            }
        }

        /// <summary>A thin dark strip where each building meets the ground.</summary>
        private static void BuildContactShadows(Transform shadows)
        {
            foreach (var footprint in footprints)
            {
                float baseY = footprint.yMin + 1.5f;
                var color = GamePalette.Outline;
                color.a = 0.26f;
                OnGround(shadows, "Contact", softCircle, new Vector2(footprint.center.x, baseY - 0.1f), new Vector2(footprint.width - 1.2f, 0.9f), color, TownShadow + 1);
            }
        }

        private static void BuildShells(Transform parent, System.Random random)
        {
            string[] tones = { "#F8F5E8", "#F4C7B8", "#E8D2A0" };
            for (int i = 0; i < 60; i++)
            {
                float x = (float)(random.NextDouble() * 80.0 - 40.0);
                float y = -19.5f - (float)random.NextDouble() * 7f;
                OnGround(parent, "Shell", circle, new Vector2(x, y), new Vector2(0.2f, 0.13f), GamePalette.Hex(tones[random.Next(tones.Length)]), WorldSand + 2);
            }
        }

        /// <summary>Little glints that blink on the sea and the river.</summary>
        private static void BuildWaterSparkles(Transform parent, System.Random random)
        {
            var glint = GamePalette.White;
            glint.a = 0.7f;
            for (int i = 0; i < 70; i++)
            {
                float x = (float)(random.NextDouble() * 92.0 - 46.0);
                float y = -28.6f - (float)random.NextDouble() * 5f;
                if (x > MapLayout.PierXMin - 0.5f && x < MapLayout.PierXMax + 0.5f)
                {
                    continue;
                }

                Flickering(OnGround(parent, "Glint", circle, new Vector2(x, y), new Vector2(0.28f, 0.1f), glint, 7), 0.6f, 0.8f + (float)random.NextDouble() * 1.4f, i);
            }

            for (int i = 0; i < 26; i++)
            {
                float x = MapLayout.RiverXMin + 0.4f + (float)random.NextDouble() * (MapLayout.RiverXMax - MapLayout.RiverXMin - 0.8f);
                float y = -26f + (float)random.NextDouble() * 50f;
                Flickering(OnGround(parent, "Glint", circle, new Vector2(x, y), new Vector2(0.24f, 0.09f), glint, 9), 0.6f, 0.8f + (float)random.NextDouble() * 1.4f, i + 100f);
            }
        }

        /// <summary>Low mist hugging the river and the waterline.</summary>
        private static void BuildMist(Transform parent, System.Random random)
        {
            var mist = GamePalette.White;
            float riverX = (MapLayout.RiverXMin + MapLayout.RiverXMax) * 0.5f;
            for (float y = -26f; y < 23f; y += 5.5f)
            {
                mist.a = 0.2f;
                Flickering(OnGround(parent, "Mist", softCircle, new Vector2(riverX + (float)random.NextDouble() - 0.5f, y), new Vector2(6.5f, 4.5f), mist, 55), 0.35f, 0.25f, y);
            }

            for (float x = -42f; x < 44f; x += 8f)
            {
                mist.a = 0.22f;
                Flickering(OnGround(parent, "Mist", softCircle, new Vector2(x + (float)random.NextDouble() * 2f, -27.4f), new Vector2(9f, 2.6f), mist, 55), 0.35f, 0.2f, x);
            }
        }

        /// <summary>Cattails along both river banks, left out where the bridges are.</summary>
        private static void BuildReeds(Transform parent)
        {
            foreach (float x in new[] { 27.5f, 31.6f })
            {
                for (float y = -24f; y < 21f; y += 1.9f)
                {
                    if ((y > -1.5f && y < 5.5f) || (y > -15.5f && y < -8.5f))
                    {
                        continue;
                    }

                    var root = Root(parent, "Reeds", new Vector2(x + Mathf.Sin(y * 3f) * 0.25f, y), y);
                    foreach (var blade in new[] { (-0.14f, 8f, 0.9f), (0.02f, -3f, 1.15f), (0.16f, -9f, 0.8f) })
                    {
                        var stem = Shape(root, "Reed", roundedSmall, new Vector2(blade.Item1, blade.Item3 * 0.5f), new Vector2(0.07f, blade.Item3), GamePalette.Hex("#4E9C5B"), 0);
                        Tilt(stem, blade.Item2);
                        var top = Shape(root, "Cattail", roundedSmall, new Vector2(blade.Item1 - blade.Item2 * 0.004f, blade.Item3 + 0.04f), new Vector2(0.12f, 0.3f), GamePalette.Hex("#7A4F2A"), 1);
                        Tilt(top, blade.Item2);
                    }
                }
            }
        }

        /// <summary>A warm cast from the upper left and a cool one from the lower right.</summary>
        private static void BuildColorGrade(Transform parent)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var warm = GamePalette.Hex("#FFC890");
            warm.a = 0.08f;
            Shape(parent, "Warm", softCircle, new Vector2(-24f, 20f), Vector2.one * 84f, warm, 5);
            var cool = GamePalette.Hex("#7FA8FF");
            cool.a = 0.09f;
            Shape(parent, "Cool", softCircle, new Vector2(28f, -26f), Vector2.one * 84f, cool, 5);

            currentLayer = previous;
        }

        /// <summary>Dark leafy shapes sliding across the ground, as if branches overhead moved in the wind.</summary>
        private static void BuildLeafShadows(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "LeafShadows", -0.2f);
            var color = GamePalette.Hex("#0B2A14");
            for (int i = 0; i < 60; i++)
            {
                color.a = 0.05f + (float)random.NextDouble() * 0.04f;
                float size = 1.6f + (float)random.NextDouble() * 2.4f;
                var leaf = Shape(layer, "LeafShadow", softCircle, new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 112f - 56f), new Vector2(size * 1.7f, size), color, 4);
                Tilt(leaf, (float)random.NextDouble() * 180f);
                leaf.gameObject.AddComponent<Drift>().Configure(new Vector2(0.18f + (float)random.NextDouble() * 0.12f, -0.05f), new Vector2(90f, 56f));
            }

            currentLayer = previous;
        }

        /// <summary>Autumn-colored leaves that drift down and across, close to the camera.</summary>
        private static void BuildFallingLeaves(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "FallingLeaves", -0.5f);
            string[] tones = { "#E8A33D", "#D9703A", "#8FBF4A", "#F2C94C" };
            for (int i = 0; i < 70; i++)
            {
                var leaf = Shape(layer, "Leaf", circle, new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 112f - 56f), new Vector2(0.24f, 0.13f), GamePalette.Hex(tones[random.Next(tones.Length)]), 6);
                Tilt(leaf, (float)random.NextDouble() * 180f);
                leaf.gameObject.AddComponent<Drift>().Configure(new Vector2(0.3f + (float)random.NextDouble() * 0.25f, -0.35f - (float)random.NextDouble() * 0.2f), new Vector2(90f, 56f));
            }

            currentLayer = previous;
        }

        /// <summary>A few birds crossing high above.</summary>
        private static void BuildBirds(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "Birds", -0.7f);
            var ink = GamePalette.Hex("#2A3345");
            ink.a = 0.8f;
            for (int i = 0; i < 7; i++)
            {
                var bird = Group(layer, "Bird");
                bird.localPosition = new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 80f - 40f);
                Tilt(Shape(bird, "Wing", circle, new Vector2(-0.16f, 0.04f), new Vector2(0.4f, 0.09f), ink, 7), 22f);
                Tilt(Shape(bird, "Wing", circle, new Vector2(0.16f, 0.04f), new Vector2(0.4f, 0.09f), ink, 7), -22f);
                bird.gameObject.AddComponent<Drift>().Configure(new Vector2(2.4f + (float)random.NextDouble() * 1.4f, 0.15f), new Vector2(90f, 56f));
            }

            currentLayer = previous;
        }

        /// <summary>Big pale clouds high above that slide faster than anything below them.</summary>
        private static void BuildHighClouds(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var layer = ParallaxGroup(parent, "HighClouds", -0.45f);
            var white = GamePalette.White;
            white.a = 0.04f;
            for (int i = 0; i < 12; i++)
            {
                var cloud = Group(layer, "Cloud");
                cloud.localPosition = new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 112f - 56f);
                for (int puff = 0; puff < 4; puff++)
                {
                    Shape(cloud, "Puff", softCircle, new Vector2((puff - 1.5f) * 3.2f, (puff % 2) * 0.9f), new Vector2(7f, 4.6f), white, 8);
                }

                cloud.gameObject.AddComponent<Drift>().Configure(new Vector2(0.7f + (float)random.NextDouble() * 0.4f, 0.02f), new Vector2(90f, 56f));
            }

            currentLayer = previous;
        }
    }
}

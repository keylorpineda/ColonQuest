using System;
using System.Collections.Generic;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The small things that make the map feel like a real city: street furniture, markings and stains on the
    /// roads, details on every facade and roof, wildlife, beach and river life, and more things in the air.
    /// Every kind of detail is its own named group, so it can be found, hidden or tuned on its own.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly List<BuildingSpec> buildingSpecs = new List<BuildingSpec>();
        private static readonly List<Vector2> occupied = new List<Vector2>();
        private static Transform cityShadows;
        private static int cityLayers;

        private static readonly Rect WholeMap = new Rect(-46f, -34f, 92f, 66f);

        private static void BuildCity(Transform parent, Transform shadows, System.Random random)
        {
            cityShadows = shadows;
            cityLayers = 0;

            var map = parent.parent;
            occupied.Clear();
            foreach (var group in map.GetComponentsInChildren<SortingGroup>())
            {
                occupied.Add(group.transform.position);
            }

            foreach (var station in map.GetComponentsInChildren<Station>())
            {
                occupied.Add(station.transform.position);
            }

            foreach (var door in map.GetComponentsInChildren<Door>())
            {
                occupied.Add(door.transform.position);
            }

            foreach (var spawn in map.GetComponentsInChildren<SpawnPoint>())
            {
                occupied.Add(spawn.transform.position);
            }

            BuildStreetMarkings(parent, random);
            BuildStreetFurnitureDetails(parent, random);
            BuildFacadeDetails(parent, random);
            BuildRoofDetails(parent, random);
            BuildNatureDetails(parent, random);
            BuildWaterAndBeachLife(parent, random);
            BuildSkyDetails(parent, random);

            Debug.Log("City detail layers: " + cityLayers);
        }

        // ---------------------------------------------------------------- helpers

        private static Transform CityGroup(Transform parent, string name)
        {
            cityLayers++;
            return Group(parent, name);
        }

        private static bool Clear(float x, float y, float radius)
        {
            float limit = radius * radius;
            foreach (var point in occupied)
            {
                float dx = point.x - x;
                float dy = point.y - y;
                if (dx * dx + dy * dy < limit)
                {
                    return false;
                }
            }

            foreach (var footprint in footprints)
            {
                if (footprint.Contains(new Vector2(x, y)))
                {
                    return false;
                }
            }

            return true;
        }

        private static void Scatter(Transform group, System.Random random, int count, Rect area, Func<float, float, bool> allowed, float clear, Action<Transform, float, float> draw)
        {
            int placed = 0;
            for (int attempt = 0; attempt < count * 30 && placed < count; attempt++)
            {
                float x = area.xMin + (float)random.NextDouble() * area.width;
                float y = area.yMin + (float)random.NextDouble() * area.height;
                if (!allowed(x, y) || !Clear(x, y, clear))
                {
                    continue;
                }

                draw(group, x, y);
                occupied.Add(new Vector2(x, y));
                placed++;
            }
        }

        private static bool OnWalkway(float x, float y)
        {
            if (y > 3.8f && y < 6.4f && x > -25f && x < 15f)
            {
                return false; // Keep the hero sidewalks in front of Tu Casa, Banco, Plaza, and Tienda free of random clutter
            }

            return (y > 4.2f && y < 5.8f) || (y > -1.8f && y < -0.2f) || (y > -9.8f && y < -8.2f) || (y > -15.8f && y < -14.2f);
        }

        private static bool OnRoadLane(float x, float y)
        {
            bool lane = (y > 0.3f && y < 3.7f && Mathf.Abs(y - 2f) > 0.4f) || (y > -13.7f && y < -10.3f && Mathf.Abs(y + 12f) > 0.4f);
            bool crossing = Mathf.Abs(Mathf.Abs(x) - 3.6f) < 1.4f;
            return lane && !crossing;
        }

        private static bool OnSand(float x, float y)
        {
            return y > -26.6f && y < -19.6f;
        }

        private static bool OnOpenGround(float x, float y)
        {
            return OnOpenGrass(x, y) && !(y > -19.4f && y < 6.8f) && !(x < -27f && y < -17f);
        }

        private static Color Hex(string hex, float alpha = 1f)
        {
            var color = GamePalette.Hex(hex);
            color.a = alpha;
            return color;
        }

        /// <summary>A standing object sorted by its feet.</summary>
        private static Transform Stand(Transform group, string name, float x, float y)
        {
            return Root(group, name, new Vector2(x, y), y);
        }

        /// <summary>A part with a dark outline: the outline is drawn first, then the colored shape on top.</summary>
        private static SpriteRenderer Part(Transform root, Sprite sprite, Vector2 center, Vector2 size, string hex, int layer)
        {
            Shape(root, "Outline", sprite, center, size + Vector2.one * 0.1f, GamePalette.Outline, layer * 2);
            return Shape(root, "Part", sprite, center, size, GamePalette.Hex(hex), layer * 2 + 1);
        }

        private static void Blob(float x, float y, float width, float alpha = 0.2f)
        {
            GroundShadow(cityShadows, new Vector2(x, y - 0.05f), new Vector2(width, width * 0.35f), alpha);
        }

        private static SpriteRenderer Flat(Transform group, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            return OnGround(group, name, sprite, center, size, color, order);
        }

        // ---------------------------------------------------------------- roads and sidewalks (flat)

        private static void BuildStreetMarkings(Transform parent, System.Random random)
        {
            var seams = CityGroup(parent, "SidewalkSeams");
            foreach (float y in new[] { -1f, -9f, -15f })
            {
                for (float x = -49f; x < 49f; x += 3f)
                {
                    Flat(seams, "Seam", square, new Vector2(x, y), new Vector2(0.04f, 1.4f), Hex("#8E8574", 0.35f), TownWalkPlate + 1);
                }
            }

            var stains = CityGroup(parent, "SidewalkStains");
            Scatter(stains, random, 90, WholeMap, (x, y) => OnWalkway(x, y), 0.2f, (g, x, y) =>
                Flat(g, "Stain", softCircle, new Vector2(x, y), new Vector2(0.3f + (float)random.NextDouble() * 0.3f, 0.2f), Hex("#5B6075", 0.35f), TownWalkPlate + 3));

            var litter = CityGroup(parent, "Litter");
            string[] papers = { "#F8F5E8", "#FFD23F", "#E8505B", "#4DA3FF" };
            Scatter(litter, random, 60, WholeMap, (x, y) => OnWalkway(x, y) || OnRoadLane(x, y), 0.3f, (g, x, y) =>
                Tilt(Flat(g, "Paper", square, new Vector2(x, y), new Vector2(0.2f, 0.14f), Hex(papers[random.Next(papers.Length)]), TownRoadMark + 1), (float)random.NextDouble() * 180f));

            var cracks = CityGroup(parent, "SidewalkWeeds");
            foreach (float y in new[] { 6.02f, 3.98f, -0.02f, -1.98f, -8.02f, -9.98f, -13.98f, -16.02f })
            {
                for (int i = 0; i < 9; i++)
                {
                    float x = (float)random.NextDouble() * 92f - 46f;
                    Flat(cracks, "Weed", circle, new Vector2(x, y), new Vector2(0.28f, 0.16f), Hex("#4CB36F"), TownRoadMark + 1);
                    Flat(cracks, "Weed", circle, new Vector2(x + 0.14f, y + 0.04f), new Vector2(0.2f, 0.12f), Hex("#3E9457"), TownRoadMark + 1);
                }
            }

            var manholes = CityGroup(parent, "Manholes");
            Scatter(manholes, random, 12, WholeMap, OnRoadLane, 2.5f, (g, x, y) =>
            {
                Flat(g, "Cover", circle, new Vector2(x, y), new Vector2(0.74f, 0.74f), GamePalette.Outline, TownRoadMark);
                Flat(g, "Cover", circle, new Vector2(x, y), new Vector2(0.62f, 0.62f), Hex("#6B7186"), TownRoadMark + 1);
                Flat(g, "Grid", square, new Vector2(x, y), new Vector2(0.5f, 0.05f), Hex("#3A3F52"), TownRoadMark + 2);
            });

            var patches = CityGroup(parent, "RoadPatches");
            Scatter(patches, random, 14, WholeMap, OnRoadLane, 1.5f, (g, x, y) =>
                Flat(g, "Patch", rounded, new Vector2(x, y), new Vector2(1.1f + (float)random.NextDouble(), 0.6f), Hex("#4A5166"), TownRoadMark));

            var roadCracks = CityGroup(parent, "RoadCracks");
            Scatter(roadCracks, random, 34, WholeMap, OnRoadLane, 0.5f, (g, x, y) =>
                Tilt(Flat(g, "Crack", square, new Vector2(x, y), new Vector2(0.9f + (float)random.NextDouble() * 0.8f, 0.04f), Hex("#2E3345", 0.7f), TownRoadMark), (float)random.NextDouble() * 60f - 30f));

            var oil = CityGroup(parent, "OilStains");
            Scatter(oil, random, 12, WholeMap, OnRoadLane, 1f, (g, x, y) =>
                Flat(g, "Oil", softCircle, new Vector2(x, y), new Vector2(1.3f, 0.7f), Hex("#14172A", 0.35f), TownRoadMark));

            var puddles = CityGroup(parent, "RoadPuddles");
            Scatter(puddles, random, 10, WholeMap, (x, y) => OnRoadLane(x, y) || OnWalkway(x, y), 1f, (g, x, y) =>
            {
                Flat(g, "Puddle", softCircle, new Vector2(x, y), new Vector2(1.5f, 0.7f), Hex("#7FB6E8", 0.45f), TownRoadMark + 1);
                Flat(g, "Shine", softCircle, new Vector2(x - 0.2f, y + 0.08f), new Vector2(0.6f, 0.2f), Hex("#FFFFFF", 0.5f), TownRoadMark + 2);
            });

            var arrows = CityGroup(parent, "RoadArrows");
            foreach (float x in new[] { -38f, -26f, -16f, 16f, 26f, 38f })
            {
                foreach (float y in new[] { 1f, -13f })
                {
                    Flat(arrows, "Shaft", square, new Vector2(x, y), new Vector2(1.4f, 0.14f), Hex("#F8F5E8", 0.85f), TownRoadMark);
                    Tilt(Flat(arrows, "Head", square, new Vector2(x + 0.75f, y), new Vector2(0.4f, 0.4f), Hex("#F8F5E8", 0.85f), TownRoadMark), 45f);
                }
            }

            var curbPaint = CityGroup(parent, "CurbPaint");
            foreach (float y in new[] { -10.14f, -13.86f })
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = (float)random.NextDouble() * 88f - 44f;
                    Flat(curbPaint, "Paint", square, new Vector2(x, y), new Vector2(2.2f, 0.16f), Hex(i % 2 == 0 ? "#FFD23F" : "#E8505B", 0.9f), TownRoadMark);
                }
            }

            var edgeLines = CityGroup(parent, "RoadEdgeLines");
            foreach (float y in new[] { 3.55f, 0.45f, -10.45f, -13.55f })
            {
                Flat(edgeLines, "Edge", square, new Vector2(0f, y), new Vector2(98f, 0.07f), Hex("#F8F5E8", 0.6f), TownRoadMark);
            }

            var parking = CityGroup(parent, "ParkingLines");
            foreach (var range in new[] { (-46f, -28f), (28f, 46f) })
            {
                for (float x = range.Item1; x < range.Item2; x += 2.8f)
                {
                    foreach (float y in new[] { 1f, 3f, -11f, -13f })
                    {
                        Flat(parking, "Bay", square, new Vector2(x, y), new Vector2(0.07f, 1.1f), Hex("#F8F5E8", 0.7f), TownRoadMark);
                    }
                }
            }

            var drains = CityGroup(parent, "StormDrains");
            foreach (float y in new[] { 3.7f, 0.3f, -10.3f, -13.7f })
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = (float)random.NextDouble() * 80f - 40f;
                    Flat(drains, "Drain", rounded, new Vector2(x, y), new Vector2(0.9f, 0.24f), GamePalette.Outline, TownRoadMark);
                    Flat(drains, "Bars", square, new Vector2(x, y), new Vector2(0.7f, 0.05f), Hex("#4A5166"), TownRoadMark + 1);
                }
            }
        }

        // ---------------------------------------------------------------- street furniture

        private static void BuildStreetFurnitureDetails(Transform parent, System.Random random)
        {
            Scatter(CityGroup(parent, "TrashCans"), random, 16, WholeMap, OnWalkway, 2.2f, (g, x, y) =>
            {
                var root = Stand(g, "TrashCan", x, y);
                Blob(x, y, 0.8f);
                Part(root, roundedSmall, new Vector2(0f, 0.3f), new Vector2(0.44f, 0.6f), "#4A6B5A", 0);
                Part(root, roundedSmall, new Vector2(0f, 0.64f), new Vector2(0.52f, 0.12f), "#38503F", 1);
            });

            Scatter(CityGroup(parent, "RecyclingBins"), random, 6, WholeMap, OnWalkway, 2.5f, (g, x, y) =>
            {
                var root = Stand(g, "Recycling", x, y);
                Blob(x, y, 1.6f);
                string[] tones = { "#4DA3FF", "#55E08F", "#FFD23F" };
                for (int i = 0; i < 3; i++)
                {
                    Part(root, roundedSmall, new Vector2((i - 1) * 0.5f, 0.3f), new Vector2(0.4f, 0.6f), tones[i], 0);
                }
            });

            Scatter(CityGroup(parent, "NewsBoxes"), random, 5, WholeMap, OnWalkway, 3f, (g, x, y) =>
            {
                var root = Stand(g, "NewsBox", x, y);
                Blob(x, y, 0.8f);
                Part(root, roundedSmall, new Vector2(0f, 0.32f), new Vector2(0.46f, 0.64f), "#E8505B", 0);
                Shape(root, "Window", square, new Vector2(0f, 0.42f), new Vector2(0.28f, 0.22f), Hex("#CFE8FF"), 3);
            });

            Scatter(CityGroup(parent, "Hydrants"), random, 6, WholeMap, OnWalkway, 4f, (g, x, y) =>
            {
                var root = Stand(g, "Hydrant", x, y);
                Blob(x, y, 0.7f);
                Part(root, roundedSmall, new Vector2(0f, 0.26f), new Vector2(0.28f, 0.5f), "#E8505B", 0);
                Part(root, circle, new Vector2(0f, 0.55f), new Vector2(0.34f, 0.2f), "#E8505B", 1);
                Part(root, roundedSmall, new Vector2(0.2f, 0.32f), new Vector2(0.16f, 0.12f), "#C93D48", 1);
            });

            Scatter(CityGroup(parent, "Mailboxes"), random, 5, WholeMap, OnWalkway, 4f, (g, x, y) =>
            {
                var root = Stand(g, "Mailbox", x, y);
                Blob(x, y, 0.8f);
                Part(root, roundedSmall, new Vector2(0f, 0.18f), new Vector2(0.1f, 0.36f), "#6B7186", 0);
                Part(root, rounded, new Vector2(0f, 0.5f), new Vector2(0.56f, 0.34f), "#4DA3FF", 1);
            });

            Scatter(CityGroup(parent, "BusStops"), random, 3, WholeMap, (x, y) => y < -14.2f && y > -15.8f, 9f, (g, x, y) =>
            {
                var root = Stand(g, "BusStop", x, y);
                Blob(x, y, 3.6f, 0.22f);
                Part(root, roundedSmall, new Vector2(-1.2f, 0.9f), new Vector2(0.12f, 1.8f), "#6B7186", 0);
                Part(root, roundedSmall, new Vector2(1.2f, 0.9f), new Vector2(0.12f, 1.8f), "#6B7186", 0);
                Part(root, rounded, new Vector2(0f, 1.9f), new Vector2(2.9f, 0.34f), "#E8505B", 1);
                Part(root, rounded, new Vector2(0f, 0.9f), new Vector2(2.1f, 0.9f), "#CFE8FF", 0).color = Hex("#CFE8FF", 0.55f);
                Part(root, roundedSmall, new Vector2(0f, 0.45f), new Vector2(1.6f, 0.2f), "#B07A45", 2);
            });

            Scatter(CityGroup(parent, "BikeRacks"), random, 5, WholeMap, OnWalkway, 4f, (g, x, y) =>
            {
                var root = Stand(g, "BikeRack", x, y);
                Blob(x, y, 1.8f);
                for (int i = -1; i <= 1; i++)
                {
                    Part(root, ring, new Vector2(i * 0.5f, 0.32f), new Vector2(0.42f, 0.55f), "#8A90A5", 0);
                }
            });

            Scatter(CityGroup(parent, "ParkedBikes"), random, 5, WholeMap, OnWalkway, 3.5f, (g, x, y) =>
            {
                var root = Stand(g, "Bike", x, y);
                Blob(x, y, 1.5f);
                string[] tones = { "#4DA3FF", "#E8505B", "#55E08F", "#FFD23F" };
                string tone = tones[random.Next(tones.Length)];
                Part(root, ring, new Vector2(-0.4f, 0.28f), new Vector2(0.5f, 0.5f), "#1D2238", 0);
                Part(root, ring, new Vector2(0.4f, 0.28f), new Vector2(0.5f, 0.5f), "#1D2238", 0);
                Shape(root, "Frame", square, new Vector2(0f, 0.38f), new Vector2(0.8f, 0.06f), Hex(tone), 4);
                Shape(root, "Bar", square, new Vector2(0.35f, 0.58f), new Vector2(0.3f, 0.05f), Hex(tone), 4);
            });

            Scatter(CityGroup(parent, "Planters"), random, 14, WholeMap, OnWalkway, 2.4f, (g, x, y) =>
            {
                var root = Stand(g, "Planter", x, y);
                Blob(x, y, 1.1f);
                Part(root, roundedSmall, new Vector2(0f, 0.2f), new Vector2(0.7f, 0.4f), "#B07A45", 0);
                Part(root, circle, new Vector2(0f, 0.5f), new Vector2(0.7f, 0.5f), "#4CB36F", 1);
                Shape(root, "Bloom", circle, new Vector2(-0.15f, 0.62f), Vector2.one * 0.16f, Hex("#FF7A8A"), 6);
                Shape(root, "Bloom", circle, new Vector2(0.18f, 0.55f), Vector2.one * 0.14f, Hex("#FFD23F"), 6);
            });

            Scatter(CityGroup(parent, "StreetSigns"), random, 10, WholeMap, OnWalkway, 4f, (g, x, y) =>
            {
                var root = Stand(g, "StreetSign", x, y);
                Blob(x, y, 0.5f);
                Part(root, roundedSmall, new Vector2(0f, 0.8f), new Vector2(0.08f, 1.6f), "#6B7186", 0);
                bool stop = random.NextDouble() < 0.4;
                Part(root, stop ? circle : rounded, new Vector2(0f, 1.6f), new Vector2(0.56f, 0.56f), stop ? "#E8505B" : "#4DA3FF", 1);
                Shape(root, "Mark", square, new Vector2(0f, 1.6f), new Vector2(0.34f, 0.08f), Hex("#F8F5E8"), 6);
            });

            Scatter(CityGroup(parent, "TrafficCones"), random, 7, WholeMap, OnRoadLane, 3f, (g, x, y) =>
            {
                var root = Stand(g, "Cone", x, y);
                Blob(x, y, 0.6f);
                Part(root, rounded, new Vector2(0f, 0.04f), new Vector2(0.4f, 0.1f), "#2A3345", 0);
                Part(root, circle, new Vector2(0f, 0.32f), new Vector2(0.3f, 0.5f), "#FF8A3D", 1);
                Shape(root, "Stripe", square, new Vector2(0f, 0.34f), new Vector2(0.24f, 0.07f), Hex("#F8F5E8"), 6);
            });

            Scatter(CityGroup(parent, "UtilityPoles"), random, 10, WholeMap, (x, y) => y < -14.2f && y > -15.8f, 8f, (g, x, y) =>
            {
                var root = Stand(g, "Pole", x, y);
                Blob(x, y, 0.8f);
                Part(root, roundedSmall, new Vector2(0f, 1.9f), new Vector2(0.18f, 3.8f), "#7A4F2A", 0);
                Part(root, roundedSmall, new Vector2(0f, 3.5f), new Vector2(1.5f, 0.14f), "#7A4F2A", 1);
                Part(root, roundedSmall, new Vector2(0f, 3.0f), new Vector2(1.1f, 0.12f), "#7A4F2A", 1);
                foreach (float dx in new[] { -0.7f, 0f, 0.7f })
                {
                    Shape(root, "Insulator", circle, new Vector2(dx, 3.62f), Vector2.one * 0.12f, Hex("#CFE8FF"), 5);
                }
            });

            var lights = CityGroup(parent, "TrafficLights");
            foreach (var spot in new[] { new Vector2(5.8f, 4.5f), new Vector2(-5.8f, 4.5f), new Vector2(5.8f, -9.4f), new Vector2(-5.8f, -9.4f), new Vector2(5.8f, -0.6f), new Vector2(-5.8f, -0.6f) })
            {
                if (!Clear(spot.x, spot.y, 1.6f))
                {
                    continue;
                }

                var root = Stand(lights, "TrafficLight", spot.x, spot.y);
                Blob(spot.x, spot.y, 0.6f);
                Part(root, roundedSmall, new Vector2(0f, 0.9f), new Vector2(0.1f, 1.8f), "#4A5166", 0);
                Part(root, roundedSmall, new Vector2(0f, 1.9f), new Vector2(0.3f, 0.78f), "#1D2238", 1);
                Shape(root, "Red", circle, new Vector2(0f, 2.14f), Vector2.one * 0.16f, Hex("#E8505B"), 6);
                Shape(root, "Yellow", circle, new Vector2(0f, 1.9f), Vector2.one * 0.16f, Hex("#FFD23F"), 6);
                Flickering(Shape(root, "Green", circle, new Vector2(0f, 1.66f), Vector2.one * 0.16f, Hex("#55E08F"), 6), 0.3f, 0.5f, spot.x);
                occupied.Add(spot);
            }

            var cars = CityGroup(parent, "ParkedCars");
            string[] carColors = { "#E8505B", "#4DA3FF", "#FFD23F", "#55E08F", "#B28DFF", "#F8F5E8" };
            for (int i = 0; i < 40 && cars.childCount < 7; i++)
            {
                float x = (float)random.NextDouble() * 90f - 45f;
                float y = new[] { 1f, 3f, -11f, -13f }[random.Next(4)];
                if (Mathf.Abs(x) < 5f || !Clear(x, y, 3.8f))
                {
                    continue;
                }

                Car(cars, cityShadows, x, y, carColors[random.Next(carColors.Length)]);
                occupied.Add(new Vector2(x, y));
            }

            Scatter(CityGroup(parent, "Motorbikes"), random, 5, WholeMap, OnRoadLane, 3f, (g, x, y) =>
            {
                var root = Stand(g, "Motorbike", x, y);
                Blob(x, y, 1.4f);
                Part(root, ring, new Vector2(-0.45f, 0.25f), new Vector2(0.46f, 0.46f), "#1D2238", 0);
                Part(root, ring, new Vector2(0.45f, 0.25f), new Vector2(0.46f, 0.46f), "#1D2238", 0);
                Part(root, rounded, new Vector2(0f, 0.5f), new Vector2(0.8f, 0.3f), random.NextDouble() < 0.5 ? "#E8505B" : "#4DA3FF", 1);
                Part(root, roundedSmall, new Vector2(0.4f, 0.74f), new Vector2(0.28f, 0.08f), "#6B7186", 2);
            });

            Scatter(CityGroup(parent, "ConstructionBarriers"), random, 4, WholeMap, OnWalkway, 6f, (g, x, y) =>
            {
                var root = Stand(g, "Barrier", x, y);
                Blob(x, y, 1.8f);
                Part(root, roundedSmall, new Vector2(0f, 0.55f), new Vector2(1.6f, 0.2f), "#F8F5E8", 0);
                Shape(root, "Stripe", square, new Vector2(-0.4f, 0.55f), new Vector2(0.3f, 0.16f), Hex("#E8505B"), 4);
                Shape(root, "Stripe", square, new Vector2(0.4f, 0.55f), new Vector2(0.3f, 0.16f), Hex("#E8505B"), 4);
                Part(root, roundedSmall, new Vector2(-0.7f, 0.25f), new Vector2(0.1f, 0.5f), "#6B7186", 0);
                Part(root, roundedSmall, new Vector2(0.7f, 0.25f), new Vector2(0.1f, 0.5f), "#6B7186", 0);
            });

            var benches = CityGroup(parent, "ExtraBenches");
            Scatter(benches, random, 6, WholeMap, OnWalkway, 4f, (g, x, y) => Bench(g, cityShadows, x, y));

            var lamps = CityGroup(parent, "ExtraLamps");
            Scatter(lamps, random, 10, WholeMap, (x, y) => y < -14.2f && y > -15.8f, 7f, (g, x, y) => Lamp(g, x, y));

            var trees = CityGroup(parent, "StreetTrees");
            string[] leaves = { "#3FA66A", "#4CB36F", "#2F8A58" };
            Scatter(trees, random, 8, WholeMap, (x, y) => y < -14.4f && y > -15.6f, 6f, (g, x, y) => Tree(g, cityShadows, x, y, 0.8f, leaves[random.Next(leaves.Length)], false));

            Scatter(CityGroup(parent, "TrashBags"), random, 12, WholeMap, OnWalkway, 2f, (g, x, y) =>
            {
                var root = Stand(g, "Bags", x, y);
                Blob(x, y, 0.9f);
                Part(root, circle, new Vector2(-0.1f, 0.2f), new Vector2(0.4f, 0.36f), "#2A3345", 0);
                Part(root, circle, new Vector2(0.18f, 0.16f), new Vector2(0.32f, 0.3f), "#38503F", 0);
            });
        }

        // ---------------------------------------------------------------- facades

        private static bool IsHouse(BuildingSpec b)
        {
            return b.Name == "Casa" || b.Name.StartsWith("Casa_", StringComparison.Ordinal);
        }

        /// <summary>Shops and offices: everything that is not a home, the workshop or the farm.</summary>
        private static bool IsBusiness(BuildingSpec b)
        {
            return !IsHouse(b) && b.Name != "Establo" && b.Name != "Taller";
        }

        /// <summary>Only the places that handle money watch their door.</summary>
        private static bool HandlesMoney(BuildingSpec b)
        {
            return b.Name == "Banco" || b.Name == "Tienda" || b.Name == "Cooperativa" || b.Name == "KioscoSinpe";
        }

        /// <summary>The places that put a menu or offers out on the sidewalk.</summary>
        private static bool PutsOutABoard(BuildingSpec b)
        {
            return b.Name == "Pulperia" || b.Name == "SodaElMuelle" || b.Name == "Tienda";
        }

        private static void BuildFacadeDetails(Transform parent, System.Random random)
        {
            // One poster on the shops that advertise, next to the door and below the sign.
            string[] posters = { "#FFD23F", "#4DA3FF", "#FF7A8A", "#55E08F" };
            var posterGroup = CityGroup(parent, "WallPosters");
            int posterIndex = 0;
            foreach (var b in buildingSpecs)
            {
                if (!PutsOutABoard(b) || b.Name == "Tienda" || b.Name == "SodaElMuelle")
                {
                    continue;
                }

                float x = Mathf.Clamp(b.DoorX - 1.75f, b.XMin + 0.4f, b.XMax - 0.4f);
                var root = Root(posterGroup, "Poster", new Vector2(x, b.YBottom + 1.15f), b.YBottom - 0.05f);
                Part(root, square, Vector2.zero, new Vector2(0.42f, 0.56f), posters[posterIndex++ % posters.Length], 0);
                Shape(root, "Line", square, new Vector2(0f, 0.08f), new Vector2(0.28f, 0.05f), Hex("#F8F5E8"), 3);
                Shape(root, "Line", square, new Vector2(0f, -0.06f), new Vector2(0.28f, 0.05f), Hex("#F8F5E8"), 3);
            }

            // Air conditioners hang beside a window of the offices and shops, never on homes or Banco's classical columns.
            var conditioners = CityGroup(parent, "WindowAirConditioners");
            foreach (var b in buildingSpecs)
            {
                float side = FacadeWindowOffset(b);
                if (!IsBusiness(b) || b.Name == "Banco" || side <= 0f || b.AwningA != null)
                {
                    continue;
                }

                var root = Root(conditioners, "AC", new Vector2(b.DoorX + side, b.YBottom + 2.35f), b.YBottom - 0.05f);
                Part(root, rounded, Vector2.zero, new Vector2(0.7f, 0.36f), "#E3E7F1", 0);
                Shape(root, "Grille", square, new Vector2(0f, -0.02f), new Vector2(0.5f, 0.06f), Hex("#8E93A8"), 3);
                Shape(root, "Grille", square, new Vector2(0f, 0.09f), new Vector2(0.5f, 0.06f), Hex("#8E93A8"), 3);
            }

            // Flower boxes under the windows of the homes (Casa already builds its own rich window planters).
            string[] blooms = { "#FF7A8A", "#FFD23F", "#B28DFF", "#F8F5E8" };
            var boxes = CityGroup(parent, "FlowerBoxes");
            foreach (var b in buildingSpecs)
            {
                float side = FacadeWindowOffset(b);
                if (!IsHouse(b) || b.Name == "Casa" || side <= 0f)
                {
                    continue;
                }

                foreach (float s in new[] { -side, side })
                {
                    var root = Root(boxes, "FlowerBox", new Vector2(b.DoorX + s, b.YBottom + 0.9f), b.YBottom - 0.06f);
                    Part(root, roundedSmall, Vector2.zero, new Vector2(1.1f, 0.22f), "#7A4F2A", 0);
                    for (int i = -2; i <= 2; i++)
                    {
                        Shape(root, "Leaf", circle, new Vector2(i * 0.2f, 0.14f), Vector2.one * 0.2f, Hex("#3E9457"), 3);
                        Shape(root, "Bloom", circle, new Vector2(i * 0.2f, 0.2f), Vector2.one * 0.12f, Hex(blooms[random.Next(blooms.Length)]), 4);
                    }
                }
            }

            // A porch light over every door.
            var lamps = CityGroup(parent, "WallLamps");
            foreach (var b in buildingSpecs)
            {
                if (b.Entrance != Entrance.Door || b.Name == "Casa" || b.Name == "Banco" || b.Name == "Tienda")
                {
                    continue;
                }

                var root = Root(lamps, "PorchLight", new Vector2(b.DoorX, b.YBottom + 2.2f), b.YBottom - 0.05f);
                Part(root, roundedSmall, Vector2.zero, new Vector2(0.3f, 0.2f), "#4A5166", 0);
                Flickering(Shape(root, "Glow", softCircle, new Vector2(0f, -0.1f), Vector2.one * 1.3f, Hex("#FFD98A", 0.35f), 6), 0.15f, 0.3f, b.DoorX, true);
            }

            // A number plate beside the door of every home (placed to the left of the door, never covering the wood panels).
            var numbers = CityGroup(parent, "HouseNumbers");
            foreach (var b in buildingSpecs)
            {
                if (!IsHouse(b))
                {
                    continue;
                }

                var root = Root(numbers, "Number", new Vector2(b.DoorX - 0.95f, b.YBottom + 1.45f), b.YBottom - 0.05f);
                Part(root, rounded, Vector2.zero, new Vector2(0.34f, 0.24f), "#F8F5E8", 0);
                Shape(root, "Digit", square, new Vector2(-0.06f, 0f), new Vector2(0.05f, 0.12f), Hex("#2A3345"), 3);
                Shape(root, "Digit", square, new Vector2(0.06f, 0f), new Vector2(0.05f, 0.12f), Hex("#2A3345"), 3);
            }

            var cameras = CityGroup(parent, "SecurityCameras");
            foreach (var b in buildingSpecs)
            {
                if (!HandlesMoney(b))
                {
                    continue;
                }

                var root = Root(cameras, "Camera", new Vector2(b.DoorX - 1.6f, b.YBottom + b.FacadeHeight - 0.35f), b.YBottom - 0.05f);
                Part(root, rounded, Vector2.zero, new Vector2(0.3f, 0.16f), "#4A5166", 0);
                Flickering(Shape(root, "Light", circle, new Vector2(0.1f, 0f), Vector2.one * 0.06f, Hex("#E8505B"), 3), 0.8f, 1.2f, b.XMin);
            }

            var mats = CityGroup(parent, "DoorMats");
            foreach (var b in buildingSpecs)
            {
                if (b.Entrance != Entrance.Door || b.Name == "Casa" || b.Name == "Banco" || b.Name == "Tienda" || b.Name == "SodaElMuelle")
                {
                    continue;
                }

                Flat(mats, "Mat", rounded, new Vector2(b.DoorX, b.YBottom - 0.22f), new Vector2(1.1f, 0.36f), GamePalette.Outline, 16);
                Flat(mats, "Mat", rounded, new Vector2(b.DoorX, b.YBottom - 0.22f), new Vector2(0.98f, 0.26f), Hex("#B2475A"), 17);
            }

            var boards = CityGroup(parent, "SandwichBoards");
            foreach (var b in buildingSpecs)
            {
                float x = Mathf.Clamp(b.DoorX + 2.1f, b.XMin, b.XMax);
                float y = b.YBottom - 0.9f;
                if (!PutsOutABoard(b) || b.Name == "Tienda" || !Clear(x, y, 0.8f))
                {
                    continue;
                }

                var root = Stand(boards, "Board", x, y);
                Blob(x, y, 0.8f);
                Part(root, roundedSmall, new Vector2(0f, 0.4f), new Vector2(0.52f, 0.76f), "#2A3345", 0);
                Shape(root, "Chalk", square, new Vector2(0f, 0.46f), new Vector2(0.34f, 0.05f), Hex("#F8F5E8"), 3);
                Shape(root, "Chalk", square, new Vector2(0f, 0.34f), new Vector2(0.24f, 0.05f), Hex("#FFD23F"), 3);
                occupied.Add(new Vector2(x, y));
            }

            // Potted plants by the doors of neighborhood homes.
            var pots = CityGroup(parent, "DoorPlants");
            foreach (var b in buildingSpecs)
            {
                if ((!IsHouse(b) && b.Name != "Banco") || b.Name == "Casa" || b.Name == "Banco")
                {
                    continue;
                }

                foreach (float side in new[] { -1.4f, 1.4f })
                {
                    float x = b.DoorX + side;
                    float y = b.YBottom - 0.55f;
                    if (!Clear(x, y, 0.7f))
                    {
                        continue;
                    }

                    var root = Stand(pots, "Pot", x, y);
                    Blob(x, y, 0.8f);
                    Part(root, roundedSmall, new Vector2(0f, 0.18f), new Vector2(0.4f, 0.34f), "#C2683F", 0);
                    Part(root, circle, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.44f), "#3FA66A", 1);
                    occupied.Add(new Vector2(x, y));
                }
            }
        }

        // ---------------------------------------------------------------- roofs

        /// <summary>
        /// What sits on each roof follows what the building is. Homes have the water tank every Costa Rican
        /// house has, some an antenna; the workshop has an exhaust pipe. Nothing is placed at random.
        /// </summary>
        private static void BuildRoofDetails(Transform parent, System.Random random)
        {
            var tanks = CityGroup(parent, "RoofWaterTanks");
            var antennas = CityGroup(parent, "RoofAntennas");
            var solar = CityGroup(parent, "SolarPanels");
            var pipes = CityGroup(parent, "RoofPipes");

            int house = 0;
            foreach (var b in buildingSpecs)
            {
                float roofY = b.YTop - 1.1f;
                float cx = (b.XMin + b.XMax) * 0.5f;
                if (IsHouse(b) && b.Name != "Casa")
                {
                    var tank = Root(tanks, "Tank", new Vector2(b.XMax - 1.0f, roofY), b.YBottom - 0.05f);
                    Part(tank, roundedSmall, new Vector2(0f, 0.3f), new Vector2(0.7f, 0.6f), "#3C7FD9", 0);
                    Part(tank, circle, new Vector2(0f, 0.62f), new Vector2(0.7f, 0.28f), "#5B9BEA", 1);
                    Shape(tank, "Band", square, new Vector2(0f, 0.3f), new Vector2(0.7f, 0.05f), Hex("#2A5FA8"), 6);

                    if (house % 2 == 0)
                    {
                        var antenna = Root(antennas, "Antenna", new Vector2(b.XMin + 1.1f, roofY), b.YBottom - 0.05f);
                        Part(antenna, roundedSmall, new Vector2(0f, 0.5f), new Vector2(0.06f, 1.0f), "#6B7186", 0);
                        Part(antenna, roundedSmall, new Vector2(0f, 0.85f), new Vector2(0.6f, 0.06f), "#6B7186", 0);
                        Part(antenna, roundedSmall, new Vector2(0f, 0.65f), new Vector2(0.44f, 0.06f), "#6B7186", 0);
                    }

                    if (house == 1)
                    {
                        var panel = Root(solar, "Solar", new Vector2(cx, roofY - 0.2f), b.YBottom - 0.05f);
                        Part(panel, rounded, Vector2.zero, new Vector2(1.6f, 0.7f), "#2E4A7A", 0);
                        for (int i = -1; i <= 1; i++)
                        {
                            Shape(panel, "Cell", square, new Vector2(i * 0.5f, 0f), new Vector2(0.02f, 0.62f), Hex("#7FA8E8"), 3);
                        }
                    }

                    house++;
                }
                else if (b.Name == "Taller")
                {
                    var pipe = Root(pipes, "Exhaust", new Vector2(b.XMin + 1.3f, roofY), b.YBottom - 0.05f);
                    Part(pipe, roundedSmall, new Vector2(0f, 0.4f), new Vector2(0.26f, 0.8f), "#8A90A5", 0);
                    Part(pipe, rounded, new Vector2(0f, 0.82f), new Vector2(0.4f, 0.12f), "#6B7186", 1);
                }
            }
        }

        // ---------------------------------------------------------------- nature

        private static void BuildNatureDetails(Transform parent, System.Random random)
        {
            string[] leaves = { "#3FA66A", "#4CB36F", "#2F8A58", "#6BBF59" };
            Scatter(CityGroup(parent, "ExtraTrees"), random, 18, WholeMap, OnOpenGround, 3.5f, (g, x, y) =>
                Tree(g, cityShadows, x, y, 0.8f + (float)random.NextDouble() * 0.4f, leaves[random.Next(leaves.Length)], random.NextDouble() < 0.3));

            Scatter(CityGroup(parent, "TreeStumps"), random, 8, WholeMap, OnOpenGround, 2f, (g, x, y) =>
            {
                var root = Stand(g, "Stump", x, y);
                Blob(x, y, 0.9f);
                Part(root, roundedSmall, new Vector2(0f, 0.2f), new Vector2(0.5f, 0.4f), "#8A5A33", 0);
                Shape(root, "Rings", ring, new Vector2(0f, 0.38f), new Vector2(0.4f, 0.16f), Hex("#C99A62"), 3);
            });

            Scatter(CityGroup(parent, "FallenLogs"), random, 6, WholeMap, OnOpenGround, 2.5f, (g, x, y) =>
            {
                var root = Stand(g, "Log", x, y);
                Blob(x, y, 1.8f);
                Tilt(Part(root, roundedSmall, new Vector2(0f, 0.2f), new Vector2(1.5f, 0.34f), "#8A5A33", 0), (float)random.NextDouble() * 20f - 10f);
            });

            Scatter(CityGroup(parent, "Mushrooms"), random, 14, WholeMap, OnOpenGround, 1f, (g, x, y) =>
            {
                var root = Stand(g, "Mushroom", x, y);
                Part(root, roundedSmall, new Vector2(0f, 0.1f), new Vector2(0.1f, 0.2f), "#F8F5E8", 0);
                Part(root, circle, new Vector2(0f, 0.24f), new Vector2(0.3f, 0.2f), random.NextDouble() < 0.5 ? "#E8505B" : "#B07A45", 1);
                Shape(root, "Dot", circle, new Vector2(0.05f, 0.28f), Vector2.one * 0.05f, Hex("#F8F5E8"), 6);
            });

            Scatter(CityGroup(parent, "Ferns"), random, 22, new Rect(-46f, 13.5f, 92f, 3.6f), (x, y) => OnOpenGrass(x, y), 1.2f, (g, x, y) =>
            {
                var root = Stand(g, "Fern", x, y);
                foreach (float angle in new[] { -50f, -25f, 0f, 25f, 50f })
                {
                    Tilt(Shape(root, "Frond", roundedSmall, new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.2f, 0.28f), new Vector2(0.1f, 0.56f), Hex("#2F8A58"), 0), -angle);
                }
            });

            Scatter(CityGroup(parent, "BerryBushes"), random, 10, WholeMap, OnOpenGround, 2.5f, (g, x, y) =>
            {
                Bush(g, cityShadows, x, y, 0.9f);
                var root = Stand(g, "Berries", x, y);
                foreach (var spot in new[] { new Vector2(-0.2f, 0.5f), new Vector2(0.15f, 0.62f), new Vector2(0.05f, 0.38f) })
                {
                    Shape(root, "Berry", circle, spot, Vector2.one * 0.13f, Hex("#E8505B"), 5);
                }
            });

            Scatter(CityGroup(parent, "Sunflowers"), random, 12, WholeMap, OnOpenGround, 1.4f, (g, x, y) =>
            {
                var root = Stand(g, "Sunflower", x, y);
                Part(root, roundedSmall, new Vector2(0f, 0.4f), new Vector2(0.08f, 0.8f), "#3E9457", 0);
                Part(root, circle, new Vector2(0f, 0.86f), new Vector2(0.46f, 0.46f), "#FFD23F", 1);
                Shape(root, "Heart", circle, new Vector2(0f, 0.86f), Vector2.one * 0.2f, Hex("#7A4F2A"), 6);
            });

            var meadow = CityGroup(parent, "Wildflowers");
            string[] colors = { "#FF7A8A", "#FFD23F", "#F8F5E8", "#B28DFF", "#FF9F43" };
            Scatter(meadow, random, 140, WholeMap, OnOpenGround, 0.5f, (g, x, y) =>
                Flat(g, "Wildflower", circle, new Vector2(x, y), Vector2.one * (0.1f + (float)random.NextDouble() * 0.08f), Hex(colors[random.Next(colors.Length)]), TownGroundDetail + 1));

            Scatter(CityGroup(parent, "DirtPatches"), random, 14, WholeMap, OnOpenGround, 1f, (g, x, y) =>
                Flat(g, "Dirt", softCircle, new Vector2(x, y), new Vector2(2.2f, 1.2f), Hex("#A5794B", 0.4f), TownGrassPatch + 1));

            Scatter(CityGroup(parent, "GrassPuddles"), random, 8, WholeMap, OnOpenGround, 1f, (g, x, y) =>
            {
                Flat(g, "Puddle", softCircle, new Vector2(x, y), new Vector2(1.4f, 0.7f), Hex("#7FB6E8", 0.5f), TownGrassPatch + 2);
                Flat(g, "Shine", softCircle, new Vector2(x - 0.15f, y + 0.06f), new Vector2(0.5f, 0.18f), Hex("#FFFFFF", 0.5f), TownGrassPatch + 3);
            });

            Scatter(CityGroup(parent, "BoulderClusters"), random, 8, WholeMap, OnOpenGround, 3f, (g, x, y) =>
            {
                Rock(g, cityShadows, x, y, 0.9f);
                Rock(g, cityShadows, x + 0.8f, y - 0.2f, 0.6f);
                Rock(g, cityShadows, x - 0.6f, y - 0.3f, 0.45f);
            });
        }

        private static void BuildWaterAndBeachLife(Transform parent, System.Random random)
        {
            var stones = CityGroup(parent, "RiverStones");
            for (int i = 0; i < 36; i++)
            {
                float x = MapLayout.RiverXMin + 0.3f + (float)random.NextDouble() * (MapLayout.RiverXMax - MapLayout.RiverXMin - 0.6f);
                float y = -26f + (float)random.NextDouble() * 50f;
                Flat(stones, "Stone", circle, new Vector2(x, y), new Vector2(0.26f, 0.18f), Hex("#6F8FA8", 0.8f), 9);
            }

            Scatter(CityGroup(parent, "Frogs"), random, 6, WholeMap, (x, y) => (x > 26.8f && x < 27.8f || x > 31.2f && x < 32.2f) && y > -25f && y < 22f, 2f, (g, x, y) =>
            {
                var root = Stand(g, "Frog", x, y);
                Part(root, circle, new Vector2(0f, 0.12f), new Vector2(0.3f, 0.22f), "#55B76A", 0);
                Shape(root, "Eye", circle, new Vector2(-0.07f, 0.24f), Vector2.one * 0.07f, Hex("#F8F5E8"), 5);
                Shape(root, "Eye", circle, new Vector2(0.07f, 0.24f), Vector2.one * 0.07f, Hex("#F8F5E8"), 5);
            });

            var ducks = CityGroup(parent, "Ducks");
            for (int i = 0; i < 6; i++)
            {
                float x = MapLayout.RiverXMin + 0.8f + (float)random.NextDouble() * 1.4f;
                float y = -20f + i * 7f + (float)random.NextDouble() * 2f;
                var root = Stand(ducks, "Duck", x, y);
                Part(root, circle, new Vector2(0f, 0.14f), new Vector2(0.44f, 0.3f), "#F8F5E8", 0);
                Part(root, circle, new Vector2(0.18f, 0.28f), new Vector2(0.22f, 0.2f), "#F8F5E8", 1);
                Shape(root, "Beak", roundedSmall, new Vector2(0.32f, 0.27f), new Vector2(0.12f, 0.07f), Hex("#FF9F43"), 6);

                // The ducks swim up and down the river, some each way, slowly.
                bool north = i % 2 == 0;
                float speed = 0.25f + (float)random.NextDouble() * 0.2f;
                root.gameObject.AddComponent<Drift>().Configure(new Vector2(0f, north ? speed : -speed), new Vector2(60f, 24f));
                root.localRotation = Quaternion.Euler(0f, 0f, north ? 90f : -90f);
            }

            var fish = CityGroup(parent, "FishShadows");
            for (int i = 0; i < 14; i++)
            {
                float x = (float)random.NextDouble() * 92f - 46f;
                float y = -29.5f - (float)random.NextDouble() * 4f;
                var shadow = Flat(fish, "Fish", softCircle, new Vector2(x, y), new Vector2(0.9f, 0.32f), Hex("#1D4F7A", 0.4f), 6);
                shadow.gameObject.AddComponent<Drift>().Configure(new Vector2(0.4f + (float)random.NextDouble() * 0.4f, 0f), new Vector2(46f, 40f));
            }

            Scatter(CityGroup(parent, "Seagulls"), random, 9, new Rect(-46f, -27f, 92f, 8f), (x, y) => OnSand(x, y), 2.5f, (g, x, y) =>
            {
                var root = Stand(g, "Seagull", x, y);
                Blob(x, y, 0.6f, 0.15f);
                Part(root, circle, new Vector2(0f, 0.16f), new Vector2(0.4f, 0.26f), "#F8F5E8", 0);
                Part(root, circle, new Vector2(0.2f, 0.28f), new Vector2(0.2f, 0.18f), "#F8F5E8", 1);
                Shape(root, "Beak", roundedSmall, new Vector2(0.34f, 0.27f), new Vector2(0.12f, 0.06f), Hex("#FFD23F"), 6);
                Shape(root, "Wing", circle, new Vector2(-0.06f, 0.18f), new Vector2(0.28f, 0.14f), Hex("#9AA3B8"), 5);
            });

            Scatter(CityGroup(parent, "Crabs"), random, 8, new Rect(-46f, -27f, 92f, 8f), (x, y) => OnSand(x, y), 2f, (g, x, y) =>
            {
                var root = Stand(g, "Crab", x, y);
                Part(root, circle, new Vector2(0f, 0.1f), new Vector2(0.28f, 0.18f), "#E8505B", 0);
                Shape(root, "Claw", circle, new Vector2(-0.2f, 0.18f), Vector2.one * 0.1f, Hex("#E8505B"), 5);
                Shape(root, "Claw", circle, new Vector2(0.2f, 0.18f), Vector2.one * 0.1f, Hex("#E8505B"), 5);
            });

            var weed = CityGroup(parent, "ShoreSeaweed");
            for (int i = 0; i < 26; i++)
            {
                float x = (float)random.NextDouble() * 92f - 46f;
                Flat(weed, "Weed", circle, new Vector2(x, -26.9f + (float)random.NextDouble() * 0.3f), new Vector2(0.5f, 0.2f), Hex("#2F6B4A", 0.8f), WorldSand + 2);
            }

            var ripples = CityGroup(parent, "SandRipples");
            for (int i = 0; i < 46; i++)
            {
                float x = (float)random.NextDouble() * 92f - 46f;
                float y = -19.8f - (float)random.NextDouble() * 6.5f;
                Flat(ripples, "Ripple", ring, new Vector2(x, y), new Vector2(1.1f, 0.22f), Hex("#D9C48A", 0.55f), WorldSand + 2);
            }

            Scatter(CityGroup(parent, "BeachToys"), random, 8, new Rect(-46f, -27f, 92f, 8f), (x, y) => OnSand(x, y), 3f, (g, x, y) =>
            {
                var root = Stand(g, "Toy", x, y);
                Blob(x, y, 0.9f);
                if (random.NextDouble() < 0.5)
                {
                    Part(root, circle, new Vector2(0f, 0.28f), new Vector2(0.5f, 0.5f), "#FF7A8A", 0);
                    Shape(root, "Stripe", square, new Vector2(0f, 0.28f), new Vector2(0.5f, 0.1f), Hex("#F8F5E8"), 5);
                }
                else
                {
                    Part(root, roundedSmall, new Vector2(0f, 0.16f), new Vector2(0.34f, 0.3f), "#4DA3FF", 0);
                    Shape(root, "Handle", ring, new Vector2(0f, 0.34f), new Vector2(0.3f, 0.2f), Hex("#2A5FA8"), 5);
                }
            });

            Scatter(CityGroup(parent, "Driftwood"), random, 6, new Rect(-46f, -27f, 92f, 8f), (x, y) => OnSand(x, y), 3f, (g, x, y) =>
            {
                var root = Stand(g, "Wood", x, y);
                Tilt(Part(root, roundedSmall, new Vector2(0f, 0.12f), new Vector2(1.2f, 0.2f), "#B08A5A", 0), (float)random.NextDouble() * 30f - 15f);
            });

            var buoys = CityGroup(parent, "Buoys");
            for (int i = 0; i < 8; i++)
            {
                float x = -42f + i * 11f + (float)random.NextDouble() * 4f;
                float y = -31f - (float)random.NextDouble() * 2.5f;
                if (x > MapLayout.PierXMin - 2f && x < MapLayout.PierXMax + 2f)
                {
                    continue;
                }

                Flat(buoys, "Buoy", circle, new Vector2(x, y), new Vector2(0.44f, 0.44f), GamePalette.Outline, 8);
                var top = Flat(buoys, "Buoy", circle, new Vector2(x, y), new Vector2(0.34f, 0.34f), Hex(i % 2 == 0 ? "#E8505B" : "#F8F5E8"), 9);
                Flickering(top, 0.1f, 0.6f, i);
            }

            var boats = CityGroup(parent, "FarBoats");
            for (int i = 0; i < 3; i++)
            {
                var boat = Group(boats, "Boat");
                boat.localPosition = new Vector2(-30f + i * 28f, -33f);
                string previous = currentLayer;
                currentLayer = LayerFloor;
                Shape(boat, "Hull", rounded, Vector2.zero, new Vector2(1.8f, 0.5f), Hex("#B2475A"), 10);
                Shape(boat, "Mast", square, new Vector2(0f, 0.6f), new Vector2(0.05f, 1.1f), Hex("#7A4F2A"), 10);
                Shape(boat, "Sail", square, new Vector2(0.25f, 0.65f), new Vector2(0.5f, 0.8f), Hex("#F8F5E8"), 10);
                currentLayer = previous;
                boat.gameObject.AddComponent<Drift>().Configure(new Vector2(0.35f + i * 0.1f, 0f), new Vector2(46f, 40f));
            }
        }

        // ---------------------------------------------------------------- air

        private static void BuildSkyDetails(Transform parent, System.Random random)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;

            var glare = CityGroup(parent, "SunGlare");
            Shape(glare, "Glare", softCircle, new Vector2(-30f, 26f), Vector2.one * 60f, Hex("#FFF1B8", 0.1f), 5);
            Shape(glare, "Core", softCircle, new Vector2(-30f, 26f), Vector2.one * 24f, Hex("#FFFFFF", 0.08f), 5);

            var butterflies = ParallaxGroup(CityGroup(parent, "Butterflies"), "Flock", -0.1f);
            string[] wings = { "#FF9F43", "#FFD23F", "#B28DFF", "#FF7A8A" };
            for (int i = 0; i < 14; i++)
            {
                var butterfly = Group(butterflies, "Butterfly");
                butterfly.localPosition = new Vector2((float)random.NextDouble() * 120f - 60f, (float)random.NextDouble() * 70f - 35f);
                string tone = wings[random.Next(wings.Length)];
                Shape(butterfly, "Wing", circle, new Vector2(-0.07f, 0.02f), new Vector2(0.14f, 0.2f), Hex(tone), 6);
                Shape(butterfly, "Wing", circle, new Vector2(0.07f, 0.02f), new Vector2(0.14f, 0.2f), Hex(tone), 6);
                Flickering(Shape(butterfly, "Body", square, Vector2.zero, new Vector2(0.03f, 0.18f), Hex("#2A3345"), 7), 0.4f, 6f, i);
                butterfly.gameObject.AddComponent<Drift>().Configure(new Vector2(0.25f + (float)random.NextDouble() * 0.3f, 0.1f * ((i % 2) * 2 - 1)), new Vector2(60f, 35f));
            }

            var dragonflies = CityGroup(parent, "Dragonflies");
            for (int i = 0; i < 6; i++)
            {
                var fly = Group(dragonflies, "Dragonfly");
                fly.localPosition = new Vector2(MapLayout.RiverXMin + 1.5f + (float)random.NextDouble() - 0.5f, -20f + i * 8f);
                Shape(fly, "Body", roundedSmall, Vector2.zero, new Vector2(0.28f, 0.05f), Hex("#2E8BC0"), 6);
                Shape(fly, "Wing", circle, new Vector2(-0.02f, 0.05f), new Vector2(0.2f, 0.07f), Hex("#CFE8FF", 0.7f), 6);
                Flickering(Shape(fly, "Wing", circle, new Vector2(-0.02f, -0.05f), new Vector2(0.2f, 0.07f), Hex("#CFE8FF", 0.7f), 6), 0.6f, 8f, i);
            }

            var seeds = ParallaxGroup(CityGroup(parent, "DandelionSeeds"), "Seeds", -0.35f);
            for (int i = 0; i < 60; i++)
            {
                var seed = Shape(seeds, "Seed", softCircle, new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 112f - 56f), Vector2.one * 0.22f, Hex("#FFFFFF", 0.7f), 6);
                seed.gameObject.AddComponent<Drift>().Configure(new Vector2(0.3f + (float)random.NextDouble() * 0.3f, 0.12f), new Vector2(90f, 56f));
            }

            var wisps = ParallaxGroup(CityGroup(parent, "CloudWisps"), "Wisps", -0.55f);
            for (int i = 0; i < 10; i++)
            {
                var wisp = Shape(wisps, "Wisp", softCircle, new Vector2((float)random.NextDouble() * 180f - 90f, (float)random.NextDouble() * 112f - 56f), new Vector2(12f + (float)random.NextDouble() * 8f, 1.2f), Hex("#FFFFFF", 0.13f), 9);
                wisp.gameObject.AddComponent<Drift>().Configure(new Vector2(1.1f + (float)random.NextDouble() * 0.6f, 0f), new Vector2(90f, 56f));
            }

            currentLayer = previous;
        }
    }
}

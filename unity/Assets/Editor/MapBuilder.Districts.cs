using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The two districts added around the original town, which bring the playable area to about twice its first
    /// size: the north district (an avenue with the real estate agency, the car agency and a new neighborhood,
    /// behind the forest) and the east district (the farm, a pasture and the beach, past the river). The original
    /// town is not touched: its streets simply run on, and the invisible walls moved out to the new rock faces.
    /// This file draws the ground; <c>MapBuilder.Districts.Props.cs</c> stands the buildings, places and people.
    /// </summary>
    public static partial class MapBuilder
    {
        private const float AvenueRoadY = MapLayout.AvenueY - 2f;

        // The streets of the original town end at x = 50 (in the fog); the east district carries them on.
        private const float EastStreetsFrom = 48f;
        private const float EastStreetsLength = 24f;

        private static void BuildDistricts(Transform map, Transform shadows)
        {
            var random = new System.Random(23);

            currentLayer = LayerFloor;
            BuildDistrictGround(Group(map, "DistrictGround"), random);
            currentLayer = LayerWorld;

            BuildNorthDistrictProps(Group(map, "NorthDistrict"), shadows, random);
            BuildEastDistrictProps(Group(map, "EastDistrict"), shadows, random);
            BuildDistrictEdges(Group(map, "DistrictEdges"), shadows, random);
            BuildMarketStations(Group(map, "MarketStations"));
            BuildMarketPeople(Group(map, "MarketPeople"));
        }

        // ---------------------------------------------------------------- ground

        private static void BuildDistrictGround(Transform parent, System.Random random)
        {
            BuildDistrictGrass(parent, random);
            BuildNorthStreets(parent);
            BuildEastStreets(parent);
            BuildNorthPath(parent);
            BuildPasture(parent);

            // The east side ends in a rock face that runs from the sea to the cliff of the north.
            float bottom = -30f;
            CliffBlock(parent, new Rect(MapLayout.EastCliffX, bottom, MapLayout.TownBounds.xMax - MapLayout.EastCliffX + 0.5f, MapLayout.WorldNorthEdgeY + 5.5f - bottom), 1, random);
        }

        /// <summary>Soft darker patches and small tufts over the new grass, like the ones of the original town.</summary>
        private static void BuildDistrictGrass(Transform parent, System.Random random)
        {
            var patch = GamePalette.Hex("#58A867");
            patch.a = 0.45f;
            var tuft = GamePalette.Hex("#58A867");

            foreach (var area in new[] { NorthGrass, EastGrass })
            {
                int patches = Mathf.RoundToInt(area.width * area.height / 70f);
                for (int i = 0; i < patches; i++)
                {
                    float x = area.xMin + (float)random.NextDouble() * area.width;
                    float y = area.yMin + (float)random.NextDouble() * area.height;
                    float size = 6f + (float)random.NextDouble() * 7f;
                    Shape(parent, "Patch", softCircle, new Vector2(x, y), new Vector2(size, size * 0.7f), patch, TownGrassPatch);
                }

                int tufts = Mathf.RoundToInt(area.width * area.height / 12f);
                for (int i = 0; i < tufts; i++)
                {
                    float x = area.xMin + (float)random.NextDouble() * area.width;
                    float y = area.yMin + (float)random.NextDouble() * area.height;
                    if (OnPavement(x, y, 0.4f) || (area.yMin < -19f && y < -19f))
                    {
                        continue;
                    }

                    Shape(parent, "Tuft", circle, new Vector2(x, y), new Vector2(0.34f, 0.2f), tuft, TownGroundDetail);
                }
            }

            Flowers(parent, random, new Rect(-44f, 40.5f, 108f, 1.5f), 40);
            Flowers(parent, random, new Rect(44f, 13f, 19f, 4f), 14);
        }

        /// <summary>The grass the player can reach in each district, kept out of the roads and the beach by the callers.</summary>
        private static readonly Rect NorthGrass = new Rect(-44f, 17f, 108f, 25f);
        private static readonly Rect EastGrass = new Rect(43f, -27f, 21f, 44f);

        private static void BuildNorthStreets(Transform parent)
        {
            // Sidewalks first, then the road on top so its edge reads as a curb. The avenue runs past both edges.
            Walk(parent, new Rect(-50f, AvenueRoadY - 2f, 124f, 2f));
            Walk(parent, new Rect(-50f, AvenueRoadY + 4f, 124f, NorthSidewalkY - (AvenueRoadY + 4f)));
            Road(parent, new Rect(-50f, AvenueRoadY, 124f, 4f));
            DashedLine(parent, MapLayout.AvenueY, -49f, 73f);

            // Where the dirt path from the first street reaches the avenue.
            Crosswalk(parent, MapLayout.NorthPathX, AvenueRoadY, AvenueRoadY + 4f);
        }

        private static void BuildEastStreets(Transform parent)
        {
            float from = EastStreetsFrom;
            float length = EastStreetsLength;
            Walk(parent, new Rect(from, 4f, length, 2f));
            Walk(parent, new Rect(from, -2f, length, 2f));
            Walk(parent, new Rect(from, -10f, length, 2f));
            Walk(parent, new Rect(from, -16f, length, 2f));
            Road(parent, new Rect(from, 0f, length, 4f));
            Road(parent, new Rect(from, -14f, length, 4f));
            DashedLine(parent, 2f, from, from + length + 1f);
            DashedLine(parent, -12f, from, from + length + 1f);
            Crosswalk(parent, 62f, 0f, 4f);
        }

        /// <summary>A dirt path from the first street, between the garage and the river, up to the avenue.</summary>
        private static void BuildNorthPath(Transform parent)
        {
            var path = GamePalette.Hex("#E8D5A3");
            float yMin = 5.75f;
            float yMax = AvenueRoadY - 2f + 0.6f;
            var center = new Vector2(MapLayout.NorthPathX, (yMin + yMax) * 0.5f);
            var size = new Vector2(2.5f, yMax - yMin);
            Shape(parent, "NorthPath_Outline", rounded, center, size + Vector2.one * 0.2f, GamePalette.Shade(path, 0.5f), 5);
            Shape(parent, "NorthPath", rounded, center, size, path, 6);
            for (float y = yMin + 1f; y < yMax - 0.5f; y += 1.3f)
            {
                Shape(parent, "NorthPath_Stone", circle, new Vector2(MapLayout.NorthPathX + ((y * 7f) % 1f - 0.5f) * 1.2f, y), new Vector2(0.3f, 0.2f), GamePalette.Shade(path, 0.82f), 7);
            }
        }

        private static readonly Rect PastureArea = new Rect(47f, -7.2f, 16f, 4.4f);

        /// <summary>Greener, tended grass inside the fence of the farm.</summary>
        private static void BuildPasture(Transform parent)
        {
            var grass = GamePalette.Hex("#7ACB86");
            Shape(parent, "Pasture", rounded, PastureArea.center, PastureArea.size, grass, TownPark);
            var trough = GamePalette.Hex("#B8D9A0");
            Shape(parent, "Pasture_Patch", softCircle, PastureArea.center + new Vector2(-3f, 0.3f), new Vector2(6f, 2.6f), trough, TownPark + 1);
            Shape(parent, "Pasture_Patch", softCircle, PastureArea.center + new Vector2(4f, -0.4f), new Vector2(5f, 2.2f), trough, TownPark + 1);
        }

        /// <summary>A road or sidewalk rectangle of the districts counts as paved, so no tree grows on it.</summary>
        private static readonly Rect[] DistrictPavedAreas =
        {
            new Rect(-50f, 24f, 124f, 9f),
            new Rect(23.9f, 5.75f, 4.8f, 19f),
            new Rect(48f, 0f, 24f, 6f),
            new Rect(48f, -16f, 24f, 8f)
        };
    }
}

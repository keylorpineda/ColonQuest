using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Everything around the town core that turns it into an open world: the mountains and
    /// volcano in the north, the river and the fields in the east, the neighborhood in the west
    /// and the beach and sea in the south. Every edge is a natural barrier, never a hedge.
    /// </summary>
    public static partial class MapBuilder
    {
        private const int WorldSand = 3;
        private const int WorldSea = 4;
        private const int WorldFoam = 5;
        private const int WorldRiver = 6;
        private const int WorldRiverFoam = 8;
        private const int WorldCliff = 20;

        // Where the north avenue's north sidewalk ends (the buildings stand on it) and the waterfall's pool.
        private const float NorthSidewalkY = 33f;
        private const float PoolY = 21.2f + MapLayout.NorthShift;

        private static readonly Color SeaColor = GamePalette.Hex("#4FB6E0");
        private static readonly Color SandColor = GamePalette.Hex("#F2DFA9");
        private static readonly Color CliffFace = GamePalette.Hex("#8C7B6E");
        private static readonly Color CliffDark = GamePalette.Hex("#6B5B50");
        private static readonly Color WoodColor = GamePalette.Hex("#B07A45");
        private static readonly Color WoodDark = GamePalette.Hex("#7A4F2A");

        private static void Tilt(SpriteRenderer target, float degrees)
        {
            target.transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>True where the player can stand on plain grass, so grass details do not end up in water or sand.</summary>
        private static bool OnOpenGrass(float x, float y)
        {
            bool inRiver = x > MapLayout.RiverXMin - 0.5f && x < MapLayout.RiverXMax + 0.5f;
            bool onBeach = y < -19f && x > -27f;
            return !inRiver && !onBeach && y < MapLayout.NorthEdgeY && y > MapLayout.ShoreY && Mathf.Abs(x) < 45f && !OnPavement(x, y, 0.4f);
        }

        /// <summary>
        /// Ground that is built or wet: the roads, the plaza, the alley, the gravel lot, the park path, the
        /// pond and the fountain. Nothing green grows there, so plants never end up on an asphalt lane.
        /// </summary>
        private static bool OnPavement(float x, float y, float margin)
        {
            var p = new Vector2(x, y);
            foreach (var area in PavedAreas)
            {
                var grown = new Rect(area.xMin - margin, area.yMin - margin, area.width + margin * 2f, area.height + margin * 2f);
                if (grown.Contains(p))
                {
                    return true;
                }
            }

            foreach (var area in DistrictPavedAreas)
            {
                var grown = new Rect(area.xMin - margin, area.yMin - margin, area.width + margin * 2f, area.height + margin * 2f);
                if (grown.Contains(p))
                {
                    return true;
                }
            }

            return Vector2.Distance(p, new Vector2(0f, 9f)) < 2.4f + margin;
        }

        private static readonly Rect[] PavedAreas =
        {
            new Rect(-50f, 0f, 100f, 4f),
            new Rect(-50f, -14f, 100f, 4f),
            new Rect(-2f, -10f, 4f, 10f),
            new Rect(-4f, 4f, 8f, 9f),
            new Rect(-16f, -8f, 4f, 6f),
            new Rect(4f, -7.4f, 6f, 4.8f),
            new Rect(14.5f, -4.12f, 9.5f, 1.14f),
            new Rect(16.7f, -7.2f, 5.6f, 2.6f)
        };

        /// <summary>True under the walls of a building. Behind its roof line (north of it) is still fine.</summary>
        private static bool InsideBuilding(float x, float y)
        {
            foreach (var b in buildingSpecs)
            {
                if (x > b.XMin - 0.3f && x < b.XMax + 0.3f && y > b.YBottom - 0.3f && y < b.YTop)
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- ground

        /// <summary>The flat things of the world: sea, sand, river, cliffs, mountains, paths and fields.</summary>
        private static void BuildWorldGround(Transform parent, System.Random random)
        {
            BuildSouthGround(parent, random);
            BuildRiver(parent);
            BuildNorthGround(parent, random);
            BuildFootballField(parent);
        }

        private static void BuildSouthGround(Transform parent, System.Random random)
        {
            // Sand, a darker wet strip along the water, then the sea on top of both.
            Shape(parent, "Sand", rounded, new Vector2(20.5f, -23.1f), new Vector2(95f, 8.2f), SandColor, WorldSand);
            var wet = GamePalette.Shade(SandColor, 0.9f);
            Shape(parent, "WetSand", rounded, new Vector2(20.5f, -26.6f), new Vector2(95f, 1.2f), wet, WorldSand + 1);
            Shape(parent, "Sea", rounded, new Vector2(15.5f, -31f), new Vector2(129f, 7.2f), SeaColor, WorldSea);

            var foam = GamePalette.White;
            foam.a = 0.7f;
            Shape(parent, "Shore_Foam", roundedSmall, new Vector2(13f, -27.45f), new Vector2(118f, 0.4f), foam, WorldFoam);
            for (int i = 0; i < 16; i++)
            {
                float x = -42f + i * 5.6f + (float)random.NextDouble() * 2f;
                float y = -28.6f - (float)random.NextDouble() * 4.2f;
                var ripple = GamePalette.White;
                ripple.a = 0.5f;
                Flickering(Shape(parent, "Wave", ring, new Vector2(x, y), new Vector2(1.8f, 0.55f), ripple, WorldFoam), 0.5f, 0.9f + (float)random.NextDouble(), x);
            }

            // Pier.
            var pier = new Vector2(5.6f, -30.35f);
            Shape(parent, "Pier_Outline", rounded, pier, new Vector2(3.4f, 6.4f), GamePalette.Outline, 8);
            Shape(parent, "Pier", rounded, pier, new Vector2(3.2f, 6.2f), WoodColor, 9);
            if (woodPlankTex != null)
            {
                TiledShape(parent, "Pier_Planks", woodPlankTex, pier, new Vector2(3.05f, 6.0f), WoodColor, 9);
            }
            for (float y = -27.8f; y > -33.2f; y -= 0.7f)
            {
                Shape(parent, "Plank", square, new Vector2(pier.x, y), new Vector2(3.1f, 0.06f), WoodDark, 10);
            }

            foreach (float y in new[] { -28.4f, -30.6f, -32.8f })
            {
                Shape(parent, "Pier_Post", circle, new Vector2(3.9f, y), Vector2.one * 0.34f, WoodDark, 11);
                Shape(parent, "Pier_Post", circle, new Vector2(7.3f, y), Vector2.one * 0.34f, WoodDark, 11);
            }

            // Boardwalk between the street and the sand.
            Shape(parent, "Boardwalk_Outline", rounded, new Vector2(20.5f, -18.3f), new Vector2(95f, 1.5f), GamePalette.Outline, 8);
            Shape(parent, "Boardwalk", rounded, new Vector2(20.5f, -18.3f), new Vector2(94.8f, 1.3f), WoodColor, 9);
            if (woodPlankTex != null)
            {
                TiledShape(parent, "Boardwalk_Planks", woodPlankTex, new Vector2(20.5f, -18.3f), new Vector2(94.4f, 1.18f), WoodColor, 9);
            }
            for (float x = -25f; x < 67f; x += 1.9f)
            {
                Shape(parent, "Plank", square, new Vector2(x, -18.3f), new Vector2(0.05f, 1.2f), WoodDark, 10);
            }

            // Sand ripples and shells.
            var shell = GamePalette.Shade(SandColor, 0.82f);
            for (int i = 0; i < 28; i++)
            {
                float x = -24f + (float)random.NextDouble() * 66f;
                float y = -26f + (float)random.NextDouble() * 6.2f;
                Shape(parent, "Pebble", circle, new Vector2(x, y), new Vector2(0.32f, 0.2f), shell, WorldSand + 1);
            }
        }

        private static void BuildRiver(Transform parent)
        {
            float x = (MapLayout.RiverXMin + MapLayout.RiverXMax) * 0.5f;
            float width = MapLayout.RiverXMax - MapLayout.RiverXMin;
            float top = MapLayout.WorldNorthEdgeY + 0.2f;
            float bottom = -28.6f;
            var center = new Vector2(x, (top + bottom) * 0.5f);

            Shape(parent, "River_Outline", rounded, center, new Vector2(width + 0.34f, top - bottom), GamePalette.Outline, WorldRiver);
            Shape(parent, "River", rounded, center, new Vector2(width, top - bottom), SeaColor, WorldRiver + 1);

            var ripple = GamePalette.White;
            ripple.a = 0.5f;
            foreach (float y in new[] { 10f, 14.5f, 18f, 21f, 27f, 35f, 38f, 44f, -5f, -20f, -24f })
            {
                Flickering(Shape(parent, "River_Ripple", ring, new Vector2(x + (y % 2f - 1f) * 0.5f, y), new Vector2(1.2f, 0.45f), ripple, WorldRiverFoam), 0.5f, 1.1f, y);
            }

            // Two bridges, on the streets: railings and posts on the street edges.
            Bridge(parent, x, MapLayout.BridgeFirstStreet.yMax + MapLayout.RailingHalf, MapLayout.BridgeFirstStreet.yMin - MapLayout.RailingHalf);
            Bridge(parent, x, MapLayout.BridgeSouthStreet.yMax + MapLayout.RailingHalf, MapLayout.BridgeSouthStreet.yMin - MapLayout.RailingHalf);
            Bridge(parent, x, MapLayout.BridgeAvenue.yMax + MapLayout.RailingHalf, MapLayout.BridgeAvenue.yMin - MapLayout.RailingHalf);
            Bridge(parent, x, -17.45f, -19.15f);

            // Plunge pool under the waterfall, at the foot of the cliff.
            Shape(parent, "Pool_Outline", rounded, new Vector2(20.3f, PoolY), new Vector2(5.8f, 2.6f), GamePalette.Outline, 5);
            Shape(parent, "Pool", rounded, new Vector2(20.3f, PoolY), new Vector2(5.5f, 2.3f), SeaColor, 6);
            Flickering(Shape(parent, "Pool_Ripple", ring, new Vector2(19.4f, PoolY), new Vector2(1.5f, 0.7f), ripple, 7), 0.5f, 1.3f, 3f);
            Flickering(Shape(parent, "Pool_Ripple", ring, new Vector2(21.6f, PoolY - 0.2f), new Vector2(1.1f, 0.55f), ripple, 7), 0.5f, 1.7f, 8f);
        }

        private static void Bridge(Transform parent, float x, float yNorth, float ySouth)
        {
            foreach (float y in new[] { yNorth, ySouth })
            {
                Shape(parent, "Bridge_Rail_Outline", roundedSmall, new Vector2(x, y), new Vector2(4.4f, 0.4f), GamePalette.Outline, 16);
                Shape(parent, "Bridge_Rail", roundedSmall, new Vector2(x, y), new Vector2(4.2f, 0.26f), WoodColor, 17);
                foreach (float dx in new[] { -2f, 0f, 2f })
                {
                    Shape(parent, "Bridge_Post", circle, new Vector2(x + dx, y), Vector2.one * 0.42f, WoodDark, 18);
                }
            }
        }

        private static void BuildNorthGround(Transform parent, System.Random random)
        {
            // Everything here was drawn for the original town; the group moves it north of the new district.
            var north = Group(parent, "NorthMass");
            north.localPosition = new Vector3(0f, MapLayout.NorthShift, 0f);

            // Far mountains, then the volcano, behind the cliff face.
            var farBlue = GamePalette.Hex("#7C8FB0");
            var nearBlue = GamePalette.Hex("#8DA0C0");
            Mountain(north, -36f, 22f, 4.4f, farBlue);
            Mountain(north, -12f, 18f, 3.8f, nearBlue);
            Mountain(north, 38f, 20f, 4.3f, farBlue);
            Mountain(north, 20f, 14f, 3.2f, nearBlue);
            Mountain(north, 54f, 18f, 3.7f, nearBlue);
            Mountain(north, 68f, 14f, 3.1f, farBlue);

            var volcano = GamePalette.Hex("#6E5A66");
            var cone = Shape(north, "Volcano_Outline", square, new Vector2(-2f, 27.5f), Vector2.one * 6.5f, GamePalette.Outline, WorldSand + 1);
            Tilt(cone, 45f);
            var body = Shape(north, "Volcano", square, new Vector2(-2f, 27.5f), Vector2.one * 6.2f, volcano, WorldSand + 2);
            Tilt(body, 45f);

            var lava = GamePalette.Hex("#FF7A3D");
            Shape(north, "Crater", circle, new Vector2(-2f, 31.55f), new Vector2(1.5f, 0.5f), lava, WorldSand + 3);
            Flickering(Glow(north, "Lava_Glow", new Vector2(-2f, 31.3f), 4.2f, "#FF7A3D", 0.4f, WorldSand + 3), 0.3f, 1.2f, 2f);
            foreach (var streak in new[] { new Vector2(-2.4f, 30.1f), new Vector2(-1.5f, 29.2f), new Vector2(-2.1f, 28.3f) })
            {
                Tilt(Shape(north, "Lava_Streak", roundedSmall, streak, new Vector2(0.18f, 1.2f), lava, WorldSand + 3), streak.x < -2f ? 42f : -42f);
            }

            // The cliff the world stops at, with a waterfall down its face.
            CliffBlock(north, new Rect(-46f, MapLayout.NorthEdgeY, MapLayout.TownBounds.width, 5.5f), 0, random);

            var water = GamePalette.Hex("#BFE6F7");
            for (int i = 0; i < 3; i++)
            {
                Flickering(Shape(north, "Waterfall", roundedSmall, new Vector2(19.3f + i * 1.0f, 25.4f), new Vector2(0.62f, 5.2f), water, WorldCliff + 6), 0.25f, 1.6f + i * 0.4f, i * 3f);
            }

            var mist = GamePalette.White;
            mist.a = 0.55f;
            Flickering(Shape(north, "Waterfall_Mist", softCircle, new Vector2(20.3f, 22.9f), new Vector2(4.6f, 1.8f), mist, WorldCliff + 7), 0.3f, 1.1f, 6f);
        }

        /// <summary>A rounded hill-like mountain with a snowy top. Half is hidden behind the cliff.</summary>
        private static void Mountain(Transform parent, float x, float width, float height, Color color)
        {
            Shape(parent, "Mountain_Outline", circle, new Vector2(x, 27.5f), new Vector2(width + 0.3f, height * 2f + 0.3f), GamePalette.Outline, WorldSand + 1);
            Shape(parent, "Mountain", circle, new Vector2(x, 27.5f), new Vector2(width, height * 2f), color, WorldSand + 2);
            Shape(parent, "Mountain_Shade", circle, new Vector2(x + width * 0.14f, 27.2f), new Vector2(width * 0.7f, height * 1.7f), GamePalette.Shade(color, 0.86f), WorldSand + 3);
            Shape(parent, "Mountain_Snow", circle, new Vector2(x - 0.2f, 27.5f + height * 0.78f), new Vector2(width * 0.2f, height * 0.5f), GamePalette.White, WorldSand + 4);
        }

        /// <summary>A rock face: dark outline, strata lines and round boulders along the edge the player sees.</summary>
        private static void CliffBlock(Transform parent, Rect area, int bumpSide, System.Random random)
        {
            Shape(parent, "Cliff_Outline", rounded, area.center, area.size + Vector2.one * 0.3f, GamePalette.Outline, WorldCliff);
            Shape(parent, "Cliff", rounded, area.center, area.size, CliffFace, WorldCliff + 1);

            bool wide = area.width >= area.height;
            if (wide)
            {
                for (float y = area.yMin + 1.1f; y < area.yMax - 0.3f; y += 1.4f)
                {
                    Shape(parent, "Strata", roundedSmall, new Vector2(area.center.x, y), new Vector2(area.width - 1f, 0.16f), CliffDark, WorldCliff + 2);
                }
            }
            else
            {
                for (float x = area.xMin + 0.7f; x < area.xMax - 0.3f; x += 0.9f)
                {
                    Shape(parent, "Strata", roundedSmall, new Vector2(x, area.center.y), new Vector2(0.14f, area.height - 1f), CliffDark, WorldCliff + 2);
                }
            }

            var rock = GamePalette.Shade(CliffFace, 1.12f);
            float length = wide ? area.width : area.height;
            for (float t = 0.8f; t < length - 0.6f; t += 1.9f + (float)random.NextDouble() * 0.9f)
            {
                float size = 1.5f + (float)random.NextDouble() * 0.8f;
                Vector2 at;
                switch (bumpSide)
                {
                    case 1:
                        at = new Vector2(area.xMin + 0.1f, area.yMin + t);
                        break;
                    case 2:
                        at = new Vector2(area.xMax - 0.1f, area.yMin + t);
                        break;
                    default:
                        at = new Vector2(area.xMin + t, area.yMin + 0.15f);
                        break;
                }

                Shape(parent, "Boulder_Outline", circle, at, Vector2.one * (size + 0.2f), GamePalette.Outline, WorldCliff + 3);
                Shape(parent, "Boulder", circle, at, Vector2.one * size, rock, WorldCliff + 4);
                var shine = GamePalette.White;
                shine.a = 0.25f;
                Shape(parent, "Boulder_Shine", circle, at + new Vector2(-0.2f, 0.25f) * size, Vector2.one * size * 0.35f, shine, WorldCliff + 5);
            }
        }

        private static void BuildFootballField(Transform parent)
        {
            var center = new Vector2(-35f, -23.2f);
            var size = new Vector2(14.4f, 8.2f);
            Shape(parent, "Field_Outline", rounded, center, size + Vector2.one * 0.3f, GamePalette.Outline, 5);
            Shape(parent, "Field", rounded, center, size, GamePalette.Hex("#4FA860"), 6);
            for (int i = 0; i < 6; i += 2)
            {
                Shape(parent, "Field_Stripe", square, new Vector2(center.x - size.x * 0.5f + (i + 0.5f) * size.x / 6f, center.y), new Vector2(size.x / 6f, size.y - 0.3f), GamePalette.Hex("#5BB86C"), 7);
            }

            var line = GamePalette.White;
            line.a = 0.9f;
            Shape(parent, "Line_Mid", square, center, new Vector2(0.1f, size.y - 0.8f), line, 8);
            Shape(parent, "Line_Circle", ring, center, Vector2.one * 2.4f, line, 8);
            Shape(parent, "Line_Top", square, center + new Vector2(0f, size.y * 0.5f - 0.4f), new Vector2(size.x - 0.8f, 0.1f), line, 8);
            Shape(parent, "Line_Bottom", square, center - new Vector2(0f, size.y * 0.5f - 0.4f), new Vector2(size.x - 0.8f, 0.1f), line, 8);
            Shape(parent, "Line_Left", square, center - new Vector2(size.x * 0.5f - 0.4f, 0f), new Vector2(0.1f, size.y - 0.8f), line, 8);
            Shape(parent, "Line_Right", square, center + new Vector2(size.x * 0.5f - 0.4f, 0f), new Vector2(0.1f, size.y - 0.8f), line, 8);

            // Ball and players running within the pitch
            Shape(parent, "SoccerBall", circle, new Vector2(-34.6f, -23.0f), Vector2.one * 0.35f, GamePalette.Hex("#F8F5E8"), 9);
            Shape(parent, "SoccerBallSpot", circle, new Vector2(-34.6f, -23.0f), Vector2.one * 0.12f, GamePalette.Hex("#1D2238"), 10);

            var teamRed = new Look { Skin = "#E8505B", Pants = "#FFFFFF", Hair = HairStyle.Buzz, HairColor = "#1D2238" };
            var teamRed2 = new Look { Skin = "#E8505B", Pants = "#FFFFFF", Hair = HairStyle.Side, HairColor = "#555A66" };
            var teamRed3 = new Look { Skin = "#E8505B", Pants = "#FFFFFF", Hair = HairStyle.Spiky, HairColor = "#2A1810" };

            var teamBlue = new Look { Skin = "#4DA3FF", Pants = "#1D2238", Hair = HairStyle.Buzz, HairColor = "#2A1810" };
            var teamBlue2 = new Look { Skin = "#4DA3FF", Pants = "#1D2238", Hair = HairStyle.Side, HairColor = "#1D2238" };
            var teamBlue3 = new Look { Skin = "#4DA3FF", Pants = "#1D2238", Hair = HairStyle.Afro, HairColor = "#1D2238" };

            BuildPedestrian(parent, "JugadorRojo1", new Vector2(-34f, -22f), new Vector2(-30.5f, -21.6f), teamRed, 2.2f);
            BuildPedestrian(parent, "JugadorRojo2", new Vector2(-38f, -24.2f), new Vector2(-33.5f, -24.0f), teamRed2, 2.0f);
            BuildPedestrian(parent, "JugadorRojo3", new Vector2(-40f, -23.0f), new Vector2(-37.0f, -23.5f), teamRed3, 1.8f);

            BuildPedestrian(parent, "JugadorAzul1", new Vector2(-36f, -22.4f), new Vector2(-39.0f, -22.0f), teamBlue, 2.2f);
            BuildPedestrian(parent, "JugadorAzul2", new Vector2(-32f, -24.0f), new Vector2(-36.0f, -24.5f), teamBlue2, 2.0f);
            BuildPedestrian(parent, "JugadorAzul3", new Vector2(-29f, -23.0f), new Vector2(-32.0f, -23.4f), teamBlue3, 1.8f);
        }

        // ---------------------------------------------------------------- borders

        /// <summary>Invisible colliders lined up with the water, the river and the rock faces.</summary>
        private static void BuildWorldBorders(Transform parent)
        {
            var bounds = MapLayout.TownBounds;
            Wall(parent, "North_Wall", new Vector2(bounds.center.x, MapLayout.PlayNorthY + 0.5f), new Vector2(bounds.width, 1f));
            Wall(parent, "West_Wall", new Vector2(MapLayout.PlayWestX - 0.5f, bounds.center.y), new Vector2(1f, bounds.height));
            Wall(parent, "East_Wall", new Vector2(MapLayout.PlayEastX + 0.5f, bounds.center.y), new Vector2(1f, bounds.height));

            // The sea stops the player at the shore, except along the pier.
            float shore = MapLayout.ShoreY;
            Wall(parent, "Sea_West", new Vector2((-46f + MapLayout.PierXMin - 0.5f) * 0.5f, shore - 0.5f), new Vector2(46f + MapLayout.PierXMin - 0.5f, 1f));
            Wall(parent, "Sea_East", new Vector2((MapLayout.PierXMax + 0.5f + bounds.xMax) * 0.5f, shore - 0.5f), new Vector2(bounds.xMax - MapLayout.PierXMax - 0.5f, 1f));
            Wall(parent, "Pier_West", new Vector2(MapLayout.PierXMin - 0.5f, -30.35f), new Vector2(1f, 6.3f));
            Wall(parent, "Pier_East", new Vector2(MapLayout.PierXMax + 0.5f, -30.35f), new Vector2(1f, 6.3f));
            Wall(parent, "Pier_End", new Vector2(5.6f, -33.7f), new Vector2(4.2f, 1f));

            // The river, open over the bridges from railing to railing: the street and both sidewalks, so a player walking
            // on the sidewalk crosses too. The railings themselves stay closed.
            float riverX = (MapLayout.RiverXMin + MapLayout.RiverXMax) * 0.5f;
            float riverWidth = MapLayout.RiverXMax - MapLayout.RiverXMin;
            RiverWall(parent, riverX, riverWidth, shore, -19.15f);
            RiverWall(parent, riverX, riverWidth, -17.45f, MapLayout.BridgeSouthStreet.yMin);
            RiverWall(parent, riverX, riverWidth, MapLayout.BridgeSouthStreet.yMax, MapLayout.BridgeFirstStreet.yMin);
            RiverWall(parent, riverX, riverWidth, MapLayout.BridgeFirstStreet.yMax, MapLayout.BridgeAvenue.yMin);
            RiverWall(parent, riverX, riverWidth, MapLayout.BridgeAvenue.yMax, MapLayout.WorldNorthEdgeY);

            Wall(parent, "Pool", new Vector2(20.3f, PoolY), new Vector2(5.2f, 2.0f));

            // The pond of the park: the player walks around it.
            Wall(parent, "Pond", new Vector2(19.5f, -5.9f), new Vector2(5.4f, 2.4f));
        }

        private static void RiverWall(Transform parent, float x, float width, float yMin, float yMax)
        {
            Wall(parent, $"River_{yMin}_{yMax}", new Vector2(x, (yMin + yMax) * 0.5f), new Vector2(width, yMax - yMin));
        }

        // ---------------------------------------------------------------- props

        /// <summary>The standing things of the world: forests, fields, beach and neighborhood.</summary>
        private static void BuildWorldProps(Transform parent, Transform shadows, System.Random random)
        {
            BuildNorthForest(parent, shadows, random);
            BuildNeighborhood(parent, shadows, random);
            BuildEastFarm(parent, shadows, random);
            BuildBeach(parent, shadows, random);
            BuildEastTreeline(parent, shadows);

            foreach (float x in new[] { -36f, -27f, -18f, -9f, 0f, 9f, 18f, 36f, 39.5f })
            {
                Lamp(parent, x, -17.2f);
            }

            Bench(parent, shadows, -20f, -17.1f);
            Bench(parent, shadows, 22f, -17.1f);
        }

        private static void BuildNorthForest(Transform parent, Transform shadows, System.Random random)
        {
            var greens = new[] { "#3FA66A", "#4CB36F", "#3B9660", "#2F8A58" };
            int row = 0;
            for (float y = 15f; y < 23f; y += 2.9f, row++)
            {
                for (float x = -43f + (row % 2) * 2.1f; x < 43f; x += 4.3f)
                {
                    float jx = x + ((float)random.NextDouble() - 0.5f) * 1.6f;
                    float jy = y + ((float)random.NextDouble() - 0.5f) * 1.2f;
                    if (InForestClearing(jx, jy))
                    {
                        continue;
                    }

                    Tree(parent, shadows, jx, jy, 1.0f + (float)random.NextDouble() * 0.35f, greens[random.Next(greens.Length)], random.Next(6) == 0);
                }
            }

            // Boulders and bushes between the trees.
            foreach (var spot in new[] { new Vector2(-8f, 16.5f), new Vector2(9f, 18f), new Vector2(-30f, 19f), new Vector2(34f, 15.5f), new Vector2(-20f, 21.8f), new Vector2(6f, 21.8f) })
            {
                Rock(parent, shadows, spot.x, spot.y, 1f);
            }

            foreach (float x in new[] { -6f, -3.4f, 3.4f, 6f, -14f, 14f, -26f, 26f })
            {
                Bush(parent, shadows, x, 14.4f + (float)random.NextDouble(), 1.1f);
            }

            Flowers(parent, random, new Rect(-44f, 13.6f, 88f, 9f), 60);

            // The trail from the plaza up to the lookout.
            var path = GamePalette.Hex("#E8D5A3");
            OnGround(parent, "Trail_Outline", rounded, new Vector2(0f, 17.2f), new Vector2(2.5f, 8.6f), GamePalette.Shade(path, 0.5f), 5);
            OnGround(parent, "Trail", rounded, new Vector2(0f, 17.2f), new Vector2(2.3f, 8.4f), path, 6);
            for (float y = 14f; y < 21f; y += 1.3f)
            {
                OnGround(parent, "Trail_Stone", circle, new Vector2(((y * 7f) % 1f - 0.5f) * 1.2f, y), new Vector2(0.3f, 0.2f), GamePalette.Shade(path, 0.82f), 7);
            }

            Lookout(parent, shadows);
        }

        private static bool InForestClearing(float x, float y)
        {
            bool trail = Mathf.Abs(x) < 3.6f;
            bool lookout = Mathf.Abs(x) < 5f && y > 18.5f;
            bool pool = x > 16.5f && x < 24.5f && y > 18f;
            bool river = x > MapLayout.RiverXMin - 1f && x < MapLayout.RiverXMax + 1f;
            bool coffee = x > 31.5f;
            bool northPath = Mathf.Abs(x - MapLayout.NorthPathX) < 2.2f;
            return trail || lookout || pool || river || coffee || northPath;
        }

        private static void Lookout(Transform parent, Transform shadows)
        {
            var deck = new Vector2(0f, 21.3f);
            OnGround(parent, "Lookout_Outline", rounded, deck, new Vector2(7f, 2.6f), GamePalette.Outline, 5);
            OnGround(parent, "Lookout", rounded, deck, new Vector2(6.8f, 2.4f), WoodColor, 6);
            for (float x = -3f; x <= 3f; x += 0.8f)
            {
                OnGround(parent, "Plank", square, new Vector2(x, deck.y), new Vector2(0.05f, 2.3f), WoodDark, 7);
            }

            // Railing along the back and a telescope.
            var rail = Root(parent, "Lookout_Rail", new Vector2(0f, 22.3f), 22.3f);
            Shape(rail, "Rail_Outline", roundedSmall, new Vector2(0f, 0.4f), new Vector2(6.8f, 0.4f), GamePalette.Outline, 0);
            Shape(rail, "Rail", roundedSmall, new Vector2(0f, 0.4f), new Vector2(6.6f, 0.26f), WoodColor, 1);
            foreach (float x in new[] { -3.2f, -1.6f, 0f, 1.6f, 3.2f })
            {
                Shape(rail, "Rail_Post", roundedSmall, new Vector2(x, 0.2f), new Vector2(0.2f, 0.9f), WoodDark, 2);
            }
            // Invisible collider along the railing so nobody walks off the deck.
            Wall(rail, "Rail_Collider", new Vector2(0f, 0.3f), new Vector2(6.8f, 0.3f));

            var scope = Root(parent, "Telescope", new Vector2(-1.6f, 20.6f), 20.4f);
            Shape(scope, "Leg_Outline", roundedSmall, new Vector2(0f, 0.4f), new Vector2(0.2f, 0.8f), GamePalette.Outline, 0);
            Shape(scope, "Leg", roundedSmall, new Vector2(0f, 0.4f), new Vector2(0.1f, 0.7f), GamePalette.Hex("#4A5370"), 1);
            var tube = Shape(scope, "Tube_Outline", roundedSmall, new Vector2(0.1f, 0.95f), new Vector2(1.0f, 0.34f), GamePalette.Outline, 2);
            Tilt(tube, 25f);
            var tubeBody = Shape(scope, "Tube", roundedSmall, new Vector2(0.1f, 0.95f), new Vector2(0.9f, 0.24f), GamePalette.Hex("#FFD23F"), 3);
            Tilt(tubeBody, 25f);
            scope.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.4f);

            Signpost(parent, new Vector2(1.9f, 13.9f), "MIRADOR");
        }

        private static void Signpost(Transform parent, Vector2 position, string text)
        {
            var root = Root(parent, "Signpost", position, position.y);
            float length = Mathf.Max(1.85f, text.Length * 0.34f + 0.8f);
            Shape(root, "Pole_Outline", roundedSmall, new Vector2(0f, 0.7f), new Vector2(0.32f, 1.44f), GamePalette.Outline, 0);
            Shape(root, "Pole", roundedSmall, new Vector2(0f, 0.7f), new Vector2(0.2f, 1.34f), WoodDark, 1);
            Shape(root, "Pole_Highlight", roundedSmall, new Vector2(-0.04f, 0.7f), new Vector2(0.06f, 1.26f), WoodColor, 1);

            var boardRig = new GameObject("BoardRig").transform;
            boardRig.SetParent(root, false);
            boardRig.localPosition = new Vector2(0f, 1.45f);
            boardRig.gameObject.AddComponent<FoliagePhysics>().Configure(1.8f, 1.4f, position.x * 0.7f + position.y, 0f, true, 1.3f, 6f);

            var shade = GamePalette.Outline;
            shade.a = 0.32f;
            Shape(boardRig, "Board_Shadow", roundedSmall, new Vector2(0.05f, -0.06f), new Vector2(length + 0.22f, 0.78f), shade, 1);
            Shape(boardRig, "Board_Outline", roundedSmall, Vector2.zero, new Vector2(length + 0.2f, 0.78f), GamePalette.Outline, 2);
            Shape(boardRig, "Board_Frame", roundedSmall, Vector2.zero, new Vector2(length + 0.06f, 0.64f), WoodDark, 2);
            Shape(boardRig, "Board", roundedSmall, Vector2.zero, new Vector2(length - 0.1f, 0.48f), GamePalette.Hex("#F6E7C1"), 3);
            Shape(boardRig, "Board_Pin", circle, new Vector2(0f, 0.2f), Vector2.one * 0.09f, GamePalette.Hex("#FFD23F"), 4);
            Label(boardRig, text, Vector2.zero, GamePalette.Hex("#3A2518"), 0.036f, TextAnchor.MiddleCenter);
            root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 0.4f);
        }

        private static void Rock(Transform parent, Transform shadows, float x, float y, float scale)
        {
            var root = Root(parent, "Rock", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.22f, y - 0.05f), new Vector2(2.0f * scale, 0.65f * scale), 0.26f);
            var stoneBase = GamePalette.Hex("#8E98AD");
            Shape(root, "Outline", circle, new Vector2(0f, 0.4f * scale), new Vector2(1.72f, 1.22f) * scale, GamePalette.Outline, 0);
            Shape(root, "Rock_Shade", circle, new Vector2(0.06f, 0.36f * scale), new Vector2(1.54f, 1.04f) * scale, GamePalette.Shade(stoneBase, 0.82f), 1);
            Shape(root, "Rock", circle, new Vector2(-0.04f, 0.43f * scale), new Vector2(1.42f, 0.94f) * scale, stoneBase, 1);
            Shape(root, "SubRock_Outline", circle, new Vector2(0.55f, 0.22f) * scale, new Vector2(0.88f, 0.66f) * scale, GamePalette.Outline, 2);
            Shape(root, "SubRock", circle, new Vector2(0.55f, 0.22f) * scale, new Vector2(0.72f, 0.52f) * scale, GamePalette.Shade(stoneBase, 0.9f), 3);
            var light = GamePalette.White;
            light.a = 0.38f;
            Shape(root, "Shine", circle, new Vector2(-0.32f, 0.65f) * scale, new Vector2(0.52f, 0.28f) * scale, light, 3);
            Shape(root, "Moss", circle, new Vector2(-0.42f, 0.16f) * scale, new Vector2(0.46f, 0.22f) * scale, GamePalette.Hex("#4CB36F"), 3);
            root.gameObject.AddComponent<CircleCollider2D>().radius = 0.55f * scale;
        }

        // ---------------------------------------------------------------- neighborhood (west)

        /// <summary>Three staggered rows of big trees along the east edge, hiding where the roads meet the rock face.</summary>
        private static void BuildEastTreeline(Transform parent, Transform shadows)
        {
            // Its own random so the rest of the world keeps the layout it had.
            var random = new System.Random(31);
            for (int row = 0; row < 3; row++)
            {
                for (float y = -23f + row * 0.8f; y < MapLayout.WorldNorthEdgeY + 3f; y += 2.4f)
                {
                    float jx = 68.2f + row * 2.5f + ((float)random.NextDouble() - 0.5f) * 0.8f;
                    float jy = y + ((float)random.NextDouble() - 0.5f) * 0.8f;
                    Tree(parent, shadows, jx, jy, 1.5f + (float)random.NextDouble() * 0.25f, DistrictGreens[random.Next(DistrictGreens.Length)], false);
                }
            }
        }

        private static void BuildNeighborhood(Transform parent, Transform shadows, System.Random random)
        {
            // North row.
            House(parent, shadows, "Casa_Vecina_A", -43f, -37.5f, 6f, 11f, "#E07A5F", "#F7E8D0");
            Building(parent, shadows, new BuildingSpec
            {
                Name = "Cooperativa", Sign = "COOPERATIVA", Roof = "#2E8B57", Wall = "#F4EBD0",
                XMin = -36.5f, XMax = -26.5f, YBottom = 6f, YTop = 13.4f, DoorX = -31.5f, FacadeHeight = 3.2f, Entrance = Entrance.Door,
                AwningA = "#2E8B57", AwningB = "#F8F5E8"
            });

            // South row.
            House(parent, shadows, "Casa_Vecina_B", -43f, -37.5f, -8f, -3f, "#4D9DE0", "#F7E8D0");
            House(parent, shadows, "Casa_Vecina_C", -35.5f, -29.5f, -8f, -3f, "#E1BC29", "#FBE3C4");
            Building(parent, shadows, new BuildingSpec
            {
                Name = "InaCursos", Sign = "INA · CURSOS", Roof = "#1D4F8C", Wall = "#F2E3C8",
                XMin = -29.2f, XMax = -24.8f, YBottom = -8f, YTop = -2.6f, DoorX = -27.0f, FacadeHeight = 2.8f, Entrance = Entrance.Door,
                AwningA = "#1D4F8C", AwningB = "#F8F5E8"
            });
            YardSign(parent, shadows, -38.2f, -8.7f, "SE VENDE", "#E8505B");

            foreach (var spot in new[] { new Vector2(-26f, 8.8f), new Vector2(-44.4f, 14f), new Vector2(-39f, 14.5f), new Vector2(-33f, 15f), new Vector2(-27f, 14.5f), new Vector2(-28f, -3.2f), new Vector2(-44.4f, -3f) })
            {
                Tree(parent, shadows, spot.x, spot.y, 1.15f, "#3FA66A", spot.x < -30f);
            }

            foreach (float x in new[] { -41f, -34f, -32f, -39f })
            {
                Bush(parent, shadows, x, x < -35f ? 5.2f : -2.7f, 1f);
            }

            // The lamp that stood in front of the cooperative door (x = -31.5) moved to the left, out of the way.
            foreach (float x in new[] { -41f, -34.5f, -26f })
            {
                Lamp(parent, x, 4.5f);
            }

            foreach (float x in new[] { -40f, -30f })
            {
                Lamp(parent, x, -9.3f);
            }

            FootballGoals(parent, shadows);
        }

        /// <summary>A stake in the front yard: the house is for sale or for rent.</summary>
        private static void YardSign(Transform parent, Transform shadows, float x, float y, string text, string hex)
        {
            var root = Root(parent, "YardSign", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.1f, y), new Vector2(1.05f, 0.34f), 0.24f);
            Shape(root, "Post_Outline", roundedSmall, new Vector2(0f, 0.45f), new Vector2(0.2f, 0.92f), GamePalette.Outline, 0);
            Shape(root, "Post", roundedSmall, new Vector2(0f, 0.45f), new Vector2(0.1f, 0.86f), GamePalette.Hex("#8B5A3C"), 1);

            var board = new GameObject("BoardRig").transform;
            board.SetParent(root, false);
            board.localPosition = new Vector2(0f, 0.95f);
            board.gameObject.AddComponent<FoliagePhysics>().Configure(2.0f, 1.5f, x * 0.9f + y, 0f, true, 1.2f, 7f);

            Shape(board, "Board_Outline", roundedSmall, Vector2.zero, new Vector2(1.62f, 0.68f), GamePalette.Outline, 2);
            Shape(board, "Board_Frame", roundedSmall, Vector2.zero, new Vector2(1.5f, 0.56f), GamePalette.Hex(hex), 2);
            Shape(board, "Board", roundedSmall, Vector2.zero, new Vector2(1.36f, 0.42f), GamePalette.Hex("#FBF5E4"), 3);
            Label(board, text, Vector2.zero, GamePalette.Hex(hex), 0.025f, TextAnchor.MiddleCenter);
            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.12f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        /// <summary>A plain neighborhood house: a door, windows and a colored roof, and no shop sign.</summary>
        private static void House(Transform parent, Transform shadows, string name, float xMin, float xMax, float yBottom, float yTop, string roof, string wall)
        {
            Building(parent, shadows, new BuildingSpec
            {
                Name = name, Sign = null, Roof = roof, Wall = wall,
                XMin = xMin, XMax = xMax, YBottom = yBottom, YTop = yTop, DoorX = (xMin + xMax) * 0.5f, FacadeHeight = 2.5f, Entrance = Entrance.Door
            });
        }

        private static void FootballGoals(Transform parent, Transform shadows)
        {
            foreach (float x in new[] { -41.6f, -28.4f })
            {
                var goal = Root(parent, "Goal", new Vector2(x, -24.2f), -24.2f);
                Shape(goal, "Goal_Outline", roundedSmall, new Vector2(0f, 0.9f), new Vector2(0.5f, 2.6f), GamePalette.Outline, 0);
                var net = GamePalette.White;
                net.a = 0.55f;
                Shape(goal, "Net", roundedSmall, new Vector2(0f, 0.9f), new Vector2(0.36f, 2.4f), net, 1);
                Shape(goal, "Post", circle, new Vector2(0f, 2.15f), Vector2.one * 0.34f, GamePalette.White, 2);
                Shape(goal, "Post", circle, new Vector2(0f, -0.35f), Vector2.one * 0.34f, GamePalette.White, 2);
                goal.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            }
        }

        // ---------------------------------------------------------------- farm (east)

        private static void BuildEastFarm(Transform parent, Transform shadows, System.Random random)
        {
            Building(parent, shadows, new BuildingSpec
            {
                Name = "Establo", Sign = "FINCA", Roof = "#8B3A3A", Wall = "#C0504D",
                XMin = 33f, XMax = 41f, YBottom = 7.5f, YTop = 13.5f, DoorX = 37f, FacadeHeight = 2.9f, Entrance = Entrance.Garage
            });
            Silo(parent, shadows, 38f, 9f);

            // Coffee rows: each row of bushes sits on a furrow of dark soil, with a big shade tree every few bushes.
            int coffeeRow = 0;
            for (float y = 15f; y < 22f; y += 2.3f, coffeeRow++)
            {
                OnGround(parent, "Furrow_Outline", rounded, new Vector2(37.9f, y + 0.02f), new Vector2(11.4f, 0.62f), GamePalette.Shade(GamePalette.Hex("#6B4A2E"), 0.6f), 5);
                OnGround(parent, "Furrow", rounded, new Vector2(37.9f, y + 0.02f), new Vector2(11.2f, 0.5f), GamePalette.Hex("#6B4A2E"), 6);
                int coffeeCol = 0;
                for (float x = 32.8f; x < 43f; x += 1.7f, coffeeCol++)
                {
                    if ((coffeeRow == 1 && coffeeCol == 1) || (coffeeRow == 2 && coffeeCol == 4))
                    {
                        // Shade tree (poro) among the coffee.
                        Tree(parent, shadows, x, y, 1.4f, "#3B9660", false);
                        continue;
                    }

                    CoffeeBush(parent, shadows, x, y);
                }
            }

            // Pasture with cows between the two streets.
            var min = new Vector2(32f, -7.2f);
            var max = new Vector2(43f, -2.8f);
            FenceRun(parent, new Vector2(min.x, min.y), new Vector2(max.x, min.y));
            FenceRun(parent, new Vector2(min.x, max.y), new Vector2(max.x, max.y));
            FenceRun(parent, new Vector2(min.x, min.y), new Vector2(min.x, max.y));
            FenceRun(parent, new Vector2(max.x, min.y), new Vector2(max.x, max.y));
            var cows = Group(parent, "Cows");
            Cow(cows, new Vector2(34f, -5f), new Vector2(38f, -4.4f), 0.5f);
            Cow(cows, new Vector2(41f, -6.2f), new Vector2(36.5f, -6f), 0.4f);
            Cow(cows, new Vector2(35.5f, -3.9f), new Vector2(40f, -3.9f), 0.45f);

            Tree(parent, shadows, 32.2f, -1.4f, 1.2f, "#3FA66A", false);
            Tree(parent, shadows, 42.6f, -1.4f, 1.2f, "#4CB36F", true);
            Tree(parent, shadows, 33f, 14.2f, 1.1f, "#3B9660", false);
            Tree(parent, shadows, 42.8f, -9.2f, 1.2f, "#3FA66A", false);
            Tree(parent, shadows, 41.5f, 6.4f, 1.0f, "#3B9660", false);
        }

        /// <summary>A coffee bush in cartoon style: tall and cone-shaped, dark glossy leaves in pairs, red and green berries.</summary>
        private static void CoffeeBush(Transform parent, Transform shadows, float x, float y)
        {
            var root = Root(parent, "Coffee", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.1f, y), new Vector2(1.1f, 0.4f), 0.24f);

            var rig = new GameObject("CoffeeRig").transform;
            rig.SetParent(root, false);
            rig.localPosition = new Vector2(0f, 0.1f);
            rig.gameObject.AddComponent<FoliagePhysics>().Configure(4.5f, 2.0f, x * 1.1f + y * 0.7f, 0.025f, true, 1.5f, 12f);

            var dark = GamePalette.Hex("#1F5E33");
            var mid = GamePalette.Hex("#2E7D46");
            var light = GamePalette.Hex("#4FA36A");

            // Tall body: three stacked rounded layers, wide at the bottom and narrower at the top, each with its outline.
            Shape(rig, "Outline_Base", circle, new Vector2(0f, 0.28f), new Vector2(1.06f, 0.64f), GamePalette.Outline, 0);
            Shape(rig, "Outline_Mid", circle, new Vector2(0f, 0.62f), new Vector2(0.94f, 0.62f), GamePalette.Outline, 0);
            Shape(rig, "Outline_Top", circle, new Vector2(0f, 0.95f), new Vector2(0.68f, 0.56f), GamePalette.Outline, 0);
            Shape(rig, "Cone_Base", circle, new Vector2(0f, 0.28f), new Vector2(0.98f, 0.56f), dark, 1);
            Shape(rig, "Cone_Mid", circle, new Vector2(0f, 0.62f), new Vector2(0.86f, 0.54f), mid, 1);
            Shape(rig, "Cone_Top", circle, new Vector2(0f, 0.95f), new Vector2(0.6f, 0.48f), mid, 1);
            Shape(rig, "Cone_Shine", circle, new Vector2(-0.12f, 1.0f), new Vector2(0.22f, 0.18f), light, 2);

            // Pairs of oval leaves along three horizontal branches, drooping a little at the tips.
            foreach (float by in new[] { 0.3f, 0.62f, 0.92f })
            {
                float reach = 0.36f - (by - 0.3f) * 0.22f;
                Shape(rig, "Branch", square, new Vector2(0f, by), new Vector2(reach * 2f, 0.04f), GamePalette.Outline, 2);
                foreach (float side in new[] { -1f, 1f })
                {
                    var leaf = Shape(rig, "Leaf", circle, new Vector2(side * reach, by + 0.02f), new Vector2(0.34f, 0.14f), side < 0f ? light : GamePalette.Shade(mid, 1.1f), 3);
                    Tilt(leaf, side * -18f);
                }
            }
            // Berry clusters stuck to the branches: mostly red, a few green and yellow.
            var berries = new[]
            {
                new Vector3(0.30f, 0.26f, 0f), new Vector3(0.36f, 0.31f, 1f), new Vector3(-0.30f, 0.28f, 0f), new Vector3(-0.24f, 0.22f, 2f),
                new Vector3(0.22f, 0.58f, 1f), new Vector3(0.27f, 0.62f, 0f), new Vector3(-0.22f, 0.6f, 0f), new Vector3(-0.28f, 0.56f, 3f),
                new Vector3(0.12f, 0.9f, 0f), new Vector3(-0.1f, 0.88f, 1f)
            };
            var berryColors = new[] { "#C0392B", "#E74C3C", "#7FB241", "#F1C40F" };
            foreach (var berry in berries)
            {
                float size = (berry.x * 37f + berry.y * 11f) % 1f > 0.5f ? 0.08f : 0.07f;
                Shape(rig, "Berry", circle, new Vector2(berry.x, berry.y), Vector2.one * size, GamePalette.Hex(berryColors[(int)berry.z]), 4);
            }

            root.gameObject.AddComponent<CircleCollider2D>().radius = 0.3f;
        }
        private static void Silo(Transform parent, Transform shadows, float x, float y)
        {
            var root = Root(parent, "Silo", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.3f, y), new Vector2(2.2f, 0.8f), 0.25f);
            Shape(root, "Body_Outline", roundedSmall, new Vector2(0f, 1.9f), new Vector2(1.7f, 3.9f), GamePalette.Outline, 0);
            Shape(root, "Body", roundedSmall, new Vector2(0f, 1.9f), new Vector2(1.5f, 3.7f), GamePalette.Hex("#C9CFDB"), 1);
            Shape(root, "Band", square, new Vector2(0f, 1.2f), new Vector2(1.5f, 0.1f), GamePalette.Hex("#8E96AA"), 2);
            Shape(root, "Band", square, new Vector2(0f, 2.4f), new Vector2(1.5f, 0.1f), GamePalette.Hex("#8E96AA"), 2);
            Shape(root, "Dome_Outline", circle, new Vector2(0f, 3.75f), new Vector2(1.7f, 1.4f), GamePalette.Outline, 2);
            Shape(root, "Dome", circle, new Vector2(0f, 3.75f), new Vector2(1.5f, 1.2f), GamePalette.Hex("#C0504D"), 3);
            root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1.5f, 0.8f);
        }

        /// <summary>A wooden fence along a straight line, either horizontal or vertical.</summary>
        private static void FenceRun(Transform parent, Vector2 from, Vector2 to)
        {
            bool horizontal = Mathf.Abs(to.x - from.x) >= Mathf.Abs(to.y - from.y);
            float length = horizontal ? to.x - from.x : to.y - from.y;
            var center = (from + to) * 0.5f;
            var root = Root(parent, "Fence", center, Mathf.Min(from.y, to.y));

            if (horizontal)
            {
                Shape(root, "Rail_Outline", roundedSmall, new Vector2(0f, 0.55f), new Vector2(length, 0.3f), GamePalette.Outline, 0);
                Shape(root, "Rail", roundedSmall, new Vector2(0f, 0.55f), new Vector2(length, 0.16f), WoodColor, 1);
                for (float x = -length * 0.5f; x <= length * 0.5f + 0.01f; x += 1.6f)
                {
                    Shape(root, "Post", roundedSmall, new Vector2(x, 0.4f), new Vector2(0.2f, 0.95f), WoodDark, 2);
                }

                root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(length, 0.3f);
            }
            else
            {
                Shape(root, "Rail_Outline", roundedSmall, Vector2.zero, new Vector2(0.3f, length), GamePalette.Outline, 0);
                Shape(root, "Rail", roundedSmall, Vector2.zero, new Vector2(0.16f, length), WoodColor, 1);
                for (float y = -length * 0.5f; y <= length * 0.5f + 0.01f; y += 1.6f)
                {
                    Shape(root, "Post", roundedSmall, new Vector2(0f, y), new Vector2(0.2f, 0.7f), WoodDark, 2);
                }

                root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, length);
            }
        }

        /// <summary>A cow that wanders between two points. Same layout as the pedestrians, so it shares their walk cycle.</summary>
        private static void Cow(Transform parent, Vector2 from, Vector2 to, float speed)
        {
            var cow = new GameObject("Cow");
            cow.transform.SetParent(parent, false);
            cow.transform.position = from;
            MakeYSorted(cow, -0.35f);

            var shadow = GamePalette.Outline;
            shadow.a = 0.35f;
            Shape(cow.transform, "Shadow", softCircle, new Vector2(0f, -0.4f), new Vector2(1.8f, 0.5f), shadow, OrderPlayer - 1);

            var bob = new GameObject("Bob").transform;
            bob.SetParent(cow.transform, false);
            bob.gameObject.AddComponent<PlayerAnimator>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(bob, false);

            var hide = GamePalette.Hex("#F4F1EA");
            var spot = GamePalette.Hex("#2B2B35");
            Shape(visual, "Body_Outline", rounded, Vector2.zero, new Vector2(1.7f, 0.95f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "Head_Outline", rounded, new Vector2(0.95f, 0.12f), new Vector2(0.74f, 0.7f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "Body", rounded, Vector2.zero, new Vector2(1.54f, 0.79f), hide, OrderPlayer + 1);
            Shape(visual, "Head", rounded, new Vector2(0.95f, 0.12f), new Vector2(0.58f, 0.54f), hide, OrderPlayer + 1);
            Shape(visual, "Spot", circle, new Vector2(-0.3f, 0.1f), new Vector2(0.55f, 0.4f), spot, OrderPlayer + 2);
            Shape(visual, "Spot", circle, new Vector2(0.25f, -0.15f), new Vector2(0.34f, 0.28f), spot, OrderPlayer + 2);
            Shape(visual, "Snout", roundedSmall, new Vector2(1.15f, 0.0f), new Vector2(0.3f, 0.24f), GamePalette.Hex("#F4A6B8"), OrderPlayer + 2);
            Shape(visual, "Eye", circle, new Vector2(1.0f, 0.24f), Vector2.one * 0.11f, GamePalette.Outline, OrderPlayer + 3);
            Shape(visual, "Horn", roundedSmall, new Vector2(0.82f, 0.5f), new Vector2(0.1f, 0.2f), GamePalette.Hex("#F2E3B0"), OrderPlayer + 2);
            Shape(visual, "Leg", roundedSmall, new Vector2(-0.5f, -0.52f), new Vector2(0.2f, 0.3f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "Leg", roundedSmall, new Vector2(0.45f, -0.52f), new Vector2(0.2f, 0.3f), GamePalette.Outline, OrderPlayer);

            // The rest of the animal: a shaded belly, an udder, a tail with its tuft, ears, nostrils, a second horn and a bell.
            Shape(visual, "BodyShade", rounded, new Vector2(0f, -0.24f), new Vector2(1.4f, 0.26f), GamePalette.Shade(hide, 0.86f), OrderPlayer + 2);
            Shape(visual, "Udder", roundedSmall, new Vector2(-0.12f, -0.45f), new Vector2(0.3f, 0.14f), GamePalette.Hex("#F4A6B8"), OrderPlayer);
            Shape(visual, "Tail", roundedSmall, new Vector2(-0.9f, 0.05f), new Vector2(0.07f, 0.52f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "TailTuft", circle, new Vector2(-0.9f, -0.24f), new Vector2(0.17f, 0.2f), spot, OrderPlayer + 1);
            Shape(visual, "Ear", roundedSmall, new Vector2(0.68f, 0.42f), new Vector2(0.2f, 0.11f), GamePalette.Shade(hide, 0.9f), OrderPlayer + 2);
            Shape(visual, "Horn", roundedSmall, new Vector2(1.08f, 0.5f), new Vector2(0.1f, 0.2f), GamePalette.Hex("#F2E3B0"), OrderPlayer + 2);
            Shape(visual, "Nostril", circle, new Vector2(1.24f, 0.05f), Vector2.one * 0.04f, GamePalette.Outline, OrderPlayer + 3);
            Shape(visual, "Nostril", circle, new Vector2(1.24f, -0.05f), Vector2.one * 0.04f, GamePalette.Outline, OrderPlayer + 3);
            Shape(visual, "Bell", circle, new Vector2(0.78f, -0.2f), Vector2.one * 0.14f, GamePalette.Hex("#FFD23F"), OrderPlayer + 3);
            Shape(visual, "BodyShine", roundedSmall, new Vector2(-0.1f, 0.3f), new Vector2(0.6f, 0.06f), GamePalette.White, OrderPlayer + 3);

            cow.AddComponent<Patrol>().Configure(from, to, speed, 2.5f);
        }

        // ---------------------------------------------------------------- beach (south)

        private static void BuildBeach(Transform parent, Transform shadows, System.Random random)
        {
            foreach (var spot in new[] { new Vector2(-24f, -22f), new Vector2(-19f, -25f), new Vector2(-12f, -21.5f), new Vector2(-3f, -24f), new Vector2(1f, -21.5f),
                new Vector2(18f, -25f), new Vector2(22f, -21.5f), new Vector2(27f, -25.5f), new Vector2(35f, -21.5f), new Vector2(40f, -25f), new Vector2(15.5f, -22f) })
            {
                Palm(parent, shadows, spot.x, spot.y, 0.95f + (float)random.NextDouble() * 0.25f);
            }

            Umbrella(parent, shadows, -15f, -23.5f, "#E8505B");
            Umbrella(parent, shadows, -8.5f, -25f, "#4DA3FF");
            Umbrella(parent, shadows, -0.5f, -22.8f, "#FFD23F");
            Umbrella(parent, shadows, 21f, -23.8f, "#55E08F");
            Umbrella(parent, shadows, 33f, -24f, "#FF8A3D");

            Lifeguard(parent, shadows, 38f, -23f);
            foreach (var spot in new[] { new Vector2(-6f, -20.5f), new Vector2(11f, -19.9f), new Vector2(29.5f, -20f) })
            {
                Rock(parent, shadows, spot.x, spot.y, 0.7f);
            }

            // People relaxing on the beach and walking the shore
            Shape(parent, "BeachTowel1", roundedSmall, new Vector2(-13.8f, -24.2f), new Vector2(1.5f, 0.85f), GamePalette.Hex("#FF8A3D"), 6);
            Shape(parent, "BeachTowelStripe1", square, new Vector2(-13.8f, -24.2f), new Vector2(1.5f, 0.22f), GamePalette.Hex("#FFFFFF"), 7);
            var lookBeach1 = new Look { Skin = "#4DA3FF", Pants = "#FFD23F", Hair = HairStyle.Side, Extra = Accessory.Cap, ExtraColor = "#E8505B" };
            BuildNpc(parent, "PlayeroTowel", new Vector2(-13.8f, -24.1f), lookBeach1, true);

            Shape(parent, "BeachTowel2", roundedSmall, new Vector2(31.5f, -24.5f), new Vector2(1.5f, 0.85f), GamePalette.Hex("#2E8B57"), 6);
            Shape(parent, "BeachTowelStripe2", square, new Vector2(31.5f, -24.5f), new Vector2(1.5f, 0.22f), GamePalette.Hex("#FFFFFF"), 7);
            var lookBeach2 = new Look { Skin = "#FFD23F", Pants = "#E8505B", Hair = HairStyle.Bun, HairColor = "#555A66" };
            BuildNpc(parent, "PlayeraTowel", new Vector2(31.5f, -24.4f), lookBeach2, true);

            var lookBeach3 = new Look { Skin = "#E07A5F", Pants = "#3D2B1F", Hair = HairStyle.Side, Extra = Accessory.Cap, ExtraColor = "#4DA3FF" };
            BuildNpc(parent, "PlayeroSombra", new Vector2(-1.6f, -23.4f), lookBeach3, true);

            var lookWalker = new Look { Skin = "#55E08F", Pants = "#FFFFFF", Hair = HairStyle.Buzz, HairColor = "#1D2238" };
            BuildPedestrian(parent, "PaseantePlaya", new Vector2(-9.0f, -26.3f), new Vector2(0.5f, -26.3f), lookWalker, 1.3f);

            Building(parent, shadows, new BuildingSpec
            {
                Name = "SodaElMuelle", Sign = "SODA EL MUELLE", Roof = "#00A8A8", Wall = "#FFF1C9",
                XMin = 9f, XMax = 16f, YBottom = -24f, YTop = -20f, DoorX = 12.5f, FacadeHeight = 2.2f, Entrance = Entrance.Door,
                AwningA = "#00A8A8", AwningB = "#F8F5E8"
            });
            CoursesBoard(parent, -25.2f, -8.7f);
            Bench(parent, shadows, 17.8f, -23.2f);
        }

        /// <summary>A wooden easel board with the courses of the INA and the UNA, beside the plaza entrance.</summary>
        private static void CoursesBoard(Transform parent, float x, float y)
        {
            var root = Root(parent, "CoursesBoard", new Vector2(x, y), y);
            // Solid base so the easel cannot be walked through; the station stays in front of it.
            Wall(root, "Board_Collider", new Vector2(0f, 0.2f), new Vector2(1.3f, 0.4f));
            foreach (float post in new[] { -0.55f, 0.55f })
            {
                Shape(root, "Post_Outline", roundedSmall, new Vector2(post, 0.52f), new Vector2(0.18f, 1.12f), GamePalette.Outline, 0);
                Shape(root, "Post", roundedSmall, new Vector2(post, 0.52f), new Vector2(0.10f, 1.02f), GamePalette.Hex("#8B5A3C"), 1);
            }

            var shade = GamePalette.Outline;
            shade.a = 0.34f;
            Shape(root, "Board_Shadow", roundedSmall, new Vector2(0.05f, 1.02f), new Vector2(1.78f, 0.92f), shade, 1);
            Shape(root, "Board_Outline", roundedSmall, new Vector2(0f, 1.08f), new Vector2(1.76f, 0.92f), GamePalette.Outline, 2);
            Shape(root, "Board_Frame", roundedSmall, new Vector2(0f, 1.08f), new Vector2(1.62f, 0.78f), GamePalette.Hex("#8B5A3C"), 2);
            Shape(root, "Board", roundedSmall, new Vector2(0f, 1.08f), new Vector2(1.48f, 0.64f), GamePalette.Hex("#1D4F8C"), 3);
            Shape(root, "Paper", square, new Vector2(-0.36f, 0.96f), new Vector2(0.44f, 0.28f), GamePalette.Hex("#F8F5E8"), 4);
            Shape(root, "Paper_Pin", circle, new Vector2(-0.36f, 1.06f), Vector2.one * 0.07f, GamePalette.Hex("#E8505B"), 5);
            Shape(root, "Paper", square, new Vector2(0.36f, 0.96f), new Vector2(0.44f, 0.28f), GamePalette.Hex("#FFE9A8"), 4);
            Shape(root, "Paper_Pin", circle, new Vector2(0.36f, 1.06f), Vector2.one * 0.07f, GamePalette.Hex("#FFD23F"), 5);
            Label(root, "CURSOS", new Vector2(0f, 1.24f), GamePalette.White, 0.026f, TextAnchor.MiddleCenter);
        }

        /// <summary>The time clock of the first job, on the wall of the soda.</summary>
        private static void TimeClock(Transform parent, float x, float y)
        {
            var root = Root(parent, "TimeClock", new Vector2(x, y), -24.05f);
            Shape(root, "Box_Outline", roundedSmall, Vector2.zero, new Vector2(0.86f, 1.06f), GamePalette.Outline, 0);
            Shape(root, "Box", roundedSmall, Vector2.zero, new Vector2(0.72f, 0.92f), GamePalette.Hex("#D7DCE2"), 1);
            Shape(root, "Face", roundedSmall, new Vector2(0f, 0.17f), new Vector2(0.48f, 0.34f), GamePalette.Hex("#F8F5E8"), 2);
            Shape(root, "Hand", roundedSmall, new Vector2(0.05f, 0.2f), new Vector2(0.2f, 0.05f), GamePalette.Outline, 3);
            Shape(root, "Slot", roundedSmall, new Vector2(0f, -0.25f), new Vector2(0.46f, 0.1f), GamePalette.Outline, 2);
        }

        private static void Palm(Transform parent, Transform shadows, float x, float y, float scale)
        {
            var root = Root(parent, "Palm", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.5f, y - 0.05f), new Vector2(2.6f * scale, 0.85f * scale), 0.25f);
            var trunkColor = GamePalette.Hex("#A9743F");
            Shape(root, "Trunk_Outline", roundedSmall, new Vector2(0.1f, 1.35f) * scale, new Vector2(0.52f, 2.84f) * scale, GamePalette.Outline, 0);
            Shape(root, "Trunk", roundedSmall, new Vector2(0.1f, 1.35f) * scale, new Vector2(0.36f, 2.7f) * scale, trunkColor, 1);
            Shape(root, "Trunk_Shade", roundedSmall, new Vector2(0.2f, 1.35f) * scale, new Vector2(0.12f, 2.6f) * scale, GamePalette.Shade(trunkColor, 0.82f), 1);
            Shape(root, "Trunk_Highlight", roundedSmall, new Vector2(0.02f, 1.35f) * scale, new Vector2(0.1f, 2.5f) * scale, GamePalette.Shade(trunkColor, 1.18f), 1);
            for (float ty = 0.42f; ty <= 2.4f; ty += 0.38f)
            {
                Shape(root, "Trunk_Ring", roundedSmall, new Vector2(0.1f, ty) * scale, new Vector2(0.38f, 0.08f) * scale, GamePalette.Shade(trunkColor, 0.72f), 2);
            }

            var top = new Vector2(0.1f, 2.75f) * scale;
            var crownRig = new GameObject("PalmCrownRig").transform;
            crownRig.SetParent(root, false);
            crownRig.localPosition = top;
            crownRig.gameObject.AddComponent<FoliagePhysics>().Configure(5.2f, 1.55f, x * 0.6f + y * 0.9f, 0.025f, true, 2.1f, 8f);

            var frondSpr = palmFrondSprite != null ? palmFrondSprite : circle;
            var leaf = GamePalette.Hex("#3FA66A");
            var leafDeep = GamePalette.Hex("#2E8550");
            var leafBright = GamePalette.Hex("#58C97E");
            float[] backAngles = { -28f, 208f, 55f, 125f };
            foreach (float angle in backAngles)
            {
                var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var at = direction * 0.85f * scale;
                var backFrond = Shape(crownRig, "Leaf_Back", frondSpr, at, new Vector2(2.15f, 0.95f) * scale, leafDeep, 2);
                Tilt(backFrond, angle);
            }

            float[] frontAngles = { 12f, 168f, 42f, 138f, 90f };
            foreach (float angle in frontAngles)
            {
                var direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var at = direction * 0.88f * scale;
                var body = Shape(crownRig, "Leaf", frondSpr, at, new Vector2(2.18f, 1.02f) * scale, angle == 90f ? leafBright : leaf, 3);
                Tilt(body, angle);
            }

            foreach (var cocoSpot in new[] { new Vector2(-0.18f, -0.18f), new Vector2(0.18f, -0.2f), new Vector2(0f, -0.08f) })
            {
                Shape(crownRig, "Coconut_Outline", circle, cocoSpot * scale, Vector2.one * 0.36f * scale, GamePalette.Outline, 4);
                Shape(crownRig, "Coconut", circle, cocoSpot * scale, Vector2.one * 0.26f * scale, GamePalette.Hex("#7A4F2A"), 5);
                Shape(crownRig, "Coconut_Shine", circle, (cocoSpot + new Vector2(-0.04f, 0.04f)) * scale, Vector2.one * 0.09f * scale, GamePalette.Hex("#C89662"), 6);
            }

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.22f;
            collider.offset = new Vector2(0.1f, 0.2f);
        }

        private static void Umbrella(Transform parent, Transform shadows, float x, float y, string hex)
        {
            var root = Root(parent, "Umbrella", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.35f, y - 0.1f), new Vector2(2.75f, 0.95f), 0.26f);

            // Cel-shaded beach towel with outline, woven stripes, and fringe ends.
            var towelCenter = new Vector2(x - 0.2f, y - 0.9f);
            OnGround(shadows, "Towel_Outline", roundedSmall, towelCenter, new Vector2(1.88f, 0.96f), GamePalette.Outline, WorldSand + 1);
            OnGround(shadows, "Towel", roundedSmall, towelCenter, new Vector2(1.74f, 0.82f), GamePalette.Hex("#F8F5E8"), WorldSand + 2);
            OnGround(shadows, "Towel_StripeMain", square, towelCenter, new Vector2(1.68f, 0.22f), GamePalette.Hex(hex), WorldSand + 3);
            OnGround(shadows, "Towel_StripeTop", square, towelCenter + new Vector2(0f, 0.24f), new Vector2(1.68f, 0.08f), GamePalette.Hex(hex), WorldSand + 3);
            OnGround(shadows, "Towel_StripeBot", square, towelCenter + new Vector2(0f, -0.24f), new Vector2(1.68f, 0.08f), GamePalette.Hex(hex), WorldSand + 3);

            // Wood-and-metal umbrella pole with outline.
            Shape(root, "Pole_Outline", roundedSmall, new Vector2(0f, 0.95f), new Vector2(0.22f, 1.95f), GamePalette.Outline, 0);
            Shape(root, "Pole", roundedSmall, new Vector2(0f, 0.95f), new Vector2(0.12f, 1.88f), GamePalette.Hex("#D8D1C4"), 1);

            var canopyRig = new GameObject("CanopyRig").transform;
            canopyRig.SetParent(root, false);
            canopyRig.localPosition = new Vector2(0f, 1.95f);
            canopyRig.gameObject.AddComponent<FoliagePhysics>().Configure(2.4f, 1.4f, x * 0.5f + y, 0.01f, false);

            var mainCol = GamePalette.Hex(hex);
            var umbrellaSpr = beachUmbrellaSprite != null ? beachUmbrellaSprite : circle;
            Shape(canopyRig, "Canopy", umbrellaSpr, Vector2.zero, new Vector2(2.55f, 1.55f), mainCol, 2);

            // Alternating cream segments and scalloped valance along the front rim.
            Shape(canopyRig, "Canopy_StripeL", roundedSmall, new Vector2(-0.56f, 0.02f), new Vector2(0.38f, 0.92f), GamePalette.Hex("#F8F5E8"), 3).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Shape(canopyRig, "Canopy_StripeC", roundedSmall, new Vector2(0f, 0.04f), new Vector2(0.44f, 1.05f), GamePalette.Hex("#F8F5E8"), 3);
            Shape(canopyRig, "Canopy_StripeR", roundedSmall, new Vector2(0.56f, 0.02f), new Vector2(0.38f, 0.92f), GamePalette.Hex("#F8F5E8"), 3).transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            Shape(canopyRig, "Canopy_CapOutline", circle, new Vector2(0f, 0.14f), Vector2.one * 0.26f, GamePalette.Outline, 4);
            Shape(canopyRig, "Canopy_Cap", circle, new Vector2(0f, 0.14f), Vector2.one * 0.16f, GamePalette.Hex("#FFD23F"), 5);

            root.gameObject.AddComponent<CircleCollider2D>().radius = 0.18f;
        }

        private static void Lifeguard(Transform parent, Transform shadows, float x, float y)
        {
            var root = Root(parent, "LifeguardTower", new Vector2(x, y), y);
            GroundShadowRect(shadows, new Vector2(x + 0.4f, y - 0.1f), new Vector2(2.6f, 1.0f), 0.22f);
            foreach (float dx in new[] { -0.8f, 0.8f })
            {
                Shape(root, "Leg_Outline", roundedSmall, new Vector2(dx, 0.9f), new Vector2(0.3f, 1.8f), GamePalette.Outline, 0);
                Shape(root, "Leg", roundedSmall, new Vector2(dx, 0.9f), new Vector2(0.18f, 1.7f), WoodDark, 1);
            }

            Shape(root, "Cabin_Outline", rounded, new Vector2(0f, 2.3f), new Vector2(2.4f, 1.5f), GamePalette.Outline, 2);
            Shape(root, "Cabin", rounded, new Vector2(0f, 2.3f), new Vector2(2.24f, 1.34f), GamePalette.Hex("#FFF1C9"), 3);
            Shape(root, "Window", roundedSmall, new Vector2(0f, 2.35f), new Vector2(1.1f, 0.5f), GamePalette.Hex("#BFE3F5"), 4);

            var guardLook = new Look
            {
                Skin = "#E8505B", SkinTone = "#D9A47A", HairColor = "#1D2238", Hair = HairStyle.Buzz,
                Pants = "#C0392B", Extra = Accessory.Cap, ExtraColor = "#FFD23F"
            };
            BuildNpc(root, "Salvavidas", new Vector2(x, y + 2.05f), guardLook, false);
            BuildThing(root, "Salvavidas", "Hablar con el salvavidas", new Vector2(0f, 0.4f), new Vector2(2.2f, 1.6f),
                "¡Ojo con la corriente, mae!");

            Shape(root, "Roof_Outline", rounded, new Vector2(0f, 3.25f), new Vector2(2.9f, 0.6f), GamePalette.Outline, 35);
            Shape(root, "Roof", rounded, new Vector2(0f, 3.25f), new Vector2(2.74f, 0.44f), GamePalette.Hex("#E8505B"), 36);

            var flagRig = new GameObject("FlagRig").transform;
            flagRig.SetParent(root, false);
            flagRig.localPosition = new Vector2(0.85f, 3.8f);
            flagRig.gameObject.AddComponent<FoliagePhysics>().Configure(6.5f, 2.8f, x, 0.03f, false);
            Shape(flagRig, "Flag_Outline", roundedSmall, new Vector2(0.25f, 0f), new Vector2(0.58f, 0.38f), GamePalette.Outline, 37);
            Shape(flagRig, "Flag", roundedSmall, new Vector2(0.25f, 0f), new Vector2(0.5f, 0.3f), GamePalette.Hex("#FFD23F"), 38);
            root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(2.0f, 0.5f);
        }

        // ---------------------------------------------------------------- public building details

        /// <summary>Four fluted classical columns with carved capitals and bases, the mark of a bank.</summary>
        private static void Columns(Transform root, float dx, float height)
        {
            foreach (float side in new[] { -3.7f, -1.65f, 1.65f, 3.7f })
            {
                float x = dx + side;
                Shape(root, "ColBase_Outline", roundedSmall, new Vector2(x, 0.24f), new Vector2(0.86f, 0.34f), GamePalette.Outline, 6);
                Shape(root, "ColBase", roundedSmall, new Vector2(x, 0.24f), new Vector2(0.74f, 0.22f), GamePalette.Hex("#DCE2EE"), 7);
                Shape(root, "ColPlinth", roundedSmall, new Vector2(x, 0.12f), new Vector2(0.82f, 0.14f), GamePalette.Hex("#C5CEDE"), 7);

                Shape(root, "Column_Outline", roundedSmall, new Vector2(x, height * 0.52f), new Vector2(0.66f, height - 0.1f), GamePalette.Outline, 6);
                Shape(root, "Column", roundedSmall, new Vector2(x, height * 0.52f), new Vector2(0.54f, height - 0.22f), GamePalette.Hex("#F5F7FC"), 7);
                Shape(root, "Column_Shade", roundedSmall, new Vector2(x + 0.16f, height * 0.52f), new Vector2(0.16f, height - 0.26f), GamePalette.Hex("#D9E0EC"), 7);
                foreach (float fx in new[] { -0.13f, 0f, 0.13f })
                {
                    Shape(root, "Flute", square, new Vector2(x + fx, height * 0.52f), new Vector2(0.035f, height - 0.55f), GamePalette.Hex("#BFC8D8"), 8);
                }

                Shape(root, "Column_CapOutline", roundedSmall, new Vector2(x, height - 0.08f), new Vector2(0.88f, 0.34f), GamePalette.Outline, 8);
                Shape(root, "Column_Cap", roundedSmall, new Vector2(x, height - 0.08f), new Vector2(0.76f, 0.22f), GamePalette.Hex("#E3E8F2"), 9);
            }
        }

        /// <summary>A curved fabric banner with gold corner grommets and hanging ropes, swaying in the breeze.</summary>
        private static void Banner(Transform root, string text, float dx, float y, string hex)
        {
            float length = text.Length * 0.32f + 0.95f;
            var bannerRig = new GameObject("BannerRig").transform;
            bannerRig.SetParent(root, false);
            bannerRig.localPosition = new Vector2(dx, y);
            bannerRig.gameObject.AddComponent<FoliagePhysics>().Configure(2.4f, 1.65f, dx + y * 0.6f, 0.015f, false);

            var shade = GamePalette.Outline;
            shade.a = 0.36f;
            var bannerColor = GamePalette.Hex(hex);
            Shape(bannerRig, "Banner_Shadow", roundedSmall, new Vector2(0.06f, -0.09f), new Vector2(length + 0.26f, 0.88f), shade, 12);
            Shape(bannerRig, "Banner_Outline", roundedSmall, Vector2.zero, new Vector2(length + 0.22f, 0.86f), GamePalette.Outline, 13);
            Shape(bannerRig, "Banner", roundedSmall, Vector2.zero, new Vector2(length + 0.08f, 0.72f), bannerColor, 14);
            Shape(bannerRig, "Banner_FoldA", roundedSmall, new Vector2(0f, -0.18f), new Vector2(length - 0.2f, 0.16f), GamePalette.Shade(bannerColor, 0.84f), 15);
            Shape(bannerRig, "Banner_Highlight", roundedSmall, new Vector2(0f, 0.22f), new Vector2(length - 0.25f, 0.10f), GamePalette.Shade(bannerColor, 1.16f), 15);

            float gx = length * 0.5f - 0.12f;
            foreach (float sx in new[] { -gx, gx })
            {
                foreach (float sy in new[] { -0.22f, 0.22f })
                {
                    Shape(bannerRig, "Grommet_Out", circle, new Vector2(sx, sy), Vector2.one * 0.15f, GamePalette.Outline, 15);
                    Shape(bannerRig, "Grommet", circle, new Vector2(sx, sy), Vector2.one * 0.09f, GamePalette.Hex("#FFD23F"), 16);
                }
            }

            Label(bannerRig, text, Vector2.zero, GamePalette.White, 0.035f, TextAnchor.MiddleCenter, 17);
        }
    }
}

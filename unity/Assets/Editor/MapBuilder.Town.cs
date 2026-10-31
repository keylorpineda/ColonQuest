using UnityEditor;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        private static readonly Color GrassColor = GamePalette.Hex("#67C242");
        private static readonly Color WalkColor = GamePalette.Hex("#DBD1BD");
        private static readonly Color RoadColor = GamePalette.Hex("#484A52");

        /// <summary>
        /// The overworld: two streets joined by an avenue, a plaza with a fountain, a park and
        /// eight places to visit. Casa, Banco and Tienda can be entered; the rest are
        /// stations out on the street.
        /// </summary>
        [MenuItem("Colon Quest/Build Town Scene")]
        public static void BuildTown()
        {
            var scene = BeginScene();
            var map = new GameObject("Map").transform;
            var random = new System.Random(11);
            footprints.Clear();
            buildingSpecs.Clear();

            currentLayer = LayerFloor;
            BuildGround(Group(map, "Ground"), random);
            BuildWorldGround(Group(map, "WorldGround"), random);
            BuildStreets(Group(map, "Streets"));
            BuildAlleyLotAndPark(Group(map, "Lots"));
            currentLayer = LayerWorld;

            var shadows = Group(map, "Shadows");
            BuildBuildings(Group(map, "Buildings"), shadows);
            BuildNature(Group(map, "Nature"), shadows, random);
            BuildStreetFurniture(Group(map, "Furniture"), shadows);
            BuildPlazaAndPark(Group(map, "Landmarks"), shadows);
            BuildAlleyAndLot(Group(map, "AlleyAndLot"), shadows);
            BuildWorldProps(Group(map, "WorldProps"), shadows, random);
            BuildDistricts(map, shadows);
            BuildWorldBorders(Group(map, "Borders"));
            BuildDoorsAndStations(Group(map, "Interactions"));
            BuildPeople(Group(map, "People"));
            BuildCrossings(Group(map, "Crossings"));
            BuildClouds(Group(map, "Clouds"), random);
            var depthShadows = Group(map, "DepthShadows");
            BuildDepth(Group(map, "Depth"), depthShadows, random);
            BuildCity(Group(map, "City"), depthShadows, random);

            BuildSpawnPoint(map, SpawnIds.Start, new Vector2(0f, 2f));
            BuildSpawnPoint(map, SpawnIds.TownFromCasa, new Vector2(-19.5f, 4.3f));
            BuildSpawnPoint(map, SpawnIds.TownFromBanco, new Vector2(-8.5f, 4.3f));
            BuildSpawnPoint(map, SpawnIds.TownFromTienda, new Vector2(8.5f, 4.3f));
            BuildSpawnPoint(map, SpawnIds.TownFromSoda, new Vector2(12.5f, -25.5f));

            var player = BuildPlayer(new Vector2(0f, 2f), true);
            BuildCamera(MapLayout.TownCameraSize, GrassColor, new Vector2(0f, 2f))
                .AddComponent<CameraFollow>().Configure(player, MapLayout.TownBounds);

            new GameObject("GameBootstrap").AddComponent<ColonQuest.UI.GameBootstrap>();

            ClearNewBuildingFootprints(map);
            foreach (var sg in map.GetComponentsInChildren<UnityEngine.Rendering.SortingGroup>(true))
            {
                if (sg != null && sg.GetComponent<YSort>() == null)
                {
                    Object.DestroyImmediate(sg);
                }
            }
            SaveScene(scene, SceneNames.Town);
        }

        private static void BuildGround(Transform parent, System.Random random)
        {
            Shape(parent, "Grass", rounded, MapLayout.TownBounds.center, MapLayout.TownBounds.size, GrassColor, TownGrass);
            if (grassDetailTex != null)
            {
                TiledShape(parent, "Grass_Texture", grassDetailTex, MapLayout.TownBounds.center, MapLayout.TownBounds.size, GamePalette.White, TownGrassPatch);
            }

            // Big soft darker patches and small tufts so the grass is not one flat color.
            var patch = GamePalette.Hex("#58A867");
            patch.a = 0.45f;
            for (int i = 0; i < 42; i++)
            {
                float x = (float)(random.NextDouble() * 88.0 - 44.0);
                float y = (float)(random.NextDouble() * 56.0 - 28.0);
                float size = 6f + (float)random.NextDouble() * 7f;
                Shape(parent, "Patch", softCircle, new Vector2(x, y), new Vector2(size, size * 0.7f), patch, TownGrassPatch);
            }

            var tuft = GamePalette.Hex("#58A867");
            for (int i = 0; i < 300; i++)
            {
                float x = (float)(random.NextDouble() * 88.0 - 44.0);
                float y = (float)(random.NextDouble() * 56.0 - 28.0);
                if (!OnOpenGrass(x, y))
                {
                    continue;
                }

                Shape(parent, "Tuft", circle, new Vector2(x, y), new Vector2(0.34f, 0.2f), tuft, TownGroundDetail);
            }

            Flowers(parent, random, new Rect(-24.5f, 7f, 49f, 5.5f), 40);
            Flowers(parent, random, new Rect(14.5f, -8f, 9.5f, 6f), 26);
        }

        private static void BuildStreets(Transform parent)
        {
            // Sidewalks first, then the roads on top so the road edge reads as a curb.
            // The roads run past the edge of the map, into the fog, so they never seem to end.
            Walk(parent, new Rect(-50f, 4f, 100f, 2f), true);
            Walk(parent, new Rect(-50f, -2f, 100f, 2f), false);
            Walk(parent, new Rect(-50f, -10f, 100f, 2f), true);
            Walk(parent, new Rect(-50f, -16f, 100f, 2f), false);
            CobblestonePlaza(parent, new Rect(-4f, 4f, 8f, 9f));
            CasaGardenPath(parent);

            Road(parent, new Rect(-50f, 0f, 100f, 4f));
            Road(parent, new Rect(-50f, -14f, 100f, 4f));
            Road(parent, new Rect(-2f, -10f, 4f, 10f));

            DashedLine(parent, 2f, -49f, 49f);
            DashedLine(parent, -12f, -49f, 49f);
            Crosswalk(parent, 3.6f, 0f, 4f);
            Crosswalk(parent, -3.6f, 0f, 4f);
            Crosswalk(parent, 3.6f, -14f, -10f);
        }

        private static void Walk(Transform parent, Rect area, bool curbSouth = true)
        {
            Shape(parent, "Walk_Outline", rounded, area.center, area.size + Vector2.one * 0.16f, GamePalette.Outline, TownWalkOutline);
            Shape(parent, "Walk", rounded, area.center, area.size, WalkColor, TownWalk);
            if (sidewalkSlabTex != null)
            {
                TiledShape(parent, "Walk_SlabPattern", sidewalkSlabTex, area.center, area.size - new Vector2(0.12f, 0.08f), GamePalette.White, TownWalkPlate + 1);
            }
            else
            {
                Shape(parent, "Walk_Plate", rounded, area.center, area.size - Vector2.one * 0.4f, GamePalette.Shade(WalkColor, 1.03f), TownWalkPlate);
            }

            if (area.width > area.height)
            {
                // Bold 3D yellow-and-amber painted street curb along the road edge
                float curbBaseY = curbSouth ? area.yMin : area.yMax;
                float dir = curbSouth ? 1f : -1f;

                Shape(parent, "Curb_GutterOutline", square, new Vector2(area.center.x, curbBaseY + dir * 0.03f), new Vector2(area.width - 0.2f, 0.06f), GamePalette.Outline, TownWalkPlate + 2);
                Shape(parent, "Curb_FrontFace", square, new Vector2(area.center.x, curbBaseY + dir * 0.10f), new Vector2(area.width - 0.2f, 0.11f), GamePalette.Hex("#C48B12"), TownWalkPlate + 2);
                Shape(parent, "Curb_TopBevel", square, new Vector2(area.center.x, curbBaseY + dir * 0.20f), new Vector2(area.width - 0.2f, 0.12f), GamePalette.Hex("#F5C42F"), TownWalkPlate + 2);
                Shape(parent, "Curb_TopHighlight", square, new Vector2(area.center.x, curbBaseY + dir * 0.24f), new Vector2(area.width - 0.2f, 0.04f), GamePalette.Hex("#FFE066"), TownWalkPlate + 3);
                Shape(parent, "Curb_InkSeam", square, new Vector2(area.center.x, curbBaseY + dir * 0.27f), new Vector2(area.width - 0.2f, 0.04f), GamePalette.Outline, TownWalkPlate + 3);

                for (float x = area.xMin + 2.0f; x < area.xMax - 1.5f; x += 2.0f)
                {
                    Shape(parent, "Curb_Joint", square, new Vector2(x, curbBaseY + dir * 0.15f), new Vector2(0.05f, 0.24f), GamePalette.Outline, TownWalkPlate + 3);
                }

                // Lush green lawn border with hand-inked scalloped grass blades overlapping the top of the concrete sidewalk
                if (curbSouth && grassEdgeTex != null)
                {
                    if (Mathf.Abs(area.yMin - 4f) < 0.1f)
                    {
                        // Leave [-14..14] open for the Terracotta Flagstone Courtyard in front of Banco, Plaza, and Tienda
                        Rect leftLawn = new Rect(area.xMin, area.yMax - 0.56f, -14.0f - area.xMin, 0.62f);
                        Rect rightLawn = new Rect(14.0f, area.yMax - 0.56f, area.xMax - 14.0f, 0.62f);
                        Shape(parent, "Walk_LawnStripL", square, leftLawn.center, leftLawn.size, GrassColor, TownWalkPlate + 2);
                        Shape(parent, "Walk_LawnStripR", square, rightLawn.center, rightLawn.size, GrassColor, TownWalkPlate + 2);
                        TiledShape(parent, "Walk_GrassEdgeL", grassEdgeTex, new Vector2(leftLawn.center.x, area.yMax - 0.52f), new Vector2(leftLawn.width, 0.56f), GamePalette.White, TownWalkPlate + 3);
                        TiledShape(parent, "Walk_GrassEdgeR", grassEdgeTex, new Vector2(rightLawn.center.x, area.yMax - 0.52f), new Vector2(rightLawn.width, 0.56f), GamePalette.White, TownWalkPlate + 3);
                    }
                    else
                    {
                        TiledShape(parent, "Walk_GrassEdge", grassEdgeTex, new Vector2(area.center.x, area.yMax - 0.18f), new Vector2(area.width - 0.4f, 0.52f), GamePalette.White, TownWalkPlate + 3);
                    }
                }
            }
        }

        private static void CobblestonePlaza(Transform parent, Rect area)
        {
            var borderTerracotta = GamePalette.Hex("#8E4828");
            var baseTerracotta = GamePalette.Hex("#D4794A");

            // Grand Colonial Terracotta Flagstone Courtyard spanning in front of Banco, Central Plaza, and Tienda
            Rect promenade = new Rect(-14.0f, 5.34f, 28.0f, 0.92f);
            Shape(parent, "Promenade_Outline", roundedSmall, promenade.center, promenade.size + new Vector2(0.16f, 0.14f), GamePalette.Outline, TownPlaza);
            Shape(parent, "Promenade_Surface", roundedSmall, promenade.center, promenade.size, baseTerracotta, TownPlaza + 1);
            if (plazaFlagstoneTex != null)
            {
                TiledShape(parent, "Promenade_Flagstones", plazaFlagstoneTex, promenade.center, promenade.size - new Vector2(0.08f, 0.06f), GamePalette.White, TownPlaza + 2);
            }

            Rect centralPlaza = new Rect(area.xMin - 0.25f, 5.34f, area.width + 0.5f, area.yMax - 5.14f);
            Shape(parent, "Plaza_Outline", rounded, centralPlaza.center, centralPlaza.size + Vector2.one * 0.22f, GamePalette.Outline, TownPlaza);
            Shape(parent, "Plaza_Border", rounded, centralPlaza.center, centralPlaza.size, borderTerracotta, TownPlaza + 1);
            Shape(parent, "Plaza_Surface", rounded, centralPlaza.center, centralPlaza.size - Vector2.one * 0.28f, baseTerracotta, TownPlaza + 2);
            if (plazaFlagstoneTex != null)
            {
                TiledShape(parent, "Plaza_Flagstones", plazaFlagstoneTex, centralPlaza.center, centralPlaza.size - Vector2.one * 0.34f, GamePalette.White, TownPlaza + 3);
            }

            // Concentric carved stone rings framing the 3-tier central fountain at (0, 9).
            Vector2 fCenter = new Vector2(0f, 9f);
            Shape(parent, "Plaza_RingOuterInk", circle, fCenter, new Vector2(5.8f, 4.0f), GamePalette.Outline, TownPlaza + 4);
            Shape(parent, "Plaza_RingOuterTerra", circle, fCenter, new Vector2(5.64f, 3.86f), GamePalette.Hex("#C96D3E"), TownPlaza + 5);
            Shape(parent, "Plaza_RingMidInk", circle, fCenter, new Vector2(5.05f, 3.45f), GamePalette.Outline, TownPlaza + 6);
            Shape(parent, "Plaza_RingStone", circle, fCenter, new Vector2(4.90f, 3.32f), GamePalette.Hex("#D8CEBC"), TownPlaza + 7);
            Shape(parent, "Plaza_RingInnerInk", circle, fCenter, new Vector2(4.46f, 3.02f), GamePalette.Outline, TownPlaza + 8);
            Shape(parent, "Plaza_RingInner", circle, fCenter, new Vector2(4.32f, 2.90f), GamePalette.Hex("#EAE2D3"), TownPlaza + 9);
        }

        private static void CasaGardenPath(Transform parent)
        {
            // Warm dirt/sand side trail to the right of Tu Casa (between Casa and Banco, under the tropical tree)
            Rect dirtTrail = new Rect(-15.55f, 5.40f, 1.75f, 7.2f);
            Shape(parent, "DirtTrail_Edge", rounded, dirtTrail.center, dirtTrail.size + new Vector2(0.24f, 0.16f), GamePalette.Hex("#3E8E2C"), TownWalkPlate + 3);
            Shape(parent, "DirtTrail", rounded, dirtTrail.center, dirtTrail.size, GamePalette.Hex("#DFA872"), TownWalkPlate + 4);
            Shape(parent, "DirtTrail_Center", rounded, dirtTrail.center, dirtTrail.size - new Vector2(0.45f, 0.4f), GamePalette.Hex("#E8B986"), TownWalkPlate + 5);

            // Irregular hand-inked grey-cream flagstones leading from the sidewalk gate to Tu Casa's door and curving into the left garden
            (Vector2 pos, Vector2 size, float angle)[] stones =
            {
                (new Vector2(-19.58f, 5.48f), new Vector2(0.72f, 0.28f), -4f),
                (new Vector2(-19.22f, 5.68f), new Vector2(0.56f, 0.24f), 6f),
                (new Vector2(-19.62f, 5.84f), new Vector2(0.64f, 0.25f), -3f),
                // Branch curving left across Tu Casa's recessed left garden
                (new Vector2(-20.45f, 5.64f), new Vector2(0.78f, 0.34f), 12f),
                (new Vector2(-21.25f, 5.88f), new Vector2(0.84f, 0.36f), -8f),
                (new Vector2(-22.05f, 6.08f), new Vector2(0.88f, 0.38f), 14f),
                (new Vector2(-22.75f, 6.38f), new Vector2(0.86f, 0.38f), -6f),
                (new Vector2(-23.05f, 6.82f), new Vector2(0.84f, 0.36f), 4f)
            };

            foreach (var s in stones)
            {
                var o = Shape(parent, "SteppingStone_Outline", roundedSmall, s.pos, s.size + new Vector2(0.12f, 0.10f), GamePalette.Outline, TownWalkPlate + 4);
                var b = Shape(parent, "SteppingStone_Base", roundedSmall, s.pos, s.size, GamePalette.Hex("#A8A396"), TownWalkPlate + 5);
                var t = Shape(parent, "SteppingStone_Top", roundedSmall, s.pos + new Vector2(-0.02f, 0.02f), s.size - new Vector2(0.10f, 0.08f), GamePalette.Hex("#C9C4B6"), TownWalkPlate + 6);
                Tilt(o, s.angle);
                Tilt(b, s.angle);
                Tilt(t, s.angle);
            }

            // Lush 4-leaf clover clusters in Tu Casa's front lawn
            Vector2[] clovers =
            {
                new Vector2(-20.15f, 5.48f), new Vector2(-20.85f, 5.52f), new Vector2(-22.4f, 5.65f),
                new Vector2(-23.3f, 5.75f), new Vector2(-18.5f, 5.62f), new Vector2(-17.4f, 5.68f)
            };
            foreach (var cp in clovers)
            {
                for (int leaf = 0; leaf < 4; leaf++)
                {
                    float a = (leaf * 90f + 25f) * Mathf.Deg2Rad;
                    Vector2 lp = cp + new Vector2(Mathf.Cos(a) * 0.09f, Mathf.Sin(a) * 0.07f);
                    Shape(parent, "Clover_Out", circle, lp, Vector2.one * 0.16f, GamePalette.Outline, TownWalkPlate + 4);
                    Shape(parent, "Clover", circle, lp, Vector2.one * 0.11f, GamePalette.Hex("#3FA834"), TownWalkPlate + 5);
                }
            }
        }

        private static void Road(Transform parent, Rect area)
        {
            Shape(parent, "Road_Outline", rounded, area.center, area.size + Vector2.one * 0.24f, GamePalette.Outline, TownRoadOutline);
            Shape(parent, "Road", rounded, area.center, area.size, RoadColor, TownRoad);

            if (area.width > area.height)
            {
                var wear = GamePalette.Shade(RoadColor, 0.92f);
                Shape(parent, "Road_WearNorth", square, new Vector2(area.center.x, area.center.y + 0.95f), new Vector2(area.width - 0.8f, 0.7f), wear, TownRoad);
                Shape(parent, "Road_WearSouth", square, new Vector2(area.center.x, area.center.y - 0.95f), new Vector2(area.width - 0.8f, 0.7f), wear, TownRoad);
            }
        }

        /// <summary>The dark alley, the gravel lot for the stand, the park path and the pond. All lie on the ground.</summary>
        private static void BuildAlleyLotAndPark(Transform parent)
        {
            // Alley between the grocery and the warehouse.
            Shape(parent, "Alley_Outline", rounded, new Vector2(-14f, -5f), new Vector2(4.16f, 6.16f), GamePalette.Outline, TownAlley);
            Shape(parent, "Alley", rounded, new Vector2(-14f, -5f), new Vector2(4f, 6f), GamePalette.Hex("#3A3F52"), TownAlley + 1);
            var puddle = GamePalette.Hex("#6B8FB8");
            puddle.a = 0.35f;
            Shape(parent, "Puddle", softCircle, new Vector2(-13.2f, -4.3f), new Vector2(1.8f, 1.0f), puddle, TownAlley + 2);

            // Gravel lot where the investment stand is.
            Shape(parent, "Lot_Outline", rounded, new Vector2(7f, -5f), new Vector2(6.16f, 4.96f), GamePalette.Shade(GamePalette.Hex("#CDBF9A"), 0.5f), TownPark);
            Shape(parent, "Lot", rounded, new Vector2(7f, -5f), new Vector2(6f, 4.8f), GamePalette.Hex("#CDBF9A"), TownPark + 1);

            // Park path and pond.
            Shape(parent, "Path_Outline", rounded, new Vector2(19.25f, -3.55f), new Vector2(9.66f, 1.3f), GamePalette.Shade(GamePalette.Hex("#E8D5A3"), 0.5f), TownPark);
            Shape(parent, "Path", rounded, new Vector2(19.25f, -3.55f), new Vector2(9.5f, 1.14f), GamePalette.Hex("#E8D5A3"), TownPark + 1);
            Shape(parent, "Pond_Outline", rounded, new Vector2(19.5f, -5.9f), new Vector2(5.9f, 2.9f), GamePalette.Outline, TownPark);
            Shape(parent, "Pond", rounded, new Vector2(19.5f, -5.9f), new Vector2(5.6f, 2.6f), GamePalette.Hex("#5DB4DF"), TownPark + 1);
            var ripple = GamePalette.White;
            ripple.a = 0.4f;
            Flickering(Shape(parent, "Pond_Ripple", ring, new Vector2(18.5f, -5.9f), new Vector2(1.6f, 0.9f), ripple, TownPark + 2), 0.6f, 1.4f, 5f);
            Flickering(Shape(parent, "Pond_Ripple", ring, new Vector2(21f, -6.2f), new Vector2(1.2f, 0.7f), ripple, TownPark + 2), 0.6f, 1.8f, 12f);
            foreach (var pad in new[] { new Vector2(17.6f, -6.4f), new Vector2(20.6f, -5.4f), new Vector2(21.6f, -6.5f) })
            {
                Shape(parent, "LilyPad", circle, pad, new Vector2(0.55f, 0.4f), GamePalette.Hex("#4CAF6A"), TownPark + 3);
            }
        }

        private static void BuildBuildings(Transform parent, Transform shadows)
        {
            Building(parent, shadows, new BuildingSpec
            {
                Name = "Casa", Sign = "TU CASA", Roof = "#C0504D", Wall = "#F2D7B6",
                XMin = -24f, XMax = -16f, YBottom = 6f, YTop = 12.6f, DoorX = -19.5f, FacadeHeight = 2.8f, Entrance = Entrance.Door
            });
            var banco = Building(parent, shadows, new BuildingSpec
            {
                Name = "Banco", Sign = "BANCO", Roof = "#3B6FB6", Wall = "#E8EEF9",
                XMin = -13.5f, XMax = -4f, YBottom = 6f, YTop = 13.8f, DoorX = -8.5f, FacadeHeight = 3.4f, Entrance = Entrance.Door
            });
            Columns(banco, -8.5f - (-13.5f + -4f) * 0.5f, 3.4f);
            var tienda = Building(parent, shadows, new BuildingSpec
            {
                Name = "Tienda", Sign = "TIENDA", Roof = "#7B5BA8", Wall = "#FFE6A8",
                XMin = 4f, XMax = 13.5f, YBottom = 6f, YTop = 13.4f, DoorX = 8.5f, FacadeHeight = 3.2f, Entrance = Entrance.Door,
                AwningA = "#E8505B", AwningB = "#F8F5E8"
            });
            Banner(tienda, "CRÉDITO FÁCIL", 8.5f - (4f + 13.5f) * 0.5f, 3.2f + 1.7f, "#E8505B");
            Building(parent, shadows, new BuildingSpec
            {
                Name = "Taller", Sign = "TALLER DON RAFA", Roof = "#4B5568", Wall = "#D7DDE8",
                XMin = 16f, XMax = 23.5f, YBottom = 6f, YTop = 12.8f, DoorX = 19.5f, FacadeHeight = 3.0f, Entrance = Entrance.Garage
            });

            Building(parent, shadows, new BuildingSpec
            {
                Name = "Pulperia", Sign = "PULPERIA", Roof = "#3FA66A", Wall = "#FBE3C4",
                XMin = -24.5f, XMax = -16f, YBottom = -8f, YTop = -2.6f, DoorX = -19.5f, FacadeHeight = 2.8f, Entrance = Entrance.Window,
                AwningA = "#3FA66A", AwningB = "#F8F5E8"
            });
            House(parent, shadows, "Casa_Vecina_D", -12f, -5f, -8f, -3f, "#8E6BB8", "#F4EBD0");
            YardSign(parent, shadows, -5.7f, -8.7f, "SE ALQUILA", "#1D4F8C");
        }

        private static void BuildNature(Transform parent, Transform shadows, System.Random random)
        {
            var greens = new[] { "#3FA66A", "#4CB36F", "#3B9660" };

            // Behind the north buildings.
            foreach (float x in new[] { -23f, -18f, -12.5f, -7f, 7f, 13.5f, 18f, 23f })
            {
                Tree(parent, shadows, x, 12.8f + (float)random.NextDouble() * 0.4f, 1.1f, greens[random.Next(3)], random.Next(4) == 0);
            }

            // Between buildings and along the edges.
            Tree(parent, shadows, -14.5f, 8.6f, 1.2f, greens[0], true);
            Tree(parent, shadows, 14.5f, 8.6f, 1.2f, greens[1], true);
            Tree(parent, shadows, -24f, 8.5f, 1.15f, greens[2], false);
            Tree(parent, shadows, 24f, 8.5f, 1.15f, greens[0], false);
            Tree(parent, shadows, -24f, -3f, 1.1f, greens[1], false);
            Tree(parent, shadows, 24f, 0.5f, 1.1f, greens[2], false);
            Tree(parent, shadows, -24f, -12f, 1.15f, greens[0], true);
            Tree(parent, shadows, 24f, -12f, 1.15f, greens[1], false);

            // South edge, in front of the hedge.
            foreach (float x in new[] { -22f, -15f, -8f, 6f, 13f, 20f })
            {
                Tree(parent, shadows, x, -16.4f, 1.1f, greens[random.Next(3)], random.Next(3) == 0);
            }

            // The park.
            Tree(parent, shadows, 15.6f, -7.2f, 1.2f, greens[0], true);
            Tree(parent, shadows, 23.2f, -7.2f, 1.2f, greens[2], false);
            Tree(parent, shadows, 23.6f, -2.2f, 1.1f, greens[1], false);
            Tree(parent, shadows, 14.8f, -2.2f, 1.1f, greens[2], true);
            Tree(parent, shadows, 15.2f, -4.9f, 0.95f, greens[1], false);

            // Plaza corners.
            Tree(parent, shadows, -3.3f, 12.4f, 1.15f, greens[1], false);
            Tree(parent, shadows, 3.3f, 12.4f, 1.15f, greens[0], true);

            // Small bushes in the gardens behind the south buildings.
            foreach (float x in new[] { -21f, -18f, -10f, -7f, 4.6f, 9.6f })
            {
                Bush(parent, shadows, x, -2.7f, 1f);
            }
        }

        private static void BuildStreetFurniture(Transform parent, Transform shadows)
        {
            foreach (float x in new[] { 14.5f, 22.8f })
            {
                Lamp(parent, x, 4.5f);
            }

            foreach (float x in new[] { -25.5f, -6f, 6f, 18f })
            {
                Lamp(parent, x, -0.8f);
            }

            foreach (float x in new[] { -23f, -10f, 0f, 9f, 18f })
            {
                Lamp(parent, x, -9.3f);
            }

            Bench(parent, shadows, -12f, -0.9f);
            Bench(parent, shadows, 12f, -0.9f);

            // Traffic keeps to the right: east along the south lane of each road, west along the north lane.
            // Cars in the same lane share a speed, so none catches up with the one ahead.
            Car(parent, shadows, -20.2f, 1.35f, "#E8505B", 2.4f);
            Car(parent, shadows, 12.5f, 1.35f, "#4DA3FF", 2.4f);
            // The north lane of the main street belongs to the collectors' car, so no other car shares it.
            Car(parent, shadows, -6f, -10.95f, "#55E08F", -2.2f);
            Car(parent, shadows, 8f, -13.05f, "#F1F4FF", 2.6f);
            Car(parent, shadows, -18f, -13.05f, "#FF8A3D", 2.6f);
        }

        private static void BuildPlazaAndPark(Transform parent, Transform shadows)
        {
            Fountain(parent, shadows, new Vector2(0f, 9f));
            PlazaBench(parent, shadows, -2.55f, 10.55f, 20f);
            PlazaBench(parent, shadows, 2.55f, 10.55f, -20f);
            ColonialLamp(parent, -3.45f, 9.6f);
            ColonialLamp(parent, 3.45f, 9.6f);
            PlazaPlanter(parent, shadows, new Vector2(-3.25f, 7.6f));
            PlazaPlanter(parent, shadows, new Vector2(3.25f, 7.6f));
            PlazaPlanter(parent, shadows, new Vector2(-3.55f, 9.3f));
            PlazaPlanter(parent, shadows, new Vector2(3.55f, 9.3f));
            FestoonGarland(parent, new Vector2(-3.45f, 9.6f), new Vector2(3.45f, 9.6f), 2.8f);
            CasaFrontFence(parent, shadows);
            BuildOverheadPowerNetwork(parent, shadows);

            Bench(parent, shadows, 16.5f, -2.9f);
            Bench(parent, shadows, 22.4f, -2.9f);
            Lamp(parent, 19.5f, -2.6f);
        }

        private static void BuildPeople(Transform parent)
        {
            BuildNpc(parent, "DonRafa", new Vector2(21.8f, 4.85f), new Look { Skin = "#3C78D8", Extra = Accessory.Helmet, ExtraColor = "#FFD23F" }, true);
            BuildNpc(parent, "DonaMarta", new Vector2(-17.3f, -8.8f), new Look { Skin = "#F4A6B8", Extra = Accessory.Vest, ExtraColor = "#F1F4FF" }, true);
            BuildNpc(parent, "Prestamista", new Vector2(-14f, -5.3f), new Look { Skin = "#5B4B7A", Extra = Accessory.Hood, ExtraColor = "#2A2140" }, true);
            BuildNpc(parent, "Promotor", new Vector2(9.0f, -5.7f), new Look { Skin = "#2EAA55", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, true);


            // Iconic cast members in the residential and central plaza zones
            var donMario = BuildNpc(parent, "DonMario", new Vector2(-17.1f, 5.05f), new Look { Skin = "#E9E2D0" }, true);
            BuildThing(donMario, "Talk_DonMario", "Saludar a Don Mario", Vector2.zero, new Vector2(0.8f, 0.8f),
                "¡Buenos días, vecino! Qué bonito día hace hoy en el barrio.",
                "Cuidar la casa y llevar las cuentas al día da una paz que no tiene precio.");

            var cajeraExt = BuildNpc(parent, "Cajera", new Vector2(-6.3f, 5.65f), new Look { Skin = "#EAF0F8" }, true);
            BuildThing(cajeraExt, "Talk_Cajera", "Hablar con la asesora del Banco", Vector2.zero, new Vector2(0.8f, 0.8f),
                "¡Bienvenido al Banco! Adentro podés abrir tu cuenta de ahorros o consultar tus cuotas.",
                "Ahorrar aunque sea un poquito cada mes te salva de cualquier imprevisto.");

            var vendedorExt = BuildNpc(parent, "Vendedor", new Vector2(8.5f, 5.45f), new Look { Skin = "#2E86C1" }, true);
            BuildThing(vendedorExt, "Talk_Vendedor", "Hablar con Kevin de la Tienda", Vector2.zero, new Vector2(0.8f, 0.8f),
                "¡Pase adelante! Tenemos fruta fresca afuera y todo para el hogar adentro.",
                "Eso sí: antes de comprar a crédito, fijate bien cuánto terminás pagando en total.");

            var martaPlaza = BuildNpc(parent, "DonaMarta", new Vector2(1.9f, 5.05f), new Look { Skin = "#D9707F" }, true);
            BuildThing(martaPlaza, "Talk_MartaPlaza", "Conversar con Doña Marta", Vector2.zero, new Vector2(0.8f, 0.8f),
                "Vengo de hacer los mandados con mi canasta y mi lista en mano.",
                "El que compra con lista en mano no gasta en lo que no ocupa.");

            BuildPedestrian(parent, "Peaton_F", new Vector2(-25f, -17.8f), new Vector2(38.5f, -17.8f), new Look { Skin = "#4DA3FF", Extra = Accessory.Cap, ExtraColor = "#FFD23F" }, 1.3f);
            BuildPedestrian(parent, "Peaton_G", new Vector2(-46f, 5.2f), new Vector2(-39.5f, 5.2f), new Look { Skin = "#FF7A5C", Extra = Accessory.None }, 1.0f);
            BuildPedestrian(parent, "Peaton_H", new Vector2(41.5f, 5.2f), new Vector2(46f, 5.2f), new Look { Skin = "#B28DFF", Extra = Accessory.Cap, ExtraColor = "#55E08F" }, 1.1f);
            BuildPedestrian(parent, "Peaton_I", new Vector2(-37f, -9.0f), new Vector2(-26f, -9.0f), new Look { Skin = "#F2B84B", Extra = Accessory.Tie, ExtraColor = "#4DA3FF" }, 1.2f);
            BuildPedestrian(parent, "Peaton_A", new Vector2(-24f, 5.2f), new Vector2(-14.5f, 5.2f), new Look { Skin = "#B28DFF", Extra = Accessory.Cap, ExtraColor = "#FF7A5C" }, 1.1f);
            BuildPedestrian(parent, "Peaton_B", new Vector2(24f, -9.0f), new Vector2(14.5f, -9.0f), new Look { Skin = "#55E08F", Extra = Accessory.Cap, ExtraColor = "#4DA3FF" }, 1.2f);
            BuildPedestrian(parent, "Peaton_C", new Vector2(-10f, -1.0f), new Vector2(10f, -1.0f), new Look { Skin = "#FF8FB1", Extra = Accessory.None }, 1.0f);
            BuildPedestrian(parent, "Peaton_D", new Vector2(17.5f, -3.5f), new Vector2(22f, -3.5f), new Look { Skin = "#FFD23F", Extra = Accessory.None }, 0.9f);
            BuildPedestrian(parent, "Peaton_E", new Vector2(-3f, 5.5f), new Vector2(3f, 5.5f), new Look { Skin = "#F2B84B", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, 1.0f);
            BuildPedestrian(parent, "Estudiante", new Vector2(-22f, -9.0f), new Vector2(-12f, -9.0f), new Look { Skin = "#C0392B" }, 1.1f);
            BuildPedestrian(parent, "Repartidor", new Vector2(10f, 5.2f), new Vector2(24f, 5.2f), new Look { Skin = "#F39C12" }, 1.5f);
        }

        /// <summary>Soft dark patches that drift across the town, as if clouds were passing overhead.</summary>
        private static void BuildClouds(Transform parent, System.Random random)
        {
            currentLayer = LayerForeground;
            var layer = ParallaxGroup(parent, "CloudShadows", -0.1f);
            var color = GamePalette.Hex("#0B1630");
            color.a = 0.03f;

            for (int i = 0; i < 20; i++)
            {
                float x = (float)(random.NextDouble() * 160.0 - 80.0);
                float y = (float)(random.NextDouble() * 110.0 - 55.0);
                float size = 8f + (float)random.NextDouble() * 6f;
                var cloud = Shape(layer, "Cloud", softCircle, new Vector2(x, y), new Vector2(size * 1.4f, size), color, OrderDust);
                cloud.gameObject.AddComponent<Drift>().Configure(new Vector2(0.35f + (float)random.NextDouble() * 0.25f, 0.04f), new Vector2(90f, 56f));
            }

            currentLayer = LayerWorld;
        }
    }
}

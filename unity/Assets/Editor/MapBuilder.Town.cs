using UnityEditor;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        private static readonly Color GrassColor = GamePalette.Hex("#6DBE7A");
        private static readonly Color WalkColor = GamePalette.Hex("#D6D9E6");
        private static readonly Color RoadColor = GamePalette.Hex("#545C70");

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
            SaveScene(scene, SceneNames.Town);
        }

        private static void BuildGround(Transform parent, System.Random random)
        {
            Shape(parent, "Grass", rounded, MapLayout.TownBounds.center, MapLayout.TownBounds.size, GrassColor, TownGrass);

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
            Walk(parent, new Rect(-50f, 4f, 100f, 2f));
            Walk(parent, new Rect(-50f, -2f, 100f, 2f));
            Walk(parent, new Rect(-50f, -10f, 100f, 2f));
            Walk(parent, new Rect(-50f, -16f, 100f, 2f));
            Walk(parent, new Rect(-4f, 4f, 8f, 9f));

            Road(parent, new Rect(-50f, 0f, 100f, 4f));
            Road(parent, new Rect(-50f, -14f, 100f, 4f));
            Road(parent, new Rect(-2f, -10f, 4f, 10f));

            DashedLine(parent, 2f, -49f, 49f);
            DashedLine(parent, -12f, -49f, 49f);
            Crosswalk(parent, 3.6f, 0f, 4f);
            Crosswalk(parent, -3.6f, 0f, 4f);
            Crosswalk(parent, 3.6f, -14f, -10f);
        }

        private static void Walk(Transform parent, Rect area)
        {
            Shape(parent, "Walk_Outline", rounded, area.center, area.size + Vector2.one * 0.16f, GamePalette.Shade(WalkColor, 0.45f), TownWalkOutline);
            Shape(parent, "Walk", rounded, area.center, area.size, WalkColor, TownWalk);
            Shape(parent, "Walk_Plate", rounded, area.center, area.size - Vector2.one * 0.5f, GamePalette.Shade(WalkColor, 1.03f), TownWalkPlate);
        }

        private static void Road(Transform parent, Rect area)
        {
            Shape(parent, "Road_Outline", rounded, area.center, area.size + Vector2.one * 0.24f, GamePalette.Outline, TownRoadOutline);
            Shape(parent, "Road", rounded, area.center, area.size, RoadColor, TownRoad);
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
            foreach (float x in new[] { -22.8f, -14.5f, -2.8f, 2.8f, 14.5f, 22.8f })
            {
                Lamp(parent, x, 4.5f);
            }

            foreach (float x in new[] { -18f, -6f, 6f, 18f })
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
            Car(parent, shadows, -21f, 0.95f, "#E8505B", 2.4f);
            Car(parent, shadows, 12.5f, 0.95f, "#4DA3FF", 2.4f);
            // The north lane of the main street belongs to the collectors' car, so no other car shares it.
            Car(parent, shadows, -6f, -10.95f, "#55E08F", -2.2f);
            Car(parent, shadows, 8f, -13.05f, "#F1F4FF", 2.6f);
            Car(parent, shadows, -18f, -13.05f, "#FF8A3D", 2.6f);
        }

        private static void BuildPlazaAndPark(Transform parent, Transform shadows)
        {
            Fountain(parent, shadows, new Vector2(0f, 9f));
            Bench(parent, shadows, -3.0f, 7.6f);
            Bench(parent, shadows, 3.0f, 7.6f);
            Lamp(parent, -3.4f, 5.2f);
            Lamp(parent, 3.4f, 5.2f);

            Bench(parent, shadows, 16.5f, -2.9f);
            Bench(parent, shadows, 22.4f, -2.9f);
            Lamp(parent, 19.5f, -2.6f);
        }

        private static void BuildAlleyAndLot(Transform parent, Transform shadows)
        {
            // Trash cans and crates in the alley.
            foreach (var can in new[] { new Vector2(-15.2f, -4.1f), new Vector2(-12.9f, -6.9f) })
            {
                var root = Root(parent, "TrashCan", can, can.y - 0.4f);
                GroundShadow(shadows, can + new Vector2(0.1f, -0.35f), new Vector2(0.9f, 0.4f), 0.3f);
                Shape(root, "Outline", roundedSmall, new Vector2(0f, 0.3f), new Vector2(0.78f, 0.9f), GamePalette.Outline, 0);
                Shape(root, "Can", roundedSmall, new Vector2(0f, 0.28f), new Vector2(0.64f, 0.76f), GamePalette.Hex("#7E869C"), 1);
                Shape(root, "Lid", roundedSmall, new Vector2(0f, 0.68f), new Vector2(0.76f, 0.2f), GamePalette.Hex("#9AA3B8"), 2);
                root.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.8f);
            }

            var crates = Root(parent, "Crates", new Vector2(-15.1f, -5.6f), -6.0f);
            Shape(crates, "Crate_Outline", rounded, new Vector2(0f, 0.4f), new Vector2(1.14f, 1.14f), GamePalette.Outline, 0);
            Shape(crates, "Crate", rounded, new Vector2(0f, 0.4f), new Vector2(1.0f, 1.0f), GamePalette.Hex("#A9743F"), 1);
            Shape(crates, "Crate_Plank", square, new Vector2(0f, 0.4f), new Vector2(1.0f, 0.06f), GamePalette.Hex("#7A4F2A"), 2);
            Shape(crates, "Crate_Plank", square, new Vector2(0f, 0.4f), new Vector2(0.06f, 1.0f), GamePalette.Hex("#7A4F2A"), 2);
            crates.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1.0f, 0.9f);

            // Neon sign over the alley mouth. It hangs high, so the player always walks under it.
            var sign = Root(parent, "AlleySign", new Vector2(-14f, -2.55f), -9f);
            Shape(sign, "Outline", roundedSmall, Vector2.zero, new Vector2(3.5f, 0.74f), GamePalette.Outline, 0);
            var neon = Shape(sign, "Neon", roundedSmall, Vector2.zero, new Vector2(3.36f, 0.6f), GamePalette.Hex("#8E1F2B"), 1);
            Flickering(neon, 0.45f, 7f, 4f);
            Label(sign, "PRESTAMOS RAPIDOS", Vector2.zero, GamePalette.Hex("#FFD23F"), 0.026f, TextAnchor.MiddleCenter);

            // The stand: striped canopy, a banner and a table with piles of cash.
            var tent = Root(parent, "Stand", new Vector2(7f, -5.2f), -5.2f);
            GroundShadowRect(shadows, new Vector2(7.3f, -4.4f), new Vector2(4.6f, 2.4f), 0.25f);
            Shape(tent, "Pole", square, new Vector2(-2.0f, 1.0f), new Vector2(0.14f, 2.0f), GamePalette.Hex("#6F7890"), 0);
            Shape(tent, "Pole", square, new Vector2(2.0f, 1.0f), new Vector2(0.14f, 2.0f), GamePalette.Hex("#6F7890"), 0);
            Shape(tent, "Canopy_Outline", rounded, new Vector2(0f, 2.25f), new Vector2(4.7f, 1.3f), GamePalette.Outline, 1);
            for (int i = 0; i < 6; i++)
            {
                Shape(tent, "Canopy", square, new Vector2(-2.0f + 0.4f + i * 0.72f, 2.25f), new Vector2(0.72f, 1.14f), GamePalette.Hex(i % 2 == 0 ? "#E8505B" : "#FFD23F"), 2);
            }

            Shape(tent, "Banner_Outline", roundedSmall, new Vector2(0f, 1.2f), new Vector2(4.1f, 0.74f), GamePalette.Outline, 3);
            Shape(tent, "Banner", roundedSmall, new Vector2(0f, 1.2f), new Vector2(3.96f, 0.6f), GamePalette.Hex("#2E7D4F"), 4);
            Label(tent, "INVIERTE YA 300%", new Vector2(0f, 1.2f), GamePalette.White, 0.027f, TextAnchor.MiddleCenter);

            var table = Root(parent, "StandTable", new Vector2(7f, -5.7f), -5.95f);
            Shape(table, "Table_Outline", rounded, new Vector2(0f, 0.3f), new Vector2(2.7f, 0.84f), GamePalette.Outline, 0);
            Shape(table, "Table", rounded, new Vector2(0f, 0.3f), new Vector2(2.54f, 0.68f), GamePalette.Hex("#A9743F"), 1);
            for (int i = 0; i < 3; i++)
            {
                Shape(table, "Cash", roundedSmall, new Vector2(-0.8f + i * 0.8f, 0.4f), new Vector2(0.56f, 0.34f), GamePalette.MoneyIn, 2);
                Shape(table, "Cash_Band", square, new Vector2(-0.8f + i * 0.8f, 0.4f), new Vector2(0.06f, 0.34f), GamePalette.Hex("#2E7D4F"), 3);
            }

            table.gameObject.AddComponent<BoxCollider2D>().offset = new Vector2(0f, 0.3f);
            table.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 0.6f);

            // The man of the stand, behind the table: the player can talk to him, and what he promises is the lesson.
            var seller = BuildNpc(parent, "Estafador", new Vector2(7f, -4.7f), new Look { Skin = "#C98B5C", Extra = Accessory.Tie, ExtraColor = "#2E7D4F" }, true);
            BuildThing(seller, "Talk", "Hablar con el del puesto", Vector2.zero, new Vector2(0.8f, 0.8f),
                "Triplicá tu plata en un mes. Solo traé efectivo y no le contés a nadie.",
                "Es seguro, es seguro. Papeles no tengo, pero confiá en mí.",
                "Con los {cash} que tenés hoy, mañana tendrías el triple. Eso me dijeron mis clientes.",
                "Nadie puede garantizar 300% en un mes. Un negocio así se llama esquema piramidal: gana el que entra primero.");
        }

        private static void BuildDoorsAndStations(Transform parent)
        {
            // Walking up into a building door goes inside.
            Doorway(parent, new Vector2(-19.5f, 5.7f), SceneNames.Casa);
            Doorway(parent, new Vector2(-8.5f, 5.7f), SceneNames.Banco, true);
            Doorway(parent, new Vector2(8.5f, 5.7f), SceneNames.Tienda, true);
            Doorway(parent, new Vector2(12.5f, -24.3f), SceneNames.Soda, true);

            // Out in the street the stations lie on the ground, in front of each place.
            currentLayer = LayerFloor;
            BuildStation(parent, "Station_Garage", StationType.Garage, "Taller de Don Rafa", new Vector2(19.5f, 4.9f));
            BuildStation(parent, "Station_Grocery", StationType.Grocery, "Pulperia", new Vector2(-19.5f, -9.0f));
            BuildStation(parent, "Station_Lender", StationType.Lender, "Prestamos Rapidos", new Vector2(-14f, -3.9f));
            BuildStation(parent, "Station_Pyramid", StationType.Pyramid, "Inversiones Seguras", new Vector2(7f, -6.9f));
            BuildStation(parent, "Station_Coop", StationType.Coop, "Cooperativa de ahorro", new Vector2(-31.5f, 4.9f));
            BuildStation(parent, "Station_Courses", StationType.Courses, "Ver los cursos", new Vector2(0f, 4.6f));
            currentLayer = LayerWorld;
        }

        private static void Doorway(Transform parent, Vector2 position, string targetScene, bool closesAtNight = false)
        {
            var go = new GameObject("Door_" + targetScene);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(1.3f, 1.0f);
            trigger.isTrigger = true;
            var door = go.AddComponent<Door>();
            door.Configure(targetScene, SpawnIds.FromTown, Vector2.up);
            door.SetClosesAtNight(closesAtNight);

            if (closesAtNight)
            {
                var sign = new GameObject("ClosedSign");
                sign.transform.SetParent(go.transform, false);
                sign.transform.localPosition = new Vector2(0f, 1.3f);
                Shape(sign.transform, "SignPlate", roundedSmall, Vector2.zero, new Vector2(1.1f, 0.45f), GamePalette.Hex("#B22222"), 15);
                Shape(sign.transform, "SignBorder", roundedSmall, Vector2.zero, new Vector2(1.18f, 0.53f), GamePalette.Outline, 14);
                Label(sign.transform, "CERRADO", Vector2.zero, GamePalette.White, 0.026f, TextAnchor.MiddleCenter);
                door.AttachClosedSign(sign);
                sign.SetActive(false);
            }
        }

        private static void BuildPeople(Transform parent)
        {
            BuildNpc(parent, "DonRafa", new Vector2(21.8f, 4.85f), new Look { Skin = "#3C78D8", Extra = Accessory.Helmet, ExtraColor = "#FFD23F" }, true);
            BuildNpc(parent, "DonaMarta", new Vector2(-17.3f, -8.8f), new Look { Skin = "#F4A6B8", Extra = Accessory.Vest, ExtraColor = "#F1F4FF" }, true);
            BuildNpc(parent, "Prestamista", new Vector2(-14f, -5.3f), new Look { Skin = "#5B4B7A", Extra = Accessory.Hood, ExtraColor = "#2A2140" }, true);
            BuildNpc(parent, "Promotor", new Vector2(9.0f, -5.7f), new Look { Skin = "#2EAA55", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, true);

            BuildNpc(parent, "AsesoraCoop", new Vector2(-29.6f, 4.85f), new Look { Skin = "#2E8B57", Extra = Accessory.Vest, ExtraColor = "#F1F4FF" }, true);

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
            color.a = 0.12f;

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

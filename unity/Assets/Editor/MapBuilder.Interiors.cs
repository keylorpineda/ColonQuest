using UnityEditor;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The three buildings the player can enter. Each one is laid out like the real thing: the Casa has a
    /// bedroom, a bathroom, a kitchen and a living room; the Banco has a teller line, a queue, a waiting
    /// area and offices; the Tienda sells phones, screens, white goods and motorbikes. The furniture of
    /// each one lives in its own file. Coordinates are in room space, with the origin in the middle.
    /// </summary>
    public static partial class MapBuilder
    {
        private const float InnerWallThickness = 0.32f;
        private const float DoorGap = 1.3f;

        private struct Room
        {
            public string Name;
            public Vector2 Center;
            public Vector2 Size;
            public Color Color;
        }

        [MenuItem("Colon Quest/Build Interior Scenes")]
        public static void BuildInteriors()
        {
            BuildInterior(SceneNames.Casa);
            BuildInterior(SceneNames.Cuarto);
            BuildInterior(SceneNames.Tienda);
            BuildInterior(SceneNames.Banco);
            BuildInterior(SceneNames.Agencia);
            BuildInterior(SceneNames.Carcel);
            BuildInterior(SceneNames.Hospital);
            BuildInterior(SceneNames.HospitalArriba);
            BuildInterior(SceneNames.Vecino);
            BuildInterior(SceneNames.Finca);
            BuildInterior(SceneNames.Inmobiliaria);
            BuildInterior(SceneNames.Soda);
            BuildInterior(SceneNames.Cooperativa);
        }

        private static Room RoomFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return new Room { Name = "Casa", Center = Vector2.zero, Size = MapLayout.CasaSize, Color = GamePalette.FloorHome };
                case SceneNames.Cuarto:
                    return new Room { Name = "Cuarto", Center = Vector2.zero, Size = MapLayout.CuartoSize, Color = GamePalette.Hex("#B5A588") };
                case SceneNames.Tienda:
                    return new Room { Name = "Tienda", Center = Vector2.zero, Size = MapLayout.TiendaSize, Color = GamePalette.FloorStore };
                case SceneNames.Agencia:
                    return new Room { Name = "Agencia", Center = Vector2.zero, Size = MapLayout.AgenciaSize, Color = GamePalette.Hex("#CFD6E6") };
                case SceneNames.Carcel:
                    return new Room { Name = "Carcel", Center = Vector2.zero, Size = MapLayout.CarcelSize, Color = GamePalette.Hex("#A7ADB8") };
                case SceneNames.Hospital:
                    return new Room { Name = "Hospital", Center = Vector2.zero, Size = MapLayout.HospitalSize, Color = GamePalette.Hex("#E3F1F2") };
                case SceneNames.HospitalArriba:
                    return new Room { Name = "HospitalArriba", Center = Vector2.zero, Size = MapLayout.HospitalArribaSize, Color = GamePalette.Hex("#DCEBF5") };
                case SceneNames.Vecino:
                    return new Room { Name = "Vecino", Center = Vector2.zero, Size = MapLayout.VecinoSize, Color = GamePalette.Hex("#CFA97A") };
                case SceneNames.Finca:
                    return new Room { Name = "Finca", Center = Vector2.zero, Size = MapLayout.FincaSize, Color = GamePalette.Hex("#B98C5A") };
                case SceneNames.Inmobiliaria:
                    return new Room { Name = "Inmobiliaria", Center = Vector2.zero, Size = MapLayout.InmobiliariaSize, Color = GamePalette.Hex("#D9E6EA") };
                case SceneNames.Cooperativa:
                    return new Room { Name = "Cooperativa", Center = Vector2.zero, Size = MapLayout.CooperativaSize, Color = GamePalette.Hex("#E6EFE3") };
                case SceneNames.Soda:
                    return new Room { Name = "Soda", Center = Vector2.zero, Size = MapLayout.SodaSize, Color = GamePalette.Hex("#F7E5C8") };
                default:
                    return new Room { Name = "Banco", Center = Vector2.zero, Size = MapLayout.BancoSize, Color = GamePalette.FloorBank };
            }
        }

        /// <summary>Where the doormat lies: walking onto it leaves the building.</summary>
        private static Vector2 MatFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return MapLayout.CasaMat;
                case SceneNames.Cuarto:
                    return MapLayout.CuartoMat;
                case SceneNames.Tienda:
                    return MapLayout.TiendaMat;
                case SceneNames.Agencia:
                    return MapLayout.AgenciaMat;
                case SceneNames.Carcel:
                    return MapLayout.CarcelMat;
                case SceneNames.Hospital:
                    return MapLayout.HospitalMat;
                case SceneNames.HospitalArriba:
                    return MapLayout.HospitalArribaMat;
                case SceneNames.Vecino:
                    return MapLayout.VecinoMat;
                case SceneNames.Finca:
                    return MapLayout.FincaMat;
                case SceneNames.Inmobiliaria:
                    return MapLayout.InmobiliariaMat;
                case SceneNames.Soda:
                    return MapLayout.SodaMat;
                case SceneNames.Cooperativa:
                    return MapLayout.CooperativaMat;
                default:
                    return MapLayout.BancoMat;
            }
        }

        private static string TownSpawnFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return SpawnIds.TownFromCasa;
                case SceneNames.Cuarto:
                    return SpawnIds.TownFromCuarto;
                case SceneNames.Tienda:
                    return SpawnIds.TownFromTienda;
                case SceneNames.Agencia:
                    return SpawnIds.TownFromAgencia;
                case SceneNames.Carcel:
                    return SpawnIds.TownFromComisaria;
                case SceneNames.Hospital:
                    return SpawnIds.TownFromHospital;
                case SceneNames.HospitalArriba:
                    return SpawnIds.HospitalFromAbove;
                case SceneNames.Vecino:
                    return SpawnIds.TownFromVecino;
                case SceneNames.Finca:
                    return SpawnIds.TownFromFinca;
                case SceneNames.Inmobiliaria:
                    return SpawnIds.TownFromInmobiliaria;
                case SceneNames.Soda:
                    return SpawnIds.TownFromSoda;
                case SceneNames.Cooperativa:
                    return SpawnIds.TownFromCooperativa;
                default:
                    return SpawnIds.TownFromBanco;
            }
        }

        private static void BuildInterior(string sceneName)
        {
            var scene = BeginScene();
            var map = new GameObject("Map").transform;
            BuildBackground(Group(map, "Background"));

            var room = RoomFor(sceneName);
            currentLayer = LayerFloor;
            BuildFloors(Group(map, "Floors"), new[] { room });
            bool upperFloor = sceneName == SceneNames.HospitalArriba;
            if (!upperFloor)
            {
                BuildDoorMat(Group(map, "Door"), MatFor(sceneName), TownSpawnFor(sceneName), SceneNames.Town);
            }
            currentLayer = LayerWorld;

            var layout = new InteriorParts
            {
                Floor = Group(map, "FloorZones"),
                Walls = Group(map, "Walls"),
                Props = Group(map, "Props"),
                Stations = Group(map, "Stations"),
                Lights = Group(map, "Lights"),
                People = Group(map, "People"),
                Labels = Group(map, "Labels")
            };

            interiorLayers = 0;
            switch (sceneName)
            {
                case SceneNames.Casa:
                    BuildHouseLayout(layout, room);
                    BuildHouseThings(layout);
                    break;
                case SceneNames.Cuarto:
                    BuildRoomLayout(layout, room);
                    break;
                case SceneNames.Tienda:
                    BuildStoreLayout(layout, room);
                    BuildStoreThings(layout);
                    break;
                case SceneNames.Agencia:
                    BuildAgencyLayout(layout, room);
                    break;
                case SceneNames.Carcel:
                    BuildJailLayout(layout, room);
                    break;
                case SceneNames.Hospital:
                    BuildHospitalLayout(layout, room);
                    break;
                case SceneNames.HospitalArriba:
                    BuildHospitalUpperLayout(layout, room);
                    break;
                case SceneNames.Vecino:
                    BuildNeighborLayout(layout, room);
                    break;
                case SceneNames.Finca:
                    BuildFarmLayout(layout, room);
                    break;
                case SceneNames.Inmobiliaria:
                    BuildRealEstateLayout(layout, room);
                    break;
                case SceneNames.Cooperativa:
                    BuildCoopLayout(layout, room);
                    break;
                case SceneNames.Soda:
                    BuildSodaLayout(layout, room);
                    break;
                default:
                    BuildBankLayout(layout, room);
                    BuildBankThings(layout);
                    break;
            }

            if (upperFloor)
            {
                // The upper floor of the hospital has no way out but the elevator down, where the mat used to be.
                var mat = MatFor(sceneName);
                BuildElevator(layout, mat + new Vector2(0f, -0.45f), mat, new Vector2(1.6f, 0.8f),
                    SceneNames.Hospital, TownSpawnFor(sceneName), Vector2.down, mat + new Vector2(0f, -0.4f));
            }
            else
            {
                var exit = MatFor(sceneName) + new Vector2(0f, -0.75f);
                Deco(layout.Labels, "ExitSign_Outline", roundedSmall, exit.x, exit.y, 1.14f, 0.42f, "#1C2233", 1f, OrderLabel - 2);
                Deco(layout.Labels, "ExitSign_Frame", roundedSmall, exit.x, exit.y, 1.06f, 0.34f, "#2E9E62", 1f, OrderLabel - 1);
                Deco(layout.Labels, "ExitSign", roundedSmall, exit.x, exit.y, 0.96f, 0.26f, "#55E08F", 1f, OrderLabel - 1);
                Label(layout.Labels, "SALIDA", exit, GamePalette.Outline, 0.03f, TextAnchor.MiddleCenter);
            }
            DustMotes(IL(layout.Props, "DustMotes"), new System.Random(5), room, 18);

            BuildRoomColliders(Group(map, "Colliders"), room);

            Vector2 spawnAt = MatFor(sceneName) + new Vector2(0f, 1.3f);
            BuildSpawnPoint(map, SpawnIds.FromTown, spawnAt);
            BuildExtraSpawns(map, sceneName);

            var player = BuildPlayer(spawnAt, false);
            var half = room.Size * 0.5f + Vector2.one * 0.3f;
            var bounds = new Rect(room.Center - half, half * 2f);
            BuildCamera(MapLayout.InteriorCameraSize, GamePalette.Space, room.Center)
                .AddComponent<CameraFollow>().Configure(player, bounds);

            Debug.Log(sceneName + " detail layers: " + interiorLayers);
            SaveScene(scene, sceneName);
        }

        /// <summary>The groups every interior fills.</summary>
        private sealed class InteriorParts
        {
            public Transform Floor;
            public Transform Walls;
            public Transform Props;
            public Transform Stations;
            public Transform Lights;
            public Transform People;
            public Transform Labels;
        }

        /// <summary>The four walls of the room, drawn by the outline and blocked by colliders.</summary>
        private static void BuildRoomColliders(Transform parent, Room room)
        {
            float left = room.Center.x - room.Size.x * 0.5f;
            float right = room.Center.x + room.Size.x * 0.5f;
            float bottom = room.Center.y - room.Size.y * 0.5f;
            float top = room.Center.y + room.Size.y * 0.5f;

            VerticalCollider(parent, left, bottom, top);
            VerticalCollider(parent, right, bottom, top);
            HorizontalCollider(parent, left, right, bottom);
            HorizontalCollider(parent, left, right, top);
        }

        /// <summary>A red doormat on the floor and the trigger that leads back to the town.</summary>
        private static void BuildDoorMat(Transform parent, Vector2 position, string townSpawn, string exitScene)
        {
            Shape(parent, "Mat_Outline", roundedSmall, position, new Vector2(1.7f, 0.7f), GamePalette.Outline, OrderRug + 4);
            Shape(parent, "Mat", roundedSmall, position, new Vector2(1.56f, 0.56f), GamePalette.Hex("#8B3A3A"), OrderRug + 5);
            Shape(parent, "Mat_Stripe", roundedSmall, position, new Vector2(1.2f, 0.2f), GamePalette.Marker, OrderRug + 6);

            var trigger = new GameObject("Door_ToTown");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = position;
            trigger.AddComponent<BoxCollider2D>().size = new Vector2(1.6f, 0.8f);
            var door = trigger.AddComponent<Door>();
            door.Configure(exitScene, townSpawn);
            if (townSpawn == SpawnIds.TownFromVecino || townSpawn == SpawnIds.TownFromCasa)
            {
                door.RememberReturn();
            }

            trigger.GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private static void BuildFloors(Transform parent, Room[] rooms)
        {
            // One merged dark silhouette behind every floor gives the single thick outer wall.
            foreach (var room in rooms)
            {
                Shape(parent, room.Name + "_Outline", rounded, room.Center,
                    room.Size + Vector2.one * (OutlineThickness * 2f), GamePalette.Outline, OrderOutline);
            }

            // Each room takes four sorting orders (floor, plate, tile joints, wall face)
            // so later rooms cover earlier ones.
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                int order = OrderFloor + i * FloorOrdersPerRoom;
                Shape(parent, room.Name + "_Floor", rounded, room.Center, room.Size, room.Color, order);
                Shape(parent, room.Name + "_Plate", rounded, room.Center,
                    room.Size - Vector2.one, GamePalette.Shade(room.Color, 0.94f), order + 1);
                TileJoints(parent, room, order + 2);

                WallFace(parent, room, order + 3);
            }
        }

        /// <summary>Faint lines every unit across the plate, so the floors read as tiles.</summary>
        private static void TileJoints(Transform parent, Room room, int order)
        {
            var color = GamePalette.Shade(room.Color, 0.84f);
            color.a = 0.5f;

            Vector2 half = (room.Size - Vector2.one) * 0.5f;
            for (float x = Mathf.Ceil(room.Center.x - half.x); x <= room.Center.x + half.x; x += 1f)
            {
                Shape(parent, room.Name + "_JointV", square, new Vector2(x, room.Center.y), new Vector2(0.04f, half.y * 2f), color, order);
            }

            for (float y = Mathf.Ceil(room.Center.y - half.y); y <= room.Center.y + half.y; y += 1f)
            {
                Shape(parent, room.Name + "_JointH", square, new Vector2(room.Center.x, y), new Vector2(half.x * 2f, 0.04f), color, order);
            }
        }

        /// <summary>The visible face of the back wall: a darker band with crown molding, wainscot and baseboard.</summary>
        private static void WallFace(Transform parent, Room room, int order)
        {
            float top = room.Center.y + room.Size.y * 0.5f;
            Shape(parent, room.Name + "_WallFace", roundedSmall, new Vector2(room.Center.x, top - 0.4f),
                new Vector2(room.Size.x, 0.8f), GamePalette.Shade(room.Color, 0.68f), order);
            Shape(parent, room.Name + "_CrownMolding", square, new Vector2(room.Center.x, top - 0.06f),
                new Vector2(room.Size.x - 0.2f, 0.08f), GamePalette.Shade(room.Color, 0.52f), order);
            Shape(parent, room.Name + "_Wainscot", square, new Vector2(room.Center.x, top - 0.62f),
                new Vector2(room.Size.x - 0.24f, 0.28f), GamePalette.Shade(room.Color, 0.58f), order);
            Shape(parent, room.Name + "_Baseboard", square, new Vector2(room.Center.x, top - 0.78f),
                new Vector2(room.Size.x - 0.24f, 0.09f), GamePalette.Shade(room.Color, 0.42f), order);
        }

        // ---------------------------------------------------------------- room structure

        /// <summary>
        /// A floor for one part of the building (wood for a bedroom, tiles for a kitchen) laid over the base
        /// floor, with joints that show planks or tiles.
        /// </summary>
        private static void ZoneFloor(Transform parent, string name, Rect area, string hex, float jointStep, bool planks)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            var zone = IL(parent, name);
            var color = GamePalette.Hex(hex);
            Shape(zone, "Floor", square, area.center, area.size, color, OrderRug - 6);

            var joint = GamePalette.Shade(color, 0.82f);
            joint.a = 0.6f;
            for (float y = area.yMin + jointStep; y < area.yMax - 0.01f; y += jointStep)
            {
                Shape(zone, "JointH", square, new Vector2(area.center.x, y), new Vector2(area.width, 0.04f), joint, OrderRug - 5);
            }

            if (planks)
            {
                // Plank ends are staggered from one row to the next, with warm alternating plank tint.
                var altTint = GamePalette.Shade(color, 0.95f);
                altTint.a = 0.45f;
                int row = 0;
                for (float y = area.yMin; y < area.yMax - 0.01f; y += jointStep, row++)
                {
                    if (row % 2 == 1)
                    {
                        float h = Mathf.Min(jointStep - 0.04f, area.yMax - y);
                        if (h > 0.05f)
                        {
                            Shape(zone, "PlankTone", square, new Vector2(area.center.x, y + h * 0.5f), new Vector2(area.width, h), altTint, OrderRug - 5);
                        }
                    }

                    for (float x = area.xMin + (row % 2 == 0 ? 1.2f : 2.4f); x < area.xMax - 0.1f; x += 2.4f)
                    {
                        Shape(zone, "PlankEnd", square, new Vector2(x, y + jointStep * 0.5f), new Vector2(0.04f, jointStep), joint, OrderRug - 5);
                    }
                }
            }
            else
            {
                for (float x = area.xMin + jointStep; x < area.xMax - 0.01f; x += jointStep)
                {
                    Shape(zone, "JointV", square, new Vector2(x, area.center.y), new Vector2(0.04f, area.height), joint, OrderRug - 5);
                }
            }

            currentLayer = previous;
        }

        /// <summary>A wall inside the building: a dark top, a light face below it when it runs across, and a collider.</summary>
        private static void InnerWall(Transform parent, Vector2 from, Vector2 to, string faceHex)
        {
            bool horizontal = Mathf.Abs(to.y - from.y) < 0.01f;
            var center = (from + to) * 0.5f;
            var size = horizontal
                ? new Vector2(Mathf.Abs(to.x - from.x), InnerWallThickness)
                : new Vector2(InnerWallThickness, Mathf.Abs(to.y - from.y));

            Shape(parent, "Wall", square, center, size, GamePalette.Outline, OrderWall);
            if (horizontal)
            {
                var faceColor = GamePalette.Hex(faceHex);
                Shape(parent, "WallFace", square, center + new Vector2(0f, -InnerWallThickness * 0.5f - 0.14f), new Vector2(size.x, 0.28f), faceColor, OrderWall);
                Shape(parent, "WallBaseboard", square, center + new Vector2(0f, -InnerWallThickness * 0.5f - 0.25f), new Vector2(size.x, 0.06f), GamePalette.Shade(faceColor, 0.68f), OrderWall);
            }

            Wall(parent, "WallCollider", center, size);
        }

        /// <summary>A wall that runs across from <paramref name="xMin"/> to <paramref name="xMax"/>, with a doorway at each given x.</summary>
        private static void WallWithDoors(Transform parent, float y, float xMin, float xMax, string faceHex, params float[] doors)
        {
            float start = xMin;
            foreach (float door in doors)
            {
                float gapStart = door - DoorGap * 0.5f;
                if (gapStart > start)
                {
                    InnerWall(parent, new Vector2(start, y), new Vector2(gapStart, y), faceHex);
                }

                // A wooden threshold marks the doorway on the floor.
                OnGroundThreshold(parent, new Vector2(door, y), DoorGap);
                start = door + DoorGap * 0.5f;
            }

            if (xMax > start)
            {
                InnerWall(parent, new Vector2(start, y), new Vector2(xMax, y), faceHex);
            }
        }

        private static void OnGroundThreshold(Transform parent, Vector2 at, float width)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            Shape(parent, "Threshold_Outline", square, at, new Vector2(width, InnerWallThickness + 0.06f), GamePalette.Outline, OrderRug + 2);
            Shape(parent, "Threshold", square, at, new Vector2(width, InnerWallThickness), GamePalette.Hex("#9C6B43"), OrderRug + 3);
            currentLayer = previous;
        }

        /// <summary>A small plate with text on a wall, such as a room name or a teller number.</summary>
        private static void WallPlate(Transform parent, string text, Vector2 at, float width, string plateHex, Color textColor, float textSize = 0.032f)
        {
            Deco(parent, "Plate_Shadow", roundedSmall, at.x + 0.03f, at.y - 0.03f, width + 0.14f, 0.46f, "#1C2233", 0.35f, OrderLabel - 3);
            Deco(parent, "Plate_Outline", roundedSmall, at.x, at.y, width + 0.14f, 0.46f, "#2A3345", 1f, OrderLabel - 2);
            Deco(parent, "Plate_Frame", roundedSmall, at.x, at.y, width + 0.06f, 0.38f, "#8B5A3C", 1f, OrderLabel - 2);
            Deco(parent, "Plate", roundedSmall, at.x, at.y, width - 0.04f, 0.3f, plateHex, 1f, OrderLabel - 1);
            Label(parent, text, at, textColor, textSize, TextAnchor.MiddleCenter);
        }

        private static void Window(Transform parent, float x, float y, float width)
        {
            Deco(parent, "Window_Frame", rounded, x, y, width, 0.52f, "#2A3345");
            Deco(parent, "Window_Wood", rounded, x, y, width - 0.08f, 0.44f, "#8B5A3C", 1f, 7);
            Deco(parent, "Window_Glass", rounded, x, y, width - 0.18f, 0.34f, "#BFE3F5", 1f, 8);
            Deco(parent, "Window_Shine", roundedSmall, x - width * 0.22f, y + 0.04f, width * 0.22f, 0.18f, "#FFFFFF", 0.45f, 8);
            Deco(parent, "Window_Bar", square, x, y, 0.05f, 0.34f, "#2A3345", 1f, 9);
            Deco(parent, "Window_Sill", roundedSmall, x, y - 0.24f, width + 0.14f, 0.1f, "#A9743F", 1f, 9);
        }
    }
}

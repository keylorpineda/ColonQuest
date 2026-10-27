using UnityEditor;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
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
            BuildInterior(SceneNames.Tienda);
            BuildInterior(SceneNames.Banco);
        }

        private static Room RoomFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return new Room { Name = "Casa", Center = new Vector2(-7f, 3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorHome };
                case SceneNames.Tienda:
                    return new Room { Name = "Tienda", Center = new Vector2(7f, 3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorStore };
                default:
                    return new Room { Name = "Banco", Center = new Vector2(0f, -3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorBank };
            }
        }

        /// <summary>Where the doormat lies: walking onto it leaves the building.</summary>
        private static Vector2 MatFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return new Vector2(-7f, 1.0f);
                case SceneNames.Tienda:
                    return new Vector2(7f, 1.0f);
                default:
                    return new Vector2(2.4f, -5.6f);
            }
        }

        private static string TownSpawnFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return SpawnIds.TownFromCasa;
                case SceneNames.Tienda:
                    return SpawnIds.TownFromTienda;
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
            BuildDoorMat(Group(map, "Door"), MatFor(sceneName), TownSpawnFor(sceneName));
            currentLayer = LayerWorld;

            var props = Group(map, "Props");
            var stations = Group(map, "Stations");
            var lights = Group(map, "Lights");
            var people = Group(map, "People");
            var labels = Group(map, "Labels");

            switch (sceneName)
            {
                case SceneNames.Casa:
                    BuildHome(props);
                    BuildStation(stations, "Station_Home", StationType.Home, "Casa", new Vector2(-4.6f, 3.9f));
                    Label(labels, "CASA", new Vector2(-10.1f, 1.95f), GamePalette.Shade(room.Color, 0.62f));
                    break;
                case SceneNames.Tienda:
                    BuildStore(props);
                    BuildStation(stations, "Station_Store", StationType.Store, "Tienda", new Vector2(8.9f, 2.9f));
                    Flickering(Glow(lights, "Light_Tv", new Vector2(4.8f, 3.8f), 3.2f, "#4DA3FF", 0.22f, OrderLight), 0.4f, 9f, 23f);
                    Label(labels, "TIENDA", new Vector2(3.9f, 1.95f), GamePalette.Shade(room.Color, 0.62f));
                    BuildNpc(people, "Vendedor", new Vector2(8.85f, 5.2f), new Look { Skin = "#F2B84B", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, false);
                    break;
                default:
                    BuildBank(props);
                    BuildStation(stations, "Station_Bank", StationType.Bank, "Cajero", new Vector2(0f, -3.4f));
                    Flickering(Glow(lights, "Light_Atm", new Vector2(0f, -4.8f), 3.2f, "#55E08F", 0.26f, OrderLight), 0.18f, 2.5f, 11f);
                    Flickering(Glow(lights, "Light_DeskLamp", new Vector2(-2.5f, -3.4f), 3.4f, "#FFD23F", 0.26f, OrderLight), 0.08f, 1.2f, 37f);
                    Label(labels, "BANCO", new Vector2(-3.1f, -1.95f), GamePalette.Shade(room.Color, 0.62f));
                    BuildNpc(people, "Cajera", new Vector2(-1.45f, -4.3f), new Look { Skin = "#9AD0A8", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, false);
                    break;
            }

            BuildRoomColliders(Group(map, "Colliders"), room);

            Vector2 spawnAt = MatFor(sceneName) + new Vector2(0f, 1.3f);
            BuildSpawnPoint(map, SpawnIds.FromTown, spawnAt);

            var player = BuildPlayer(spawnAt, false);
            var half = room.Size * 0.5f + Vector2.one * 0.3f;
            var bounds = new Rect(room.Center - half, half * 2f);
            BuildCamera(MapLayout.InteriorCameraSize, GamePalette.Space, room.Center)
                .AddComponent<CameraFollow>().Configure(player, bounds);

            SaveScene(scene, sceneName);
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
        private static void BuildDoorMat(Transform parent, Vector2 position, string townSpawn)
        {
            Shape(parent, "Mat_Outline", roundedSmall, position, new Vector2(1.7f, 0.7f), GamePalette.Outline, OrderRug + 4);
            Shape(parent, "Mat", roundedSmall, position, new Vector2(1.56f, 0.56f), GamePalette.Hex("#8B3A3A"), OrderRug + 5);
            Shape(parent, "Mat_Stripe", roundedSmall, position, new Vector2(1.2f, 0.2f), GamePalette.Marker, OrderRug + 6);

            var trigger = new GameObject("Door_ToTown");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = position;
            trigger.AddComponent<BoxCollider2D>().size = new Vector2(1.6f, 0.8f);
            trigger.AddComponent<Door>().Configure(SceneNames.Town, townSpawn);
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

        /// <summary>The visible face of the back wall: a darker band with a baseboard line.</summary>
        private static void WallFace(Transform parent, Room room, int order)
        {
            float top = room.Center.y + room.Size.y * 0.5f;
            Shape(parent, room.Name + "_WallFace", roundedSmall, new Vector2(room.Center.x, top - 0.4f),
                new Vector2(room.Size.x, 0.8f), GamePalette.Shade(room.Color, 0.66f), order);
            Shape(parent, room.Name + "_Baseboard", square, new Vector2(room.Center.x, top - 0.78f),
                new Vector2(room.Size.x - 0.3f, 0.07f), GamePalette.Shade(room.Color, 0.45f), order);
        }

        private static void BuildHome(Transform parent)
        {
            var home = Group(parent, "Casa");
            // The rug lies on the floor, so it belongs to the floor layer, below every shadow.
            currentLayer = LayerFloor;
            Shape(home, "Rug", rounded, new Vector2(-7.2f, 3.4f), new Vector2(3.4f, 2.2f), GamePalette.Debt, OrderRug);
            Shape(home, "Rug_Stripe", rounded, new Vector2(-7.2f, 3.4f), new Vector2(2.6f, 1.4f), GamePalette.Shade(GamePalette.Debt, 0.82f), OrderRug + 1);
            currentLayer = LayerWorld;
            Prop(home, "Bed", rounded, new Vector2(-9.3f, 4.9f), new Vector2(2.0f, 1.8f), GamePalette.Alert);
            Prop(home, "Pillow", roundedSmall, new Vector2(-9.9f, 4.9f), new Vector2(0.6f, 1.0f), GamePalette.White, OrderProp + 2);
            Prop(home, "Wardrobe", rounded, new Vector2(-7.1f, 5.6f), new Vector2(1.8f, 1.0f), GamePalette.Hex("#6A4A8C"));
            Prop(home, "Table", rounded, new Vector2(-4.6f, 4.9f), new Vector2(2.0f, 1.0f), GamePalette.Hex("#8B5A3C"));
            Prop(home, "Bill_A", roundedSmall, new Vector2(-5.0f, 4.95f), new Vector2(0.55f, 0.4f), GamePalette.White, OrderProp + 2);
            Prop(home, "Bill_B", roundedSmall, new Vector2(-4.3f, 4.85f), new Vector2(0.55f, 0.4f), GamePalette.White, OrderProp + 2);
        }

        private static void BuildStore(Transform parent)
        {
            var store = Group(parent, "Tienda");
            Prop(store, "ShelfLeft", rounded, new Vector2(5.2f, 5.5f), new Vector2(2.2f, 0.9f), GamePalette.Hex("#C9753A"));
            Prop(store, "ShelfRight", rounded, new Vector2(7.4f, 5.5f), new Vector2(1.6f, 0.9f), GamePalette.Hex("#C9753A"));
            Prop(store, "Phone_A", roundedSmall, new Vector2(4.7f, 5.55f), new Vector2(0.4f, 0.5f), GamePalette.Marker, OrderProp + 2);
            Prop(store, "Phone_B", roundedSmall, new Vector2(5.3f, 5.55f), new Vector2(0.4f, 0.5f), GamePalette.Debt, OrderProp + 2);
            Prop(store, "Phone_C", roundedSmall, new Vector2(5.9f, 5.55f), new Vector2(0.4f, 0.5f), GamePalette.White, OrderProp + 2);
            Prop(store, "Fridge", rounded, new Vector2(9.9f, 5.3f), new Vector2(0.9f, 1.4f), GamePalette.Hex("#DFE8F5"));
            Prop(store, "Counter", rounded, new Vector2(8.9f, 4.1f), new Vector2(2.4f, 1.0f), GamePalette.Hex("#D6B656"));
            Prop(store, "Register", roundedSmall, new Vector2(9.5f, 4.2f), new Vector2(0.7f, 0.5f), GamePalette.Wall, OrderProp + 2);
            Prop(store, "TV", rounded, new Vector2(4.8f, 3.8f), new Vector2(1.4f, 0.9f), GamePalette.Wall);
            Shape(store, "TV_Screen", roundedSmall, new Vector2(4.8f, 3.8f), new Vector2(1.1f, 0.65f), GamePalette.Alert, OrderProp + 2);
        }

        private static void BuildBank(Transform parent)
        {
            var bank = Group(parent, "Banco");
            Prop(bank, "AtmBody", rounded, new Vector2(0f, -5.1f), new Vector2(1.6f, 1.8f), GamePalette.Hex("#4B5568"));
            Prop(bank, "AtmScreen", roundedSmall, new Vector2(0f, -4.8f), new Vector2(1.0f, 0.7f), GamePalette.MoneyIn, OrderProp + 2);
            Prop(bank, "AtmKeypad", roundedSmall, new Vector2(0f, -5.55f), new Vector2(0.8f, 0.3f), GamePalette.Hex("#2A3040"), OrderProp + 2);
            Prop(bank, "Desk", rounded, new Vector2(-2.5f, -4.2f), new Vector2(1.2f, 2.6f), GamePalette.Hex("#7A6A52"));
            Prop(bank, "DeskLamp", circle, new Vector2(-2.5f, -3.4f), Vector2.one * 0.45f, GamePalette.Marker, OrderProp + 2);
            Prop(bank, "PlantPot", roundedSmall, new Vector2(2.8f, -3.1f), new Vector2(0.7f, 0.6f), GamePalette.Hex("#C26A3A"));
            Prop(bank, "Plant", circle, new Vector2(2.8f, -2.7f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
        }
    }
}

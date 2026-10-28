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
                    return new Room { Name = "Casa", Center = Vector2.zero, Size = MapLayout.CasaSize, Color = GamePalette.FloorHome };
                case SceneNames.Tienda:
                    return new Room { Name = "Tienda", Center = Vector2.zero, Size = MapLayout.TiendaSize, Color = GamePalette.FloorStore };
                default:
                    return new Room { Name = "Banco", Center = Vector2.zero, Size = MapLayout.BancoSize, Color = GamePalette.FloorBank };
            }
        }

        /// <summary>
        /// The furniture was built for the original small rooms. It is moved up so it still leans on the
        /// back wall of the bigger room; the new details fill the space that opens up in front of it.
        /// </summary>
        private static Vector2 ShiftFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return new Vector2(0f, MapLayout.CasaSize.y * 0.5f - 4f);
                case SceneNames.Tienda:
                    return new Vector2(0f, MapLayout.TiendaSize.y * 0.5f - 4.2f);
                default:
                    return new Vector2(0f, MapLayout.BancoSize.y * 0.5f - 4.2f);
            }
        }

        /// <summary>Where the doormat lies: walking onto it leaves the building.</summary>
        private static Vector2 MatFor(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Casa:
                    return MapLayout.CasaMat;
                case SceneNames.Tienda:
                    return MapLayout.TiendaMat;
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

            var shift = ShiftFor(sceneName);
            props.localPosition = shift;
            stations.localPosition = shift;
            lights.localPosition = shift;
            var extras = Group(map, "Extras");
            var extraLights = Group(map, "ExtraLights");
            var extraPeople = Group(map, "ExtraPeople");
            var matLabel = MatFor(sceneName) + new Vector2(1.3f, 0f);
            var random = new System.Random(5);
            interiorLayers = 0;

            switch (sceneName)
            {
                case SceneNames.Casa:
                    BuildHome(props);
                    BuildStation(stations, "Station_Home", StationType.Home, "Casa", new Vector2(-3f, -0.8f));
                    Flickering(Glow(lights, "Light_Nightstand", new Vector2(-2.7f, -3.0f), 3.0f, "#FFD23F", 0.24f, OrderLight), 0.1f, 1.1f, 5f);
                    Flickering(Glow(lights, "Light_Tv", new Vector2(3.3f, 3.0f), 3.4f, "#4DA3FF", 0.2f, OrderLight), 0.3f, 7f, 17f);
                    Label(labels, "CASA", matLabel, GamePalette.Shade(room.Color, 0.62f));
                    BuildHomeExtras(extras, extraLights, room, random);
                    break;
                case SceneNames.Tienda:
                    BuildStore(props);
                    BuildStation(stations, "Station_Store", StationType.Store, "Tienda", new Vector2(4.2f, -1.8f));
                    Flickering(Glow(lights, "Light_Tv", new Vector2(-3.0f, 3.2f), 4.2f, "#4DA3FF", 0.22f, OrderLight), 0.4f, 9f, 23f);
                    Flickering(Glow(lights, "Light_Offer", new Vector2(0f, 3.6f), 4.6f, "#E8505B", 0.2f, OrderLight), 0.2f, 3f, 31f);
                    Label(labels, "TIENDA", matLabel, GamePalette.Shade(room.Color, 0.62f));
                    BuildStoreExtras(extras, extraLights, room, random);
                    BuildNpc(people, "Vendedor", new Vector2(4.2f, 0.95f) + shift, new Look { Skin = "#F2B84B", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, false);
                    break;
                default:
                    BuildBank(props);
                    BuildStation(stations, "Station_Bank", StationType.Bank, "Cajero", new Vector2(3.8f, -0.4f));
                    Flickering(Glow(lights, "Light_Atm", new Vector2(5.1f, -0.4f), 3.4f, "#55E08F", 0.26f, OrderLight), 0.18f, 2.5f, 11f);
                    Flickering(Glow(lights, "Light_DeskLamp", new Vector2(-3.0f, 1.6f), 3.6f, "#FFD23F", 0.24f, OrderLight), 0.08f, 1.2f, 37f);
                    Label(labels, "BANCO", matLabel, GamePalette.Shade(room.Color, 0.62f));
                    BuildBankExtras(extras, extraLights, extraPeople, room, random);
                    BuildNpc(people, "Cajera", new Vector2(-1.9f, 2.4f) + shift, new Look { Skin = "#9AD0A8", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, false);
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

            Debug.Log(sceneName + " detail layers: " + interiorLayers);
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

        /// <summary>A chair, drawn as a small solid seat.</summary>
        private static void Chair(Transform parent, Vector2 at, Color color)
        {
            Prop(parent, "Chair", roundedSmall, at, new Vector2(0.55f, 0.55f), color);
        }

        private static void BuildHome(Transform parent)
        {
            var home = Group(parent, "Casa");

            // Rugs lie on the floor, so they belong to the floor layer, below every shadow.
            currentLayer = LayerFloor;
            Shape(home, "Rug_Living", rounded, new Vector2(3.5f, 1.1f), new Vector2(4.0f, 2.8f), GamePalette.Debt, OrderRug);
            Shape(home, "Rug_Living_Stripe", rounded, new Vector2(3.5f, 1.1f), new Vector2(3.2f, 2.0f), GamePalette.Shade(GamePalette.Debt, 0.82f), OrderRug + 1);
            Shape(home, "Rug_Dining", rounded, new Vector2(-3.2f, 0.3f), new Vector2(3.4f, 2.4f), GamePalette.Hex("#C8A878"), OrderRug);
            Shape(home, "Rug_Bed", rounded, new Vector2(-4.0f, -2.6f), new Vector2(3.0f, 2.4f), GamePalette.Hex("#8E6BB8"), OrderRug);
            currentLayer = LayerWorld;

            // Kitchen along the back wall.
            Prop(home, "Counter", rounded, new Vector2(-4.4f, 3.0f), new Vector2(2.2f, 1.0f), GamePalette.Hex("#D6B656"));
            Prop(home, "Sink", roundedSmall, new Vector2(-4.7f, 3.05f), new Vector2(0.8f, 0.5f), GamePalette.Hex("#C9CFDB"), OrderProp + 2);
            Prop(home, "Stove", rounded, new Vector2(-2.6f, 3.0f), new Vector2(1.2f, 1.0f), GamePalette.Hex("#8E96AA"));
            Prop(home, "Burner", circle, new Vector2(-2.9f, 3.05f), Vector2.one * 0.34f, GamePalette.Hex("#2A3040"), OrderProp + 2);
            Prop(home, "Burner", circle, new Vector2(-2.3f, 3.05f), Vector2.one * 0.34f, GamePalette.Hex("#2A3040"), OrderProp + 2);
            Prop(home, "Fridge", rounded, new Vector2(-1.3f, 3.1f), new Vector2(1.0f, 1.4f), GamePalette.Hex("#DFE8F5"));
            Prop(home, "Bookshelf", rounded, new Vector2(0.6f, 3.1f), new Vector2(1.6f, 1.0f), GamePalette.Hex("#8B5A3C"));
            for (int i = 0; i < 4; i++)
            {
                Prop(home, "Book", roundedSmall, new Vector2(0.05f + i * 0.35f, 3.15f), new Vector2(0.24f, 0.6f), GamePalette.Hex(i % 2 == 0 ? "#E8505B" : "#4DA3FF"), OrderProp + 2);
            }

            // Living room on the right.
            Prop(home, "TvStand", rounded, new Vector2(3.3f, 3.1f), new Vector2(2.4f, 0.9f), GamePalette.Hex("#6A4A8C"));
            Prop(home, "Tv", roundedSmall, new Vector2(3.3f, 3.35f), new Vector2(1.5f, 0.6f), GamePalette.Wall, OrderProp + 2);
            Prop(home, "Tv_Screen", roundedSmall, new Vector2(3.3f, 3.35f), new Vector2(1.3f, 0.45f), GamePalette.Alert, OrderProp + 4);
            Prop(home, "Sofa", rounded, new Vector2(5.0f, 0.9f), new Vector2(1.0f, 2.6f), GamePalette.Hex("#3B6FB6"));
            Prop(home, "Sofa_Cushion", roundedSmall, new Vector2(4.85f, 0.9f), new Vector2(0.5f, 2.0f), GamePalette.Hex("#6FA0E0"), OrderProp + 2);
            Prop(home, "CoffeeTable", rounded, new Vector2(3.2f, 0.9f), new Vector2(1.3f, 1.0f), GamePalette.Hex("#8B5A3C"));
            Prop(home, "Bill_A", roundedSmall, new Vector2(3.0f, 0.95f), new Vector2(0.5f, 0.36f), GamePalette.White, OrderProp + 2);
            Prop(home, "Bill_B", roundedSmall, new Vector2(3.45f, 0.85f), new Vector2(0.5f, 0.36f), GamePalette.White, OrderProp + 2);

            // Dining table with four chairs.
            Prop(home, "Table", rounded, new Vector2(-3.2f, 0.3f), new Vector2(2.0f, 1.1f), GamePalette.Hex("#8B5A3C"));
            Prop(home, "Plate", circle, new Vector2(-3.7f, 0.35f), Vector2.one * 0.4f, GamePalette.White, OrderProp + 2);
            Prop(home, "Plate", circle, new Vector2(-2.7f, 0.35f), Vector2.one * 0.4f, GamePalette.White, OrderProp + 2);
            Chair(home, new Vector2(-3.7f, 1.15f), GamePalette.Hex("#B07A45"));
            Chair(home, new Vector2(-2.7f, 1.15f), GamePalette.Hex("#B07A45"));
            Chair(home, new Vector2(-3.7f, -0.55f), GamePalette.Hex("#B07A45"));
            Chair(home, new Vector2(-2.7f, -0.55f), GamePalette.Hex("#B07A45"));

            // Bedroom corner.
            Prop(home, "Bed", rounded, new Vector2(-4.3f, -2.7f), new Vector2(2.2f, 1.8f), GamePalette.Alert);
            Prop(home, "Pillow", roundedSmall, new Vector2(-5.0f, -2.7f), new Vector2(0.6f, 1.0f), GamePalette.White, OrderProp + 2);
            Prop(home, "Nightstand", rounded, new Vector2(-2.7f, -3.2f), new Vector2(0.7f, 0.7f), GamePalette.Hex("#8B5A3C"));
            Prop(home, "NightLamp", circle, new Vector2(-2.7f, -3.1f), Vector2.one * 0.34f, GamePalette.Marker, OrderProp + 2);
            Prop(home, "Wardrobe", rounded, new Vector2(4.4f, -3.2f), new Vector2(1.8f, 1.0f), GamePalette.Hex("#6A4A8C"));
            Prop(home, "WardrobeDoor", square, new Vector2(4.4f, -3.2f), new Vector2(0.05f, 0.9f), GamePalette.Outline, OrderProp + 2);

            Prop(home, "PlantPot", roundedSmall, new Vector2(5.0f, -1.2f), new Vector2(0.6f, 0.5f), GamePalette.Hex("#C26A3A"));
            Prop(home, "Plant", circle, new Vector2(5.0f, -0.85f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
            Prop(home, "PlantPot", roundedSmall, new Vector2(-5.1f, 0.0f), new Vector2(0.6f, 0.5f), GamePalette.Hex("#C26A3A"));
            Prop(home, "Plant", circle, new Vector2(-5.1f, 0.35f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
        }

        private static void BuildStore(Transform parent)
        {
            var store = Group(parent, "Tienda");

            // Back wall: a row of TVs and the white goods, under the big offer.
            var screens = new[] { "#4DA3FF", "#E8505B", "#55E08F" };
            for (int i = 0; i < 3; i++)
            {
                float x = -4.8f + i * 1.8f;
                Prop(store, "TV", rounded, new Vector2(x, 3.0f), new Vector2(1.5f, 1.0f), GamePalette.Wall);
                Shape(store, "TV_Screen", roundedSmall, new Vector2(x, 3.05f), new Vector2(1.2f, 0.7f), GamePalette.Hex(screens[i]), OrderProp + 2);
                Shape(store, "TV_Shine", roundedSmall, new Vector2(x - 0.3f, 3.2f), new Vector2(0.3f, 0.2f), GamePalette.White, OrderProp + 3);
            }

            Prop(store, "Fridge", rounded, new Vector2(3.0f, 3.3f), new Vector2(1.0f, 1.5f), GamePalette.Hex("#DFE8F5"));
            Prop(store, "Washer", rounded, new Vector2(4.3f, 3.3f), new Vector2(1.0f, 1.2f), GamePalette.Hex("#C9CFDB"));
            Prop(store, "Washer_Door", circle, new Vector2(4.3f, 3.25f), Vector2.one * 0.6f, GamePalette.Hex("#6FA0E0"), OrderProp + 2);
            Prop(store, "Fridge", rounded, new Vector2(5.4f, 3.3f), new Vector2(0.9f, 1.5f), GamePalette.Hex("#DFE8F5"));

            var banner = new Vector2(0f, 3.85f);
            Prop(store, "Offer_Frame", roundedSmall, banner, new Vector2(5.2f, 0.5f), GamePalette.Outline, OrderProp + 2);
            Shape(store, "Offer", roundedSmall, banner, new Vector2(5.06f, 0.36f), GamePalette.Debt, OrderProp + 4);
            Label(store, "36 CUOTAS SIN INTERESES", banner, GamePalette.White, 0.03f, TextAnchor.MiddleCenter);

            // Four shelving islands with phones and laptops.
            foreach (float y in new[] { 0.8f, -0.9f })
            {
                foreach (float x in new[] { -4.2f, -1.2f })
                {
                    Prop(store, "Shelf", rounded, new Vector2(x, y), new Vector2(2.4f, 0.8f), GamePalette.Hex("#C9753A"));
                    for (int i = 0; i < 4; i++)
                    {
                        var color = y > 0f ? new[] { "#FFD23F", "#E8505B", "#FFFFFF", "#4DA3FF" }[i] : "#2A3040";
                        Prop(store, y > 0f ? "Phone" : "Laptop", roundedSmall, new Vector2(x - 0.85f + i * 0.57f, y + 0.05f), new Vector2(0.4f, 0.5f), GamePalette.Hex(color), OrderProp + 2);
                    }
                }
            }

            // Glass display case and the boxes of stock.
            Prop(store, "Display", rounded, new Vector2(-5.2f, -2.7f), new Vector2(1.4f, 0.9f), GamePalette.Hex("#BFE3F5"));
            Prop(store, "Display_Item", roundedSmall, new Vector2(-5.2f, -2.7f), new Vector2(0.6f, 0.4f), GamePalette.Marker, OrderProp + 2);
            Prop(store, "Boxes", rounded, new Vector2(5.2f, -3.2f), new Vector2(1.2f, 1.0f), GamePalette.Hex("#A9743F"));
            Prop(store, "Boxes_Top", rounded, new Vector2(5.2f, -2.85f), new Vector2(0.9f, 0.5f), GamePalette.Hex("#C9945A"), OrderProp + 2);

            // The counter where the seller works.
            Prop(store, "Counter", rounded, new Vector2(4.2f, -0.4f), new Vector2(2.8f, 1.0f), GamePalette.Hex("#D6B656"));
            Prop(store, "Register", roundedSmall, new Vector2(4.9f, -0.3f), new Vector2(0.7f, 0.5f), GamePalette.Wall, OrderProp + 2);
            Prop(store, "Contract", roundedSmall, new Vector2(3.6f, -0.35f), new Vector2(0.6f, 0.4f), GamePalette.White, OrderProp + 2);

            Prop(store, "PlantPot", roundedSmall, new Vector2(-5.5f, 1.8f), new Vector2(0.6f, 0.5f), GamePalette.Hex("#C26A3A"));
            Prop(store, "Plant", circle, new Vector2(-5.5f, 2.15f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
        }

        private static void BuildBank(Transform parent)
        {
            var bank = Group(parent, "Banco");

            // Back wall: a clock and the vault door.
            Prop(bank, "Clock_Frame", circle, new Vector2(0.2f, 3.65f), Vector2.one * 0.75f, GamePalette.Outline, OrderProp + 2);
            Shape(bank, "Clock", circle, new Vector2(0.2f, 3.65f), Vector2.one * 0.62f, GamePalette.White, OrderProp + 4);
            Shape(bank, "Clock_HandA", square, new Vector2(0.2f, 3.75f), new Vector2(0.05f, 0.24f), GamePalette.Outline, OrderProp + 5);
            Shape(bank, "Clock_HandB", square, new Vector2(0.3f, 3.65f), new Vector2(0.18f, 0.05f), GamePalette.Outline, OrderProp + 5);
            Prop(bank, "Vault", circle, new Vector2(4.4f, 2.9f), Vector2.one * 2.0f, GamePalette.Hex("#8E96AA"));
            Prop(bank, "Vault_Ring", ring, new Vector2(4.4f, 2.9f), Vector2.one * 1.5f, GamePalette.Hex("#C9CFDB"), OrderProp + 2);
            Prop(bank, "Vault_Wheel", circle, new Vector2(4.4f, 2.9f), Vector2.one * 0.5f, GamePalette.Marker, OrderProp + 4);

            // Teller counter with glass partitions; the cashier works behind it.
            Prop(bank, "Counter", rounded, new Vector2(-3.0f, 1.2f), new Vector2(5.0f, 0.9f), GamePalette.Hex("#7A6A52"));
            foreach (float x in new[] { -4.7f, -3.0f, -1.3f })
            {
                var glass = GamePalette.Hex("#BFE3F5");
                glass.a = 0.7f;
                Prop(bank, "Glass", roundedSmall, new Vector2(x, 1.75f), new Vector2(0.16f, 1.0f), glass, OrderProp + 2);
            }

            Prop(bank, "DeskLamp", circle, new Vector2(-3.0f, 1.3f), Vector2.one * 0.4f, GamePalette.Marker, OrderProp + 2);
            Prop(bank, "Ledger", roundedSmall, new Vector2(-4.2f, 1.25f), new Vector2(0.6f, 0.4f), GamePalette.White, OrderProp + 2);

            // Three ATMs against the right wall; the middle one is the interactive one.
            foreach (float y in new[] { 1.5f, -0.4f, -2.3f })
            {
                Prop(bank, "AtmBody", rounded, new Vector2(5.2f, y), new Vector2(1.2f, 1.5f), GamePalette.Hex("#4B5568"));
                Prop(bank, "AtmScreen", roundedSmall, new Vector2(5.2f, y + 0.25f), new Vector2(0.9f, 0.6f), GamePalette.MoneyIn, OrderProp + 2);
                Prop(bank, "AtmKeypad", roundedSmall, new Vector2(5.2f, y - 0.4f), new Vector2(0.7f, 0.3f), GamePalette.Hex("#2A3040"), OrderProp + 2);
            }

            // Queue posts with a rope in front of the counter.
            Shape(bank, "Rope", square, new Vector2(-2.0f, -0.3f), new Vector2(6.0f, 0.06f), GamePalette.Debt, OrderProp + 2);
            foreach (float x in new[] { -5.0f, -3.0f, -1.0f, 1.0f })
            {
                Prop(bank, "Post", circle, new Vector2(x, -0.3f), Vector2.one * 0.32f, GamePalette.Marker);
            }

            // Waiting area.
            Prop(bank, "Bench", rounded, new Vector2(-4.4f, -2.7f), new Vector2(2.4f, 0.7f), GamePalette.Hex("#3B6FB6"));
            Prop(bank, "Bench", rounded, new Vector2(-1.6f, -2.7f), new Vector2(2.4f, 0.7f), GamePalette.Hex("#3B6FB6"));
            Prop(bank, "PlantPot", roundedSmall, new Vector2(5.2f, -3.5f), new Vector2(0.7f, 0.6f), GamePalette.Hex("#C26A3A"));
            Prop(bank, "Plant", circle, new Vector2(5.2f, -3.1f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
            Prop(bank, "PlantPot", roundedSmall, new Vector2(-5.5f, 3.0f), new Vector2(0.7f, 0.6f), GamePalette.Hex("#C26A3A"));
            Prop(bank, "Plant", circle, new Vector2(-5.5f, 3.35f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
        }
    }
}

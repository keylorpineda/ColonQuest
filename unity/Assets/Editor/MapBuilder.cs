using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Main.unity: a small Among Us style map with a corridor
    /// joining Casa, Tienda and Banco, one "task" station in each. Run it from
    /// Colon Quest > Build Map Scene; the scene is rebuilt from scratch every time.
    /// </summary>
    public static class MapBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const float OutlineThickness = 0.28f;
        private const float PropOutline = 0.08f;
        private const float CameraSize = 4.6f;
        private static readonly Rect MapBounds = new Rect(-11.5f, -6.7f, 23f, 13.4f);

        // Sorting layers, back to front. Orders below only matter inside one layer.
        private const string LayerBackground = "Background";
        private const string LayerFloor = "Floor";
        private const string LayerWorld = "Default";
        private const string LayerForeground = "Foreground";

        private const int OrderNebula = 0;
        private const int OrderStarsFar = 1;
        private const int OrderStarsMid = 2;

        private const int OrderOutline = 0;
        private const int OrderFloor = 1;
        private const int FloorOrdersPerRoom = 4;
        private const int OrderRug = 40;

        private const int OrderShadow = 0;
        private const int OrderProp = 5;
        private const int OrderWall = 8;
        private const int OrderMarker = 10;
        private const int OrderLabel = 12;
        private const int OrderPlayer = 20;

        private const int OrderDust = 0;
        private const int OrderVignette = 10;

        private static string currentLayer = LayerWorld;

        private const float WallThickness = 0.3f;

        private static Sprite rounded;
        private static Sprite roundedSmall;
        private static Sprite circle;
        private static Sprite ring;
        private static Sprite softCircle;
        private static Sprite vignette;
        private static Sprite square;

        private struct Room
        {
            public string Name;
            public Vector2 Center;
            public Vector2 Size;
            public Color Color;
        }

        [MenuItem("Colon Quest/Build Map Scene")]
        public static void Build()
        {
            SpriteFactory.EnsureGenerated();
            LoadSprites();
            EnsureSortingLayers();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var map = new GameObject("Map").transform;
            BuildBackground(Group(map, "Background"));

            // Rooms first, corridor last: the corridor covers the rounded corners of
            // the rooms where they overlap, so each doorway has a clean seam.
            var rooms = new[]
            {
                new Room { Name = "Casa", Center = new Vector2(-7f, 3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorHome },
                new Room { Name = "Tienda", Center = new Vector2(7f, 3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorStore },
                new Room { Name = "Banco", Center = new Vector2(0f, -3.4f), Size = new Vector2(7f, 5.6f), Color = GamePalette.FloorBank },
                new Room { Name = "Plaza", Center = new Vector2(0f, 2.2f), Size = new Vector2(5f, 2.8f), Color = GamePalette.FloorPlaza },
                new Room { Name = "Pasillo", Center = Vector2.zero, Size = new Vector2(22f, 2.4f), Color = GamePalette.FloorCorridor }
            };
            currentLayer = LayerFloor;
            BuildFloors(Group(map, "Floors"), rooms);
            currentLayer = LayerWorld;
            BuildWalls(Group(map, "Walls"));

            var props = Group(map, "Props");
            BuildHome(props);
            BuildStore(props);
            BuildBank(props);
            BuildPlaza(props);

            var stations = Group(map, "Stations");
            BuildStation(stations, "Station_Home", StationType.Home, "Casa", new Vector2(-4.6f, 3.9f));
            BuildStation(stations, "Station_Store", StationType.Store, "Tienda", new Vector2(8.9f, 2.9f));
            BuildStation(stations, "Station_Bank", StationType.Bank, "Cajero", new Vector2(0f, -3.4f));

            BuildLabels(Group(map, "Labels"));
            BuildWallColliders(Group(map, "Colliders"));

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = Vector3.zero;
            spawn.transform.SetParent(map, false);

            var player = BuildPlayer(spawn.transform.position);
            BuildCamera().AddComponent<CameraFollow>().Configure(player, MapBounds);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// Renders the main camera of Main.unity to a PNG. Pass -previewPath file.png, and
        /// optionally -previewZoom (orthographic size) and -previewAt x,y to frame the shot.
        /// </summary>
        public static void RenderPreview()
        {
            string path = GetArgument("-previewPath") ?? "MapPreview.png";
            EditorSceneManager.OpenScene(ScenePath);

            const int width = 1920;
            const int height = 1080;
            var camera = Camera.main;

            string zoom = GetArgument("-previewZoom");
            if (zoom != null)
            {
                camera.orthographicSize = float.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture);
            }

            string at = GetArgument("-previewAt");
            if (at != null)
            {
                var parts = at.Split(',');
                camera.transform.position = new Vector3(
                    float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    camera.transform.position.z);
            }

            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void LoadSprites()
        {
            rounded = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.RoundedRectPath);
            roundedSmall = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.RoundedRectSmallPath);
            circle = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.CirclePath);
            ring = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.RingPath);
            softCircle = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.SoftCirclePath);
            vignette = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.VignettePath);
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.SquarePath);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static SpriteRenderer Shape(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = currentLayer;
            renderer.sortingOrder = order;

            if (sprite == rounded || sprite == roundedSmall)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            return renderer;
        }

        /// <summary>
        /// A dark outline shape with the colored body on top, the core look of the style.
        /// Props at the default order are furniture and block the player; props drawn above
        /// it (order greater than OrderProp) are decoration lying on furniture and do not.
        /// </summary>
        private static void Prop(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order = OrderProp)
        {
            bool solid = order <= OrderProp;
            if (solid)
            {
                var shadowColor = GamePalette.Outline;
                shadowColor.a = 0.3f;
                Shape(parent, name + "_Shadow", sprite, center + new Vector2(0.07f, -0.16f), size + Vector2.one * 0.12f, shadowColor, OrderShadow);
            }

            Shape(parent, name + "_Outline", sprite, center, size + Vector2.one * (PropOutline * 2f), GamePalette.Outline, order);
            var body = Shape(parent, name, sprite, center, size, color, order + 1);
            if (solid)
            {
                body.gameObject.AddComponent<BoxCollider2D>().size = size;
            }
        }

        private static void Wall(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        private static void HorizontalCollider(Transform parent, float xMin, float xMax, float y)
        {
            Wall(parent, $"Wall_H_{xMin}_{xMax}_{y}", new Vector2((xMin + xMax) * 0.5f, y), new Vector2(xMax - xMin, WallThickness));
        }

        private static void VerticalCollider(Transform parent, float x, float yMin, float yMax)
        {
            Wall(parent, $"Wall_V_{x}_{yMin}_{yMax}", new Vector2(x, (yMin + yMax) * 0.5f), new Vector2(WallThickness, yMax - yMin));
        }

        private static GameObject BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GamePalette.Space;
            go.AddComponent<AudioListener>();
            return go;
        }

        private static Transform ParallaxGroup(Transform parent, string name, float factor)
        {
            var group = Group(parent, name);
            group.gameObject.AddComponent<ParallaxLayer>().SetFactor(factor);
            return group;
        }

        /// <summary>Distant layers move less than the camera, the dust in front moves more.</summary>
        private static void BuildBackground(Transform parent)
        {
            var random = new System.Random(7);
            currentLayer = LayerBackground;

            var nebula = ParallaxGroup(parent, "Nebula", 0.92f);
            Nebula(nebula, new Vector2(-9f, 4f), 15f, "#4B2E9A", 0.34f);
            Nebula(nebula, new Vector2(8f, 5f), 12f, "#1D4F8C", 0.34f);
            Nebula(nebula, new Vector2(1f, -6f), 16f, "#6B2A6B", 0.28f);
            Nebula(nebula, new Vector2(-11f, -5f), 11f, "#1D6B7A", 0.26f);
            Nebula(nebula, new Vector2(12f, -3f), 12f, "#3A2A8C", 0.28f);

            StarField(ParallaxGroup(parent, "Stars_Far", 0.88f), random, 90, 0.04f, 0.09f, 0.2f, 0.45f, OrderStarsFar);
            StarField(ParallaxGroup(parent, "Stars_Mid", 0.72f), random, 45, 0.07f, 0.14f, 0.3f, 0.6f, OrderStarsMid);
            currentLayer = LayerForeground;
            Dust(ParallaxGroup(parent, "Dust", -0.25f), random);
            currentLayer = LayerWorld;
        }

        /// <summary>
        /// Registers the sorting layers the scene uses, in back to front order:
        /// Background, Floor, Default (props, walls, characters), Foreground.
        /// </summary>
        private static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");

            AddSortingLayer(layers, LayerBackground, 0, 1111001);
            AddSortingLayer(layers, LayerFloor, 1, 1111002);
            AddSortingLayer(layers, LayerForeground, layers.arraySize, 1111003);

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private static void AddSortingLayer(SerializedProperty layers, string name, int index, int uniqueId)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                {
                    return;
                }
            }

            layers.InsertArrayElementAtIndex(index);
            var element = layers.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("name").stringValue = name;
            element.FindPropertyRelative("uniqueID").intValue = uniqueId;
            element.FindPropertyRelative("locked").intValue = 0;
        }

        private static void Nebula(Transform parent, Vector2 center, float size, string hex, float alpha)
        {
            var color = GamePalette.Hex(hex);
            color.a = alpha;
            Shape(parent, "Nebula", softCircle, center, Vector2.one * size, color, OrderNebula);
        }

        private static void StarField(Transform parent, System.Random random, int count, float minSize, float maxSize, float minAlpha, float maxAlpha, int order)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)(random.NextDouble() * 28.0 - 14.0);
                float y = (float)(random.NextDouble() * 16.0 - 8.0);
                float size = Mathf.Lerp(minSize, maxSize, (float)random.NextDouble());
                var color = GamePalette.White;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, (float)random.NextDouble());
                Shape(parent, "Star", circle, new Vector2(x, y), Vector2.one * size, color, order);
            }
        }

        private static void Dust(Transform parent, System.Random random)
        {
            for (int i = 0; i < 36; i++)
            {
                float x = (float)(random.NextDouble() * 28.0 - 14.0);
                float y = (float)(random.NextDouble() * 18.0 - 9.0);
                float size = 0.05f + (float)random.NextDouble() * 0.08f;
                var color = GamePalette.Hex("#FFF1C2");
                color.a = 0.16f + (float)random.NextDouble() * 0.2f;

                var mote = Shape(parent, "Mote", circle, new Vector2(x, y), Vector2.one * size, color, OrderDust);
                mote.gameObject.AddComponent<Mote>().Configure(
                    0.15f + (float)random.NextDouble() * 0.3f,
                    0.1f + (float)random.NextDouble() * 0.2f,
                    (float)(random.NextDouble() * 6.28),
                    new Vector2(14f, 9f));
            }
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

                if (room.Name != "Banco" && room.Name != "Pasillo")
                {
                    WallFace(parent, room, order + 3);
                }
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

        private static void BuildWalls(Transform parent)
        {
            // Room walls toward the corridor, each with a 2.4 wide doorway.
            HorizontalWall(parent, "Casa_Wall_L", -10.5f, -8.2f, 1.2f);
            HorizontalWall(parent, "Casa_Wall_R", -5.8f, -3.5f, 1.2f);
            HorizontalWall(parent, "Tienda_Wall_L", 3.5f, 5.8f, 1.2f);
            HorizontalWall(parent, "Tienda_Wall_R", 8.2f, 10.5f, 1.2f);
            HorizontalWall(parent, "Banco_Wall_L", -3.5f, -1.2f, -1.2f);
            HorizontalWall(parent, "Banco_Wall_R", 1.2f, 3.5f, -1.2f);
        }

        private static void HorizontalWall(Transform parent, string name, float xMin, float xMax, float y)
        {
            var center = new Vector2((xMin + xMax) * 0.5f, y);
            var size = new Vector2(xMax - xMin, 0.3f);
            Shape(parent, name + "_Outline", roundedSmall, center, size + Vector2.one * 0.14f, GamePalette.Outline, OrderWall);
            Shape(parent, name, roundedSmall, center, size, GamePalette.Wall, OrderWall + 1);
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

        private static void BuildPlaza(Transform parent)
        {
            var plaza = Group(parent, "Plaza");
            Prop(plaza, "Bench", rounded, new Vector2(0f, 2.6f), new Vector2(2.2f, 0.7f), GamePalette.Hex("#8B5A3C"));
            Prop(plaza, "PlantPot", roundedSmall, new Vector2(-1.8f, 2.2f), new Vector2(0.7f, 0.6f), GamePalette.Hex("#C26A3A"));
            Prop(plaza, "Plant", circle, new Vector2(-1.8f, 2.6f), Vector2.one * 0.9f, GamePalette.Hex("#3FA66A"), OrderProp + 2);
        }

        private static void BuildStation(Transform parent, string name, StationType type, string displayName, Vector2 center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2.6f, 2.2f);

            var station = go.AddComponent<Station>();
            station.Configure(type, displayName);

            var marker = Shape(go.transform, "Marker", ring, Vector2.zero, Vector2.one * 1.3f, GamePalette.Marker, OrderMarker);
            marker.gameObject.AddComponent<StationMarker>().Bind(station);
        }

        /// <summary>The outer perimeter of the map plus the walls on each side of every doorway.</summary>
        private static void BuildWallColliders(Transform parent)
        {
            // Corridor
            VerticalCollider(parent, -11f, -1.2f, 1.2f);
            VerticalCollider(parent, 11f, -1.2f, 1.2f);
            HorizontalCollider(parent, -11f, -3.5f, -1.2f);
            HorizontalCollider(parent, 3.5f, 11f, -1.2f);
            HorizontalCollider(parent, -11f, -10.5f, 1.2f);
            HorizontalCollider(parent, -3.5f, -2.5f, 1.2f);
            HorizontalCollider(parent, 2.5f, 3.5f, 1.2f);
            HorizontalCollider(parent, 10.5f, 11f, 1.2f);

            // Casa
            VerticalCollider(parent, -10.5f, 1.2f, 6.2f);
            VerticalCollider(parent, -3.5f, 1.2f, 6.2f);
            HorizontalCollider(parent, -10.5f, -3.5f, 6.2f);
            HorizontalCollider(parent, -10.5f, -8.2f, 1.2f);
            HorizontalCollider(parent, -5.8f, -3.5f, 1.2f);

            // Plaza
            VerticalCollider(parent, -2.5f, 1.2f, 3.2f);
            VerticalCollider(parent, 2.5f, 1.2f, 3.2f);
            HorizontalCollider(parent, -2.5f, 2.5f, 3.2f);

            // Tienda
            VerticalCollider(parent, 3.5f, 1.2f, 6.2f);
            VerticalCollider(parent, 10.5f, 1.2f, 6.2f);
            HorizontalCollider(parent, 3.5f, 10.5f, 6.2f);
            HorizontalCollider(parent, 3.5f, 5.8f, 1.2f);
            HorizontalCollider(parent, 8.2f, 10.5f, 1.2f);

            // Banco
            VerticalCollider(parent, -3.5f, -6.2f, -1.2f);
            VerticalCollider(parent, 3.5f, -6.2f, -1.2f);
            HorizontalCollider(parent, -3.5f, 3.5f, -6.2f);
            HorizontalCollider(parent, -3.5f, -1.2f, -1.2f);
            HorizontalCollider(parent, 1.2f, 3.5f, -1.2f);
        }

        private static Transform BuildPlayer(Vector3 position)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;
            collider.offset = new Vector2(0f, -0.1f);

            // Shadow stays put; the visual flips to face the walking direction.
            var shadowColor = GamePalette.Outline;
            shadowColor.a = 0.35f;
            shadowColor.a = 0.45f;
            Shape(player.transform, "Shadow", softCircle, new Vector2(0f, -0.5f), new Vector2(1.1f, 0.4f), shadowColor, OrderPlayer - 1);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            player.AddComponent<PlayerController>().SetVisual(visual);

            var skin = GamePalette.Hex("#FF8A3D");
            Shape(visual, "Body_Outline", rounded, Vector2.zero, new Vector2(0.96f, 1.16f), GamePalette.Outline, OrderPlayer);
            Shape(visual, "Body", rounded, Vector2.zero, new Vector2(0.8f, 1.0f), skin, OrderPlayer + 1);
            Shape(visual, "Belly", roundedSmall, new Vector2(0f, -0.28f), new Vector2(0.5f, 0.28f), GamePalette.Shade(skin, 1.15f), OrderPlayer + 2);

            // Two big cartoon eyes looking toward the walking direction (right when unflipped).
            Eye(visual, new Vector2(0.0f, 0.22f));
            Eye(visual, new Vector2(0.3f, 0.22f));

            return player.transform;
        }

        private static void Eye(Transform parent, Vector2 center)
        {
            Shape(parent, "Eye_Outline", circle, center, Vector2.one * 0.3f, GamePalette.Outline, OrderPlayer + 2);
            Shape(parent, "Eye", circle, center, Vector2.one * 0.24f, GamePalette.White, OrderPlayer + 3);
            Shape(parent, "Pupil", circle, center + new Vector2(0.04f, 0f), Vector2.one * 0.11f, GamePalette.Outline, OrderPlayer + 4);
        }

        private static void BuildLabels(Transform parent)
        {
            Label(parent, "CASA", new Vector2(-10.1f, 1.95f), GamePalette.FloorHome);
            Label(parent, "TIENDA", new Vector2(3.9f, 1.95f), GamePalette.FloorStore);
            Label(parent, "BANCO", new Vector2(-3.1f, -1.95f), GamePalette.FloorBank);
        }

        private static void Label(Transform parent, string text, Vector2 position, Color floor)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 100;
            mesh.characterSize = 0.07f;
            mesh.fontStyle = FontStyle.Bold;
            mesh.anchor = TextAnchor.MiddleLeft;
            mesh.color = GamePalette.Shade(floor, 0.62f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = OrderLabel;
        }
    }
}

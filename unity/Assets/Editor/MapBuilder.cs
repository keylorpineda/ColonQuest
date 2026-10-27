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
        private const float CameraSize = 7.5f;

        private const int OrderOutline = -8;
        private const int OrderFloor = -5;
        private const int OrderRug = 4;
        private const int OrderProp = 5;
        private const int OrderWall = 8;
        private const int OrderMarker = 10;
        private const int OrderLabel = 12;

        private static Sprite rounded;
        private static Sprite roundedSmall;
        private static Sprite circle;
        private static Sprite ring;

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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var map = new GameObject("Map").transform;
            BuildCamera();
            BuildStars(Group(map, "Background"));

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
            BuildFloors(Group(map, "Floors"), rooms);
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

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = Vector3.zero;
            spawn.transform.SetParent(map, false);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>Renders the main camera of Main.unity to a PNG. Pass -previewPath file.png.</summary>
        public static void RenderPreview()
        {
            string path = GetArgument("-previewPath") ?? "MapPreview.png";
            EditorSceneManager.OpenScene(ScenePath);

            const int width = 1920;
            const int height = 1080;
            var camera = Camera.main;
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

        /// <summary>A dark outline shape with the colored body on top, the core look of the style.</summary>
        private static void Prop(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order = OrderProp)
        {
            Shape(parent, name + "_Outline", sprite, center, size + Vector2.one * (PropOutline * 2f), GamePalette.Outline, order);
            Shape(parent, name, sprite, center, size, color, order + 1);
        }

        private static void BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GamePalette.Space;
            go.AddComponent<AudioListener>();
        }

        private static void BuildStars(Transform parent)
        {
            var random = new System.Random(7);
            for (int i = 0; i < 80; i++)
            {
                float x = (float)(random.NextDouble() * 34.0 - 17.0);
                float y = (float)(random.NextDouble() * 19.0 - 9.5);
                float size = 0.05f + (float)random.NextDouble() * 0.1f;
                var color = GamePalette.White;
                color.a = 0.25f + (float)random.NextDouble() * 0.35f;
                Shape(parent, "Star", circle, new Vector2(x, y), Vector2.one * size, color, -10);
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

            // Each room takes two sorting orders (floor, then plate) so later rooms cover earlier ones.
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                int order = OrderFloor + i * 2;
                Shape(parent, room.Name + "_Floor", rounded, room.Center, room.Size, room.Color, order);
                Shape(parent, room.Name + "_Plate", rounded, room.Center,
                    room.Size - Vector2.one, GamePalette.Shade(room.Color, 0.94f), order + 1);
            }
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
            Shape(home, "Rug", rounded, new Vector2(-7.2f, 3.4f), new Vector2(3.4f, 2.2f), GamePalette.Debt, OrderRug);
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

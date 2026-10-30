using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// Builds every scene of the game from code: the town overworld and the Casa, Tienda
    /// and Banco interiors. Run Colon Quest > Build All Scenes; each scene is rebuilt from
    /// scratch every time. This file holds the helpers shared by all of them.
    /// </summary>
    public static partial class MapBuilder
    {
        private const float OutlineThickness = 0.28f;
        private const float PropOutline = 0.08f;
        private const float WallThickness = 0.3f;

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
        private const int OrderLight = 4;
        private const int OrderProp = 5;
        private const int OrderWall = 8;
        private const int OrderLabel = 12;
        private const int OrderPlayer = 20;

        private const int OrderDust = 0;
        private const int OrderVignette = 10;
        private const int OrderFader = 100;

        private static string currentLayer = LayerWorld;

        private static Sprite rounded;
        private static Sprite roundedSmall;
        private static Sprite circle;
        private static Sprite ring;
        private static Sprite softCircle;
        private static Sprite vignette;
        private static Sprite square;

        private static string ScenePathFor(string sceneName)
        {
            return $"Assets/Scenes/{sceneName}.unity";
        }

        [MenuItem("Colon Quest/Build All Scenes")]
        public static void BuildAll()
        {
            BuildMenu();
            BuildTown();
            BuildInteriors();
        }

        private static UnityEngine.SceneManagement.Scene BeginScene()
        {
            SpriteFactory.EnsureGenerated();
            LoadSprites();
            EnsureSortingLayers();
            currentLayer = LayerWorld;
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string sceneName)
        {
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePathFor(sceneName));
            UpdateBuildSettings();
        }

        /// <summary>The menu is the first scene, then the town; the interiors follow. Only scenes that exist are listed.</summary>
        private static void UpdateBuildSettings()
        {
            var names = new[] { SceneNames.Menu, SceneNames.Town, SceneNames.Casa, SceneNames.Tienda, SceneNames.Banco, SceneNames.Agencia, SceneNames.Carcel, SceneNames.Hospital, SceneNames.HospitalArriba, SceneNames.Vecino };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var name in names)
            {
                if (File.Exists(ScenePathFor(name)))
                {
                    list.Add(new EditorBuildSettingsScene(ScenePathFor(name), true));
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>
        /// Renders the main camera of a scene to a PNG. Pass -previewPath file.png, and
        /// optionally -previewScene (default Town), -previewZoom (orthographic size) and
        /// -previewAt x,y to frame the shot.
        /// </summary>
        public static void RenderPreview()
        {
            string path = GetArgument("-previewPath") ?? "MapPreview.png";
            EditorSceneManager.OpenScene(ScenePathFor(GetArgument("-previewScene") ?? SceneNames.Town));

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
            foreach (var fitter in UnityEngine.Object.FindObjectsByType<ViewFitter>(FindObjectsSortMode.None))
            {
                fitter.Fit();
            }

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
            noise = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFactory.NoisePath);
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

        private static GameObject BuildCamera(float size, Color background, Vector2 position)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(position.x, position.y, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();

            currentLayer = LayerForeground;

            // Dark corners keep the eye on the middle of the screen.
            var tint = GamePalette.Hex("#05070F");
            tint.a = 0.55f;
            var shade = Shape(go.transform, "Vignette", vignette, Vector2.zero, Vector2.one, tint, OrderVignette);
            shade.transform.localPosition = new Vector3(0f, 0f, 5f);
            shade.gameObject.AddComponent<ViewFitter>().Fit();

            // Black sheet used to fade between scenes.
            var fade = Shape(go.transform, "Fader", square, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f), OrderFader);
            fade.transform.localPosition = new Vector3(0f, 0f, 4f);
            fade.gameObject.AddComponent<ViewFitter>().Fit();
            fade.gameObject.AddComponent<ScreenFader>();

            currentLayer = LayerWorld;
            return go;
        }

        private static SpriteRenderer Glow(Transform parent, string name, Vector2 center, float size, string hex, float alpha, int order)
        {
            var color = GamePalette.Hex(hex);
            color.a = alpha;
            return Shape(parent, name, softCircle, center, Vector2.one * size, color, order);
        }

        private static void Flickering(SpriteRenderer target, float amplitude, float speed, float seed)
        {
            target.gameObject.AddComponent<Flicker>().Configure(amplitude, speed, seed);
        }

        private static Transform ParallaxGroup(Transform parent, string name, float factor)
        {
            var group = Group(parent, name);
            group.gameObject.AddComponent<ParallaxLayer>().SetFactor(factor);
            return group;
        }

        /// <summary>Space behind the interiors. Distant layers move less than the camera, the dust in front moves more.</summary>
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

            StarField(ParallaxGroup(parent, "Stars_Far", 0.88f), random, 90, 0.04f, 0.09f, 0.2f, 0.45f, OrderStarsFar, false);
            StarField(ParallaxGroup(parent, "Stars_Mid", 0.72f), random, 45, 0.07f, 0.14f, 0.3f, 0.6f, OrderStarsMid, true);
            currentLayer = LayerForeground;
            Dust(ParallaxGroup(parent, "Dust", -0.25f), random);
            currentLayer = LayerWorld;
        }

        /// <summary>
        /// Registers the sorting layers the scenes use, in back to front order:
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

        private static void StarField(Transform parent, System.Random random, int count, float minSize, float maxSize, float minAlpha, float maxAlpha, int order, bool twinkle)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)(random.NextDouble() * 28.0 - 14.0);
                float y = (float)(random.NextDouble() * 16.0 - 8.0);
                float size = Mathf.Lerp(minSize, maxSize, (float)random.NextDouble());
                var color = GamePalette.White;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, (float)random.NextDouble());
                var star = Shape(parent, "Star", circle, new Vector2(x, y), Vector2.one * size, color, order);
                if (twinkle)
                {
                    Flickering(star, 0.6f, 1.2f, (float)(random.NextDouble() * 100.0));
                }
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

        /// <summary>An invisible area: walking into it shows the prompt to interact. Nothing is drawn on the floor.</summary>
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
        }

        /// <summary>
        /// An everyday object the player can use: walking next to it shows "[E] <paramref name="prompt"/>"
        /// and pressing E shows one of its lines. The zone is a little larger than the object so the
        /// player can reach it even when the object itself is solid.
        /// </summary>
        private static Transform BuildThing(Transform parent, string name, string prompt, Vector2 center, Vector2 size, params string[] lines)
        {
            var go = new GameObject("Thing_" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size + new Vector2(1.2f, 1.2f);

            go.AddComponent<Station>().Configure(StationType.Thing, prompt, lines);
            return go.transform;
        }

        /// <summary>A cash machine zone: it shows the account statement.</summary>
        private static void BuildAtm(Transform parent, string name, Vector2 center)
        {
            var go = new GameObject("Atm_" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.8f, 1.8f);

            go.AddComponent<Station>().Configure(StationType.Atm, "Cajero automático");
        }

        private static void BuildSpawnPoint(Transform parent, string id, Vector2 position)
        {
            var go = new GameObject("Spawn_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<SpawnPoint>().Configure(id);
        }

        private static void Label(Transform parent, string text, Vector2 position, Color color, float size = 0.07f, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = font;
            mesh.fontSize = 100;
            mesh.characterSize = size;
            mesh.fontStyle = FontStyle.Bold;
            mesh.anchor = anchor;
            mesh.color = color;
            go.AddComponent<LocalizedMesh>().Bind(text);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingLayerName = currentLayer;
            renderer.sortingOrder = OrderLabel;
        }
    }
}

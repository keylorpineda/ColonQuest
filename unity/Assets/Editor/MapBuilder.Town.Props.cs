using UnityEngine;
using UnityEngine.Rendering;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        // Floor layer orders used by the town, back to front.
        private const int TownGrass = 0;
        private const int TownGrassPatch = 1;
        private const int TownGroundDetail = 2;
        private const int TownPark = 5;
        private const int TownWalkOutline = 8;
        private const int TownWalk = 9;
        private const int TownWalkPlate = 10;
        private const int TownRoadOutline = 11;
        private const int TownRoad = 12;
        private const int TownAlley = 13;
        private const int TownRoadMark = 14;
        private const int TownShadow = 50;

        private enum Entrance
        {
            Door,
            Garage,
            Window,
            None
        }

        private struct BuildingSpec
        {
            public string Name;
            public string Sign;
            public string Roof;
            public string Wall;
            public float XMin;
            public float XMax;
            public float YBottom;
            public float YTop;
            public float DoorX;
            public float FacadeHeight;
            public Entrance Entrance;
            public string AwningA;
            public string AwningB;
        }

        /// <summary>An object that is sorted by the height of its feet, so the player can pass in front of or behind it.</summary>
        /// <summary>Ground taken by the buildings, so the ground cover is not planted under them.</summary>
        private static readonly System.Collections.Generic.List<Rect> footprints = new System.Collections.Generic.List<Rect>();

        private static Transform Root(Transform parent, string name, Vector2 position, float footY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var group = go.AddComponent<SortingGroup>();
            group.sortingLayerName = LayerWorld;
            group.sortingOrder = YSort.OrderFor(footY);
            return go.transform;
        }

        /// <summary>Draws something flat on the ground layer (shadows, paths, water) and returns to the world layer.</summary>
        private static SpriteRenderer OnGround(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            var renderer = Shape(parent, name, sprite, center, size, color, order);
            currentLayer = previous;
            return renderer;
        }

        private static void GroundShadow(Transform shadows, Vector2 center, Vector2 size, float alpha = 0.28f)
        {
            var color = GamePalette.Outline;
            color.a = alpha;
            OnGround(shadows, "Shadow", softCircle, center, size, color, TownShadow);
        }

        private static void GroundShadowRect(Transform shadows, Vector2 center, Vector2 size, float alpha = 0.25f)
        {
            var color = GamePalette.Outline;
            color.a = alpha;
            color.a = alpha * 0.55f;
            OnGround(shadows, "Shadow", softCircle, center, size, color, TownShadow);
        }

        private static Transform Building(Transform parent, Transform shadows, BuildingSpec b)
        {
            buildingSpecs.Add(b);
            footprints.Add(new Rect(b.XMin - 1f, b.YBottom - 1.5f, b.XMax - b.XMin + 2f, b.YTop - b.YBottom + b.FacadeHeight + 3f));
            float cx = (b.XMin + b.XMax) * 0.5f;
            float w = b.XMax - b.XMin;
            float h = b.YTop - b.YBottom;
            float fh = b.FacadeHeight;
            float dx = b.DoorX - cx;
            float roofH = h - fh + 0.15f;

            var root = Root(parent, b.Name, new Vector2(cx, b.YBottom), b.YBottom);
            var wall = GamePalette.Hex(b.Wall);
            var roof = GamePalette.Hex(b.Roof);

            GroundShadowRect(shadows, new Vector2(cx + 0.3f, b.YBottom - 0.1f), new Vector2(w + 1.2f, 1.8f));

            Shape(root, "Outline", rounded, new Vector2(0f, h * 0.5f), new Vector2(w + 0.16f, h + 0.16f), GamePalette.Outline, 0);
            Shape(root, "Facade", rounded, new Vector2(0f, (fh + 0.4f) * 0.5f), new Vector2(w, fh + 0.4f), wall, 1);

            // Roof with an overhanging eave, a shaded edge, tile lines and a bright ridge.
            float roofY = fh - 0.15f + roofH * 0.5f;
            Shape(root, "Roof_Outline", rounded, new Vector2(0f, roofY), new Vector2(w + 0.66f, roofH + 0.16f), GamePalette.Outline, 2);
            Shape(root, "Roof", rounded, new Vector2(0f, roofY), new Vector2(w + 0.5f, roofH), roof, 3);
            Shape(root, "Eave", roundedSmall, new Vector2(0f, fh - 0.02f), new Vector2(w + 0.3f, 0.3f), GamePalette.Shade(roof, 0.74f), 4);

            var tile = GamePalette.Shade(roof, 0.82f);
            tile.a = 0.55f;
            for (float x = -w * 0.5f + 0.5f; x <= w * 0.5f - 0.4f; x += 0.8f)
            {
                Shape(root, "RoofTile", square, new Vector2(x, fh + (roofH - 0.15f) * 0.5f + 0.1f), new Vector2(0.05f, roofH - 0.9f), tile, 4);
            }

            Shape(root, "Ridge", roundedSmall, new Vector2(0f, h - 0.3f), new Vector2(w - 0.5f, 0.2f), GamePalette.Shade(roof, 1.2f), 4);

            DrawEntrance(root, b, dx, wall);
            DrawFacadeWindows(root, b, w, dx, wall);

            if (b.AwningA != null)
            {
                DrawAwning(root, w, fh, b.AwningA, b.AwningB);
            }

            // Homes carry no shop sign; only the player's house is marked so it can be found.
            if (!string.IsNullOrEmpty(b.Sign))
            {
                DrawSign(root, b.Sign, dx, fh + 0.62f, wall);
            }

            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, h * 0.5f);
            collider.size = new Vector2(w, h);
            return root;
        }

        private static void DrawEntrance(Transform root, BuildingSpec b, float dx, Color wall)
        {
            switch (b.Entrance)
            {
                case Entrance.Door:
                    Shape(root, "Step", roundedSmall, new Vector2(dx, 0.11f), new Vector2(1.9f, 0.22f), GamePalette.Shade(wall, 0.78f), 5);
                    Shape(root, "Door_Outline", roundedSmall, new Vector2(dx, 1.05f), new Vector2(1.4f, 2.0f), GamePalette.Outline, 5);
                    Shape(root, "Door", roundedSmall, new Vector2(dx, 1.0f), new Vector2(1.18f, 1.82f), GamePalette.Hex("#8B5A3C"), 6);
                    Shape(root, "Door_PanelA", roundedSmall, new Vector2(dx, 1.45f), new Vector2(0.8f, 0.6f), GamePalette.Hex("#774A30"), 7);
                    Shape(root, "Door_PanelB", roundedSmall, new Vector2(dx, 0.62f), new Vector2(0.8f, 0.6f), GamePalette.Hex("#774A30"), 7);
                    Shape(root, "Door_Knob", circle, new Vector2(dx + 0.38f, 0.95f), Vector2.one * 0.16f, GamePalette.Marker, 8);
                    break;
                case Entrance.Garage:
                    Shape(root, "Garage_Outline", roundedSmall, new Vector2(dx, 1.1f), new Vector2(3.1f, 2.1f), GamePalette.Outline, 5);
                    Shape(root, "Garage", roundedSmall, new Vector2(dx, 1.05f), new Vector2(2.94f, 1.94f), GamePalette.Hex("#9AA3B8"), 6);
                    for (int i = 0; i < 4; i++)
                    {
                        Shape(root, "Garage_Rib", square, new Vector2(dx, 0.35f + i * 0.45f), new Vector2(2.8f, 0.05f), GamePalette.Hex("#6F7890"), 7);
                    }

                    break;
                case Entrance.Window:
                    Shape(root, "Counter_Outline", roundedSmall, new Vector2(dx, 1.5f), new Vector2(2.3f, 1.3f), GamePalette.Outline, 5);
                    Shape(root, "Counter_Inside", roundedSmall, new Vector2(dx, 1.5f), new Vector2(2.14f, 1.14f), GamePalette.Hex("#3A2E2A"), 6);
                    Shape(root, "Counter_Ledge", roundedSmall, new Vector2(dx, 0.92f), new Vector2(2.5f, 0.26f), GamePalette.Hex("#A9743F"), 8);
                    break;
            }
        }

        private static void DrawFacadeWindows(Transform root, BuildingSpec b, float w, float dx, Color wall)
        {
            float offset = FacadeWindowOffset(b);
            if (offset <= 0f)
            {
                return;
            }

            foreach (float side in new[] { -offset, offset })
            {
                float wx = dx + side;
                if (Mathf.Abs(wx) > w * 0.5f - 0.6f)
                {
                    continue;
                }

                Shape(root, "Window_Outline", roundedSmall, new Vector2(wx, 1.55f), new Vector2(1.2f, 1.1f), GamePalette.Outline, 5);
                Shape(root, "Window", roundedSmall, new Vector2(wx, 1.55f), new Vector2(1.04f, 0.94f), GamePalette.Hex("#BFE3F5"), 6);
                Shape(root, "Window_FrameV", square, new Vector2(wx, 1.55f), new Vector2(0.05f, 0.94f), GamePalette.White, 7);
                Shape(root, "Window_FrameH", square, new Vector2(wx, 1.55f), new Vector2(1.04f, 0.05f), GamePalette.White, 7);
                var shine = GamePalette.White;
                shine.a = 0.55f;
                Shape(root, "Window_Shine", roundedSmall, new Vector2(wx - 0.27f, 1.78f), new Vector2(0.22f, 0.3f), shine, 7);
            }
        }

        /// <summary>
        /// How far from the door the two front windows sit, or 0 when the front has none. Narrow houses
        /// bring their windows closer to the door instead of losing them.
        /// </summary>
        private static float FacadeWindowOffset(BuildingSpec b)
        {
            if (b.Entrance != Entrance.Door && b.Entrance != Entrance.Garage)
            {
                return 0f;
            }

            float w = b.XMax - b.XMin;
            float offset = Mathf.Min(2.7f, w * 0.5f - 1.25f);
            float doorHalf = b.Entrance == Entrance.Garage ? 1.55f : 0.7f;
            return offset >= doorHalf + 0.75f ? offset : 0f;
        }

        private static void DrawAwning(Transform root, float w, float fh, string a, string b)
        {
            Shape(root, "Awning_Outline", roundedSmall, new Vector2(0f, fh - 0.4f), new Vector2(w + 0.1f, 0.86f), GamePalette.Outline, 8);
            int stripes = Mathf.RoundToInt(w / 0.9f);
            float width = w / stripes;
            for (int i = 0; i < stripes; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * width;
                Shape(root, "Awning", square, new Vector2(x, fh - 0.4f), new Vector2(width, 0.7f), GamePalette.Hex(i % 2 == 0 ? a : b), 9);
            }
        }

        private static void DrawSign(Transform root, string text, float dx, float y, Color wall)
        {
            float length = Mathf.Max(1.7f, text.Length * 0.33f + 0.7f);
            Shape(root, "Sign_Outline", roundedSmall, new Vector2(dx, y), new Vector2(length + 0.14f, 0.7f), GamePalette.Outline, 10);
            Shape(root, "Sign", roundedSmall, new Vector2(dx, y), new Vector2(length, 0.56f), GamePalette.Hex("#F8F5E8"), 11);
            Label(root, text, new Vector2(dx, y), GamePalette.Outline, 0.042f, TextAnchor.MiddleCenter);
        }

        private static void Tree(Transform parent, Transform shadows, float x, float y, float scale, string leaf, bool fruit)
        {
            if (OnPavement(x, y, 0.3f) || InsideBuilding(x, y))
            {
                return;
            }

            var root = Root(parent, "Tree", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.25f, y - 0.05f), new Vector2(2.1f * scale, 0.75f * scale));

            var color = GamePalette.Hex(leaf);
            Shape(root, "Trunk_Outline", roundedSmall, new Vector2(0f, 0.5f * scale), new Vector2(0.5f * scale, 1.0f * scale), GamePalette.Outline, 0);
            Shape(root, "Trunk", roundedSmall, new Vector2(0f, 0.46f * scale), new Vector2(0.34f * scale, 0.9f * scale), GamePalette.Hex("#8B5A3C"), 1);
            Shape(root, "Canopy_Outline", circle, new Vector2(0f, 1.55f * scale), Vector2.one * 2.1f * scale, GamePalette.Outline, 2);
            Shape(root, "Canopy", circle, new Vector2(0f, 1.55f * scale), Vector2.one * 1.9f * scale, color, 3);
            var light = GamePalette.Shade(color, 1.25f);
            light.a = 0.6f;
            Shape(root, "Canopy_Light", circle, new Vector2(-0.4f * scale, 1.9f * scale), Vector2.one * 0.9f * scale, light, 4);

            if (fruit)
            {
                foreach (var spot in new[] { new Vector2(0.45f, 1.7f), new Vector2(-0.2f, 1.25f), new Vector2(0.15f, 2.05f) })
                {
                    Shape(root, "Fruit", circle, spot * scale, Vector2.one * 0.24f * scale, GamePalette.Hex("#FF9F3D"), 5);
                }
            }

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f;
            collider.offset = new Vector2(0f, 0.2f);
        }

        private static void Lamp(Transform parent, float x, float y)
        {
            var root = Root(parent, "Lamp", new Vector2(x, y), y);
            Shape(root, "Post_Outline", roundedSmall, new Vector2(0f, 0.95f), new Vector2(0.26f, 1.9f), GamePalette.Outline, 0);
            Shape(root, "Post", roundedSmall, new Vector2(0f, 0.9f), new Vector2(0.14f, 1.8f), GamePalette.Hex("#4A5370"), 1);
            Shape(root, "Head_Outline", circle, new Vector2(0f, 1.95f), Vector2.one * 0.52f, GamePalette.Outline, 2);
            Shape(root, "Head", circle, new Vector2(0f, 1.95f), Vector2.one * 0.38f, GamePalette.Hex("#FFE08A"), 3);
            Flickering(Glow(root, "Glow", new Vector2(0f, 1.8f), 3.6f, "#FFD98A", 0.2f, 20), 0.12f, 1.3f, x * 3.1f);

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.15f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        private static void Bench(Transform parent, Transform shadows, float x, float y)
        {
            var root = Root(parent, "Bench", new Vector2(x, y), y);
            BuildThing(root, "Bench", "Sentarte en la banca", Vector2.zero, new Vector2(2.0f, 0.7f),
                "Te sentás un rato a ver pasar la gente. Pasear por el parque no cuesta nada.");
            GroundShadowRect(shadows, new Vector2(x + 0.08f, y - 0.02f), new Vector2(2.0f, 0.7f), 0.22f);
            Shape(root, "Back_Outline", roundedSmall, new Vector2(0f, 0.62f), new Vector2(2.0f, 0.34f), GamePalette.Outline, 0);
            Shape(root, "Back", roundedSmall, new Vector2(0f, 0.62f), new Vector2(1.86f, 0.2f), GamePalette.Hex("#8B5A3C"), 1);
            Shape(root, "Seat_Outline", roundedSmall, new Vector2(0f, 0.28f), new Vector2(2.0f, 0.62f), GamePalette.Outline, 2);
            Shape(root, "Seat", roundedSmall, new Vector2(0f, 0.28f), new Vector2(1.84f, 0.46f), GamePalette.Hex("#B07A45"), 3);

            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.offset = new Vector2(0f, 0.3f);
            collider.size = new Vector2(2.0f, 0.6f);
        }

        private static void Bush(Transform parent, Transform shadows, float x, float y, float scale)
        {
            if (OnPavement(x, y, 0.5f) || InsideBuilding(x, y))
            {
                return;
            }

            var root = Root(parent, "Bush", new Vector2(x, y), y);
            GroundShadow(shadows, new Vector2(x + 0.15f, y), new Vector2(1.3f * scale, 0.5f * scale), 0.22f);
            Shape(root, "Outline", circle, new Vector2(0f, 0.45f * scale), Vector2.one * 1.2f * scale, GamePalette.Outline, 0);
            Shape(root, "Bush", circle, new Vector2(0f, 0.45f * scale), Vector2.one * 1.04f * scale, GamePalette.Hex("#4CB36F"), 1);
            var light = GamePalette.Hex("#7FD79A");
            light.a = 0.7f;
            Shape(root, "Light", circle, new Vector2(-0.2f * scale, 0.65f * scale), Vector2.one * 0.4f * scale, light, 2);
        }

        /// <summary>A car on the street. With a speed it drives along its lane and wraps around; without, it stands parked.</summary>
        private static void Car(Transform parent, Transform shadows, float x, float y, string hex, float speed = 0f)
        {
            var root = Root(parent, "Car", new Vector2(x, y), y - 0.55f);
            bool driving = speed != 0f;
            if (driving)
            {
                // The shadow travels with the car, and a car that moves has nothing to interact with.
                var shadowColor = GamePalette.Outline;
                shadowColor.a = 0.16f;
                OnGround(root, "Shadow", softCircle, new Vector2(0.12f, -0.2f), new Vector2(2.6f, 1.3f), shadowColor, TownShadow);
                root.gameObject.AddComponent<Drift>().Configure(new Vector2(speed, 0f), new Vector2(MapLayout.PlayEastX - 4f, 100f));
                if (speed < 0f)
                {
                    root.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            else
            {
                BuildThing(root, "Car", "Ver el carro", Vector2.zero, new Vector2(2.4f, 1.1f),
                    "Un carro no es solo la cuota: marchamo, RTV, seguro, gasolina y mantenimiento, todos los años.");
                GroundShadowRect(shadows, new Vector2(x + 0.12f, y - 0.2f), new Vector2(2.6f, 1.3f), 0.3f);
            }

            var body = GamePalette.Hex(hex);
            foreach (float sx in new[] { -0.8f, 0.8f })
            {
                foreach (float sy in new[] { -0.64f, 0.64f })
                {
                    Shape(root, "Wheel", roundedSmall, new Vector2(sx, sy), new Vector2(0.56f, 0.26f), GamePalette.Outline, 0);
                }
            }

            Shape(root, "Body_Outline", rounded, Vector2.zero, new Vector2(2.66f, 1.36f), GamePalette.Outline, 1);
            Shape(root, "Body", rounded, Vector2.zero, new Vector2(2.5f, 1.2f), body, 2);
            Shape(root, "Windshield", roundedSmall, new Vector2(0.62f, 0f), new Vector2(0.5f, 0.9f), GamePalette.Hex("#A9DDF2"), 3);
            Shape(root, "RearWindow", roundedSmall, new Vector2(-0.88f, 0f), new Vector2(0.36f, 0.86f), GamePalette.Hex("#8FC3DA"), 3);
            Shape(root, "Roof", rounded, new Vector2(-0.12f, 0f), new Vector2(1.0f, 0.96f), GamePalette.Shade(body, 1.12f), 3);
            Shape(root, "Headlight", circle, new Vector2(1.18f, 0.4f), Vector2.one * 0.2f, GamePalette.Hex("#FFF1A8"), 4);
            Shape(root, "Headlight", circle, new Vector2(1.18f, -0.4f), Vector2.one * 0.2f, GamePalette.Hex("#FFF1A8"), 4);

            if (!driving)
            {
                var collider = root.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2.5f, 1.1f);
            }
        }

        private static void Hedge(Transform parent, Vector2 center, Vector2 size)
        {
            var root = Root(parent, "Hedge", center, center.y - size.y * 0.5f);
            Shape(root, "Outline", rounded, Vector2.zero, size + Vector2.one * 0.16f, GamePalette.Outline, 0);
            Shape(root, "Hedge", rounded, Vector2.zero, size, GamePalette.Hex("#3B9660"), 1);

            var light = GamePalette.Hex("#58B97C");
            light.a = 0.6f;
            float length = Mathf.Max(size.x, size.y);
            bool horizontal = size.x >= size.y;
            for (float t = -length * 0.5f + 1f; t < length * 0.5f - 0.5f; t += 2.2f)
            {
                Vector2 spot = horizontal ? new Vector2(t, 0.2f) : new Vector2(0.2f, t);
                Shape(root, "Leaf", circle, spot, Vector2.one * 0.55f, light, 2);
            }

            root.gameObject.AddComponent<BoxCollider2D>().size = size;
        }

        private static void Fountain(Transform parent, Transform shadows, Vector2 center)
        {
            GroundShadow(shadows, center + new Vector2(0.25f, -0.55f), new Vector2(4.8f, 2.2f), 0.25f);
            var root = Root(parent, "Fountain", center, center.y - 1.3f);
            BuildThing(root, "Fountain", "Ver la fuente", Vector2.zero, new Vector2(3.7f, 3.0f),
                "Hay monedas en el fondo. Pedir un deseo es gratis; cumplirlo casi siempre empieza por ahorrar.");

            var stone = GamePalette.Hex("#D5DAE8");
            var water = GamePalette.Hex("#5CB8E6");

            // The wide basin, seen from a little above: a stone rim, the inner wall and the water.
            Shape(root, "Basin_Outline", circle, new Vector2(0f, 0f), new Vector2(4.3f, 2.9f), GamePalette.Outline, 0);
            Shape(root, "Basin_Front", circle, new Vector2(0f, -0.1f), new Vector2(4.1f, 2.7f), GamePalette.Shade(stone, 0.78f), 1);
            Shape(root, "Basin_Rim", circle, new Vector2(0f, 0.05f), new Vector2(4.1f, 2.6f), stone, 2);
            Shape(root, "Basin_Wall", circle, new Vector2(0f, 0.08f), new Vector2(3.5f, 2.1f), GamePalette.Shade(stone, 0.72f), 3);
            Shape(root, "Water", circle, new Vector2(0f, 0.04f), new Vector2(3.3f, 1.95f), water, 4);

            var glint = GamePalette.White;
            glint.a = 0.35f;
            Shape(root, "Water_Glint", softCircle, new Vector2(-0.8f, 0.25f), new Vector2(1.6f, 0.5f), glint, 5);
            var ripple = GamePalette.White;
            ripple.a = 0.5f;
            Flickering(Shape(root, "Ripple", ring, new Vector2(0f, 0.04f), new Vector2(2.7f, 1.5f), ripple, 5), 0.5f, 1.6f, 3f);
            Flickering(Shape(root, "Ripple", ring, new Vector2(0f, 0.04f), new Vector2(1.9f, 1.05f), ripple, 5), 0.5f, 2.1f, 9f);

            // The pedestal rises from the middle: a base, a column and a smaller bowl on top.
            Shape(root, "Base_Outline", circle, new Vector2(0f, 0.1f), new Vector2(1.5f, 0.85f), GamePalette.Outline, 6);
            Shape(root, "Base", circle, new Vector2(0f, 0.12f), new Vector2(1.34f, 0.72f), stone, 7);
            Shape(root, "Column_Outline", roundedSmall, new Vector2(0f, 0.75f), new Vector2(0.56f, 1.3f), GamePalette.Outline, 8);
            Shape(root, "Column", roundedSmall, new Vector2(0f, 0.75f), new Vector2(0.4f, 1.24f), GamePalette.Shade(stone, 1.04f), 9);
            Shape(root, "Bowl_Outline", circle, new Vector2(0f, 1.45f), new Vector2(2.1f, 1.0f), GamePalette.Outline, 10);
            Shape(root, "Bowl_Front", circle, new Vector2(0f, 1.38f), new Vector2(1.96f, 0.9f), GamePalette.Shade(stone, 0.78f), 11);
            Shape(root, "Bowl_Rim", circle, new Vector2(0f, 1.46f), new Vector2(1.96f, 0.86f), stone, 12);
            Shape(root, "Bowl_Water", circle, new Vector2(0f, 1.48f), new Vector2(1.6f, 0.68f), water, 13);
            Shape(root, "Finial", circle, new Vector2(0f, 1.75f), Vector2.one * 0.3f, GamePalette.Shade(stone, 1.06f), 14);

            // Jets arch out of the top and fall into the bowl; the droplets keep flickering.
            var spray = GamePalette.White;
            spray.a = 0.85f;
            for (int k = -2; k <= 2; k++)
            {
                float angle = k * 24f;
                float length = 0.95f - Mathf.Abs(k) * 0.12f;
                var jet = Shape(root, "Jet", roundedSmall,
                    new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * length * 0.55f, 1.8f + Mathf.Cos(angle * Mathf.Deg2Rad) * length * 0.5f),
                    new Vector2(0.07f, length), spray, 15);
                Tilt(jet, -angle);
                Flickering(jet, 0.55f, 2.4f + Mathf.Abs(k) * 0.3f, k * 1.7f);
            }

            for (int k = -3; k <= 3; k++)
            {
                var drop = Shape(root, "Drop", circle, new Vector2(k * 0.34f, 1.7f - Mathf.Abs(k) * 0.12f), Vector2.one * 0.1f, spray, 15);
                Flickering(drop, 0.3f, 3.1f, k * 2.3f);
            }

            var collider = root.gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 1.45f;
            collider.offset = new Vector2(0f, 0.1f);
        }

        /// <summary>Colored dots on the ground for flower beds and wildflowers.</summary>
        private static void Flowers(Transform parent, System.Random random, Rect area, int count)
        {
            var colors = new[] { "#FFD23F", "#FF8FB1", "#F1F4FF", "#FF7A5C", "#B28DFF" };
            for (int i = 0; i < count; i++)
            {
                float x = area.xMin + (float)random.NextDouble() * area.width;
                float y = area.yMin + (float)random.NextDouble() * area.height;
                if (OnPavement(x, y, 0.3f) || InsideBuilding(x, y))
                {
                    continue;
                }

                OnGround(parent, "Flower", circle, new Vector2(x, y), Vector2.one * (0.16f + (float)random.NextDouble() * 0.08f),
                    GamePalette.Hex(colors[random.Next(colors.Length)]), TownGroundDetail + 1);
            }
        }

        private static void DashedLine(Transform parent, float y, float xMin, float xMax)
        {
            var color = GamePalette.White;
            color.a = 0.8f;
            for (float x = xMin + 1f; x < xMax - 1f; x += 2.8f)
            {
                OnGround(parent, "Dash", square, new Vector2(x, y), new Vector2(1.3f, 0.13f), color, TownRoadMark);
            }
        }

        private static void Crosswalk(Transform parent, float x, float yMin, float yMax)
        {
            var color = GamePalette.White;
            color.a = 0.85f;
            for (float y = yMin + 0.35f; y < yMax - 0.2f; y += 0.75f)
            {
                OnGround(parent, "Stripe", square, new Vector2(x, y), new Vector2(1.5f, 0.38f), color, TownRoadMark);
            }
        }
    }
}

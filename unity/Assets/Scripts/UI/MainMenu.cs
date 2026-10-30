using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The first screen of the game, built from code like the rest of the interface. The picture has depth: a sky with a sun and
    /// birds, far mountains, the rock, the sea and the sand, then the town and the palms in front, and each layer moves a little
    /// with the mouse. "Modo historia" starts a fresh life behind the loading screen; "Jugar online" explains what the online
    /// mode is.
    /// </summary>
    public sealed partial class MainMenu : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;

        /// <summary>The width of the bands that cross the whole screen: the scale follows the height, so a 21:9 screen is about 2520 wide.</summary>
        private const float BackdropWidth = 3400f;
        private const float CloudWrap = 400f;

        private static readonly Color Cream = GamePalette.Hex("#F6F0D4");

        private sealed class Layer
        {
            public RectTransform Rect;
            public float Depth;
        }

        private readonly List<Layer> layers = new List<Layer>();
        private readonly List<RectTransform> clouds = new List<RectTransform>();
        private readonly List<float> cloudSpeeds = new List<float>();
        private readonly List<RectTransform> coins = new List<RectTransform>();
        private readonly List<float> coinRest = new List<float>();
        private readonly List<RectTransform> puffs = new List<RectTransform>();
        private readonly List<RectTransform> waves = new List<RectTransform>();
        private readonly List<RectTransform> birds = new List<RectTransform>();
        private readonly List<RectTransform> rays = new List<RectTransform>();

        // What slides in when the menu opens: the title, and each button a moment after the other.
        private readonly List<RectTransform> entering = new List<RectTransform>();
        private readonly List<Vector2> enterTarget = new List<Vector2>();
        private readonly List<float> enterFrom = new List<float>();

        private Transform root;
        private Vector2 parallax;
        private float openedAt;
        private GameObject notice;
        private GameObject credits;
        private SettingsPanel settings;

        private void Awake()
        {
            AudioDirector.EnsureCreated();
            BuildCanvas();
            BuildBackdrop();
            BuildTitle();
            BuildButtons();
            BuildFooter();
            BuildNotice();
            BuildCredits();
            BuildSlots();
            settings = new SettingsPanel(root, null);
            openedAt = Time.unscaledTime;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            for (int i = 0; i < clouds.Count; i++)
            {
                var position = clouds[i].anchoredPosition;
                position.x = MenuMotion.Drift(position.x, cloudSpeeds[i], dt, -ReferenceWidth * 0.5f - CloudWrap, ReferenceWidth * 0.5f + CloudWrap);
                clouds[i].anchoredPosition = position;
            }

            for (int i = 0; i < coins.Count; i++)
            {
                var position = coins[i].anchoredPosition;
                position.y = coinRest[i] + MenuMotion.Bob(now, i * 1.3f, 14f);
                coins[i].anchoredPosition = position;
            }

            for (int i = 0; i < puffs.Count; i++)
            {
                var position = puffs[i].anchoredPosition;
                position.y = MenuMotion.Drift(position.y, 40f, dt, 380f, 800f);
                position.x = 620f + (position.y - 380f) * 0.25f;
                puffs[i].anchoredPosition = position;
            }

            // The foam breathes in and out, the birds flap and cross the sky, the rays of the sun turn slowly.
            for (int i = 0; i < waves.Count; i++)
            {
                var position = waves[i].anchoredPosition;
                position.x = -480f + Mathf.Sin(now * 0.6f + i) * 36f;
                waves[i].anchoredPosition = position;
            }

            for (int i = 0; i < birds.Count; i++)
            {
                var position = birds[i].anchoredPosition;
                position.x = MenuMotion.Drift(position.x, 55f + i * 12f, dt, -ReferenceWidth * 0.5f - 100f, ReferenceWidth * 0.5f + 100f);
                position.y = 330f + i * 60f + Mathf.Sin(now * 1.4f + i * 2f) * 14f;
                birds[i].anchoredPosition = position;
                birds[i].localScale = new Vector3(1f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(now * 5f + i)), 1f);
            }

            foreach (var ray in rays)
            {
                ray.localRotation = Quaternion.Euler(0f, 0f, now * 4f);
            }

            MoveLayers(dt);
            SlideIn(now - openedAt);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindows();
            }
        }

        /// <summary>The layers drift against the mouse, the near ones more than the far ones: that is the depth.</summary>
        private void MoveLayers(float dt)
        {
            var mouse = new Vector2(Input.mousePosition.x / Mathf.Max(1f, Screen.width), Input.mousePosition.y / Mathf.Max(1f, Screen.height)) - new Vector2(0.5f, 0.5f);
            mouse = Vector2.ClampMagnitude(mouse * 2f, 1f);
            parallax = Vector2.Lerp(parallax, mouse, Mathf.Clamp01(dt * 3f));
            foreach (var layer in layers)
            {
                layer.Rect.anchoredPosition = -parallax * layer.Depth;
            }
        }

        private void SlideIn(float seconds)
        {
            for (int i = 0; i < entering.Count; i++)
            {
                float t = Mathf.Clamp01((seconds - 0.15f * i) / 0.7f);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                var position = enterTarget[i];
                position.x -= enterFrom[i] * (1f - ease);
                entering[i].anchoredPosition = position;
            }
        }

        private void Slide(RectTransform rect, float from)
        {
            entering.Add(rect);
            enterTarget.Add(rect.anchoredPosition);
            enterFrom.Add(from);
        }

        /// <summary>True while a window (notice, credits or settings) covers the buttons.</summary>
        public bool WindowOpen => notice.activeSelf || credits.activeSelf || settings.IsOpen || slots.activeSelf;

        /// <summary>Starts a new life saved in a slot and goes to the town, behind the loading screen.</summary>
        public void StartStory(int slot, string runName)
        {
            // The first run is created fresh by the session itself; a later one has to be restarted.
            bool existed = GameSession.Instance != null;
            var session = GameSession.Create();
            if (existed)
            {
                session.StartNewGame();
            }

            session.AssignSlot(slot, runName);
            CloseWindows();
            LoadingScreen.Load(SceneNames.Town);
        }

        public void CloseWindows()
        {
            notice.SetActive(false);
            credits.SetActive(false);
            slots.SetActive(false);
            settings.Hide();
        }

        private static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ---------------------------------------------------------------- build

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, 1080f);
            // The signs stack down the left side, so the height decides the scale: every sign fits in a wide or short
            // window, and a wider screen only shows more of the beach.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            gameObject.AddComponent<GraphicRaycaster>();
            root = transform;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(events);
            }
        }

        /// <summary>A group that covers the screen and moves as a whole with the mouse. Its children are drawn in order.</summary>
        private Transform NewLayer(string layerName, float depth)
        {
            var rect = UiKit.Rect(root, layerName, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-80f, 0f);
            rect.offsetMax = new Vector2(80f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            layers.Add(new Layer { Rect = rect, Depth = depth });
            return rect;
        }

        private void BuildBackdrop()
        {
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);

            // The sky: bands from a deep blue at the top to a pale one at the horizon.
            var sky = NewLayer("SkyLayer", 4f);
            Color top = GamePalette.Hex("#4FA9EE");
            Color horizon = GamePalette.Hex("#D5EFFF");
            const int bands = 8;
            for (int i = 0; i < bands; i++)
            {
                float t = i / (float)(bands - 1);
                var band = UiKit.Fill(sky, "Sky" + i, Color.Lerp(top, horizon, t), new Vector2(BackdropWidth, 1100f / bands + 4f), new Vector2(0.5f, 1f), new Vector2(0f, 60f - i * (1100f / bands)));
                band.sprite = null;
            }

            // The sun with its glow and a few slow rays.
            var sun = NewLayer("SunLayer", 8f);
            UiKit.Round(sun, "SunGlow", new Color(1f, 0.95f, 0.7f, 0.25f), 520f, center, new Vector2(560f, 300f));
            UiKit.Round(sun, "SunGlow2", new Color(1f, 0.95f, 0.7f, 0.35f), 360f, center, new Vector2(560f, 300f));
            for (int i = 0; i < 3; i++)
            {
                var ray = UiKit.Fill(sun, "Ray" + i, new Color(1f, 0.97f, 0.75f, 0.14f), new Vector2(900f, 34f), center, new Vector2(560f, 300f));
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
                rays.Add(ray.rectTransform);
            }

            UiKit.Round(sun, "Sun", GamePalette.Hex("#FFE27A"), 190f, center, new Vector2(560f, 300f));

            // Clouds made of overlapping discs.
            var far = NewLayer("FarLayer", 12f);
            float[] heights = { 360f, 200f, 440f, 130f };
            float[] sizes = { 1.1f, 0.8f, 0.95f, 0.7f };
            for (int i = 0; i < heights.Length; i++)
            {
                clouds.Add(Cloud(far, new Vector2(-760f + i * 520f, heights[i]), sizes[i]));
                cloudSpeeds.Add(12f + i * 5f);
            }

            // Birds far away.
            for (int i = 0; i < 3; i++)
            {
                birds.Add(Bird(far, new Vector2(-600f + i * 520f, 330f + i * 60f)));
            }

            // Blue mountains behind everything.
            var mountains = NewLayer("MountainLayer", 18f);
            Mountain(mountains, new Vector2(-150f, 120f), 760f, 380f, GamePalette.Hex("#7FA6D6"));
            Mountain(mountains, new Vector2(520f, 100f), 640f, 300f, GamePalette.Hex("#8DB4E0"));
            Mountain(mountains, new Vector2(1000f, 90f), 560f, 250f, GamePalette.Hex("#7FA6D6"));

            // The rocky hill, the sea on the left and the sandy shore on the right.
            var mid = NewLayer("MidLayer", 26f);
            UiKit.Fill(mid, "Rock", GamePalette.Hex("#3C4259"), new Vector2(900f, 760f), bottom, new Vector2(620f, -240f));
            UiKit.Fill(mid, "RockLight", GamePalette.Hex("#4F5775"), new Vector2(520f, 600f), bottom, new Vector2(820f, -200f));
            UiKit.Fill(mid, "Sea", GamePalette.Hex("#5BC0DE"), new Vector2(2400f, 360f), bottom, new Vector2(-970f, -150f));
            UiKit.Fill(mid, "SeaDeep", GamePalette.Hex("#3EA5CF"), new Vector2(2400f, 150f), bottom, new Vector2(-970f, -190f));
            for (int i = 0; i < 3; i++)
            {
                var foam = UiKit.Fill(mid, "Foam" + i, new Color(1f, 1f, 1f, 0.5f - i * 0.12f), new Vector2(1000f - i * 220f, 28f - i * 6f), bottom, new Vector2(-480f, 140f - i * 52f));
                waves.Add(foam.rectTransform);
            }

            UiKit.Fill(mid, "Sand", GamePalette.Hex("#EBC77E"), new Vector2(2400f, 520f), bottom, new Vector2(1060f, -260f));
            UiKit.Fill(mid, "SandLight", GamePalette.Hex("#F4D898"), new Vector2(900f, 300f), bottom, new Vector2(700f, -150f));

            // The town on the shore: two little houses with a bank between them, then the palms and the coins in front.
            var near = NewLayer("NearLayer", 48f);
            House(near, "HouseA", bottom, new Vector2(300f, 190f), new Vector2(120f, 110f), GamePalette.Hex("#F2B8B0"), GamePalette.Hex("#C2453D"));
            House(near, "Bank", bottom, new Vector2(620f, 210f), new Vector2(170f, 150f), GamePalette.Hex("#BFD0F2"), GamePalette.Hex("#3E5BA8"));
            House(near, "HouseB", bottom, new Vector2(900f, 180f), new Vector2(130f, 100f), GamePalette.Hex("#F8E4A0"), GamePalette.Hex("#C27A2C"));

            Palm(near, bottom, new Vector2(-30f, 250f), 1f);
            Palm(near, bottom, new Vector2(1110f, 230f), 1.15f);
            Palm(near, bottom, new Vector2(-760f, 120f), 0.9f);

            // Smoke from the chimney of the bank, rising and drifting.
            for (int i = 0; i < 5; i++)
            {
                var puff = UiKit.Round(near, "Smoke" + i, new Color(0.45f, 0.47f, 0.52f, 0.6f), 40f + i * 9f, bottom, new Vector2(620f, 380f + i * 70f));
                puffs.Add(puff.rectTransform);
            }

            float[] coinX = { 360f, 760f, 960f };
            float[] coinY = { 20f, 70f, -20f };
            for (int i = 0; i < coinX.Length; i++)
            {
                var coin = UiKit.Card(near, "Coin" + i, GameUiColors.Yellow, new Vector2(84f, 84f), center, new Vector2(coinX[i], coinY[i]));
                var outer = (RectTransform)coin.parent;
                outer.GetComponent<Image>().raycastTarget = false;
                UiKit.Label(coin, "C", 46, UiKit.Ink, new Vector2(70f, 70f), center, Vector2.zero, TextAnchor.MiddleCenter);
                coins.Add(outer);
                coinRest.Add(coinY[i]);
            }

            // A soft shade at the edges keeps the eyes on the middle.
            var shade = new Color(0.03f, 0.05f, 0.15f, 1f);
            for (int i = 0; i < 4; i++)
            {
                var bar = UiKit.Fill(root, "ShadeBottom" + i, new Color(shade.r, shade.g, shade.b, 0.10f), new Vector2(BackdropWidth, 70f + i * 50f), bottom, Vector2.zero);
                bar.sprite = null;
                var barTop = UiKit.Fill(root, "ShadeTop" + i, new Color(shade.r, shade.g, shade.b, 0.07f), new Vector2(BackdropWidth, 40f + i * 40f), new Vector2(0.5f, 1f), Vector2.zero);
                barTop.sprite = null;
            }
        }

        private RectTransform Cloud(Transform parent, Vector2 at, float scale)
        {
            var group = UiKit.Rect(parent, "Cloud", Vector2.zero, new Vector2(0.5f, 0.5f), at);
            var white = new Color(1f, 1f, 1f, 0.9f);
            var shade = new Color(0.82f, 0.9f, 0.98f, 0.9f);
            UiKit.Round(group, "Shade", shade, 120f * scale, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f) * scale);
            UiKit.Round(group, "PuffL", white, 130f * scale, new Vector2(0.5f, 0.5f), new Vector2(-80f, 0f) * scale);
            UiKit.Round(group, "PuffM", white, 190f * scale, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f) * scale);
            UiKit.Round(group, "PuffR", white, 140f * scale, new Vector2(0.5f, 0.5f), new Vector2(90f, 4f) * scale);
            UiKit.Fill(group, "Base", white, new Vector2(300f, 70f) * scale, new Vector2(0.5f, 0.5f), new Vector2(5f, -26f) * scale);
            return group;
        }

        private RectTransform Bird(Transform parent, Vector2 at)
        {
            var group = UiKit.Rect(parent, "Bird", Vector2.zero, new Vector2(0.5f, 0.5f), at);
            var ink = GamePalette.Hex("#2A3345");
            var left = UiKit.Fill(group, "WingL", ink, new Vector2(34f, 6f), new Vector2(0.5f, 0.5f), new Vector2(-15f, 0f));
            left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            var right = UiKit.Fill(group, "WingR", ink, new Vector2(34f, 6f), new Vector2(0.5f, 0.5f), new Vector2(15f, 0f));
            right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            return group;
        }

        private static void Mountain(Transform parent, Vector2 at, float width, float height, Color color)
        {
            var bottom = new Vector2(0.5f, 0f);
            var body = UiKit.Round(parent, "Mountain", color, width, bottom, new Vector2(at.x, at.y - width * 0.5f));
            body.rectTransform.sizeDelta = new Vector2(width, height * 2f);
            body.rectTransform.anchoredPosition = new Vector2(at.x, at.y - height);
            UiKit.Round(parent, "MountainShade", Color.Lerp(color, GamePalette.Hex("#1D2B4F"), 0.18f), width * 0.55f, bottom, new Vector2(at.x + width * 0.14f, at.y - height * 0.7f));
        }

        private static void House(Transform parent, string houseName, Vector2 anchor, Vector2 position, Vector2 size, Color wall, Color roof)
        {
            var body = UiKit.Card(parent, houseName, wall, size, anchor, position);
            ((RectTransform)body.parent).GetComponent<Image>().raycastTarget = false;
            UiKit.Fill(parent, houseName + "Roof", roof, new Vector2(size.x + 30f, 46f), anchor, position + new Vector2(0f, size.y - 4f));
            UiKit.Fill(parent, houseName + "Door", UiKit.Ink, new Vector2(size.x * 0.22f, size.y * 0.5f), anchor, position + new Vector2(0f, 4f));
            // A lit window.
            UiKit.Fill(parent, houseName + "Window", GamePalette.Hex("#FFE9A8"), new Vector2(size.x * 0.2f, size.y * 0.22f), anchor, position + new Vector2(-size.x * 0.28f, size.y * 0.52f));
        }

        private static void Palm(Transform parent, Vector2 anchor, Vector2 position, float scale)
        {
            var trunk = UiKit.Fill(parent, "PalmTrunk", GamePalette.Hex("#8A5A2B"), new Vector2(26f, 230f) * scale, anchor, position + new Vector2(0f, 110f * scale));
            trunk.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            float[] tilt = { 28f, -28f, 0f };
            for (int i = 0; i < tilt.Length; i++)
            {
                float dx = i == 0 ? 60f : i == 1 ? -60f : 0f;
                var leaf = UiKit.Fill(parent, "PalmLeaf", GamePalette.Hex(i == 2 ? "#3E9A57" : "#2F8449"), new Vector2(190f, 56f) * scale, anchor, position + new Vector2(dx, 340f * scale - Mathf.Abs(tilt[i]) * 0.9f));
                leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt[i] * 0.8f);
            }
        }

        private void BuildTitle()
        {
            var topLeft = new Vector2(0f, 1f);
            var title = UiKit.Label(root, "Colón Quest", 150, Cream, new Vector2(1000f, 190f), topLeft, new Vector2(80f, -50f), TextAnchor.MiddleLeft);
            title.font = UiKit.TitleFont;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Outlined(title, 8f);
            Shadowed(title);
            Slide(title.rectTransform, 700f);

            var subtitle = UiKit.Label(root, "Aprendé a manejar tu plata", 44, Cream, new Vector2(900f, 56f), topLeft, new Vector2(92f, -235f), TextAnchor.MiddleLeft);
            subtitle.font = UiKit.TitleFont;
            Outlined(subtitle, 5f);
            Slide(subtitle.rectTransform, 700f);
        }

        private static void Outlined(Text label, float width)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(width, -width);
            outline.useGraphicAlpha = false;
        }

        private static void Shadowed(Text label)
        {
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(14f, -16f);
        }

        private void BuildButtons()
        {
            var topLeft = new Vector2(0f, 1f);
            var wood = GamePalette.Hex("#C58B52");
            var entries = MenuEntries.For(Application.platform);

            // The post the signs hang from, with a darker side to give it roundness.
            UiKit.Fill(root, "Post", GamePalette.Hex("#7A4E26"), new Vector2(40f, 780f), topLeft, new Vector2(300f, -330f));
            UiKit.Fill(root, "PostShade", GamePalette.Hex("#5E3A1B"), new Vector2(12f, 780f), topLeft, new Vector2(328f, -330f));

            float[] widths = { 600f, 540f, 420f, 380f, 320f };
            float[] tilts = { 2f, -2.5f, 1.8f, -2f, 2.4f };
            float[] shifts = { 90f, 20f, 120f, 150f, 170f };
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float height = i < 2 ? 92f : 76f;
                var color = entry == MenuEntry.Story ? GamePalette.Hex("#E0A560") : wood;
                var button = UiKit.MakeButton(root, MenuEntries.LabelOf(entry), color, new Vector2(widths[i], height), topLeft, new Vector2(shifts[i], -350f - i * 128f), () => Press(entry));
                button.transform.localRotation = Quaternion.Euler(0f, 0f, tilts[i]);
                var label = button.transform.Find("Body/Text").GetComponent<Text>();
                label.font = UiKit.TitleFont;
                label.fontSize = i == 0 ? 44 : 38;
                Slide((RectTransform)button.transform, 900f + i * 120f);
            }
        }

        /// <summary>The alpha version top left, the credit bottom left and a quiet note bottom center: small, and out of the way of the picture.</summary>
        private void BuildFooter()
        {
            var version = UiKit.Label(root, BuildInfo.Label, 26, UiKit.Ink, new Vector2(300f, 34f), new Vector2(0f, 1f), new Vector2(22f, -12f), TextAnchor.MiddleLeft);
            version.name = "Version";

            var credit = UiKit.Label(root, BuildInfo.Credit, 22, new Color(1f, 1f, 1f, 0.9f), new Vector2(520f, 34f), new Vector2(0f, 0f), new Vector2(26f, 18f), TextAnchor.MiddleLeft);
            credit.name = "Credit";
            var shadow = credit.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var note = UiKit.Label(root, "Versión alfa. Si algo se rompe, avisanos y lo arreglamos.", 22, new Color(1f, 1f, 1f, 0.85f), new Vector2(1100f, 34f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), TextAnchor.MiddleCenter);
            note.name = "AlphaNote";
            var noteShadow = note.gameObject.AddComponent<Shadow>();
            noteShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            noteShadow.effectDistance = new Vector2(2f, -2f);
        }

        private void Press(MenuEntry entry)
        {
            if (WindowOpen)
            {
                return;
            }

            switch (entry)
            {
                case MenuEntry.Story:
                    ShowSlots();
                    break;
                case MenuEntry.Online:
                    notice.SetActive(true);
                    break;
                case MenuEntry.Settings:
                    settings.Show();
                    break;
                case MenuEntry.Credits:
                    credits.SetActive(true);
                    break;
                default:
                    Quit();
                    break;
            }
        }

        /// <summary>A thin line that separates the parts of a window.</summary>
        private static void Divider(RectTransform card, float y, float width)
        {
            UiKit.Fill(card, "Divider", new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.18f), new Vector2(width, 4f), new Vector2(0.5f, 1f), new Vector2(0f, y));
        }

        private void BuildNotice()
        {
            notice = UiKit.Overlay(root, "Notice");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(notice.transform, "Card", UiKit.Paper, new Vector2(960f, 640f), center, Vector2.zero);

            // A little globe: the online mode lives on the other side of the connection.
            UiKit.Round(card, "Globe", GamePalette.Hex("#5BC0DE"), 96f, top, new Vector2(0f, -30f));
            UiKit.Round(card, "Land", GamePalette.Hex("#3E9A57"), 40f, top, new Vector2(-14f, -52f));
            UiKit.Round(card, "Land", GamePalette.Hex("#3E9A57"), 26f, top, new Vector2(18f, -78f));

            var title = UiKit.Label(card, "Jugar online", 56, UiKit.Ink, new Vector2(880f, 70f), top, new Vector2(0f, -140f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;
            UiKit.Label(card, MenuEntries.OnlineNotice, 38, GameUiColors.Muted, new Vector2(880f, 56f), top, new Vector2(0f, -214f), TextAnchor.MiddleCenter);
            Divider(card, -284f, 760f);

            string[] points =
            {
                "El game master y los vendedores usan inteligencia artificial en la nube.",
                "Si no hay conexión, el juego sigue con eventos y vendedores locales.",
                "No pedimos tu nombre ni datos personales: solo se guardan tus decisiones de juego."
            };
            for (int i = 0; i < points.Length; i++)
            {
                UiKit.Round(card, "Dot", GameUiColors.Gain, 18f, top, new Vector2(-410f, -332f - i * 74f));
                UiKit.Label(card, points[i], 30, UiKit.Ink, new Vector2(780f, 70f), top, new Vector2(30f, -312f - i * 74f), TextAnchor.MiddleLeft);
            }

            UiKit.MakeButton(card, "Entendido", GameUiColors.Green, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), CloseWindows);
            notice.SetActive(false);
        }

        private void BuildCredits()
        {
            credits = UiKit.Overlay(root, "Credits");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(credits.transform, "Card", UiKit.Paper, new Vector2(1100f, 940f), center, Vector2.zero);

            var title = UiKit.Label(card, "Créditos", 60, UiKit.Ink, new Vector2(1000f, 76f), top, new Vector2(0f, -26f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;
            UiKit.Label(card, "Hecho por " + MenuEntries.Team, 42, UiKit.Ink, new Vector2(1000f, 56f), top, new Vector2(0f, -112f), TextAnchor.MiddleCenter);
            UiKit.Label(card, string.Join(" y ", MenuEntries.Makers), 34, GameUiColors.Muted, new Vector2(1000f, 46f), top, new Vector2(0f, -166f), TextAnchor.MiddleCenter);
            UiKit.Label(card, "Keylor Pineda: backend, IA y motor financiero", 28, GameUiColors.Muted, new Vector2(1000f, 40f), top, new Vector2(0f, -214f), TextAnchor.MiddleCenter);
            UiKit.Label(card, "Jefferson Abarca: Unity, arte y flujo del juego", 28, GameUiColors.Muted, new Vector2(1000f, 40f), top, new Vector2(0f, -252f), TextAnchor.MiddleCenter);
            Divider(card, -308f, 860f);

            UiKit.Label(card, "Herramientas de Meta", 40, UiKit.Ink, new Vector2(1000f, 56f), top, new Vector2(0f, -330f), TextAnchor.MiddleCenter);
            for (int i = 0; i < MenuEntries.Tools.Length; i++)
            {
                var tool = MenuEntries.Tools[i];
                UiKit.Round(card, "ToolDot", GameUiColors.Cycle(i), 22f, top, new Vector2(-460f, -410f - i * 104f));
                UiKit.Label(card, tool[0], 34, GameUiColors.Gain, new Vector2(920f, 44f), top, new Vector2(0f, -396f - i * 104f), TextAnchor.MiddleCenter);
                UiKit.Label(card, tool[1], 28, GameUiColors.Muted, new Vector2(860f, 60f), top, new Vector2(0f, -440f - i * 104f), TextAnchor.UpperCenter);
            }

            Divider(card, -726f, 860f);
            UiKit.Label(card, "Hecho para el Meta Global AI Developer Hackathon", 28, UiKit.Ink, new Vector2(1000f, 40f), top, new Vector2(0f, -748f), TextAnchor.MiddleCenter);
            UiKit.Label(card, BuildInfo.Label, 26, GameUiColors.Muted, new Vector2(1000f, 36f), top, new Vector2(0f, -792f), TextAnchor.MiddleCenter);

            UiKit.MakeButton(card, "Volver", GameUiColors.Yellow, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), CloseWindows);
            credits.SetActive(false);
        }
    }
}

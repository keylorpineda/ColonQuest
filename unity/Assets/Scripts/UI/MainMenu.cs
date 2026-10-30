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
    /// The first screen of the game, built from code like the rest of the interface: a cartoon sky with
    /// drifting clouds and bobbing coins, the title and the buttons. "Modo historia" starts a fresh life;
    /// "Jugar online" only says it comes with the full version.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float CloudWrap = 400f;

        private static readonly Color Sky = GamePalette.Hex("#8FD3FF");

        private readonly List<RectTransform> clouds = new List<RectTransform>();
        private readonly List<float> cloudSpeeds = new List<float>();
        private readonly List<RectTransform> coins = new List<RectTransform>();
        private readonly List<float> coinRest = new List<float>();
        private readonly List<RectTransform> puffs = new List<RectTransform>();

        private Transform root;
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
            BuildNotice();
            BuildCredits();
            settings = new SettingsPanel(root, null);
        }

        private void Update()
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                var position = clouds[i].anchoredPosition;
                position.x = MenuMotion.Drift(position.x, cloudSpeeds[i], Time.unscaledDeltaTime, -ReferenceWidth * 0.5f - CloudWrap, ReferenceWidth * 0.5f + CloudWrap);
                clouds[i].anchoredPosition = position;
            }

            for (int i = 0; i < coins.Count; i++)
            {
                var position = coins[i].anchoredPosition;
                position.y = coinRest[i] + MenuMotion.Bob(Time.unscaledTime, i * 1.3f, 14f);
                coins[i].anchoredPosition = position;
            }

            for (int i = 0; i < puffs.Count; i++)
            {
                var position = puffs[i].anchoredPosition;
                position.y = MenuMotion.Drift(position.y, 40f, Time.unscaledDeltaTime, 380f, 800f);
                position.x = 620f + (position.y - 380f) * 0.25f;
                puffs[i].anchoredPosition = position;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindows();
            }
        }

        /// <summary>True while a window (notice, credits or settings) covers the buttons.</summary>
        public bool WindowOpen => notice.activeSelf || credits.activeSelf || settings.IsOpen;

        /// <summary>Starts a new life and goes to the town.</summary>
        public void StartStory()
        {
            // The first run is created fresh by the session itself; a later one has to be restarted.
            bool existed = GameSession.Instance != null;
            var session = GameSession.Create();
            if (existed)
            {
                session.StartNewGame();
            }

            SceneManager.LoadScene(SceneNames.Town);
        }

        public void CloseWindows()
        {
            notice.SetActive(false);
            credits.SetActive(false);
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            root = transform;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(events);
            }
        }

        private void BuildBackdrop()
        {
            var sky = UiKit.Overlay(root, "Sky");
            sky.GetComponent<Image>().color = Sky;
            sky.GetComponent<Image>().raycastTarget = false;

            var center = new Vector2(0.5f, 0.5f);
            var cloudColor = new Color(1f, 1f, 1f, 0.85f);
            float[] heights = { 360f, 200f, 440f, 120f };
            float[] widths = { 520f, 380f, 440f, 300f };
            for (int i = 0; i < heights.Length; i++)
            {
                var cloud = UiKit.Fill(root, "Cloud" + i, cloudColor, new Vector2(widths[i], 110f), center, new Vector2(-760f + i * 520f, heights[i]));
                clouds.Add(cloud.rectTransform);
                cloudSpeeds.Add(12f + i * 5f);
            }

            // A rocky hill at the back, the sea in front of it on the left and a sandy shore on the right.
            var bottom = new Vector2(0.5f, 0f);
            UiKit.Fill(root, "Rock", GamePalette.Hex("#3C4259"), new Vector2(900f, 760f), bottom, new Vector2(620f, -240f));
            UiKit.Fill(root, "RockLight", GamePalette.Hex("#4F5775"), new Vector2(520f, 600f), bottom, new Vector2(820f, -200f));
            UiKit.Fill(root, "Sea", GamePalette.Hex("#5BC0DE"), new Vector2(1300f, 360f), bottom, new Vector2(-420f, -150f));
            UiKit.Fill(root, "Foam", new Color(1f, 1f, 1f, 0.55f), new Vector2(1000f, 40f), bottom, new Vector2(-480f, 140f));
            UiKit.Fill(root, "Sand", GamePalette.Hex("#EBC77E"), new Vector2(1400f, 520f), bottom, new Vector2(560f, -260f));
            UiKit.Fill(root, "SandLight", GamePalette.Hex("#F4D898"), new Vector2(900f, 300f), bottom, new Vector2(700f, -150f));

            // The town on the shore: two little houses with a bank between them.
            House("HouseA", bottom, new Vector2(300f, 190f), new Vector2(120f, 110f), GamePalette.Hex("#F2B8B0"), GamePalette.Hex("#C2453D"));
            House("Bank", bottom, new Vector2(620f, 210f), new Vector2(170f, 150f), GamePalette.Hex("#BFD0F2"), GamePalette.Hex("#3E5BA8"));
            House("HouseB", bottom, new Vector2(900f, 180f), new Vector2(130f, 100f), GamePalette.Hex("#F8E4A0"), GamePalette.Hex("#C27A2C"));

            Palm(bottom, new Vector2(-30f, 250f), 1f);
            Palm(bottom, new Vector2(1110f, 230f), 1.15f);
            Palm(bottom, new Vector2(-760f, 120f), 0.9f);

            // Smoke from the chimney of the bank, rising and drifting.
            for (int i = 0; i < 5; i++)
            {
                var puff = UiKit.Fill(root, "Smoke" + i, new Color(0.45f, 0.47f, 0.52f, 0.7f), new Vector2(46f + i * 8f, 46f + i * 8f), bottom, new Vector2(620f, 380f + i * 70f));
                puffs.Add(puff.rectTransform);
            }

            float[] coinX = { 360f, 760f, 960f };
            float[] coinY = { 20f, 70f, -20f };
            for (int i = 0; i < coinX.Length; i++)
            {
                var coin = UiKit.Card(root, "Coin" + i, GameUiColors.Yellow, new Vector2(84f, 84f), center, new Vector2(coinX[i], coinY[i]));
                var outer = (RectTransform)coin.parent;
                outer.GetComponent<Image>().raycastTarget = false;
                UiKit.Label(coin, "C", 46, UiKit.Ink, new Vector2(70f, 70f), center, Vector2.zero, TextAnchor.MiddleCenter);
                coins.Add(outer);
                coinRest.Add(coinY[i]);
            }
        }

        private void House(string houseName, Vector2 anchor, Vector2 position, Vector2 size, Color wall, Color roof)
        {
            var body = UiKit.Card(root, houseName, wall, size, anchor, position);
            ((RectTransform)body.parent).GetComponent<Image>().raycastTarget = false;
            UiKit.Fill(root, houseName + "Roof", roof, new Vector2(size.x + 30f, 46f), anchor, position + new Vector2(0f, size.y - 4f));
            UiKit.Fill(root, houseName + "Door", UiKit.Ink, new Vector2(size.x * 0.22f, size.y * 0.5f), anchor, position + new Vector2(0f, 4f));
        }

        private void Palm(Vector2 anchor, Vector2 position, float scale)
        {
            var trunk = UiKit.Fill(root, "PalmTrunk", GamePalette.Hex("#8A5A2B"), new Vector2(26f, 230f) * scale, anchor, position + new Vector2(0f, 110f * scale));
            trunk.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            float[] tilt = { 28f, -28f, 0f };
            for (int i = 0; i < tilt.Length; i++)
            {
                float dx = i == 0 ? 60f : i == 1 ? -60f : 0f;
                var leaf = UiKit.Fill(root, "PalmLeaf", GamePalette.Hex(i == 2 ? "#3E9A57" : "#2F8449"), new Vector2(190f, 56f) * scale, anchor, position + new Vector2(dx, 340f * scale - Mathf.Abs(tilt[i]) * 0.9f));
                leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt[i] * 0.8f);
            }
        }

        private void BuildTitle()
        {
            var topLeft = new Vector2(0f, 1f);
            var cream = GamePalette.Hex("#F6F0D4");
            var title = UiKit.Label(root, "Colón Quest", 150, cream, new Vector2(1000f, 190f), topLeft, new Vector2(80f, -50f), TextAnchor.MiddleLeft);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Outlined(title, 8f);
            var subtitle = UiKit.Label(root, "Aprendé a manejar tu plata", 44, cream, new Vector2(900f, 56f), topLeft, new Vector2(92f, -235f), TextAnchor.MiddleLeft);
            Outlined(subtitle, 5f);

            var version = UiKit.Label(root, "alfa", 26, UiKit.Ink, new Vector2(200f, 34f), topLeft, new Vector2(22f, -14f), TextAnchor.MiddleLeft);
            version.name = "Version";
        }

        private static void Outlined(Text label, float width)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(width, -width);
            outline.useGraphicAlpha = false;
        }

        private void BuildButtons()
        {
            var topLeft = new Vector2(0f, 1f);
            var wood = GamePalette.Hex("#C58B52");
            var entries = MenuEntries.For(Application.platform);

            // The post the signs hang from.
            UiKit.Fill(root, "Post", GamePalette.Hex("#7A4E26"), new Vector2(40f, 760f), topLeft, new Vector2(300f, -330f));

            float[] widths = { 580f, 520f, 400f, 360f, 300f };
            float[] tilts = { 2.5f, -3f, 2f, -2.5f, 3f };
            float[] shifts = { 90f, 20f, 120f, 150f, 170f };
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                float height = i < 2 ? 96f : 78f;
                var color = entry == MenuEntry.Story ? GamePalette.Hex("#E0A560") : wood;
                var button = UiKit.MakeButton(root, MenuEntries.LabelOf(entry), color, new Vector2(widths[i], height), topLeft, new Vector2(shifts[i], -360f - i * 112f), () => Press(entry));
                button.transform.localRotation = Quaternion.Euler(0f, 0f, tilts[i]);
            }

            var note = UiKit.Card(root, "Note", GameUiColors.Yellow, new Vector2(520f, 150f), new Vector2(1f, 0f), new Vector2(-40f, 150f));
            UiKit.Label(note, "Versión alfa. Si algo se rompe, avisanos y lo arreglamos.", 28, UiKit.Ink, new Vector2(470f, 120f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            UiKit.Label(root, MenuEntries.Team, 30, UiKit.Ink, new Vector2(520f, 40f), new Vector2(1f, 0f), new Vector2(-40f, 90f), TextAnchor.MiddleRight);
        }

        private static Color ColorOf(MenuEntry entry)
        {
            switch (entry)
            {
                case MenuEntry.Story: return GameUiColors.Green;
                case MenuEntry.Online: return GameUiColors.Blue;
                case MenuEntry.Settings: return GameUiColors.Yellow;
                case MenuEntry.Credits: return GameUiColors.Pink;
                default: return UiKit.Paper;
            }
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
                    StartStory();
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

        private void BuildNotice()
        {
            notice = UiKit.Overlay(root, "Notice");
            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Card(notice.transform, "Card", UiKit.Paper, new Vector2(800f, 400f), center, Vector2.zero);
            UiKit.Label(card, "Jugar online", 54, UiKit.Ink, new Vector2(720f, 70f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.MiddleCenter);
            UiKit.Label(card, MenuEntries.OnlineNotice, 40, GameUiColors.Muted, new Vector2(720f, 100f), center, new Vector2(0f, 10f), TextAnchor.MiddleCenter);
            UiKit.MakeButton(card, "Entendido", GameUiColors.Green, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), CloseWindows);
            notice.SetActive(false);
        }

        private void BuildCredits()
        {
            credits = UiKit.Overlay(root, "Credits");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(credits.transform, "Card", UiKit.Paper, new Vector2(1000f, 800f), center, Vector2.zero);
            UiKit.Label(card, "Créditos", 56, UiKit.Ink, new Vector2(920f, 70f), top, new Vector2(0f, -28f), TextAnchor.MiddleCenter);
            UiKit.Label(card, "Hecho por " + MenuEntries.Team, 40, UiKit.Ink, new Vector2(920f, 56f), top, new Vector2(0f, -120f), TextAnchor.MiddleCenter);
            UiKit.Label(card, string.Join(" y ", MenuEntries.Makers), 36, GameUiColors.Muted, new Vector2(920f, 50f), top, new Vector2(0f, -180f), TextAnchor.MiddleCenter);
            UiKit.Label(card, "Herramientas de Meta", 40, UiKit.Ink, new Vector2(920f, 56f), top, new Vector2(0f, -270f), TextAnchor.MiddleCenter);

            for (int i = 0; i < MenuEntries.Tools.Length; i++)
            {
                var tool = MenuEntries.Tools[i];
                UiKit.Label(card, tool[0], 34, GameUiColors.Gain, new Vector2(920f, 44f), top, new Vector2(0f, -340f - i * 112f), TextAnchor.MiddleCenter);
                UiKit.Label(card, tool[1], 28, GameUiColors.Muted, new Vector2(860f, 60f), top, new Vector2(0f, -382f - i * 112f), TextAnchor.UpperCenter);
            }

            UiKit.MakeButton(card, "Volver", GameUiColors.Yellow, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), CloseWindows);
            credits.SetActive(false);
        }
    }
}

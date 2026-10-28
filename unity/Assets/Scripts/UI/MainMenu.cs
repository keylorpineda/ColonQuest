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
            var cloudColor = new Color(1f, 1f, 1f, 0.9f);
            float[] heights = { 330f, 190f, 410f, 90f };
            float[] widths = { 340f, 260f, 300f, 220f };
            for (int i = 0; i < heights.Length; i++)
            {
                var cloud = UiKit.Fill(root, "Cloud" + i, cloudColor, new Vector2(widths[i], 90f), center, new Vector2(-760f + i * 520f, heights[i]));
                clouds.Add(cloud.rectTransform);
                cloudSpeeds.Add(14f + i * 6f);
            }

            // Two rolling hills along the bottom.
            UiKit.Fill(root, "HillBack", GamePalette.Hex("#58A867"), new Vector2(1500f, 520f), new Vector2(0.5f, 0f), new Vector2(-380f, -330f));
            UiKit.Fill(root, "HillFront", GamePalette.Hex("#6BC27A"), new Vector2(1700f, 460f), new Vector2(0.5f, 0f), new Vector2(420f, -330f));

            float[] coinX = { -780f, -640f, 650f, 790f, 720f };
            float[] coinY = { -120f, 120f, 30f, -170f, 210f };
            for (int i = 0; i < coinX.Length; i++)
            {
                var coin = UiKit.Card(root, "Coin" + i, GameUiColors.Yellow, new Vector2(96f, 96f), center, new Vector2(coinX[i], coinY[i]));
                var outer = (RectTransform)coin.parent;
                outer.GetComponent<Image>().raycastTarget = false;
                UiKit.Label(coin, "C", 52, UiKit.Ink, new Vector2(80f, 80f), center, Vector2.zero, TextAnchor.MiddleCenter);
                coins.Add(outer);
                coinRest.Add(coinY[i]);
            }
        }

        private void BuildTitle()
        {
            var top = new Vector2(0.5f, 1f);
            var plate = UiKit.Card(root, "TitleCard", UiKit.Paper, new Vector2(860f, 230f), top, new Vector2(0f, -50f));
            UiKit.Label(plate, "Colón Quest", 120, UiKit.Ink, new Vector2(820f, 140f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), TextAnchor.MiddleCenter);
            UiKit.Label(plate, "Aprendé a manejar tu plata", 40, GameUiColors.Muted, new Vector2(820f, 50f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), TextAnchor.MiddleCenter);
        }

        private void BuildButtons()
        {
            var top = new Vector2(0.5f, 1f);
            var entries = MenuEntries.For(Application.platform);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                UiKit.MakeButton(root, MenuEntries.LabelOf(entry), ColorOf(entry), new Vector2(520f, 90f), top, new Vector2(0f, -320f - i * 108f), () => Press(entry));
            }

            UiKit.Label(root, "Versión alfa  |  " + MenuEntries.Team, 28, UiKit.Ink, new Vector2(900f, 40f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), TextAnchor.MiddleCenter);
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

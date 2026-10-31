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
    /// The first screen of the game: an inked Costa Rican valley with a toucan that carries a banknote in its beak (made with Muse
    /// Image, see server/scripts/generate_menu_art.py), with a camera that breathes and follows the mouse a little. Over its left
    /// side, the name of the game and one column of equal buttons; "Modo historia" is the highlighted one.
    /// </summary>
    public sealed partial class MainMenu : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;

        /// <summary>The layout, in units of a screen 1080 high.</summary>
        private const float ColumnX = 84f;
        private const float ButtonWidth = 480f;
        private const float ButtonHeight = 84f;
        private const float ButtonGap = 18f;
        private const float ButtonsTop = -286f;

        private const float ArtWidth = 1920f;
        private const float ArtHeight = 1080f;
        private const float ArtMargin = 1.15f;
        private const float ParallaxReach = 14f;

        private static readonly Color Cream = GamePalette.Hex("#F8F1DC");
        private static readonly Color Beak = GamePalette.Hex("#F2A23A");
        private static readonly Color Ink = GamePalette.Hex("#17201E");

        private RectTransform art;
        private GameObject firstButton;

        // What slides in when the menu opens: the title, and each button a moment after the other.
        private readonly List<RectTransform> entering = new List<RectTransform>();
        private readonly List<Vector2> enterTarget = new List<Vector2>();
        private readonly List<float> enterFrom = new List<float>();

        private Transform root;
        private Vector2 parallax;
        private float openedAt;
        private GameObject notice;
        private GameObject credits;
        private GameObject quitConfirm;
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
            BuildQuitConfirm();
            BuildSlots();
            BuildNewGame();
            settings = new SettingsPanel(root, null);
            openedAt = Time.unscaledTime;
            BuildSplash();
        }

        private void OnEnable()
        {
            Loc.Changed += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (slots != null && slots.activeSelf)
            {
                RefreshSlots();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            MoveLayers(dt);
            if (UpdateSplash(now))
            {
                return;
            }

            SlideIn(now - openedAt);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindows();
            }

            // The arrows wake up the keyboard: from then on they move between the buttons and Enter presses.
            bool arrow = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow);
            if (arrow && !WindowOpen && firstButton != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton);
            }
        }

        /// <summary>
        /// The picture always covers the window, whatever its shape, with a small margin for the movement: it breathes very
        /// slowly and drifts against the mouse. A window narrower than the picture loses its left side, where the buttons are,
        /// so the toucan stays whole. Clamped so borders never detach to expose the background.
        /// </summary>
        private void MoveLayers(float dt)
        {
            var mouse = new Vector2(Input.mousePosition.x / Mathf.Max(1f, Screen.width), Input.mousePosition.y / Mathf.Max(1f, Screen.height)) - new Vector2(0.5f, 0.5f);
            mouse = Vector2.ClampMagnitude(mouse * 2f, 1f);
            parallax = Vector2.Lerp(parallax, mouse, Mathf.Clamp01(dt * 2f));

            var window = ((RectTransform)root).rect;
            float cover = Mathf.Max(window.width / ArtWidth, window.height / ArtHeight);
            float scale = cover * (ArtMargin + Mathf.Sin(Time.unscaledTime * 0.12f) * 0.006f);
            art.localScale = new Vector3(scale, scale, 1f);

            float halfW = ArtWidth * scale * 0.5f;
            float halfH = ArtHeight * scale * 0.5f;
            float minX = window.width * 0.5f - halfW;
            float maxX = halfW - window.width * 0.5f;
            float minY = window.height * 0.5f - halfH;
            float maxY = halfH - window.height * 0.5f;

            float keepRight = (window.width - ArtWidth * cover) * 0.5f;
            float desiredX = keepRight - parallax.x * ParallaxReach;
            float desiredY = -parallax.y * ParallaxReach;

            float clampedX = minX <= maxX ? Mathf.Clamp(desiredX, minX, maxX) : 0f;
            float clampedY = minY <= maxY ? Mathf.Clamp(desiredY, minY, maxY) : 0f;
            art.anchoredPosition = new Vector2(clampedX, clampedY);
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
        public bool WindowOpen => notice.activeSelf || credits.activeSelf || quitConfirm.activeSelf || settings.IsOpen || slots.activeSelf || newGame.activeSelf;

        /// <summary>Starts a new life saved in a slot and goes to the opening in the house, behind the loading screen.</summary>
        public void StartStory(int slot, string runName, string characterName, bool female)
        {
            // The first run is created fresh by the session itself; a later one has to be restarted.
            bool existed = GameSession.Instance != null;
            var session = GameSession.Create();
            if (existed)
            {
                session.StartNewGame();
            }

            session.AssignSlot(slot, runName, characterName, female);
            GameUi.Create(session).PlayOpeningNext();
            CloseWindows();
            LoadingScreen.Load(SceneNames.Casa);
        }

        public void CloseWindows()
        {
            notice.SetActive(false);
            credits.SetActive(false);
            quitConfirm.SetActive(false);
            slots.SetActive(false);
            newGame.SetActive(false);
            if (finishedReport != null)
            {
                finishedReport.SetActive(false);
            }

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
            // The buttons stack down the left side, so the height decides the scale: every button fits in a wide or short
            // window, and a wider screen only shows more of the valley.
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

        private void BuildBackdrop()
        {
            art = UiKit.Rect(root, "KeyArt", new Vector2(ArtWidth, ArtHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var image = art.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Menu/KeyArt");
            image.raycastTarget = false;
        }

        private void BuildTitle()
        {
            var topLeft = new Vector2(0f, 1f);
            var title = UiKit.Label(root, "Colón Quest", 104, Cream, new Vector2(800f, 130f), topLeft, new Vector2(ColumnX, -70f));
            title.font = UiKit.TitleFont;
            title.name = "Title";
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(5f, -5f);
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(7f, -9f);
            Slide(title.rectTransform, 700f);

            var subtitle = UiKit.Label(root, "Aprendé a manejar tu plata", 36, Cream, new Vector2(700f, 50f), topLeft, new Vector2(ColumnX + 4f, -204f));
            subtitle.name = "Subtitle";
            var subtitleOutline = subtitle.gameObject.AddComponent<Outline>();
            subtitleOutline.effectColor = Ink;
            subtitleOutline.effectDistance = new Vector2(3f, -3f);
            Slide(subtitle.rectTransform, 700f);
        }

        /// <summary>One column: the same width, the same height and the same gap. Only the first button has its own color.</summary>
        private void BuildButtons()
        {
            var topLeft = new Vector2(0f, 1f);
            var entries = MenuEntries.For(Application.platform);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                bool main = entry == MenuEntry.Story;
                var button = UiKit.MakeButton(root, MenuEntries.LabelOf(entry), main ? Beak : Cream, new Vector2(ButtonWidth, ButtonHeight), topLeft, new Vector2(ColumnX, ButtonsTop - i * (ButtonHeight + ButtonGap)), () => Press(entry));
                UiKit.SetFontSize(button, 34);
                Slide((RectTransform)button.transform, 760f);
                if (i == 0)
                {
                    firstButton = button.gameObject;
                }
            }
        }

        private static Text FooterLabel(Transform parent, string text, int size, Vector2 boxSize, Vector2 anchor, Vector2 position, TextAnchor alignment)
        {
            var label = UiKit.Label(parent, text, size, Cream, boxSize, anchor, position, alignment);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        /// <summary>The note of the alpha, the version and the credit under the buttons, away from the toucan.</summary>
        private void BuildFooter()
        {
            var bottomLeft = new Vector2(0f, 0f);
            FooterLabel(root, BuildInfo.Label, 24, new Vector2(ButtonWidth, 32f), bottomLeft, new Vector2(ColumnX, 56f), TextAnchor.MiddleLeft).name = "Version";
            FooterLabel(root, BuildInfo.Credit, 22, new Vector2(ButtonWidth, 32f), bottomLeft, new Vector2(ColumnX, 24f), TextAnchor.MiddleLeft).name = "Credit";
            FooterLabel(root, "Versión alfa. Si algo se rompe, avisanos y lo arreglamos.", 22, new Vector2(720f, 32f), bottomLeft, new Vector2(ColumnX, 88f), TextAnchor.MiddleLeft).name = "AlphaNote";
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
                    quitConfirm.SetActive(true);
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
            var card = UiKit.Card(notice.transform, "Card", UiKit.Paper, new Vector2(960f, 600f), center, Vector2.zero);
            UiKit.Header(card, "Jugar online", GamePalette.Hex("#2E7D9A"));

            UiKit.Label(card, MenuEntries.OnlineNotice, 38, UiKit.Ink, new Vector2(880f, 56f), top, new Vector2(0f, -134f), TextAnchor.MiddleCenter);
            Divider(card, -206f, 760f);

            string[] points =
            {
                "El game master y los vendedores usan inteligencia artificial en la nube.",
                "Si no hay conexión, el juego sigue con eventos y vendedores locales.",
                "No pedimos tu nombre ni datos personales: solo se guardan tus decisiones de juego."
            };
            for (int i = 0; i < points.Length; i++)
            {
                UiKit.Round(card, "Dot", GameUiColors.Gain, 18f, top, new Vector2(-410f, -256f - i * 76f));
                UiKit.Label(card, points[i], 30, UiKit.Ink, new Vector2(780f, 70f), top, new Vector2(30f, -236f - i * 76f), TextAnchor.MiddleLeft);
            }

            UiKit.MakeButton(card, "Entendido", GameUiColors.Green, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), CloseWindows);
            notice.SetActive(false);
        }

        private void BuildCredits()
        {
            credits = UiKit.Overlay(root, "Credits");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(credits.transform, "Card", UiKit.Paper, new Vector2(1100f, 960f), center, Vector2.zero);
            UiKit.Header(card, "Créditos", GamePalette.Hex("#C0612B"));

            UiKit.Label(card, "Desarrollado por", 30, GameUiColors.Muted, new Vector2(1000f, 40f), top, new Vector2(0f, -124f), TextAnchor.MiddleCenter);
            var team = UiKit.PlainLabel(card, "Team", MenuEntries.Team, 64, UiKit.Ink, new Vector2(1000f, 76f), top, new Vector2(0f, -160f), TextAnchor.MiddleCenter);
            team.font = UiKit.TitleFont;
            UiKit.Label(card, "Keylor Pineda: backend, IA y motor financiero", 28, UiKit.Ink, new Vector2(1000f, 40f), top, new Vector2(0f, -244f), TextAnchor.MiddleCenter);
            UiKit.Label(card, "Jefferson Abarca: Unity, arte y flujo del juego", 28, UiKit.Ink, new Vector2(1000f, 40f), top, new Vector2(0f, -284f), TextAnchor.MiddleCenter);
            Divider(card, -340f, 860f);

            UiKit.Label(card, "Herramientas de Meta", 38, UiKit.Ink, new Vector2(1000f, 52f), top, new Vector2(0f, -358f), TextAnchor.MiddleCenter);
            for (int i = 0; i < MenuEntries.Tools.Length; i++)
            {
                var tool = MenuEntries.Tools[i];
                UiKit.Round(card, "ToolDot", GameUiColors.Cycle(i), 22f, top, new Vector2(-460f, -434f - i * 96f));
                UiKit.Label(card, tool[0], 32, GameUiColors.Gain, new Vector2(920f, 42f), top, new Vector2(0f, -420f - i * 96f), TextAnchor.MiddleCenter);
                UiKit.Label(card, tool[1], 26, GameUiColors.Muted, new Vector2(860f, 54f), top, new Vector2(0f, -462f - i * 96f), TextAnchor.UpperCenter);
            }

            Divider(card, -804f, 860f);
            UiKit.Label(card, "Hecho para el Meta Global AI Developer Hackathon", 26, UiKit.Ink, new Vector2(1000f, 36f), top, new Vector2(0f, -818f), TextAnchor.MiddleCenter);
            UiKit.Label(card, BuildInfo.Label, 24, GameUiColors.Muted, new Vector2(1000f, 32f), top, new Vector2(0f, -852f), TextAnchor.MiddleCenter);

            UiKit.MakeButton(card, "Volver", GameUiColors.Yellow, new Vector2(320f, 72f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), CloseWindows);
            credits.SetActive(false);
        }

        /// <summary>Quitting asks first: a click by mistake on "Salir" should not close the game.</summary>
        private void BuildQuitConfirm()
        {
            quitConfirm = UiKit.Overlay(root, "QuitConfirm");
            var card = UiKit.Card(quitConfirm.transform, "Card", UiKit.Paper, new Vector2(820f, 400f), new Vector2(0.5f, 0.5f), Vector2.zero);
            UiKit.Header(card, "Salir del juego", GameUiColors.Loss);
            UiKit.Label(card, "¿Seguro que querés salir de Colón Quest?", 36, UiKit.Ink, new Vector2(740f, 110f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), TextAnchor.MiddleCenter);

            var bottom = new Vector2(0.5f, 0f);
            UiKit.MakeButton(card, "Cancelar", GameUiColors.Blue, new Vector2(300f, 84f), bottom, new Vector2(-170f, 36f), CloseWindows);
            UiKit.MakeButton(card, "Salir", GameUiColors.Pink, new Vector2(300f, 84f), bottom, new Vector2(170f, 36f), Quit);
            quitConfirm.SetActive(false);
        }

        // ---------------------------------------------------------------- splash

        /// <summary>The presentation shows once per launch, not every time the player comes back to the menu.</summary>
        private static bool splashShown;

        private const float SplashFadeIn = 0.7f;
        private const float SplashHold = 4.0f;
        private const float SplashFadeOut = 0.7f;

        private CanvasGroup splash;
        private CanvasGroup splashContent;
        private float splashStartedAt;
        private bool splashLeaving;
        private float splashLeftAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSplash()
        {
            splashShown = false;
        }

        /// <summary>
        /// After the Unity logo, a screen of the team: who made the game and who designed it, then the menu. Any key or click skips it.
        /// </summary>
        private void BuildSplash()
        {
            if (splashShown)
            {
                return;
            }

            splashShown = true;
            var layer = UiKit.Overlay(root, "Splash");
            layer.GetComponent<Image>().color = GamePalette.Hex("#0E1413");
            layer.transform.SetAsLastSibling();
            splash = layer.AddComponent<CanvasGroup>();
            splash.alpha = 1f;

            var center = new Vector2(0.5f, 0.5f);
            var content = UiKit.Rect(layer.transform, "Content", new Vector2(1400f, 560f), center, Vector2.zero);
            splashContent = content.gameObject.AddComponent<CanvasGroup>();
            splashContent.alpha = 0f;

            var muted = GamePalette.Hex("#C9C3AE");
            UiKit.Label(content, "Desarrollado por", 36, muted, new Vector2(1200f, 50f), center, new Vector2(0f, 170f), TextAnchor.MiddleCenter);
            var team = UiKit.PlainLabel(content, "Team", MenuEntries.Team, 132, Cream, new Vector2(1400f, 170f), center, new Vector2(0f, 60f), TextAnchor.MiddleCenter);
            team.font = UiKit.TitleFont;
            team.horizontalOverflow = HorizontalWrapMode.Overflow;
            var teamShadow = team.gameObject.AddComponent<Shadow>();
            teamShadow.effectColor = Beak;
            teamShadow.effectDistance = new Vector2(6f, -6f);

            UiKit.Fill(content, "Rule", Beak, new Vector2(420f, 6f), center, new Vector2(0f, -40f));
            UiKit.Label(content, "Diseño y desarrollo: Jefferson Abarca y Keylor Pineda", 34, Cream, new Vector2(1300f, 50f), center, new Vector2(0f, -96f), TextAnchor.MiddleCenter);
            UiKit.Label(content, "Hecho para el Meta Global AI Developer Hackathon", 28, muted, new Vector2(1300f, 44f), center, new Vector2(0f, -150f), TextAnchor.MiddleCenter);

            UiKit.Label(layer.transform, "Clic o cualquier tecla para saltar", 22, GamePalette.Hex("#7C8580"), new Vector2(800f, 36f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), TextAnchor.MiddleCenter);
            splashStartedAt = Time.unscaledTime;
        }

        /// <summary>Plays the presentation. True while it covers the menu, so the menu waits under it.</summary>
        private bool UpdateSplash(float now)
        {
            if (splash == null)
            {
                return false;
            }

            float shown = now - splashStartedAt;
            if (!splashLeaving)
            {
                splashContent.alpha = Mathf.SmoothStep(0f, 1f, shown / SplashFadeIn);
                bool skip = shown > 0.3f && (Input.anyKeyDown || Input.GetMouseButtonDown(0));
                if (skip || shown >= SplashFadeIn + SplashHold)
                {
                    splashLeaving = true;
                    splashLeftAt = now;
                }

                return true;
            }

            float leaving = (now - splashLeftAt) / SplashFadeOut;
            splash.alpha = 1f - Mathf.SmoothStep(0f, 1f, leaving);
            if (leaving >= 1f)
            {
                Destroy(splash.gameObject);
                splash = null;
                // The title and the buttons slide in once the menu is in sight.
                openedAt = now;
                return false;
            }

            return true;
        }
    }
}

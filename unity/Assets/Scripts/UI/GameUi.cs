using System.Collections.Generic;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The overlay of the game: the status card, the "press E" prompt, the loading card and the
    /// modal windows for events, results, the statement and the end of the run. It is the only
    /// place that turns what happens in <see cref="GameSession"/> into screens.
    /// </summary>
    public sealed partial class GameUi : MonoBehaviour
    {
        private GameSession session;
        private Canvas canvas;

        /// <summary>How many times each everyday object was used, so its lines rotate.</summary>
        private readonly Dictionary<string, int> thingUses = new Dictionary<string, int>();
        private ModalPanel modal;
        private SubtitleBar subtitles;
        private ScreenEffects effects;
        private NegotiationPanel negotiationPanel;
        private TalkPanel talkPanel;
        private PhoneUi phone;
        private PauseMenu pause;
        private Negotiation spokenNegotiation;
        private int spokenTurns;

        private Text monthText;
        private Text clockText;
        private Text tasksText;
        private Text cashText;
        private Text savingsText;
        private Text cardText;
        private Text installmentsText;
        private Image wellbeingFill;
        private GameObject promptCard;
        private Text promptText;
        private GameObject loadingCard;
        private Text loadingText;

        private const float BarWidth = 190f;

        private readonly List<Station> inside = new List<Station>();

        /// <summary>Places used since the player walked into them; they come back once the player leaves.</summary>
        private readonly HashSet<Station> usedStations = new HashSet<Station>();

        /// <summary>The place whose person is speaking in the subtitle, or null for a thought or nobody.</summary>
        private Station speakingStation;

        /// <summary>Past this distance from the person who talks, the line goes away even if the trigger missed it.</summary>
        private const float SpeechRadius = 3.5f;

        private static GameUi current;

        public static GameUi Create(GameSession gameSession)
        {
            if (current != null)
            {
                return current;
            }

            var go = new GameObject("GameUi");
            DontDestroyOnLoad(go);
            var ui = go.AddComponent<GameUi>();
            ui.Bind(gameSession);
            current = ui;
            return ui;
        }

        private void Bind(GameSession gameSession)
        {
            session = gameSession;
            BuildCanvas();
            BuildPowerCut();
            BuildHud();
            BuildWorkGuide();
            BuildTasks();
            BuildPrompt();
            BuildLoading();
            modal = new ModalPanel(canvas.transform);
            subtitles = new SubtitleBar(canvas.transform);
            effects = new ScreenEffects(canvas.transform);
            Thoughts.Said += OnThought;
            Interaction.Spoke += OnSpoke;
            negotiationPanel = new NegotiationPanel(canvas.transform, session);
            negotiationPanel.SignContract = PlayContract;
            talkPanel = new TalkPanel(canvas.transform, session, TalkActions);
            phone = new PhoneUi(canvas.transform, session);
            pause = new PauseMenu(canvas.transform, ResumeFromPause, GoToMenu, () => session.SaveNow(), () => session.LeaveWithoutSaving());
            BuildIncidents();
            BuildCell();

            // A new game can create the overlay from the menu, which has its own screen.
            canvas.enabled = SceneManager.GetActiveScene().name != SceneNames.Menu;

            session.Changed += Refresh;
            session.NegotiationStarted += OnNegotiationChanged;
            session.NegotiationUpdated += OnNegotiationChanged;
            session.TalkUpdated += OnTalkChanged;
            session.NewGameStarted += CloseWindows;
            Loc.Changed += Refresh;
            LocalSeller.Translate = Loc.T;
            ApiService.LocalTextsOnly = () => false;
            ApiService.Language = () => Loc.Current == GameLanguage.English ? "en" : "es";
            session.EventReady += ShowEvent;
            session.ChoiceApplied += ShowOutcome;
            session.MonthClosed += ShowMonthReport;
            session.QuincenaPaid += ShowQuincenaNotice;
            session.Finished += ShowEnd;
            SceneManager.sceneLoaded += OnSceneLoaded;

            BindStations();
            Refresh();
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }

            ReleaseIncidents();
            Thoughts.Said -= OnThought;
            Interaction.Spoke -= OnSpoke;
            Time.timeScale = 1f;
            PlayerController.Blocked = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Loc.Changed -= Refresh;
            if (session != null)
            {
                session.Changed -= Refresh;
                session.NegotiationStarted -= OnNegotiationChanged;
                session.NegotiationUpdated -= OnNegotiationChanged;
                session.TalkUpdated -= OnTalkChanged;
                session.NewGameStarted -= CloseWindows;
                session.EventReady -= ShowEvent;
                session.ChoiceApplied -= ShowOutcome;
                session.MonthClosed -= ShowMonthReport;
                session.QuincenaPaid -= ShowQuincenaNotice;
                session.Finished -= ShowEnd;
            }
        }

        // ---------------------------------------------------------------- build

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(events);
            }
        }

        private void BuildHud()
        {
            var topLeft = new Vector2(0f, 1f);
            var hudPaper = new Color(UiKit.Paper.r, UiKit.Paper.g, UiKit.Paper.b, 0.86f);
            var card = UiKit.Card(canvas.transform, "Hud", hudPaper, new Vector2(470f, HudHeight), topLeft, new Vector2(24f, -24f));

            // A thin ribbon in the colors of the Costa Rican flag over the header: blue, white, red, white, blue.
            var flag = new[] { FlagBlue, Color.white, FlagRed, Color.white, FlagBlue };
            for (int i = 0; i < flag.Length; i++)
            {
                var stripe = UiKit.Fill(card, "FlagStripe" + i, flag[i], new Vector2(434f, i == 2 ? 4f : 2f), topLeft, new Vector2(12f, -8f - (i <= 2 ? i * 2f : i * 2f + 2f)));
                stripe.type = Image.Type.Simple;
            }

            // Warm header band under the month and clock.
            UiKit.Fill(card, "HeaderBand", new Color(0.93f, 0.89f, 0.78f, 0.7f), new Vector2(434f, 46f), topLeft, new Vector2(12f, -22f));
            UiKit.Fill(card, "HeaderDivider", new Color(0.18f, 0.20f, 0.30f, 0.16f), new Vector2(426f, 2f), topLeft, new Vector2(16f, -70f));

            // Top line: the month and its week on the left, the hour on the right. Their boxes never touch, and a long
            // month name shrinks to fit (TextFit) instead of running under the clock.
            monthText = UiKit.Label(card, string.Empty, 26, UiKit.Ink, new Vector2(316f, 44f), topLeft, new Vector2(22f, -23f));
            monthText.font = UiKit.TitleFont;
            monthText.horizontalOverflow = HorizontalWrapMode.Overflow;
            clockText = UiKit.Label(card, string.Empty, 30, UiKit.Ink, new Vector2(100f, 44f), new Vector2(1f, 1f), new Vector2(-22f, -23f), TextAnchor.MiddleRight);
            clockText.font = UiKit.TitleFont;
            clockText.horizontalOverflow = HorizontalWrapMode.Overflow;

            cashText = Row(card, "Efectivo", 1, HudIcon.Cash);
            savingsText = Row(card, "Ahorros", 2, HudIcon.Savings);
            cardText = Row(card, "Tarjeta", 3, HudIcon.Card);
            installmentsText = Row(card, "Cuotas al mes", 4, HudIcon.Installments);

            DrawHudIcon(card, HudIcon.Mood, -RowTop(5));
            var mood = UiKit.Label(card, "Ánimo", 26, GameUiColors.Muted, new Vector2(180f, 36f), topLeft, new Vector2(RowTextX, -RowTop(5)));
            mood.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Fill(card, "WellbeingOutline", UiKit.Ink, new Vector2(BarWidth + 6f, 32f), topLeft, new Vector2(247f, -RowTop(5) - 2f));
            UiKit.Fill(card, "WellbeingBack", new Color(0.82f, 0.84f, 0.9f, 1f), new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -RowTop(5) - 5f));
            wellbeingFill = UiKit.Fill(card, "WellbeingFill", GameUiColors.Gain, new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -RowTop(5) - 5f));

            // Bottom line: the phone key on the left, the tasks of the month on the right, in boxes that do not overlap.
            var phoneKey = UiKit.Label(card, "[P]  Celular", 24, GameUiColors.Muted, new Vector2(226f, 36f), topLeft, new Vector2(22f, -HudHeight + 50f));
            phoneKey.horizontalOverflow = HorizontalWrapMode.Overflow;
            tasksText = UiKit.Label(card, string.Empty, 24, GameUiColors.Muted, new Vector2(186f, 36f), new Vector2(1f, 1f), new Vector2(-22f, -HudHeight + 50f), TextAnchor.MiddleRight);
            tasksText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private const float HudHeight = 344f;
        private const float RowTextX = 62f;
        private static readonly Color FlagBlue = GamePalette.Hex("#002B7F");
        private static readonly Color FlagRed = GamePalette.Hex("#CE1126");

        private enum HudIcon
        {
            Cash,
            Savings,
            Card,
            Installments,
            Mood
        }

        /// <summary>The top of the row <paramref name="index"/> (1 is the first money row), in pixels below the top of the card.</summary>
        private static float RowTop(int index)
        {
            return 36f + index * 40f;
        }

        private Text Row(RectTransform card, string label, int index, HudIcon icon)
        {
            float y = -RowTop(index);
            DrawHudIcon(card, icon, y);

            // Label on the left (62 to 248) and value on the right (252 to 448): the two boxes never overlap, and a
            // longer translation or a bigger amount shrinks inside its own box.
            var name = UiKit.Label(card, label, 26, GameUiColors.Muted, new Vector2(186f, 36f), new Vector2(0f, 1f), new Vector2(RowTextX, y));
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            var value = UiKit.Label(card, string.Empty, 30, UiKit.Ink, new Vector2(196f, 36f), new Vector2(1f, 1f), new Vector2(-22f, y), TextAnchor.MiddleRight);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            return value;
        }

        /// <summary>
        /// A small round badge in front of a row, drawn with shapes (no font glyphs needed): a coin for cash, a piggy bank
        /// for savings, a card, a calendar page for the installments and a heart for the mood.
        /// </summary>
        private static void DrawHudIcon(RectTransform card, HudIcon icon, float rowTop)
        {
            var topLeft = new Vector2(0f, 1f);
            var at = new Vector2(22f, rowTop - 2f);
            const float size = 32f;
            UiKit.Round(card, "Icon_" + icon + "_Outline", UiKit.Ink, size, topLeft, at);
            var center = at + new Vector2(size * 0.5f, -size * 0.5f);
            switch (icon)
            {
                case HudIcon.Cash:
                    UiKit.Round(card, "Icon_Cash", GamePalette.Hex("#FFD23F"), size - 6f, topLeft, at + new Vector2(3f, -3f));
                    UiKit.Round(card, "Icon_CashRim", GamePalette.Hex("#E0A800"), size - 16f, topLeft, at + new Vector2(8f, -8f));
                    UiKit.Round(card, "Icon_CashCore", GamePalette.Hex("#FFE27A"), size - 20f, topLeft, at + new Vector2(10f, -10f));
                    break;
                case HudIcon.Savings:
                    UiKit.Round(card, "Icon_Savings", GamePalette.Hex("#FF9DB5"), size - 6f, topLeft, at + new Vector2(3f, -3f));
                    IconFill(UiKit.Fill(card, "Icon_SavingsSlot", UiKit.Ink, new Vector2(10f, 3f), topLeft, center + new Vector2(-5f, 8f)));
                    UiKit.Round(card, "Icon_SavingsSnout", GamePalette.Hex("#E86F8E"), 9f, topLeft, center + new Vector2(4f, 0f));
                    break;
                case HudIcon.Card:
                    UiKit.Round(card, "Icon_CardBack", GamePalette.Hex("#8FD3FF"), size - 6f, topLeft, at + new Vector2(3f, -3f));
                    IconFill(UiKit.Fill(card, "Icon_Card", GamePalette.Hex("#1D4F8C"), new Vector2(18f, 13f), topLeft, center + new Vector2(-9f, 6.5f)));
                    IconFill(UiKit.Fill(card, "Icon_CardStripe", GamePalette.Hex("#FFD23F"), new Vector2(18f, 3f), topLeft, center + new Vector2(-9f, 2f)));
                    break;
                case HudIcon.Installments:
                    UiKit.Round(card, "Icon_CalendarBack", GamePalette.Hex("#C9E8B8"), size - 6f, topLeft, at + new Vector2(3f, -3f));
                    IconFill(UiKit.Fill(card, "Icon_Calendar", Color.white, new Vector2(16f, 15f), topLeft, center + new Vector2(-8f, 6f)));
                    IconFill(UiKit.Fill(card, "Icon_CalendarTop", FlagRed, new Vector2(16f, 5f), topLeft, center + new Vector2(-8f, 7f)));
                    break;
                default:
                    UiKit.Round(card, "Icon_MoodBack", GamePalette.Hex("#FFE0E0"), size - 6f, topLeft, at + new Vector2(3f, -3f));
                    UiKit.Round(card, "Icon_HeartL", GameUiColors.Loss, 10f, topLeft, center + new Vector2(-8.5f, 6f));
                    UiKit.Round(card, "Icon_HeartR", GameUiColors.Loss, 10f, topLeft, center + new Vector2(-1.5f, 6f));
                    var tip = IconFill(UiKit.Fill(card, "Icon_HeartTip", GameUiColors.Loss, new Vector2(9f, 9f), new Vector2(0.5f, 0.5f), Vector2.zero));
                    tip.type = Image.Type.Simple;
                    var tipRect = tip.rectTransform;
                    tipRect.anchorMin = topLeft;
                    tipRect.anchorMax = topLeft;
                    tipRect.anchoredPosition = center + new Vector2(0f, -2f);
                    tipRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
            }
        }

        /// <summary>A tiny icon part: the rounded sprite scaled whole, since nine-slicing breaks below its border size.</summary>
        private static Image IconFill(Image image)
        {
            image.type = Image.Type.Simple;
            return image;
        }

        private void BuildPrompt()
        {
            var card = UiKit.Card(canvas.transform, "Prompt", UiKit.Paper, new Vector2(980f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 36f));
            promptCard = card.parent.gameObject;
            promptText = UiKit.Label(card, string.Empty, 32, UiKit.Ink, new Vector2(920f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            promptCard.SetActive(false);
        }

        private void BuildLoading()
        {
            // A small pill in the top right corner: the wait never covers the player or the middle of the town.
            var card = UiKit.Card(canvas.transform, "Loading", UiKit.Paper, new Vector2(440f, 64f), new Vector2(1f, 1f), new Vector2(-24f, -24f));
            loadingCard = card.parent.gameObject;
            loadingText = UiKit.Label(card, string.Empty, 26, UiKit.Ink, new Vector2(410f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            loadingCard.SetActive(false);
        }

        // ---------------------------------------------------------------- stations

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The overlay lives across scenes, but the main menu has its own screen.
            canvas.enabled = scene.name != SceneNames.Menu;
            HideSpeech();
            usedStations.Clear();
            workStation = null;
            homeDoor = null;
            RefreshPowerCut();
            TryStartOpening(scene);
            BindStations();
            if (session != null && session.Loop.Outfit != PlayerOutfit.Everyday)
            {
                PlayerOutfit.Wear(GameObject.FindWithTag(Station.PlayerTag), session.Loop.Outfit);
            }

            if (scene.name == SceneNames.Casa && HouseVisit.InListing)
            {
                // A house for sale or rent uses the same rooms, but the mother lives in her own house.
                foreach (var name in new[] { "Mama", "Thing_Mama" })
                {
                    var mother = GameObject.Find(name);
                    if (mother != null)
                    {
                        mother.SetActive(false);
                    }
                }
            }
        }

        /// <summary>True inside a house for sale or rent that the player has not rented or bought.</summary>
        private bool InHouseNotMine()
        {
            if (!HouseVisit.InListing || SceneManager.GetActiveScene().name != SceneNames.Casa)
            {
                return false;
            }

            var home = session.Holdings.Home;
            foreach (var listing in MarketAssumptions.Homes)
            {
                if (listing.Id == HouseVisit.ListingId)
                {
                    return home == null || home.Name != listing.Name;
                }
            }

            return true;
        }

        private void BindStations()
        {
            inside.Clear();
            homeDoor = GameObject.Find(HomeDoorName);
            foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None))
            {
                station.PlayerEntered += OnStationEntered;
                station.PlayerExited += OnStationExited;
                if (station.Type == StationType.Work)
                {
                    workStation = station;
                }
            }
        }

        private void OnStationEntered(Station station)
        {
            inside.Remove(station);
            inside.Add(station);
        }

        private void OnStationExited(Station station)
        {
            inside.Remove(station);

            // Walking away is what makes a place usable again, and it ends the conversation with whoever is there.
            usedStations.Remove(station);
            if (station == speakingStation)
            {
                HideSpeech();
            }
        }

        /// <summary>Remembers who is talking, so the line goes away when the player leaves them. Thoughts follow the player.</summary>
        private void ShowSpeech(Station station, string speaker, string text)
        {
            speakingStation = string.IsNullOrEmpty(speaker) ? null : station;
            subtitles.Show(speaker, text);
        }

        private void HideSpeech()
        {
            speakingStation = null;
            subtitles.Hide();
        }

        /// <summary>Backup for a speaker the trigger misses (the player walked through it fast): too far means the talk is over.</summary>
        private void TickSpeech()
        {
            if (speakingStation == null || !subtitles.IsVisible)
            {
                speakingStation = null;
                return;
            }

            var player = GameObject.FindWithTag(Station.PlayerTag);
            if (player != null && Vector2.Distance(player.transform.position, speakingStation.transform.position) > SpeechRadius)
            {
                HideSpeech();
            }
        }

        private Station Nearby
        {
            get
            {
                inside.RemoveAll(s => s == null);
                var hero = GameObject.FindWithTag(Station.PlayerTag);
                for (int i = inside.Count - 1; i >= 0; i--)
                {
                    // A wall between rooms hides what is on the other side, even if its zone reaches through.
                    if (hero == null || inside[i].CanBeReachedFrom(hero.transform.position))
                    {
                        return inside[i];
                    }
                }

                return null;
            }
        }

        // ---------------------------------------------------------------- frame

        private float nextMoodUpdate;

        private void Update()
        {
            if (!canvas.enabled)
            {
                return;
            }

            if (Time.time >= nextMoodUpdate)
            {
                nextMoodUpdate = Time.time + 1f;
                if (session?.State != null)
                {
                    var hero = GameObject.FindWithTag(Station.PlayerTag);
                    if (hero != null)
                    {
                        var anim = hero.GetComponentInChildren<PlayerAnimator>();
                        if (anim != null)
                        {
                            anim.SetMood(session.State.Wellbeing / 100f);
                        }
                    }
                }
            }

            TickIncidents();
            TickCell();
            TickWorkGuide();
            TickTasks();
            effects.Tick(Time.unscaledDeltaTime);
            subtitles.Tick(Time.unscaledDeltaTime, modal.IsOpen || pause.IsOpen || phone.IsOpen || negotiationPanel.IsOpen);
            TickSpeech();

            if (pause.IsOpen)
            {
                PlayerController.Blocked = true;
                promptCard.SetActive(false);
                loadingCard.SetActive(false);
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    pause.Back();
                }

                return;
            }

            if (talkPanel.IsOpen)
            {
                // The talk takes the keyboard like the seller's window: the text box needs every key.
                PlayerController.Blocked = true;
                loadingCard.SetActive(false);
                promptCard.SetActive(false);
                talkPanel.Tick();
                return;
            }

            if (negotiationPanel.IsOpen)
            {
                // The seller's window takes the keyboard: the text box needs every key.
                PlayerController.Blocked = true;
                loadingCard.SetActive(false);
                promptCard.SetActive(false);
                negotiationPanel.Tick();
                return;
            }

            if (phone.IsOpen)
            {
                PlayerController.Blocked = true;
                promptCard.SetActive(false);
                loadingCard.SetActive(false);
                phone.Tick(Time.unscaledDeltaTime);
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    phone.Back();
                }
                else if (Input.GetKeyDown(KeyCode.P) && !phone.IsTyping)
                {
                    phone.Close();
                }

                return;
            }

            bool open = modal.IsOpen;

            PlayerController.Blocked = open || session.Busy;
            loadingCard.SetActive(session.Busy && !open);
            if (loadingCard.activeSelf)
            {
                // The wait for the summary of the year says what is being made, with dots that come and go.
                string wait = Loc.T(session.Finishing ? "Armando tu año" : "El game master está pensando");
                loadingText.text = wait + new string('.', 1 + (int)(Time.unscaledTime * 2.5f) % 3);
            }

            RemoteImages.Prefetch();

            modal.Tick(Time.unscaledDeltaTime);
            if (open)
            {
                promptCard.SetActive(false);

                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    {
                        modal.Press(i);
                    }
                }

                if (modal.OptionCount == 1 && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E)))
                {
                    modal.Press(0);
                }

                return;
            }

            // At work the clock goes through the morning while the shift plays, instead of jumping at the end.
            if (Interaction.Playing && session.Loop.Job.OnShift)
            {
                session.Loop.TickShift(Time.deltaTime);
            }

            // Otherwise the clock runs only while the player is free: no window, no scene, no wait for the server.
            if (!session.Busy && !Interaction.Playing && !SceneTransition.IsBusy && session.TickClock(Time.deltaTime))
            {
                FallAsleepScene();
                return;
            }

            string time = session.Loop.Clock.Time;
            if (clockText.text != time)
            {
                clockText.text = time;
            }

            if (!session.Busy && !Interaction.Playing && Input.GetKeyDown(KeyCode.P))
            {
                OpenPhone();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OpenPause();
                return;
            }

            if (openingPlaying)
            {
                promptCard.SetActive(true);
                promptText.text = Loc.T(SkipHint);
                return;
            }

            // A place used once waits until the player walks away and comes back, so E cannot repeat it on the spot.
            var station = Nearby;
            string text = station != null && !session.Busy && !Interaction.Playing && !usedStations.Contains(station) ? PromptFor(station) : null;
            promptCard.SetActive(text != null);
            if (text != null)
            {
                promptText.text = PromptText(text);
                if (Input.GetKeyDown(KeyCode.E) && text.StartsWith("[E]  "))
                {
                    usedStations.Add(station);
                    Interact(station);
                }
            }
        }

        /// <summary>The prompt in the player's language: the key "[E]  " stays, the rest is translated.</summary>
        private static string PromptText(string text)
        {
            const string key = "[E]  ";
            return text.StartsWith(key) ? key + Loc.T(text.Substring(key.Length)) : Loc.T(text);
        }

        private string PromptFor(Station station)
        {
            var loop = session.Loop;
            switch (station.Type)
            {
                case StationType.Bank:
                    return "[E]  Cobrar el salario y cerrar el mes";
                case StationType.Home:
                    return "[E]  Revisar tu estado de cuenta";
                case StationType.Thing:
                    if (station.name == "Thing_CellCot")
                    {
                        return loop.JailDaysRemaining > 0 ? "[E]  " + Loc.T(station.DisplayName) : string.Empty;
                    }

                    return "[E]  " + station.DisplayName;
                case StationType.Atm:
                    return "[E]  Usar el cajero automático";
                case StationType.RealEstate:
                case StationType.CarDealer:
                case StationType.Farm:
                case StationType.HealthDesk:
                case StationType.Wardrobe:
                case StationType.Beach:
                    return "[E]  " + station.DisplayName;
                case StationType.Furniture:
                    return "[E]  " + station.DisplayName;
                case StationType.Courses:
                    return loop.Clock.IsNight ? Loc.T("Está cerrado. Abrimos a las 06:00.") : "[E]  " + station.DisplayName;
                case StationType.Work:
                    return WorkPrompt();
                default:
                    if (loop.Clock.IsNight)
                    {
                        return Loc.T("Está cerrado. Abrimos a las 06:00.");
                    }

                    if (loop.CanDoTask(station.Type))
                    {
                        return "[E]  " + station.DisplayName;
                    }

                    if (loop.AllTasksDone)
                    {
                        return Loc.T("Ya hiciste tus tareas del mes. Ve al banco a cobrar.");
                    }

                    if (loop.TasksToday >= GameRules.TasksPerDay)
                    {
                        return Loc.T("Ya resolviste los pendientes de hoy. Descansa hasta mañana.");
                    }

                    return Loc.F("Ya pasaste por {0} hoy.", Loc.T(station.DisplayName));
            }
        }

        /// <summary>What the station does once its scene is over (see GameUi.Scenes.cs): opens its window or starts its task.</summary>
        private void InteractNow(Station station)
        {
            switch (station.Type)
            {
                case StationType.Bank:
                    AskCloseMonth();
                    break;
                case StationType.Home:
                    ShowStatement();
                    break;
                case StationType.Atm:
                    ShowAtm();
                    break;
                case StationType.Thing:
                    if (station.name == DepositWindowName)
                    {
                        ShowDeposit();
                        break;
                    }

                    ShowThing(station);
                    break;
                case StationType.Store:
                    session.BeginNegotiation();
                    break;
                case StationType.RealEstate:
                    ShowRealEstate(station);
                    break;
                case StationType.CarDealer:
                    ShowCarCatalog();
                    break;
                case StationType.Farm:
                    ShowFarm();
                    break;
                case StationType.HealthDesk:
                    ShowHealthDesk();
                    break;
                case StationType.Wardrobe:
                    ShowWardrobe();
                    break;
                case StationType.Beach:
                    ShowBeach();
                    break;
                case StationType.Furniture:
                    ShowFurniture();
                    break;
                case StationType.Courses:
                    ShowCourses();
                    break;
                case StationType.Work:
                    session.FinishShift();
                    break;
                case StationType.Lender:
                case StationType.Pyramid:
                    AskHowToTalk(station.Type);
                    break;
                default:
                    if (session.Loop.CanDoTask(station.Type))
                    {
                        session.BeginTask(station.Type);
                    }

                    break;
            }
        }

        // ---------------------------------------------------------------- hud

        private void CloseWindows()
        {
            ResetSentence();
            modal.Hide();
            phone.Close();
            negotiationPanel.Hide();
            pause.Hide();
            Time.timeScale = 1f;
        }

        private void OnNegotiationChanged(Negotiation negotiation)
        {
            negotiationPanel.Refresh();
            SpeakSeller(negotiation);
        }

        /// <summary>The seller babbles once for each new line: the greeting first, then every answer.</summary>
        private void SpeakSeller(Negotiation negotiation)
        {
            if (negotiation == null)
            {
                return;
            }

            if (negotiation != spokenNegotiation)
            {
                spokenNegotiation = negotiation;
                spokenTurns = 0;
                AudioDirector.Talk(negotiation.Opening, "vendedor");
            }

            if (negotiation.Turns.Count > spokenTurns)
            {
                spokenTurns = negotiation.Turns.Count;
                AudioDirector.Talk(negotiation.Turns[spokenTurns - 1].Seller, "vendedor");
            }
        }

        private void Refresh()
        {
            var state = session.State;
            var loop = session.Loop;
            WalkWatch.HasSwimsuit = loop.HasSwimsuit;
            monthText.text = Loc.F("Mes {0} de {1}", Mathf.Min(loop.MonthsPlayed + 1, GameRules.MonthsToPlay), GameRules.MonthsToPlay)
                + "  ·  " + Loc.F("Semana {0}", loop.Clock.Day);
            clockText.text = loop.Clock.Time;
            tasksText.text = Loc.F("Tareas {0}/{1}", loop.TasksDone, GameRules.TasksPerMonth);
            // In the opening the pocket is empty until the mother hands over the present.
            cashText.text = Money.Format(openingPlaying && !openingCashGiven ? 0L : state.Cash);
            savingsText.text = Money.Format(state.Savings + state.EmergencyFund);
            cardText.text = Money.Format(state.CreditCard.Balance);
            installmentsText.text = Money.Format(GameRules.MonthlyInstallments(state));

            float wellbeing = Mathf.Clamp01(state.Wellbeing / 100f);
            wellbeingFill.rectTransform.sizeDelta = new Vector2(BarWidth * wellbeing, 26f);
            wellbeingFill.color = wellbeing > 0.5f ? GameUiColors.Gain : (wellbeing > 0.25f ? new Color(0.95f, 0.65f, 0.15f, 1f) : GameUiColors.Loss);

            negotiationPanel.Refresh();
            RefreshPowerCut();
            CelebrateGoals();
        }

        /// <summary>A goal reached gets its sound, its confetti and a line, once.</summary>
        private void CelebrateGoals()
        {
            foreach (var goal in session.Loop.Goals.TakeNewlyDone(session.State, session.Holdings))
            {
                AudioDirector.Effect(SoundEffectKind.Chime);
                Confetti.Burst(canvas.transform);
                subtitles.Show(string.Empty, Loc.F("¡Meta cumplida: {0}!", Loc.T(PhoneUi.TitleOf(goal.Kind))));
            }
        }

        // ---------------------------------------------------------------- windows

        private void ShowEvent(GameEvent gameEvent, ResultSource source)
        {
            phone.Close();
            var options = new List<ModalOption>();
            for (int i = 0; i < gameEvent.Choices.Count; i++)
            {
                var choice = gameEvent.Choices[i];
                string id = choice.Id;
                options.Add(new ModalOption { Label = $"{i + 1}.  {Loc.T(choice.Label)}", Color = GameUiColors.Cycle(i), OnPress = () => session.Choose(id) });
            }

            string who = gameEvent.Npc != null ? gameEvent.Npc.Name + "  ·  " + gameEvent.Npc.Role : string.Empty;
            if (source == ResultSource.Fallback)
            {
                who = (who.Length > 0 ? who + "  ·  " : string.Empty) + Loc.T("evento local");
            }

            modal.Backdrop = RemoteImages.Get(gameEvent.ImageKey);
            AudioDirector.Talk(Loc.T(gameEvent.Narrative), gameEvent.Npc != null ? gameEvent.Npc.Name : string.Empty);
            modal.Show(Loc.T(gameEvent.Title), who, Loc.T(gameEvent.Narrative), options, gameEvent.Npc != null ? gameEvent.Npc.Name : Loc.T("Narrador"));
        }

        private void ShowOutcome(ChoiceOutcome outcome)
        {
            phone.Close();
            var text = new StringBuilder();
            Line(text, "Efectivo", outcome.CashDelta, true);
            Line(text, "Ahorros", outcome.SavingsDelta, true);
            Line(text, "Fondo de emergencia", outcome.EmergencyFundDelta, true);
            Line(text, "Tarjeta", outcome.CardDelta, false);
            Line(text, "Cuotas al mes", outcome.InstallmentsPerMonthDelta, false);
            Line(text, "Deuda con prestamista", outcome.InformalDebtDelta, false);
            if (outcome.WellbeingDelta != 0)
            {
                text.AppendLine($"{Loc.T("Ánimo")}  <color={GameUiColors.Hex(outcome.WellbeingDelta > 0 ? GameUiColors.Gain : GameUiColors.Loss)}>{(outcome.WellbeingDelta > 0 ? "+" : string.Empty)}{outcome.WellbeingDelta}</color>");
            }

            if (outcome.ChangedNothing)
            {
                text.AppendLine(Loc.T("Tus cuentas quedaron igual."));
            }

            if (!string.IsNullOrEmpty(outcome.Note))
            {
                text.AppendLine();
                text.AppendLine(outcome.Note);
            }

            AppendDeal(text, session.LastDeal);

            text.AppendLine();
            text.Append(session.Loop.AllTasksDone
                ? Loc.T("Hiciste todas tus tareas. Ve al banco a cobrar el salario.")
                : Loc.F("Tareas del mes: {0} de {1}.", session.Loop.TasksDone, GameRules.TasksPerMonth));

            modal.Show(Loc.T("Lo que pasó"), Loc.T(outcome.ChoiceLabel), text.ToString(), new[] { Continue(null) });
        }

        /// <summary>After a deal: the real cost of the installments next to the cash price, and the questions asked.</summary>
        private static void AppendDeal(StringBuilder text, DealReport deal)
        {
            if (deal == null)
            {
                return;
            }

            text.AppendLine();
            text.AppendLine(Loc.F("Precio de contado  {0}", SellerOffer.FormatColones(deal.CashPrice)));
            string verb = Loc.T(deal.Bought ? "Terminas pagando" : "A cuotas ibas a pagar");
            text.AppendLine(Loc.F("{0}  {1}  ({2} más)", verb, $"<color={GameUiColors.Hex(GameUiColors.Loss)}>{SellerOffer.FormatColones(deal.TotalCost)}</color>", SellerOffer.FormatColones(deal.ExtraOverCash)));
            text.AppendLine(Loc.F("Preguntas clave que hiciste  {0} de {1}", deal.QuestionsAsked, deal.QuestionsTotal));
            if (!string.IsNullOrEmpty(deal.Note))
            {
                text.AppendLine($"<color={GameUiColors.Hex(GameUiColors.Loss)}>{Loc.T(deal.Note)}</color>");
            }
        }

        private static void Line(StringBuilder text, string label, long delta, bool moreIsGood)
        {
            if (delta == 0L)
            {
                return;
            }

            bool good = (delta > 0L) == moreIsGood;
            text.AppendLine($"{Loc.T(label)}  <color={GameUiColors.Hex(good ? GameUiColors.Gain : GameUiColors.Loss)}>{Money.FormatSigned(delta)}</color>");
        }

        private void AskCloseMonth()
        {
            var text = new StringBuilder();
            if (session.Loop.AllTasksDone)
            {
                text.AppendLine(Loc.T("Cobras tu salario y se pagan las cuotas, la tarjeta y los gastos del mes."));
            }
            else
            {
                text.AppendLine(Loc.F("Todavía te faltan tareas este mes ({0} de {1}). Si cierras ahora, te pierdes esas decisiones.", session.Loop.TasksDone, GameRules.TasksPerMonth));
            }

            if (session.Loop.Clock.Day < GameRules.DaysPerMonth)
            {
                int remainingShifts = GameRules.DaysPerMonth - session.Loop.Clock.Day + (!session.Loop.Job.WorkedToday && session.Loop.Clock.Hour >= GameRules.WorkStartHour ? 1 : 0);
                text.AppendLine();
                text.Append(Loc.F("Aviso: el mes aún no termina (semana {0} de {1}). Se te rebajarán {2} turnos de trabajo no laborados.", session.Loop.Clock.Day, GameRules.DaysPerMonth, remainingShifts));
            }

            modal.Show(Loc.T("Banco"), Loc.T("Cierre de mes"), text.ToString(), new[]
            {
                new ModalOption { Label = Loc.T("1.  Cobrar y cerrar el mes"), Color = GameUiColors.Green, OnPress = () => session.CloseMonth() },
                new ModalOption { Label = Loc.T("2.  Todavía no"), Color = GameUiColors.Pink, OnPress = null }
            });
        }

        private void ShowMonthReport(MonthlyTickReport report)
        {
            phone.Close();
            var text = new StringBuilder();
            text.AppendLine(Loc.F("Salario  {0}", $"<color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.SalaryCredited)}</color>"));
            if (report.SalaryDocked > 0L)
            {
                text.AppendLine(Loc.F("Ya con el rebajo por tardanzas y faltas  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(report.SalaryDocked)}</color>"));
            }

            if (report.AguinaldoCredited > 0L)
            {
                text.AppendLine(Loc.F("Aguinaldo  {0}", $"<color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.AguinaldoCredited)}</color>"));
            }

            text.AppendLine(Loc.F("Gastos de vida  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(GameRules.LivingExpenses)}</color>"));
            if (session.Loop.Bills.LastLateFees > 0L)
            {
                text.AppendLine(Loc.F("Recargos por recibos sin pagar  {0} más que debés", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>{Money.Format(session.Loop.Bills.LastLateFees)}</color>"));
            }

            if (session.Loop.LastRoomRent > 0L)
            {
                text.AppendLine(Loc.F("Alquiler del cuarto  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(session.Loop.LastRoomRent)}</color>"));
            }

            if (report.InstallmentsCharged > 0L)
            {
                text.AppendLine(Loc.F("Cuotas  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(report.InstallmentsCharged)}</color>"));
            }

            var card = report.CardCycle;
            if (card.StartingBalance > 0L)
            {
                text.AppendLine(Loc.F("Pago mínimo de la tarjeta  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(card.AmountPaid)}</color>"));
                text.AppendLine(Loc.F("Intereses de la tarjeta  {0} de deuda", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>+{Money.Format(card.InterestCharged)}</color>"));
            }

            if (report.InformalLoanInterestAccrued > 0L)
            {
                text.AppendLine(Loc.F("Intereses del prestamista  {0} de deuda", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>+{Money.Format(report.InformalLoanInterestAccrued)}</color>"));
            }

            text.AppendLine();
            text.Append(Loc.F("Te queda {0} en efectivo y la tarjeta debe {1}.", Money.Format(report.EndingCash), Money.Format(report.EndingCardBalance)));

            bool last = session.Loop.IsFinished;
            modal.Show(Loc.F("Mes {0} cerrado", report.MonthProcessed), Loc.T("Estado de cuenta del mes"), text.ToString(), new[]
            {
                new ModalOption { Label = last ? Loc.T("Ver mi resumen") : Loc.T("Seguir"), Color = GameUiColors.Yellow, OnPress = last ? (System.Action)(() => session.FinishRun()) : null }
            });
        }

        private void ShowQuincenaNotice(long amount)
        {
            modal.Show(
                Loc.T("SINPE Móvil"),
                Loc.T("Pago de quincena"),
                Loc.F("Depósito por {0} de Soda El Muelle por concepto de adelanto quincenal de salario.", Money.Format(amount)),
                new[] { Continue(null) });
        }

        /// <summary>Everyday objects only talk: they never move money, that is the finance engine's job.</summary>
        private void ShowThing(Station station)
        {
            if (station.name == "Thing_Pet" && session != null && session.State != null)
            {
                session.State.Wellbeing = Mathf.Clamp(session.State.Wellbeing + 2, 0, 100);
            }

            string cut = CutLine(station);
            if (cut != null)
            {
                ShowSpeech(station, string.Empty, Loc.T(cut));
                return;
            }

            string key = SceneManager.GetActiveScene().name + "/" + station.name;
            thingUses.TryGetValue(key, out int uses);
            thingUses[key] = uses + 1;
            string line = ThingLines.Pick(station.Lines, uses, session.State);
            AudioDirector.Talk(Loc.T(line), station.DisplayName);

            // A pedestrian stops walking to chat.
            var walker = station.GetComponentInParent<Patrol>();
            var player = GameObject.FindWithTag(Station.PlayerTag);
            if (walker != null && player != null)
            {
                walker.Greet(player.transform.position, 8f);
            }

            // People speak as a subtitle with their name; an object only gives the player a thought. Neither stops the walk.
            bool person = station.name == "Thing_Talk";
            ShowSpeech(station, person ? Loc.T(PersonName(station)) : string.Empty, Loc.T(line));
        }

        /// <summary>The name that goes before what a person says: the one of the character, or a plain word for a passer-by.</summary>
        private static string PersonName(Station station)
        {
            var root = station.transform.parent;
            string name = root != null ? root.name : string.Empty;
            if (string.IsNullOrEmpty(name) || name.StartsWith("Peaton"))
            {
                return "Vecino";
            }

            if (name == "Estafador")
            {
                return "El del puesto";
            }

            // "DonBeto" is shown as "Don Beto".
            var spaced = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                {
                    spaced.Append(' ');
                }

                spaced.Append(name[i]);
            }

            return spaced.ToString();
        }

        private void OnThought(string spanish)
        {
            ShowSpeech(null, string.Empty, Loc.T(spanish));
        }

        private void ShowStatement()
        {
            var state = session.State;
            var text = new StringBuilder();
            text.AppendLine(Loc.F("Efectivo  {0}", Money.Format(state.Cash)));
            text.AppendLine(Loc.F("Ahorros  {0}", Money.Format(state.Savings)));
            text.AppendLine(Loc.F("Fondo de emergencia  {0}", Money.Format(state.EmergencyFund)));
            text.AppendLine(Loc.F("Tarjeta  {0} de {1}  ({2}% al mes)", Money.Format(state.CreditCard.Balance), Money.Format(state.CreditCard.Limit), state.CreditCard.MonthlyRatePct));
            foreach (var installment in state.Installments)
            {
                if (installment.MonthsLeft > 0)
                {
                    text.AppendLine(Loc.F("{0}  {1} al mes, faltan {2} cuotas", Loc.T(installment.Item), Money.Format(installment.Monthly), installment.MonthsLeft));
                }
            }

            foreach (var loan in state.InformalLoans)
            {
                text.AppendLine(Loc.F("{0}  {1}  ({2}% al mes)", Loc.T(loan.Lender), Money.Format(loan.Balance), loan.MonthlyRatePct));
            }

            text.AppendLine();
            text.Append(Loc.F("Lo que tienes menos lo que debes: {0}", Money.Format(GameRules.NetWorth(state))));
            modal.Show(Loc.T("Tu estado de cuenta"), Loc.F("Salario neto {0} al mes", Money.Format(state.SalaryNet)), text.ToString(), new[] { Continue(null) });
        }

        private void ShowEnd(SessionSummary summary)
        {
            phone.Close();
            modal.Backdrop = RemoteImages.Get(PostcardKeys.For(summary.TotalDebt));
            var text = new StringBuilder();
            text.AppendLine(Loc.T(summary.Narrative));
            text.AppendLine();
            text.AppendLine(Loc.F("Decisiones tomadas: {0}", summary.Decisions));
            text.AppendLine(Loc.F("Lo que debes: {0}", Money.Format(summary.TotalDebt)));
            text.AppendLine(Loc.F("Lo que tienes menos lo que debes: {0}", Money.Format(summary.NetWorth)));
            text.AppendLine();
            text.AppendLine(Loc.T("Tus notas"));
            foreach (var line in ReportCard.Build(session.State, session.Loop.Holdings, session.Loop.Bills))
            {
                text.AppendLine($"{Loc.T(line.Concept)}  <b>{line.Grade}</b>");
            }

            var assets = AssetsReport.Build(session.Loop.Holdings, session.Loop.MonthsPlayed);
            if (assets.Count == 0)
            {
                modal.Show(Loc.T("Fin de la partida"), Loc.F("{0} meses jugados", summary.MonthsPlayed), text.ToString(), new[]
                {
                    new ModalOption { Label = Loc.T("Jugar de nuevo"), Color = GameUiColors.Green, OnPress = Restart }
                });
                return;
            }

            long withAssets = GameRules.NetWorth(session.State, session.Loop.Holdings);
            text.AppendLine();
            text.Append(Loc.F("Con lo que compraste: {0}", Money.Format(withAssets)));
            modal.Show(Loc.T("Fin de la partida"), Loc.F("{0} meses jugados", summary.MonthsPlayed), text.ToString(), new[]
            {
                new ModalOption { Label = Loc.T("Ver lo que compraste"), Color = GameUiColors.Yellow, OnPress = () => ShowAssetsReport(assets) },
                new ModalOption { Label = Loc.T("Jugar de nuevo"), Color = GameUiColors.Green, OnPress = Restart }
            });
        }

        /// <summary>True while the pause window or its settings are up.</summary>
        public bool PauseOpen => pause.IsOpen;

        /// <summary>
        /// Opens the pause window. Time stops while it is up; the audio and the window run on unscaled time.
        /// Nothing happens while the game master is answering.
        /// </summary>
        public void OpenPause()
        {
            if (session.Busy || pause.IsOpen)
            {
                return;
            }

            Time.timeScale = 0f;
            pause.Open();
        }

        private void ResumeFromPause()
        {
            Time.timeScale = 1f;
        }

        private void GoToMenu()
        {
            Time.timeScale = 1f;
            PlayerController.Blocked = false;
            LoadingScreen.Load(SceneNames.Menu);
        }

        private void Restart()
        {
            session.StartNewGame();
            LoadingScreen.Load(SceneNames.Town);
        }

        private static ModalOption Continue(System.Action then)
        {
            return new ModalOption { Label = Loc.T("Continuar"), Color = GameUiColors.Yellow, OnPress = then };
        }
    }
}

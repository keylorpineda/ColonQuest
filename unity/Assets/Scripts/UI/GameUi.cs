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
        private PhoneUi phone;
        private PauseMenu pause;
        private Negotiation spokenNegotiation;
        private int spokenTurns;

        private Text monthText;
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
            BuildHud();
            BuildPrompt();
            BuildLoading();
            modal = new ModalPanel(canvas.transform);
            subtitles = new SubtitleBar(canvas.transform);
            effects = new ScreenEffects(canvas.transform);
            Thoughts.Said += OnThought;
            Interaction.Spoke += OnSpoke;
            negotiationPanel = new NegotiationPanel(canvas.transform, session);
            negotiationPanel.SignContract = PlayContract;
            phone = new PhoneUi(canvas.transform, session);
            pause = new PauseMenu(canvas.transform, ResumeFromPause, GoToMenu);
            BuildIncidents();
            BuildCell();

            session.Changed += Refresh;
            session.NegotiationStarted += OnNegotiationChanged;
            session.NegotiationUpdated += OnNegotiationChanged;
            session.NewGameStarted += CloseWindows;
            Loc.Changed += Refresh;
            LocalSeller.Translate = Loc.T;
            session.EventReady += ShowEvent;
            session.ChoiceApplied += ShowOutcome;
            session.MonthClosed += ShowMonthReport;
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
                session.NewGameStarted -= CloseWindows;
                session.EventReady -= ShowEvent;
                session.ChoiceApplied -= ShowOutcome;
                session.MonthClosed -= ShowMonthReport;
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
            var card = UiKit.Card(canvas.transform, "Hud", UiKit.Paper, new Vector2(470f, 330f), topLeft, new Vector2(24f, -24f));

            monthText = UiKit.Label(card, string.Empty, 36, UiKit.Ink, new Vector2(260f, 44f), topLeft, new Vector2(22f, -14f));
            tasksText = UiKit.Label(card, string.Empty, 26, GameUiColors.Muted, new Vector2(170f, 44f), new Vector2(1f, 1f), new Vector2(-22f, -14f), TextAnchor.MiddleRight);

            cashText = Row(card, "Efectivo", 1);
            savingsText = Row(card, "Ahorros", 2);
            cardText = Row(card, "Tarjeta", 3);
            installmentsText = Row(card, "Cuotas al mes", 4);

            UiKit.Label(card, "Ánimo", 26, GameUiColors.Muted, new Vector2(200f, 36f), topLeft, new Vector2(22f, -226f));
            UiKit.Fill(card, "WellbeingBack", new Color(0.82f, 0.84f, 0.9f, 1f), new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -231f));
            wellbeingFill = UiKit.Fill(card, "WellbeingFill", GameUiColors.Gain, new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -231f));
            UiKit.Label(card, "[P]  Celular", 24, GameUiColors.Muted, new Vector2(300f, 36f), topLeft, new Vector2(22f, -276f));
        }

        private Text Row(RectTransform card, string label, int index)
        {
            float y = -(14f + 44f + index * 40f) + 36f;
            UiKit.Label(card, label, 26, GameUiColors.Muted, new Vector2(280f, 36f), new Vector2(0f, 1f), new Vector2(22f, y));
            return UiKit.Label(card, string.Empty, 30, UiKit.Ink, new Vector2(160f, 36f), new Vector2(1f, 1f), new Vector2(-22f, y), TextAnchor.MiddleRight);
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
            var card = UiKit.Card(canvas.transform, "Loading", UiKit.Paper, new Vector2(640f, 120f), new Vector2(0.5f, 0.5f), Vector2.zero);
            loadingCard = card.parent.gameObject;
            loadingText = UiKit.Label(card, string.Empty, 34, UiKit.Ink, new Vector2(580f, 80f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            loadingCard.SetActive(false);
        }

        // ---------------------------------------------------------------- stations

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The overlay lives across scenes, but the main menu has its own screen.
            canvas.enabled = scene.name != SceneNames.Menu;
            subtitles.Hide();
            BindStations();
        }

        private void BindStations()
        {
            inside.Clear();
            foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None))
            {
                station.PlayerEntered += OnStationEntered;
                station.PlayerExited += OnStationExited;
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
        }

        private Station Nearby
        {
            get
            {
                inside.RemoveAll(s => s == null);
                return inside.Count > 0 ? inside[inside.Count - 1] : null;
            }
        }

        // ---------------------------------------------------------------- frame

        private void Update()
        {
            if (!canvas.enabled)
            {
                return;
            }

            TickIncidents();
            TickCell();
            effects.Tick(Time.unscaledDeltaTime);
            subtitles.Tick(Time.unscaledDeltaTime, modal.IsOpen || pause.IsOpen || phone.IsOpen || negotiationPanel.IsOpen);

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

            var station = Nearby;
            string text = station != null && !session.Busy && !Interaction.Playing ? PromptFor(station) : null;
            promptCard.SetActive(text != null);
            if (text != null)
            {
                promptText.text = PromptText(text);
                if (Input.GetKeyDown(KeyCode.E))
                {
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
                    return "[E]  " + station.DisplayName;
                case StationType.Atm:
                    return "[E]  Consultar tu saldo en el cajero";
                case StationType.RealEstate:
                case StationType.CarDealer:
                case StationType.Farm:
                case StationType.HealthDesk:
                case StationType.Wardrobe:
                case StationType.Beach:
                    return "[E]  " + station.DisplayName;
                default:
                    if (loop.CanDoTask(station.Type))
                    {
                        return "[E]  " + station.DisplayName;
                    }

                    return loop.AllTasksDone ? "Ya hiciste tus tareas del mes. Ve al banco a cobrar." : Loc.F("Ya pasaste por {0} este mes.", Loc.T(station.DisplayName));
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
                case StationType.Atm:
                    ShowStatement();
                    break;
                case StationType.Thing:
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
            monthText.text = Loc.F("Mes {0} de {1}", Mathf.Min(loop.MonthsPlayed + 1, GameRules.MonthsToPlay), GameRules.MonthsToPlay);
            tasksText.text = Loc.F("Tareas {0}/{1}", loop.TasksDone, GameRules.TasksPerMonth);
            cashText.text = Money.Format(state.Cash);
            savingsText.text = Money.Format(state.Savings + state.EmergencyFund);
            cardText.text = Money.Format(state.CreditCard.Balance);
            installmentsText.text = Money.Format(GameRules.MonthlyInstallments(state));

            float wellbeing = Mathf.Clamp01(state.Wellbeing / 100f);
            wellbeingFill.rectTransform.sizeDelta = new Vector2(BarWidth * wellbeing, 26f);
            wellbeingFill.color = wellbeing > 0.5f ? GameUiColors.Gain : (wellbeing > 0.25f ? new Color(0.95f, 0.65f, 0.15f, 1f) : GameUiColors.Loss);

            negotiationPanel.Refresh();
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
            modal.Show(gameEvent.Title, who, Loc.T(gameEvent.Narrative), options, gameEvent.Npc != null ? gameEvent.Npc.Name : "Narrador");
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

            AppendDeal(text, session.LastDeal);

            text.AppendLine();
            text.Append(session.Loop.AllTasksDone
                ? Loc.T("Hiciste todas tus tareas. Ve al banco a cobrar el salario.")
                : Loc.F("Tareas del mes: {0} de {1}.", session.Loop.TasksDone, GameRules.TasksPerMonth));

            modal.Show("Lo que pasó", outcome.ChoiceLabel, text.ToString(), new[] { Continue(null) });
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
            string body = session.Loop.AllTasksDone
                ? "Cobras tu salario y se pagan las cuotas, la tarjeta y los gastos del mes."
                : Loc.F("Todavía te faltan tareas este mes ({0} de {1}). Si cierras ahora, te pierdes esas decisiones.", session.Loop.TasksDone, GameRules.TasksPerMonth);

            modal.Show("Banco", "Cierre de mes", body, new[]
            {
                new ModalOption { Label = "1.  Cobrar y cerrar el mes", Color = GameUiColors.Green, OnPress = () => session.CloseMonth() },
                new ModalOption { Label = "2.  Todavía no", Color = GameUiColors.Pink, OnPress = null }
            });
        }

        private void ShowMonthReport(MonthlyTickReport report)
        {
            phone.Close();
            var text = new StringBuilder();
            text.AppendLine(Loc.F("Salario  {0}", $"<color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.SalaryCredited)}</color>"));
            if (report.AguinaldoCredited > 0L)
            {
                text.AppendLine(Loc.F("Aguinaldo  {0}", $"<color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.AguinaldoCredited)}</color>"));
            }

            text.AppendLine(Loc.F("Gastos de vida  {0}", $"<color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(GameRules.LivingExpenses)}</color>"));
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
            modal.Show(Loc.F("Mes {0} cerrado", report.MonthProcessed), "Estado de cuenta del mes", text.ToString(), new[]
            {
                new ModalOption { Label = last ? "Ver mi resumen" : "Seguir", Color = GameUiColors.Yellow, OnPress = last ? (System.Action)(() => session.FinishRun()) : null }
            });
        }

        /// <summary>Everyday objects only talk: they never move money, that is the finance engine's job.</summary>
        private void ShowThing(Station station)
        {
            string key = SceneManager.GetActiveScene().name + "/" + station.name;
            thingUses.TryGetValue(key, out int uses);
            thingUses[key] = uses + 1;
            string line = ThingLines.Pick(station.Lines, uses, session.State);
            AudioDirector.Talk(line, station.DisplayName);

            // A pedestrian stops walking to chat.
            var walker = station.GetComponentInParent<Patrol>();
            var player = GameObject.FindWithTag(Station.PlayerTag);
            if (walker != null && player != null)
            {
                walker.Greet(player.transform.position, 8f);
            }

            // People speak as a subtitle with their name; an object only gives the player a thought. Neither stops the walk.
            bool person = station.name == "Thing_Talk";
            subtitles.Show(person ? Loc.T(PersonName(station)) : string.Empty, line);
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
            subtitles.Show(string.Empty, Loc.T(spanish));
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
            modal.Show("Tu estado de cuenta", Loc.F("Salario neto {0} al mes", Money.Format(state.SalaryNet)), text.ToString(), new[] { Continue(null) });
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
            text.Append(Loc.F("Lo que tienes menos lo que debes: {0}", Money.Format(summary.NetWorth)));

            var assets = AssetsReport.Build(session.Loop.Holdings, session.Loop.MonthsPlayed);
            if (assets.Count == 0)
            {
                modal.Show("Fin de la partida", Loc.F("{0} meses jugados", summary.MonthsPlayed), text.ToString(), new[]
                {
                    new ModalOption { Label = "Jugar de nuevo", Color = GameUiColors.Green, OnPress = Restart }
                });
                return;
            }

            long withAssets = GameRules.NetWorth(session.State, session.Loop.Holdings);
            text.AppendLine();
            text.Append(Loc.F("Con lo que compraste: {0}", Money.Format(withAssets)));
            modal.Show("Fin de la partida", Loc.F("{0} meses jugados", summary.MonthsPlayed), text.ToString(), new[]
            {
                new ModalOption { Label = "Ver lo que compraste", Color = GameUiColors.Yellow, OnPress = () => ShowAssetsReport(assets) },
                new ModalOption { Label = "Jugar de nuevo", Color = GameUiColors.Green, OnPress = Restart }
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
            return new ModalOption { Label = "Continuar", Color = GameUiColors.Yellow, OnPress = then };
        }
    }
}

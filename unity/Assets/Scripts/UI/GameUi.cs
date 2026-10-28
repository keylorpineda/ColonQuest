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
    public sealed class GameUi : MonoBehaviour
    {
        private GameSession session;
        private Canvas canvas;
        private ModalPanel modal;

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

        private const float BarWidth = 190f;

        private readonly List<Station> inside = new List<Station>();

        public static GameUi Create(GameSession gameSession)
        {
            var go = new GameObject("GameUi");
            DontDestroyOnLoad(go);
            var ui = go.AddComponent<GameUi>();
            ui.Bind(gameSession);
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

            session.Changed += Refresh;
            session.NewGameStarted += CloseWindows;
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
            PlayerController.Blocked = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (session != null)
            {
                session.Changed -= Refresh;
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
            var card = UiKit.Card(canvas.transform, "Hud", UiKit.Paper, new Vector2(470f, 290f), topLeft, new Vector2(24f, -24f));

            monthText = UiKit.Label(card, string.Empty, 36, UiKit.Ink, new Vector2(260f, 44f), topLeft, new Vector2(22f, -14f));
            tasksText = UiKit.Label(card, string.Empty, 26, GameUiColors.Muted, new Vector2(170f, 44f), new Vector2(1f, 1f), new Vector2(-22f, -14f), TextAnchor.MiddleRight);

            cashText = Row(card, "Efectivo", 1);
            savingsText = Row(card, "Ahorros", 2);
            cardText = Row(card, "Tarjeta", 3);
            installmentsText = Row(card, "Cuotas al mes", 4);

            UiKit.Label(card, "Ánimo", 26, GameUiColors.Muted, new Vector2(200f, 36f), topLeft, new Vector2(22f, -226f));
            UiKit.Fill(card, "WellbeingBack", new Color(0.82f, 0.84f, 0.9f, 1f), new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -231f));
            wellbeingFill = UiKit.Fill(card, "WellbeingFill", GameUiColors.Gain, new Vector2(BarWidth, 26f), topLeft, new Vector2(250f, -231f));
        }

        private Text Row(RectTransform card, string label, int index)
        {
            float y = -(14f + 44f + index * 40f) + 36f;
            UiKit.Label(card, label, 26, GameUiColors.Muted, new Vector2(220f, 36f), new Vector2(0f, 1f), new Vector2(22f, y));
            return UiKit.Label(card, string.Empty, 30, UiKit.Ink, new Vector2(220f, 36f), new Vector2(1f, 1f), new Vector2(-22f, y), TextAnchor.MiddleRight);
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
            UiKit.Label(card, "El game master está pensando...", 34, UiKit.Ink, new Vector2(580f, 80f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            loadingCard.SetActive(false);
        }

        // ---------------------------------------------------------------- stations

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
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
            bool open = modal.IsOpen;
            PlayerController.Blocked = open || session.Busy;
            loadingCard.SetActive(session.Busy && !open);

            if (open)
            {
                promptCard.SetActive(false);
                for (int i = 0; i < 4; i++)
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

            var station = Nearby;
            string text = station != null && !session.Busy ? PromptFor(station) : null;
            promptCard.SetActive(text != null);
            if (text != null)
            {
                promptText.text = text;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact(station);
                }
            }
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
                default:
                    if (loop.CanDoTask(station.Type))
                    {
                        return "[E]  " + station.DisplayName;
                    }

                    return loop.AllTasksDone ? "Ya hiciste tus tareas del mes. Ve al banco a cobrar." : "Ya pasaste por " + station.DisplayName + " este mes.";
            }
        }

        private void Interact(Station station)
        {
            switch (station.Type)
            {
                case StationType.Bank:
                    AskCloseMonth();
                    break;
                case StationType.Home:
                    ShowStatement();
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
            modal.Hide();
        }

        private void Refresh()
        {
            var state = session.State;
            var loop = session.Loop;
            monthText.text = $"Mes {Mathf.Min(loop.MonthsPlayed + 1, GameRules.MonthsToPlay)} de {GameRules.MonthsToPlay}";
            tasksText.text = $"Tareas {loop.TasksDone}/{GameRules.TasksPerMonth}";
            cashText.text = Money.Format(state.Cash);
            savingsText.text = Money.Format(state.Savings + state.EmergencyFund);
            cardText.text = Money.Format(state.CreditCard.Balance);
            installmentsText.text = Money.Format(GameRules.MonthlyInstallments(state));

            float wellbeing = Mathf.Clamp01(state.Wellbeing / 100f);
            wellbeingFill.rectTransform.sizeDelta = new Vector2(BarWidth * wellbeing, 26f);
            wellbeingFill.color = wellbeing > 0.5f ? GameUiColors.Gain : (wellbeing > 0.25f ? new Color(0.95f, 0.65f, 0.15f, 1f) : GameUiColors.Loss);
        }

        // ---------------------------------------------------------------- windows

        private void ShowEvent(GameEvent gameEvent, ResultSource source)
        {
            var options = new List<ModalOption>();
            for (int i = 0; i < gameEvent.Choices.Count; i++)
            {
                var choice = gameEvent.Choices[i];
                string id = choice.Id;
                options.Add(new ModalOption { Label = $"{i + 1}.  {choice.Label}", Color = GameUiColors.Cycle(i), OnPress = () => session.Choose(id) });
            }

            string who = gameEvent.Npc != null ? gameEvent.Npc.Name + "  ·  " + gameEvent.Npc.Role : string.Empty;
            if (source == ResultSource.Fallback)
            {
                who = (who.Length > 0 ? who + "  ·  " : string.Empty) + "evento local";
            }

            modal.Show(gameEvent.Title, who, gameEvent.Narrative, options);
        }

        private void ShowOutcome(ChoiceOutcome outcome)
        {
            var text = new StringBuilder();
            Line(text, "Efectivo", outcome.CashDelta, true);
            Line(text, "Ahorros", outcome.SavingsDelta, true);
            Line(text, "Fondo de emergencia", outcome.EmergencyFundDelta, true);
            Line(text, "Tarjeta", outcome.CardDelta, false);
            Line(text, "Cuotas al mes", outcome.InstallmentsPerMonthDelta, false);
            Line(text, "Deuda con prestamista", outcome.InformalDebtDelta, false);
            if (outcome.WellbeingDelta != 0)
            {
                text.AppendLine($"Ánimo  <color={GameUiColors.Hex(outcome.WellbeingDelta > 0 ? GameUiColors.Gain : GameUiColors.Loss)}>{(outcome.WellbeingDelta > 0 ? "+" : string.Empty)}{outcome.WellbeingDelta}</color>");
            }

            if (outcome.ChangedNothing)
            {
                text.AppendLine("Tus cuentas quedaron igual.");
            }

            text.AppendLine();
            text.Append(session.Loop.AllTasksDone
                ? "Hiciste todas tus tareas. Ve al banco a cobrar el salario."
                : $"Tareas del mes: {session.Loop.TasksDone} de {GameRules.TasksPerMonth}.");

            modal.Show("Lo que pasó", outcome.ChoiceLabel, text.ToString(), new[] { Continue(null) });
        }

        private static void Line(StringBuilder text, string label, long delta, bool moreIsGood)
        {
            if (delta == 0L)
            {
                return;
            }

            bool good = (delta > 0L) == moreIsGood;
            text.AppendLine($"{label}  <color={GameUiColors.Hex(good ? GameUiColors.Gain : GameUiColors.Loss)}>{Money.FormatSigned(delta)}</color>");
        }

        private void AskCloseMonth()
        {
            string body = session.Loop.AllTasksDone
                ? "Cobras tu salario y se pagan las cuotas, la tarjeta y los gastos del mes."
                : $"Todavía te faltan tareas este mes ({session.Loop.TasksDone} de {GameRules.TasksPerMonth}). Si cierras ahora, te pierdes esas decisiones.";

            modal.Show("Banco", "Cierre de mes", body, new[]
            {
                new ModalOption { Label = "1.  Cobrar y cerrar el mes", Color = GameUiColors.Green, OnPress = () => session.CloseMonth() },
                new ModalOption { Label = "2.  Todavía no", Color = GameUiColors.Pink, OnPress = null }
            });
        }

        private void ShowMonthReport(MonthlyTickReport report)
        {
            var text = new StringBuilder();
            text.AppendLine($"Salario  <color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.SalaryCredited)}</color>");
            if (report.AguinaldoCredited > 0L)
            {
                text.AppendLine($"Aguinaldo  <color={GameUiColors.Hex(GameUiColors.Gain)}>{Money.FormatSigned(report.AguinaldoCredited)}</color>");
            }

            text.AppendLine($"Gastos de vida  <color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(GameRules.LivingExpenses)}</color>");
            if (report.InstallmentsCharged > 0L)
            {
                text.AppendLine($"Cuotas  <color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(report.InstallmentsCharged)}</color>");
            }

            var card = report.CardCycle;
            if (card.StartingBalance > 0L)
            {
                text.AppendLine($"Pago mínimo de la tarjeta  <color={GameUiColors.Hex(GameUiColors.Loss)}>-{Money.Format(card.AmountPaid)}</color>");
                text.AppendLine($"Intereses de la tarjeta  <color={GameUiColors.Hex(GameUiColors.Loss)}>+{Money.Format(card.InterestCharged)}</color> de deuda");
            }

            if (report.InformalLoanInterestAccrued > 0L)
            {
                text.AppendLine($"Intereses del prestamista  <color={GameUiColors.Hex(GameUiColors.Loss)}>+{Money.Format(report.InformalLoanInterestAccrued)}</color> de deuda");
            }

            text.AppendLine();
            text.Append($"Te queda {Money.Format(report.EndingCash)} en efectivo y la tarjeta debe {Money.Format(report.EndingCardBalance)}.");

            bool last = session.Loop.IsFinished;
            modal.Show($"Mes {report.MonthProcessed} cerrado", "Estado de cuenta del mes", text.ToString(), new[]
            {
                new ModalOption { Label = last ? "Ver mi resumen" : "Seguir", Color = GameUiColors.Yellow, OnPress = last ? (System.Action)(() => session.FinishRun()) : null }
            });
        }

        private void ShowStatement()
        {
            var state = session.State;
            var text = new StringBuilder();
            text.AppendLine($"Efectivo  {Money.Format(state.Cash)}");
            text.AppendLine($"Ahorros  {Money.Format(state.Savings)}");
            text.AppendLine($"Fondo de emergencia  {Money.Format(state.EmergencyFund)}");
            text.AppendLine($"Tarjeta  {Money.Format(state.CreditCard.Balance)} de {Money.Format(state.CreditCard.Limit)}  ({state.CreditCard.MonthlyRatePct}% al mes)");
            foreach (var installment in state.Installments)
            {
                if (installment.MonthsLeft > 0)
                {
                    text.AppendLine($"{installment.Item}  {Money.Format(installment.Monthly)} al mes, faltan {installment.MonthsLeft} cuotas");
                }
            }

            foreach (var loan in state.InformalLoans)
            {
                text.AppendLine($"{loan.Lender}  {Money.Format(loan.Balance)}  ({loan.MonthlyRatePct}% al mes)");
            }

            text.AppendLine();
            text.Append($"Lo que tienes menos lo que debes: {Money.Format(GameRules.NetWorth(state))}");
            modal.Show("Tu estado de cuenta", $"Salario neto {Money.Format(state.SalaryNet)} al mes", text.ToString(), new[] { Continue(null) });
        }

        private void ShowEnd(SessionSummary summary)
        {
            var text = new StringBuilder();
            text.AppendLine(summary.Narrative);
            text.AppendLine();
            text.AppendLine($"Decisiones tomadas: {summary.Decisions}");
            text.AppendLine($"Lo que debes: {Money.Format(summary.TotalDebt)}");
            text.Append($"Lo que tienes menos lo que debes: {Money.Format(summary.NetWorth)}");

            modal.Show("Fin de la partida", $"{summary.MonthsPlayed} meses jugados", text.ToString(), new[]
            {
                new ModalOption { Label = "Jugar de nuevo", Color = GameUiColors.Green, OnPress = Restart }
            });
        }

        private void Restart()
        {
            session.StartNewGame();
            SceneManager.LoadScene(SceneNames.Town);
        }

        private static ModalOption Continue(System.Action then)
        {
            return new ModalOption { Label = "Continuar", Color = GameUiColors.Yellow, OnPress = then };
        }
    }
}

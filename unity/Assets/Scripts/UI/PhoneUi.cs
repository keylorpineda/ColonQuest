using System;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// A mobile phone on screen, opened with P: a home screen with apps (SINPE, bank, search, a loan
    /// calculator, a block game and a casino). Apps only show or ask; every colon that moves goes through
    /// <see cref="GameSession"/> and the finance engine.
    /// </summary>
    public sealed class PhoneUi
    {
        private enum App
        {
            Home,
            Sinpe,
            Bank,
            Search,
            Calculator,
            Blocks,
            Casino
        }

        private sealed class Contact
        {
            public string Name;
            public string Number;
            public string Message;
            public bool Scam;
            public string Warning;
        }

        private const float PhoneWidth = 520f;
        private const float PhoneHeight = 960f;
        private const float ScreenWidth = PhoneWidth - 56f;
        private const float ScreenHeight = PhoneHeight - 80f;
        private const float StatusHeight = 52f;
        private const float BottomHeight = 84f;
        private const float Margin = 20f;
        private const float InnerWidth = ScreenWidth - Margin * 2f;
        private const float CellSize = 28f;

        private static readonly Color Night = new Color(0.14f, 0.16f, 0.26f, 1f);
        private static readonly Color Screen = new Color(0.95f, 0.96f, 1f, 1f);
        private static readonly Color Violet = new Color(0.78f, 0.7f, 1f, 1f);
        private static readonly Color Red = new Color(1f, 0.55f, 0.55f, 1f);

        private static readonly Contact[] Contacts =
        {
            new Contact { Name = "Mamá", Number = "8845-1203", Message = "Mae, ¿me ayudás con parte de la luz de este mes?" },
            new Contact { Name = "Don Rafa (taller)", Number = "8712-5530", Message = "Te quedó listo el arreglo. Si querés dejame un adelanto." },
            new Contact
            {
                Name = "Número desconocido", Number = "6034-9987", Scam = true,
                Message = "Hola, me equivoqué y te mandé plata. Devolvémela YA por SINPE, es urgente!!",
                Warning = "Nadie te mandó plata y no hay ninguna persona con ese nombre en tus contactos. Si enviás, no se devuelve."
            }
        };

        private static readonly long[] SinpeAmounts = { 5_000L, 10_000L, 25_000L, 50_000L };
        private static readonly long[] CasinoStakes = { 1_000L, 5_000L, 10_000L, 25_000L };
        private static readonly string[] Symbols = { "7", "$", "A", "K", "Q" };

        private static readonly Color[] BlockColors =
        {
            new Color(0.35f, 0.85f, 0.95f), new Color(1f, 0.85f, 0.25f), new Color(0.75f, 0.45f, 0.95f),
            new Color(0.4f, 0.9f, 0.45f), new Color(0.95f, 0.4f, 0.4f), new Color(0.35f, 0.5f, 0.95f), new Color(1f, 0.6f, 0.25f)
        };

        private readonly GameSession session;
        private readonly GameObject root;
        private readonly RectTransform content;
        private readonly Text clock;

        private App app = App.Home;
        private int openings;
        private Text cashLabel;

        // SINPE
        private int sinpeContact = -1;
        private int sinpeAmount = 1;
        private bool sinpeChecked;
        private string sinpeResult = string.Empty;
        private Color sinpeResultColor = UiKit.Ink;

        // Search
        private InputField searchInput;
        private Text searchResult;

        // Calculator
        private long calcPrice = 500_000L;
        private int calcMonths = 24;
        private decimal calcRate = 2m;
        private Text calcValues;

        // Blocks
        private TetrisBoard board;
        private Image[,] cells;
        private Text blocksInfo;
        private GameObject blocksOver;
        private float stepClock;

        // Casino
        private int stakeIndex = 1;
        private Text[] slots;
        private Text casinoMessage;
        private Text casinoTotal;
        private float spinUntil;
        private float spinClock;
        private CasinoResult spinResult;
        private bool spinning;

        public PhoneUi(Transform canvas, GameSession gameSession)
        {
            session = gameSession;
            root = new GameObject("Phone", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var dim = root.AddComponent<Image>();
            dim.color = UiKit.Dim;

            var body = UiKit.Card(root.transform, "Frame", Night, new Vector2(PhoneWidth, PhoneHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var notch = UiKit.Fill(body, "Notch", Color.black, new Vector2(130f, 16f), new Vector2(0.5f, 1f), new Vector2(0f, -12f));
            notch.raycastTarget = false;

            var screen = UiKit.Fill(body, "Screen", Screen, new Vector2(ScreenWidth, ScreenHeight), new Vector2(0.5f, 0f), new Vector2(0f, 22f));
            clock = UiKit.Label(screen.transform, string.Empty, 26, GameUiColors.Muted, new Vector2(ScreenWidth - 40f, 40f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), TextAnchor.MiddleLeft);
            content = UiKit.Rect(screen.transform, "Content", new Vector2(ScreenWidth, ScreenHeight - StatusHeight - BottomHeight), new Vector2(0f, 1f), new Vector2(0f, -StatusHeight));

            root.SetActive(false);
            session.Changed += OnSessionChanged;
        }

        public bool IsOpen => root.activeSelf;

        /// <summary>True while a text box has the keyboard, so letters like P do not close the phone.</summary>
        public bool IsTyping => searchInput != null && searchInput.isFocused;

        public void Open()
        {
            app = App.Home;
            root.SetActive(true);
            Show(App.Home);
            openings++;
        }

        public void Close()
        {
            root.SetActive(false);
            spinning = false;
        }

        /// <summary>Escape: back to the home screen from an app, or closes the phone from the home screen.</summary>
        public void Back()
        {
            if (app == App.Home)
            {
                Close();
            }
            else
            {
                Show(App.Home);
            }
        }

        private void OnSessionChanged()
        {
            if (IsOpen && cashLabel != null)
            {
                cashLabel.text = "Efectivo  " + Money.Format(session.State.Cash);
            }
        }

        // ---------------------------------------------------------------- frame

        public void Tick(float dt)
        {
            if (!IsOpen)
            {
                return;
            }

            clock.text = $"Mes {Mathf.Min(session.Loop.MonthsPlayed + 1, GameRules.MonthsToPlay)}  ·  {session.Loop.TasksDone}/{GameRules.TasksPerMonth} tareas";

            if (app == App.Blocks)
            {
                TickBlocks(dt);
            }
            else if (app == App.Casino && spinning)
            {
                TickCasino(dt);
            }
            else if (app == App.Search && searchInput != null && searchInput.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                RunSearch();
            }
        }

        private void Show(App next)
        {
            app = next;
            spinning = false;
            cashLabel = null;
            searchInput = null;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            switch (next)
            {
                case App.Sinpe: BuildSinpe(); break;
                case App.Bank: BuildBank(); break;
                case App.Search: BuildSearch(); break;
                case App.Calculator: BuildCalculator(); break;
                case App.Blocks: BuildBlocks(); break;
                case App.Casino: BuildCasino(); break;
                default: BuildHome(); break;
            }

            string label = next == App.Home ? "Cerrar celular  (P)" : "Inicio  (Esc)";
            AddButton(label, next == App.Home ? Red : GameUiColors.Yellow, new Vector2(InnerWidth, 64f), new Vector2(0f, -(ScreenHeight - StatusHeight - 74f)), Back);
        }

        // ---------------------------------------------------------------- helpers

        private Text Label(string text, int size, Color color, float width, Vector2 position, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var label = UiKit.Label(content, text, size, color, new Vector2(width, size * 1.4f), new Vector2(0f, 1f), new Vector2(Margin + position.x, position.y), anchor);
            label.fontStyle = FontStyle.Normal;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Text Heading(string text)
        {
            var label = UiKit.Label(content, text, 38, UiKit.Ink, new Vector2(InnerWidth, 50f), new Vector2(0f, 1f), new Vector2(Margin, -4f), TextAnchor.MiddleLeft);
            return label;
        }

        private Button AddButton(string text, Color color, Vector2 size, Vector2 position, Action click)
        {
            var button = UiKit.MakeButton(content, text, color, size, new Vector2(0f, 1f), new Vector2(Margin + position.x, position.y), click);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = size.y > 60f ? 28 : 24;
            return button;
        }

        private Text CashLine(float y)
        {
            cashLabel = Label("Efectivo  " + Money.Format(session.State.Cash), 30, GameUiColors.Muted, InnerWidth, new Vector2(0f, y));
            return cashLabel;
        }

        // ---------------------------------------------------------------- home

        private void BuildHome()
        {
            Heading("Mi celular");

            var notice = UiKit.Card(content, "Notice", Violet, new Vector2(InnerWidth, 150f), new Vector2(0f, 1f), new Vector2(Margin, -64f));
            UiKit.Label(notice, PhoneMessages.SinpeNotice(openings), 24, UiKit.Ink, new Vector2(InnerWidth - 36f, 130f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft).fontStyle = FontStyle.Normal;

            var tiles = new[]
            {
                (App.Sinpe, "S", "SINPE", GameUiColors.Blue),
                (App.Bank, "B", "Banco", GameUiColors.Green),
                (App.Search, "?", "Buscador", GameUiColors.Yellow),
                (App.Calculator, "=", "Cuotas", GameUiColors.Pink),
                (App.Blocks, "T", "Bloques", Violet),
                (App.Casino, "$", "Casino", Red)
            };

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                float x = (i % 3) * (InnerWidth - 130f) / 2f;
                float y = -250f - (i / 3) * 190f;
                Tile(tile.Item2, tile.Item3, tile.Item4, new Vector2(x, y), () => Show(tile.Item1));
            }

            var sound = AudioDirector.Muted ? "Sonido: no" : "Sonido: sí";
            AddButton(sound, Color.white, new Vector2(InnerWidth, 56f), new Vector2(0f, -640f), () =>
            {
                AudioDirector.Muted = !AudioDirector.Muted;
                Show(App.Home);
            });
        }

        private void Tile(string glyph, string name, Color color, Vector2 position, Action click)
        {
            var outline = UiKit.Fill(content, "Tile_" + name, UiKit.Ink, new Vector2(130f, 130f), new Vector2(0f, 1f), new Vector2(Margin + position.x, position.y));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", color, new Vector2(130f - UiKit.Outline * 2f, 130f - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
            body.raycastTarget = false;
            var button = outline.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = body;
            button.onClick.AddListener(() => click());
            UiKit.Label(body.transform, glyph, 72, UiKit.Ink, new Vector2(110f, 110f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            UiKit.Label(content, name, 26, UiKit.Ink, new Vector2(150f, 34f), new Vector2(0f, 1f), new Vector2(Margin + position.x - 10f, position.y - 136f), TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- sinpe

        private void BuildSinpe()
        {
            Heading("SINPE Móvil");
            CashLine(-54f);

            for (int i = 0; i < Contacts.Length; i++)
            {
                int index = i;
                var color = index == sinpeContact ? GameUiColors.Yellow : Color.white;
                AddButton(Contacts[i].Name + "   " + Contacts[i].Number, color, new Vector2(InnerWidth, 64f), new Vector2(0f, -96f - i * 72f), () =>
                {
                    sinpeContact = index;
                    sinpeChecked = false;
                    sinpeResult = string.Empty;
                    Show(App.Sinpe);
                });
            }

            if (sinpeContact < 0)
            {
                Label("Elegí un contacto para enviarle plata. Revisá bien el nombre antes de mandar.", 26, GameUiColors.Muted, InnerWidth, new Vector2(0f, -330f));
                return;
            }

            var contact = Contacts[sinpeContact];
            Label("«" + contact.Message + "»", 26, UiKit.Ink, InnerWidth, new Vector2(0f, -320f));

            if (contact.Scam)
            {
                AddButton(sinpeChecked ? "Verificado" : "Verificar este número", GameUiColors.Pink, new Vector2(InnerWidth, 52f), new Vector2(0f, -410f), () =>
                {
                    sinpeChecked = true;
                    Show(App.Sinpe);
                });
                if (sinpeChecked)
                {
                    Label(contact.Warning, 24, GameUiColors.Loss, InnerWidth, new Vector2(0f, -468f));
                }
            }

            float gap = (InnerWidth - 4f * 96f) / 3f;
            for (int i = 0; i < SinpeAmounts.Length; i++)
            {
                int index = i;
                var color = index == sinpeAmount ? GameUiColors.Green : Color.white;
                AddButton(Money.Format(SinpeAmounts[i]).Replace("₡", string.Empty), color, new Vector2(96f, 56f), new Vector2(i * (96f + gap), -560f), () =>
                {
                    sinpeAmount = index;
                    Show(App.Sinpe);
                });
            }

            AddButton("Enviar " + Money.Format(SinpeAmounts[sinpeAmount]), GameUiColors.Blue, new Vector2(InnerWidth, 64f), new Vector2(0f, -626f), SendSinpe);
            if (sinpeResult.Length > 0)
            {
                Label(sinpeResult, 24, sinpeResultColor, InnerWidth, new Vector2(0f, -700f));
            }
        }

        private void SendSinpe()
        {
            var contact = Contacts[sinpeContact];
            long amount = SinpeAmounts[sinpeAmount];
            if (!session.SendSinpe(amount))
            {
                sinpeResult = "No te alcanza el efectivo para enviar eso.";
                sinpeResultColor = GameUiColors.Loss;
            }
            else if (contact.Scam)
            {
                sinpeResult = "Era una estafa. Enviaste " + Money.Format(amount) + " y ese dinero no vuelve.";
                sinpeResultColor = GameUiColors.Loss;
            }
            else
            {
                sinpeResult = "Enviaste " + Money.Format(amount) + " a " + contact.Name + ".";
                sinpeResultColor = GameUiColors.Gain;
            }

            Show(App.Sinpe);
        }

        // ---------------------------------------------------------------- bank

        private void BuildBank()
        {
            Heading("Banco");
            var state = session.State;
            var text = new StringBuilder();
            text.AppendLine($"Efectivo  {Money.Format(state.Cash)}");
            text.AppendLine($"Ahorros  {Money.Format(state.Savings)}");
            text.AppendLine($"Fondo de emergencia  {Money.Format(state.EmergencyFund)}");
            text.AppendLine();
            text.AppendLine($"Tarjeta  {Money.Format(state.CreditCard.Balance)} de {Money.Format(state.CreditCard.Limit)}");
            text.AppendLine($"({state.CreditCard.MonthlyRatePct}% de interés al mes)");
            foreach (var installment in state.Installments)
            {
                if (installment.MonthsLeft > 0)
                {
                    text.AppendLine($"{installment.Item}: {Money.Format(installment.Monthly)}, faltan {installment.MonthsLeft}");
                }
            }

            foreach (var loan in state.InformalLoans)
            {
                text.AppendLine($"{loan.Lender}: {Money.Format(loan.Balance)}");
            }

            text.AppendLine();
            text.AppendLine($"SINPE enviado en esta partida: {Money.Format(session.SinpeSent)}");
            text.Append($"Lo que tenés menos lo que debés: {Money.Format(GameRules.NetWorth(state))}");
            Label(text.ToString(), 26, UiKit.Ink, InnerWidth, new Vector2(0f, -64f));
        }

        // ---------------------------------------------------------------- search

        private void BuildSearch()
        {
            Heading("Buscador");

            var outline = UiKit.Fill(content, "SearchBox", UiKit.Ink, new Vector2(InnerWidth, 64f), new Vector2(0f, 1f), new Vector2(Margin, -64f));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", Color.white, new Vector2(InnerWidth - UiKit.Outline * 2f, 64f - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var text = UiKit.Label(body.transform, string.Empty, 28, UiKit.Ink, new Vector2(InnerWidth - 40f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.fontStyle = FontStyle.Normal;
            var hint = UiKit.Label(body.transform, "Preguntá algo de plata...", 26, new Color(0.5f, 0.52f, 0.6f, 1f), new Vector2(InnerWidth - 40f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
            hint.fontStyle = FontStyle.Italic;

            searchInput = outline.gameObject.AddComponent<InputField>();
            searchInput.targetGraphic = outline;
            searchInput.textComponent = text;
            searchInput.placeholder = hint;
            searchInput.characterLimit = 80;
            searchInput.lineType = InputField.LineType.SingleLine;

            AddButton("Buscar", GameUiColors.Blue, new Vector2(InnerWidth, 56f), new Vector2(0f, -140f), RunSearch);
            searchResult = Label("Probá con: tarjeta, cuotas, sinpe, estafa, ahorro, casino, café, casa.", 26, GameUiColors.Muted, InnerWidth, new Vector2(0f, -214f));
        }

        private void RunSearch()
        {
            if (searchInput == null || searchResult == null)
            {
                return;
            }

            var entry = FinanceGlossary.Search(searchInput.text);
            searchResult.text = entry == null
                ? "No encontré eso. Probá con: tarjeta, cuotas, sinpe, estafa, ahorro, casino, café, casa."
                : "<b>" + entry.Title + "</b>\n\n" + entry.Text;
            searchResult.color = UiKit.Ink;
            searchResult.supportRichText = true;
        }

        // ---------------------------------------------------------------- calculator

        private void BuildCalculator()
        {
            Heading("Calculadora de cuotas");
            Label("Mirá cuánto pagás de verdad por comprar a cuotas.", 24, GameUiColors.Muted, InnerWidth, new Vector2(0f, -56f));

            Stepper("Precio", -110f, () => calcPrice = Math.Max(50_000L, calcPrice - 50_000L), () => calcPrice = Math.Min(20_000_000L, calcPrice + 50_000L));
            Stepper("Plazo (meses)", -190f, () => calcMonths = Math.Max(1, calcMonths - 3), () => calcMonths = Math.Min(120, calcMonths + 3));
            Stepper("Interés al mes (%)", -270f, () => calcRate = Math.Max(0m, calcRate - 0.5m), () => calcRate = Math.Min(10m, calcRate + 0.5m));

            calcValues = Label(string.Empty, 28, UiKit.Ink, InnerWidth, new Vector2(0f, -360f));
            RefreshCalculator();
        }

        private void Stepper(string name, float y, Action less, Action more)
        {
            Label(name, 24, GameUiColors.Muted, InnerWidth, new Vector2(0f, y));
            AddButton("-", Color.white, new Vector2(70f, 52f), new Vector2(InnerWidth - 160f, y - 4f), () => { less(); RefreshCalculator(); });
            AddButton("+", Color.white, new Vector2(70f, 52f), new Vector2(InnerWidth - 80f, y - 4f), () => { more(); RefreshCalculator(); });
        }

        private void RefreshCalculator()
        {
            if (calcValues == null)
            {
                return;
            }

            long payment = LoanMath.MonthlyPayment(calcPrice, calcRate, calcMonths);
            long total = payment * calcMonths;
            long extra = total - calcPrice;
            var text = new StringBuilder();
            text.AppendLine($"Precio  {Money.Format(calcPrice)}");
            text.AppendLine($"Plazo  {calcMonths} meses");
            text.AppendLine($"Interés  {calcRate}% al mes");
            text.AppendLine();
            text.AppendLine($"Cuota mensual  <b>{Money.Format(payment)}</b>");
            text.AppendLine($"Pagás en total  <b>{Money.Format(total)}</b>");
            text.Append($"De más  <color={GameUiColors.Hex(extra > 0 ? GameUiColors.Loss : GameUiColors.Gain)}><b>{Money.Format(extra)}</b></color>");
            calcValues.supportRichText = true;
            calcValues.text = text.ToString();
        }

        // ---------------------------------------------------------------- blocks

        private void BuildBlocks()
        {
            Heading("Bloques");
            board = new TetrisBoard(Environment.TickCount);
            stepClock = 0f;
            cells = new Image[TetrisBoard.Width, TetrisBoard.Height];

            float boardWidth = TetrisBoard.Width * CellSize;
            float left = (ScreenWidth - boardWidth) / 2f;
            var frame = UiKit.Fill(content, "BoardFrame", UiKit.Ink, new Vector2(boardWidth + 8f, TetrisBoard.Height * CellSize + 8f), new Vector2(0f, 1f), new Vector2(left - 4f, -56f));
            frame.raycastTarget = false;
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                for (int y = 0; y < TetrisBoard.Height; y++)
                {
                    var cell = UiKit.Fill(content, "Cell", new Color(0.88f, 0.9f, 0.97f), new Vector2(CellSize - 2f, CellSize - 2f), new Vector2(0f, 1f), new Vector2(left + x * CellSize + 1f, -60f - y * CellSize));
                    cells[x, y] = cell;
                }
            }

            blocksInfo = Label(string.Empty, 24, GameUiColors.Muted, InnerWidth, new Vector2(0f, -(60f + TetrisBoard.Height * CellSize + 6f)));

            float buttonWidth = (InnerWidth - 3f * 8f) / 4f;
            float rowY = -(60f + TetrisBoard.Height * CellSize + 44f);
            AddButton("<", Color.white, new Vector2(buttonWidth, 56f), new Vector2(0f, rowY), () => board?.MoveSideways(-1));
            AddButton("Girar", GameUiColors.Yellow, new Vector2(buttonWidth, 56f), new Vector2(buttonWidth + 8f, rowY), () => board?.Rotate());
            AddButton(">", Color.white, new Vector2(buttonWidth, 56f), new Vector2((buttonWidth + 8f) * 2f, rowY), () => board?.MoveSideways(1));
            AddButton("Caer", GameUiColors.Green, new Vector2(buttonWidth, 56f), new Vector2((buttonWidth + 8f) * 3f, rowY), () => board?.HardDrop());

            blocksOver = new GameObject("Over", typeof(RectTransform));
            blocksOver.transform.SetParent(content, false);
            var overRect = (RectTransform)blocksOver.transform;
            overRect.anchorMin = new Vector2(0f, 1f);
            overRect.anchorMax = new Vector2(0f, 1f);
            overRect.pivot = new Vector2(0f, 1f);
            overRect.sizeDelta = Vector2.zero;
            overRect.anchoredPosition = Vector2.zero;
            var overCard = UiKit.Card(blocksOver.transform, "OverCard", Color.white, new Vector2(300f, 170f), new Vector2(0f, 1f), new Vector2(ScreenWidth / 2f - 150f, -200f));
            UiKit.Label(overCard, "Se acabó", 40, UiKit.Ink, new Vector2(280f, 60f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.MiddleCenter);
            UiKit.MakeButton(overCard, "Otra vez", GameUiColors.Green, new Vector2(240f, 64f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), () => Show(App.Blocks));
            blocksOver.SetActive(false);

            RenderBlocks();
        }

        private void TickBlocks(float dt)
        {
            if (board == null)
            {
                return;
            }

            if (!board.IsOver)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) board.MoveSideways(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) board.MoveSideways(1);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) board.Rotate();
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) board.Step();
                if (Input.GetKeyDown(KeyCode.Space)) board.HardDrop();

                stepClock += dt;
                float interval = Mathf.Max(0.12f, 0.6f - 0.04f * board.Lines);
                if (stepClock >= interval)
                {
                    stepClock = 0f;
                    board.Step();
                }
            }

            RenderBlocks();
        }

        private void RenderBlocks()
        {
            if (board == null || cells == null)
            {
                return;
            }

            var empty = new Color(0.88f, 0.9f, 0.97f);
            for (int x = 0; x < TetrisBoard.Width; x++)
            {
                for (int y = 0; y < TetrisBoard.Height; y++)
                {
                    int kind = board.Cells[x, y];
                    cells[x, y].color = kind == 0 ? empty : BlockColors[(kind - 1) % BlockColors.Length];
                }
            }

            if (!board.IsOver)
            {
                var piece = board.PieceCells;
                for (int i = 0; i < 4; i++)
                {
                    int x = piece[i * 2];
                    int y = piece[i * 2 + 1];
                    if (x >= 0 && x < TetrisBoard.Width && y >= 0 && y < TetrisBoard.Height)
                    {
                        cells[x, y].color = BlockColors[(board.PieceKind - 1) % BlockColors.Length];
                    }
                }
            }

            if (blocksInfo != null)
            {
                blocksInfo.text = $"Puntos {board.Score}   ·   Líneas {board.Lines}   ·   flechas, Espacio";
            }

            if (blocksOver != null)
            {
                blocksOver.SetActive(board.IsOver);
            }
        }

        // ---------------------------------------------------------------- casino

        private void BuildCasino()
        {
            Heading("Casino virtual");
            CashLine(-54f);

            slots = new Text[3];
            float slotSize = 112f;
            float gap = (InnerWidth - 3f * slotSize) / 2f;
            for (int i = 0; i < 3; i++)
            {
                var card = UiKit.Card(content, "Slot", Color.white, new Vector2(slotSize, slotSize), new Vector2(0f, 1f), new Vector2(Margin + i * (slotSize + gap), -100f));
                slots[i] = UiKit.Label(card, "?", 72, UiKit.Ink, new Vector2(slotSize - 10f, slotSize - 10f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            }

            Label("Apuesta", 24, GameUiColors.Muted, InnerWidth, new Vector2(0f, -240f));
            float stakeGap = (InnerWidth - 4f * 96f) / 3f;
            for (int i = 0; i < CasinoStakes.Length; i++)
            {
                int index = i;
                var color = index == stakeIndex ? GameUiColors.Yellow : Color.white;
                AddButton(Money.Format(CasinoStakes[i]).Replace("₡", string.Empty), color, new Vector2(96f, 56f), new Vector2(i * (96f + stakeGap), -276f), () =>
                {
                    stakeIndex = index;
                    Show(App.Casino);
                });
            }

            AddButton("Apostar " + Money.Format(CasinoStakes[stakeIndex]), Red, new Vector2(InnerWidth, 72f), new Vector2(0f, -348f), PlaceBet);
            casinoMessage = Label("Probá tu suerte.", 28, UiKit.Ink, InnerWidth, new Vector2(0f, -440f));
            casinoTotal = Label(string.Empty, 24, GameUiColors.Muted, InnerWidth, new Vector2(0f, -520f));
            Label("La casa siempre gana a largo plazo: los premios chicos te animan a seguir y las pérdidas llegan solas.", 22, GameUiColors.Muted, InnerWidth, new Vector2(0f, -590f));
            RefreshCasinoTotal();
        }

        private void PlaceBet()
        {
            if (spinning)
            {
                return;
            }

            if (!session.TryCasino(CasinoStakes[stakeIndex], out spinResult))
            {
                casinoMessage.text = "No te alcanza el efectivo para esa apuesta.";
                casinoMessage.color = GameUiColors.Loss;
                return;
            }

            spinning = true;
            spinClock = 0f;
            spinUntil = 0.9f;
            casinoMessage.text = "Girando...";
            casinoMessage.color = UiKit.Ink;
        }

        private void TickCasino(float dt)
        {
            spinClock += dt;
            if (spinClock < spinUntil)
            {
                // The reels flicker through the symbols while they spin.
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i].text = Symbols[(int) (spinClock * 18f + i * 2) % Symbols.Length];
                }

                return;
            }

            spinning = false;
            string[] finish;
            switch (spinResult.Outcome)
            {
                case CasinoOutcome.Jackpot:
                    finish = new[] { "7", "7", "7" };
                    break;
                case CasinoOutcome.SmallWin:
                    finish = new[] { "$", "$", "K" };
                    break;
                default:
                    finish = new[] { "A", "Q", "7" };
                    break;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].text = finish[i];
            }

            if (spinResult.Net > 0L)
            {
                casinoMessage.text = (spinResult.Outcome == CasinoOutcome.Jackpot ? "Premio grande: " : "Ganaste ") + Money.Format(spinResult.Net);
                casinoMessage.color = GameUiColors.Gain;
            }
            else
            {
                casinoMessage.text = "Perdiste " + Money.Format(-spinResult.Net);
                casinoMessage.color = GameUiColors.Loss;
            }

            RefreshCasinoTotal();
        }

        private void RefreshCasinoTotal()
        {
            if (casinoTotal == null)
            {
                return;
            }

            long net = session.CasinoNet;
            casinoTotal.text = net >= 0L
                ? "En esta partida vas ganando " + Money.Format(net)
                : "En esta partida llevás perdidos " + Money.Format(-net);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The window to haggle with the store seller. On the left the conversation, on the right what the player knows so far;
    /// under them the questions worth asking, one click each, or the player's own words by text or voice; and at the foot the
    /// decision. It is rebuilt whenever the conversation changes, so it holds no state of its own. What the player says shows
    /// at once, before the seller answers.
    /// </summary>
    public sealed class NegotiationPanel
    {
        private const float CardWidth = 1300f;
        private const float CardHeight = 900f;
        private const float Pad = 40f;
        private const float LogHeight = 470f;
        private const float LogWidth = 790f;
        private const float SheetWidth = 410f;
        private const float ColumnsWidth = LogWidth + SheetWidth + 20f;
        private const float RowHeight = 62f;
        private const float InputHeight = 64f;

        private const string MicNotice = "No pude usar el micrófono, escribilo.";
        private const string TypeHint = "O escribí tu propia pregunta...";
        private const string ListeningHint = "Te escucho. Al callarte se envía solo.";
        private const string Understanding = "Entendiendo lo que dijiste...";

        private readonly GameSession session;
        private readonly GameObject root;
        private readonly IVoiceRecorder recorder;
        private InputField input;
        private Text hint;
        private VoiceButton voiceButton;
        private Text waitLabel;
        private string waitText;
        private string micNotice;
        private string lastKey = string.Empty;
        private int lastKnownFeesCount;

        /// <summary>The questions on the buttons, in the order of <see cref="KeyQuestion"/>.</summary>
        private static readonly string[] Questions =
        {
            "¿Cuánto pago en total?", "¿Tiene cargos escondidos?", "¿Qué interés cobra?", "¿A cuántos meses es?", "¿Cuánto cuesta de contado?", "¿Incluye algún seguro?"
        };

        private static readonly string[] FactNames =
        {
            "Precio de contado", "Al mes, con todo", "Plazo", "Tasa de interés", "Seguro", "Comisión", "Costo total"
        };

        public NegotiationPanel(Transform canvas, GameSession gameSession, IVoiceRecorder voiceRecorder = null)
        {
            session = gameSession;
            recorder = voiceRecorder ?? VoiceRecorders.Create();
            root = new GameObject("Negotiation", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.AddComponent<Image>().color = UiKit.Dim;
            root.SetActive(false);
        }

        public bool IsOpen => root.activeSelf;

        /// <summary>
        /// Set by the game window: it plays the signing of the contract and then runs the action it is given, which closes the deal.
        /// Without it the deal closes at once.
        /// </summary>
        public System.Action<System.Action> SignContract;

        /// <summary>Keeps the window and its shade out of this much of the right of the screen.</summary>
        public void LeaveRoom(float right)
        {
            ((RectTransform)root.transform).offsetMax = new Vector2(-right, 0f);
        }

        public void Hide()
        {
            recorder.Cancel();
            micNotice = null;
            Clear();
            lastKey = string.Empty;
            lastKnownFeesCount = 0;
            root.SetActive(false);
        }

        /// <summary>Redraws the window if the conversation, the waiting state or the closing changed.</summary>
        public void Refresh()
        {
            var negotiation = session.CurrentNegotiation;
            if (negotiation == null)
            {
                if (IsOpen)
                {
                    Hide();
                }

                return;
            }

            string key = negotiation.Offer.OfferId + "|" + negotiation.Offer.TermMonths + "|" + negotiation.KeyQuestionsAsked.Count + "|" + negotiation.Turns.Count + "|" + session.Busy + "|" + session.Transcribing + "|" + session.PendingLine + "|" + negotiation.Status + "|" + Notice + "|" + session.DealNotice;
            if (IsOpen && key == lastKey)
            {
                return;
            }

            lastKey = key;
            Build(negotiation);
        }

        private void Accept()
        {
            if (SignContract != null)
            {
                SignContract(() => session.AcceptDeal());
            }
            else
            {
                session.AcceptDeal();
            }
        }

        /// <summary>Enter sends the typed line, like the send button. It also follows the recording.</summary>
        public void Tick()
        {
            if (input != null && input.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                Submit();
            }

            ChatView.AnimateWait(waitLabel, waitText);
            TickVoice();
        }

        /// <summary>What the text box says when voice did not work, or null.</summary>
        private string Notice => session.VoiceNotice ?? micNotice;

        private void TickVoice()
        {
            var state = recorder.State;
            if (state == VoiceRecorderState.Idle)
            {
                return;
            }

            recorder.Tick();
            state = recorder.State;
            if (state == VoiceRecorderState.Ready)
            {
                byte[] wav = recorder.TakeWav();
                if (wav != null && wav.Length > 0)
                {
                    session.SendNegotiationVoice(wav);
                }
                else
                {
                    ShowMicProblem();
                }
            }
            else if (state == VoiceRecorderState.Failed)
            {
                recorder.TakeWav();
                ShowMicProblem();
            }

            UpdateVoice();
        }

        /// <summary>Tap to start talking, tap again to send; the recorder also stops by itself when the player goes quiet.</summary>
        private void ToggleTalk()
        {
            if (session.Busy)
            {
                return;
            }

            switch (recorder.State)
            {
                case VoiceRecorderState.Recording:
                    recorder.Stop();
                    break;
                case VoiceRecorderState.Starting:
                    recorder.Cancel();
                    break;
                default:
                    micNotice = null;
                    recorder.Start();
                    break;
            }

            UpdateVoice();
        }

        private void UpdateVoice()
        {
            voiceButton?.Refresh();
            if (hint != null && Notice == null)
            {
                hint.text = Loc.T(recorder.State == VoiceRecorderState.Recording && recorder.StopsOnSilence ? ListeningHint : TypeHint);
            }
        }

        private void ShowMicProblem()
        {
            micNotice = MicNotice;
            Refresh();
        }

        private void Submit()
        {
            if (input == null || session.Busy)
            {
                return;
            }

            string text = input.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                session.SendNegotiation(text);
            }
        }

        private void Build(Negotiation negotiation)
        {
            int currentFeesCount = 0;
            foreach (var fact in negotiation.RevealedFacts)
            {
                if (fact == SellerFact.InsuranceFee || fact == SellerFact.Commission)
                {
                    currentFeesCount++;
                }
            }
            if (currentFeesCount > lastKnownFeesCount)
            {
                ReactionBubble.Show(ReactionKind.Alert);
            }
            lastKnownFeesCount = currentFeesCount;

            Clear();
            float textWidth = ColumnsWidth;
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, CardHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var top = new Vector2(0.5f, 1f);
            float y = -24f;

            UiKit.Portrait(card, negotiation.Offer.SellerName, 76f, top, new Vector2(-textWidth * 0.5f + 38f, y));
            var title = UiKit.Label(card, Loc.F("{0}  ·  {1}", Loc.T(negotiation.Venue), negotiation.Offer.SellerName), 36, UiKit.Ink, new Vector2(textWidth - 100f, 44f), top, new Vector2(50f, y), TextAnchor.MiddleLeft);
            title.font = UiKit.TitleFont;
            y -= 44f;
            UiKit.Label(card, SubtitleFor(negotiation), 24, GameUiColors.Muted, new Vector2(textWidth - 100f, 34f), top, new Vector2(50f, y), TextAnchor.MiddleLeft);
            y -= 46f;

            BuildConversation(card, negotiation, y);
            BuildSheet(card, negotiation, y);
            y -= LogHeight + 12f;

            if (negotiation.IsClosed)
            {
                string label = negotiation.Status == DealStatus.Accepted ? "Cerrar el trato" : "Continuar";
                UiKit.MakeButton(card, label, GameUiColors.Yellow, new Vector2(textWidth, 84f), top, new Vector2(0f, y - 60f), () => session.FinishNegotiation());
                root.SetActive(true);
                return;
            }

            BuildQuestions(card, negotiation, y);
            y -= 124f;
            BuildInput(card, textWidth, y);
            y -= InputHeight + 10f;

            float half = (textWidth - 16f) / 2f;
            float left = -(half + 16f) / 2f;
            bool free = !session.Busy;
            string buy = Knows(negotiation, SellerFact.TotalCost)
                ? Loc.F("Me lo llevo  ·  {0}", SellerOffer.FormatColones(negotiation.Offer.TotalCost))
                : Loc.T("Me lo llevo");
            UiKit.MakeButton(card, buy, GameUiColors.Green, new Vector2(half, InputHeight), top, new Vector2(left, y), () => Accept()).interactable = free;
            UiKit.MakeButton(card, "Mejor me voy", GameUiColors.Pink, new Vector2(half, InputHeight), top, new Vector2(-left, y), () => session.WalkAway()).interactable = free;

            if (!string.IsNullOrEmpty(session.DealNotice))
            {
                UiKit.Label(card, session.DealNotice, 22, GameUiColors.Loss, new Vector2(textWidth, 30f), top, new Vector2(0f, y - InputHeight), TextAnchor.MiddleCenter);
            }

            root.SetActive(true);
            if (input != null && !session.Busy)
            {
                input.ActivateInputField();
            }
        }

        /// <summary>True once the seller said it, or the player asked the question that makes the seller say it.</summary>
        private static bool Knows(Negotiation negotiation, SellerFact fact)
        {
            if (negotiation.RevealedFacts.Contains(fact))
            {
                return true;
            }

            var asked = negotiation.KeyQuestionsAsked;
            switch (fact)
            {
                case SellerFact.CashPrice: return asked.Contains(KeyQuestion.CashPrice);
                case SellerFact.TermMonths: return asked.Contains(KeyQuestion.Term);
                case SellerFact.InterestRate: return asked.Contains(KeyQuestion.InterestRate);
                case SellerFact.InsuranceFee: return asked.Contains(KeyQuestion.Insurance) || asked.Contains(KeyQuestion.HiddenFees);
                case SellerFact.Commission: return asked.Contains(KeyQuestion.HiddenFees);
                case SellerFact.TotalCost: return asked.Contains(KeyQuestion.TotalCost);
                default: return false;
            }
        }

        /// <summary>What the player knows so far, in figures that do not have to be fished out of the conversation.</summary>
        private static void BuildSheet(RectTransform card, Negotiation negotiation, float y)
        {
            var top = new Vector2(0.5f, 1f);
            float x = (ColumnsWidth - SheetWidth) / 2f;
            UiKit.Label(card, "Lo que ya sabés", 24, GameUiColors.Muted, new Vector2(SheetWidth, 30f), top, new Vector2(x, y), TextAnchor.MiddleLeft);
            float rowY = y - 34f;
            for (int i = 0; i < FactNames.Length; i++)
            {
                var fact = (SellerFact)i;
                bool known = Knows(negotiation, fact);
                var row = UiKit.Fill(card, "Fact_" + fact, known ? GameUiColors.Green : new Color(0.9f, 0.9f, 0.92f, 1f), new Vector2(SheetWidth, RowHeight - 6f), top, new Vector2(x, rowY));
                var name = UiKit.Label(row.transform, FactNames[i], 20, known ? UiKit.Ink : GameUiColors.Muted, new Vector2(SheetWidth * 0.5f, RowHeight - 6f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Normal;
                UiKit.PlainLabel(row.transform, "Value", known ? ValueOf(negotiation.Offer, fact) : "? ? ?", 22, known ? UiKit.Ink : GameUiColors.Muted, new Vector2(SheetWidth * 0.5f - 12f, RowHeight - 6f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), TextAnchor.MiddleRight);
                rowY -= RowHeight;
            }
        }

        /// <summary>The figure of a line. The monthly one is what really leaves the pocket, with the insurance, as the seller says it.</summary>
        private static string ValueOf(SellerOffer offer, SellerFact fact)
        {
            return fact == SellerFact.MonthlyPayment ? SellerOffer.FormatColones(offer.MonthlyCharge) : offer.Describe(fact);
        }

        /// <summary>The questions worth asking, one click each. Asked, they turn green and stay as a record.</summary>
        private void BuildQuestions(RectTransform card, Negotiation negotiation, float y)
        {
            const float gap = 10f;
            int perRow = Questions.Length / 2;
            float width = (ColumnsWidth - gap * (perRow - 1)) / perRow;
            for (int i = 0; i < Questions.Length; i++)
            {
                var question = (KeyQuestion)i;
                string spoken = Loc.T(Questions[i]);
                bool asked = negotiation.KeyQuestionsAsked.Contains(question);
                float x = -ColumnsWidth / 2f + width / 2f + (i % perRow) * (width + gap);
                var button = UiKit.MakeButton(card, Questions[i], asked ? GameUiColors.Green : Color.white, new Vector2(width, 54f), new Vector2(0.5f, 1f), new Vector2(x, y - (i / perRow) * 62f), () => session.AskKeyQuestion(question, spoken));
                button.name = "Chip_" + i;
                UiKit.SetFontSize(button, 23);
                button.interactable = !session.Busy;
                if (asked)
                {
                    UiKit.MarkDone(button);
                }
            }
        }

        /// <summary>The bubbles, ending in the line on its way and whoever is working on it.</summary>
        private void BuildConversation(RectTransform card, Negotiation negotiation, float y)
        {
            string seller = negotiation.Offer.SellerName;
            string player = Loc.T("Tú:").TrimEnd(':');
            var lines = new List<ChatLine> { new ChatLine(seller, negotiation.Opening, false) };
            foreach (var turn in negotiation.Turns)
            {
                lines.Add(new ChatLine(player, turn.Player, true));
                lines.Add(new ChatLine(seller, turn.Seller, false));
            }

            ChatLine? wait = null;
            if (session.Busy && !negotiation.IsClosed)
            {
                if (session.Transcribing)
                {
                    wait = new ChatLine(player, Loc.T(Understanding), true);
                }
                else
                {
                    if (!string.IsNullOrEmpty(session.PendingLine))
                    {
                        lines.Add(new ChatLine(player, session.PendingLine, true));
                    }

                    wait = new ChatLine(seller, Loc.F("{0} está pensando...", seller), false);
                }
            }

            waitText = wait.HasValue ? wait.Value.Text : null;
            waitLabel = ChatView.Build(card, new Vector2(LogWidth, LogHeight), new Vector2(0.5f, 1f), new Vector2(-(ColumnsWidth - LogWidth) / 2f, y), lines, wait);
        }

        /// <summary>The line under the title: what is offered and, at the car agency, the costs that come with the car.</summary>
        private string SubtitleFor(Negotiation negotiation)
        {
            var car = session.CurrentCar;
            if (car == null)
            {
                return Loc.F("Te ofrece un {0} en cuotas", Loc.T(negotiation.Offer.Item));
            }

            return Loc.F("Un {0}: prima {1}, marchamo {2}, gasolina {3} al mes (ejemplo)", Loc.T(negotiation.Offer.Item), Money.Format(car.DownPayment), Money.Format(car.MarchamoFirstYear), Money.Format(car.RunningCostsMonthly));
        }

        private void BuildInput(RectTransform card, float textWidth, float y)
        {
            var top = new Vector2(0.5f, 1f);
            bool voice = recorder.IsSupported;
            bool free = !session.Busy;
            float sendWidth = 160f;
            float talkWidth = voice ? 190f : 0f;
            float gap = 12f;
            float boxWidth = textWidth - sendWidth - gap - (voice ? talkWidth + gap : 0f);
            float boxX = -(textWidth - boxWidth) / 2f;
            float sendX = (textWidth - sendWidth) / 2f;
            float talkX = sendX - sendWidth / 2f - gap - talkWidth / 2f;

            var outline = UiKit.Fill(card, "InputBox", UiKit.Ink, new Vector2(boxWidth, InputHeight), top, new Vector2(boxX, y));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", Color.white, new Vector2(boxWidth - UiKit.Outline * 2f, InputHeight - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);

            var text = UiKit.Label(body.transform, string.Empty, 26, UiKit.Ink, new Vector2(boxWidth - 50f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.fontStyle = FontStyle.Normal;
            string notice = Notice;
            hint = UiKit.PlainLabel(body.transform, "Hint", Loc.T(notice ?? TypeHint), 25, notice != null ? GameUiColors.Loss : new Color(0.5f, 0.52f, 0.6f, 1f), new Vector2(boxWidth - 50f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero);
            hint.fontStyle = FontStyle.Italic;

            input = outline.gameObject.AddComponent<InputField>();
            input.targetGraphic = outline;
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = 300;
            input.lineType = InputField.LineType.SingleLine;
            input.interactable = free;

            if (voice)
            {
                var talk = UiKit.MakeButton(card, "Hablar", GameUiColors.Yellow, new Vector2(talkWidth, InputHeight), top, new Vector2(talkX, y), ToggleTalk);
                talk.interactable = free;
                voiceButton = new VoiceButton(talk, recorder, talkWidth);
            }

            UiKit.MakeButton(card, "Enviar", GameUiColors.Blue, new Vector2(sendWidth, InputHeight), top, new Vector2(sendX, y), Submit).interactable = free;
        }

        private void Clear()
        {
            input = null;
            hint = null;
            voiceButton = null;
            waitLabel = null;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }
}

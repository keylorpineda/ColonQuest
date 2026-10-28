using System.Linq;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The window to haggle with the store seller: the last lines of the conversation, the key
    /// questions the player has already asked, a text box and the buttons to buy or leave. It is
    /// rebuilt whenever the conversation changes, so it holds no state of its own.
    /// </summary>
    public sealed class NegotiationPanel
    {
        private const float CardWidth = 1300f;
        private const float CardHeight = 900f;
        private const float Pad = 40f;
        private const float LogHeight = 330f;

        private const string MicNotice = "No pude usar el micrófono, escribilo.";
        private const string TypeHint = "Escribe lo que le dices al vendedor...";

        private readonly GameSession session;
        private readonly GameObject root;
        private readonly IVoiceRecorder recorder;
        private InputField input;
        private Text talkLabel;
        private string micNotice;
        private string lastKey = string.Empty;

        private static readonly string[] QuestionLabels =
        {
            "Costo total", "Cargos escondidos", "Tasa de interés", "Plazo", "Precio de contado", "Seguro"
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

        public void Hide()
        {
            recorder.Cancel();
            micNotice = null;
            Clear();
            lastKey = string.Empty;
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

            string key = negotiation.Offer.OfferId + "|" + negotiation.Turns.Count + "|" + session.Busy + "|" + negotiation.Status + "|" + Notice + "|" + session.DealNotice;
            if (IsOpen && key == lastKey)
            {
                return;
            }

            lastKey = key;
            Build(negotiation);
        }

        /// <summary>Enter sends the typed line, like the send button. It also follows the recording.</summary>
        public void Tick()
        {
            if (input != null && input.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                Submit();
            }

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

            UpdateTalkLabel();
        }

        /// <summary>Tap to start talking, tap again to send; the recorder also stops by itself at the limit.</summary>
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

            UpdateTalkLabel();
        }

        private void UpdateTalkLabel()
        {
            if (talkLabel == null)
            {
                return;
            }

            switch (recorder.State)
            {
                case VoiceRecorderState.Starting:
                    talkLabel.text = "Abriendo micro...";
                    break;
                case VoiceRecorderState.Recording:
                    talkLabel.text = $"Enviar voz  {Mathf.FloorToInt(recorder.Seconds)}/{WavEncoder.MaxSeconds} s";
                    break;
                default:
                    talkLabel.text = "Hablar";
                    break;
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
            Clear();
            float textWidth = CardWidth - Pad * 2f - UiKit.Outline * 2f;
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, CardHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var top = new Vector2(0.5f, 1f);
            float y = -Pad + UiKit.Outline;

            UiKit.Portrait(card, negotiation.Offer.SellerName, 92f, top, new Vector2(textWidth * 0.5f - 46f, y - 4f));
            UiKit.Label(card, negotiation.Venue + "  ·  " + negotiation.Offer.SellerName, 46, UiKit.Ink, new Vector2(textWidth - 110f, 60f), top, new Vector2(-55f, y), TextAnchor.MiddleLeft);
            y -= 60f;
            UiKit.Label(card, SubtitleFor(negotiation), 24, GameUiColors.Muted, new Vector2(textWidth - 110f, 38f), top, new Vector2(-55f, y), TextAnchor.MiddleLeft);
            y -= 50f;

            var log = UiKit.Label(card, ConversationText(negotiation), 30, UiKit.Ink, new Vector2(textWidth, LogHeight), top, new Vector2(0f, y), TextAnchor.UpperLeft);
            log.supportRichText = true;
            log.verticalOverflow = VerticalWrapMode.Truncate;
            log.fontStyle = FontStyle.Normal;
            y -= LogHeight + 14f;

            UiKit.Label(card, "Preguntas clave que ya hiciste", 22, GameUiColors.Muted, new Vector2(textWidth, 30f), top, new Vector2(0f, y), TextAnchor.MiddleLeft);
            y -= 34f;
            BuildChips(card, negotiation, textWidth, y);
            y -= 70f;

            if (negotiation.IsClosed)
            {
                string label = negotiation.Status == DealStatus.Accepted ? "Cerrar el trato" : "Continuar";
                UiKit.MakeButton(card, label, GameUiColors.Yellow, new Vector2(textWidth, 84f), top, new Vector2(0f, y - 10f), () => session.FinishNegotiation());
                root.SetActive(true);
                return;
            }

            if (session.Busy)
            {
                UiKit.Label(card, negotiation.Offer.SellerName + " está pensando...", 32, GameUiColors.Muted, new Vector2(textWidth, 84f), top, new Vector2(0f, y - 10f), TextAnchor.MiddleCenter);
            }
            else
            {
                BuildInput(card, textWidth, y);
            }

            y -= 100f;
            float half = (textWidth - 20f) / 2f;
            float left = -(half + 20f) / 2f;
            UiKit.MakeButton(card, "Me lo llevo", GameUiColors.Green, new Vector2(half, 80f), top, new Vector2(left, y), () => session.AcceptDeal());
            UiKit.MakeButton(card, "Mejor me voy", GameUiColors.Pink, new Vector2(half, 80f), top, new Vector2(-left, y), () => session.WalkAway());

            if (!string.IsNullOrEmpty(session.DealNotice))
            {
                UiKit.Label(card, session.DealNotice, 26, GameUiColors.Loss, new Vector2(textWidth, 70f), top, new Vector2(0f, y - 88f), TextAnchor.UpperCenter);
            }

            root.SetActive(true);
            if (input != null)
            {
                input.ActivateInputField();
            }
        }

        /// <summary>The line under the title: what is offered and, at the car agency, the costs that come with the car.</summary>
        private string SubtitleFor(Negotiation negotiation)
        {
            var car = session.CurrentCar;
            if (car == null)
            {
                return "Te ofrece un " + negotiation.Offer.Item + " en cuotas";
            }

            return $"Un {negotiation.Offer.Item}: prima {Money.Format(car.DownPayment)}, marchamo {Money.Format(car.MarchamoFirstYear)}, gasolina {Money.Format(car.RunningCostsMonthly)} al mes (ejemplo)";
        }

        private void BuildChips(RectTransform card, Negotiation negotiation, float textWidth, float y)
        {
            const float gap = 10f;
            float width = (textWidth - gap * (QuestionLabels.Length - 1)) / QuestionLabels.Length;
            float x = -textWidth / 2f + width / 2f;
            for (int i = 0; i < QuestionLabels.Length; i++)
            {
                bool asked = negotiation.KeyQuestionsAsked.Contains((KeyQuestion)i);
                var chip = UiKit.Card(card, "Chip_" + i, asked ? GameUiColors.Green : new Color(0.88f, 0.88f, 0.9f, 1f), new Vector2(width, 62f), new Vector2(0.5f, 1f), new Vector2(x, y));
                UiKit.Label(chip, QuestionLabels[i], 20, asked ? UiKit.Ink : GameUiColors.Muted, new Vector2(width - 20f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
                x += width + gap;
            }
        }

        private void BuildInput(RectTransform card, float textWidth, float y)
        {
            var top = new Vector2(0.5f, 1f);
            bool voice = recorder.IsSupported;
            float sendWidth = 220f;
            float talkWidth = voice ? 300f : 0f;
            float gap = 16f;
            float boxWidth = textWidth - sendWidth - gap - (voice ? talkWidth + gap : 0f);
            float boxX = -(textWidth - boxWidth) / 2f;
            float sendX = (textWidth - sendWidth) / 2f;
            float talkX = sendX - sendWidth / 2f - gap - talkWidth / 2f;

            var outline = UiKit.Fill(card, "InputBox", UiKit.Ink, new Vector2(boxWidth, 84f), top, new Vector2(boxX, y - 10f));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", Color.white, new Vector2(boxWidth - UiKit.Outline * 2f, 84f - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);

            var text = UiKit.Label(body.transform, string.Empty, 32, UiKit.Ink, new Vector2(boxWidth - 60f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.fontStyle = FontStyle.Normal;
            string notice = Notice;
            var hint = UiKit.Label(body.transform, notice ?? TypeHint, 30, notice != null ? GameUiColors.Loss : new Color(0.5f, 0.52f, 0.6f, 1f), new Vector2(boxWidth - 60f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
            hint.fontStyle = FontStyle.Italic;

            input = outline.gameObject.AddComponent<InputField>();
            input.targetGraphic = outline;
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = 300;
            input.lineType = InputField.LineType.SingleLine;

            if (voice)
            {
                var talk = UiKit.MakeButton(card, "Hablar", GameUiColors.Yellow, new Vector2(talkWidth, 84f), top, new Vector2(talkX, y - 10f), ToggleTalk);
                talkLabel = talk.GetComponentInChildren<Text>();
                UpdateTalkLabel();
            }

            UiKit.MakeButton(card, "Enviar", GameUiColors.Blue, new Vector2(sendWidth, 84f), top, new Vector2(sendX, y - 10f), Submit);
        }

        /// <summary>The last lines that fit the window, newest at the bottom of the text.</summary>
        private string ConversationText(Negotiation negotiation)
        {
            string seller = negotiation.Offer.SellerName;
            var lines = new System.Collections.Generic.List<string> { $"<b>{seller}:</b> {negotiation.Opening}" };
            foreach (var turn in negotiation.Turns)
            {
                lines.Add($"<color={GameUiColors.Hex(GameUiColors.Muted)}><b>Tú:</b> {Escape(turn.Player)}</color>");
                lines.Add($"<b>{seller}:</b> {turn.Seller}");
            }

            // Keep the newest lines: drop the oldest ones until the text fits the log area.
            int skip = 0;
            while (lines.Count - skip > 2 && Estimate(lines, skip) > LogHeight)
            {
                skip++;
            }

            var text = new StringBuilder();
            for (int i = skip; i < lines.Count; i++)
            {
                if (text.Length > 0)
                {
                    text.Append("\n\n");
                }

                text.Append(lines[i]);
            }

            return text.ToString();
        }

        /// <summary>A cheap height guess (about 62 characters per line at this size) so no layout pass is needed.</summary>
        private static float Estimate(System.Collections.Generic.List<string> lines, int skip)
        {
            float height = 0f;
            for (int i = skip; i < lines.Count; i++)
            {
                int rows = Mathf.CeilToInt(Mathf.Max(1, lines[i].Length) / 62f);
                height += rows * 38f + 38f;
            }

            return height;
        }

        private static string Escape(string text)
        {
            return text.Replace("<", "‹").Replace(">", "›");
        }

        private void Clear()
        {
            input = null;
            talkLabel = null;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }
}

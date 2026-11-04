using System;
using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The window to talk with the collectors, the lender or the pyramid stand: questions worth asking, one click each, or the
    /// user's own words by text or voice. Under them, the ways out the simulator window gives (pay, accept, listen, leave).
    /// </summary>
    public sealed class TalkPanel
    {
        private const float CardWidth = 1300f;
        private const float CardHeight = 800f;
        private const float TextWidth = 1220f;
        private const float LogHeight = 370f;
        private const float InputHeight = 64f;

        private const string MicNotice = "No pude usar el micrófono, escribilo.";
        private const string TypeHint = "O escribí tu propia pregunta...";
        private const string Understanding = "Entendiendo lo que dijiste...";

        private readonly GameSession session;
        private readonly GameObject root;
        private readonly IVoiceRecorder recorder;
        private readonly Func<DebtTalk, IList<ModalOption>> actionsFor;
        private InputField input;
        private VoiceButton voiceButton;
        private Text waitLabel;
        private string waitText;
        private string micNotice;

        public TalkPanel(Transform canvas, GameSession gameSession, Func<DebtTalk, IList<ModalOption>> actions, IVoiceRecorder voiceRecorder = null)
        {
            session = gameSession;
            actionsFor = actions;
            recorder = voiceRecorder ?? VoiceRecorders.Create();
            root = UiKit.Overlay(canvas, "Talk");
            root.SetActive(false);
        }

        public bool IsOpen => root.activeSelf;

        public void Show(DebtTalk talk)
        {
            micNotice = null;
            Build(talk);
            root.SetActive(true);
        }

        /// <summary>Keeps the window and its shade out of this much of the right of the screen.</summary>
        public void LeaveRoom(float right)
        {
            ((RectTransform)root.transform).offsetMax = new Vector2(-right, 0f);
        }

        public void Hide()
        {
            recorder.Cancel();
            Clear();
            root.SetActive(false);
        }

        /// <summary>Draws the window again after a turn, keeping the box ready to type.</summary>
        public void Refresh(DebtTalk talk)
        {
            if (talk == null)
            {
                Hide();
                return;
            }

            if (IsOpen)
            {
                Build(talk);
            }
        }

        public void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            if (input != null && input.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                Submit();
            }

            ChatView.AnimateWait(waitLabel, waitText);
            TickVoice();
        }

        private void TickVoice()
        {
            if (recorder.State == VoiceRecorderState.Idle)
            {
                return;
            }

            recorder.Tick();
            if (recorder.State == VoiceRecorderState.Ready)
            {
                byte[] wav = recorder.TakeWav();
                if (wav != null && wav.Length > 0)
                {
                    session.SendTalkVoice(wav);
                }
                else
                {
                    ShowMicProblem();
                }
            }
            else if (recorder.State == VoiceRecorderState.Failed)
            {
                recorder.TakeWav();
                ShowMicProblem();
            }

            voiceButton?.Refresh();
        }

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

            voiceButton?.Refresh();
        }

        private void ShowMicProblem()
        {
            micNotice = MicNotice;
            if (session.CurrentTalk != null)
            {
                Build(session.CurrentTalk);
            }
        }

        private void Submit()
        {
            if (input == null || session.Busy || string.IsNullOrWhiteSpace(input.text))
            {
                return;
            }

            session.SendTalk(input.text);
        }

        private void Build(DebtTalk talk)
        {
            Clear();
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, CardHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var title = UiKit.Label(card, talk.Name, 38, UiKit.Ink, new Vector2(TextWidth, 50f), top, new Vector2(0f, -20f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;

            BuildConversation(card, talk);
            BuildQuestions(card, talk, -456f);
            BuildInput(card, -574f);

            var actions = actionsFor != null ? actionsFor(talk) : new List<ModalOption>();
            float gap = 16f;
            float width = actions.Count > 0 ? (TextWidth - gap * (actions.Count - 1)) / actions.Count : TextWidth;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                float x = -TextWidth / 2f + width / 2f + i * (width + gap);
                UiKit.MakeButton(card, action.Label, action.Color, new Vector2(width, InputHeight), top, new Vector2(x, -650f), () => action.OnPress?.Invoke()).interactable = !session.Busy;
            }

            if (input != null && !session.Busy)
            {
                input.ActivateInputField();
            }
        }

        /// <summary>The bubbles and, while a spoken line is being understood, one more that says so.</summary>
        private void BuildConversation(RectTransform card, DebtTalk talk)
        {
            string name = Loc.T(talk.Name);
            string player = Loc.T("Tú:").TrimEnd(':');
            var lines = new List<ChatLine> { new ChatLine(name, Loc.T(talk.Opening), false) };
            foreach (var turn in talk.Turns)
            {
                lines.Add(new ChatLine(player, turn.Player, true));
                lines.Add(new ChatLine(name, Loc.T(turn.Reply), false));
            }

            // What the user said shows at once, and the other one is seen thinking until the answer comes.
            ChatLine? wait = null;
            if (session.Transcribing)
            {
                wait = new ChatLine(player, Loc.T(Understanding), true);
            }
            else if (session.Busy && !string.IsNullOrEmpty(session.PendingLine))
            {
                lines.Add(new ChatLine(player, session.PendingLine, true));
                wait = new ChatLine(name, Loc.F("{0} está pensando...", name), false);
            }

            waitText = wait.HasValue ? wait.Value.Text : null;
            waitLabel = ChatView.Build(card, new Vector2(TextWidth, LogHeight), new Vector2(0.5f, 1f), new Vector2(0f, -76f), lines, wait);
        }

        /// <summary>The questions worth asking, one click each. Asked, they turn green and stay as a record.</summary>
        private void BuildQuestions(RectTransform card, DebtTalk talk, float y)
        {
            const float gap = 10f;
            var questions = talk.SuggestedQuestions;
            int perRow = questions.Length / 2;
            float width = (TextWidth - gap * (perRow - 1)) / perRow;
            for (int i = 0; i < questions.Length; i++)
            {
                string spoken = Loc.T(questions[i]);
                var fact = DebtTalk.Classify(spoken);
                bool asked = fact.HasValue && System.Linq.Enumerable.Contains(talk.Asked, fact.Value);
                float x = -TextWidth / 2f + width / 2f + (i % perRow) * (width + gap);
                var button = UiKit.MakeButton(card, questions[i], asked ? GameUiColors.Green : Color.white, new Vector2(width, 50f), new Vector2(0.5f, 1f), new Vector2(x, y - (i / perRow) * 58f), () => session.SendTalk(spoken));
                button.name = "Question_" + i;
                UiKit.SetFontSize(button, 23);
                button.interactable = !session.Busy;
                if (asked)
                {
                    UiKit.MarkDone(button);
                }
            }
        }

        private void BuildInput(RectTransform card, float y)
        {
            var top = new Vector2(0.5f, 1f);
            bool voice = recorder.IsSupported;
            bool free = !session.Busy;
            float sendWidth = 160f;
            float talkWidth = voice ? 190f : 0f;
            float gap = 12f;
            float boxWidth = TextWidth - sendWidth - gap - (voice ? talkWidth + gap : 0f);
            float boxX = -(TextWidth - boxWidth) / 2f;
            float sendX = (TextWidth - sendWidth) / 2f;
            float talkX = sendX - sendWidth / 2f - gap - talkWidth / 2f;

            var outline = UiKit.Fill(card, "InputBox", UiKit.Ink, new Vector2(boxWidth, InputHeight), top, new Vector2(boxX, y));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", Color.white, new Vector2(boxWidth - UiKit.Outline * 2f, InputHeight - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);

            var text = UiKit.PlainLabel(body.transform, "Text", string.Empty, 26, UiKit.Ink, new Vector2(boxWidth - 50f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            string notice = session.VoiceNotice ?? micNotice;
            var hint = UiKit.Label(body.transform, notice ?? TypeHint, 25, notice != null ? GameUiColors.Loss : new Color(0.5f, 0.52f, 0.6f, 1f), new Vector2(boxWidth - 50f, 50f), new Vector2(0.5f, 0.5f), Vector2.zero);
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
            voiceButton = null;
            waitLabel = null;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }
}

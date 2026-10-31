using System;
using System.Collections.Generic;
using System.Text;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The window to talk in the player's own words with the collectors, the lender or the pyramid stand: typed, or spoken
    /// with the microphone. Under the conversation, the ways out the game window gives (pay, accept, listen, leave).
    /// </summary>
    public sealed class TalkPanel
    {
        private const float CardWidth = 1300f;
        private const float CardHeight = 860f;
        private const float TextWidth = 1220f;
        private const float LogHeight = 360f;

        private const string MicNotice = "No pude usar el micrófono, escribilo.";
        private const string TypeHint = "Escribí lo que le querés decir...";

        private readonly GameSession session;
        private readonly GameObject root;
        private readonly IVoiceRecorder recorder;
        private readonly Func<DebtTalk, IList<ModalOption>> actionsFor;
        private InputField input;
        private Text talkLabel;
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

            UpdateTalkLabel();
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
                    talkLabel.text = Loc.T("Abriendo micro...");
                    break;
                case VoiceRecorderState.Recording:
                    talkLabel.text = Loc.F("Enviar voz  {0}/{1} s", Mathf.FloorToInt(recorder.Seconds), WavEncoder.MaxSeconds);
                    break;
                default:
                    talkLabel.text = Loc.T("Hablar");
                    break;
            }
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
            var title = UiKit.Label(card, talk.Name, 54, UiKit.Ink, new Vector2(TextWidth, 70f), top, new Vector2(0f, -28f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;

            var log = UiKit.PlainLabel(card, "Log", Conversation(talk), 30, UiKit.Ink, new Vector2(TextWidth, LogHeight), top, new Vector2(0f, -110f), TextAnchor.LowerLeft);
            log.supportRichText = true;
            log.horizontalOverflow = HorizontalWrapMode.Wrap;
            log.verticalOverflow = VerticalWrapMode.Truncate;

            BuildInput(card, -500f);

            var actions = actionsFor != null ? actionsFor(talk) : new List<ModalOption>();
            float gap = 16f;
            float width = actions.Count > 0 ? (TextWidth - gap * (actions.Count - 1)) / actions.Count : TextWidth;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                float x = -TextWidth / 2f + width / 2f + i * (width + gap);
                UiKit.MakeButton(card, action.Label, action.Color, new Vector2(width, 84f), top, new Vector2(x, -640f), () => action.OnPress?.Invoke());
            }

            if (input != null)
            {
                input.ActivateInputField();
            }
        }

        private void BuildInput(RectTransform card, float y)
        {
            var top = new Vector2(0.5f, 1f);
            bool voice = recorder.IsSupported;
            float sendWidth = 220f;
            float talkWidth = voice ? 300f : 0f;
            float gap = 16f;
            float boxWidth = TextWidth - sendWidth - gap - (voice ? talkWidth + gap : 0f);
            float boxX = -(TextWidth - boxWidth) / 2f;
            float sendX = (TextWidth - sendWidth) / 2f;
            float talkX = sendX - sendWidth / 2f - gap - talkWidth / 2f;

            var outline = UiKit.Fill(card, "InputBox", UiKit.Ink, new Vector2(boxWidth, 84f), top, new Vector2(boxX, y));
            outline.raycastTarget = true;
            var body = UiKit.Fill(outline.transform, "Body", Color.white, new Vector2(boxWidth - UiKit.Outline * 2f, 84f - UiKit.Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);

            var text = UiKit.PlainLabel(body.transform, "Text", string.Empty, 32, UiKit.Ink, new Vector2(boxWidth - 60f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            string notice = session.VoiceNotice ?? micNotice;
            var hint = UiKit.Label(body.transform, notice ?? TypeHint, 30, notice != null ? GameUiColors.Loss : new Color(0.5f, 0.52f, 0.6f, 1f), new Vector2(boxWidth - 60f, 60f), new Vector2(0.5f, 0.5f), Vector2.zero);
            hint.fontStyle = FontStyle.Italic;

            input = outline.gameObject.AddComponent<InputField>();
            input.targetGraphic = outline;
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = 300;
            input.lineType = InputField.LineType.SingleLine;

            if (voice)
            {
                var talk = UiKit.MakeButton(card, "Hablar", GameUiColors.Yellow, new Vector2(talkWidth, 84f), top, new Vector2(talkX, y), ToggleTalk);
                talkLabel = talk.GetComponentInChildren<Text>();
                UpdateTalkLabel();
            }

            UiKit.MakeButton(card, "Enviar", GameUiColors.Blue, new Vector2(sendWidth, 84f), top, new Vector2(sendX, y), Submit);
        }

        /// <summary>The last turns that fit, newest at the bottom.</summary>
        private static string Conversation(DebtTalk talk)
        {
            string name = Loc.T(talk.Name);
            var lines = new List<string> { $"<b>{name}:</b> {Loc.T(talk.Opening)}" };
            foreach (var turn in talk.Turns)
            {
                lines.Add($"<color={GameUiColors.Hex(GameUiColors.Muted)}><b>{Loc.T("Tú:")}</b> {Escape(turn.Player)}</color>");
                lines.Add($"<b>{name}:</b> {Loc.T(turn.Reply)}");
            }

            int skip = Math.Max(0, lines.Count - 6);
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
                UnityEngine.Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }
}

using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>One bubble of a conversation.</summary>
    public struct ChatLine
    {
        public string Name;
        public string Text;
        public bool FromPlayer;

        public ChatLine(string name, string text, bool fromPlayer)
        {
            Name = name;
            Text = text;
            FromPlayer = fromPlayer;
        }
    }

    /// <summary>
    /// The conversation of the negotiation and talk windows as bubbles: the other person on the left and the player on the
    /// right, the newest at the bottom, with the older ones a scroll away.
    /// </summary>
    public static class ChatView
    {
        private const float Pad = 18f;
        private const float BubblePad = 16f;
        private const float NameHeight = 28f;
        private const float Gap = 14f;
        private const float MinWidth = 180f;
        private const int TextSize = 28;

        private static readonly Color Tray = GamePalette.Hex("#E9E6D6");
        private static readonly Color OtherBubble = Color.white;
        private static readonly Color PlayerBubble = GamePalette.Hex("#CFE6FF");

        /// <summary>Builds the window. When <paramref name="wait"/> is given it is the last bubble, whose text <see cref="AnimateWait"/> keeps alive; it comes back here.</summary>
        public static Text Build(RectTransform parent, Vector2 size, Vector2 anchor, Vector2 position, IList<ChatLine> lines, ChatLine? wait = null)
        {
            var tray = UiKit.Fill(parent, "Chat", Tray, size, anchor, position);
            tray.raycastTarget = true;
            tray.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Rect(tray.transform, "Content", new Vector2(size.x, 0f), new Vector2(0.5f, 1f), Vector2.zero);

            float y = -Pad;
            foreach (var line in lines)
            {
                y -= Bubble(content, size.x, y, line, false, out _) + Gap;
            }

            Text waiting = null;
            if (wait.HasValue)
            {
                y -= Bubble(content, size.x, y, wait.Value, true, out waiting) + Gap;
            }

            float height = -y - Gap + Pad;
            content.sizeDelta = new Vector2(size.x, height);

            // The newest bubble in view; a short conversation starts at the top.
            content.anchoredPosition = new Vector2(0f, Mathf.Max(0f, height - size.y));

            var scroll = tray.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = tray.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 40f;
            return waiting;
        }

        /// <summary>Dots that come and go, so a wait never looks frozen.</summary>
        public static void AnimateWait(Text label, string text)
        {
            if (label != null)
            {
                label.text = text.TrimEnd('.') + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
            }
        }

        /// <summary>Lays out one bubble and returns its height.</summary>
        private static float Bubble(RectTransform content, float width, float y, ChatLine line, bool waiting, out Text label)
        {
            float maxText = width * 0.74f - (BubblePad + UiKit.Outline) * 2f;
            label = UiKit.PlainLabel(content, "Text", line.Text, TextSize, waiting ? GameUiColors.Muted : UiKit.Ink, new Vector2(maxText, 10f), Vector2.up, Vector2.zero, TextAnchor.UpperLeft);
            label.supportRichText = false;
            label.fontStyle = waiting ? FontStyle.Italic : FontStyle.Normal;
            TextFit.Attach(label).PrepareForMeasure();
            float textHeight = label.preferredHeight + 4f;

            var name = UiKit.PlainLabel(content, "Name", line.Name, 22, GameUiColors.Muted, new Vector2(maxText, NameHeight), Vector2.up, Vector2.zero);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            TextFit.Attach(name).PrepareForMeasure();

            float textWidth = Mathf.Min(maxText, Mathf.Max(MinWidth, Mathf.Max(label.preferredWidth, name.preferredWidth) + 6f));
            var box = new Vector2(textWidth + (BubblePad + UiKit.Outline) * 2f, NameHeight + textHeight + (BubblePad + UiKit.Outline) * 2f - 6f);

            var side = line.FromPlayer ? Vector2.one : Vector2.up;
            var at = new Vector2(line.FromPlayer ? -Pad : Pad, y);
            var bubble = UiKit.Card(content, line.FromPlayer ? "PlayerBubble" : "OtherBubble", line.FromPlayer ? PlayerBubble : OtherBubble, box, side, at);

            name.rectTransform.SetParent(bubble, false);
            name.rectTransform.sizeDelta = new Vector2(textWidth, NameHeight);
            name.rectTransform.anchoredPosition = new Vector2(BubblePad, -BubblePad + 6f);

            label.rectTransform.SetParent(bubble, false);
            label.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);
            label.rectTransform.anchoredPosition = new Vector2(BubblePad, -BubblePad + 6f - NameHeight);
            return box.y;
        }
    }

    /// <summary>
    /// The button that records: yellow to start, red while it listens with a bar that follows the voice, and the seconds so far.
    /// Tap to start and tap to send; where the recorder can tell, it also sends by itself when the player stops talking.
    /// </summary>
    public sealed class VoiceButton
    {
        private readonly IVoiceRecorder recorder;
        private readonly Text label;
        private readonly Image body;
        private readonly RectTransform meter;
        private readonly float meterWidth;

        public VoiceButton(Button button, IVoiceRecorder voiceRecorder, float width)
        {
            recorder = voiceRecorder;
            label = button.GetComponentInChildren<Text>();
            body = button.transform.Find("Body").GetComponent<Image>();
            meterWidth = width - 48f;
            meter = UiKit.Fill(body.transform, "Meter", new Color(1f, 1f, 1f, 0.75f), new Vector2(0f, 8f), new Vector2(0f, 0f), new Vector2(18f, 8f)).rectTransform;
            Refresh();
        }

        public void Refresh()
        {
            switch (recorder.State)
            {
                case VoiceRecorderState.Starting:
                    label.text = Loc.T("Abriendo micro...");
                    body.color = GameUiColors.Yellow;
                    break;
                case VoiceRecorderState.Recording:
                    label.text = Loc.F("Enviar voz  {0}/{1} s", Mathf.FloorToInt(recorder.Seconds), WavEncoder.MaxSeconds);
                    body.color = GameUiColors.Pink;
                    break;
                default:
                    label.text = Loc.T("Hablar");
                    body.color = GameUiColors.Yellow;
                    break;
            }

            meter.sizeDelta = new Vector2(meterWidth * recorder.Level, 8f);
        }
    }
}

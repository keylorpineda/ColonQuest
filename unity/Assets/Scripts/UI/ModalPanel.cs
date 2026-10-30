using System;
using System.Collections.Generic;
using ColonQuest.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    public sealed class ModalOption
    {
        public string Label;
        public Color Color;
        public Action OnPress;
    }

    /// <summary>
    /// The one overlay window used for events, results, the account statement, the month report and
    /// the end screen: a title, an optional subtitle, wrapped text and up to four buttons. The card
    /// grows to fit its content.
    /// </summary>
    public sealed class ModalPanel
    {
        private const float CardWidth = 1100f;
        private const float Pad = 40f;
        private const float ButtonHeight = 84f;
        private const float ButtonGap = 16f;
        private const float MaxBodyHeight = 460f;

        private readonly GameObject root;
        private readonly Transform parent;
        private readonly List<ModalOption> options = new List<ModalOption>();

        // A talking window shows its text a few letters at a time while the speaker's face moves its mouth.
        private Text revealText;
        private string revealFull = string.Empty;
        private float revealShown;
        private int revealTotal;
        private RectTransform mouth;
        private float talkClock;

        public ModalPanel(Transform canvas)
        {
            parent = canvas;
            root = new GameObject("Modal", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // The dim layer also catches clicks, so nothing behind the window is pressed.
            var dim = root.AddComponent<Image>();
            dim.color = UiKit.Dim;
            root.SetActive(false);
        }

        public bool IsOpen => root.activeSelf;

        public int OptionCount => options.Count;

        /// <summary>True while the text is still appearing.</summary>
        public bool IsRevealing => revealText != null && revealShown < revealTotal;

        /// <summary>Shows all the text at once and the speaker stops talking.</summary>
        public void Complete()
        {
            if (revealText != null)
            {
                revealShown = revealTotal;
                revealText.text = revealFull;
                CloseMouth();
            }
        }

        /// <summary>Moves the typing and the mouth on. Call it every frame.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsOpen || revealText == null)
            {
                return;
            }

            if (!IsRevealing)
            {
                CloseMouth();
                return;
            }

            revealShown = Mathf.Min(revealTotal, revealShown + deltaSeconds * TextReveal.LettersPerSecond);
            revealText.text = TextReveal.Prefix(revealFull, Mathf.FloorToInt(revealShown));

            talkClock += deltaSeconds;
            if (mouth != null)
            {
                float open = Mathf.Abs(Mathf.Sin(talkClock * 13f)) * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(talkClock * 5.3f)));
                mouth.sizeDelta = new Vector2(34f, 6f + 22f * open);
            }

            if (!IsRevealing)
            {
                CloseMouth();
            }
        }

        private void CloseMouth()
        {
            if (mouth != null)
            {
                mouth.sizeDelta = new Vector2(34f, 6f);
            }
        }

        /// <param name="speaker">When set, the window talks: the text types itself and a face in this name's color speaks it.</param>
        /// <param name="bodyFontSize">Smaller than the default for windows that carry a lot of figures.</param>
        /// <param name="maxBodyHeight">Zero keeps the default; a bigger value leaves room for a longer text.</param>
        public void Show(string title, string subtitle, string body, IList<ModalOption> choices, string speaker = null, int bodyFontSize = 32, float maxBodyHeight = 0f)
        {
            Clear();
            options.AddRange(choices);
            revealText = null;
            mouth = null;
            talkClock = 0f;
            bool talks = speaker != null && !string.IsNullOrEmpty(body);

            // Measure first, so the card can be sized to its content.
            float textWidth = CardWidth - Pad * 2f - UiKit.Outline * 2f;
            var probe = UiKit.Label(root.transform, body, bodyFontSize, UiKit.Ink, new Vector2(textWidth, 10f), new Vector2(0.5f, 0.5f), Vector2.zero);
            float bodyHeight = string.IsNullOrEmpty(body) ? 0f : Mathf.Min(UiKit.MeasureHeight(probe, textWidth) + 8f, maxBodyHeight > 0f ? maxBodyHeight : MaxBodyHeight);
            UnityEngine.Object.Destroy(probe.gameObject);

            float titleHeight = 64f;
            float subtitleHeight = string.IsNullOrEmpty(subtitle) ? 0f : 40f;
            float faceGap = talks && bodyHeight > 0f ? 16f : 0f;
            float buttonsHeight = options.Count == 0 ? 0f : options.Count * ButtonHeight + (options.Count - 1) * ButtonGap;
            float height = Pad + titleHeight + subtitleHeight + faceGap + (bodyHeight > 0f ? bodyHeight + 20f : 0f) + (buttonsHeight > 0f ? buttonsHeight + 24f : 0f) + Pad;

            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, height), new Vector2(0.5f, 0.5f), Vector2.zero);
            var top = new Vector2(0.5f, 1f);
            float y = -Pad + UiKit.Outline;

            UiKit.Label(card, title, 46, UiKit.Ink, new Vector2(talks ? textWidth - 130f : textWidth, titleHeight), top, new Vector2(talks ? -65f : 0f, y), TextAnchor.MiddleLeft);
            if (talks)
            {
                BuildFace(card, speaker, new Vector2(CardWidth * 0.5f - Pad - 56f, y - 6f));
            }

            y -= titleHeight;

            if (subtitleHeight > 0f)
            {
                UiKit.Label(card, subtitle, 26, GameUiColors.Muted, new Vector2(textWidth, subtitleHeight), top, new Vector2(0f, y), TextAnchor.MiddleLeft);
                y -= subtitleHeight;
            }

            if (bodyHeight > 0f)
            {
                y -= 12f + faceGap;
                var text = UiKit.Label(card, body, bodyFontSize, UiKit.Ink, new Vector2(textWidth, bodyHeight), top, new Vector2(0f, y), TextAnchor.UpperLeft);
                text.fontStyle = FontStyle.Normal;
                if (talks)
                {
                    revealText = text;
                    revealFull = body;
                    revealTotal = TextReveal.VisibleLength(body);
                    revealShown = 0f;
                    text.text = string.Empty;
                }
                y -= bodyHeight + 8f;
            }

            if (buttonsHeight > 0f)
            {
                y -= 16f;
                for (int i = 0; i < options.Count; i++)
                {
                    var option = options[i];
                    var press = option.OnPress;
                    UiKit.MakeButton(card, option.Label, option.Color, new Vector2(textWidth, ButtonHeight), top, new Vector2(0f, y), () => Activate(press));
                    y -= ButtonHeight + ButtonGap;
                }
            }

            root.SetActive(true);
        }

        /// <summary>A face in the speaker's color: two eyes and a mouth that opens while the text appears.</summary>
        private void BuildFace(RectTransform card, string speaker, Vector2 position)
        {
            mouth = UiKit.Portrait(card, speaker, 112f, new Vector2(0.5f, 1f), position);
        }

        /// <summary>Presses the option with this index, as if it was clicked. Used for the number keys.</summary>
        public void Press(int index)
        {
            if (IsOpen && index >= 0 && index < options.Count)
            {
                Activate(options[index].OnPress);
            }
        }

        public void Hide()
        {
            Clear();
            root.SetActive(false);
        }

        private void Activate(Action action)
        {
            if (IsRevealing)
            {
                // A first press only finishes the line; the next one chooses.
                Complete();
                return;
            }

            Hide();
            action?.Invoke();
        }

        private void Clear()
        {
            options.Clear();
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }

    public static class GameUiColors
    {
        public static readonly Color Muted = new Color(0.32f, 0.35f, 0.45f, 1f);
        public static readonly Color Gain = new Color(0.12f, 0.62f, 0.33f, 1f);
        public static readonly Color Loss = new Color(0.85f, 0.21f, 0.25f, 1f);
        public static readonly Color Blue = new Color(0.56f, 0.83f, 1f, 1f);
        public static readonly Color Green = new Color(0.66f, 0.94f, 0.72f, 1f);
        public static readonly Color Yellow = new Color(1f, 0.88f, 0.54f, 1f);
        public static readonly Color Pink = new Color(1f, 0.70f, 0.70f, 1f);

        public static Color Cycle(int index)
        {
            switch (index % 4)
            {
                case 0: return Blue;
                case 1: return Green;
                case 2: return Yellow;
                default: return Pink;
            }
        }

        public static string Hex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// A line at the bottom of the screen, like the subtitles of a film: who speaks, then the words typing themselves.
    /// It never stops the player. The player's own thoughts use it too, in italics and with no name.
    /// </summary>
    public sealed class SubtitleBar
    {
        private const float CharactersPerSecond = 55f;

        private readonly GameObject root;
        private readonly Text speakerText;
        private readonly Text bodyText;

        private string full = string.Empty;
        private float shown;
        private float remaining;

        public SubtitleBar(Transform canvas)
        {
            var back = UiKit.Fill(canvas, "Subtitles", new Color(0.04f, 0.06f, 0.13f, 0.84f), new Vector2(1500f, 150f), new Vector2(0.5f, 0f), new Vector2(0f, 150f));
            back.raycastTarget = false;
            root = back.gameObject;
            speakerText = UiKit.Label(back.transform, string.Empty, 30, GameUiColors.Yellow, new Vector2(1420f, 38f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.MiddleLeft);
            bodyText = UiKit.Label(back.transform, string.Empty, 34, Color.white, new Vector2(1420f, 92f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), TextAnchor.UpperLeft);
            bodyText.fontStyle = FontStyle.Normal;
            root.SetActive(false);
        }

        public bool IsVisible => root.activeSelf;

        /// <summary>Shows a line. The speaker may be empty, which is a thought: the text is slanted and has no name.</summary>
        public void Show(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            bool thought = string.IsNullOrEmpty(speaker);
            speakerText.text = thought ? string.Empty : speaker;
            bodyText.fontStyle = thought ? FontStyle.Italic : FontStyle.Normal;
            bodyText.color = thought ? new Color(0.86f, 0.92f, 1f, 1f) : Color.white;
            full = text;
            shown = 0f;
            bodyText.text = string.Empty;
            remaining = Mathf.Clamp(text.Length * 0.06f + 2.2f, 3f, 10f);
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
            full = string.Empty;
        }

        /// <summary>Types the line and hides it when its time is up. A window that opens takes the screen, so the line goes away.</summary>
        public void Tick(float dt, bool windowOpen)
        {
            if (!root.activeSelf)
            {
                return;
            }

            if (windowOpen)
            {
                Hide();
                return;
            }

            shown = Mathf.Min(full.Length, shown + CharactersPerSecond * dt);
            bodyText.text = full.Substring(0, Mathf.FloorToInt(shown));
            if (shown >= full.Length)
            {
                remaining -= dt;
                if (remaining <= 0f)
                {
                    Hide();
                }
            }
        }
    }
}

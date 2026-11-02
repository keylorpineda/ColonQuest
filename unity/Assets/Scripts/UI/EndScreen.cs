using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The end of a run as a report to look at, not a page to read: the postcard of the year that Muse Image painted, three big
    /// figures, the grades and the few words the model wrote. The figures are drawn here: no picture carries text.
    /// </summary>
    public sealed class EndScreen
    {
        private const float CardWidth = 1600f;
        private const float CardHeight = 900f;
        private const float PictureSize = 600f;
        private const float RightWidth = 880f;
        private const float RightX = 330f;

        private readonly GameObject root;
        private Image picture;
        private string pictureKey;

        public EndScreen(Transform canvas)
        {
            root = UiKit.Overlay(canvas, "EndScreen");
            root.SetActive(false);
        }

        public bool IsOpen => root.activeSelf;

        public void Hide()
        {
            Clear();
            root.SetActive(false);
        }

        /// <summary>The postcard arrives from the backend when it can; until then its place stays plain.</summary>
        public void Tick()
        {
            if (!IsOpen || picture == null || picture.sprite != null)
            {
                return;
            }

            var sprite = RemoteImages.Fetch(pictureKey);
            if (sprite != null)
            {
                picture.sprite = sprite;
                picture.color = Color.white;
            }
        }

        public void Show(string title, SessionSummary summary, PlayerState state, Holdings holdings, BillBook bills, IList<ModalOption> options)
        {
            Clear();
            root.transform.SetAsLastSibling();
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, CardHeight), new Vector2(0.5f, 0.5f), Vector2.zero);

            BuildPicture(card, summary);
            var heading = UiKit.Label(card, title, 52, UiKit.Ink, new Vector2(RightWidth, 66f), top, new Vector2(RightX, -28f), TextAnchor.MiddleLeft);
            heading.font = UiKit.TitleFont;
            UiKit.Label(card, Loc.F("{0} meses jugados", summary.MonthsPlayed), 24, GameUiColors.Muted, new Vector2(RightWidth, 32f), top, new Vector2(RightX, -92f), TextAnchor.MiddleLeft);

            BuildFigures(card, summary, state, -136f);
            BuildGrades(card, state, holdings, bills, -268f);

            var words = UiKit.PlainLabel(card, "Narrative", Loc.T(summary.Narrative), 25, UiKit.Ink, new Vector2(RightWidth, 300f), top, new Vector2(RightX, -404f), TextAnchor.UpperLeft);
            words.fontStyle = FontStyle.Normal;
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.resizeTextForBestFit = true;
            words.resizeTextMinSize = 20;
            words.resizeTextMaxSize = 25;

            BuildOptions(card, options, top);
            root.SetActive(true);
        }

        private void BuildPicture(RectTransform card, SessionSummary summary)
        {
            var left = new Vector2(0f, 0.5f);
            var frame = UiKit.Fill(card, "PictureFrame", UiKit.Ink, new Vector2(PictureSize + 12f, PictureSize + 12f), left, new Vector2(28f, 0f));
            frame.type = Image.Type.Simple;
            picture = UiKit.Fill(frame.transform, "Picture", new Color(0.85f, 0.83f, 0.74f, 1f), new Vector2(PictureSize, PictureSize), new Vector2(0.5f, 0.5f), Vector2.zero);
            picture.type = Image.Type.Simple;
            picture.preserveAspect = true;
            picture.sprite = null;
            pictureKey = PostcardKeys.For(summary.TotalDebt);
        }

        private static void BuildFigures(RectTransform card, SessionSummary summary, PlayerState state, float y)
        {
            float gap = 14f;
            float width = (RightWidth - 2f * gap) / 3f;
            long have = state.Cash + state.Savings + state.EmergencyFund;
            Figure(card, "Lo que tenés", Money.Format(have), UiKit.Ink, width, RightX - width - gap, y);
            Figure(card, "Lo que debés", Money.Format(summary.TotalDebt), summary.TotalDebt > 0L ? GameUiColors.Loss : GameUiColors.Gain, width, RightX, y);
            Figure(card, "Lo que tenés menos lo que debés", Money.Format(summary.NetWorth), summary.NetWorth < 0L ? GameUiColors.Loss : GameUiColors.Gain, width, RightX + width + gap, y);
        }

        private static void Figure(RectTransform card, string label, string value, Color color, float width, float x, float y)
        {
            var tile = UiKit.Fill(card, "Figure", Color.white, new Vector2(width, 118f), new Vector2(0.5f, 1f), new Vector2(x, y));
            var name = UiKit.Label(tile.transform, label, 20, GameUiColors.Muted, new Vector2(width - 20f, 46f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Normal;
            var figure = UiKit.PlainLabel(tile.transform, "Value", value, 36, color, new Vector2(width - 16f, 56f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), TextAnchor.MiddleCenter);
            figure.font = UiKit.TitleFont;
        }

        private static void BuildGrades(RectTransform card, PlayerState state, Holdings holdings, BillBook bills, float y)
        {
            var lines = ReportCard.Build(state, holdings, bills);
            float gap = 10f;
            float width = (RightWidth - gap * (lines.Count - 1)) / lines.Count;
            float x = RightX - RightWidth / 2f + width / 2f;
            foreach (var line in lines)
            {
                var tile = UiKit.Fill(card, "Grade", ColorOf(line.Grade), new Vector2(width, 124f), new Vector2(0.5f, 1f), new Vector2(x, y));
                var letter = UiKit.PlainLabel(tile.transform, "Letter", line.Grade.ToString(), 50, UiKit.Ink, new Vector2(width, 64f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), TextAnchor.MiddleCenter);
                letter.font = UiKit.TitleFont;
                var name = UiKit.Label(tile.transform, line.Concept, 18, UiKit.Ink, new Vector2(width - 12f, 50f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), TextAnchor.MiddleCenter);
                name.fontStyle = FontStyle.Normal;
                x += width + gap;
            }
        }

        private static Color ColorOf(char grade)
        {
            switch (grade)
            {
                case 'A': return GameUiColors.Green;
                case 'B': return GameUiColors.Blue;
                case 'C': return GameUiColors.Yellow;
                default: return GameUiColors.Pink;
            }
        }

        private void BuildOptions(RectTransform card, IList<ModalOption> options, Vector2 top)
        {
            float gap = 14f;
            float width = (RightWidth - gap * (options.Count - 1)) / options.Count;
            float x = RightX - RightWidth / 2f + width / 2f;
            foreach (var option in options)
            {
                var press = option.OnPress;
                var button = UiKit.MakeButton(card, option.Label, option.Color, new Vector2(width, 76f), top, new Vector2(x, -800f), () =>
                {
                    Hide();
                    press?.Invoke();
                });
                UiKit.SetFontSize(button, 26);
                x += width + gap;
            }
        }

        private void Clear()
        {
            picture = null;
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.transform.GetChild(i).gameObject);
            }
        }
    }
}

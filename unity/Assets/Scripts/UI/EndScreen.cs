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
    /// The end of a run as a report to look at: the verdict, three big figures, the money month by month, the grades, what the
    /// player did, the picture of how it went and the few words the model wrote. The figures are drawn here: no picture has text.
    /// </summary>
    public sealed class EndScreen
    {
        private const float CardWidth = 1600f;
        private const float CardHeight = 900f;
        private const float Inner = 1544f;
        private const float ColumnWidth = 760f;
        private const float LeftX = -392f;
        private const float RightX = 392f;
        private const float ChartHeight = 250f;
        private const float ChartMaxBar = 136f;
        private const int MostMonthsWithFigures = 4;
        private const float PictureSize = 256f;

        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Color Band = GamePalette.Hex("#1B2826");
        private static readonly Color Cream = GamePalette.Hex("#F8F1DC");
        private static readonly Color Accent = GamePalette.Hex("#F2A23A");

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

        /// <summary>The picture arrives from the backend when it can; until then its place stays plain.</summary>
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

        public void Show(string title, SessionSummary summary, GameLoop loop, SessionTraceDto trace, IList<ModalOption> options)
        {
            Clear();
            root.transform.SetAsLastSibling();
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, CardHeight), new Vector2(0.5f, 0.5f), Vector2.zero);

            BuildBand(card, title, summary, loop.State, trace);
            BuildFigures(card, summary, -142f);

            Heading(card, "Tu plata y tus deudas, mes a mes", LeftX, -270f);
            BuildChart(card, loop.MonthCloses, -306f);
            Heading(card, "Tus notas", LeftX, -562f);
            BuildGrades(card, loop, -598f);

            Heading(card, "Lo que hiciste", RightX, -270f);
            BuildDeeds(card, summary, -306f);
            Heading(card, summary.Source == "model" ? "Tu historia, contada por la IA" : "Tu historia", RightX, -430f);
            BuildWords(card, summary, EndingArt.KeyFor(loop.State, summary.NetWorth, summary.DealsAccepted), -466f);

            BuildOptions(card, options);
            root.SetActive(true);
        }

        private static void BuildBand(RectTransform card, string title, SessionSummary summary, PlayerState state, SessionTraceDto trace)
        {
            var band = UiKit.Fill(card, "Band", Band, new Vector2(CardWidth - 16f, 122f), Top, new Vector2(0f, -8f));
            var left = Vector2.up;
            var right = Vector2.one;
            var heading = UiKit.PlainLabel(band.transform, "Title", title, 54, Cream, new Vector2(740f, 66f), left, new Vector2(30f, -6f), TextAnchor.MiddleLeft);
            heading.font = UiKit.TitleFont;
            var months = UiKit.PlainLabel(band.transform, "Months", Loc.F("{0} meses jugados", summary.MonthsPlayed), 26, Cream, new Vector2(740f, 36f), left, new Vector2(30f, -74f), TextAnchor.MiddleLeft);
            months.fontStyle = FontStyle.Normal;

            string verdict = ReportCard.Verdict(summary.NetWorth, summary.TotalDebt, state.InformalLoans.Count > 0);
            var said = UiKit.Label(band.transform, verdict, 40, Accent, new Vector2(800f, 60f), right, new Vector2(-30f, -8f), TextAnchor.MiddleRight);
            said.font = UiKit.TitleFont;
            if (trace != null && trace.calls.Count > 0)
            {
                var calls = UiKit.PlainLabel(band.transform, "Calls", trace.calls.Count == 1 ? Loc.F("Una llamada real a un modelo, unos {0}", DemoReadout.Dollars(trace.totalCostUsd)) : trace.calls.Count == 1 ? Loc.F("Una llamada real a un modelo, unos {0}", DemoReadout.Dollars(trace.totalCostUsd)) : Loc.F("{0} llamadas reales a modelos, unos {1}", trace.calls.Count, DemoReadout.Dollars(trace.totalCostUsd)), 26, Cream, new Vector2(800f, 36f), right, new Vector2(-30f, -74f), TextAnchor.MiddleRight);
                calls.fontStyle = FontStyle.Normal;
            }
        }

        private static void BuildFigures(RectTransform card, SessionSummary summary, float y)
        {
            float gap = 14f;
            float width = (Inner - 2f * gap) / 3f;
            long have = summary.NetWorth + summary.TotalDebt;
            Figure(card, "Lo que tenés", Money.Format(have), UiKit.Ink, width, -width - gap, y);
            Figure(card, "Lo que debés", Money.Format(summary.TotalDebt), summary.TotalDebt > 0L ? GameUiColors.Loss : GameUiColors.Gain, width, 0f, y);
            Figure(card, "Lo que tenés menos lo que debés", Money.Format(summary.NetWorth), summary.NetWorth < 0L ? GameUiColors.Loss : GameUiColors.Gain, width, width + gap, y);
        }

        private static void Figure(RectTransform card, string label, string value, Color color, float width, float x, float y)
        {
            var tile = UiKit.Fill(card, "Figure", Color.white, new Vector2(width, 116f), Top, new Vector2(x, y));
            var name = UiKit.Label(tile.transform, label, 24, GameUiColors.Muted, new Vector2(width - 20f, 40f), Top, new Vector2(0f, -8f), TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Normal;
            var figure = UiKit.PlainLabel(tile.transform, "Value", value, 48, color, new Vector2(width - 16f, 62f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), TextAnchor.MiddleCenter);
            figure.font = UiKit.TitleFont;
        }

        private static void Heading(RectTransform card, string spanish, float x, float y)
        {
            var label = UiKit.Label(card, spanish, 28, UiKit.Ink, new Vector2(ColumnWidth, 34f), Top, new Vector2(x, y), TextAnchor.MiddleLeft);
            label.font = UiKit.TitleFont;
        }

        private static void BuildChart(RectTransform card, IList<MonthClose> months, float y)
        {
            var tile = UiKit.Fill(card, "Chart", Color.white, new Vector2(ColumnWidth, ChartHeight), Top, new Vector2(LeftX, y));
            if (months.Count == 0)
            {
                var none = UiKit.Label(tile.transform, "Esta partida no llegó a cerrar un mes.", 26, GameUiColors.Muted, new Vector2(ColumnWidth - 40f, ChartHeight), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
                none.fontStyle = FontStyle.Normal;
                return;
            }

            Legend(tile.transform);

            long highest = 1L;
            foreach (var month in months)
            {
                highest = System.Math.Max(highest, System.Math.Max(month.money, month.debts));
            }

            bool withFigures = months.Count <= MostMonthsWithFigures;
            float slot = (ColumnWidth - 32f) / months.Count;
            float bar = Mathf.Min(96f, (slot - 14f) / 2f);
            for (int i = 0; i < months.Count; i++)
            {
                float center = 16f + slot * (i + 0.5f);
                Bar(tile.transform, months[i].money, highest, GameUiColors.Green, center - bar - 1f, bar, withFigures);
                Bar(tile.transform, months[i].debts, highest, GameUiColors.Pink, center + 1f, bar, withFigures);
                var label = UiKit.PlainLabel(tile.transform, "Month", months.Count > 6 ? DemoReadout.Whole(i + 1) : Loc.F("Mes {0}", i + 1), 22, GameUiColors.Muted, new Vector2(slot, 30f), Vector2.zero, new Vector2(center - slot / 2f, 4f), TextAnchor.MiddleCenter);
                label.fontStyle = FontStyle.Normal;
            }
        }

        private static void Legend(Transform tile)
        {
            var right = Vector2.one;
            UiKit.Fill(tile, "MoneyKey", GameUiColors.Green, new Vector2(20f, 20f), right, new Vector2(-226f, -14f));
            UiKit.Label(tile, "Plata", 22, GameUiColors.Muted, new Vector2(80f, 28f), right, new Vector2(-142f, -10f), TextAnchor.MiddleLeft).fontStyle = FontStyle.Normal;
            UiKit.Fill(tile, "DebtKey", GameUiColors.Pink, new Vector2(20f, 20f), right, new Vector2(-112f, -14f));
            UiKit.Label(tile, "Deudas", 22, GameUiColors.Muted, new Vector2(90f, 28f), right, new Vector2(-16f, -10f), TextAnchor.MiddleLeft).fontStyle = FontStyle.Normal;
        }

        private static void Bar(Transform tile, long value, long highest, Color color, float x, float width, bool withFigure)
        {
            const float baseline = 36f;
            float height = Mathf.Max(4f, ChartMaxBar * value / highest);
            var fill = UiKit.Fill(tile, "Bar", color, new Vector2(width, height), Vector2.zero, new Vector2(x, baseline));
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            if (withFigure)
            {
                var figure = UiKit.PlainLabel(tile, "BarFigure", Money.Format(value), 20, UiKit.Ink, new Vector2(width + 12f, 26f), Vector2.zero, new Vector2(x - 6f, baseline + height + 2f), TextAnchor.MiddleCenter);
                figure.fontStyle = FontStyle.Normal;
                figure.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        private static void BuildGrades(RectTransform card, GameLoop loop, float y)
        {
            var lines = ReportCard.Build(loop.State, loop.Holdings, loop.Bills);
            float gap = 10f;
            float width = (ColumnWidth - gap * (lines.Count - 1)) / lines.Count;
            float x = LeftX - ColumnWidth / 2f + width / 2f;
            foreach (var line in lines)
            {
                var tile = UiKit.Fill(card, "Grade", ColorOf(line.Grade), new Vector2(width, 148f), Top, new Vector2(x, y));
                var letter = UiKit.PlainLabel(tile.transform, "Letter", line.Grade.ToString(), 62, UiKit.Ink, new Vector2(width, 80f), Top, new Vector2(0f, -4f), TextAnchor.MiddleCenter);
                letter.font = UiKit.TitleFont;
                var name = UiKit.Label(tile.transform, line.Concept, 21, UiKit.Ink, new Vector2(width - 10f, 58f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), TextAnchor.MiddleCenter);
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

        private static void BuildDeeds(RectTransform card, SessionSummary summary, float y)
        {
            var deeds = new[]
            {
                new KeyValuePair<string, int>("Decisiones", summary.Decisions),
                new KeyValuePair<string, int>("Negociaciones", summary.Negotiations),
                new KeyValuePair<string, int>("Preguntas clave", summary.KeyQuestionsAsked.Count),
                new KeyValuePair<string, int>("Conceptos vistos", summary.ConceptsTaught.Count)
            };

            float gap = 10f;
            float width = (ColumnWidth - gap * (deeds.Length - 1)) / deeds.Length;
            float x = RightX - ColumnWidth / 2f + width / 2f;
            foreach (var deed in deeds)
            {
                var tile = UiKit.Fill(card, "Deed", Color.white, new Vector2(width, 112f), Top, new Vector2(x, y));
                var number = UiKit.PlainLabel(tile.transform, "Number", DemoReadout.Whole(deed.Value), 50, UiKit.Ink, new Vector2(width, 64f), Top, new Vector2(0f, -4f), TextAnchor.MiddleCenter);
                number.font = UiKit.TitleFont;
                var name = UiKit.Label(tile.transform, deed.Key, 22, GameUiColors.Muted, new Vector2(width - 8f, 36f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), TextAnchor.MiddleCenter);
                name.fontStyle = FontStyle.Normal;
                x += width + gap;
            }
        }

        private void BuildWords(RectTransform card, SessionSummary summary, string key, float y)
        {
            var tile = UiKit.Fill(card, "Words", Color.white, new Vector2(ColumnWidth, 280f), Top, new Vector2(RightX, y));
            var left = new Vector2(0f, 0.5f);
            picture = UiKit.Fill(tile.transform, "Picture", new Color(0.85f, 0.83f, 0.74f, 1f), new Vector2(PictureSize, PictureSize), left, new Vector2(12f, 0f));
            picture.type = Image.Type.Simple;
            picture.preserveAspect = true;
            picture.sprite = null;
            pictureKey = key;

            float textWidth = ColumnWidth - PictureSize - 44f;
            var words = UiKit.PlainLabel(tile.transform, "Narrative", Loc.T(summary.Narrative), 27, UiKit.Ink, new Vector2(textWidth, 256f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), TextAnchor.MiddleLeft);
            words.fontStyle = FontStyle.Normal;
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.resizeTextForBestFit = true;
            words.resizeTextMinSize = 18;
            words.resizeTextMaxSize = 27;
        }

        private void BuildOptions(RectTransform card, IList<ModalOption> options)
        {
            float gap = 14f;
            float width = (Inner - gap * (options.Count - 1)) / options.Count;
            float x = -Inner / 2f + width / 2f;
            foreach (var option in options)
            {
                var press = option.OnPress;
                var button = UiKit.MakeButton(card, option.Label, option.Color, new Vector2(width, 76f), Top, new Vector2(x, -808f), () =>
                {
                    Hide();
                    press?.Invoke();
                });
                UiKit.SetFontSize(button, 30);
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

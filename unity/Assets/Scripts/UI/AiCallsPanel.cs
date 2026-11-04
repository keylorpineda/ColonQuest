using System.Collections.Generic;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The real calls to the models in this run, on the right of the screen, as the backend reports them: the plan of the year,
    /// the cost and each call. L or its tab hides it, and the windows of the simulator move over to leave it room. Nothing here is
    /// made up: a call that did not happen does not appear, and it never shows a key.
    /// </summary>
    public sealed class AiCallsPanel
    {
        public const float Width = 490f;
        private const float TopSpace = 340f;
        private const float BottomSpace = 24f;
        private const float TabWidth = 190f;
        private const int MonthsShown = 3;

        private static readonly Color Dark = GamePalette.Hex("#1B2826");
        private static readonly Color Accent = GamePalette.Hex("#F2A23A");

        private readonly GameObject panel;
        private readonly Text body;
        private readonly RectTransform content;
        private readonly ScrollRect scroll;
        private readonly GameObject tab;
        private readonly Text tabText;
        private static string Hint => Loc.T("Cada llamada real a un modelo va a aparecer aquí, con su tiempo y su costo.");

        private string written = string.Empty;
        private string shown;
        private bool waiting;

        /// <summary>What the backend said last, or null before the first answer.</summary>
        public SessionTraceDto Last { get; private set; }

        public bool Expanded { get; private set; }

        public AiCallsPanel(Transform canvas)
        {
            var size = new Vector2(Width, 1080f - TopSpace - BottomSpace);
            var card = UiKit.Card(canvas, "AiCalls", Dark, size, Vector2.right, new Vector2(-24f, BottomSpace));
            panel = card.parent.gameObject;
            var title = UiKit.Label(card, "Qué hizo la IA", 34, Accent, new Vector2(Width - 150f, 44f), Vector2.up, new Vector2(20f, -10f));
            title.font = UiKit.TitleFont;
            var hide = UiKit.Label(card, "[L] Ocultar", 24, new Color(0.8f, 0.8f, 0.76f, 1f), new Vector2(150f, 44f), Vector2.one, new Vector2(-18f, -10f), TextAnchor.MiddleRight);
            hide.fontStyle = FontStyle.Normal;

            var view = UiKit.Rect(card, "View", new Vector2(Width - 36f, size.y - 70f), new Vector2(0.5f, 0f), new Vector2(0f, 14f));
            view.gameObject.AddComponent<RectMask2D>();
            content = UiKit.Rect(view, "Content", new Vector2(Width - 36f, 100f), Vector2.up, Vector2.zero);
            body = UiKit.PlainLabel(content, "Text", string.Empty, 27, new Color(0.95f, 0.94f, 0.88f, 1f), new Vector2(Width - 56f, 100f), Vector2.up, Vector2.zero, TextAnchor.UpperLeft);
            body.fontStyle = FontStyle.Normal;
            body.supportRichText = true;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            view.gameObject.AddComponent<Image>().color = Color.clear;

            var button = UiKit.MakeButton(canvas, "IA  [L]", Accent, new Vector2(TabWidth, 54f), Vector2.right, new Vector2(-24f, BottomSpace), () => SetExpanded(true));
            tab = button.gameObject;
            tabText = button.GetComponentInChildren<Text>();
            UiKit.SetFontSize(button, 25);

            panel.SetActive(false);
            tab.SetActive(false);
        }

        /// <summary>The room the windows of the simulator have to leave on the right of the screen.</summary>
        public float RoomNeeded => panel.activeSelf ? Width + 24f : 0f;

        public void SetExpanded(bool expanded)
        {
            Expanded = expanded;
        }

        public void Toggle()
        {
            Expanded = !Expanded;
        }

        /// <summary>Shows the panel or its tab, or neither when the simulator has no use for them right now.</summary>
        public void Tick(bool available)
        {
            panel.SetActive(available && Expanded);
            tab.SetActive(available && !Expanded);
            if (tab.activeSelf && Last != null)
            {
                tabText.text = Loc.T("IA  [L]") + "  " + DemoReadout.Whole(Last.calls.Count);
            }
        }

        /// <summary>A model is answering right now: the panel says so at once, before the call can be read back.</summary>
        public void SetWaiting(bool isWaiting)
        {
            if (isWaiting != waiting)
            {
                waiting = isWaiting;
                Render();
            }
        }

        /// <summary>A new run starts with an empty panel.</summary>
        public void Clear()
        {
            Last = null;
            shown = null;
            Write(Hint);
        }

        public void Show(SessionTraceDto trace)
        {
            Last = trace;
            var text = new StringBuilder();
            AppendPlan(text, trace);
            if (trace.calls.Count == 0)
            {
                Write(text + Hint);
                return;
            }

            text.Append(DemoReadout.Tint(Loc.F("Esta simulación lleva gastados unos {0}", DemoReadout.Dollars(trace.totalCostUsd)), "#F2A23A")).Append("\n\n");

            // In the order they happened, and the panel follows the last one down.
            foreach (var call in trace.calls)
            {
                DemoReadout.AppendShortCall(text, call);
            }

            // The tools by name: what they asked and answered is in the feature demo.
            if (trace.toolSteps.Count > 0)
            {
                var tools = new List<string>();
                foreach (var step in trace.toolSteps)
                {
                    if (!tools.Contains(step.tool))
                    {
                        tools.Add(step.tool);
                    }
                }

                text.Append(DemoReadout.Tint(Loc.T("Herramientas usadas"), "#8FD3FF")).Append('\n').Append(string.Join(", ", tools));
            }

            Write(text.ToString());
        }

        private static void AppendPlan(StringBuilder text, SessionTraceDto trace)
        {
            if (trace.seasonPlan == null || trace.seasonPlan.Count == 0)
            {
                return;
            }

            string who = DemoReadout.PlannedByTheModel(trace)
                ? Loc.T("Tu año, por Muse Wonder")
                : Loc.T(DemoReadout.PlannerAnswered(trace) ? "Plan base: Muse Wonder no llegó a tiempo" : "Muse Wonder está escribiendo tu año...");
            text.Append(DemoReadout.Tint(who, "#8FD3FF")).Append('\n');
            int months = Mathf.Min(MonthsShown, trace.seasonPlan.Count);
            for (int i = 0; i < months; i++)
            {
                text.Append(trace.seasonPlan[i].month).Append(".  ").Append(trace.seasonPlan[i].theme.Replace('<', '‹').Replace('>', '›')).Append('\n');
            }

            text.Append('\n');
        }

        private void Write(string text)
        {
            written = text;
            Render();
        }

        private void Render()
        {
            string text = waiting ? written + "\n" + DemoReadout.Tint(Loc.T("Esperando al modelo..."), "#8FD3FF") : written;
            if (text == shown)
            {
                return;
            }

            shown = text;
            body.text = text;
            float height = body.preferredHeight + 20f;
            body.rectTransform.sizeDelta = new Vector2(body.rectTransform.sizeDelta.x, height);
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            scroll.verticalNormalizedPosition = 0f;
        }
    }
}

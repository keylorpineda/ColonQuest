using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The list of things to do, under the HUD: what is left today and what is already done. Tab hides it and shows it again.
    /// What goes in it is read from the run (see <see cref="TaskList"/>).
    /// </summary>
    public sealed partial class GameUi
    {
        private const int TaskRows = 6;
        private const float TaskRowHeight = 32f;

        private GameObject tasksCard;
        private RectTransform tasksBody;
        private readonly List<Text> taskLines = new List<Text>();
        private bool tasksHidden;
        private string tasksShown;

        private void BuildTasks()
        {
            var topLeft = new Vector2(0f, 1f);
            var card = UiKit.Card(canvas.transform, "Tasks", new Color(0.97f, 0.96f, 0.91f, 0.82f),
                new Vector2(470f, 58f + TaskRows * TaskRowHeight), topLeft, new Vector2(24f, -24f - HudHeight - 18f));
            tasksCard = card.parent.gameObject;
            tasksBody = card;
            var title = UiKit.Label(card, "Tareas de hoy  ·  Tab", 24, UiKit.Ink, new Vector2(430f, 36f), topLeft, new Vector2(20f, -10f));
            title.font = UiKit.TitleFont;
            for (int i = 0; i < TaskRows; i++)
            {
                taskLines.Add(UiKit.PlainLabel(card, "Task" + i, string.Empty, 22, UiKit.Ink, new Vector2(430f, TaskRowHeight), topLeft,
                    new Vector2(20f, -50f - i * TaskRowHeight)));
            }

            tasksCard.SetActive(false);
        }

        private void TickTasks()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && !phone.IsTyping && !negotiationPanel.IsOpen)
            {
                tasksHidden = !tasksHidden;
            }

            bool free = !modal.IsOpen && !phone.IsOpen && !pause.IsOpen && !negotiationPanel.IsOpen && !openingPlaying && !LoadingScreen.IsLoading;
            var items = TaskList.For(session.Loop);
            bool show = free && !tasksHidden && items.Count > 0;
            tasksCard.SetActive(show);
            if (!show)
            {
                return;
            }

            // The lines are written again only when something changed.
            string key = Loc.Current.ToString();
            foreach (var item in items)
            {
                key += item.Text + item.Done;
            }

            if (key == tasksShown)
            {
                return;
            }

            tasksShown = key;
            for (int i = 0; i < taskLines.Count; i++)
            {
                bool used = i < items.Count;
                taskLines[i].gameObject.SetActive(used);
                if (used)
                {
                    taskLines[i].text = (items[i].Done ? "[x]  " : "[  ]  ") + Loc.T(items[i].Text);
                    taskLines[i].color = items[i].Done ? new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.45f) : UiKit.Ink;
                }
            }

            LayOutTasks();
        }

        /// <summary>
        /// Stacks the lines one under the other with the height each one really needs, so a task that wraps to two lines
        /// (often in English) pushes the next one down instead of writing over it, and the card grows to hold them all.
        /// </summary>
        private void LayOutTasks()
        {
            const float width = 430f;
            float y = -50f;
            foreach (var line in taskLines)
            {
                if (!line.gameObject.activeSelf)
                {
                    continue;
                }

                float height = TaskLineHeight(UiKit.MeasureHeight(line, width));
                var rect = line.rectTransform;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(20f, y);
                y -= height;
            }

            UiKit.ResizeCard(tasksBody, new Vector2(470f, -y + 14f));
        }

        /// <summary>The room a task line takes: what its text needs plus a small gap, never less than one row.</summary>
        public static float TaskLineHeight(float textHeight)
        {
            return Mathf.Max(TaskRowHeight, Mathf.Ceil(textHeight) + 6f);
        }
    }
}

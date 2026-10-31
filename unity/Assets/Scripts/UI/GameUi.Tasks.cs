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
        private readonly List<Text> taskLines = new List<Text>();
        private bool tasksHidden;
        private string tasksShown;

        private void BuildTasks()
        {
            var topLeft = new Vector2(0f, 1f);
            var card = UiKit.Card(canvas.transform, "Tasks", new Color(0.97f, 0.96f, 0.91f, 0.82f),
                new Vector2(470f, 58f + TaskRows * TaskRowHeight), topLeft, new Vector2(24f, -372f));
            tasksCard = card.parent.gameObject;
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
        }
    }
}

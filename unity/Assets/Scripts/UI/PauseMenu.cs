using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The pause window opened with Esc in the town: resume, settings or back to the main menu. Leaving
    /// the run asks first whether to save it: "Sí" saves and leaves, "No" only leaves.
    /// </summary>
    public sealed class PauseMenu
    {
        public const string MenuLabel = "Volver al menú";
        public const string SureLabel = "¿Seguro? Se pierde la partida, ¿quieres guardar?";
        public const string YesLabel = "Sí";
        public const string NoLabel = "No";

        private readonly GameObject root;
        private readonly SettingsPanel settings;
        private readonly GameObject confirm;
        private readonly Action onResume;
        private readonly Action onMenu;
        private readonly Action onSave;
        private readonly Action onDiscard;

        public PauseMenu(Transform canvas, Action resume, Action menu, Action save = null, Action discard = null)
        {
            onResume = resume;
            onMenu = menu;
            onSave = save;
            onDiscard = discard;

            root = UiKit.Overlay(canvas, "Pause");
            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(760f, 640f), center, Vector2.zero);
            UiKit.Label(card, "Pausa", 60, UiKit.Ink, new Vector2(680f, 80f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.MiddleCenter);

            var top = new Vector2(0.5f, 1f);
            UiKit.MakeButton(card, "Reanudar", GameUiColors.Green, new Vector2(620f, 90f), top, new Vector2(0f, -150f), Resume);
            UiKit.MakeButton(card, "Ajustes", GameUiColors.Yellow, new Vector2(620f, 90f), top, new Vector2(0f, -266f), OpenSettings);
            UiKit.MakeButton(card, MenuLabel, GameUiColors.Pink, new Vector2(620f, 90f), top, new Vector2(0f, -382f), AskToLeave);
            root.SetActive(false);

            // Leaving loses the run unless it is saved, so the window asks before going to the menu.
            confirm = UiKit.Overlay(canvas, "PauseConfirm");
            var sure = UiKit.Card(confirm.transform, "Card", UiKit.Paper, new Vector2(760f, 420f), center, Vector2.zero);
            UiKit.Label(sure, SureLabel, 40, UiKit.Ink, new Vector2(660f, 140f), top, new Vector2(0f, -40f), TextAnchor.MiddleCenter);
            UiKit.MakeButton(sure, YesLabel, GameUiColors.Green, new Vector2(300f, 90f), top, new Vector2(-165f, -230f), () => LeaveRun(true));
            UiKit.MakeButton(sure, NoLabel, GameUiColors.Pink, new Vector2(300f, 90f), top, new Vector2(165f, -230f), () => LeaveRun(false));
            confirm.SetActive(false);

            settings = new SettingsPanel(canvas, ShowCard);
        }

        public bool IsOpen => root.activeSelf || confirm.activeSelf || settings.IsOpen;

        public void Open()
        {
            confirm.SetActive(false);
            settings.Hide();
            root.SetActive(true);
        }

        /// <summary>Closes the window without telling anyone, for when the run restarts or the scene changes.</summary>
        public void Hide()
        {
            root.SetActive(false);
            confirm.SetActive(false);
            settings.Hide();
        }

        /// <summary>What Esc does: go back one step, and from the pause window itself, resume.</summary>
        public void Back()
        {
            if (settings.IsOpen)
            {
                settings.Back();
            }
            else if (confirm.activeSelf)
            {
                confirm.SetActive(false);
                root.SetActive(true);
            }
            else
            {
                Resume();
            }
        }

        private void Resume()
        {
            Hide();
            onResume?.Invoke();
        }

        private void OpenSettings()
        {
            root.SetActive(false);
            settings.Show();
        }

        private void ShowCard()
        {
            root.SetActive(true);
        }

        private void AskToLeave()
        {
            root.SetActive(false);
            confirm.SetActive(true);
        }

        private void LeaveRun(bool save)
        {
            if (save)
            {
                onSave?.Invoke();
            }
            else
            {
                onDiscard?.Invoke();
            }

            Hide();
            onMenu?.Invoke();
        }
    }
}

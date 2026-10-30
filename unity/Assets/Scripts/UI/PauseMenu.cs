using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The pause window opened with Esc in the town: resume, settings or back to the main menu. Leaving
    /// the run takes two presses, because the run is lost; the second one has to follow the first.
    /// </summary>
    public sealed class PauseMenu
    {
        public const string MenuLabel = "Volver al menú";
        public const string SureLabel = "¿Seguro? Se pierde la partida";

        private readonly GameObject root;
        private readonly SettingsPanel settings;
        private readonly Button menuButton;
        private readonly Action onResume;
        private readonly Action onMenu;
        private bool armed;

        public PauseMenu(Transform canvas, Action resume, Action menu)
        {
            onResume = resume;
            onMenu = menu;

            root = UiKit.Overlay(canvas, "Pause");
            var center = new Vector2(0.5f, 0.5f);
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(760f, 640f), center, Vector2.zero);
            UiKit.Label(card, "Pausa", 60, UiKit.Ink, new Vector2(680f, 80f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.MiddleCenter);

            var top = new Vector2(0.5f, 1f);
            UiKit.MakeButton(card, "Reanudar", GameUiColors.Green, new Vector2(620f, 90f), top, new Vector2(0f, -150f), Resume);
            UiKit.MakeButton(card, "Ajustes", GameUiColors.Yellow, new Vector2(620f, 90f), top, new Vector2(0f, -266f), OpenSettings);
            menuButton = UiKit.MakeButton(card, MenuLabel, GameUiColors.Pink, new Vector2(620f, 90f), top, new Vector2(0f, -382f), LeaveRun);
            root.SetActive(false);

            settings = new SettingsPanel(canvas, ShowCard);
        }

        public bool IsOpen => root.activeSelf || settings.IsOpen;

        public void Open()
        {
            armed = false;
            UiKit.SetLabel(menuButton, MenuLabel);
            settings.Hide();
            root.SetActive(true);
        }

        /// <summary>Closes the window without telling anyone, for when the run restarts or the scene changes.</summary>
        public void Hide()
        {
            root.SetActive(false);
            settings.Hide();
        }

        /// <summary>What Esc does: go back one step, and from the pause window itself, resume.</summary>
        public void Back()
        {
            if (settings.IsOpen)
            {
                settings.Back();
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

        private void LeaveRun()
        {
            if (!armed)
            {
                armed = true;
                UiKit.SetLabel(menuButton, SureLabel);
                return;
            }

            Hide();
            onMenu?.Invoke();
        }
    }
}

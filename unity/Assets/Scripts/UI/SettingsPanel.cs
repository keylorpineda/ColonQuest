using System;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The settings window, shared by the main menu and the pause menu: sound on or off and the volume.
    /// Every change is applied to the audio at once and written to storage.
    /// </summary>
    public sealed class SettingsPanel
    {
        private const float CardWidth = 760f;
        private const float BarWidth = 300f;

        private readonly GameObject root;
        private readonly Action onBack;
        private readonly Button soundButton;
        private readonly Button languageButton;
        private readonly RectTransform volumeFill;
        private readonly Text volumeText;

        public SettingsPanel(Transform parent, Action back)
        {
            onBack = back;
            root = UiKit.Overlay(parent, "Settings");
            var card = UiKit.Card(root.transform, "Card", UiKit.Paper, new Vector2(CardWidth, 700f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var top = new Vector2(0.5f, 1f);

            UiKit.Label(card, "Ajustes", 56, UiKit.Ink, new Vector2(CardWidth - 80f, 70f), top, new Vector2(0f, -30f), TextAnchor.MiddleCenter);

            languageButton = UiKit.MakeButton(card, "Idioma", GameUiColors.Blue, new Vector2(520f, 84f), top, new Vector2(0f, -130f), ToggleLanguage);
            soundButton = UiKit.MakeButton(card, "Sonido", GameUiColors.Green, new Vector2(520f, 84f), top, new Vector2(0f, -240f), ToggleSound);

            UiKit.Label(card, "Volumen", 34, GameUiColors.Muted, new Vector2(CardWidth - 80f, 44f), top, new Vector2(0f, -362f), TextAnchor.MiddleCenter);
            UiKit.MakeButton(card, "-", GameUiColors.Blue, new Vector2(84f, 84f), top, new Vector2(-250f, -420f), () => ChangeVolume(-1));
            UiKit.MakeButton(card, "+", GameUiColors.Blue, new Vector2(84f, 84f), top, new Vector2(250f, -420f), () => ChangeVolume(1));

            UiKit.Fill(card, "VolumeBack", new Color(0.82f, 0.84f, 0.9f, 1f), new Vector2(BarWidth, 30f), top, new Vector2(0f, -447f));
            volumeFill = UiKit.Fill(card, "VolumeFill", GameUiColors.Gain, new Vector2(BarWidth, 30f), top, new Vector2(-BarWidth * 0.5f, -447f)).rectTransform;
            volumeFill.pivot = new Vector2(0f, 1f);
            volumeText = UiKit.Label(card, string.Empty, 30, UiKit.Ink, new Vector2(200f, 40f), top, new Vector2(0f, -494f), TextAnchor.MiddleCenter);

            UiKit.MakeButton(card, "Volver", GameUiColors.Yellow, new Vector2(360f, 84f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), Back);

            root.SetActive(false);
            Refresh();
        }

        public bool IsOpen => root.activeSelf;

        public void Show()
        {
            root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        /// <summary>Closes the window and tells whoever opened it.</summary>
        public void Back()
        {
            Hide();
            onBack?.Invoke();
        }

        /// <summary>Spanish to English and back. Every label hears the change and writes itself again.</summary>
        private void ToggleLanguage()
        {
            Loc.Set(Loc.Current == GameLanguage.Spanish ? GameLanguage.English : GameLanguage.Spanish);
            Refresh();
        }

        private void ToggleSound()
        {
            var settings = GameSettings.Current;
            settings.SoundOn = !settings.SoundOn;
            Commit(settings);
        }

        private void ChangeVolume(int steps)
        {
            var settings = GameSettings.Current;
            settings.StepVolume(steps);
            Commit(settings);
        }

        private void Commit(GameSettings settings)
        {
            AudioDirector.Apply(settings);
            settings.Save();
            Refresh();
        }

        private void Refresh()
        {
            var settings = GameSettings.Current;
            // Both names stay in their own language, so the player can always find the way back.
            UiKit.SetLabel(languageButton, settings.Language == GameLanguage.English ? "Language: English" : "Idioma: Español");
            UiKit.SetLabel(soundButton, Loc.T(settings.SoundOn ? "Sonido: activado" : "Sonido: desactivado"));
            volumeFill.sizeDelta = new Vector2(BarWidth * settings.Volume, volumeFill.sizeDelta.y);
            volumeText.text = settings.VolumePercent + " %";
        }
    }
}

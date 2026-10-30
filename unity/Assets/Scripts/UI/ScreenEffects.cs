using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>A layer over the whole screen for the big moments: a flash when a car hits and the red and blue lights of the police.</summary>
    public sealed class ScreenEffects
    {
        private readonly Image overlay;

        private Color flashColor;
        private float flashLeft;
        private float flashTotal;
        private float sirenLeft;

        public ScreenEffects(Transform canvas)
        {
            var go = UiKit.Overlay(canvas, "Effects");
            overlay = go.GetComponent<Image>();
            overlay.raycastTarget = false;
            overlay.color = Color.clear;
        }

        /// <summary>The whole screen takes a color and gives it back.</summary>
        public void Flash(Color color, float seconds)
        {
            flashColor = color;
            flashLeft = seconds;
            flashTotal = Mathf.Max(0.01f, seconds);
        }

        /// <summary>Red and blue take turns on the screen, four times a second.</summary>
        public void Siren(float seconds)
        {
            sirenLeft = seconds;
        }

        public void Tick(float dt)
        {
            Color color = Color.clear;
            if (sirenLeft > 0f)
            {
                sirenLeft -= dt;
                bool red = (int)(sirenLeft * 4f) % 2 == 0;
                color = red ? new Color(0.9f, 0.1f, 0.15f, 0.28f) : new Color(0.15f, 0.3f, 0.95f, 0.28f);
            }

            if (flashLeft > 0f)
            {
                flashLeft -= dt;
                var faded = flashColor;
                faded.a *= Mathf.Clamp01(flashLeft / flashTotal);
                color = faded.a > color.a ? faded : color;
            }

            overlay.color = color;
        }
    }
}

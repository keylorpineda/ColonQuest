using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The light of the town over a run: warm early morning at the start, clear around noon, golden in
    /// the afternoon and a blue dusk at the end. The curve is pure so it can be tested.
    /// </summary>
    public static class DayCycle
    {
        private static readonly float[] Times = { 0f, 0.35f, 0.7f, 1f };

        private static readonly Color[] Tints =
        {
            new Color(1f, 0.85f, 0.63f, 0.16f),
            new Color(1f, 0.96f, 0.85f, 0f),
            new Color(1f, 0.69f, 0.44f, 0.14f),
            new Color(0.35f, 0.42f, 0.75f, 0.26f)
        };

        /// <summary>How far through the run it is, from 0 (first morning) to 1 (last evening).</summary>
        public static float ProgressOf(int monthsPlayed, int tasksDone, int monthsToPlay, int tasksPerMonth)
        {
            if (monthsToPlay <= 0 || tasksPerMonth <= 0)
            {
                return 0f;
            }

            float month = monthsPlayed + Mathf.Clamp01(tasksDone / (float)tasksPerMonth);
            return Mathf.Clamp01(month / monthsToPlay);
        }

        // The sun as a light (used when the town is lit by URP 2D lights instead of the colored wash). Same day as the
        // tints: progress 0 is 06:00 and 1 is 22:00, so 0.75 is 18:00, when the street lights come on.
        private static readonly float[] LightTimes = { 0f, 0.3f, 0.62f, 0.75f, 0.88f, 1f };

        private static readonly Color[] LightColors =
        {
            new Color(1f, 0.86f, 0.72f),
            new Color(1f, 1f, 0.97f),
            new Color(1f, 0.94f, 0.84f),
            new Color(1f, 0.74f, 0.52f),
            new Color(0.66f, 0.64f, 0.92f),
            new Color(0.5f, 0.56f, 0.9f)
        };

        private static readonly float[] LightIntensities = { 0.92f, 1f, 1f, 0.86f, 0.66f, 0.55f };

        /// <summary>The darkest the town gets, so the streets, the people and the signs always stay readable.</summary>
        public const float MinLightIntensity = 0.55f;

        /// <summary>
        /// The color and strength of the sunlight for a moment of the day: a warm morning, a white noon, a golden sunset
        /// at six and a blue night at ten. The intensity never drops below <see cref="MinLightIntensity"/>.
        /// </summary>
        public static void LightFor(float progress, out Color color, out float intensity)
        {
            progress = Mathf.Clamp01(progress);
            for (int i = 1; i < LightTimes.Length; i++)
            {
                if (progress <= LightTimes[i])
                {
                    float t = Mathf.InverseLerp(LightTimes[i - 1], LightTimes[i], progress);
                    color = Color.Lerp(LightColors[i - 1], LightColors[i], t);
                    intensity = Mathf.Max(MinLightIntensity, Mathf.Lerp(LightIntensities[i - 1], LightIntensities[i], t));
                    return;
                }
            }

            color = LightColors[LightColors.Length - 1];
            intensity = Mathf.Max(MinLightIntensity, LightIntensities[LightIntensities.Length - 1]);
        }

        /// <summary>
        /// The brightness of a night light: how far it has come on (0 to 1) times its full intensity, with a faint
        /// flicker from a smooth noise (0 to 1). Never negative.
        /// </summary>
        public static float LampIntensity(float level01, float onIntensity, float flickerAmount, float noise01)
        {
            float wobble = 1f + (Mathf.Clamp01(noise01) - 0.5f) * 2f * Mathf.Clamp01(flickerAmount);
            return Mathf.Max(0f, Mathf.Clamp01(level01) * Mathf.Max(0f, onIntensity) * wobble);
        }

        public static Color TintFor(float progress)
        {
            progress = Mathf.Clamp01(progress);
            for (int i = 1; i < Times.Length; i++)
            {
                if (progress <= Times[i])
                {
                    float t = Mathf.InverseLerp(Times[i - 1], Times[i], progress);
                    return Color.Lerp(Tints[i - 1], Tints[i], t);
                }
            }

            return Tints[Tints.Length - 1];
        }
    }
}

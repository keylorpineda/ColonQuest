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

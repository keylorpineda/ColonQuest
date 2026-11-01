using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The numbers behind the moving water: the foam that laps the beach, the swells that roll to the shore, the
    /// streaks that the river carries and the rings that spread and fade. No scene objects, so it is easy to test.
    /// </summary>
    public static class WaterMath
    {
        /// <summary>
        /// The reach of a lapping wave, from -1 (pulled back) to 1 (furthest up the beach). The water rushes up and slides
        /// back more slowly, which reads as a real wave rather than a pendulum.
        /// </summary>
        public static float Lap(float time, float period, float phase)
        {
            if (period <= 0f)
            {
                return 0f;
            }

            float t = Mathf.Repeat(time / period + phase, 1f);
            // A third of the cycle going up (eased out), two thirds coming down (eased in and out).
            if (t < 1f / 3f)
            {
                float up = t * 3f;
                return -1f + 2f * (1f - (1f - up) * (1f - up));
            }

            float down = (t - 1f / 3f) * 1.5f;
            return 1f - 2f * (down * down * (3f - 2f * down));
        }

        /// <summary>How far through its life a looping thing is, from 0 to 1, offset so many of them do not move together.</summary>
        public static float Cycle(float time, float period, float offset)
        {
            return period <= 0f ? 0f : Mathf.Repeat(time / period + offset, 1f);
        }

        /// <summary>Fades in at the start of a life and out at the end: 0 at both ends, 1 in the middle.</summary>
        public static float FadeInOut(float t01)
        {
            float t = Mathf.Clamp01(t01);
            return Mathf.Sin(t * Mathf.PI);
        }

        /// <summary>
        /// The opacity of a streak carried by the river near the ends of its run: it fades out over
        /// <paramref name="fade"/> units before the bottom and fades in after the top, so it never pops.
        /// </summary>
        public static float EdgeFade(float y, float minY, float maxY, float fade)
        {
            if (fade <= 0f)
            {
                return y >= minY && y <= maxY ? 1f : 0f;
            }

            float fromBottom = Mathf.Clamp01((y - minY) / fade);
            float fromTop = Mathf.Clamp01((maxY - y) / fade);
            return Mathf.Min(fromBottom, fromTop);
        }

        /// <summary>A ring of water: it grows fast at first and slower later, and fades as it grows.</summary>
        public static void Ripple(float t01, float startSize, float endSize, out float size, out float alpha)
        {
            float t = Mathf.Clamp01(t01);
            float eased = 1f - (1f - t) * (1f - t);
            size = Mathf.Lerp(startSize, endSize, eased);
            alpha = 1f - t;
        }

        /// <summary>The gentle rocking of a boat or a buoy, in degrees.</summary>
        public static float Rock(float time, float degrees, float period, float phase)
        {
            if (period <= 0f)
            {
                return 0f;
            }

            float w = Mathf.PI * 2f / period;
            return degrees * (Mathf.Sin(time * w + phase) * 0.8f + Mathf.Sin(time * w * 2.3f + phase * 1.7f) * 0.2f);
        }
    }
}

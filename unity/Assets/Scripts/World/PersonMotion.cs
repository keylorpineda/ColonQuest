using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The numbers behind the people animations: easing curves in the style of vector (SVG) animation
    /// for walking, waving, blinking, talking and looking at someone. No scene objects, so it is easy to test.
    /// </summary>
    public static class PersonMotion
    {
        public const float WaveDuration = 1.9f;

        /// <summary>The swing of a leg, in degrees. The two legs move in opposite directions.</summary>
        public static float LegAngle(float phase, float moving, bool left)
        {
            return (left ? 1f : -1f) * Mathf.Sin(phase) * 26f * moving;
        }

        /// <summary>The swing of an arm, opposite to the leg on the same side.</summary>
        public static float ArmAngle(float phase, float moving, bool left)
        {
            return -LegAngle(phase, moving, left) * 0.9f;
        }

        /// <summary>How high a foot lifts while it is the one stepping forward.</summary>
        public static float FootLift(float phase, float moving, bool left)
        {
            return Mathf.Max(0f, (left ? 1f : -1f) * Mathf.Sin(phase)) * 0.07f * moving;
        }

        /// <summary>
        /// The raised arm of a greeting, in degrees, <paramref name="seconds"/> after it started: it lifts with an
        /// ease-out, shakes from side to side, then lowers. Outside of the greeting it returns 0.
        /// </summary>
        public static float WaveAngle(float seconds)
        {
            if (seconds < 0f || seconds > WaveDuration)
            {
                return 0f;
            }

            const float lift = 0.3f;
            const float lower = 0.3f;
            const float raised = 150f;

            if (seconds < lift)
            {
                float t = seconds / lift;
                return raised * (1f - (1f - t) * (1f - t));
            }

            if (seconds > WaveDuration - lower)
            {
                float t = (seconds - (WaveDuration - lower)) / lower;
                return raised * (1f - t * t);
            }

            return raised + Mathf.Sin((seconds - lift) * 9f * Mathf.PI) * 18f;
        }

        public static bool IsWaving(float seconds)
        {
            return seconds >= 0f && seconds <= WaveDuration;
        }

        /// <summary>How closed the eyelids are: 0 is open, 1 is shut. A blink takes a fifth of a second.</summary>
        public static float BlinkClosed(float sinceBlinkStarted)
        {
            const float duration = 0.18f;
            if (sinceBlinkStarted < 0f || sinceBlinkStarted > duration)
            {
                return 0f;
            }

            float half = duration * 0.5f;
            float t = sinceBlinkStarted < half ? sinceBlinkStarted / half : (duration - sinceBlinkStarted) / half;
            return t * t * (3f - 2f * t);
        }

        public static float NextBlinkDelay(float random01)
        {
            return 1.8f + Mathf.Clamp01(random01) * 3.4f;
        }

        /// <summary>
        /// Where the pupils move to look at something. Far away targets pull them all the way, near ones
        /// less, and a target on top of the character leaves them centered.
        /// </summary>
        public static Vector2 PupilOffset(Vector2 toTarget, float max)
        {
            float distance = toTarget.magnitude;
            if (distance < 0.01f)
            {
                return Vector2.zero;
            }

            return toTarget / distance * max * Mathf.Clamp01(distance / 2.5f);
        }

        /// <summary>The height of the mouth, from 0.25 (closed) to 1 (open). It moves like speech while talking.</summary>
        public static float MouthOpen(float time, bool talking)
        {
            if (!talking)
            {
                return 0.25f;
            }

            float syllable = Mathf.Abs(Mathf.Sin(time * 9f)) * (0.6f + 0.4f * Mathf.Sin(time * 3.1f));
            return Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(syllable));
        }

        /// <summary>The tilt of the head toward someone to its side, in degrees, limited to a small turn.</summary>
        public static float HeadTilt(float toTargetX)
        {
            return Mathf.Clamp(-toTargetX * 2.2f, -9f, 9f);
        }
    }
}

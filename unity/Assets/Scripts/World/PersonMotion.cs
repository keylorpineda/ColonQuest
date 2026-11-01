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

        /// <summary>
        /// The extra bend of the elbow of the waving arm, in degrees: it follows the lift and the lowering of the
        /// wave, reaches about 40 degrees and shakes a little. Outside of the greeting it returns 0.
        /// </summary>
        public static float ElbowAngle(float seconds)
        {
            if (!IsWaving(seconds))
            {
                return 0f;
            }

            float lifted = Mathf.Clamp01(WaveAngle(seconds) / 150f);
            return lifted * (40f + Mathf.Sin(seconds * 9f * Mathf.PI) * 8f);
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

        /// <summary>Slower than this (units per second) a person is standing, not walking away.</summary>
        public const float AwaySpeed = 0.6f;

        /// <summary>
        /// True while a person walks away from the camera (up the screen), so the back of the head shows. It needs a
        /// clear upward walk to turn around and only gives it up when the walk is clearly something else, so walking
        /// diagonally does not flicker between the face and the back.
        /// </summary>
        public static bool FacingAway(Vector2 velocity, bool wasAway)
        {
            float up = velocity.y;
            float side = Mathf.Abs(velocity.x);
            if (wasAway)
            {
                return up > AwaySpeed * 0.5f && up > side * 0.6f;
            }

            return up > AwaySpeed && up > side * 1.1f;
        }

        /// <summary>
        /// The height of the body with every step: a little shorter as the foot lands, a little taller between steps.
        /// <paramref name="step"/> is |sin(phase)|: 0 when a foot lands, 1 between the steps.
        /// </summary>
        public static float StepSquash(float step, float moving)
        {
            return 1f + (Mathf.Clamp01(step) - 0.4f) * 0.05f * Mathf.Clamp01(moving);
        }

        /// <summary>
        /// The lean of the torso into the walk, in degrees, in the person's own (possibly mirrored) space: negative leans
        /// toward the way they face. Only the sideways part of the walk leans, so walking up or down stays upright.
        /// </summary>
        public static float WalkLean(float moving, float sideways01)
        {
            return -3f * Mathf.Clamp01(moving) * Mathf.Clamp01(sideways01);
        }
    }
}

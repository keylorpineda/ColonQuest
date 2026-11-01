using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>The movement of the main menu backdrop: clouds that drift, coins that bob, the toucan that flies in and the volcano smoke.</summary>
    public static class MenuMotion
    {
        /// <summary>
        /// Moves a cloud to the right. Once it is past <paramref name="max"/> it comes back in at
        /// <paramref name="min"/>, keeping what it went over.
        /// </summary>
        public static float Drift(float x, float speed, float deltaSeconds, float min, float max)
        {
            float next = x + speed * deltaSeconds;
            if (max <= min)
            {
                return min;
            }

            float span = max - min;
            while (next > max)
            {
                next -= span;
            }

            while (next < min)
            {
                next += span;
            }

            return next;
        }

        /// <summary>How far a coin is from its resting height at one moment; it stays within the amplitude.</summary>
        public static float Bob(float seconds, float phase, float amplitude)
        {
            return Mathf.Sin(seconds * 2f + phase) * amplitude;
        }
    

        /// <summary>A point of a cubic curve from <paramref name="p0"/> to <paramref name="p3"/>, bent by the two middle points.</summary>
        public static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        /// <summary>Fast at first and gentle at the end, like a bird braking to land: 0 at the start, 1 at the end.</summary>
        public static float GlideEase(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>The angle of a beating wing, in degrees: <paramref name="lift"/> plus a swing of <paramref name="amplitude"/>.</summary>
        public static float FlapAngle(float seconds, float beatsPerSecond, float amplitude, float lift)
        {
            return lift + amplitude * Mathf.Sin(seconds * beatsPerSecond * Mathf.PI * 2f);
        }

        /// <summary>
        /// The squash of a bird after it lands, as its height: it sinks a little on the branch and springs back, settling to
        /// exactly 1 within half a second.
        /// </summary>
        public static float LandingSquash(float secondsSinceLanding)
        {
            if (secondsSinceLanding < 0f || secondsSinceLanding > 0.6f)
            {
                return 1f;
            }

            return 1f - 0.09f * Mathf.Exp(-7f * secondsSinceLanding) * Mathf.Cos(secondsSinceLanding * 20f);
        }

        /// <summary>A short ease out and back: 0 at both ends and 1 in the middle, for a head tilt or a hop.</summary>
        public static float Pulse(float t01)
        {
            float t = Mathf.Clamp01(t01);
            return Mathf.Sin(t * Mathf.PI);
        }

        /// <summary>
        /// Where a puff of volcano smoke is after <paramref name="t01"/> of its life, in pixels of the key art from the crater:
        /// it rises, wavers a little and bends to the right with the wind, like the column in the drawing.
        /// </summary>
        public static Vector2 SmokeOffset(float t01, float sway)
        {
            float t = Mathf.Clamp01(t01);
            float x = Mathf.Sin(t * 6.5f + sway) * 22f * t + Mathf.Pow(t, 1.6f) * 180f;
            float y = t * 235f;
            return new Vector2(x, y);
        }

        /// <summary>The size of a puff over its life: small out of the crater, wide high up.</summary>
        public static float SmokeScale(float t01)
        {
            return Mathf.Lerp(0.12f, 0.72f, Mathf.Pow(Mathf.Clamp01(t01), 0.8f));
        }

        /// <summary>The opacity of a puff over its life: it appears quickly and thins away in its last third.</summary>
        public static float SmokeAlpha(float t01)
        {
            float t = Mathf.Clamp01(t01);
            if (t < 0.08f)
            {
                return t / 0.08f;
            }

            return t > 0.65f ? Mathf.Clamp01(1f - (t - 0.65f) / 0.35f) : 1f;
        }
    }
}

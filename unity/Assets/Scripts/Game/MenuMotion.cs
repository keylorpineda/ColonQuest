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
        /// How far the smoke column of the key art bends at a height over the crater, in pixels of the art: nothing at the
        /// crater and more the higher it is, with waves that climb the column and a slow lean with the wind.
        /// </summary>
        public static float SmokeSway(float heightPx, float seconds)
        {
            float reach = Mathf.Clamp01(heightPx / 170f);
            float wave = Mathf.Sin((heightPx / 95f - seconds / 3.4f) * Mathf.PI * 2f) * 7f
                + Mathf.Sin((heightPx / 47f - seconds / 2.1f) * Mathf.PI * 2f + 1.3f) * 2f;
            float lean = Mathf.Sin(seconds * 0.4f) * 4f * reach;
            return reach * wave + lean * reach;
        }

        // The middle of the drawn column every 15 pixels of height, measured on the key art.
        private static readonly float[] SmokeAxisPx = { 0f, 2f, 2f, 6f, 6f, 10f, 30f, 55f, 67f, 78f, 88f, 104f };

        /// <summary>The middle of the drawn column at a height over the crater, in pixels of the art from the crater.</summary>
        public static float SmokeAxis(float heightPx)
        {
            float at = Mathf.Clamp(heightPx / 15f, 0f, SmokeAxisPx.Length - 1f);
            int low = Mathf.Min((int)at, SmokeAxisPx.Length - 2);
            return Mathf.Lerp(SmokeAxisPx[low], SmokeAxisPx[low + 1], at - low);
        }

        /// <summary>The size of a wisp over its life: it stays narrower than the column it rises along.</summary>
        public static float WispScale(float t01)
        {
            return Mathf.Lerp(0.05f, 0.27f, Mathf.Clamp01(t01));
        }

        /// <summary>The opacity of a wisp over its life: faint, so the drawn column still reads under it.</summary>
        public static float WispAlpha(float t01)
        {
            return Pulse(t01) * 0.5f;
        }

        /// <summary>
        /// One step of a spring toward a target, for a head that moves with weight: it gets there quickly, passes it a
        /// little and settles.
        /// </summary>
        public static void SpringStep(ref float value, ref float velocity, float target, float stiffness, float damping, float deltaSeconds)
        {
            velocity += (stiffness * (target - value) - damping * velocity) * deltaSeconds;
            value += velocity * deltaSeconds;
        }

        /// <summary>The small movement a resting head never stops making, in degrees.</summary>
        public static float HeadDrift(float seconds)
        {
            return Mathf.Sin(seconds * 1.3f) * 1.1f + Mathf.Sin(seconds * 2.9f + 1f) * 0.5f;
        }
    }
}

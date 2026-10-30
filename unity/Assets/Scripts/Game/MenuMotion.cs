using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>The movement of the main menu backdrop: clouds that drift across and coins that bob.</summary>
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
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Pure movement rules, kept apart from MonoBehaviours so they can be unit tested.</summary>
    public static class MovementMath
    {
        /// <summary>
        /// Velocity for a raw input vector. Input longer than 1 (diagonals on a keyboard)
        /// is clamped, so moving diagonally is never faster than moving straight.
        /// </summary>
        public static Vector2 Velocity(Vector2 input, float speed)
        {
            return Vector2.ClampMagnitude(input, 1f) * speed;
        }

        /// <summary>1 when facing right, -1 when facing left. Keeps the current side while idle.</summary>
        public static int FacingSign(float horizontalInput, int current)
        {
            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
                return current;
            }

            return horizontalInput > 0f ? 1 : -1;
        }
    }
}

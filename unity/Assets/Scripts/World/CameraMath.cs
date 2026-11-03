using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Pure camera rules, kept apart from MonoBehaviours so they can be unit tested.</summary>
    public static class CameraMath
    {
        /// <summary>
        /// Keeps a camera of the given half size inside the bounds. When the view is larger
        /// than the bounds on an axis, the camera is centered on that axis instead.
        /// </summary>
        public static Vector2 ClampCenter(Vector2 desired, Vector2 halfView, Rect bounds)
        {
            return new Vector2(
                ClampAxis(desired.x, halfView.x, bounds.xMin, bounds.xMax),
                ClampAxis(desired.y, halfView.y, bounds.yMin, bounds.yMax));
        }

        private static float ClampAxis(float value, float half, float min, float max)
        {
            if (half * 2f >= max - min)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + half, max - half);
        }
    }
}

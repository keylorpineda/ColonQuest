using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Turns a wheel by how far the car really moved, so it slows and stops with the car at a light.</summary>
    public sealed class WheelSpin : MonoBehaviour
    {
        /// <summary>Less than this in a frame is a car that stands still, not one that rolls.</summary>
        private const float StillBelow = 0.0001f;

        [SerializeField] private float radius = 0.31f;

        private float lastX;

        public void Configure(float wheelRadius)
        {
            radius = wheelRadius;
        }

        private void OnEnable()
        {
            lastX = transform.position.x;
        }

        private void LateUpdate()
        {
            float x = transform.position.x;
            float moved = x - lastX;
            lastX = x;
            if (Mathf.Abs(moved) > StillBelow)
            {
                // A car that faces west is mirrored, so its wheels turn the same way on its own drawing.
                float forward = moved * Mathf.Sign(transform.lossyScale.x);
                transform.Rotate(0f, 0f, CarRules.WheelTurnDegrees(forward, radius));
            }
        }
    }
}

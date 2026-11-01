using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Turns a wheel by how far the car really moved, so it slows and stops with the car at a light.</summary>
    public sealed class WheelSpin : MonoBehaviour
    {
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
            if (moved != 0f)
            {
                // A car that faces west is mirrored, so its wheels turn the same way on its own drawing.
                float forward = moved * Mathf.Sign(transform.lossyScale.x);
                transform.Rotate(0f, 0f, CarRules.WheelTurnDegrees(forward, radius));
            }
        }
    }
}

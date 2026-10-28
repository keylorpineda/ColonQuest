using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Slides an object at a constant velocity and wraps it around, like a cloud shadow crossing the town.</summary>
    public sealed class Drift : MonoBehaviour
    {
        [SerializeField] private Vector2 velocity = new Vector2(0.4f, 0.05f);
        [SerializeField] private Vector2 halfRange = new Vector2(45f, 28f);

        public void Configure(Vector2 speed, Vector2 range)
        {
            velocity = speed;
            halfRange = range;
        }

        private void Update()
        {
            Vector3 position = transform.localPosition;
            position.x += velocity.x * Time.deltaTime;
            position.y += velocity.y * Time.deltaTime;

            if (position.x > halfRange.x)
            {
                position.x = -halfRange.x;
            }
            else if (position.x < -halfRange.x)
            {
                position.x = halfRange.x;
            }

            if (position.y > halfRange.y)
            {
                position.y = -halfRange.y;
            }
            else if (position.y < -halfRange.y)
            {
                position.y = halfRange.y;
            }

            transform.localPosition = position;
        }
    }
}

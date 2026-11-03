using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Slides an object at a constant velocity and wraps it around, like a cloud shadow crossing the town.</summary>
    public sealed class Drift : MonoBehaviour
    {
        [SerializeField] private Vector2 velocity = new Vector2(0.4f, 0.05f);
        [SerializeField] private Vector2 halfRange = new Vector2(45f, 28f);

        [SerializeField] private bool laneWrap;
        [SerializeField] private float laneMinX;
        [SerializeField] private float laneMaxX;

        [SerializeField] private bool span;
        [SerializeField] private float spanMinX;
        [SerializeField] private float spanMaxX;

        public void Configure(Vector2 speed, Vector2 range)
        {
            velocity = speed;
            halfRange = range;
        }

        /// <summary>
        /// A car on its lane: it wraps around only beyond both ends of the walkable map, so it leaves the street out of
        /// sight instead of vanishing where the player can see it.
        /// </summary>
        public void ConfigureLane(float speedX, float minX, float maxX)
        {
            velocity = new Vector2(speedX, 0f);
            laneWrap = true;
            laneMinX = minX;
            laneMaxX = maxX;
        }

        /// <summary>
        /// A boat on the sea: it sails back and forth between two x positions, turning around at each end, so it never crosses
        /// what stands outside them (the pier).
        /// </summary>
        public void ConfigureSpan(float speedX, float minX, float maxX)
        {
            velocity = new Vector2(speedX, 0f);
            span = true;
            spanMinX = minX;
            spanMaxX = maxX;
        }

        /// <summary>One step of a back-and-forth move: the new x, and the speed turned around when an end was reached.</summary>
        public static float Bounce(float x, ref float speed, float deltaTime, float minX, float maxX)
        {
            x += speed * deltaTime;
            if (x > maxX)
            {
                x = maxX - (x - maxX);
                speed = -Mathf.Abs(speed);
            }
            else if (x < minX)
            {
                x = minX + (minX - x);
                speed = Mathf.Abs(speed);
            }

            return Mathf.Clamp(x, minX, maxX);
        }

        private float laneSpeed;

        private void OnEnable()
        {
            if (laneWrap)
            {
                laneSpeed = Mathf.Abs(velocity.x);
                Traffic.Register(transform);
            }
        }

        private void OnDisable()
        {
            Traffic.Unregister(transform);
        }

        private void Update()
        {
            Vector3 position = transform.localPosition;
            if (span)
            {
                position.x = Bounce(position.x, ref velocity.x, Time.deltaTime, spanMinX, spanMaxX);
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (velocity.x >= 0f ? 1f : -1f);
                transform.localScale = scale;
                transform.localPosition = position;
                return;
            }

            if (laneWrap)
            {
                // A car: it slows down for the lights and the car ahead (see CarRules), then wraps around the map.
                int direction = velocity.x >= 0f ? 1 : -1;
                float target = CarRules.TargetSpeed(Mathf.Abs(velocity.x), Traffic.RoomAhead(transform, direction));
                laneSpeed = CarRules.NextSpeed(laneSpeed, target, Time.deltaTime);
                position.x += direction * laneSpeed * Time.deltaTime;
            }
            else
            {
                position.x += velocity.x * Time.deltaTime;
                position.y += velocity.y * Time.deltaTime;
            }

            if (laneWrap)
            {
                if (position.x > laneMaxX)
                {
                    position.x = laneMinX;
                }
                else if (position.x < laneMinX)
                {
                    position.x = laneMaxX;
                }

                transform.localPosition = position;
                return;
            }

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

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A lily pad (or anything light) on a pond: it drifts in a small slow circle, bobs, and is pushed away when the
    /// player walks up to the edge, then floats back on a soft spring. It never leaves the water.
    /// </summary>
    public sealed class FloatingPad : MonoBehaviour
    {
        [SerializeField] private Rect water;
        [SerializeField] private float drift = 0.08f;
        [SerializeField] private float phase;

        private Vector2 rest;
        private Vector2 offset;
        private Vector2 velocity;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Transform player;
        private float nextPlayerSearch;

        /// <summary>Floats inside <paramref name="pond"/> (world units).</summary>
        public void Configure(Rect pond, float driftRadius, float cyclePhase)
        {
            water = pond;
            drift = driftRadius;
            phase = cyclePhase;
        }

        private void Awake()
        {
            rest = transform.position;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (player == null && Time.time >= nextPlayerSearch)
            {
                nextPlayerSearch = Time.time + 0.5f;
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }

            Vector2 push = player != null ? WaterMath.Push(rest + offset, player.position, 2.2f, 3.5f) : Vector2.zero;

            // A soft spring back to the resting spot, with the water's drag.
            Vector2 acceleration = push - offset * 2.2f - velocity * 1.6f;
            velocity += acceleration * dt;
            offset += velocity * dt;

            float t = Time.time * 0.35f + phase;
            Vector2 circle = new Vector2(Mathf.Cos(t), Mathf.Sin(t) * 0.6f) * drift;
            Vector2 position = WaterMath.KeepInside(rest + offset + circle, water, 0.35f);
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.7f) * 6f + velocity.x * 8f);
            float bob = 1f + Mathf.Sin(Time.time * 1.9f + phase * 3f) * 0.04f;
            // The pad keeps the size it was drawn with and only breathes a little around it.
            transform.localScale = new Vector3(restScale.x * bob, restScale.y * (2f - bob), restScale.z);
        }
    }
}

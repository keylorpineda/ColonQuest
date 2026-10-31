using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Gentle 2D wind sway and reactive spring physics for tree canopies, bushes,
    /// flower boxes, hanging signs, and string-light garlands.
    /// </summary>
    public sealed class FoliagePhysics : MonoBehaviour
    {
        [SerializeField] private float swayDegrees = 3.5f;
        [SerializeField] private float swaySpeed = 1.4f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private float scaleBreath = 0.025f;
        [SerializeField] private bool reactToPlayer = true;
        [SerializeField] private float reactRadius = 1.45f;
        [SerializeField] private float reactImpulseDegrees = 11f;

        private Quaternion baseRotation;
        private Vector3 baseScale;
        private float springAngle;
        private float springVelocity;
        private float lastRustleDistance = float.MaxValue;

        private static Transform cachedPlayer;
        private static float nextPlayerLookupTime;

        public float SwayDegrees => swayDegrees;
        public float SwaySpeed => swaySpeed;
        public bool ReactToPlayer => reactToPlayer;

        public void Configure(
            float degrees,
            float speed,
            float phase,
            float breath = 0.025f,
            bool playerReactive = true,
            float radius = 1.45f,
            float impulseDegrees = 11f)
        {
            swayDegrees = degrees;
            swaySpeed = speed;
            phaseOffset = phase;
            scaleBreath = breath;
            reactToPlayer = playerReactive;
            reactRadius = radius;
            reactImpulseDegrees = impulseDegrees;
        }

        public static float ComputeWindAngle(float time, float speed, float maxDegrees, float phase)
        {
            float primary = Mathf.Sin(time * speed + phase);
            float gust = (Mathf.PerlinNoise(time * speed * 0.45f + phase * 0.37f, phase * 0.73f) - 0.5f) * 1.35f;
            return (primary * 0.72f + gust * 0.28f) * maxDegrees;
        }

        public static Vector3 ComputeWindScale(Vector3 initialScale, float time, float speed, float breathAmount, float phase)
        {
            if (breathAmount <= 0f)
            {
                return initialScale;
            }

            float wave = Mathf.Sin(time * speed * 1.15f + phase);
            float sx = 1f + wave * breathAmount;
            float sy = 1f - wave * breathAmount * 0.65f;
            return new Vector3(initialScale.x * sx, initialScale.y * sy, initialScale.z);
        }

        public static void StepSpring(
            ref float displacement,
            ref float velocity,
            float stiffness,
            float damping,
            float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            float clampedDt = Mathf.Min(dt, 0.05f);
            float acceleration = -stiffness * displacement - damping * velocity;
            velocity += acceleration * clampedDt;
            displacement += velocity * clampedDt;

            if (Mathf.Abs(displacement) < 0.001f && Mathf.Abs(velocity) < 0.001f)
            {
                displacement = 0f;
                velocity = 0f;
            }
        }

        private void Awake()
        {
            baseRotation = transform.localRotation;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float time = Time.time;
            float windAngle = ComputeWindAngle(time, swaySpeed, swayDegrees, phaseOffset);

            if (reactToPlayer)
            {
                UpdatePlayerRustle();
                StepSpring(ref springAngle, ref springVelocity, 42f, 5.5f, Time.deltaTime);
            }

            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, windAngle + springAngle);
            transform.localScale = ComputeWindScale(baseScale, time, swaySpeed, scaleBreath, phaseOffset);
        }

        private void UpdatePlayerRustle()
        {
            Transform player = FindPlayerTransform();
            if (player == null)
            {
                return;
            }

            Vector2 myPos = transform.position;
            Vector2 playerPos = player.position;
            float dist = Vector2.Distance(myPos, playerPos);

            if (dist < reactRadius && lastRustleDistance >= reactRadius * 0.85f && dist < lastRustleDistance - 0.01f)
            {
                float dir = myPos.x >= playerPos.x ? -1f : 1f;
                float strength = Mathf.Clamp01(1f - dist / reactRadius);
                springVelocity += dir * reactImpulseDegrees * (2.8f + strength * 2.2f);
            }

            lastRustleDistance = dist;
        }

        private static Transform FindPlayerTransform()
        {
            if (cachedPlayer != null)
            {
                return cachedPlayer;
            }

            if (Time.unscaledTime < nextPlayerLookupTime)
            {
                return null;
            }

            nextPlayerLookupTime = Time.unscaledTime + 0.5f;
            var controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                cachedPlayer = controller.transform;
            }

            return cachedPlayer;
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Gentle 2D wind sway and reactive spring physics for tree canopies, bushes,
    /// flower boxes, hanging signs, and string-light garlands.
    /// </summary>
    /// <remarks>
    /// Two ways to move. The classic one (signs, flags, garlands, potted plants) swings around the object's own origin
    /// with its own sine. The outdoor plants use the town wind instead (<see cref="UseGlobalWind"/>): one breeze crosses
    /// the whole map from west to east, so neighbouring trees bend one after the other, each one following it through
    /// a soft spring and bending from the foot of its trunk (<see cref="PivotDrop"/>), not from the middle of its crown.
    /// </remarks>
    public sealed class FoliagePhysics : MonoBehaviour
    {
        /// <summary>How stiff the bounce is when the player brushes past.</summary>
        public const float RustleStiffness = 42f;

        /// <summary>Damping of that bounce: about 0.7 of critical, so it settles in one or two swings instead of wobbling.</summary>
        public const float RustleDamping = 9f;

        /// <summary>How fast a gust travels across the town, in units per second.</summary>
        public const float GustSpeed = 6f;

        [SerializeField] private float swayDegrees = 3.5f;
        [SerializeField] private float swaySpeed = 1.4f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private float scaleBreath = 0.025f;
        [SerializeField] private bool reactToPlayer = true;
        [SerializeField] private float reactRadius = 1.45f;
        [SerializeField] private float reactImpulseDegrees = 11f;
        [SerializeField] private bool useGlobalWind;
        [SerializeField] private float pivotDrop;

        private Quaternion baseRotation;
        private Vector3 baseScale;
        private Vector3 basePosition;
        private float springAngle;
        private float springVelocity;
        private float bendAngle;
        private float bendVelocity;
        private bool bendStarted;
        private float lastRustleDistance = float.MaxValue;

        private static Transform cachedPlayer;
        private static float nextPlayerLookupTime;

        public float SwayDegrees => swayDegrees;
        public float SwaySpeed => swaySpeed;
        public bool ReactToPlayer => reactToPlayer;

        /// <summary>True for the plants that follow the breeze of the whole town.</summary>
        public bool UseGlobalWind => useGlobalWind;

        /// <summary>How far below this object it turns, in its parent's units: 0 turns it around its own origin.</summary>
        public float PivotDrop => pivotDrop;

        public void Configure(
            float degrees,
            float speed,
            float phase,
            float breath = 0.025f,
            bool playerReactive = true,
            float radius = 1.45f,
            float impulseDegrees = 11f,
            bool globalWind = false,
            float pivot = 0f)
        {
            swayDegrees = degrees;
            swaySpeed = speed;
            phaseOffset = phase;
            scaleBreath = breath;
            reactToPlayer = playerReactive;
            reactRadius = radius;
            reactImpulseDegrees = impulseDegrees;
            useGlobalWind = globalWind;
            pivotDrop = Mathf.Max(0f, pivot);
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

        /// <summary>
        /// The strength of the town breeze at a place and a time, from 0 (calm) to 1 (a gust). It is the same wave for
        /// everyone, delayed by the distance from the west edge, so a gust visibly rolls across the map.
        /// </summary>
        /// <param name="noise">A smooth noise in 0..1 at the same delayed time (Perlin at runtime; any value in tests).</param>
        public static float GlobalWindStrength(float time, float worldX, float noise)
        {
            float t = time - worldX / GustSpeed;
            float slow = Mathf.Sin(t * 0.33f) * 0.6f + Mathf.Sin(t * 0.91f + 1.7f) * 0.4f;
            float wind = 0.5f + 0.32f * slow + 0.18f * (Mathf.Clamp01(noise) * 2f - 1f);
            return Mathf.Clamp01(wind);
        }

        /// <summary>
        /// Where a plant leans for a breeze: always a little to the east (the wind blows from the west), more in a gust.
        /// Negative degrees turn clockwise, which reads as bending to the right.
        /// </summary>
        public static float WindLean(float strength, float maxDegrees)
        {
            return -maxDegrees * (0.2f + 0.8f * Mathf.Clamp01(strength));
        }

        /// <summary>
        /// The local position of an object turned by <paramref name="degrees"/> around a point <paramref name="pivotDrop"/>
        /// below its resting position, so a crown swings on its trunk instead of spinning in place.
        /// </summary>
        public static Vector3 PivotPosition(Vector3 restPosition, float pivotDrop, float degrees)
        {
            if (pivotDrop <= 0f)
            {
                return restPosition;
            }

            float radians = degrees * Mathf.Deg2Rad;
            // The rest position seen from the pivot is straight up (0, pivotDrop); turning it gives the new offset.
            return new Vector3(
                restPosition.x - Mathf.Sin(radians) * pivotDrop,
                restPosition.y - pivotDrop + Mathf.Cos(radians) * pivotDrop,
                restPosition.z);
        }

        public static void StepSpring(
            ref float displacement,
            ref float velocity,
            float stiffness,
            float damping,
            float dt)
        {
            StepSpringToward(ref displacement, ref velocity, 0f, stiffness, damping, dt);
        }

        /// <summary>One step of a damped spring pulled toward <paramref name="target"/>.</summary>
        public static void StepSpringToward(
            ref float displacement,
            ref float velocity,
            float target,
            float stiffness,
            float damping,
            float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            float clampedDt = Mathf.Min(dt, 0.05f);
            float acceleration = -stiffness * (displacement - target) - damping * velocity;
            velocity += acceleration * clampedDt;
            displacement += velocity * clampedDt;

            if (Mathf.Abs(displacement - target) < 0.001f && Mathf.Abs(velocity) < 0.001f)
            {
                displacement = target;
                velocity = 0f;
            }
        }

        private void Awake()
        {
            baseRotation = transform.localRotation;
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
        }

        private void Update()
        {
            float time = Time.time;
            float dt = Time.deltaTime;
            float windAngle = useGlobalWind ? UpdateGlobalWind(time, dt) : ComputeWindAngle(time, swaySpeed, swayDegrees, phaseOffset);

            if (reactToPlayer)
            {
                UpdatePlayerRustle();
                StepSpring(ref springAngle, ref springVelocity, RustleStiffness, RustleDamping, dt);
            }

            float angle = windAngle + springAngle;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            transform.localScale = ComputeWindScale(baseScale, time, swaySpeed, scaleBreath, phaseOffset);
            if (pivotDrop > 0f)
            {
                transform.localPosition = PivotPosition(basePosition, pivotDrop, angle);
            }
        }

        /// <summary>The breeze of the town, followed through a soft spring, plus a light flutter of the leaves.</summary>
        private float UpdateGlobalWind(float time, float dt)
        {
            float x = transform.position.x;
            float delayed = time - x / GustSpeed;
            float noise = Mathf.PerlinNoise(delayed * 0.17f, 3.1f + phaseOffset * 0.01f);
            float target = WindLean(GlobalWindStrength(time, x, noise), swayDegrees);

            if (!bendStarted)
            {
                // Start already leaning, so the trees do not all swing together when the scene opens.
                bendStarted = true;
                bendAngle = target;
                bendVelocity = 0f;
            }

            // A slow spring (about half a swing per second), lightly damped: the crown overshoots a little and settles.
            float omega = Mathf.Max(0.5f, swaySpeed * 2f);
            float stiffness = omega * omega;
            float damping = 2f * 0.35f * omega;
            StepSpringToward(ref bendAngle, ref bendVelocity, target, stiffness, damping, dt);

            float flutter = Mathf.Sin(time * swaySpeed * 2.3f + phaseOffset) * swayDegrees * 0.12f;
            return bendAngle + flutter;
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

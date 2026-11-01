using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Tall grass and flowers that lean away from the player when stepped near and sway with the town breeze otherwise.
    /// </summary>
    public sealed class GrassBend : MonoBehaviour
    {
        private const float ReactRadius = 0.9f;
        private const float MaxBendDegrees = 18f;
        private const float IdleRadius = 8f;
        private const float Stiffness = 60f;
        private const float Damping = 8f;
        private const float BreezeDegrees = 3f;

        private Quaternion baseRotation;
        private float angle;
        private float velocity;
        private Transform player;
        private float nextLookupTime;

        /// <summary>The angle the plant wants, in degrees: away from the player when near, otherwise the breeze.</summary>
        public static float TargetAngle(float distance, bool playerIsEast, float breezeAngle)
        {
            if (distance >= ReactRadius)
            {
                return breezeAngle;
            }

            float push = 1f - distance / ReactRadius;
            // Negative degrees lean to the right (east), so a player on the west pushes the plant east.
            float sign = playerIsEast ? 1f : -1f;
            return sign * MaxBendDegrees * Mathf.Clamp01(push + 0.35f);
        }

        private void Awake()
        {
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            if (player == null)
            {
                if (Time.unscaledTime < nextLookupTime)
                {
                    return;
                }

                nextLookupTime = Time.unscaledTime + 0.5f;
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                {
                    return;
                }

                player = found.transform;
            }

            Vector3 mine = transform.position;
            Vector3 theirs = player.position;
            float dx = theirs.x - mine.x;
            float dy = theirs.y - mine.y;
            float sqr = dx * dx + dy * dy;
            if (sqr > IdleRadius * IdleRadius)
            {
                return;
            }

            float time = Time.time;
            float noise = Mathf.PerlinNoise((time - mine.x / FoliagePhysics.GustSpeed) * 0.17f, 3.1f);
            float breeze = FoliagePhysics.WindLean(FoliagePhysics.GlobalWindStrength(time, mine.x, noise), BreezeDegrees);
            float target = TargetAngle(Mathf.Sqrt(sqr), theirs.x > mine.x, breeze);
            FoliagePhysics.StepSpringToward(ref angle, ref velocity, target, Stiffness, Damping, Time.deltaTime);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// While the player stands still in town, picks a casual pose (scratch head, look around, check phone...)
    /// every 6 to 9 seconds, holding it 1.5 to 2.5 seconds. Moving cancels it immediately.
    /// </summary>
    public sealed class IdleActions : MonoBehaviour
    {
        private PlayerAnimator animator;
        private Rigidbody2D body;
        private PersonPose lastPose = PersonPose.None;
        private float nextIdleTime;
        private bool inIdle;

        public static readonly PersonPose[] Poses = new[]
        {
            PersonPose.Stretch,
            PersonPose.ScratchHead,
            PersonPose.LookAround,
            PersonPose.CheckPhone,
            PersonPose.ShiftWeight
        };

        public static PersonPose Pick(float random01, PersonPose last)
        {
            int index = Mathf.Clamp(Mathf.FloorToInt(random01 * Poses.Length), 0, Poses.Length - 1);
            if (Poses[index] == last)
            {
                index = (index + 1) % Poses.Length;
            }

            return Poses[index];
        }

        public static float NextDelay(float random01)
        {
            return 6f + Mathf.Clamp01(random01) * 3f;
        }

        public static float HoldDuration(float random01)
        {
            return 1.5f + Mathf.Clamp01(random01) * 1f;
        }

        private void Awake()
        {
            animator = GetComponent<PlayerAnimator>();
            body = GetComponentInParent<Rigidbody2D>();
            ScheduleNext();
        }

        private void ScheduleNext()
        {
            nextIdleTime = Time.time + NextDelay(Random.value);
            inIdle = false;
        }

        private void Update()
        {
            if (PlayerController.Blocked || PlayerController.Frozen)
            {
                if (inIdle && animator != null)
                {
                    animator.SetPose(PersonPose.None);
                }

                ScheduleNext();
                return;
            }

            bool moving = body != null ? body.linearVelocity.sqrMagnitude > 0.04f : false;
            if (moving)
            {
                if (inIdle && animator != null)
                {
                    animator.SetPose(PersonPose.None);
                }

                ScheduleNext();
                return;
            }

            if (animator == null) return;

            if (!inIdle)
            {
                if (animator.Pose == PersonPose.None && Time.time >= nextIdleTime)
                {
                    PersonPose picked = Pick(Random.value, lastPose);
                    lastPose = picked;
                    float duration = HoldDuration(Random.value);
                    animator.SetPose(picked, duration);
                    inIdle = true;
                    nextIdleTime = Time.time + duration + NextDelay(Random.value);
                }
            }
            else
            {
                if (animator.Pose == PersonPose.None)
                {
                    inIdle = false;
                }
            }
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Loops a working pose on an NPC according to their trade (typing, cooking, reaching), alternating every 4 to 7
    /// seconds with a neutral pause or an alternate pose. Pauses while someone is talking with the NPC.
    /// </summary>
    public sealed class WorkLoop : MonoBehaviour
    {
        [SerializeField] private PersonPose primaryPose = PersonPose.None;
        [SerializeField] private PersonPose secondaryPose = PersonPose.None;
        [SerializeField] private float minInterval = 4f;
        [SerializeField] private float maxInterval = 7f;

        private PlayerAnimator animator;
        private float nextSwitch;
        private bool inPrimary;

        public void Configure(PersonPose primary, PersonPose secondary = PersonPose.None, float minTime = 4f, float maxTime = 7f)
        {
            primaryPose = primary;
            secondaryPose = secondary;
            minInterval = minTime;
            maxInterval = maxTime;
        }

        private void Awake()
        {
            animator = GetComponent<PlayerAnimator>() ?? GetComponentInChildren<PlayerAnimator>();
            ScheduleNext();
            ApplyPose(true);
        }

        private void ScheduleNext()
        {
            nextSwitch = Time.time + Random.Range(minInterval, maxInterval);
        }

        private void ApplyPose(bool isPrimary)
        {
            inPrimary = isPrimary;
            if (animator != null)
            {
                PersonPose pose = isPrimary ? primaryPose : secondaryPose;
                animator.SetPose(pose, 0f);
            }
        }

        private void Update()
        {
            if (animator == null) return;

            if (animator.Talking)
            {
                if (animator.Pose == primaryPose || animator.Pose == secondaryPose)
                {
                    animator.SetPose(PersonPose.None, 0f);
                }

                nextSwitch = Time.time + Random.Range(minInterval, maxInterval);
                return;
            }

            if (Time.time >= nextSwitch)
            {
                ApplyPose(!inPrimary);
                ScheduleNext();
            }
        }
    }
}

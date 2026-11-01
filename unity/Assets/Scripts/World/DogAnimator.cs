using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The numbers behind the dog's moods and moves. No scene objects, so it is easy to test.</summary>
    public static class DogMotion
    {
        /// <summary>Closer than this the dog notices the player, lifts its head and starts to wag.</summary>
        public const float NoticeRadius = 2.6f;

        /// <summary>Further than this, for a while, and the dog dozes off.</summary>
        public const float DozeRadius = 4.5f;

        /// <summary>Seconds alone before it falls asleep.</summary>
        public const float DozeAfter = 6f;

        /// <summary>How long the joy of being petted lasts.</summary>
        public const float JoySeconds = 2.6f;

        /// <summary>How excited the dog wants to be with the player at this distance: 0 far away, up to 0.6 right beside it.</summary>
        public static float ExcitementFor(float distance)
        {
            if (distance >= NoticeRadius)
            {
                return 0f;
            }

            return 0.25f + 0.35f * (1f - Mathf.Clamp01(distance / NoticeRadius));
        }

        /// <summary>
        /// The tail, in degrees added to its resting angle: a lazy sway when calm, a fast and wide wag when happy.
        /// The phase is kept by the caller so a change of mood never makes the tail jump.
        /// </summary>
        public static float TailWag(float phase, float excitement)
        {
            float e = Mathf.Clamp01(excitement);
            float amplitude = Mathf.Lerp(6f, 34f, e);
            return Mathf.Sin(phase) * amplitude;
        }

        /// <summary>How fast the tail phase runs, in radians per second.</summary>
        public static float TailSpeed(float excitement)
        {
            return Mathf.Lerp(2.2f, 17f, Mathf.Clamp01(excitement));
        }

        /// <summary>The breathing of the chest: slow and deep while asleep, quicker when awake, a pant when excited.</summary>
        public static float Breath(float time, bool asleep, float excitement)
        {
            if (asleep)
            {
                return 1f + Mathf.Sin(time * 1.4f) * 0.045f;
            }

            float speed = Mathf.Lerp(2.4f, 9f, Mathf.Clamp01(excitement));
            return 1f + Mathf.Sin(time * speed) * Mathf.Lerp(0.03f, 0.022f, excitement);
        }

        /// <summary>A quick flick of the ear that comes back on its own: degrees for the seconds since it started.</summary>
        public static float EarTwitch(float since)
        {
            const float length = 0.32f;
            if (since < 0f || since > length)
            {
                return 0f;
            }

            return Mathf.Sin(since / length * Mathf.PI) * 22f;
        }

        /// <summary>The little jumps of joy while it is being petted, in units.</summary>
        public static float Hop(float time, float joy)
        {
            return Mathf.Abs(Mathf.Sin(time * 9f)) * 0.06f * Mathf.Clamp01(joy);
        }

        /// <summary>The heart that floats up when the dog is petted: height and opacity for 0..1 of its life.</summary>
        public static void HeartFloat(float t01, out float rise, out float alpha)
        {
            float t = Mathf.Clamp01(t01);
            rise = t * 0.45f;
            alpha = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
        }
    }

    /// <summary>
    /// The house dog lying on its cushion. It breathes, flicks an ear, blinks, dozes when nobody is around, lifts its
    /// head and wags when the player comes close, and jumps with joy (tongue out, a heart floating up) when the player
    /// pets it. Being petted is noticed when the scene shakes the dog (see <c>Interaction.Wobble</c>), so the
    /// interaction code does not need to know about the dog. It only moves its own parts, never the dog's root.
    /// </summary>
    public sealed class DogAnimator : MonoBehaviour
    {
        [SerializeField] private Transform rig;
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform tail;
        [SerializeField] private Transform ear;
        [SerializeField] private Transform eye;
        [SerializeField] private Transform tongue;
        [SerializeField] private Transform heart;

        private Vector3 rootRest;
        private Vector3 rigRest;
        private Vector3 heartRest;
        private float tailRest;
        private float headRest;
        private float earRest;
        private float tailPhase;
        private float excitement;
        private float joyUntil = -10f;
        private float heartStarted = -10f;
        private float lonelySince;
        private bool asleep;
        private float headLift;
        private float nextTwitch;
        private float twitchStarted = -10f;
        private float nextBlink;
        private float blinkStarted = -10f;
        private float eyeOpen = 1f;
        private SpriteRenderer[] heartParts;
        private Color[] heartColors;
        private Transform player;
        private float nextPlayerSearch;

        public bool Asleep => asleep;

        public float Excitement => excitement;

        public void Bind(Transform rigRoot, Transform bodyPart, Transform headPivot, Transform tailPivot, Transform earPivot, Transform eyePart, Transform tonguePart, Transform heartGroup)
        {
            rig = rigRoot;
            body = bodyPart;
            head = headPivot;
            tail = tailPivot;
            ear = earPivot;
            eye = eyePart;
            tongue = tonguePart;
            heart = heartGroup;
        }

        /// <summary>The dog has just been petted: a burst of joy and a heart.</summary>
        public void Cheer()
        {
            joyUntil = Time.time + DogMotion.JoySeconds;
            heartStarted = Time.time;
            asleep = false;
            lonelySince = Time.time;
            ReactionBubble.Show(ReactionKind.Love);
        }

        private void Awake()
        {
            rootRest = transform.localPosition;
            rigRest = rig != null ? rig.localPosition : Vector3.zero;
            heartRest = heart != null ? heart.localPosition : Vector3.zero;
            tailRest = Angle(tail);
            headRest = Angle(head);
            earRest = Angle(ear);
            lonelySince = Time.time;
            nextTwitch = Time.time + Random.Range(2f, 5f);
            nextBlink = Time.time + PersonMotion.NextBlinkDelay(Random.value);

            if (heart != null)
            {
                heartParts = heart.GetComponentsInChildren<SpriteRenderer>(true);
                heartColors = new Color[heartParts.Length];
                for (int i = 0; i < heartParts.Length; i++)
                {
                    heartColors[i] = heartParts[i].color;
                }

                SetHeart(0f, 0f);
            }

            if (tongue != null)
            {
                tongue.localScale = new Vector3(1f, 0f, 1f);
            }
        }

        private void Update()
        {
            if (rig == null || body == null)
            {
                return;
            }

            float time = Time.time;
            float dt = Time.deltaTime;

            // Being shaken by the petting scene: that is the cue to cheer.
            // One cheer per petting: the shake lasts less than the joy, so it does not start the heart over and over.
            if ((transform.localPosition - rootRest).sqrMagnitude > 0.00002f && time - heartStarted > DogMotion.JoySeconds)
            {
                Cheer();
            }

            float distance = PlayerDistance();
            bool joyful = time < joyUntil;
            float wanted = joyful ? 1f : DogMotion.ExcitementFor(distance);
            excitement = Mathf.MoveTowards(excitement, wanted, dt * (wanted > excitement ? 2.5f : 0.6f));

            if (distance < DogMotion.NoticeRadius || joyful)
            {
                lonelySince = time;
                asleep = false;
            }
            else if (distance > DogMotion.DozeRadius && time - lonelySince > DogMotion.DozeAfter)
            {
                asleep = true;
            }

            // Tail: a phase that runs faster with joy, so the wag speeds up smoothly.
            tailPhase += DogMotion.TailSpeed(asleep ? 0f : excitement) * dt;
            float wag = asleep ? 0f : DogMotion.TailWag(tailPhase, excitement);
            SetAngle(tail, tailRest + wag);

            // Chest and back: breathing.
            float breath = DogMotion.Breath(time, asleep, excitement);
            body.localScale = new Vector3(1f, breath, 1f);

            // Head: down on the paws while asleep, up and a little toward the player when awake.
            float liftGoal = asleep ? -7f : (distance < DogMotion.NoticeRadius || joyful ? 9f : 2f);
            headLift = Mathf.Lerp(headLift, liftGoal, 1f - Mathf.Exp(-4f * dt));
            float headBob = joyful ? Mathf.Sin(time * 9f) * 3f : 0f;
            SetAngle(head, headRest + headLift + headBob);

            // Ear: a flick now and then while awake.
            if (!asleep && time >= nextTwitch)
            {
                twitchStarted = time;
                nextTwitch = time + Random.Range(2.5f, 6.5f);
            }

            SetAngle(ear, earRest + DogMotion.EarTwitch(time - twitchStarted) + (joyful ? Mathf.Sin(time * 9f) * 6f : 0f));

            AnimateEye(time, dt);

            // Tongue out and panting while excited.
            if (tongue != null)
            {
                float out01 = Mathf.InverseLerp(0.45f, 0.8f, excitement);
                float pant = 0.75f + 0.25f * Mathf.Sin(time * 12f);
                tongue.localScale = new Vector3(1f, asleep ? 0f : out01 * pant, 1f);
            }

            // Little jumps of joy.
            float joy = joyful ? Mathf.Clamp01((joyUntil - time) / 0.6f) : 0f;
            rig.localPosition = rigRest + new Vector3(0f, DogMotion.Hop(time, joy), 0f);

            if (heart != null)
            {
                float heartT = (time - heartStarted) / 1.4f;
                if (heartT >= 0f && heartT <= 1f)
                {
                    DogMotion.HeartFloat(heartT, out float rise, out float alpha);
                    heart.localPosition = heartRest + new Vector3(Mathf.Sin(heartT * 9f) * 0.03f, rise, 0f);
                    SetHeart(alpha, 0.8f + 0.2f * heartT);
                }
                else
                {
                    SetHeart(0f, 0f);
                }
            }
        }

        private void AnimateEye(float time, float dt)
        {
            if (eye == null)
            {
                return;
            }

            if (time >= nextBlink)
            {
                blinkStarted = time;
                nextBlink = time + PersonMotion.NextBlinkDelay(Random.value);
            }

            // Happy eyes squeeze shut for a moment while being petted.
            bool squeeze = time < joyUntil && time > joyUntil - DogMotion.JoySeconds + 0.3f && time < joyUntil - 1.2f;
            float goal = asleep || squeeze ? 0.15f : 1f;
            eyeOpen = Mathf.Lerp(eyeOpen, goal, 1f - Mathf.Exp(-10f * dt));
            float blink = asleep ? 1f : 1f - PersonMotion.BlinkClosed(time - blinkStarted) * 0.85f;
            eye.localScale = new Vector3(1f, Mathf.Max(0.12f, eyeOpen * blink), 1f);
        }

        private void SetHeart(float alpha, float scale)
        {
            if (heartParts == null)
            {
                return;
            }

            for (int i = 0; i < heartParts.Length; i++)
            {
                var color = heartColors[i];
                color.a *= alpha;
                heartParts[i].color = color;
                heartParts[i].enabled = alpha > 0.001f;
            }

            heart.localScale = Vector3.one * Mathf.Max(0.001f, scale);
        }

        private float PlayerDistance()
        {
            if (player == null && Time.time >= nextPlayerSearch)
            {
                nextPlayerSearch = Time.time + 0.5f;
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }

            return player != null ? Vector2.Distance(player.position, transform.position) : float.MaxValue;
        }

        private static float Angle(Transform part)
        {
            return part != null ? Mathf.DeltaAngle(0f, part.localEulerAngles.z) : 0f;
        }

        private static void SetAngle(Transform part, float degrees)
        {
            if (part != null)
            {
                part.localRotation = Quaternion.Euler(0f, 0f, degrees);
            }
        }
    }
}

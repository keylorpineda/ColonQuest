using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The moving parts of a person, found by the builder and bound to the animator.</summary>
    [System.Serializable]
    public sealed class PersonRig
    {
        public Transform Torso;
        public Transform Head;
        public Transform ArmL;
        public Transform ArmR;
        public Transform LegL;
        public Transform LegR;
        public Transform PupilL;
        public Transform PupilR;
        public Transform LidL;
        public Transform LidR;
        public Transform Mouth;
    }

    /// <summary>
    /// Brings a person to life with smooth, vector-style motion: a walk cycle with swinging arms and legs,
    /// breathing and blinking while standing, eyes and head that follow the player, a wave when the player
    /// comes close and speech gestures while someone talks. Put it on the child that holds the visual; the
    /// Rigidbody2D is read from the parent. A character without a rig only bounces and squashes.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private const float LookRadius = 5f;

        [SerializeField] private float referenceSpeed = 4.5f;
        [SerializeField] private float bobHeight = 0.09f;
        [SerializeField] private float stepRate = 11f;
        [SerializeField] private PersonRig rig;
        [SerializeField] private bool social;
        [SerializeField] private float greetRadius = 3.2f;
        [SerializeField] private float talkRadius;

        private Rigidbody2D body;
        private Transform visual;
        private Transform player;
        private Vector3 restPosition;
        private Vector3 legLRest;
        private Vector3 legRRest;
        private Vector3 pupilLRest;
        private Vector3 pupilRRest;
        private Vector3 headRest;
        private float phase;
        private float moving;
        private float externalSpeed;
        private float nextBlink;
        private float blinkStarted = -10f;
        private float waveStarted = -10f;
        private float lastGreet = -100f;
        private float nextPlayerSearch;
        private float headTilt;
        private Vector2 pupilShift;
        private bool talking;

        /// <summary>True while the person gestures as if speaking, because the player is close or someone set it.</summary>
        public bool Talking => talking;

        public bool IsWaving => PersonMotion.IsWaving(Time.time - waveStarted);

        public bool HasRig => rig != null && rig.Torso != null;

        /// <summary>Walking amount from 0 to 1 for characters that have no Rigidbody2D, such as pedestrians.</summary>
        public void SetSpeed01(float amount)
        {
            externalSpeed = Mathf.Clamp01(amount);
        }

        /// <summary>Hands the animator the parts it moves. A social person looks at and greets the player.</summary>
        public void Bind(PersonRig parts, bool isSocial, float speakRadius)
        {
            rig = parts;
            social = isSocial;
            talkRadius = speakRadius;
        }

        public void Greet()
        {
            waveStarted = Time.time;
        }

        public void SetTalking(bool value)
        {
            talking = value;
        }

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody2D>();
            restPosition = transform.localPosition;
            visual = transform.Find("Visual");
            if (HasRig)
            {
                legLRest = rig.LegL.localPosition;
                legRRest = rig.LegR.localPosition;
                pupilLRest = rig.PupilL.localPosition;
                pupilRRest = rig.PupilR.localPosition;
                headRest = rig.Head.localPosition;
            }

            nextBlink = Time.time + PersonMotion.NextBlinkDelay(Random.value);
        }

        private void Update()
        {
            float target = body != null ? Mathf.Clamp01(body.linearVelocity.magnitude / referenceSpeed) : externalSpeed;
            moving = Mathf.MoveTowards(moving, target, Time.deltaTime * 8f);
            phase += Time.deltaTime * stepRate * moving;

            float step = Mathf.Abs(Mathf.Sin(phase));
            float breathing = Mathf.Sin(Time.time * 2f) * 0.02f * (1f - moving);
            float squash = HasRig ? 1f + step * 0.03f * moving : 1f + (step - 0.5f) * 0.1f * moving + breathing;

            transform.localPosition = restPosition + new Vector3(0f, step * bobHeight * moving, 0f);
            transform.localScale = new Vector3(1f / squash, squash, 1f);

            if (HasRig)
            {
                AnimateRig();
            }
        }

        private void AnimateRig()
        {
            float time = Time.time;

            // Walk cycle: legs and arms swing in opposition and the feet lift.
            rig.LegL.localRotation = Z(PersonMotion.LegAngle(phase, moving, true));
            rig.LegR.localRotation = Z(PersonMotion.LegAngle(phase, moving, false));
            rig.LegL.localPosition = legLRest + new Vector3(0f, PersonMotion.FootLift(phase, moving, true), 0f);
            rig.LegR.localPosition = legRRest + new Vector3(0f, PersonMotion.FootLift(phase, moving, false), 0f);

            float sway = Mathf.Sin(time * 1.3f) * 2f * (1f - moving);
            float armLeft = PersonMotion.ArmAngle(phase, moving, true) - sway;
            float armRight = PersonMotion.ArmAngle(phase, moving, false) + sway;

            // The greeting and the speech gestures take over an arm.
            float sinceWave = time - waveStarted;
            if (PersonMotion.IsWaving(sinceWave))
            {
                armRight = PersonMotion.WaveAngle(sinceWave);
            }
            else if (talking)
            {
                armRight = 18f + Mathf.Sin(time * 4.5f) * 16f;
                armLeft = -(8f + Mathf.Sin(time * 3.3f + 1f) * 10f);
            }

            rig.ArmL.localRotation = Z(armLeft);
            rig.ArmR.localRotation = Z(armRight);

            // Torso: breathing, and a small lean with every step.
            float breath = 1f + Mathf.Sin(time * 2f) * 0.018f;
            rig.Torso.localScale = new Vector3(1f / breath, breath, 1f);
            rig.Torso.localRotation = Z(Mathf.Sin(phase) * 2.5f * moving);

            AnimateFace(time);
        }

        private void AnimateFace(float time)
        {
            // Blink every few seconds.
            if (time >= nextBlink)
            {
                blinkStarted = time;
                nextBlink = time + PersonMotion.NextBlinkDelay(Random.value);
            }

            float closed = PersonMotion.BlinkClosed(time - blinkStarted);
            rig.LidL.localScale = new Vector3(1f, closed, 1f);
            rig.LidR.localScale = new Vector3(1f, closed, 1f);

            Vector2 pupilGoal = Vector2.zero;
            float tiltGoal = 0f;
            bool near = false;

            if (social)
            {
                FindPlayer();
                if (player != null)
                {
                    Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
                    float distance = toPlayer.magnitude;
                    float side = visual != null && visual.lossyScale.x < 0f ? -1f : 1f;

                    if (distance < LookRadius)
                    {
                        // The visual can be mirrored, so the offset is turned into its own space.
                        Vector2 look = PersonMotion.PupilOffset(toPlayer, 0.028f);
                        pupilGoal = new Vector2(look.x * side, look.y);
                        tiltGoal = PersonMotion.HeadTilt(toPlayer.x) * side;
                    }

                    if (distance < greetRadius && time - lastGreet > 9f)
                    {
                        lastGreet = time;
                        Greet();
                    }

                    near = talkRadius > 0f && distance < talkRadius;
                }
            }

            if (talkRadius > 0f)
            {
                talking = near;
            }

            pupilShift = Vector2.Lerp(pupilShift, pupilGoal, 1f - Mathf.Exp(-12f * Time.deltaTime));
            headTilt = Mathf.Lerp(headTilt, tiltGoal, 1f - Mathf.Exp(-8f * Time.deltaTime));

            rig.PupilL.localPosition = pupilLRest + (Vector3)pupilShift;
            rig.PupilR.localPosition = pupilRRest + (Vector3)pupilShift;
            rig.Head.localRotation = Z(headTilt);
            rig.Head.localPosition = headRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.012f * moving + Mathf.Sin(time * 2f) * 0.006f, 0f);

            // A smile while greeting, a moving mouth while talking.
            bool greeting = PersonMotion.IsWaving(time - waveStarted);
            float open = PersonMotion.MouthOpen(time, talking);
            rig.Mouth.localScale = new Vector3(greeting ? 1.5f : 1f, greeting ? Mathf.Max(open, 0.6f) : open, 1f);
        }

        private void FindPlayer()
        {
            if (player != null || Time.time < nextPlayerSearch)
            {
                return;
            }

            nextPlayerSearch = Time.time + 0.5f;
            var found = GameObject.FindWithTag("Player");
            player = found != null ? found.transform : null;
        }

        private static Quaternion Z(float degrees)
        {
            return Quaternion.Euler(0f, 0f, degrees);
        }
    }
}

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
        public Transform ElbowL;
        public Transform ElbowR;
        public Transform LegL;
        public Transform LegR;
        public Transform PupilL;
        public Transform PupilR;
        public Transform LidL;
        public Transform LidR;
        public Transform Mouth;

        /// <summary>The height of the mouth while it is closed. A smile needs more than a flat bar.</summary>
        public float MouthRest = 0.25f;

        /// <summary>The face and the fringe, hidden while the person walks away from the camera. Optional.</summary>
        public GameObject Face;

        /// <summary>What is worn on the chest (tie, badge, apron...), hidden from behind. Optional.</summary>
        public GameObject TorsoFront;

        /// <summary>The back of the head, shown only while walking away. Without it the person always shows the face.</summary>
        public GameObject BackHead;
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
        private float armLRest;
        private float armRRest;
        private float elbowLRest;
        private float elbowRRest;
        private float phase;
        private float moving;
        private float externalSpeed;
        private float nextBlink;
        private float blinkStarted = -10f;
        private float waveStarted = -10f;
        private float lastGreet = -100f;
        private float nextPlayerSearch;
        private float headTilt;
        private float torsoLean;
        private Vector2 pupilShift;
        private bool talking;
        private PersonPose pose;
        private PersonPose shownPose;
        private float poseStarted;
        private float poseUntil;
        private float poseWeight;
        private PoseAngles poseAngles;
        private SpriteRenderer[] forearmParts;
        private int[] forearmOrders;
        private bool handsInFront;

        /// <summary>How many sorting steps the forearms and hands climb to pass over the torso and stay under the face.</summary>
        private const int HandsFrontLift = 7;

        /// <summary>How long the back stays turned after the upward walk ends, so a short diagonal does not flicker.</summary>
        private const float AwayGrace = 0.3f;

        /// <summary>A move faster than this in one frame is a jump to another place (a spawn), not a walk.</summary>
        private const float TeleportSpeed = 25f;

        private Vector3 lastRootPosition;
        private bool hasLastRoot;
        private Vector2 walkVelocity;
        private bool away;
        private float lastAwayTime = -10f;
        private float settle;
        private float settleVelocity;
        private bool stopKicked = true;

        /// <summary>True while the person gestures as if speaking, because the player is close or someone set it.</summary>
        public bool Talking => talking;

        /// <summary>The pose the person holds now, or None.</summary>
        public PersonPose Pose => pose;

        /// <summary>
        /// Holds a pose: lying down, eating, handing over money. It blends in and out. With a duration it ends by itself;
        /// with 0 it lasts until it is cleared with <see cref="PersonPose.None"/>.
        /// </summary>
        public void SetPose(PersonPose value, float seconds = 0f)
        {
            pose = value;
            if (value != PersonPose.None)
            {
                shownPose = value;
                poseStarted = Time.time;
                poseUntil = seconds > 0f ? Time.time + seconds : float.MaxValue;
            }
        }

        public bool IsWaving => PersonMotion.IsWaving(Time.time - waveStarted);

        public bool HasRig => rig != null && rig.Torso != null;

        /// <summary>True while the back of the head shows, because the person walks up the screen.</summary>
        public bool ShowsBack => away;

        private bool HasBackView => HasRig && rig.BackHead != null && rig.Face != null;

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
                armLRest = RestAngle(rig.ArmL);
                armRRest = RestAngle(rig.ArmR);
                elbowLRest = RestAngle(rig.ElbowL);
                elbowRRest = RestAngle(rig.ElbowR);
                CollectForearms();
            }

            nextBlink = Time.time + PersonMotion.NextBlinkDelay(Random.value);
        }

        private void OnDisable()
        {
            // No SetActive here: this also runs while the person is being destroyed or switched off, when changing the
            // active state of its parts is not allowed. Once the animator runs again, the next Update turns the face
            // back by itself (a person standing still never shows the back for more than the short grace).
            hasLastRoot = false;
        }

        private void Update()
        {
            walkVelocity = WalkVelocity();
            float target = body != null ? Mathf.Clamp01(body.linearVelocity.magnitude / referenceSpeed) : externalSpeed;
            moving = Mathf.MoveTowards(moving, target, Time.deltaTime * 8f);
            phase += Time.deltaTime * stepRate * moving;

            float step = Mathf.Abs(Mathf.Sin(phase));
            float breathing = Mathf.Sin(Time.time * 2f) * 0.02f * (1f - moving);
            float squash = HasRig ? PersonMotion.StepSquash(step, moving) : 1f + (step - 0.5f) * 0.1f * moving + breathing;

            // Stopping: the body sinks a little and comes back up, as if the weight settled on the feet.
            if (moving > 0.6f)
            {
                stopKicked = false;
            }
            else if (!stopKicked && target < 0.05f && moving < 0.5f)
            {
                stopKicked = true;
                settleVelocity -= 0.9f;
            }

            FoliagePhysics.StepSpring(ref settle, ref settleVelocity, 180f, 14f, Time.deltaTime);
            if (HasRig)
            {
                squash = Mathf.Clamp(squash + settle, 0.92f, 1.08f);
            }

            if (pose != PersonPose.None && Time.time > poseUntil)
            {
                pose = PersonPose.None;
            }

            poseWeight = Mathf.MoveTowards(poseWeight, pose != PersonPose.None ? 1f : 0f, Time.deltaTime * 5f);
            poseAngles = poseWeight > 0f ? PersonPoses.Evaluate(shownPose, Time.time - poseStarted) : new PoseAngles();
            UpdateFacing();

            transform.localPosition = restPosition + new Vector3(0f, step * bobHeight * moving * (1f - poseWeight) + poseAngles.Drop * poseWeight, 0f);
            transform.localRotation = Z(poseAngles.Roll * poseWeight);
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
            rig.LegL.localRotation = Z(Mathf.Lerp(PersonMotion.LegAngle(phase, moving, true), poseAngles.LegL, poseWeight));
            rig.LegR.localRotation = Z(Mathf.Lerp(PersonMotion.LegAngle(phase, moving, false), poseAngles.LegR, poseWeight));
            rig.LegL.localPosition = legLRest + new Vector3(0f, PersonMotion.FootLift(phase, moving, true), 0f);
            rig.LegR.localPosition = legRRest + new Vector3(0f, PersonMotion.FootLift(phase, moving, false), 0f);

            float sway = Mathf.Sin(time * 1.3f) * 2f * (1f - moving);
            float armLeft = PersonMotion.ArmAngle(phase, moving, true) - sway;
            float armRight = PersonMotion.ArmAngle(phase, moving, false) + sway;

            float elbowLeft = 0f;
            float elbowRight = 0f;

            // The greeting and the speech gestures take over an arm. The angles add to the arm's resting pose.
            float sinceWave = time - waveStarted;
            if (PersonMotion.IsWaving(sinceWave))
            {
                armRight = PersonMotion.WaveAngle(sinceWave) * 0.85f;
                elbowRight = PersonMotion.ElbowAngle(sinceWave);
            }
            else if (talking)
            {
                armRight = 18f + Mathf.Sin(time * 4.5f) * 16f;
                armLeft = -(8f + Mathf.Sin(time * 3.3f + 1f) * 10f);
                elbowRight = 20f + Mathf.Sin(time * 4.5f + 0.8f) * 14f;
                elbowLeft = -(10f + Mathf.Sin(time * 3.3f + 1.6f) * 8f);
            }

            // A pose takes over the arms, and the blend gives it back when it ends.
            float w = poseWeight;
            armLeft = Mathf.Lerp(armLeft, poseAngles.ArmL, w);
            armRight = Mathf.Lerp(armRight, poseAngles.ArmR, w);
            elbowLeft = Mathf.Lerp(elbowLeft, poseAngles.ElbowL, w);
            elbowRight = Mathf.Lerp(elbowRight, poseAngles.ElbowR, w);

            rig.ArmL.localRotation = Z(armLRest + armLeft);
            rig.ArmR.localRotation = Z(armRRest + armRight);
            if (rig.ElbowL != null && rig.ElbowR != null)
            {
                rig.ElbowL.localRotation = Z(elbowLRest + elbowLeft);
                rig.ElbowR.localRotation = Z(elbowRRest + elbowRight);
            }

            // Torso: breathing, and a small lean with every step.
            float breath = 1f + Mathf.Sin(time * 2f) * 0.018f;
            rig.Torso.localScale = new Vector3(1f / breath, breath, 1f);
            float speed = walkVelocity.magnitude;
            float sideways = speed > 0.01f ? Mathf.Abs(walkVelocity.x) / speed : 0f;
            torsoLean = Mathf.Sin(phase) * 2.5f * moving + poseAngles.Lean * poseWeight + PersonMotion.WalkLean(moving, sideways) * (1f - poseWeight);
            rig.Torso.localRotation = Z(torsoLean);

            SetHandsInFront(poseWeight > 0.5f && PersonPoses.HandsInFront(shownPose));
            AnimateFace(time);
        }

        /// <summary>
        /// The walk in world units per second: the Rigidbody2D's velocity for the player, and for the people moved by a
        /// script (pedestrians, officers) how far their root went since the last frame.
        /// </summary>
        private Vector2 WalkVelocity()
        {
            if (body != null)
            {
                return body.linearVelocity;
            }

            var root = transform.parent != null ? transform.parent : transform;
            Vector3 position = root.position;
            float dt = Time.deltaTime;
            Vector2 velocity = Vector2.zero;
            if (hasLastRoot && dt > 0f)
            {
                velocity = (Vector2)(position - lastRootPosition) / dt;
                if (velocity.magnitude > TeleportSpeed)
                {
                    velocity = Vector2.zero;
                }
            }

            lastRootPosition = position;
            hasLastRoot = true;
            return velocity;
        }

        /// <summary>Shows the back of the head while the person walks up the screen, and the face otherwise.</summary>
        private void UpdateFacing()
        {
            if (!HasBackView)
            {
                return;
            }

            bool free = pose == PersonPose.None && poseWeight < 0.01f;
            bool walkingAway = free && PersonMotion.FacingAway(walkVelocity, away);
            if (walkingAway)
            {
                lastAwayTime = Time.time;
            }

            SetAway(walkingAway || (free && away && Time.time - lastAwayTime < AwayGrace));
        }

        private void SetAway(bool value)
        {
            if (value == away || !HasBackView)
            {
                return;
            }

            away = value;
            rig.BackHead.SetActive(value);
            rig.Face.SetActive(!value);
            if (rig.TorsoFront != null)
            {
                rig.TorsoFront.SetActive(!value);
            }
        }

        /// <summary>The forearms, hands and what they hold, with the order each was built with.</summary>
        private void CollectForearms()
        {
            var parts = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var elbow in new[] { rig.ElbowL, rig.ElbowR })
            {
                if (elbow != null)
                {
                    parts.AddRange(elbow.GetComponentsInChildren<SpriteRenderer>(true));
                }
            }

            forearmParts = parts.ToArray();
            forearmOrders = new int[forearmParts.Length];
            for (int i = 0; i < forearmParts.Length; i++)
            {
                forearmOrders[i] = forearmParts[i].sortingOrder;
            }
        }

        private void SetHandsInFront(bool value)
        {
            if (value == handsInFront || forearmParts == null)
            {
                return;
            }

            handsInFront = value;
            for (int i = 0; i < forearmParts.Length; i++)
            {
                if (forearmParts[i] != null)
                {
                    forearmParts[i].sortingOrder = forearmOrders[i] + (value ? HandsFrontLift : 0);
                }
            }
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
                        Vector2 look = PersonMotion.PupilOffset(toPlayer, 0.012f);
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
            // The head sits on the neck, which leans with the torso: it turns around the torso's pivot by the same angle,
            // so no gap or step opens under the chin.
            Vector3 torsoPivot = rig.Torso.localPosition;
            Vector3 onNeck = torsoPivot + Z(torsoLean) * (headRest - torsoPivot);
            rig.Head.localRotation = Z(headTilt + torsoLean);
            rig.Head.localPosition = onNeck + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.012f * moving + Mathf.Sin(time * 2f) * 0.006f, 0f);

            // A smile while greeting, a moving mouth while talking.
            bool greeting = PersonMotion.IsWaving(time - waveStarted);
            float open = PersonMotion.MouthOpen(time, talking);
            float rest = Mathf.Clamp(rig.MouthRest, 0.25f, 1f);
            open = Mathf.Lerp(rest, 1f, (open - 0.25f) / 0.75f);
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

        /// <summary>The Z rotation a part was built with, between -180 and 180 degrees.</summary>
        private static float RestAngle(Transform part)
        {
            return part != null ? Mathf.DeltaAngle(0f, part.localEulerAngles.z) : 0f;
        }

        private static Quaternion Z(float degrees)
        {
            return Quaternion.Euler(0f, 0f, degrees);
        }
    }
}

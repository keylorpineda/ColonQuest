using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The poses a person takes while something happens to them: sleeping, eating, handing over money...</summary>
    public enum PersonPose
    {
        None,
        Sleep,
        Sit,
        Eat,
        Type,
        Handover,
        Count,
        Sign,
        Wave,
        Sweep,
        Swim,
        Read,
        Cook,
        Wash,
        Reach,
        SitEat,
        HandsUp,

        /// <summary>The right hand goes down to the pocket of the pants.</summary>
        Pocket,

        /// <summary>Stretches arms up and back slightly.</summary>
        Stretch,

        /// <summary>Scratches the head with the right hand.</summary>
        ScratchHead,

        /// <summary>Looks around casually.</summary>
        LookAround,

        /// <summary>Checks the phone with one hand.</summary>
        CheckPhone,

        /// <summary>Shifts weight from one leg to the other.</summary>
        ShiftWeight
    }

    /// <summary>
    /// The angles of a pose, all added to the resting pose of the person: shoulders and elbows, a lean of the torso, how far
    /// the body drops, how far it rolls over (lying down) and how far the legs open.
    /// </summary>
    public struct PoseAngles
    {
        public float ArmL;
        public float ArmR;
        public float ElbowL;
        public float ElbowR;
        public float Lean;
        public float Drop;
        public float Roll;
        public float LegL;
        public float LegR;
    }

    /// <summary>
    /// The numbers behind the poses. A positive angle turns counter-clockwise: for the right arm that is away from the body,
    /// for the left arm it is across it, and the elbows follow the same sign. No scene objects, so it is easy to test.
    /// </summary>
    public static class PersonPoses
    {
        /// <summary>How far a seated body sinks onto the seat. The seat point already puts the player on it.</summary>
        public const float SeatDrop = -0.04f;

        /// <summary>How far the legs turn out while sitting.</summary>
        public const float SeatLegs = 6f;

        public static PoseAngles Evaluate(PersonPose pose, float seconds)
        {
            float t = Mathf.Max(0f, seconds);
            switch (pose)
            {
                case PersonPose.Sleep:
                    return new PoseAngles { Roll = 90f, Drop = -0.3f, ArmL = 6f, ArmR = -6f, Lean = Mathf.Sin(t * 1.6f) * 1.5f };

                case PersonPose.Sit:
                    // Seen from the front, a seated person keeps the legs down; spreading them read as a split.
                    return new PoseAngles { Drop = SeatDrop, LegL = -SeatLegs, LegR = SeatLegs, ArmL = 18f, ArmR = -18f, ElbowL = 10f, ElbowR = -10f };

                case PersonPose.Eat:
                {
                    float lift = (Mathf.Sin(t * 3f) + 1f) * 0.5f;
                    return new PoseAngles { ArmR = -12f, ElbowR = -55f - 45f * lift, ArmL = 20f, ElbowL = 25f, Lean = 3f * lift };
                }

                case PersonPose.SitEat:
                {
                    float lift = (Mathf.Sin(t * 3f) + 1f) * 0.5f;
                    return new PoseAngles { Drop = SeatDrop, LegL = -SeatLegs, LegR = SeatLegs, ArmR = -12f, ElbowR = -55f - 45f * lift, ArmL = 20f, ElbowL = 25f, Lean = 3f * lift };
                }

                case PersonPose.Type:
                    return new PoseAngles { ArmR = -24f, ElbowR = -78f + Mathf.Sin(t * 14f) * 4f, ArmL = 20f, ElbowL = 40f, Lean = 2f };

                case PersonPose.Handover:
                {
                    float reach = Mathf.Clamp01(t * 4f);
                    return new PoseAngles { ArmR = 62f * reach + Mathf.Sin(t * 5f) * 3f, ElbowR = 22f * reach, ArmL = 6f };
                }

                case PersonPose.Count:
                    return new PoseAngles
                    {
                        ArmR = -34f + Mathf.Sin(t * 8f) * 8f, ElbowR = -62f + Mathf.Sin(t * 8f + 1f) * 10f,
                        ArmL = 34f - Mathf.Sin(t * 8f + 2f) * 8f, ElbowL = 62f - Mathf.Sin(t * 8f + 3f) * 10f, Lean = 3f
                    };

                case PersonPose.Sign:
                    return new PoseAngles { ArmR = -26f, ElbowR = -42f + Mathf.Sin(t * 7f) * 6f, ArmL = 24f, ElbowL = 34f, Lean = 7f };

                case PersonPose.Wave:
                {
                    float wave = t % PersonMotion.WaveDuration;
                    return new PoseAngles { ArmR = PersonMotion.WaveAngle(wave) * 0.85f, ElbowR = PersonMotion.ElbowAngle(wave) };
                }

                case PersonPose.Sweep:
                    return new PoseAngles { ArmR = -18f + Mathf.Sin(t * 5f) * 22f, ElbowR = -30f, ArmL = 18f + Mathf.Sin(t * 5f) * 22f, ElbowL = 30f, Lean = Mathf.Sin(t * 5f) * 5f };

                case PersonPose.Swim:
                    return new PoseAngles { ArmR = 95f + Mathf.Sin(t * 4f) * 65f, ArmL = -(95f + Mathf.Sin(t * 4f + Mathf.PI) * 65f), Drop = -0.22f, Lean = Mathf.Sin(t * 2f) * 4f };

                case PersonPose.Read:
                    return new PoseAngles { ArmR = -20f, ElbowR = -64f, ArmL = 20f, ElbowL = 64f, Lean = 3f };

                case PersonPose.Cook:
                    return new PoseAngles { ArmR = -14f + Mathf.Sin(t * 6f) * 14f, ElbowR = -56f + Mathf.Sin(t * 6f + 1f) * 10f, ArmL = 14f, ElbowL = 20f, Lean = 3f };

                case PersonPose.Reach:
                {
                    float stretch = Mathf.Clamp01(t * 4f);
                    return new PoseAngles { ArmR = 68f * stretch, ElbowR = 18f * stretch, Lean = 6f * stretch };
                }

                case PersonPose.HandsUp:
                {
                    // Both arms go up beside the head, the elbows a little bent, with a small nervous shake.
                    float raise = Mathf.Clamp01(t * 3f);
                    float shake = Mathf.Sin(t * 9f) * 3f * raise;
                    return new PoseAngles { ArmR = 150f * raise + shake, ElbowR = 25f * raise, ArmL = -150f * raise - shake, ElbowL = -25f * raise };
                }

                case PersonPose.Pocket:
                {
                    float down = Mathf.Clamp01(t * 6f);
                    return new PoseAngles { ArmR = -8f * down, ElbowR = -30f * down, Lean = -2f * down };
                }

                case PersonPose.Wash:
                    return new PoseAngles { ArmR = -26f, ElbowR = -60f + Mathf.Sin(t * 9f) * 8f, ArmL = 26f, ElbowL = 60f - Mathf.Sin(t * 9f) * 8f, Lean = 4f };

                case PersonPose.Stretch:
                {
                    float stretch = Mathf.Clamp01(t * 3f);
                    return new PoseAngles { ArmR = 140f * stretch, ElbowR = 20f * stretch, ArmL = -140f * stretch, ElbowL = -20f * stretch, Lean = -4f * stretch };
                }

                case PersonPose.ScratchHead:
                    return new PoseAngles { ArmR = 150f + Mathf.Sin(t * 14f) * 8f, ElbowR = 70f, Lean = 3f };

                case PersonPose.LookAround:
                    return new PoseAngles { ArmL = 8f, ArmR = -8f, Lean = Mathf.Sin(t * 2f) * 4f };

                case PersonPose.CheckPhone:
                    return new PoseAngles { ArmR = -18f, ElbowR = -70f, ArmL = 12f, ElbowL = 30f, Lean = 4f };

                case PersonPose.ShiftWeight:
                    return new PoseAngles { Lean = Mathf.Sin(t * 1.5f) * 3f, Drop = -0.02f, LegL = -4f, LegR = 4f };

                default:
                    return new PoseAngles();
            }
        }

        /// <summary>True when the pose changes the height or the angle of the whole body, so the walk animation must give way.</summary>
        public static bool MovesBody(PersonPose pose)
        {
            return pose == PersonPose.Sleep || pose == PersonPose.Sit || pose == PersonPose.SitEat || pose == PersonPose.Swim;
        }

        /// <summary>
        /// True when the pose brings the hands to the chest or the mouth. The forearms and hands are drawn under the torso
        /// at rest, so for these poses the animator draws them over it; otherwise the hands vanish behind the shirt.
        /// </summary>
        public static bool HandsInFront(PersonPose pose)
        {
            switch (pose)
            {
                case PersonPose.Eat:
                case PersonPose.SitEat:
                case PersonPose.Read:
                case PersonPose.Type:
                case PersonPose.Count:
                case PersonPose.Sign:
                case PersonPose.Wash:
                case PersonPose.Cook:
                case PersonPose.Pocket:
                case PersonPose.ScratchHead:
                case PersonPose.CheckPhone:
                    return true;
                default:
                    return false;
            }
        }
    }
}

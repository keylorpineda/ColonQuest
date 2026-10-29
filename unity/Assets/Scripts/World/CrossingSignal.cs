using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The pedestrian light of a crosswalk: a fixed cycle of walking time and waiting time, read from the clock.</summary>
    public static class CrossingClock
    {
        public const float WalkSeconds = 9f;
        public const float WaitSeconds = 11f;

        public static float Cycle => WalkSeconds + WaitSeconds;

        /// <summary>True while pedestrians may cross. The phase moves one crosswalk against another.</summary>
        public static bool IsWalk(float time, float phase)
        {
            float t = (time + phase) % Cycle;
            if (t < 0f)
            {
                t += Cycle;
            }

            return t < WalkSeconds;
        }
    }

    /// <summary>When walking on a crosswalk is an offense. Pure, so the rule can be tested.</summary>
    public static class CrossingRules
    {
        /// <summary>Being on the crosswalk while the light says wait, unless the officer already fined this crossing.</summary>
        public static bool IsRedLightOffense(bool onCrosswalk, bool walkLight, bool alreadyFined)
        {
            return onCrosswalk && !walkLight && !alreadyFined;
        }
    }

    /// <summary>
    /// The light and the zone of one crosswalk. Crossing on red only raises the offense: it costs nothing by itself,
    /// a fine arrives only if an officer who sees the player stops them (<see cref="PoliceOfficer"/>).
    /// </summary>
    public sealed class CrossingSignal : MonoBehaviour
    {
        private static readonly List<CrossingSignal> all = new List<CrossingSignal>();

        [SerializeField] private Vector2 zoneCenter;
        [SerializeField] private Vector2 zoneSize = new Vector2(2.4f, 4f);
        [SerializeField] private float phase;
        [SerializeField] private SpriteRenderer walkLamp;
        [SerializeField] private SpriteRenderer waitLamp;

        private static readonly Color WalkOn = new Color(0.30f, 0.85f, 0.45f, 1f);
        private static readonly Color WalkOff = new Color(0.12f, 0.28f, 0.16f, 1f);
        private static readonly Color WaitOn = new Color(0.95f, 0.25f, 0.25f, 1f);
        private static readonly Color WaitOff = new Color(0.32f, 0.10f, 0.10f, 1f);

        private Transform player;
        private bool raised;
        private bool fined;

        /// <summary>True while the pedestrian light of this crosswalk is green.</summary>
        public bool WalkLight => CrossingClock.IsWalk(Time.time, phase);

        public void Configure(Vector2 center, Vector2 size, float lightPhase, SpriteRenderer walk, SpriteRenderer wait)
        {
            zoneCenter = center;
            zoneSize = size;
            phase = lightPhase;
            walkLamp = walk;
            waitLamp = wait;
        }

        /// <summary>The officer fined the player: the crossings stop counting until the player leaves or the light turns green.</summary>
        public static void ForgiveAll()
        {
            foreach (var signal in all)
            {
                signal.fined = true;
            }
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
            if (raised)
            {
                Offenses.Clear(OffenseKind.RedLight);
                raised = false;
            }
        }

        private void Update()
        {
            bool walk = WalkLight;
            if (walkLamp != null)
            {
                walkLamp.color = walk ? WalkOn : WalkOff;
            }

            if (waitLamp != null)
            {
                waitLamp.color = walk ? WaitOff : WaitOn;
            }

            if (player == null)
            {
                var found = GameObject.FindWithTag(Station.PlayerTag);
                player = found != null ? found.transform : null;
            }

            bool inside = player != null && new Rect(zoneCenter - zoneSize * 0.5f, zoneSize).Contains(player.position);
            if (!inside || walk)
            {
                fined = false;
            }

            bool offending = !SceneTransition.IsBusy && CrossingRules.IsRedLightOffense(inside, walk, fined);
            if (offending && !raised)
            {
                Offenses.Raise(OffenseKind.RedLight);
                raised = true;
            }
            else if (!offending && raised)
            {
                Offenses.Clear(OffenseKind.RedLight);
                raised = false;
            }
        }
    }
}

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

    /// <summary>
    /// The light and the zone of one crosswalk. The cars stop at it while pedestrians have green. Crossing on red is not
    /// an offense: the only risk of walking across is the traffic.
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

        /// <summary>True while the pedestrian light of this crosswalk is green.</summary>
        public bool WalkLight => CrossingClock.IsWalk(Time.time, phase);

        /// <summary>The crosswalks of the scene, for the cars that stop at them.</summary>
        public static IReadOnlyList<CrossingSignal> All => all;

        public float ZoneMinX => zoneCenter.x - zoneSize.x * 0.5f;

        public float ZoneMaxX => zoneCenter.x + zoneSize.x * 0.5f;

        /// <summary>True when a lane at this height runs over this crosswalk.</summary>
        public bool CoversLane(float y)
        {
            return y >= zoneCenter.y - zoneSize.y * 0.5f && y <= zoneCenter.y + zoneSize.y * 0.5f;
        }

        public void Configure(Vector2 center, Vector2 size, float lightPhase, SpriteRenderer walk, SpriteRenderer wait)
        {
            zoneCenter = center;
            zoneSize = size;
            phase = lightPhase;
            walkLamp = walk;
            waitLamp = wait;
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
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
        }
    }
}

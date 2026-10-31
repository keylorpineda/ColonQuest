using System;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>What the player did that the law cares about. A fine only exists while an officer sees it.</summary>
    public enum OffenseKind
    {
        None,
        Trespass,
        RedLight
    }

    /// <summary>
    /// The offense the player is committing right now, if any. It costs nothing by itself: a fine arrives only when
    /// an officer sees the player and stops them (<see cref="PoliceOfficer"/>).
    /// </summary>
    public static class Offenses
    {
        public static OffenseKind Current { get; private set; }

        public static void Raise(OffenseKind kind)
        {
            Current = kind;
        }

        /// <summary>Ends the offense, but only if it is still the one that was raised.</summary>
        public static void Clear(OffenseKind kind)
        {
            if (Current == kind)
            {
                Current = OffenseKind.None;
            }
        }

        public static void Reset()
        {
            Current = OffenseKind.None;
        }
    }

    public enum OfficerAction
    {
        Idle,
        Pursue,
        Stop
    }

    /// <summary>What an officer does about the player. Pure, so the rules of the law can be tested.</summary>
    public static class OfficerLogic
    {
        public const float SightRange = 6f;
        public const float StopRange = 1.3f;

        /// <summary>
        /// No offense, nothing happens, however close the officer is. With an offense the officer stops a player who
        /// is within reach, goes after one he can see, and goes after any player when he was called to the place.
        /// </summary>
        public static OfficerAction Decide(Vector2 officer, Vector2 player, OffenseKind offense, bool called)
        {
            if (offense == OffenseKind.None)
            {
                return OfficerAction.Idle;
            }

            float distance = Vector2.Distance(officer, player);
            if (distance <= StopRange)
            {
                return OfficerAction.Stop;
            }

            return called || distance <= SightRange ? OfficerAction.Pursue : OfficerAction.Idle;
        }
    }

    /// <summary>
    /// A police officer. He does nothing while the player breaks no rule. When someone called him, or while the player
    /// commits an offense in his sight, he walks to the player and stops them once; the overlay then charges the fine.
    /// </summary>
    public sealed class PoliceOfficer : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<PoliceOfficer> activeOfficers = new System.Collections.Generic.List<PoliceOfficer>();

        [SerializeField] private float speed = 4.6f;

        private Transform player;
        private bool called;
        private bool stopped;
        private bool pursuingRedLight;

        /// <summary>Raised once, when the officer reaches the player and stops them for this offense.</summary>
        public static event Action<OffenseKind> StoppedPlayer;

        public bool HasStopped => stopped;

        public bool IsPursuingRedLight => pursuingRedLight && !stopped;

        public static bool IsAnyPursuingRedLight()
        {
            for (int i = 0; i < activeOfficers.Count; i++)
            {
                if (activeOfficers[i] != null && activeOfficers[i].IsPursuingRedLight)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            if (!activeOfficers.Contains(this))
            {
                activeOfficers.Add(this);
            }
        }

        private void OnDisable()
        {
            activeOfficers.Remove(this);
            pursuingRedLight = false;
        }

        /// <summary>The owner called the police: the officer appears and heads straight for the player.</summary>
        public void Alert()
        {
            called = true;
            stopped = false;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (stopped)
            {
                // An officer on the street can stop the player again once the offense is over; one who was called cannot.
                if (!called && Offenses.Current == OffenseKind.None)
                {
                    stopped = false;
                    pursuingRedLight = false;
                }

                return;
            }

            if (SceneTransition.IsBusy)
            {
                pursuingRedLight = false;
                return;
            }

            if (player == null)
            {
                var found = GameObject.FindWithTag(Station.PlayerTag);
                player = found != null ? found.transform : null;
                if (player == null)
                {
                    return;
                }
            }

            var offense = Offenses.Current;
            if (offense == OffenseKind.None && pursuingRedLight)
            {
                offense = OffenseKind.RedLight;
            }

            var action = OfficerLogic.Decide(transform.position, player.position, offense, called || pursuingRedLight);
            if (action == OfficerAction.Pursue)
            {
                if (offense == OffenseKind.RedLight)
                {
                    pursuingRedLight = true;
                    Offenses.Raise(OffenseKind.RedLight);
                }

                transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
            }
            else if (action == OfficerAction.Stop)
            {
                stopped = true;
                pursuingRedLight = false;
                StoppedPlayer?.Invoke(offense);
            }
        }
    }
}

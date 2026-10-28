using System;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The countdown of an owner who found a stranger in the house: it runs while the player stays inside and
    /// ends in the arrest. Plain data, so it can be tested without a scene.
    /// </summary>
    public sealed class TrespassClock
    {
        public float Remaining { get; private set; }

        /// <summary>True once the time ran out. It stays true; the arrest happens once.</summary>
        public bool Expired { get; private set; }

        public TrespassClock(float graceSeconds)
        {
            Remaining = Mathf.Max(0f, graceSeconds);
        }

        /// <summary>Moves the clock on and returns true only on the step in which the time runs out.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (Expired)
            {
                return false;
            }

            Remaining = Mathf.Max(0f, Remaining - Mathf.Max(0f, deltaSeconds));
            if (Remaining > 0f)
            {
                return false;
            }

            Expired = true;
            return true;
        }
    }

    /// <summary>
    /// Sits in a house that is not the player's. The owner shouts a warning as the player comes in and, if the
    /// player is still inside when the time is up, <see cref="Arrested"/> is raised once. Walking out through
    /// the door loads the town, which destroys this object and cancels everything.
    /// </summary>
    public sealed class TrespassWatch : MonoBehaviour
    {
        [SerializeField] private string ownerName = "El dueño";
        [SerializeField] private float graceSeconds = 9f;

        private TrespassClock clock;

        /// <summary>The watch of the house the player is in now, or null in any other scene.</summary>
        public static TrespassWatch Active { get; private set; }

        /// <summary>Raised once when the owner calls the police and the player is still inside.</summary>
        public static event Action Arrested;

        public string OwnerName => ownerName;

        public float Remaining => clock != null ? clock.Remaining : 0f;

        public string Warning => Loc.F("{0}: ¡Salga de mi casa o llamo a la policía!", ownerName);

        public void Configure(string owner, float seconds)
        {
            ownerName = owner;
            graceSeconds = seconds;
            if (clock != null)
            {
                clock = new TrespassClock(seconds);
            }
        }

        private void OnEnable()
        {
            clock = new TrespassClock(graceSeconds);
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            if (SceneTransition.IsBusy)
            {
                return;
            }

            if (clock.Tick(Time.deltaTime))
            {
                Arrested?.Invoke();
            }
        }
    }
}

using System;
using UnityEngine;

namespace ColonQuest.World
{
    public enum StationType
    {
        Home,
        Store,
        Bank,
        Garage,
        Grocery,
        Lender,
        Pyramid,
        Kiosk,
        Soda,
        Coop
    }

    /// <summary>
    /// An interaction zone, like a "task" station in Among Us. Raises events when
    /// the player (tagged <see cref="PlayerTag"/>) walks in or out of the trigger.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Station : MonoBehaviour
    {
        public const string PlayerTag = "Player";

        [SerializeField] private StationType type;
        [SerializeField] private string displayName;

        public event Action<Station> PlayerEntered;
        public event Action<Station> PlayerExited;

        public StationType Type => type;
        public string DisplayName => displayName;
        public bool IsPlayerInside { get; private set; }

        public void Configure(StationType stationType, string stationName)
        {
            type = stationType;
            displayName = stationName;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(PlayerTag))
            {
                return;
            }

            IsPlayerInside = true;
            PlayerEntered?.Invoke(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(PlayerTag))
            {
                return;
            }

            IsPlayerInside = false;
            PlayerExited?.Invoke(this);
        }
    }
}

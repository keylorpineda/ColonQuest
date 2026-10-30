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
        Coop,

        /// <summary>Something from everyday life to look at or use: a bed, the fridge, a person. Not a task.</summary>
        Thing,

        /// <summary>A cash machine: it shows the account balance. Not a task.</summary>
        Atm,

        /// <summary>The real estate agency, or a house with a sign: rent and mortgage. Not a task.</summary>
        RealEstate,

        /// <summary>The car agency: a car with a down payment and installments. Not a task.</summary>
        CarDealer,

        /// <summary>The farm: coffee or cattle. Not a task.</summary>
        Farm,

        /// <summary>The reception of the hospital, where the health insurance is taken out. Not a task.</summary>
        HealthDesk,

        /// <summary>The wardrobe at home, where the swimsuit is bought. Not a task.</summary>
        Wardrobe,

        /// <summary>The shore of the beach: a swim once a month, with the swimsuit. Not a task.</summary>
        Beach,

        /// <summary>The time clock of the first job: the player clocks in and works the morning. Not a task.</summary>
        Work,

        /// <summary>The furniture corner of the store: cash down or in installments. Not a task.</summary>
        Furniture,

        /// <summary>The board of courses: classes that cost money and afternoons. Not a task.</summary>
        Courses
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
        [SerializeField] private string[] lines = new string[0];

        public event Action<Station> PlayerEntered;
        public event Action<Station> PlayerExited;

        public StationType Type => type;
        public string DisplayName => displayName;

        /// <summary>What the game says when the player uses a <see cref="StationType.Thing"/>, one line per use.</summary>
        public string[] Lines => lines;
        public bool IsPlayerInside { get; private set; }

        public void Configure(StationType stationType, string stationName)
        {
            type = stationType;
            displayName = stationName;
        }

        public void Configure(StationType stationType, string stationName, string[] stationLines)
        {
            Configure(stationType, stationName);
            lines = stationLines ?? new string[0];
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

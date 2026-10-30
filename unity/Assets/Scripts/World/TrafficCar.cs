using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A car that drives along a street. The audio director listens for these, so the sound of traffic comes
    /// from the cars that really pass near the player and from nothing else.
    /// </summary>
    public sealed class TrafficCar : MonoBehaviour
    {
        private static readonly List<TrafficCar> all = new List<TrafficCar>();

        public static IReadOnlyList<TrafficCar> All => all;

        /// <summary>Raised when a moving car runs into the player. The overlay decides what that costs.</summary>
        public static event Action PlayerHit;

        /// <summary>Where the car that hit the player was at that moment, so the player is thrown the right way.</summary>
        public static Vector2 LastHitFrom { get; private set; }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag(Station.PlayerTag))
            {
                LastHitFrom = transform.position;
                PlayerHit?.Invoke();
            }
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}

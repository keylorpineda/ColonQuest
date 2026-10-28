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

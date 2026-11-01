using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Pure deterministic math and hashing for building materials, facade variation and tico details.
    /// </summary>
    public static class BuildingMath
    {
        public static readonly string[] TicaHouseColors =
        {
            "#F2D7B6",
            "#BFE3D0",
            "#F7C6C7",
            "#FFE8A3",
            "#C9D8F0",
            "#E8D5F2",
            "#F4F1E8"
        };

        /// <summary>
        /// Stable non-negative 31-multiplier hash that does not vary across .NET runtime runs.
        /// </summary>
        public static int StableHash(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int seed = 0;
            unchecked
            {
                foreach (char c in text)
                {
                    seed = seed * 31 + c;
                }
            }

            if (seed == int.MinValue)
            {
                seed = 0;
            }

            return Mathf.Abs(seed);
        }

        public static string ChooseHouseColorHex(string name)
        {
            int seed = StableHash(name);
            return TicaHouseColors[seed % TicaHouseColors.Length];
        }

        public static bool HasWindowBars(string name)
        {
            int seed = StableHash(name);
            return (seed % 10) < 7;
        }

        public static bool HasTvAntenna(string name)
        {
            int seed = StableHash(name);
            return ((seed / 10) % 2) == 0;
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Names of the scenes the player moves between.</summary>
    public static class SceneNames
    {
        public const string Town = "Town";
        public const string Casa = "Casa";
        public const string Tienda = "Tienda";
        public const string Banco = "Banco";
    }

    /// <summary>
    /// Ids of the points where the player appears. A scene without a pending spawn id
    /// keeps the player wherever the scene was built, which is its "Start" point.
    /// </summary>
    public static class SpawnIds
    {
        public const string Start = "Start";

        // Inside a building, in front of the doormat.
        public const string FromTown = "FromTown";

        // In the town, in front of each building door.
        public const string TownFromCasa = "Town_FromCasa";
        public const string TownFromTienda = "Town_FromTienda";
        public const string TownFromBanco = "Town_FromBanco";
    }

    /// <summary>Sizes shared by the map builder and the tests.</summary>
    public static class MapLayout
    {
        /// <summary>Everything the camera may show in the town.</summary>
        public static readonly Rect TownBounds = new Rect(-26f, -18f, 52f, 33f);

        public const float TownCameraSize = 4.6f;
        public const float InteriorCameraSize = 3.9f;
    }
}

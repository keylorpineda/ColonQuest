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
        public static readonly Rect TownBounds = new Rect(-46f, -34f, 92f, 66f);

        // Where the rock face and the sea are drawn; the player is stopped well before them.
        public const float NorthEdgeY = 23f;

        /// <summary>
        /// Where the invisible walls stand. The world goes on past them (roads, trees, river, sea), so the
        /// player sees there is more but cannot get to it.
        /// </summary>
        public const float PlayWestX = -38f;
        public const float PlayEastX = 40f;
        public const float PlayNorthY = 17f;

        public const float ShoreY = -27.2f;

        // The river that runs north to south on the east side, and the pier on the beach.
        public const float RiverXMin = 28f;
        public const float RiverXMax = 31f;
        public const float PierXMin = 4f;
        public const float PierXMax = 7.2f;

        public const float TownCameraSize = 4.6f;
        public const float InteriorCameraSize = 3.9f;

        // The interiors: the Casa and the Tienda are 1.5 times the original size, the Banco twice as wide
        // and twice as deep (four times the area). The doormat lies one unit above the bottom wall.
        public static readonly Vector2 CasaSize = new Vector2(16.5f, 12f);
        public static readonly Vector2 TiendaSize = new Vector2(18f, 12.6f);
        public static readonly Vector2 BancoSize = new Vector2(24f, 16.8f);
        public static readonly Vector2 CasaMat = new Vector2(0f, -5f);
        public static readonly Vector2 TiendaMat = new Vector2(0f, -5.3f);
        public static readonly Vector2 BancoMat = new Vector2(0f, -7.4f);
    }
}

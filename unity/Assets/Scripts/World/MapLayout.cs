using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Names of the scenes the player moves between.</summary>
    public static class SceneNames
    {
        public const string Menu = "Menu";
        public const string Town = "Town";
        public const string Casa = "Casa";
        public const string Tienda = "Tienda";
        public const string Banco = "Banco";
        public const string Agencia = "Agencia";
        public const string Carcel = "Carcel";
        public const string Hospital = "Hospital";
        public const string HospitalArriba = "HospitalArriba";
        public const string Vecino = "Vecino";
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
        public const string TownFromAgencia = "Town_FromAgencia";
        public const string TownFromComisaria = "Town_FromComisaria";
        public const string TownFromHospital = "Town_FromHospital";
        public const string TownFromVecino = "Town_FromVecino";

        // Inside: the cell, the hospital bed and the two ends of the hospital stairs.
        public const string JailCell = "Carcel_Cell";
        public const string HospitalBed = "Hospital_Bed";
        public const string HospitalFromAbove = "Hospital_FromAbove";
    }

    /// <summary>Sizes shared by the map builder and the tests.</summary>
    public static class MapLayout
    {
        /// <summary>
        /// Everything the camera may show in the town: the original town plus the north and east districts, which
        /// bring the playable area to about twice its first size.
        /// </summary>
        public static readonly Rect TownBounds = new Rect(-46f, -34f, 118f, 91f);

        // Where the rock face was first drawn, north of the original town. The cliff, the mountains and the
        // waterfall are built from this line and moved up by NorthShift, so the north district fits below them.
        public const float NorthEdgeY = 23f;

        public const float NorthShift = 25f;

        /// <summary>Where the rock face and the sea are drawn now; the player is stopped well before them.</summary>
        public const float WorldNorthEdgeY = NorthEdgeY + NorthShift;

        /// <summary>The east side ends in a rock face between this line and <see cref="TownBounds"/>.</summary>
        public const float EastCliffX = 67.5f;

        /// <summary>
        /// Where the invisible walls stand. The world goes on past them (roads, trees, river, sea), so the
        /// player sees there is more but cannot get to it.
        /// </summary>
        public const float PlayWestX = -44.5f;
        public const float PlayEastX = 64f;
        public const float PlayNorthY = 42f;

        public const float ShoreY = -27.2f;

        // The river that runs north to south on the east side, and the pier on the beach.
        public const float RiverXMin = 28f;
        public const float RiverXMax = 31f;
        public const float PierXMin = 4f;
        public const float PierXMax = 7.2f;

        // The north district: an avenue runs east to west and a dirt path joins it to the first street.
        public const float AvenueY = 28f;
        public const float NorthPathX = 26.3f;

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

        public static readonly Vector2 AgenciaSize = new Vector2(22f, 14f);
        public static readonly Vector2 AgenciaMat = new Vector2(0f, -6f);
        public static readonly Vector2 CarcelSize = new Vector2(20f, 13f);
        public static readonly Vector2 CarcelMat = new Vector2(0f, -5.5f);
        public static readonly Vector2 HospitalSize = new Vector2(24f, 14f);
        public static readonly Vector2 HospitalMat = new Vector2(0f, -6f);
        public static readonly Vector2 HospitalArribaSize = new Vector2(22f, 12f);
        public static readonly Vector2 HospitalArribaMat = new Vector2(-8f, -5f);
        public static readonly Vector2 VecinoSize = new Vector2(16.5f, 12f);
        public static readonly Vector2 VecinoMat = new Vector2(0f, -5f);
    }
}

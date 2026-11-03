using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class MapLayoutTests
    {
        // The playable box of the original town, before the north and east districts: walls at x -44.5 and 40,
        // the shore at y -27.2 and the north wall at 17.
        private const float OriginalArea = (40f + 44.5f) * (17f + 27.2f);

        private static float PlayableArea => (MapLayout.PlayEastX - MapLayout.PlayWestX) * (MapLayout.PlayNorthY - MapLayout.ShoreY);

        [Test]
        public void ThePlayableArea_IsAboutTwiceTheOriginalTown()
        {
            float ratio = PlayableArea / OriginalArea;

            Assert.GreaterOrEqual(ratio, 1.9f);
            Assert.LessOrEqual(ratio, 2.3f);
        }

        [Test]
        public void TheCameraBounds_ReachPastEveryWallAndRockFace()
        {
            var bounds = MapLayout.TownBounds;

            Assert.Less(bounds.xMin, MapLayout.PlayWestX);
            Assert.Greater(bounds.xMax, MapLayout.EastCliffX);
            Assert.Less(bounds.yMin, MapLayout.ShoreY);
            Assert.GreaterOrEqual(bounds.yMax, MapLayout.WorldNorthEdgeY + 5.5f, "the north cliff is 5.5 tall");
        }

        [Test]
        public void TheRockFaces_StandBeyondTheInvisibleWalls()
        {
            Assert.Greater(MapLayout.EastCliffX, MapLayout.PlayEastX + 1f);
            Assert.Greater(MapLayout.WorldNorthEdgeY, MapLayout.PlayNorthY + 3f);
            Assert.AreEqual(MapLayout.NorthEdgeY + MapLayout.NorthShift, MapLayout.WorldNorthEdgeY);
        }

        [Test]
        public void TheAvenue_RunsBetweenTheForestAndTheNorthWall()
        {
            Assert.Greater(MapLayout.AvenueY, 24f, "north of the forest of the original town");
            Assert.Less(MapLayout.AvenueY, MapLayout.PlayNorthY - 8f, "with room for a row of buildings north of it");
        }

        [Test]
        public void TheDirtPath_StaysWestOfTheRiver()
        {
            Assert.Less(MapLayout.NorthPathX + 1.25f, MapLayout.RiverXMin);
            Assert.Greater(MapLayout.NorthPathX - 1.25f, 23.5f, "east of the garage, which ends at 23.5");
        }
    }
}

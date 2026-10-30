using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class TrafficTests
    {
        // A crosswalk from x 2.4 to 4.8, like the one east of the plaza.
        private const float ZoneMin = 2.4f;
        private const float ZoneMax = 4.8f;

        [Test]
        public void StopLine_IsBeforeTheStripesOnEachSide()
        {
            Assert.Less(CarRules.StopLine(ZoneMin, ZoneMax, 1), ZoneMin);
            Assert.Greater(CarRules.StopLine(ZoneMin, ZoneMax, -1), ZoneMax);
        }

        [Test]
        public void Car_StopsBeforeTheLineWhilePedestriansHaveGreen()
        {
            float line = CarRules.StopLine(ZoneMin, ZoneMax, 1);
            float room = CarRules.RoomTo(-3f, 1, line);

            Assert.IsTrue(CarRules.MustStop(room, walkLight: true));
            Assert.IsFalse(CarRules.MustStop(room, walkLight: false), "red for pedestrians is green for cars");
        }

        [Test]
        public void Car_AlreadyOverTheLineClearsTheCrosswalk()
        {
            float line = CarRules.StopLine(ZoneMin, ZoneMax, 1);
            float room = CarRules.RoomTo(ZoneMin + 0.5f, 1, line);

            Assert.Less(room, 0f);
            Assert.IsFalse(CarRules.MustStop(room, walkLight: true));
        }

        [Test]
        public void Car_IgnoresALightThatIsStillFarAway()
        {
            float line = CarRules.StopLine(ZoneMin, ZoneMax, 1);
            Assert.IsFalse(CarRules.MustStop(CarRules.RoomTo(-30f, 1, line), walkLight: true));
        }

        [Test]
        public void Car_GoingWestStopsOnTheEastSide()
        {
            float line = CarRules.StopLine(ZoneMin, ZoneMax, -1);
            float room = CarRules.RoomTo(9f, -1, line);

            Assert.Greater(room, 0f);
            Assert.IsTrue(CarRules.MustStop(room, walkLight: true));
        }

        [Test]
        public void TargetSpeed_SlowsDownToAStopAtTheLine()
        {
            Assert.AreEqual(2.4f, CarRules.TargetSpeed(2.4f, 20f), 0.0001f);
            Assert.AreEqual(2.4f, CarRules.TargetSpeed(2.4f, float.PositiveInfinity), 0.0001f);
            Assert.Less(CarRules.TargetSpeed(2.4f, CarRules.BrakeDistance * 0.5f), 2.4f);
            Assert.AreEqual(0f, CarRules.TargetSpeed(2.4f, 0f));
            Assert.AreEqual(0f, CarRules.TargetSpeed(2.4f, -1f));
        }

        [Test]
        public void Cars_KeepTheirDistanceInARow()
        {
            // The car ahead waits at x 10; this one, going east, must not get closer than two half cars plus the gap.
            float stopAt = 10f - 2f * CarRules.HalfLength - CarRules.MinGap;
            Assert.AreEqual(0f, CarRules.RoomBehind(stopAt, 1, 10f), 0.0001f);
            Assert.Greater(CarRules.RoomBehind(stopAt - 2f, 1, 10f), 0f);
            Assert.Less(CarRules.RoomBehind(stopAt + 0.5f, 1, 10f), 0f);
        }

        [Test]
        public void Cars_BehindDoNotCount()
        {
            Assert.AreEqual(float.PositiveInfinity, CarRules.RoomBehind(10f, 1, 5f));
            Assert.AreEqual(float.PositiveInfinity, CarRules.RoomBehind(5f, -1, 10f));
        }

        [Test]
        public void Speed_ChangesWithoutJumps()
        {
            float next = CarRules.NextSpeed(2.4f, 0f, 0.1f);
            Assert.AreEqual(2.4f - CarRules.Acceleration * 0.1f, next, 0.0001f);
            Assert.AreEqual(0f, CarRules.NextSpeed(0.1f, 0f, 1f));
        }
    }
}

using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class HouseVisitTests
    {
        [Test]
        public void NeighborHasDog_OnlySomeHousesKeepOne()
        {
            Assert.IsTrue(HouseVisit.NeighborHasDog("Town_From_Casa_Vecina_A"));
            Assert.IsTrue(HouseVisit.NeighborHasDog("Town_From_Casa_Almendros_4"));
            Assert.IsFalse(HouseVisit.NeighborHasDog("Town_From_Casa_Almendros_3"));
            Assert.IsFalse(HouseVisit.NeighborHasDog("Town_From_Casa_Almendros_5"));
            Assert.IsFalse(HouseVisit.NeighborHasDog(string.Empty));
            Assert.IsFalse(HouseVisit.NeighborHasDog(null));
        }

        [Test]
        public void Enter_RemembersTheListingAndClearsIt()
        {
            HouseVisit.Enter("casa_vecina_d");
            Assert.IsTrue(HouseVisit.InListing);
            Assert.AreEqual("casa_vecina_d", HouseVisit.ListingId);

            HouseVisit.Enter(null);
            Assert.IsFalse(HouseVisit.InListing);
        }
    }
}

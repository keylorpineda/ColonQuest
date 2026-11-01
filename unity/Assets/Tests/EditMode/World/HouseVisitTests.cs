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

        [Test]
        public void VariantFor_ReturnsDeterministicValuesBetweenZeroAndThree()
        {
            Assert.AreEqual(0, HouseVisit.VariantFor(null));
            Assert.AreEqual(0, HouseVisit.VariantFor(string.Empty));

            int v1 = HouseVisit.VariantFor("Town_From_Casa_Vecina_A");
            int v2 = HouseVisit.VariantFor("Town_From_Casa_Vecina_A");
            Assert.AreEqual(v1, v2);
            Assert.GreaterOrEqual(v1, 0);
            Assert.LessOrEqual(v1, 3);

            // Test coverage across typical neighbor doors
            var variantsFound = new System.Collections.Generic.HashSet<int>();
            string[] doors =
            {
                "Town_From_Casa_Vecina_A",
                "Town_From_Casa_Vecina_B",
                "Town_From_Casa_Vecina_C",
                "Town_From_Casa_Vecina_D",
                "Town_From_Casa_Almendros_1",
                "Town_From_Casa_Almendros_2",
                "Town_From_Casa_Almendros_3",
                "Town_From_Casa_Almendros_4",
                "Town_From_Casa_Almendros_5"
            };

            foreach (var door in doors)
            {
                int variant = HouseVisit.VariantFor(door);
                Assert.GreaterOrEqual(variant, 0);
                Assert.LessOrEqual(variant, 3);
                variantsFound.Add(variant);
            }

            Assert.GreaterOrEqual(variantsFound.Count, 3);
        }
    }
}

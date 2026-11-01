using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class BuildingMathTests
    {
        [Test]
        public void StableHash_IsDeterministicAndNonNegative()
        {
            Assert.AreEqual(0, BuildingMath.StableHash(null));
            Assert.AreEqual(0, BuildingMath.StableHash(string.Empty));

            int h1 = BuildingMath.StableHash("Casa_Vecina_A");
            int h2 = BuildingMath.StableHash("Casa_Vecina_A");
            Assert.AreEqual(h1, h2);
            Assert.GreaterOrEqual(h1, 0);

            int hCasa = BuildingMath.StableHash("Casa");
            int hBanco = BuildingMath.StableHash("Banco");
            Assert.AreNotEqual(hCasa, hBanco);
        }

        [Test]
        public void ChooseHouseColorHex_ReturnsValidHexFromPalette()
        {
            string color = BuildingMath.ChooseHouseColorHex("Casa_Almendros_1");
            Assert.IsTrue(color.StartsWith("#"));
            Assert.AreEqual(7, color.Length);
            CollectionAssert.Contains(BuildingMath.TicaHouseColors, color);
        }

        [Test]
        public void HasWindowBars_ReturnsDeterministicBoolean()
        {
            bool b1 = BuildingMath.HasWindowBars("Casa_Vecina_A");
            bool b2 = BuildingMath.HasWindowBars("Casa_Vecina_A");
            Assert.AreEqual(b1, b2);

            int countTrue = 0;
            for (int i = 0; i < 20; i++)
            {
                if (BuildingMath.HasWindowBars($"Casa_{i}"))
                {
                    countTrue++;
                }
            }
            Assert.Greater(countTrue, 5);
            Assert.Less(countTrue, 20);
        }

        [Test]
        public void HasTvAntenna_ReturnsDeterministicBoolean()
        {
            bool a1 = BuildingMath.HasTvAntenna("Casa_Vecina_B");
            bool a2 = BuildingMath.HasTvAntenna("Casa_Vecina_B");
            Assert.AreEqual(a1, a2);

            int countTrue = 0;
            for (int i = 0; i < 20; i++)
            {
                if (BuildingMath.HasTvAntenna($"Casa_{i}"))
                {
                    countTrue++;
                }
            }
            Assert.Greater(countTrue, 2);
            Assert.Less(countTrue, 18);
        }
    }
}

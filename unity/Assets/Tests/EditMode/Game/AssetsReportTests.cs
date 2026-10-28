using System.Linq;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class AssetsReportTests
    {
        [Test]
        public void Build_SaysNothingWhenThePlayerBoughtNothing()
        {
            Assert.IsEmpty(AssetsReport.Build(new Holdings(), 3));
            Assert.IsEmpty(AssetsReport.Build(null, 3));
        }

        [Test]
        public void Build_ProjectsACoffeeStakeAtRealTime()
        {
            var holdings = new Holdings();
            holdings.AddFarm(FarmKind.Coffee, 100_000L, 0);

            var lines = AssetsReport.Build(holdings, GameRules.MonthsToPlay);

            string farm = lines[0];
            StringAssert.Contains("Finca (café)", farm);
            StringAssert.Contains(Money.Format(100_000L), farm);
            StringAssert.Contains("36 meses", farm);
            StringAssert.Contains("todavía faltan 33", farm, "3 of the 36 months went by during the run");
            StringAssert.Contains(Money.Format(70_000L), farm, "the bad scenario");
            StringAssert.Contains(Money.Format(125_000L), farm, "the normal scenario");
            StringAssert.Contains(Money.Format(160_000L), farm, "the good scenario");
        }

        [Test]
        public void Build_ShowsTheRangeOfMonthsForCattle()
        {
            var holdings = new Holdings();
            holdings.AddFarm(FarmKind.Cattle, 200_000L, 1);

            string farm = AssetsReport.Build(holdings, 3)[0];

            StringAssert.Contains("Finca (ganado)", farm);
            StringAssert.Contains("18 a 24 meses", farm);
            StringAssert.Contains("pasaron 2", farm, "bought after one month, run of three");
            StringAssert.Contains("todavía faltan 19", farm);
        }

        [Test]
        public void Build_DescribesARentalAndAMortgageDifferently()
        {
            var rental = new Holdings();
            rental.SetHome(new HomeHolding("Casa Vecina D", false, 0L, 260_000L, 12));
            var owner = new Holdings();
            owner.SetHome(new HomeHolding("Casa Vecina B", true, 38_000_000L, 405_000L, 120));

            string rent = AssetsReport.Build(rental, 3)[0];
            string mortgage = AssetsReport.Build(owner, 3)[0];

            StringAssert.Contains("alquilas Casa Vecina D", rent);
            StringAssert.Contains("12 meses", rent);
            StringAssert.Contains("pagaste 3", rent);
            StringAssert.Contains("compraste Casa Vecina B con hipoteca", mortgage);
            StringAssert.Contains("faltan 117 cuotas", mortgage);
            StringAssert.Contains(Money.Format(38_000_000L), mortgage);
        }

        [Test]
        public void Build_MentionsTheRunningCostsOfTheCar()
        {
            var holdings = new Holdings();
            holdings.SetCar(new CarHolding("Compacto usado 2017", 2_720_000L, 50_000L));

            string car = AssetsReport.Build(holdings, 3)[0];

            StringAssert.Contains("Compacto usado 2017", car);
            StringAssert.Contains("gasolina", car);
            StringAssert.Contains(Money.Format(50_000L), car);
            StringAssert.Contains(Money.Format(2_720_000L), car);
        }

        [Test]
        public void Build_AlwaysEndsWithTheWarningAboutTheFigures()
        {
            var holdings = new Holdings();
            holdings.AddFarm(FarmKind.Coffee, 50_000L, 0);
            holdings.SetCar(new CarHolding("Carro", 1_000_000L, 40_000L));

            var lines = AssetsReport.Build(holdings, 3);

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(MarketAssumptions.Disclaimer, lines.Last());
        }
    }
}

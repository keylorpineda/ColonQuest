using System.Collections.Generic;
using System.Linq;
using ColonQuest.Api;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class MarketAssumptionsTests
    {
        [Test]
        public void Homes_HaveUniqueIdsAndEitherAPriceOrARent()
        {
            Assert.AreEqual(MarketAssumptions.Homes.Count, MarketAssumptions.Homes.Select(h => h.Id).Distinct().Count());
            foreach (var home in MarketAssumptions.Homes)
            {
                if (home.ForSale)
                {
                    Assert.Greater(home.Price, 0L, home.Id);
                    Assert.AreEqual(0L, home.MonthlyRent, home.Id);
                }
                else
                {
                    Assert.Greater(home.MonthlyRent, 0L, home.Id);
                    Assert.AreEqual(0L, home.Price, home.Id);
                }
            }

            Assert.IsTrue(MarketAssumptions.Homes.Any(h => h.ForSale));
            Assert.IsTrue(MarketAssumptions.Homes.Any(h => !h.ForSale));
        }

        [Test]
        public void FindHome_ReturnsTheListingOrNull()
        {
            Assert.AreEqual("Casa Vecina D", MarketAssumptions.FindHome("casa_vecina_d").Name);
            Assert.IsNull(MarketAssumptions.FindHome("no_existe"));
        }

        [Test]
        public void TheMortgageTermIsTheLongestTheContractAllows()
        {
            Assert.AreEqual(ContractLimits.MaxInstallmentMonths, MarketAssumptions.MortgageTermMonths);
        }

        [Test]
        public void EveryListingProducesAQuote()
        {
            foreach (var home in MarketAssumptions.Homes)
            {
                if (home.ForSale)
                {
                    Assert.DoesNotThrow(() => MarketAssumptions.QuoteMortgage(home), home.Id);
                }
                else
                {
                    Assert.DoesNotThrow(() => MarketAssumptions.QuoteRent(home), home.Id);
                }
            }
        }

        [Test]
        public void ABuyerOnTheStartingSalaryCannotAffordAHouse()
        {
            var player = GameRules.NewPlayer();
            foreach (var home in MarketAssumptions.Homes.Where(h => h.ForSale))
            {
                var quote = MarketAssumptions.QuoteMortgage(home);

                Assert.Greater(quote.UpfrontCost, player.Cash * 10L, home.Id + ": the down payment is far beyond the starting cash");
                Assert.GreaterOrEqual(FinanceEngine.ShareOfSalaryPct(quote.MonthlyPayment, player.SalaryNet), 50L, home.Id + ": the payment eats half the salary or more");
                Assert.Greater(quote.TotalInterest, 0L, home.Id);
            }
        }

        [Test]
        public void Cars_HaveUniqueIdsAndAFeasibleDownPayment()
        {
            Assert.AreEqual(MarketAssumptions.Cars.Count, MarketAssumptions.Cars.Select(c => c.Id).Distinct().Count());
            foreach (var car in MarketAssumptions.Cars)
            {
                Assert.Greater(car.DownPayment, 0L, car.Id);
                Assert.Less(car.DownPayment, car.Price, car.Id);
                Assert.LessOrEqual(car.TermMonths, ContractLimits.MaxInstallmentMonths, car.Id);
                Assert.Less(car.ResaleValue, car.Price, car.Id + ": a car loses value as soon as it leaves the agency");
                Assert.DoesNotThrow(() => car.ToPlan(), car.Id);
            }
        }

        [Test]
        public void TheSellerOfferOfACarAgreesWithTheEngineQuote()
        {
            foreach (var car in MarketAssumptions.Cars)
            {
                var offer = car.ToSellerOffer();
                var quote = FinanceEngine.QuoteCar(car.ToPlan());

                Assert.AreEqual(quote.MonthlyInstallment, offer.MonthlyCharge, car.Id + " monthly");
                Assert.AreEqual(quote.TotalPaidForTheCar, offer.TotalCost, car.Id + " total");
                Assert.AreEqual(quote.ExtraOverPrice, offer.ExtraOverCash, car.Id + " extra");
                Assert.AreEqual(car.DownPayment, offer.DownPayment, car.Id + " down payment");
            }
        }

        [Test]
        public void CarOffersAreClientOnlyAndTheBackendCatalogIsUntouched()
        {
            Assert.AreEqual(4, SellerOffer.Catalog.Count, "SellerOffer.Catalog mirrors server/app/negotiation/seller_sheet.py");
            foreach (var offer in SellerOffer.Catalog)
            {
                Assert.AreEqual(0L, offer.DownPayment, offer.OfferId);
            }

            foreach (var car in MarketAssumptions.Cars)
            {
                Assert.IsNull(SellerOffer.Find(car.ToSellerOffer().OfferId), car.Id + " must not be a backend offer");
            }
        }

        [Test]
        public void FarmOptions_HaveOrderedScenariosAndRealisticWaits()
        {
            Assert.AreEqual(2, MarketAssumptions.Farm.Count);
            foreach (var option in MarketAssumptions.Farm)
            {
                Assert.LessOrEqual(option.LowPct, option.BasePct, option.Name);
                Assert.LessOrEqual(option.BasePct, option.HighPct, option.Name);
                Assert.LessOrEqual(option.MonthsMin, option.MonthsToPayoff, option.Name);
                Assert.GreaterOrEqual(option.MonthsMax, option.MonthsToPayoff, option.Name);
                Assert.Greater(option.MonthsToPayoff, GameRules.MonthsToPlay, option.Name + ": the money must outlast the run");
                Assert.Less(option.LowPct, 0, option.Name + ": a farm can lose money");
            }

            var cattle = MarketAssumptions.FindFarm(FarmKind.Cattle);
            Assert.AreEqual(18, cattle.MonthsMin);
            Assert.AreEqual(24, cattle.MonthsMax);
        }

        [Test]
        public void FarmStakes_ArePositiveAndGrow()
        {
            var stakes = MarketAssumptions.FarmStakes;
            Assert.IsNotEmpty(stakes);
            Assert.Greater(stakes[0], 0L);
            for (int i = 1; i < stakes.Length; i++)
            {
                Assert.Greater(stakes[i], stakes[i - 1]);
            }

            foreach (var option in MarketAssumptions.Farm)
            {
                foreach (long stake in stakes)
                {
                    Assert.DoesNotThrow(() => option.Project(stake));
                }
            }
        }

        [Test]
        public void TheDisclaimerSaysTheFiguresAreExamples()
        {
            StringAssert.Contains("ejemplo", MarketAssumptions.Disclaimer);
            StringAssert.Contains("verificarlas", MarketAssumptions.Disclaimer);
            StringAssert.Contains("ejemplo", MarketAssumptions.ShortDisclaimer);
        }
    }
}

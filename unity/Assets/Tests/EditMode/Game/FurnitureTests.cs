using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class FurnitureTests
    {
        private static FurnitureModel Table => Find(FurnitureKind.Table);

        private static FurnitureModel Find(FurnitureKind kind)
        {
            foreach (var item in MarketAssumptions.Furniture)
            {
                if (item.Kind == kind)
                {
                    return item;
                }
            }

            Assert.Fail("not on sale: " + kind);
            return null;
        }

        [Test]
        public void Catalog_SellsEveryPieceOfTheRoom()
        {
            foreach (FurnitureKind kind in System.Enum.GetValues(typeof(FurnitureKind)))
            {
                Assert.IsNotNull(Find(kind));
            }
        }

        [Test]
        public void CashDown_TakesTheCashAndDeliversThePiece()
        {
            var loop = new GameLoop();
            long cash = loop.State.Cash;
            int mood = loop.State.Wellbeing;

            var purchase = loop.BuyFurniture(Table);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(cash - Table.Price, loop.State.Cash);
            Assert.IsTrue(loop.Holdings.Has(FurnitureKind.Table));
            Assert.Greater(loop.State.Wellbeing, mood);
            Assert.AreEqual(-Table.Price, purchase.Outcome.CashDelta);
        }

        [Test]
        public void CashDown_WithoutTheCashChangesNothing()
        {
            var loop = new GameLoop();
            loop.State.Cash = 1_000L;

            Assert.AreEqual(PurchaseFailure.NotEnoughCash, loop.CheckBuyFurniture(Table));
            var purchase = loop.BuyFurniture(Table);

            Assert.IsFalse(purchase.Result.Bought);
            Assert.AreEqual(1_000L, loop.State.Cash);
            Assert.IsFalse(loop.Holdings.Has(FurnitureKind.Table));
        }

        [Test]
        public void TheSamePieceCannotBeBoughtTwice()
        {
            var loop = new GameLoop();
            loop.BuyFurniture(Table);

            Assert.AreEqual(PurchaseFailure.AlreadyHasFurniture, loop.CheckBuyFurniture(Table));
            Assert.AreEqual(PurchaseFailure.AlreadyHasFurniture, loop.CheckBuyFurnitureInInstallments(Table));
        }

        [Test]
        public void Installments_PayTheCommissionAndStartTheMonthlyCharge()
        {
            var loop = new GameLoop();
            long cash = loop.State.Cash;
            int installments = loop.State.Installments.Count;
            var bed = Find(FurnitureKind.Bed);
            var offer = bed.ToSellerOffer();

            var purchase = loop.BuyFurnitureInInstallments(bed);

            Assert.IsTrue(purchase.Result.Bought);
            Assert.AreEqual(cash - bed.Commission, loop.State.Cash);
            Assert.AreEqual(installments + 1, loop.State.Installments.Count);
            Assert.AreEqual(offer.MonthlyCharge, purchase.Outcome.InstallmentsPerMonthDelta);
            Assert.Greater(offer.TotalCost, bed.Price, "the installments cost more than the cash price");
            Assert.IsTrue(loop.Holdings.Has(FurnitureKind.Bed));
        }

        [Test]
        public void Installments_WithoutTheCommissionChangeNothing()
        {
            var loop = new GameLoop();
            loop.State.Cash = 0L;
            int installments = loop.State.Installments.Count;
            var bed = Find(FurnitureKind.Bed);

            Assert.AreEqual(PurchaseFailure.NotEnoughCash, loop.CheckBuyFurnitureInInstallments(bed));
            Assert.IsFalse(loop.BuyFurnitureInInstallments(bed).Result.Bought);
            Assert.AreEqual(installments, loop.State.Installments.Count);
        }

        [Test]
        public void FurnitureOffers_AreAnsweredByTheLocalSeller()
        {
            var negotiation = new Negotiation(Table.ToSellerOffer());
            Assert.IsTrue(negotiation.IsLocalOnly);
        }

        [Test]
        public void Rent_GoesOutEveryMonthUntilMovingAway()
        {
            var loop = new GameLoop();
            loop.ClockIn();
            long cash = loop.State.Cash;
            var report = loop.CloseMonth();

            Assert.AreEqual(MarketAssumptions.RoomRent, loop.LastRoomRent);
            Assert.AreEqual(report.SalaryCredited, loop.State.SalaryNet);
            Assert.Less(report.EndingCash, cash + report.SalaryCredited - GameRules.LivingExpenses - MarketAssumptions.RoomRent + 1L);

            loop.Holdings.SetHome(new HomeHolding("Casa Vecina D", false, 0L, 260_000L, 12));
            loop.CloseMonth();
            Assert.AreEqual(0L, loop.LastRoomRent);
        }

        [Test]
        public void ABed_RestsMoreThanTheMattress()
        {
            var onMattress = new GameLoop();
            var onBed = new GameLoop();
            onBed.Holdings.AddFurniture(FurnitureKind.Bed);
            onMattress.State.Wellbeing = 40;
            onBed.State.Wellbeing = 40;

            onMattress.Rest();
            onBed.Rest();

            Assert.AreEqual(onMattress.State.Wellbeing + MarketAssumptions.BedRestBonus, onBed.State.Wellbeing);
        }

        [Test]
        public void TheFurnitureIsSavedWithTheRun()
        {
            var loop = new GameLoop();
            loop.BuyFurniture(Table);

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.IsTrue(restored.Holdings.Has(FurnitureKind.Table));
            Assert.IsFalse(restored.Holdings.Has(FurnitureKind.Bed));
        }

        [Test]
        public void TheFurnitureCorner_IsNotATask()
        {
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.Furniture));
        }

        [TestCase("Ver los muebles")]
        [TestCase("Mesa con dos sillas")]
        [TestCase("Alquiler del cuarto  {0}")]
        public void TheTextsOfTheRoom_AreTranslated(string spanish)
        {
            Assert.AreNotEqual(spanish, Loc.Translate(spanish, GameLanguage.English));
        }
    }
}

using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class BillBookTests
    {
        private static long Flat(BillKind kind) => 10_000L;

        private static void ToLastWeek(GameLoop loop)
        {
            for (int i = 1; i < GameRules.DaysPerMonth; i++)
            {
                loop.ClockIn();
                loop.Sleep();
            }
        }

        [Test]
        public void Arrive_OnlyOnceAMonth()
        {
            var book = new BillBook();
            book.Arrive(Flat);
            book.Arrive(Flat);

            Assert.AreEqual(4, book.Due.Count);
            Assert.AreEqual(40_000L, book.TotalDue);
        }

        [Test]
        public void CloseMonth_AddsTheFeeToWhatArrivedBefore()
        {
            var book = new BillBook();
            book.Arrive(Flat);
            long fees = book.CloseMonth(Flat);

            Assert.AreEqual(4_000L, fees);
            Assert.AreEqual(44_000L, book.TotalDue);
            Assert.AreEqual(1, book.Find(BillKind.Water).MonthsLate);
            Assert.IsFalse(book.IsCut(BillKind.Water));
        }

        [Test]
        public void CloseMonth_ForBillsThatNeverArrivedIsNotLate()
        {
            var book = new BillBook();
            Assert.AreEqual(0L, book.CloseMonth(Flat));

            Assert.AreEqual(40_000L, book.TotalDue);
            Assert.AreEqual(0, book.Find(BillKind.Internet).MonthsLate);
        }

        [Test]
        public void TwoClosesUnpaid_CutTheServiceUntilItIsPaid()
        {
            var book = new BillBook();
            book.Arrive(Flat);
            book.CloseMonth(Flat);
            book.Arrive(Flat);
            book.CloseMonth(Flat);

            Assert.IsTrue(book.IsCut(BillKind.Electricity));
            book.Settle(BillKind.Electricity);
            Assert.IsFalse(book.IsCut(BillKind.Electricity));
            Assert.IsTrue(book.IsCut(BillKind.Water));
        }

        [Test]
        public void Amounts_GrowWithTheFurnitureAndTheHome()
        {
            var holdings = new Holdings();
            long bare = MarketAssumptions.BillAmount(BillKind.Electricity, holdings);

            holdings.AddFurniture(FurnitureKind.Fridge);
            long withFridge = MarketAssumptions.BillAmount(BillKind.Electricity, holdings);
            Assert.Greater(withFridge, bare);

            holdings.AddFurniture(FurnitureKind.Tv);
            Assert.Greater(MarketAssumptions.BillAmount(BillKind.Electricity, holdings), withFridge);

            long water = MarketAssumptions.BillAmount(BillKind.Water, holdings);
            holdings.SetHome(new HomeHolding("Casa Vecina D", false, 0L, 260_000L, 12));
            Assert.Greater(MarketAssumptions.BillAmount(BillKind.Water, holdings), water);
        }

        [Test]
        public void Loop_BillsArriveInTheLastWeek()
        {
            var loop = new GameLoop();
            Assert.AreEqual(0, loop.Bills.Due.Count);

            ToLastWeek(loop);

            Assert.AreEqual(4, loop.Bills.Due.Count);
        }

        [Test]
        public void Loop_PayingTakesTheCashAndClearsTheBill()
        {
            var loop = new GameLoop();
            ToLastWeek(loop);
            long cash = loop.State.Cash;
            long amount = loop.Bills.Find(BillKind.Water).Amount;

            Assert.IsTrue(loop.PayBill(BillKind.Water));

            Assert.AreEqual(cash - amount, loop.State.Cash);
            Assert.IsNull(loop.Bills.Find(BillKind.Water));
            Assert.IsFalse(loop.PayBill(BillKind.Water), "there is nothing left to pay");
        }

        [Test]
        public void Loop_WithoutTheCashTheBillStays()
        {
            var loop = new GameLoop();
            ToLastWeek(loop);
            loop.State.Cash = 0L;

            Assert.IsFalse(loop.PayBill(BillKind.Water));
            Assert.IsNotNull(loop.Bills.Find(BillKind.Water));
            Assert.AreEqual(0L, loop.State.Cash);
        }

        [Test]
        public void Loop_PayAllPaysWhatTheCashReaches()
        {
            var loop = new GameLoop();
            ToLastWeek(loop);
            loop.State.Cash = loop.Bills.Find(BillKind.Electricity).Amount;

            Assert.AreEqual(1, loop.PayAllBills());
            Assert.AreEqual(3, loop.Bills.Due.Count);
            Assert.AreEqual(0L, loop.State.Cash);
        }

        [Test]
        public void Loop_UnpaidBillsCutTheServiceAfterTwoCloses()
        {
            var loop = new GameLoop();
            ToLastWeek(loop);
            loop.CloseMonth();
            Assert.Greater(loop.Bills.LastLateFees, 0L);
            Assert.IsFalse(loop.Bills.IsCut(BillKind.Electricity));

            ToLastWeek(loop);
            loop.CloseMonth();
            Assert.IsTrue(loop.Bills.IsCut(BillKind.Electricity));
        }

        [Test]
        public void Loop_TheBillsAreSavedWithTheRun()
        {
            var loop = new GameLoop();
            ToLastWeek(loop);
            loop.PayBill(BillKind.Water);

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.AreEqual(3, restored.Bills.Due.Count);
            Assert.AreEqual(loop.Bills.TotalDue, restored.Bills.TotalDue);
            Assert.IsTrue(restored.Bills.ArrivedThisMonth);
        }

        [TestCase("Recibos")]
        [TestCase("Cortaron la luz. La tele no prende.")]
        public void TheTextsOfTheBills_AreTranslated(string spanish)
        {
            Assert.AreNotEqual(spanish, Loc.Translate(spanish, GameLanguage.English));
        }
    }
}

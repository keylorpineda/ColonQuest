using System.Linq;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class GoalsAndGradesTests
    {
        private static Goal Find(GameLoop loop, GoalKind kind) => loop.Goals.Goals.First(g => g.Kind == kind);

        private static char Grade(GameLoop loop, string concept)
        {
            return ReportCard.Build(loop.State, loop.Holdings, loop.Bills).First(l => l.Concept == concept).Grade;
        }

        [Test]
        public void Propose_ThreeGoalsFromTheSituation()
        {
            var withCard = GoalBook.Propose(GameRules.NewPlayer());
            Assert.AreEqual(3, withCard.Goals.Count);
            Assert.IsTrue(withCard.Goals.Any(g => g.Kind == GoalKind.PayDownCard));

            var state = GameRules.NewPlayer();
            state.CreditCard.Balance = 0L;
            var noCard = GoalBook.Propose(state);
            Assert.AreEqual(3, noCard.Goals.Count);
            Assert.IsFalse(noCard.Goals.Any(g => g.Kind == GoalKind.PayDownCard));
            Assert.IsTrue(noCard.Goals.Any(g => g.Kind == GoalKind.Savings));
        }

        [Test]
        public void Fund_MovesCashAndReachesTheGoalOnce()
        {
            var loop = new GameLoop();
            loop.State.Cash = 200_000L;
            Assert.AreEqual(10_000L, loop.PutInFund(10_000L));
            Assert.AreEqual(190_000L, loop.State.Cash);
            Assert.AreEqual(10_000L, loop.State.EmergencyFund);
            Assert.AreEqual(0.1f, GoalBook.Progress(Find(loop, GoalKind.EmergencyFund), loop.State, loop.Holdings), 0.001f);
            Assert.IsEmpty(loop.Goals.TakeNewlyDone(loop.State, loop.Holdings));

            loop.PutInFund(90_000L);
            Assert.AreEqual(1, loop.Goals.TakeNewlyDone(loop.State, loop.Holdings).Count);
            Assert.IsEmpty(loop.Goals.TakeNewlyDone(loop.State, loop.Holdings), "it is celebrated once");

            loop.State.EmergencyFund = 0L;
            Assert.AreEqual(1f, GoalBook.Progress(Find(loop, GoalKind.EmergencyFund), loop.State, loop.Holdings), "a goal reached stays reached");
        }

        [Test]
        public void Card_PaysOnlyWhatIsOwedAndWhatTheCashReaches()
        {
            var loop = new GameLoop();
            loop.State.Cash = 5_000L;
            long balance = loop.State.CreditCard.Balance;

            Assert.AreEqual(5_000L, loop.PayTowardCard(10_000L));
            Assert.AreEqual(0L, loop.State.Cash);
            Assert.AreEqual(balance - 5_000L, loop.State.CreditCard.Balance);

            loop.State.Cash = 900_000L;
            Assert.AreEqual(balance - 5_000L, loop.PayTowardCard(900_000L));
            Assert.AreEqual(0L, loop.State.CreditCard.Balance);
        }

        [Test]
        public void Card_GoalIsHalfTheStartingBalance()
        {
            var loop = new GameLoop();
            var goal = Find(loop, GoalKind.PayDownCard);
            Assert.AreEqual(loop.State.CreditCard.Balance / 2L, goal.Target);
            Assert.AreEqual(0f, GoalBook.Progress(goal, loop.State, loop.Holdings), 0.001f);

            loop.State.CreditCard.Balance = goal.Target;
            Assert.AreEqual(1f, GoalBook.Progress(goal, loop.State, loop.Holdings), 0.001f);
        }

        [Test]
        public void Furniture_CountsTowardTheRoom()
        {
            var loop = new GameLoop();
            loop.Holdings.AddFurniture(FurnitureKind.Bed);
            Assert.AreEqual(0.25f, GoalBook.Progress(Find(loop, GoalKind.FurnishRoom), loop.State, loop.Holdings), 0.001f);
        }

        [Test]
        public void Motorbike_ProgressTracksSavingsAndVehicleOwnership()
        {
            var loop = new GameLoop();
            var goal = new Goal(GoalKind.BuyMotorbike, 150_000L, 0L);
            loop.State.Savings = 75_000L;
            Assert.AreEqual(0.5f, GoalBook.Progress(goal, loop.State, loop.Holdings), 0.001f);

            loop.State.History.Add("bought_car");
            Assert.AreEqual(1f, GoalBook.Progress(goal, loop.State, loop.Holdings), "holding vehicle achieves goal");
        }

        [Test]
        public void Goals_AreSavedWithTheRun()
        {
            var loop = new GameLoop();
            loop.State.Cash = 200_000L;
            loop.PutInFund(100_000L);
            loop.Goals.TakeNewlyDone(loop.State, loop.Holdings);

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.AreEqual(3, restored.Goals.Goals.Count);
            Assert.IsTrue(Find(restored, GoalKind.EmergencyFund).Done);
            Assert.AreEqual(Find(loop, GoalKind.PayDownCard).Target, Find(restored, GoalKind.PayDownCard).Target);
        }

        [Test]
        public void Report_HasFiveConcepts()
        {
            var loop = new GameLoop();
            Assert.AreEqual(5, ReportCard.Build(loop.State, loop.Holdings, loop.Bills).Count);
        }

        [Test]
        public void Report_FundFollowsTheSalary()
        {
            var loop = new GameLoop();
            Assert.AreEqual('D', Grade(loop, "Fondo de emergencia"));

            loop.State.EmergencyFund = loop.State.SalaryNet / 2L + 1L;
            Assert.AreEqual('A', Grade(loop, "Fondo de emergencia"));
        }

        [Test]
        public void Report_LenderCapsTheDebtsAtC()
        {
            var loop = new GameLoop();
            loop.State.CreditCard.Balance = 0L;
            Assert.AreEqual('A', Grade(loop, "Deudas"));

            loop.State.InformalLoans.Add(new InformalLoan("Prestamista", 1_000L, 20m));
            Assert.AreEqual('C', Grade(loop, "Deudas"));
        }

        [Test]
        public void Report_LateBillsLowerTheBudget()
        {
            var loop = new GameLoop();
            loop.State.Cash = loop.State.SalaryNet;
            Assert.AreEqual('A', Grade(loop, "Presupuesto"));

            loop.Bills.Restore(BillKind.Water, 5_000L, 1);
            Assert.AreEqual('C', Grade(loop, "Presupuesto"));

            loop.Bills.Restore(BillKind.Electricity, 5_000L, BillBook.MonthsUntilCut);
            Assert.AreEqual('D', Grade(loop, "Presupuesto"));
        }

        [TestCase("Juntar el fondo de emergencia")]
        [TestCase("Presupuesto")]
        [TestCase("¡Meta cumplida!")]
        public void TheTexts_AreTranslated(string spanish)
        {
            Assert.AreNotEqual(spanish, Loc.Translate(spanish, GameLanguage.English));
        }
    }
}

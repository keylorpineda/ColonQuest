using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class GameLoopTests
    {
        private GameLoop loop;

        [SetUp]
        public void SetUp()
        {
            loop = new GameLoop();
        }

        /// <summary>An expense of 30 000 colones in cash or on the card, so the tests do not depend on the catalog.</summary>
        private static GameEvent Expense()
        {
            return new GameEvent
            {
                EventId = "evt_test",
                Type = EventType.Expense,
                Title = "Prueba",
                Narrative = "Texto",
                TeachingConcept = TeachingConcept.Budget,
                Choices = new List<Choice>
                {
                    new Choice("cash", "Pagar en efectivo", new List<Effect> { new Effect(EffectKind.Expense, 30_000L, MoneySource.Cash) }),
                    new Choice("card", "Pasarlo a la tarjeta", new List<Effect> { new Effect(EffectKind.Expense, 30_000L, MoneySource.CreditCard) })
                }
            };
        }

        [Test]
        public void NewPlayer_StartsWithTheContractExampleSituation()
        {
            var state = GameRules.NewPlayer();

            Assert.AreEqual(1, state.Month);
            Assert.AreEqual(FinanceEngine.CalculateNetSalary(GameRules.MinimumWageGross), state.SalaryNet);
            Assert.AreEqual(180_000L, state.CreditCard.Balance);
            Assert.AreEqual(25_000L, GameRules.MonthlyInstallments(state));
            Assert.AreEqual(180_000L, GameRules.DebtBalance(state));
        }

        [Test]
        public void Money_UsesColonsWithSpacesForThousands()
        {
            Assert.AreEqual("₡85 000", Money.Format(85_000L));
            Assert.AreEqual("₡0", Money.Format(0L));
            Assert.AreEqual("₡999", Money.Format(999L));
            Assert.AreEqual("₡1 250 000", Money.Format(1_250_000L));
            Assert.AreEqual("-₡5 000", Money.Format(-5_000L));
            Assert.AreEqual("+₡5 000", Money.FormatSigned(5_000L));
        }

        [Test]
        public void Choose_AppliesTheEffectThroughTheEngineAndReportsTheChange()
        {
            long cash = loop.State.Cash;

            var outcome = loop.Choose(Expense(), "cash", StationType.Garage);

            Assert.AreEqual(cash - 30_000L, loop.State.Cash);
            Assert.AreEqual(-30_000L, outcome.CashDelta);
            Assert.AreEqual(0L, outcome.CardDelta);
            Assert.AreEqual("Pagar en efectivo", outcome.ChoiceLabel);
        }

        [Test]
        public void Choose_OnTheCardRaisesTheCardBalanceAndKeepsTheCash()
        {
            long cash = loop.State.Cash;

            var outcome = loop.Choose(Expense(), "card", StationType.Garage);

            Assert.AreEqual(cash, loop.State.Cash);
            Assert.AreEqual(30_000L, outcome.CardDelta);
        }

        [Test]
        public void Choose_RejectsAChoiceOutOfTheContractAndLeavesTheStateUntouched()
        {
            var gameEvent = Expense();
            gameEvent.Choices[0].Effects[0] = new Effect(EffectKind.Expense, 900_000L, MoneySource.Cash);
            long cash = loop.State.Cash;

            Assert.Throws<InvalidGameEventEffectException>(() => loop.Choose(gameEvent, "cash", StationType.Garage));

            Assert.AreEqual(cash, loop.State.Cash);
            Assert.AreEqual(0, loop.TasksDone, "a rejected choice is not a task");
        }

        [Test]
        public void Choose_CountsOneTaskPerPlaceAndMonth()
        {
            loop.Choose(Expense(), "cash", StationType.Garage);

            Assert.AreEqual(1, loop.TasksDone);
            Assert.IsFalse(loop.CanDoTask(StationType.Garage), "the same place once a month");
            Assert.IsTrue(loop.CanDoTask(StationType.Store));
        }

        [Test]
        public void Tasks_StopAtTheLimitOfTheMonth()
        {
            loop.Choose(Expense(), "cash", StationType.Garage);
            loop.Choose(Expense(), "cash", StationType.Store);
            loop.Choose(Expense(), "cash", StationType.Kiosk);

            Assert.IsTrue(loop.AllTasksDone);
            Assert.IsFalse(loop.CanDoTask(StationType.Pyramid));
        }

        [Test]
        public void TheBankAndTheHomeAreNotTasks()
        {
            Assert.IsFalse(loop.CanDoTask(StationType.Bank));
            Assert.IsFalse(loop.CanDoTask(StationType.Home));
            Assert.IsTrue(loop.CanDoTask(StationType.Coop));
            Assert.IsTrue(loop.CanDoTask(StationType.Soda));
        }

        [Test]
        public void CloseMonth_PaysTheSalaryAndTheObligationsAndAdvancesTheMonth()
        {
            long cash = loop.State.Cash;

            var report = loop.CloseMonth();

            Assert.AreEqual(GameRules.NewPlayer().SalaryNet, report.SalaryCredited);
            Assert.AreEqual(25_000L, report.InstallmentsCharged);
            Assert.AreEqual(2, loop.State.Month);
            Assert.AreEqual(1, loop.MonthsPlayed);
            Assert.Less(loop.State.Cash, cash + 450_000L, "living expenses and the card minimum come out of the cash");
        }

        [Test]
        public void CloseMonth_ClearsTheTasksOfTheMonth()
        {
            loop.Choose(Expense(), "cash", StationType.Garage);

            loop.CloseMonth();

            Assert.AreEqual(0, loop.TasksDone);
            Assert.IsTrue(loop.CanDoTask(StationType.Garage));
        }

        [Test]
        public void TheRunEndsAfterTheMonthsToPlay()
        {
            for (int i = 0; i < GameRules.MonthsToPlay; i++)
            {
                Assert.IsFalse(loop.IsFinished);
                loop.CloseMonth();
            }

            Assert.IsTrue(loop.IsFinished);
            Assert.IsFalse(loop.CanDoTask(StationType.Garage), "nothing to do once the run is over");
        }

        [Test]
        public void Money_StaysInWholeColonesAndTheCashNeverGoesNegativeWithoutDebt()
        {
            for (int i = 0; i < GameRules.MonthsToPlay; i++)
            {
                loop.CloseMonth();
            }

            Assert.GreaterOrEqual(loop.State.Cash, 0L);
            Assert.GreaterOrEqual(loop.State.CreditCard.Balance, 0L);
        }

        [Test]
        public void NewGame_ResetsEverything()
        {
            loop.Choose(Expense(), "cash", StationType.Garage);
            loop.CloseMonth();

            loop.NewGame();

            Assert.AreEqual(1, loop.State.Month);
            Assert.AreEqual(0, loop.MonthsPlayed);
            Assert.AreEqual(0, loop.TasksDone);
            Assert.AreEqual(120_000L, loop.State.Cash);
        }

        [Test]
        public void NetWorth_SubtractsTheDebtAndTheInstallmentsLeft()
        {
            var state = GameRules.NewPlayer();

            // 120 000 + 50 000 - 180 000 card - 25 000 x 33 installments
            Assert.AreEqual(120_000L + 50_000L - 180_000L - 825_000L, GameRules.NetWorth(state));
        }
    }
}

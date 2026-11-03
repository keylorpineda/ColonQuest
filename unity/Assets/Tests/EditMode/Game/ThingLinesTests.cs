using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class ThingLinesTests
    {
        [Test]
        public void Lines_RotateWithEachUse()
        {
            var lines = new[] { "a", "b", "c" };

            Assert.AreEqual("a", ThingLines.Pick(lines, 0, null));
            Assert.AreEqual("b", ThingLines.Pick(lines, 1, null));
            Assert.AreEqual("c", ThingLines.Pick(lines, 2, null));
            Assert.AreEqual("a", ThingLines.Pick(lines, 3, null), "back to the first");
        }

        [Test]
        public void Tokens_TakeTheAmountsFromTheState()
        {
            var state = new PlayerState { Cash = 120000L, Savings = 30000L, EmergencyFund = 20000L, SalaryNet = 450000L };
            state.CreditCard.Balance = 180000L;

            string line = ThingLines.Pick(new[] { "{cash}|{savings}|{card}|{salary}" }, 0, state);

            Assert.AreEqual(
                Money.Format(120000L) + "|" + Money.Format(50000L) + "|" + Money.Format(180000L) + "|" + Money.Format(450000L),
                line);
        }

        [Test]
        public void NoLines_IsAnEmptyText()
        {
            Assert.AreEqual(string.Empty, ThingLines.Pick(null, 0, null));
            Assert.AreEqual(string.Empty, ThingLines.Pick(new string[0], 4, null));
        }

        [Test]
        public void ThingsAndCashMachines_AreNotTasks()
        {
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.Thing));
            Assert.IsFalse(GameLoop.IsTaskStation(StationType.Atm));
            Assert.IsTrue(GameLoop.IsTaskStation(StationType.Garage));
        }
    }
}

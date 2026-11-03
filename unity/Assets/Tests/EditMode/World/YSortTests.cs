using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class YSortTests
    {
        [Test]
        public void OrderFor_ThingsLowerOnTheScreenDrawInFront()
        {
            Assert.Greater(YSort.OrderFor(-3f), YSort.OrderFor(2f));
        }

        [Test]
        public void OrderFor_SameHeightGivesSameOrder()
        {
            Assert.AreEqual(YSort.OrderFor(1.5f), YSort.OrderFor(1.5f));
        }

        [Test]
        public void OrderFor_KeepsFinePrecision()
        {
            // 0.1 units apart must still sort differently, or characters would flicker at the same height.
            Assert.AreNotEqual(YSort.OrderFor(4.0f), YSort.OrderFor(4.1f));
        }
    }
}

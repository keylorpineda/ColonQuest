using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class PostcardKeysTests
    {
        [Test]
        public void For_ChoosesTheCardByTheDebtAtTheEnd()
        {
            Assert.AreEqual(PostcardKeys.DebtFree, PostcardKeys.For(0L));
            Assert.AreEqual(PostcardKeys.DebtFree, PostcardKeys.For(-5L));
            Assert.AreEqual(PostcardKeys.SomeDebt, PostcardKeys.For(120_000L));
            Assert.AreEqual(PostcardKeys.SomeDebt, PostcardKeys.For(PostcardKeys.HeavyDebtColones - 1L));
            Assert.AreEqual(PostcardKeys.HeavyDebt, PostcardKeys.For(PostcardKeys.HeavyDebtColones));
        }

        [Test]
        public void Keys_AreTheOnesTheBackendPregenerates()
        {
            CollectionAssert.AreEquivalent(new[] { "card_year_debt_free", "card_year_some_debt", "card_year_heavy_debt" }, PostcardKeys.All);
        }
    }
}

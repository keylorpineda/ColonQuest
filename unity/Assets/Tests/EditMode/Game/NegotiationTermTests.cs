using ColonQuest.Api;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class NegotiationTermTests
    {
        private static Negotiation Phone() => new Negotiation(SellerOffer.Find("celular_gama_alta"));

        [Test]
        public void TheDealIsSignedAtTheTermTheSellerLastQuoted()
        {
            var negotiation = Phone();

            negotiation.Record("Algo más largo", new NegotiationReply { NpcReply = "A 36 meses", TermMonths = 36 }, false);

            var signed = negotiation.ToEvent().Choices[0].Effects.Find(e => e.Kind == EffectKind.NewInstallment);
            Assert.AreEqual(36, signed.Months);
            Assert.AreEqual(negotiation.Offer.MonthlyCharge, signed.Amount);
        }

        [Test]
        public void AnotherTerm_KeepsTheCashPriceAndMovesTheTotal()
        {
            var negotiation = Phone();
            long total = negotiation.Offer.TotalCost;

            negotiation.Record("Algo más largo", new NegotiationReply { NpcReply = "A 36 meses", TermMonths = 36 }, false);

            Assert.AreEqual(350_000L, negotiation.Offer.CashPrice);
            Assert.Greater(negotiation.Offer.TotalCost, total);
            Assert.AreEqual("celular_gama_alta", negotiation.Offer.OfferId, "still the offer the backend knows");
            Assert.IsFalse(negotiation.IsLocalOnly);
        }

        [Test]
        public void AReplyWithoutATerm_LeavesItAsItIs()
        {
            var negotiation = Phone();

            negotiation.Record("Hola", new NegotiationReply { NpcReply = "Hola" }, true);

            Assert.AreEqual(24, negotiation.Offer.TermMonths);
        }

        [Test]
        public void TheClientAndTheBackendPriceAnotherTermAlike()
        {
            // The figures of test_the_tool_and_the_sheet_agree_on_a_term, computed by the server's sheet.
            var longer = SellerOffer.Find("celular_gama_alta").AtTerm(36);

            Assert.AreEqual(14_900L, longer.MonthlyPayment);
        }
    }
}

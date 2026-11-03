using ColonQuest.Api;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class CarNegotiationTests
    {
        /// <summary>No interest, so every figure can be checked by hand: 4 000 000 financed over 40 months.</summary>
        private static SellerOffer CarOffer()
        {
            return new SellerOffer("car_prueba", "sedán de prueba", "Don Chepe", 4_800_000L, 40, 0m, 10_000L, 50_000L, 800_000L);
        }

        [Test]
        public void DownPayment_ReducesTheFinancedPartButCountsInTheTotal()
        {
            var offer = CarOffer();

            Assert.AreEqual(4_000_000L, offer.Financed);
            Assert.AreEqual(100_000L, offer.MonthlyPayment);
            Assert.AreEqual(110_000L, offer.MonthlyCharge);
            Assert.AreEqual(5_250_000L, offer.TotalCost, "down payment, installments with insurance and the commission");
            Assert.AreEqual(450_000L, offer.ExtraOverCash);
        }

        [Test]
        public void TheBackendCatalogStillFinancesTheWholePrice()
        {
            var phone = SellerOffer.Find("celular_gama_alta");

            Assert.AreEqual(0L, phone.DownPayment);
            Assert.AreEqual(phone.CashPrice, phone.Financed);
            Assert.AreEqual(phone.MonthlyCharge * phone.TermMonths + phone.Commission, phone.TotalCost);
        }

        [Test]
        public void Greeting_MentionsTheDownPaymentOnlyForOffersThatHaveOne()
        {
            StringAssert.Contains("prima de ₡800 000", LocalSeller.Greeting(CarOffer()));
            StringAssert.DoesNotContain("prima", LocalSeller.Greeting(SellerOffer.Find("moto_125")));
        }

        [Test]
        public void Reply_AnswersTheDownPaymentQuestionWithTheTrueAmount()
        {
            var reply = LocalSeller.Reply(CarOffer(), "¿Cuánto es la prima?");

            StringAssert.Contains("₡800 000", reply.NpcReply);
        }

        [Test]
        public void Reply_TheTotalCostQuestionIncludesTheDownPayment()
        {
            var offer = CarOffer();

            var reply = LocalSeller.Reply(offer, "¿Cuánto termino pagando en total?");

            StringAssert.Contains("₡5 250 000", reply.NpcReply);
            CollectionAssert.Contains(reply.KeyQuestionsAsked, KeyQuestion.TotalCost);
        }

        [Test]
        public void Reply_WithoutAKeyQuestionStillQuotesTheMonthlyAndTheDownPayment()
        {
            var reply = LocalSeller.Reply(CarOffer(), "Hola, me gusta");

            StringAssert.Contains("₡100 000", reply.NpcReply, "the seller quotes the payment and keeps the insurance quiet");
            StringAssert.Contains("prima", reply.NpcReply);
        }

        [Test]
        public void ANegotiationOfACarIsLocalOnlyAndOneOfTheStoreIsNot()
        {
            var car = new Negotiation(CarOffer());
            var store = new Negotiation(SellerOffer.ForMonth(0));

            Assert.IsTrue(car.IsLocalOnly, "the backend does not know this offer, so no turn is sent to it");
            Assert.IsFalse(store.IsLocalOnly);
            Assert.AreEqual(Negotiation.StoreVenue, store.Venue);
        }

        [Test]
        public void ACarNegotiationFeedsTheSameKeyQuestionMetric()
        {
            var negotiation = new Negotiation(CarOffer());

            negotiation.Record("¿Hay cargos escondidos?", LocalSeller.Reply(negotiation.Offer, "¿Hay cargos escondidos?"), true);

            CollectionAssert.Contains(negotiation.KeyQuestionsAsked, KeyQuestion.HiddenFees);
            Assert.AreEqual(1, negotiation.Report(false).QuestionsAsked);
            Assert.AreEqual(5_250_000L, negotiation.Report(false).TotalCost);
        }
    }
}

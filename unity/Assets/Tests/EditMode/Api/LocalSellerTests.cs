using NUnit.Framework;

namespace ColonQuest.Api.Tests
{
    [TestFixture]
    public sealed class LocalSellerTests
    {
        private static SellerOffer Celular => SellerOffer.Find("celular_gama_alta");

        [Test]
        public void Offer_MatchesTheBackendSheetFigures()
        {
            var offer = Celular;

            Assert.AreEqual(350_000L, offer.CashPrice);
            Assert.AreEqual(24, offer.TermMonths);
            Assert.AreEqual(0L, offer.MonthlyPayment % 100L, "installments are rounded up to the next 100");
            Assert.AreEqual(offer.MonthlyPayment + 3_500L, offer.MonthlyCharge);
            Assert.AreEqual(offer.MonthlyCharge * 24 + 15_000L, offer.TotalCost);
            Assert.Greater(offer.TotalCost, offer.CashPrice);
        }

        [Test]
        public void Offer_FrenchPaymentCoversThePrincipalAtTheRate()
        {
            var offer = Celular;

            // Paying the installment for the whole term must pay back at least the price plus interest.
            Assert.GreaterOrEqual(offer.MonthlyPayment * offer.TermMonths, offer.CashPrice);
            Assert.Less(offer.MonthlyPayment, offer.CashPrice / 12L);
        }

        [Test]
        public void EveryOffer_FitsTheInstallmentRangesOfTheEngine()
        {
            foreach (var offer in SellerOffer.Catalog)
            {
                Assert.That(offer.MonthlyCharge, Is.InRange(5_000L, 80_000L), offer.OfferId);
                Assert.That(offer.TermMonths, Is.InRange(3, 48), offer.OfferId);
            }
        }

        [Test]
        public void ForMonth_RotatesThroughTheCatalog()
        {
            Assert.AreEqual(SellerOffer.Catalog[0], SellerOffer.ForMonth(0));
            Assert.AreEqual(SellerOffer.Catalog[1], SellerOffer.ForMonth(1));
            Assert.AreEqual(SellerOffer.Catalog[0], SellerOffer.ForMonth(SellerOffer.Catalog.Count));
        }

        [Test]
        public void FormatColones_UsesSpacesForThousands()
        {
            Assert.AreEqual("₡31 500", SellerOffer.FormatColones(31_500L));
            Assert.AreEqual("₡1 200 000", SellerOffer.FormatColones(1_200_000L));
        }

        [Test]
        public void Classify_FindsKeyQuestionsIgnoringAccentsAndCase()
        {
            var asked = LocalSeller.Classify("¿Cuánto pago en TOTAL? ¿Y el interés? ¿Tiene comisión?");

            Assert.Contains(KeyQuestion.TotalCost, asked);
            Assert.Contains(KeyQuestion.InterestRate, asked);
            Assert.Contains(KeyQuestion.HiddenFees, asked);
            Assert.IsFalse(asked.Contains(KeyQuestion.Term));
        }

        [Test]
        public void Classify_FindsTermCashPriceAndInsurance()
        {
            var asked = LocalSeller.Classify("cuantos meses es el plazo, y de contado cuanto sale, lleva seguro?");

            Assert.Contains(KeyQuestion.Term, asked);
            Assert.Contains(KeyQuestion.CashPrice, asked);
            Assert.Contains(KeyQuestion.Insurance, asked);
        }

        [Test]
        public void Reply_WithoutAKeyQuestion_OnlyPitchesTheMonthlyPayment()
        {
            var reply = LocalSeller.Reply(Celular, "hola, que tal");

            StringAssert.Contains(Celular.Describe(SellerFact.MonthlyPayment), reply.NpcReply);
            CollectionAssert.AreEqual(new[] { SellerFact.MonthlyPayment }, reply.RevealedFacts);
            Assert.IsEmpty(reply.KeyQuestionsAsked);
            StringAssert.DoesNotContain(Celular.Describe(SellerFact.TotalCost), reply.NpcReply);
        }

        [Test]
        public void Reply_RevealsTheTotalCostOnlyWhenAsked()
        {
            var reply = LocalSeller.Reply(Celular, "Cuanto pago en total?");

            StringAssert.Contains(Celular.Describe(SellerFact.TotalCost), reply.NpcReply);
            Assert.Contains(SellerFact.TotalCost, reply.RevealedFacts);
            Assert.Contains(KeyQuestion.TotalCost, reply.KeyQuestionsAsked);
            Assert.AreEqual("Cuanto pago en total?", reply.Transcript);
        }

        [Test]
        public void Reply_HiddenFeesRevealTheCommissionAndTheInsurance()
        {
            var reply = LocalSeller.Reply(Celular, "hay algun cargo escondido?");

            Assert.Contains(SellerFact.Commission, reply.RevealedFacts);
            Assert.Contains(SellerFact.InsuranceFee, reply.RevealedFacts);
            StringAssert.Contains("₡15 000", reply.NpcReply);
        }

        [Test]
        public void StatusOf_ReadsAcceptingAndLeaving()
        {
            Assert.AreEqual(DealStatus.Accepted, LocalSeller.StatusOf("Trato hecho, me lo llevo"));
            Assert.AreEqual(DealStatus.WalkedAway, LocalSeller.StatusOf("No gracias, mejor no"));
            Assert.AreEqual(DealStatus.Open, LocalSeller.StatusOf("cuanto cuesta"));
        }
    }
}

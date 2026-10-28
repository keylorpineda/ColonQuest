using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class NegotiationTests
    {
        private SellerOffer offer;
        private Negotiation negotiation;

        [SetUp]
        public void SetUp()
        {
            offer = SellerOffer.Find("pantalla_65");
            negotiation = new Negotiation(offer);
        }

        private void Say(string text)
        {
            negotiation.Record(text, LocalSeller.Reply(offer, text), true);
        }

        [Test]
        public void Record_AccumulatesQuestionsAndFactsWithoutRepeats()
        {
            Say("cuanto es el interes?");
            Say("y el interes otra vez");
            Say("cuanto pago en total?");

            Assert.AreEqual(3, negotiation.Turns.Count);
            Assert.AreEqual(2, negotiation.KeyQuestionsAsked.Count);
            CollectionAssert.AreEquivalent(new[] { SellerFact.InterestRate, SellerFact.TotalCost }, negotiation.RevealedFacts);
        }

        [Test]
        public void Record_AcceptedStatusClosesTheConversation()
        {
            Say("hmm");
            Assert.IsFalse(negotiation.IsClosed);

            Say("trato hecho");

            Assert.IsTrue(negotiation.IsClosed);
            Assert.AreEqual(DealStatus.Accepted, negotiation.Status);
        }

        [Test]
        public void ToEvent_BuyingChargesTheCommissionAndAddsTheInstallmentWithInsurance()
        {
            var state = GameRules.NewPlayer();
            long cashBefore = state.Cash;
            long installmentsBefore = GameRules.MonthlyInstallments(state);

            FinanceEngine.ApplyChoice(state, negotiation.ToEvent(), Negotiation.BuyChoice);

            Assert.AreEqual(cashBefore - offer.Commission, state.Cash);
            Assert.AreEqual(installmentsBefore + offer.MonthlyCharge, GameRules.MonthlyInstallments(state));
        }

        [Test]
        public void ToEvent_SkippingChangesNothing()
        {
            var state = GameRules.NewPlayer();
            long cashBefore = state.Cash;
            long installmentsBefore = GameRules.MonthlyInstallments(state);

            FinanceEngine.ApplyChoice(state, negotiation.ToEvent(), Negotiation.SkipChoice);

            Assert.AreEqual(cashBefore, state.Cash);
            Assert.AreEqual(installmentsBefore, GameRules.MonthlyInstallments(state));
        }

        [Test]
        public void ToEvent_EveryOfferPassesTheEngineValidation()
        {
            foreach (var each in SellerOffer.Catalog)
            {
                var state = GameRules.NewPlayer();
                var deal = new Negotiation(each).ToEvent();

                Assert.DoesNotThrow(() => FinanceEngine.ApplyChoice(state, deal, Negotiation.BuyChoice), each.OfferId);
                Assert.GreaterOrEqual(state.Cash, 0L, each.OfferId);
            }
        }

        [Test]
        public void Report_TellsTheExtraCostAndHowManyQuestionsWereAsked()
        {
            Say("cuanto pago en total?");
            Say("tiene comision?");
            Say("me lo llevo");

            var report = negotiation.Report(true);

            Assert.IsTrue(report.Bought);
            Assert.AreEqual(offer.CashPrice, report.CashPrice);
            Assert.AreEqual(offer.TotalCost - offer.CashPrice, report.ExtraOverCash);
            Assert.AreEqual(2, report.QuestionsAsked);
            Assert.AreEqual(6, report.QuestionsTotal);
            Assert.IsTrue(report.AskedAboutTotalCost);
            Assert.IsTrue(report.AskedAboutHiddenFees);
        }

        [Test]
        public void ToEvent_HasTwoChoicesAsTheContractRequires()
        {
            var deal = negotiation.ToEvent();

            Assert.AreEqual(EventType.Offer, deal.Type);
            Assert.AreEqual(2, deal.Choices.Count);
            Assert.AreEqual(TeachingConcept.InstallmentsTotalCost, deal.TeachingConcept);
        }
    }
}

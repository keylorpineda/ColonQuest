using System.IO;
using ColonQuest.Finance;
using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.Game.Tests
{
    public sealed class EndingArtTests
    {
        private static PlayerState Clean()
        {
            var state = new PlayerState();
            state.CreditCard.Balance = 0L;
            return state;
        }

        [Test]
        public void Choose_ByWhatHappenedAndHowItEnded()
        {
            Assert.AreEqual("end_calm_ok", EndingArt.KeyFor(Clean(), 100L, 0));
            Assert.AreEqual("end_calm_hard", EndingArt.KeyFor(Clean(), -1L, 0));
            Assert.AreEqual("end_installments_ok", EndingArt.KeyFor(Clean(), 100L, 1));

            var card = Clean();
            card.CreditCard.Balance = card.CreditCard.Limit / 2L;
            Assert.AreEqual("end_card_hard", EndingArt.KeyFor(card, -1L, 0));

            var lender = Clean();
            lender.InformalLoans.Add(new InformalLoan("Loan shark", 100_000L, 20m));
            Assert.AreEqual("end_lender_hard", EndingArt.KeyFor(lender, -1L, 1), "the lender is what this run is remembered for");

            var paid = Clean();
            paid.RecordHistory(DebtTalk.OfferEvent(TalkPartner.Lender).EventId + ":" + DebtTalk.TakeOffer + "_300000");
            Assert.AreEqual("end_lender_ok", EndingArt.KeyFor(paid, 100L, 0), "any amount counts, even once it is paid");

            var pyramid = Clean();
            pyramid.RecordHistory(DebtTalk.OfferEvent(TalkPartner.Pyramid).EventId + ":" + DebtTalk.TakeOffer);
            Assert.AreEqual("end_pyramid_hard", EndingArt.KeyFor(pyramid, -1L, 0));
        }

        [Test]
        public void EveryKey_IsOneTheServerKnows()
        {
            string server = Path.Combine(Application.dataPath, "..", "..", "server", "app", "images", "keys.py");
            if (!File.Exists(server))
            {
                Assert.Ignore("this checkout has no server");
            }

            string keys = File.ReadAllText(server);
            foreach (string trap in new[] { "calm", "installments", "card", "lender", "pyramid" })
            {
                StringAssert.Contains("\"end_" + trap + "_ok\"", keys);
                StringAssert.Contains("\"end_" + trap + "_hard\"", keys);
            }
        }
    }
}

using System.Linq;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class DebtTalkTests
    {
        [TestCase("¿Cuánto es el interés?", TalkFact.Rate)]
        [TestCase("cuanto pago en total al final", TalkFact.TotalCost)]
        [TestCase("No tengo toda la plata, ¿hacemos un arreglo?", TalkFact.Arrangement)]
        [TestCase("¿Y qué pasa si no pago?", TalkFact.WhatIfNotPaid)]
        [TestCase("¿De dónde sale la plata que pagan?", TalkFact.WhereMoneyComes)]
        [TestCase("¿Me da un contrato por escrito?", TalkFact.Guarantee)]
        [TestCase("¿Eso es legal? ¿Están en la SUGEF?", TalkFact.Legal)]
        public void Classify_UnderstandsTheKeyQuestionsWithOrWithoutAccents(string text, TalkFact expected)
        {
            Assert.AreEqual(expected, DebtTalk.Classify(text));
        }

        [Test]
        public void Classify_SmallTalkIsNoKeyQuestion()
        {
            Assert.IsNull(DebtTalk.Classify("Hola, buenas tardes"));
        }

        [Test]
        public void Collectors_OfferTheArrangementOnlyToTheOneWhoAsks()
        {
            var talk = new DebtTalk(TalkPartner.Collectors, 100_000L);
            talk.Say("Hola");
            Assert.IsFalse(talk.ArrangementOffered);

            string reply = talk.Say("¿Podemos negociar?");
            Assert.IsTrue(talk.ArrangementOffered);
            Assert.AreEqual(50_000L, talk.ArrangementToday);
            Assert.AreEqual(55_000L, talk.ArrangementLater, "the half left plus the surcharge");
            StringAssert.Contains(Money.Format(50_000L), reply);
        }

        [Test]
        public void Lender_SaysTheRealTotalWhenAsked()
        {
            Assert.AreEqual(172_800L, DebtTalk.LenderTotal(), "100 000 at 20% a month for 3 months");
            var talk = new DebtTalk(TalkPartner.Lender);
            StringAssert.Contains(Money.Format(172_800L), talk.Say("¿Cuánto tengo que devolver en total?"));
        }

        [Test]
        public void Pyramid_AdmitsWhereTheMoneyComesFromWhenAsked()
        {
            var talk = new DebtTalk(TalkPartner.Pyramid);
            StringAssert.Contains("nuevos", talk.Say("¿De dónde sale la plata?"));
        }

        [Test]
        public void HistoryTags_KeepEachQuestionOnce()
        {
            var talk = new DebtTalk(TalkPartner.Lender);
            talk.Say("¿Qué tasa cobran?");
            talk.Say("¿y el interés?");
            talk.Say("¿es legal?");
            CollectionAssert.AreEqual(new[] { "asked_lender_rate", "asked_lender_legal" }, talk.HistoryTags().ToArray());
            Assert.AreEqual(3, talk.Turns.Count);
        }
    }
}

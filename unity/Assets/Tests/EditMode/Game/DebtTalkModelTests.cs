using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class DebtTalkModelTests
    {
        [Test]
        public void TheCollectorsSendTheTrueFiguresOfTheDemand()
        {
            var talk = new DebtTalk(TalkPartner.Collectors, 80_000L);

            var request = talk.ToRequest("Can we work something out?", "en");

            Assert.AreEqual("collectors", request.partner);
            Assert.AreEqual("en", request.language);
            CollectionAssert.AreEquivalent(new[] { 80_000L, 40_000L, 44_000L }, request.amounts);
            CollectionAssert.Contains(request.percents, MarketAssumptions.CollectionNegotiateSurchargePct);
            Assert.AreEqual(4, request.sheet.Count);
        }

        [Test]
        public void TheLenderSendsEachLoanAndWhatItCosts()
        {
            var request = new DebtTalk(TalkPartner.Lender).ToRequest("How much?", "en");

            CollectionAssert.AreEquivalent(new[] { 100_000L, 172_800L, 200_000L, 345_600L, 300_000L, 518_400L }, request.amounts);
            CollectionAssert.AreEqual(new[] { 20, 15, 240, 36 }, request.percents);
        }

        [Test]
        public void ThePyramidAdmitsWhereTheMoneyComesFromOnItsSheet()
        {
            var request = new DebtTalk(TalkPartner.Pyramid).ToRequest("Where does the money come from?", "en");

            CollectionAssert.AreEquivalent(new[] { 100_000L, 400_000L }, request.amounts);
            Assert.IsTrue(request.sheet.Exists(line => line.fact == "where_money_comes"));
        }

        [Test]
        public void EveryFactOnASheetTravelsAsTheBackendNamesIt()
        {
            var known = new HashSet<string> { "total_cost", "rate", "arrangement", "what_if_not_paid", "where_money_comes", "guarantee", "legal" };
            foreach (TalkPartner partner in System.Enum.GetValues(typeof(TalkPartner)))
            {
                foreach (var line in new DebtTalk(partner, 50_000L).ToRequest("hola", "es").sheet)
                {
                    Assert.IsTrue(known.Contains(line.fact), line.fact);
                    Assert.IsNotEmpty(line.text);
                }
            }
        }

        [Test]
        public void ARequestCarriesTheConversationSoFar()
        {
            var talk = new DebtTalk(TalkPartner.Lender);
            talk.Record("Hola", "¿Cuánto ocupa?", new TalkFact[0]);

            var request = talk.ToRequest("Mucho", "es");

            Assert.AreEqual(1, request.history.Count);
            Assert.AreEqual("¿Cuánto ocupa?", request.history[0].reply);
        }

        [Test]
        public void WhatTheModelSaysWasAsked_OpensTheArrangementEvenInWordsTheRulesDoNotKnow()
        {
            var talk = new DebtTalk(TalkPartner.Collectors, 80_000L);
            Assert.IsFalse(talk.ArrangementOffered);

            talk.Record("¿Y si nos arreglamos usted y yo, mae?", "Bueno, mae: 40 000 hoy.", new[] { TalkFact.Arrangement });

            Assert.IsTrue(talk.ArrangementOffered);
            Assert.AreEqual("Bueno, mae: 40 000 hoy.", talk.Turns[0].Reply);
        }

        [TestCase("Can we work something out?", TalkFact.Arrangement)]
        [TestCase("Can I pay half today?", TalkFact.Arrangement)]
        [TestCase("What happens if I can't pay?", TalkFact.WhatIfNotPaid)]
        [TestCase("What's the interest rate?", TalkFact.Rate)]
        [TestCase("How much do I pay back?", TalkFact.TotalCost)]
        [TestCase("Is this legal?", TalkFact.Legal)]
        [TestCase("Where does the money come from?", TalkFact.WhereMoneyComes)]
        [TestCase("Do I get a contract?", TalkFact.Guarantee)]
        public void TheLocalRules_UnderstandEnglish(string text, TalkFact fact)
        {
            Assert.AreEqual(fact, DebtTalk.Classify(text));
        }

        [TestCase("¿Podemos hacer un arreglo?", TalkFact.Arrangement)]
        [TestCase("¿Cuál es la tasa de interés?", TalkFact.Rate)]
        [TestCase("¿De dónde sale la plata?", TalkFact.WhereMoneyComes)]
        public void TheLocalRules_StillUnderstandSpanish(string text, TalkFact fact)
        {
            Assert.AreEqual(fact, DebtTalk.Classify(text));
        }
    }
}

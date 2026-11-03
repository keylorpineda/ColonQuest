using System.Collections.Generic;
using System.Linq;
using ColonQuest.Api;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    /// <summary>Every text the game writes from data (events, offers, houses, cars, the farm, the glossary) has an English entry.</summary>
    [TestFixture]
    public sealed class TranslationCoverageTests
    {
        private GameLanguage savedLanguage;

        // The texts are read through the language saved on this machine; the keys of the table are the Spanish ones.
        [SetUp]
        public void SetUp()
        {
            savedLanguage = GameSettings.Current.Language;
            GameSettings.Current.Language = GameLanguage.Spanish;
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.Current.Language = savedLanguage;
        }

        private static void AssertKnown(string spanish, string where)
        {
            Assert.IsTrue(Loc.KnownTexts.Contains(spanish), "no English entry for (" + where + "): " + spanish);
        }

        [Test]
        public void FallbackEvents_AreTranslated()
        {
            foreach (var gameEvent in FallbackEvents.Catalog)
            {
                AssertKnown(gameEvent.Title, gameEvent.EventId);
                AssertKnown(gameEvent.Narrative, gameEvent.EventId);
                foreach (var choice in gameEvent.Choices)
                {
                    AssertKnown(choice.Label, gameEvent.EventId);
                }
            }
        }

        [Test]
        public void Market_NamesAndDescriptionsAreTranslated()
        {
            foreach (var home in MarketAssumptions.Homes)
            {
                AssertKnown(home.Name, home.Id);
                AssertKnown(home.Description, home.Id);
            }

            foreach (var car in MarketAssumptions.Cars)
            {
                AssertKnown(car.Name, car.Id);
                AssertKnown(car.Name.ToLowerInvariant(), car.Id);
            }

            foreach (var farm in MarketAssumptions.Farm)
            {
                AssertKnown(farm.Name, farm.Name);
                AssertKnown(farm.Description, farm.Name);
            }

            AssertKnown(MarketAssumptions.Disclaimer, "disclaimer");
        }

        [Test]
        public void SellerItems_AreTranslated()
        {
            foreach (var offer in SellerOffer.Catalog)
            {
                AssertKnown(offer.Item, offer.OfferId);
            }
        }

        [Test]
        public void Glossary_IsTranslated()
        {
            foreach (var entry in FinanceGlossary.Entries)
            {
                AssertKnown(entry.Title, "glossary title");
                AssertKnown(entry.Text, entry.Title);
            }
        }

        [Test]
        public void SinpeNotices_AreTranslated()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                seen.Add(PhoneMessages.SinpeNotice(i));
            }

            Assert.AreEqual(4, seen.Count);
            foreach (string notice in seen)
            {
                AssertKnown(notice, "SINPE notice");
            }
        }

        [Test]
        public void LocalSeller_AnswersInEnglishWhenTheGameAsks()
        {
            var offer = SellerOffer.Catalog[0];
            var previous = LocalSeller.Translate;
            try
            {
                LocalSeller.Translate = text => Loc.Translate(text, GameLanguage.English);
                string greeting = LocalSeller.Greeting(offer);
                StringAssert.Contains("Pura vida", greeting);
                StringAssert.DoesNotContain("Mire este", greeting);

                var reply = LocalSeller.Reply(offer, "what is the insurance?");
                CollectionAssert.Contains(reply.KeyQuestionsAsked, KeyQuestion.Insurance);
                StringAssert.Contains("the insurance is", reply.NpcReply);
            }
            finally
            {
                LocalSeller.Translate = previous;
            }
        }
    }
}

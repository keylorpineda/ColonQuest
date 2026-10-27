using System.Collections.Generic;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Api.Tests
{
    [TestFixture]
    public sealed class FallbackEventsTests
    {
        [Test]
        public void Catalog_HasTheSeventeenEventsOfTheServer()
        {
            // Resources/fallback_events.json is a copy of the server catalog; every event must survive validation.
            Assert.AreEqual(17, FallbackEvents.Catalog.Count);
        }

        [Test]
        public void Catalog_EventsHaveValidChoicesAndEffects()
        {
            foreach (var gameEvent in FallbackEvents.Catalog)
            {
                Assert.GreaterOrEqual(gameEvent.Choices.Count, 2, gameEvent.EventId);
                foreach (var choice in gameEvent.Choices)
                {
                    foreach (var effect in choice.Effects)
                    {
                        Assert.IsTrue(EffectLimits.TryValidateEffect(gameEvent.Type, effect, out string error), gameEvent.EventId + ": " + error);
                    }
                }
            }
        }

        [Test]
        public void Parse_SkipsABrokenEventAndKeepsTheRest()
        {
            string json = @"{ ""events"": [
                { ""eventId"": ""bad"", ""type"": ""nope"", ""teachingConcept"": ""budget"", ""choices"": [] },
                { ""eventId"": ""good"", ""type"": ""expense"", ""title"": ""t"", ""narrative"": ""n"", ""teachingConcept"": ""budget"",
                  ""choices"": [ { ""id"": ""a"", ""label"": ""A"", ""effects"": [] }, { ""id"": ""b"", ""label"": ""B"", ""effects"": [] } ] }
            ] }";

            var events = FallbackEvents.Parse(json);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("good", events[0].EventId);
        }

        [Test]
        public void Parse_OfGarbageGivesAnEmptyList()
        {
            Assert.AreEqual(0, FallbackEvents.Parse("not json").Count);
        }

        [Test]
        public void Next_CallsTheLenderWhenTheCardIsNearItsLimit()
        {
            var state = new PlayerState { Month = 3, SalaryNet = 450000L, CreditCard = new CreditCard(250000L, 300000L, 3.5m, 10m) };

            var gameEvent = FallbackEvents.Next(state, new HashSet<string>());

            Assert.AreEqual(TeachingConcept.InformalLending, gameEvent.TeachingConcept);
        }

        [Test]
        public void Next_OffersThePyramidWhenSavingsAreHigh()
        {
            var state = new PlayerState { Month = 5, SalaryNet = 450000L, Savings = 600000L, CreditCard = new CreditCard(0L, 300000L, 3.5m, 10m) };

            var gameEvent = FallbackEvents.Next(state, new HashSet<string>());

            Assert.AreEqual(TeachingConcept.PyramidScheme, gameEvent.TeachingConcept);
        }

        [Test]
        public void Next_SkipsEventsAlreadyPlayed()
        {
            var state = new PlayerState { Month = 1 };
            var seen = new HashSet<string>();
            var first = FallbackEvents.Next(state, seen);
            seen.Add(first.EventId);

            var second = FallbackEvents.Next(state, seen);

            Assert.AreNotEqual(first.EventId, second.EventId);
        }

        [Test]
        public void Next_StillAnswersWhenEveryEventWasPlayed()
        {
            var seen = new HashSet<string>();
            foreach (var gameEvent in FallbackEvents.Catalog)
            {
                seen.Add(gameEvent.EventId);
            }

            Assert.IsNotNull(FallbackEvents.Next(new PlayerState { Month = 2 }, seen));
        }
    }
}

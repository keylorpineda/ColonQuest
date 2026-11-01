using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class DemoReadoutTests
    {
        [TestCase("muse-spark-1.3", "Muse Spark")]
        [TestCase("muse-spark-1.3-contributor", "Muse Spark")]
        [TestCase("meta-models/muse-spark-open-1026", "Muse Spark Open")]
        [TestCase("muse-wonder-1.0", "Muse Wonder")]
        [TestCase("muse-voice-transcribe-1.0", "Muse Voice Transcribe")]
        [TestCase("muse-image-1.0", "Muse Image")]
        public void ModelName_IsTheNameTheCompetitionKnows(string id, string name)
        {
            Assert.AreEqual(name, DemoReadout.ModelName(id));
        }

        [Test]
        public void ProviderName_TellsBasetenFromMeta()
        {
            Assert.AreEqual("Baseten", DemoReadout.ProviderName("open_weights"));
            Assert.AreEqual(Loc.T("API de Meta"), DemoReadout.ProviderName("meta_api"));
        }

        [TestCase("agent", true)]
        [TestCase("model", true)]
        [TestCase("cache", false)]
        [TestCase("fallback", false)]
        [TestCase("local", false)]
        [TestCase("", false)]
        public void WrittenByAModel_IsOnlyTrueWhenAModelReallyAnswered(string origin, bool written)
        {
            Assert.AreEqual(written, DemoReadout.WrittenByAModel(origin));
        }

        [Test]
        public void PlannedByTheModel_NeedsTheCallOfThePlannerAndAStoredPlan()
        {
            var trace = new SessionTraceDto();
            Assert.IsFalse(DemoReadout.PlannedByTheModel(trace));
            Assert.IsFalse(DemoReadout.PlannerAnswered(trace));

            trace.seasonPlan.Add(new PlanMonthDto { month = 1, theme = "base" });
            trace.calls.Add(new ModelCallDto { endpoint = "event_agent", ok = true });
            Assert.IsFalse(DemoReadout.PlannedByTheModel(trace), "the base plan is there from the first event");

            trace.calls.Add(new ModelCallDto { endpoint = "season_plan", ok = false });
            Assert.IsTrue(DemoReadout.PlannerAnswered(trace));
            Assert.IsFalse(DemoReadout.PlannedByTheModel(trace), "a call that failed planned nothing");

            trace.calls.Add(new ModelCallDto { endpoint = "season_plan", ok = true });
            Assert.IsTrue(DemoReadout.PlannedByTheModel(trace));
        }

        [Test]
        public void Describe_SaysWhereTheMoneyGoes()
        {
            string text = DemoReadout.Describe(new Effect(EffectKind.Expense, 85_000L, MoneySource.CreditCard));
            StringAssert.Contains(Money.Format(85_000L), text);
            StringAssert.Contains(Loc.T("a la tarjeta"), text);
            StringAssert.Contains("-", DemoReadout.Describe(new Effect(EffectKind.SavingsChange, -5_000L)));
        }

        [Test]
        public void DescribeAll_ANoChangeChoiceStillSaysSomething()
        {
            Assert.AreEqual(Loc.T("Sin cambios en la plata"), DemoReadout.DescribeAll(new Choice("skip", "Nada", new List<Effect>())));
        }

        [Test]
        public void Gallery_HasTheTwentyPicturesOfTheBackend()
        {
            Assert.AreEqual(5, DemoReadout.Portraits.Length);
            Assert.AreEqual(12, DemoReadout.Scenes.Length);
            Assert.AreEqual(3, PostcardKeys.All.Length);
        }
    }

    public sealed class DemoScenariosTests
    {
        [Test]
        public void EveryScenario_IsAValidStateWithItsTextsTranslated()
        {
            Assert.AreEqual(4, DemoScenarios.All.Count);
            foreach (var scenario in DemoScenarios.All)
            {
                var state = scenario.Build();
                Assert.That(state.Month, Is.InRange(ContractLimits.MinMonth, ContractLimits.MaxMonth));
                Assert.That(state.CreditCard.Balance, Is.LessThanOrEqualTo(state.CreditCard.Limit));
                Assert.IsTrue(Translations.English.ContainsKey(scenario.Name), scenario.Name);
                Assert.IsTrue(Translations.English.ContainsKey(scenario.Summary), scenario.Summary);
            }
        }

        [Test]
        public void Scenarios_ReachTheSituationsTheGameMasterReactsTo()
        {
            var card = DemoScenarios.CardAtTheLimit().CreditCard;
            Assert.Greater(card.Balance, card.Limit * 9L / 10L);
            Assert.Greater(DemoScenarios.HighSavings().Savings, 3L * DemoScenarios.HighSavings().SalaryNet / 2L);
            Assert.AreEqual(12, DemoScenarios.December().Month);
        }

        [Test]
        public void YearEnd_GivesThePostcardItIsAbout()
        {
            Assert.AreEqual(PostcardKeys.HeavyDebt, PostcardKeys.For(DemoScenarios.TotalDebt(DemoScenarios.YearEnd(true))));
            Assert.AreEqual(PostcardKeys.DebtFree, PostcardKeys.For(DemoScenarios.TotalDebt(DemoScenarios.YearEnd(false))));
        }

        [Test]
        public void AScenario_IsANewStateEveryTime()
        {
            var first = DemoScenarios.FirstMonth();
            first.Cash = 0L;
            Assert.AreNotEqual(0L, DemoScenarios.FirstMonth().Cash);
        }
    }
}

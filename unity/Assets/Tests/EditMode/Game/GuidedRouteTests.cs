using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class GuidedRouteTests
    {
        // ---------------------------------------------------------------- where to point

        [Test]
        public void PointAt_ThePlaceHereTheDoorFromTownTheWayOutFromElsewhere()
        {
            var store = new Objective("x", SceneNames.Tienda, "Station_Store");

            Assert.AreEqual("Station_Store", Objectives.PointAt(store, SceneNames.Tienda));
            Assert.AreEqual("Door_" + SceneNames.Tienda, Objectives.PointAt(store, SceneNames.Town));
            Assert.AreEqual(Objectives.WayOut, Objectives.PointAt(store, SceneNames.Banco));
        }

        [Test]
        public void PointAt_NothingWithoutAPlaceOrOnceInsideAScene()
        {
            Assert.IsNull(Objectives.PointAt(null, SceneNames.Town));
            Assert.IsNull(Objectives.PointAt(new Objective("x"), SceneNames.Town));
            Assert.IsNull(Objectives.PointAt(new Objective("x", SceneNames.Cuarto), SceneNames.Cuarto));
        }

        // ---------------------------------------------------------------- the objectives of a normal run

        [Test]
        public void ForGame_TheFirstDayGoesRoomInsuranceWork()
        {
            var loop = new GameLoop();
            loop.BeginArrivalDay();
            Assert.AreEqual(SceneNames.Cuarto, Objectives.ForGame(loop, SceneNames.Town).Scene);

            loop.MarkRoomVisited();
            Assert.AreEqual("Station_HealthDesk", Objectives.ForGame(loop, SceneNames.Town).Target);

            loop.BuyHealthInsurance();
            Assert.AreEqual("Station_Work", Objectives.ForGame(loop, SceneNames.Town).Target);
        }

        [Test]
        public void ForGame_TheMorningIsForWorkAndAFreeAfternoonAsksNothing()
        {
            var loop = new GameLoop();
            Assert.AreEqual("Station_Work", Objectives.ForGame(loop, SceneNames.Town).Target);

            loop.ClockIn();
            loop.FinishShift();
            Assert.IsNull(Objectives.ForGame(loop, SceneNames.Town));
        }

        [Test]
        public void ForGame_ALoanSendsToTheLenderAndLateBillsToThePhoneFirst()
        {
            var loop = new GameLoop();
            loop.ClockIn();
            loop.FinishShift();
            loop.State.InformalLoans.Add(new InformalLoan("Prestamista", 50_000L, 15m));
            Assert.AreEqual("Station_Lender", Objectives.ForGame(loop, SceneNames.Town).Target);

            loop.Bills.Arrive(_ => 5_000L);
            var bills = Objectives.ForGame(loop, SceneNames.Town);
            Assert.IsNull(bills.Scene, "they are paid from the phone, anywhere");
        }

        [Test]
        public void ForGame_TheNightSendsHome()
        {
            var loop = new GameLoop();
            loop.ClockIn();
            loop.FinishShift();
            loop.Clock.SkipTo(GameRules.NightHour * 60f + 5f);

            Assert.AreEqual(SceneNames.Cuarto, Objectives.ForGame(loop, SceneNames.Town).Scene);
            Assert.IsNull(Objectives.PointAt(Objectives.ForGame(loop, SceneNames.Cuarto), SceneNames.Cuarto));
        }

        [Test]
        public void ForGame_TheTextsAreTranslated()
        {
            foreach (var text in new[] { "Pagá los recibos: abrí el teléfono con la P", TaskList.CatchUp, TaskList.CloseMonth, TaskList.VisitRoom })
            {
                Assert.IsTrue(Translations.English.ContainsKey(text), text);
            }
        }

        // ---------------------------------------------------------------- the guided run

        [Test]
        public void Consequence_FollowsTheMoneyAfterTheDecisions()
        {
            var state = GameRules.NewPlayer();
            GuidedRoute.Apply(state, GuidedProfile.Saver);
            Assert.AreEqual(TalkPartner.Pyramid, GuidedRoute.Consequence(state));

            GuidedRoute.Apply(state, GuidedProfile.Tight);
            Assert.AreEqual(TalkPartner.Lender, GuidedRoute.Consequence(state));

            state.Savings = 2_000_000L;
            Assert.AreEqual(TalkPartner.Pyramid, GuidedRoute.Consequence(state), "what counts is the money now, not the choice at the start");
        }

        [Test]
        public void GuidedMonth_PaysTheFullSalaryAndLeavesNoBillsBehind()
        {
            var loop = new GameLoop();
            GuidedRoute.Apply(loop.State, GuidedProfile.Tight);
            long net = loop.State.SalaryNet;

            loop.PayMidMonthQuincena();
            var report = loop.CloseMonth(dockRemainingShifts: false, withBills: false);

            Assert.AreEqual(net, report.SalaryCredited);
            Assert.AreEqual(0L, report.SalaryDocked);
            Assert.AreEqual(0, loop.Bills.Due.Count);
        }

        [Test]
        public void GuidedRun_ThreeMonthsFinishTheRun()
        {
            var loop = new GameLoop();
            for (int month = 0; month < 3; month++)
            {
                Assert.IsFalse(loop.IsFinished);
                loop.CloseMonth(dockRemainingShifts: false, withBills: false);
            }

            Assert.IsTrue(loop.IsFinished);
        }

        // ---------------------------------------------------------------- the offers

        [Test]
        public void LenderOffer_LendsWhatTheSheetSaysAtTheRateItSays()
        {
            var state = GameRules.NewPlayer();
            long cash = state.Cash;
            var offer = DebtTalk.OfferEvent(TalkPartner.Lender);

            FinanceEngine.ApplyChoice(state, offer, DebtTalk.TakeOffer);

            Assert.AreEqual(cash + DebtTalk.ExampleLoan, state.Cash);
            Assert.AreEqual(DebtTalk.ExampleLoan, state.InformalLoans[0].Balance);
            Assert.AreEqual((decimal)DebtTalk.LenderRatePct, state.InformalLoans[0].MonthlyRatePct);
        }

        [Test]
        public void LenderOffer_LendsEachAmountOnTheSheet()
        {
            var offer = DebtTalk.OfferEvent(TalkPartner.Lender);
            var request = new DebtTalk(TalkPartner.Lender).ToRequest("300 000", "en");

            Assert.AreEqual(DebtTalk.LenderLoans.Length + 1, offer.Choices.Count);
            foreach (long loan in DebtTalk.LenderLoans)
            {
                CollectionAssert.Contains(request.amounts, loan);
                CollectionAssert.Contains(request.amounts, DebtTalk.LenderTotal(loan));
            }

            var state = GameRules.NewPlayer();
            long cash = state.Cash;
            FinanceEngine.ApplyChoice(state, offer, offer.Choices[2].Id);
            Assert.AreEqual(cash + 300_000L, state.Cash);
            Assert.AreEqual(300_000L, state.InformalLoans[0].Balance);
        }

        [Test]
        public void PyramidOffer_TakesTheMoneyAndSkippingChangesNothing()
        {
            var state = GameRules.NewPlayer();
            GuidedRoute.Apply(state, GuidedProfile.Saver);
            long savings = state.Savings;
            var offer = DebtTalk.OfferEvent(TalkPartner.Pyramid);

            FinanceEngine.ApplyChoice(state, offer, DebtTalk.SkipOffer);
            Assert.AreEqual(savings, state.Savings);

            FinanceEngine.ApplyChoice(state, offer, DebtTalk.TakeOffer);
            Assert.AreEqual(savings - DebtTalk.ExampleLoan, state.Savings);
        }

        [Test]
        public void Offers_TeachTheirConcept()
        {
            Assert.AreEqual(TeachingConcept.InformalLending, DebtTalk.OfferEvent(TalkPartner.Lender).TeachingConcept);
            Assert.AreEqual(TeachingConcept.PyramidScheme, DebtTalk.OfferEvent(TalkPartner.Pyramid).TeachingConcept);
        }

        // ---------------------------------------------------------------- the people of the town

        [Test]
        public void PeopleOfTheTown_TheSamePersonIsAlwaysTheSame()
        {
            Assert.AreEqual(DebtTalk.PartnerFor("ClienteBarra"), DebtTalk.PartnerFor("ClienteBarra"));
            CollectionAssert.Contains(new[] { TalkPartner.Neighbor, TalkPartner.Stranger }, DebtTalk.PartnerFor("Peaton3"));
        }

        [Test]
        public void Neighbor_SaysOnlyFiguresFromTheGameAndAnswersWithoutAServer()
        {
            var talk = new DebtTalk(TalkPartner.Neighbor, 0L, 3.5m);
            var request = talk.ToRequest("hello", "en");

            Assert.AreEqual("neighbor", request.partner);
            CollectionAssert.Contains(request.percents, 3);
            CollectionAssert.Contains(request.percents, DebtTalk.LenderRatePct);
            Assert.IsEmpty(request.amounts);
            StringAssert.Contains("3.5", talk.Say("¿Qué tan caro sale la tarjeta?"));
        }

        [Test]
        public void Stranger_AsksForWhatTheGameSays()
        {
            var talk = new DebtTalk(TalkPartner.Stranger);
            var request = talk.ToRequest("who are you?", "en");

            Assert.AreEqual("stranger", request.partner);
            CollectionAssert.AreEqual(new[] { DebtTalk.StrangerAsk }, request.amounts);
            Assert.IsNotEmpty(talk.Say("¿Me manda el comprobante?"));
        }

        [Test]
        public void PeopleOfTheTown_TheirQuestionsAreTranslated()
        {
            foreach (var partner in new[] { TalkPartner.Neighbor, TalkPartner.Stranger })
            {
                foreach (string question in new DebtTalk(partner).SuggestedQuestions)
                {
                    Assert.IsTrue(Translations.English.ContainsKey(question), question);
                }
            }
        }
    }
}

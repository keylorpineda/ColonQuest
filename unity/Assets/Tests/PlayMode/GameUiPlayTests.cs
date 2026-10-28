using System;
using System.Collections;
using ColonQuest.Api;
using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>A backend that is never reachable, so the tests always play the local events.</summary>
    internal sealed class OfflineTransport : IHttpTransport
    {
        public IEnumerator Send(HttpRequestData request, Action<HttpResponseData> done)
        {
            done(new HttpResponseData { NetworkError = "offline" });
            yield break;
        }
    }

    /// <summary>Plays the overlay on the real Town scene: the prompt, an event, its result and the month close.</summary>
    public sealed class GameUiPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static GameSession Session => GameSession.Instance;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        private static bool ModalOpen => Ui.Find("Modal").gameObject.activeSelf;

        private IEnumerator StartOfflineRun()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            Session.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Town_CreatesTheSessionAndTheOverlay()
        {
            yield return null;

            Assert.IsNotNull(Session);
            Assert.IsNotNull(GameObject.Find("GameUi"));
            Assert.IsNotNull(Ui.Find("Hud"));
        }

        [UnityTest]
        public IEnumerator Prompt_AppearsInsideAStationAndHidesOutside()
        {
            yield return StartOfflineRun();
            var prompt = Ui.Find("Prompt").gameObject;

            Teleport(GameObject.Find("Station_Garage").transform.position);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(prompt.activeSelf, "inside the station");

            Teleport(new Vector2(0f, -6f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(prompt.activeSelf, "out on the avenue");
        }

        [UnityTest]
        public IEnumerator Task_ShowsAnEventThenItsResultAndCountsTheTask()
        {
            yield return StartOfflineRun();

            Session.BeginTask(StationType.Garage);
            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(Session.CurrentEvent, "the offline run plays a local event");
            Assert.AreEqual(ResultSource.Fallback, Session.CurrentEventSource);
            Assert.IsTrue(ModalOpen, "the event window is open");
            Assert.IsTrue(PlayerController.Blocked, "the player stands still while a window is open");

            long cash = Session.State.Cash;
            string choiceId = Session.CurrentEvent.Choices[0].Id;
            Session.Choose(choiceId);
            yield return null;

            Assert.AreEqual(1, Session.Loop.TasksDone);
            Assert.IsTrue(ModalOpen, "the result window replaces the event window");
            Assert.AreNotEqual(cash, Session.State.Cash + Session.State.CreditCard.Balance + Session.State.Savings + Session.State.EmergencyFund,
                "the decision changed the numbers");
        }

        private static bool NegotiationOpen => Ui.Find("Negotiation").gameObject.activeSelf;

        [UnityTest]
        public IEnumerator Negotiation_OffRunTheLocalSellerAnswersAndRevealsOnlyWhatIsAsked()
        {
            yield return StartOfflineRun();

            Session.BeginNegotiation();
            yield return null;
            Assert.IsTrue(NegotiationOpen, "the seller's window opens");
            Assert.IsTrue(PlayerController.Blocked || Session.CurrentNegotiation != null);

            Session.SendNegotiation("Cuanto pago en total?");
            yield return new WaitForSeconds(0.4f);

            var negotiation = Session.CurrentNegotiation;
            Assert.AreEqual(1, negotiation.Turns.Count);
            Assert.IsTrue(negotiation.Turns[0].FromLocalSeller, "offline, so the local seller answers");
            StringAssert.Contains(negotiation.Offer.Describe(SellerFact.TotalCost), negotiation.Turns[0].Seller);
            Assert.AreEqual(1, negotiation.KeyQuestionsAsked.Count);
            Assert.AreEqual(0, Session.Loop.TasksDone, "talking is not buying");
        }

        [UnityTest]
        public IEnumerator Negotiation_BuyingAddsTheInstallmentAndCountsTheTask()
        {
            yield return StartOfflineRun();
            long installmentsBefore = GameRules.MonthlyInstallments(Session.State);

            Session.BeginNegotiation();
            var offer = Session.CurrentNegotiation.Offer;
            Session.AcceptDeal();
            yield return null;

            Assert.AreEqual(installmentsBefore + offer.MonthlyCharge, GameRules.MonthlyInstallments(Session.State));
            Assert.AreEqual(1, Session.Loop.TasksDone);
            Assert.IsFalse(NegotiationOpen, "the window closes");
            Assert.IsTrue(ModalOpen, "the result window shows what the deal cost");
            Assert.IsNotNull(Session.LastDeal);
        }

        [UnityTest]
        public IEnumerator Negotiation_WalkingAwayKeepsTheNumbersAndStillCountsTheTask()
        {
            yield return StartOfflineRun();
            long cash = Session.State.Cash;

            Session.BeginNegotiation();
            Session.WalkAway();
            yield return null;

            Assert.AreEqual(cash, Session.State.Cash);
            Assert.AreEqual(1, Session.Loop.TasksDone);
            Assert.IsFalse(NegotiationOpen);
        }

        [UnityTest]
        public IEnumerator Hud_ShowsTheStateOfTheRun()
        {
            yield return StartOfflineRun();
            yield return null;

            var texts = Ui.Find("Hud").GetComponentsInChildren<UnityEngine.UI.Text>();
            string all = string.Empty;
            foreach (var text in texts)
            {
                all += text.text + "|";
            }

            StringAssert.Contains("Mes 1 de " + GameRules.MonthsToPlay, all);
            StringAssert.Contains("₡120 000", all);
        }

        [UnityTest]
        public IEnumerator ClosingTheMonthsEndsTheRunWithASummary()
        {
            yield return StartOfflineRun();
            SessionSummary summary = null;
            Session.Finished += s => summary = s;

            for (int i = 0; i < GameRules.MonthsToPlay; i++)
            {
                Session.CloseMonth();
                yield return null;
            }

            Assert.IsTrue(Session.Loop.IsFinished);
            Session.FinishRun();
            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(summary);
            Assert.AreEqual("local", summary.Source, "offline, so the summary is written locally");
        }
    }
}

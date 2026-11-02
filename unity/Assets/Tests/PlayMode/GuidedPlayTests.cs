using System;
using System.Collections;
using System.Reflection;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the guided run from the first window to the last, pressing what a player would press.</summary>
    internal sealed class GuidedDriver
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly GameSession session;
        private readonly GameUi ui;
        private readonly ModalPanel modal;
        private readonly Func<string, IEnumerator> shot;

        public bool TookTheOffer { get; set; } = true;

        public GuidedDriver(GameSession session, Func<string, IEnumerator> shot = null)
        {
            this.session = session;
            this.shot = shot;
            ui = UnityEngine.Object.FindFirstObjectByType<GameUi>();
            modal = (ModalPanel)typeof(GameUi).GetField("modal", Private).GetValue(ui);
        }

        private IEnumerator Shot(string name)
        {
            if (shot != null)
            {
                yield return shot(name);
            }
        }

        public static IEnumerator Until(Func<bool> done, float seconds, string what)
        {
            float started = Time.unscaledTime;
            while (!done())
            {
                Assert.Less(Time.unscaledTime - started, seconds, "still waiting for " + what);
                if (Interaction.Playing)
                {
                    Interaction.SkipCurrent();
                }

                yield return null;
            }
        }

        private IEnumerator WindowOpens(string what)
        {
            yield return Until(() => modal.IsOpen, 40f, what);
            yield return null;
        }

        /// <summary>Presses an option as a player does: the first press only finishes the line that is being written.</summary>
        private IEnumerator Press(int option)
        {
            if (modal.IsRevealing)
            {
                modal.Press(option);
                yield return null;
            }

            modal.Press(option);
            yield return null;
        }

        private IEnumerator Next(string what, string name, int option = 0)
        {
            yield return WindowOpens(what);
            yield return Shot(name);
            yield return Press(option);
        }

        private StationType? GuidedType => (StationType?)typeof(GameUi).GetField("guidedStation", Private).GetValue(ui);

        private Station Guided()
        {
            var type = GuidedType;
            if (!type.HasValue)
            {
                return null;
            }

            foreach (var station in UnityEngine.Object.FindObjectsByType<Station>(FindObjectsSortMode.None))
            {
                if (station.Type == type.Value)
                {
                    return station;
                }
            }

            return null;
        }

        private void Use(Station station)
        {
            typeof(GameUi).GetMethod("Interact", Private).Invoke(ui, new object[] { station });
        }

        private static void Teleport(Vector2 at)
        {
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            hero.transform.position = at;
            hero.GetComponent<Rigidbody2D>().position = at;
        }

        public IEnumerator Play(int profile)
        {
            yield return Next("the first window", "intro", profile);
            yield return Next("the first salary", "salary");

            // Month 1: the game master.
            yield return Until(() => session.CurrentEvent != null && modal.IsOpen, 30f, "the first event");
            yield return Shot("event");
            for (int i = 0; i < modal.OptionCount && session.CurrentEvent != null; i++)
            {
                yield return Press(i);
            }

            Assert.IsNull(session.CurrentEvent, "some choice of the event was open");
            yield return Next("the outcome of the event", "outcome");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("wonder");
            yield return Next("the report of the first month", "month1");
            Assert.AreEqual(1, session.Loop.MonthsPlayed);

            // Month 2: the seller.
            yield return Until(() => GuidedType.HasValue, 10f, "the second objective");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("to_store");
            SceneTransition.Go(SceneNames.Tienda, SpawnIds.FromTown);
            yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Tienda && !SceneTransition.IsBusy, 15f, "the store");
            yield return Until(() => Guided() != null, 10f, "the seller's counter");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("in_store");
            Use(Guided());
            yield return Until(() => session.CurrentNegotiation != null, 15f, "the seller");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("seller_opens");
            session.AskKeyQuestion(KeyQuestion.TotalCost, "How much do I pay in total?");
            yield return Until(() => !session.Busy, 30f, "the seller's answer");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("seller");
            session.WalkAway();
            yield return Next("the outcome of the talk", "deal");
            yield return Next("the report of the second month", "month2");
            Assert.AreEqual(2, session.Loop.MonthsPlayed);

            // Month 3: what those decisions bring.
            yield return Until(() => GuidedType.HasValue, 10f, "the third objective");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("way_out");
            SceneTransition.Go(SceneNames.Town, SpawnIds.TownFromTienda);
            yield return Until(() => SceneManager.GetActiveScene().name == SceneNames.Town && !SceneTransition.IsBusy, 15f, "the town");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("to_consequence");
            var place = Guided();
            bool lender = place.Type == StationType.Lender;
            Teleport((Vector2)place.transform.position + new Vector2(0f, -1f));
            yield return new WaitForSecondsRealtime(0.3f);
            Use(place);
            yield return Until(() => session.CurrentTalk != null, 10f, "the talk");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("at_consequence");
            session.SendTalk(lender ? "What is the interest?" : "Where does the money come from?");
            yield return Until(() => !session.Busy, 30f, "the answer");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("talk");
            session.EndTalk();
            session.BeginOffer(lender ? TalkPartner.Lender : TalkPartner.Pyramid);
            yield return Next("the offer", "offer", TookTheOffer ? 0 : 1);
            yield return Next("the outcome of the offer", "offer_outcome");
            if (TookTheOffer)
            {
                yield return Next(lender ? "the collectors" : "the pyramid's end", "after_offer");
                yield return Until(() => !Interaction.Playing, 10f, "the men going back to the car");
                Assert.IsTrue(session.Guided, "the run goes on after the offer");
                if (lender)
                {
                    yield return Next("what the collectors did", "collectors_outcome");
                }
            }

            yield return Next("the report of the third month", "month3");
            Assert.IsTrue(session.Loop.IsFinished);
            yield return Until(() => modal.IsOpen && !session.Busy && !session.Guided, 60f, "the end of the run");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("end");
        }
    }

    public sealed class GuidedPlayTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator GuidedRun_GoesFromTheFirstWindowToTheReportWithoutABackend()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.StartGuided();
            GameUi.Create(session).PlayGuidedNext();
            LoadingScreen.Load(SceneNames.Town);
            yield return GuidedDriver.Until(() => SceneManager.GetActiveScene().name == SceneNames.Town && !LoadingScreen.IsLoading, 30f, "the town");
            Assert.IsTrue(session.Guided);
            Assert.AreEqual(-1, session.ActiveSlot, "the guided run never touches a saved game");

            yield return new GuidedDriver(session).Play(0);

            Assert.IsFalse(session.Guided, "it hands the game back");
            Assert.AreEqual(GameRules.MonthsToPlay, session.Loop.MonthsPlayed);
        }

        [UnityTest]
        public IEnumerator GuidedRun_KeepsTheClockStill()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.StartGuided();
            float at = session.Loop.Clock.Minutes;

            Assert.IsFalse(session.TickClock(3600f));
            yield return null;

            Assert.AreEqual(at, session.Loop.Clock.Minutes);
            session.LeaveGuided();
        }
    }
}

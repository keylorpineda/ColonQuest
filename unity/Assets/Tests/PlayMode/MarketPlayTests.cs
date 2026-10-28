using System.Collections;
using System.Linq;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the new districts on the real Town scene: the places to buy, the way there and the purchases themselves.</summary>
    public sealed class MarketPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static GameSession Session => GameSession.Instance;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        private static bool ModalOpen => Ui.Find("Modal").gameObject.activeSelf;

        private static bool NegotiationOpen => Ui.Find("Negotiation").gameObject.activeSelf;

        private static Station[] Stations(StationType type)
        {
            return Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Where(s => s.Type == type).ToArray();
        }

        private IEnumerator StartOfflineRun()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            Session.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;
        }

        // ---------------------------------------------------------------- the places

        [UnityTest]
        public IEnumerator Districts_HaveTheAgenciesTheFarmAndTheHouseSigns()
        {
            yield return null;

            Assert.AreEqual(1, GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "Inmobiliaria"));
            Assert.AreEqual(1, GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "AgenciaDeCarros"));
            Assert.AreEqual(1, GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "FincaSanIsidro"));

            var homes = Stations(StationType.RealEstate);
            Assert.AreEqual(5, homes.Length, "the agency and four house signs");
            Assert.AreEqual(1, homes.Count(s => s.Lines.Length == 0), "only the agency lists every house");
            foreach (var home in homes.Where(s => s.Lines.Length > 0))
            {
                Assert.IsNotNull(MarketAssumptions.FindHome(home.Lines[0]), home.name + " names a listing that exists");
            }

            Assert.AreEqual(1, Stations(StationType.CarDealer).Length);
            Assert.AreEqual(1, Stations(StationType.Farm).Length);
        }

        [UnityTest]
        public IEnumerator EveryListingHasASignAndAHouseToSee()
        {
            yield return null;

            var signed = Stations(StationType.RealEstate).Where(s => s.Lines.Length > 0).Select(s => s.Lines[0]).ToList();
            foreach (var home in MarketAssumptions.Homes)
            {
                CollectionAssert.Contains(signed, home.Id, home.Name + " has a sign out in the street");
            }
        }

        [UnityTest]
        public IEnumerator MarketPlaces_AreInsideTheWallsAndOutOfTheRiver()
        {
            yield return null;

            var places = Stations(StationType.RealEstate).Concat(Stations(StationType.CarDealer)).Concat(Stations(StationType.Farm));
            foreach (var place in places)
            {
                Vector2 at = place.transform.position;
                Assert.Greater(at.x, MapLayout.PlayWestX, place.name);
                Assert.Less(at.x, MapLayout.PlayEastX, place.name);
                Assert.Less(at.y, MapLayout.PlayNorthY, place.name);
                Assert.IsFalse(at.x > MapLayout.RiverXMin - 1f && at.x < MapLayout.RiverXMax + 1f, place.name + " is not in the water");
            }
        }

        [UnityTest]
        public IEnumerator Town_StillHasItsSixTaskStations()
        {
            yield return null;

            var tasks = Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Where(s => GameLoop.IsTaskStation(s.Type));
            Assert.AreEqual(6, tasks.Count(), "the districts add places to spend, not tasks of the month");
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksIntoTheFarm()
        {
            var farm = Stations(StationType.Farm)[0];
            Station entered = null;
            farm.PlayerEntered += s => entered = s;

            Teleport(farm.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(farm, entered);
        }

        // ---------------------------------------------------------------- getting there

        [UnityTest]
        public IEnumerator Player_WalksFromTheFirstStreetToTheAvenueByTheDirtPath()
        {
            yield return Walk(new Vector2(MapLayout.NorthPathX, 7.5f), Vector2.up, 4.6f);

            Assert.Greater(Body.position.y, 25f, "the path reaches the avenue");
            Assert.Less(Body.position.x, MapLayout.RiverXMin, "still west of the river");
        }

        [UnityTest]
        public IEnumerator Player_CrossesTheRiverOnTheAvenueBridge()
        {
            yield return Walk(new Vector2(25f, MapLayout.AvenueY), Vector2.right, 3.2f);

            Assert.Greater(Body.position.x, MapLayout.RiverXMax + 1f);
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheRiverNorthOfTheAvenue()
        {
            yield return Walk(new Vector2(25.5f, 37f), Vector2.right, 2f);

            Assert.Less(Body.position.x, MapLayout.RiverXMin);
        }

        [UnityTest]
        public IEnumerator Player_WalksEastAlongTheStreetToTheFarm()
        {
            yield return Walk(new Vector2(38f, 2f), Vector2.right, 3.2f);

            Assert.Greater(Body.position.x, 50f, "the street runs on past the old wall");
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheNewInvisibleWallInTheEast()
        {
            yield return Walk(new Vector2(60f, 2f), Vector2.right, 2f);

            Assert.Less(Body.position.x, MapLayout.PlayEastX + 0.1f);
            Assert.Greater(Body.position.x, MapLayout.PlayEastX - 1.5f, "it reached the wall");
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheNewInvisibleWallInTheNorth()
        {
            yield return Walk(new Vector2(22f, 36f), Vector2.up, 3f);

            Assert.Less(Body.position.y, MapLayout.PlayNorthY + 0.1f);
            Assert.Greater(Body.position.y, MapLayout.PlayNorthY - 1.5f, "it reached the wall");
        }

        // ---------------------------------------------------------------- buying

        [UnityTest]
        public IEnumerator CarNegotiation_TheAgencySellerAnswersLocallyWithoutTheBackend()
        {
            yield return StartOfflineRun();
            var car = MarketAssumptions.FindCar("compacto");

            Session.BeginCarNegotiation(car);
            yield return null;
            Assert.IsTrue(NegotiationOpen, "the window opens like the store's");
            Assert.AreEqual("Agencia de carros", Session.CurrentNegotiation.Venue);

            Session.SendNegotiation("¿Cuánto es la prima?");

            Assert.IsFalse(Session.Busy, "nothing was sent to the backend");
            Assert.AreEqual(1, Session.CurrentNegotiation.Turns.Count);
            Assert.IsTrue(Session.CurrentNegotiation.Turns[0].FromLocalSeller);
            StringAssert.Contains(Money.Format(car.DownPayment), Session.CurrentNegotiation.Turns[0].Seller);
        }

        [UnityTest]
        public IEnumerator CarNegotiation_WithoutTheCashTheAgencySaysSoAndWaits()
        {
            yield return StartOfflineRun();
            Session.BeginCarNegotiation(MarketAssumptions.FindCar("compacto"));
            yield return null;

            Session.AcceptDeal();
            yield return null;

            Assert.IsNotNull(Session.CurrentNegotiation, "the conversation stays open");
            Assert.IsNotNull(Session.DealNotice);
            Assert.IsNull(Session.Holdings.Car);
            Assert.AreEqual(120_000L, Session.State.Cash);
        }

        [UnityTest]
        public IEnumerator CarNegotiation_WithTheCashTheCarIsBoughtThroughTheEngine()
        {
            yield return StartOfflineRun();
            var car = MarketAssumptions.FindCar("compacto");
            Session.State.Cash = 2_000_000L;
            Session.BeginCarNegotiation(car);
            yield return null;
            Session.SendNegotiation("¿Cuánto termino pagando en total?");

            Session.AcceptDeal();
            yield return null;

            Assert.IsNull(Session.CurrentNegotiation);
            Assert.IsNotNull(Session.Holdings.Car);
            Assert.AreEqual(2_000_000L - car.DownPayment - car.Commission - car.MarchamoFirstYear, Session.State.Cash);
            Assert.IsNotNull(Session.LastDeal);
            Assert.AreEqual(1, Session.LastDeal.QuestionsAsked);
            Assert.IsTrue(ModalOpen, "the result window shows what changed");
            Assert.AreEqual(0, Session.Loop.TasksDone, "the agency is not a task of the month");
        }

        [UnityTest]
        public IEnumerator CarNegotiation_WalkingAwayLeavesTheAccountsAsTheyWere()
        {
            yield return StartOfflineRun();
            Session.State.Cash = 2_000_000L;
            Session.BeginCarNegotiation(MarketAssumptions.FindCar("sedan"));
            yield return null;

            Session.WalkAway();
            yield return null;

            Assert.IsNull(Session.CurrentNegotiation);
            Assert.IsNull(Session.CurrentCar);
            Assert.IsTrue(Session.Holdings.IsEmpty);
            Assert.AreEqual(2_000_000L, Session.State.Cash);
        }

        [UnityTest]
        public IEnumerator Rent_ThroughTheSessionStartsTheInstallmentAndShowsTheResult()
        {
            yield return StartOfflineRun();
            Session.State.Cash = 1_000_000L;
            long installments = GameRules.MonthlyInstallments(Session.State);

            bool done = Session.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));
            yield return null;

            Assert.IsTrue(done);
            Assert.AreEqual(installments + 260_000L, GameRules.MonthlyInstallments(Session.State));
            Assert.IsTrue(ModalOpen);
        }

        [UnityTest]
        public IEnumerator Rent_WithoutTheDepositIsRefusedAndNothingChanges()
        {
            yield return StartOfflineRun();

            bool done = Session.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));
            yield return null;

            Assert.IsFalse(done);
            Assert.IsTrue(Session.Holdings.IsEmpty);
            Assert.AreEqual(120_000L, Session.State.Cash);
        }

        [UnityTest]
        public IEnumerator Farm_TheStakeLeavesTheCashAndShowsUpInTheEndReport()
        {
            yield return StartOfflineRun();
            Session.State.Cash = 300_000L;
            var coffee = MarketAssumptions.FindFarm(FarmKind.Coffee);

            bool done = Session.InvestInFarm(coffee, 100_000L);
            yield return null;

            Assert.IsTrue(done);
            Assert.AreEqual(200_000L, Session.State.Cash);
            var lines = AssetsReport.Build(Session.Holdings, GameRules.MonthsToPlay);
            StringAssert.Contains("Finca (café)", lines[0]);
            Assert.AreEqual(MarketAssumptions.Disclaimer, lines[lines.Count - 1]);
        }

        [UnityTest]
        public IEnumerator Purchases_AreRefusedWhileAConversationIsOpen()
        {
            yield return StartOfflineRun();
            Session.State.Cash = 1_000_000L;
            Session.BeginNegotiation();
            yield return null;

            bool done = Session.RentHome(MarketAssumptions.FindHome("casa_vecina_d"));

            Assert.IsFalse(done);
            Assert.AreEqual(1_000_000L, Session.State.Cash);
        }
    }
}

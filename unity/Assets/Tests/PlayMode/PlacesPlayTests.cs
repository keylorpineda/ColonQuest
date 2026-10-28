using System.Collections;
using System.Linq;
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
    /// <summary>Makes sure the session and the overlay exist, offline, whatever scene a test starts in.</summary>
    internal static class PlaceTestSetup
    {
        public static GameSession StartOfflineRun()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            AudioDirector.EnsureCreated();
            var session = GameSession.Create();
            if (GameObject.Find("GameUi") == null)
            {
                GameUi.Create(session);
            }

            session.StartNewGame();
            PlayerController.Blocked = false;
            Time.timeScale = 1f;
            return session;
        }

        public static Station[] Stations(StationType type)
        {
            return Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Where(s => s.Type == type).ToArray();
        }
    }

    /// <summary>The car agency: three cars on show, Don Chepe at his desk, a glass office, a waiting room and a workshop.</summary>
    public sealed class AgenciaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Agencia;

        [UnityTest]
        public IEnumerator Agency_HasTheShowroomTheOfficeTheWaitingRoomAndTheWorkshop()
        {
            yield return null;

            var cars = Object.FindObjectsByType<Station>(FindObjectsSortMode.None).Count(s => s.name == "Thing_Car" || s.name.StartsWith("Thing_Car"));
            Assert.GreaterOrEqual(cars, 4, "three cars on show and one on the lift");
            Assert.IsNotNull(GameObject.Find("DonChepe"), "he welcomes the player inside");
            Assert.AreEqual(1, PlaceTestSetup.Stations(StationType.CarDealer).Length, "the negotiation opens here");
            Assert.IsNotNull(GameObject.Find("FinancingOffice"));
            Assert.IsNotNull(GameObject.Find("WaitingRoom"));
            Assert.IsNotNull(GameObject.Find("Lift"));
            Assert.IsNotNull(GameObject.Find("Mecanico"));
            Assert.IsNotNull(GameObject.Find("PriceTag0"));
        }

        [UnityTest]
        public IEnumerator Agency_TheDeskOpensTheCarCatalogWithDonChepe()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            yield return null;
            var desk = PlaceTestSetup.Stations(StationType.CarDealer)[0];
            Station entered = null;
            desk.PlayerEntered += s => entered = s;

            Teleport(desk.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(desk, entered);
            session.BeginCarNegotiation(MarketAssumptions.FindCar("compacto"));
            Assert.AreEqual("Agencia de carros", session.CurrentNegotiation.Venue);
        }

        [UnityTest]
        public IEnumerator Agency_TheDoormatLeadsBackToTheAvenue()
        {
            Teleport(MapLayout.AgenciaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(9f, Body.position.x, 0.8f);
            Assert.AreEqual(31.6f, Body.position.y, 0.8f);
        }
    }

    /// <summary>The police station: cells, the officer's counter and the judge.</summary>
    public sealed class CarcelPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Carcel;

        [UnityTest]
        public IEnumerator Jail_HasFourCellsACounterAndAJudge()
        {
            yield return null;

            Assert.IsNotNull(GameObject.Find("Cells"));
            Assert.IsNotNull(GameObject.Find("CellBed3"));
            Assert.IsNotNull(GameObject.Find("Oficial"));
            Assert.IsNotNull(GameObject.Find("Juez"));
            Assert.IsNotNull(GameObject.Find("Thing_Judge"));
        }

        [UnityTest]
        public IEnumerator Jail_AnArrestedPlayerWakesUpInTheCell()
        {
            SceneTransition.Go(SceneNames.Carcel, SpawnIds.JailCell);
            yield return new WaitForSeconds(2.5f);
            yield return null;
            FindPlayer();

            Assert.AreEqual(-7.6f, Body.position.x, 0.6f);
            Assert.AreEqual(3.0f, Body.position.y, 0.6f);
        }

        [UnityTest]
        public IEnumerator Jail_TheDoormatLeadsToTheStationDoor()
        {
            Teleport(MapLayout.CarcelMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(56f, Body.position.x, 0.8f);
            Assert.AreEqual(31.6f, Body.position.y, 0.8f);
        }
    }

    /// <summary>The hospital: reception with the insurance desk, the emergency ward and the stairs up.</summary>
    public sealed class HospitalPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Hospital;

        [UnityTest]
        public IEnumerator Hospital_HasTheInsuranceDeskTheWardAndTheStairs()
        {
            yield return null;

            Assert.AreEqual(1, PlaceTestSetup.Stations(StationType.HealthDesk).Length);
            Assert.IsNotNull(GameObject.Find("EmergencyWard"));
            var stairs = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Where(d => d.TargetScene == SceneNames.HospitalArriba).ToArray();
            Assert.AreEqual(1, stairs.Length);
        }

        [UnityTest]
        public IEnumerator Hospital_TheInsuranceDeskIsNotATaskAndTheNurseIsThere()
        {
            yield return null;

            Assert.IsFalse(GameLoop.IsTaskStation(StationType.HealthDesk));
            Assert.IsNotNull(GameObject.Find("Enfermera"));
        }

        [UnityTest]
        public IEnumerator Hospital_AHurtPlayerWakesUpInABed()
        {
            SceneTransition.Go(SceneNames.Hospital, SpawnIds.HospitalBed);
            yield return new WaitForSeconds(2.5f);
            yield return null;
            FindPlayer();

            Assert.AreEqual(6.4f, Body.position.x, 0.6f);
            Assert.AreEqual(-2.6f, Body.position.y, 0.6f);
        }

        [UnityTest]
        public IEnumerator Hospital_TheStairsLeadUpAndTheUpperMatLeadsBackDown()
        {
            var stairs = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First(d => d.TargetScene == SceneNames.HospitalArriba);
            Body.linearVelocity = Vector2.zero;
            Teleport((Vector2)stairs.transform.position + new Vector2(0f, -0.3f));
            Body.linearVelocity = Vector2.up * 3f;
            yield return WaitForScene(SceneNames.HospitalArriba, 3f);
            Assert.AreEqual(SceneNames.HospitalArriba, SceneManager.GetActiveScene().name);
            FindPlayer();
            Body.GetComponent<PlayerController>().enabled = false;

            Teleport(MapLayout.HospitalArribaMat);
            yield return WaitForScene(SceneNames.Hospital, 3f);
            FindPlayer();

            Assert.AreEqual(-8.4f, Body.position.x, 0.8f);
            Assert.AreEqual(4.0f, Body.position.y, 0.8f);
        }
    }

    /// <summary>The upper floor: the consulting rooms and the lab.</summary>
    public sealed class HospitalArribaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.HospitalArriba;

        [UnityTest]
        public IEnumerator Upstairs_HasThreeConsultingRoomsAndALab()
        {
            yield return null;

            Assert.IsNotNull(GameObject.Find("ConsultingRooms"));
            Assert.IsNotNull(GameObject.Find("Doctor1"));
            Assert.IsNotNull(GameObject.Find("Doctor3"));
            Assert.IsNotNull(GameObject.Find("Laboratorista"));
        }
    }

    /// <summary>The neighbor's house: the owner shouts, and a player who stays is arrested.</summary>
    public sealed class VecinoPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Vecino;

        [UnityTest]
        public IEnumerator Neighbor_TheOwnerCountsDownAndThePlayersOwnDeskIsNotThere()
        {
            yield return null;

            var watch = TrespassWatch.Active;
            Assert.IsNotNull(watch);
            Assert.AreEqual(MarketAssumptions.TrespassGraceSeconds, watch.Remaining, 1.5f);
            Assert.IsNotNull(GameObject.Find("DonMario"));
            Assert.AreEqual(0, PlaceTestSetup.Stations(StationType.Home).Length, "the statement desk is in the player's house only");
        }

        [UnityTest]
        public IEnumerator Neighbor_LeavingInTimeCostsNothing()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            yield return null;
            long cash = session.State.Cash;

            Teleport(MapLayout.VecinoMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            yield return new WaitForSeconds(0.5f);

            Assert.IsNull(TrespassWatch.Active);
            Assert.AreEqual(cash, session.State.Cash);
            Assert.AreEqual(0, session.Loop.TasksDone);
        }

        [UnityTest]
        public IEnumerator Neighbor_StayingTooLongMeansTheFineTheLostDaysAndTheJail()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.State.Cash = 500_000L;
            yield return null;
            TrespassWatch.Active.Configure("Don Mario", 0.3f);

            yield return WaitForScene(SceneNames.Carcel, 4f);

            Assert.AreEqual(SceneNames.Carcel, SceneManager.GetActiveScene().name);
            Assert.AreEqual(500_000L - MarketAssumptions.TrespassFine, session.State.Cash);
            Assert.AreEqual(MarketAssumptions.JailDaysLost, session.Loop.TasksDone);
        }
    }

    /// <summary>The street: the new doors, see-through glass, the car that runs the player over and the collectors.</summary>
    public sealed class StreetIncidentsPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        [UnityTest]
        public IEnumerator Town_HasTheDoorsOfTheNewBuildings()
        {
            yield return null;

            var doors = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Select(d => d.TargetScene).ToList();
            foreach (string scene in new[] { SceneNames.Agencia, SceneNames.Carcel, SceneNames.Hospital, SceneNames.Vecino })
            {
                CollectionAssert.Contains(doors, scene);
            }

            foreach (var buildingName in new[] { "Comisaria", "Hospital" })
            {
                Assert.AreEqual(1, Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == buildingName), buildingName);
            }
        }

        [UnityTest]
        public IEnumerator NewBuildings_StandOnClearGroundAwayFromEveryOtherBuilding()
        {
            yield return null;

            var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var buildings = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
                .Where(c => !c.isTrigger && c.transform.Find("Roof") != null)
                .ToArray();

            foreach (var name in new[] { "Comisaria", "Hospital" })
            {
                var building = buildings.First(c => c.name == name);
                foreach (var other in buildings.Where(c => c != building))
                {
                    Assert.IsFalse(building.bounds.Intersects(other.bounds), name + " overlaps " + other.name);
                }

                var area = building.bounds;
                foreach (var item in all.Where(t => t.name == "Rock" || t.name == "Tree" || t.name == "Bush"))
                {
                    Assert.IsFalse(area.Contains(item.position), name + " has a " + item.name + " inside it at " + item.position);
                }
            }
        }

        [UnityTest]
        public IEnumerator Cars_MovingOnesShowADriverAndParkedOnesAreEmpty()
        {
            yield return null;

            var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var driving = all.Where(t => t.name == "Car" && t.GetComponent<TrafficCar>() != null).ToArray();
            var parked = all.Where(t => t.name == "Car" && t.GetComponent<TrafficCar>() == null).ToArray();

            Assert.IsNotEmpty(driving);
            Assert.IsNotEmpty(parked);
            foreach (var car in driving)
            {
                Assert.IsNotNull(car.Find("DriverHead"), "somebody is driving");
            }

            foreach (var car in parked)
            {
                Assert.IsNull(car.Find("DriverHead"), "nobody in a parked car");
                Assert.IsNotNull(car.Find("Seat"), "the empty seat shows through the glass");
            }

            var collector = GameObject.Find("CollectorCar");
            Assert.IsNotNull(collector);
            Assert.IsNull(collector.transform.Find("DriverHead"), "tinted glass: nobody can see in");
        }

        [UnityTest]
        public IEnumerator Cars_ARunOverPlayerGoesToTheHospitalOnceAMonth()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.State.Cash = 600_000L;
            yield return null;

            var car = Object.FindObjectsByType<TrafficCar>(FindObjectsSortMode.None).First(c => c.name == "Car");
            Teleport(car.transform.position);

            yield return WaitForScene(SceneNames.Hospital, 4f);

            Assert.AreEqual(SceneNames.Hospital, SceneManager.GetActiveScene().name);
            Assert.IsTrue(session.Loop.HurtThisMonth);
            Assert.AreEqual(600_000L - MarketAssumptions.HospitalBill, session.State.Cash);
            Assert.AreEqual(MarketAssumptions.HospitalDaysLost, session.Loop.TasksDone);
        }

        [UnityTest]
        public IEnumerator Collectors_StopInFrontOfAPlayerWhoOwesAndAskForMoney()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.State.InformalLoans.Add(new InformalLoan("Prestamista", 100_000L, 15m));
            session.State.Cash = 300_000L;
            yield return null;
            Assert.IsTrue(session.InArrears);

            var car = CollectorCar.Instance;
            Assert.IsNotNull(car);
            Teleport((Vector2)car.transform.position + new Vector2(1.5f, 2.0f));
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(car.IsStopped);
            var modal = Ui.Find("Modal");
            Assert.IsTrue(modal.gameObject.activeSelf, "the collectors' window is open");

            long debt = FinanceEngine.DebtOwed(session.State);
            session.PayCollectors();
            Assert.Less(FinanceEngine.DebtOwed(session.State), debt);
        }

        [UnityTest]
        public IEnumerator Collectors_LeaveAPlayerWhoOwesNothingAlone()
        {
            var session = PlaceTestSetup.StartOfflineRun();
            session.State.InformalLoans.Clear();
            yield return null;
            Assert.IsFalse(session.InArrears);

            var car = CollectorCar.Instance;
            Teleport((Vector2)car.transform.position + new Vector2(1.5f, 2.0f));
            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(car.IsStopped);
        }
    }
}

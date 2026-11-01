using System.Collections;
using System.Linq;
using System.Reflection;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Starts an offline run inside an interior and opens a station's window as pressing E would.</summary>
    internal static class InteriorUi
    {
        public static Transform Root => GameObject.Find("GameUi").transform;

        public static bool ModalOpen => Root.Find("Modal").gameObject.activeSelf;

        public static string ModalText => string.Join("\n", Root.Find("Modal").GetComponentsInChildren<Text>().Select(t => t.text));

        public static IEnumerator StartOfflineRun()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            GameSession.Instance.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;
        }

        public static void InteractNow(Station station)
        {
            Call("InteractNow", station);
        }

        /// <summary>Plays the station's scene first, then opens its window: the whole press of E.</summary>
        public static void Interact(Station station)
        {
            Call("Interact", station);
        }

        private static void Call(string method, Station station)
        {
            var ui = Object.FindFirstObjectByType<GameUi>();
            typeof(GameUi).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, new object[] { station });
        }
    }

    /// <summary>Plays the Banco interior: walls, furniture, the station, the vendor and the way back out.</summary>
    public sealed class BancoPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Banco;

        [UnityTest]
        public IEnumerator Interior_HasExactlyOneGlobalLight()
        {
            yield return null;
            var lightGo = GameObject.Find("InteriorGlobalLight");
            Assert.IsNotNull(lightGo, "Interior scene must have InteriorGlobalLight");
            var lightComp = lightGo.GetComponent("Light2D");
            Assert.IsNotNull(lightComp, "InteriorGlobalLight must have Light2D component");
        }

        [UnityTest]
        public IEnumerator Player_AppearsInFrontOfTheDoormat()
        {
            yield return null;

            Assert.AreEqual(0f, Body.position.x, 0.5f);
            Assert.AreEqual(MapLayout.BancoMat.y + 1.3f, Body.position.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator Player_IsStoppedByTheRoomWall()
        {
            yield return Walk(new Vector2(-8f, -0.4f), Vector2.left, 3f);

            Assert.Greater(Body.position.x, -MapLayout.BancoSize.x * 0.5f);
        }

        [UnityTest]
        public IEnumerator Player_IsBlockedByTheAtm()
        {
            yield return Walk(new Vector2(9f, -2.6f), Vector2.right, 1.5f);

            // The ATMs stand against the right wall; their body starts at x = 10.9.
            Assert.Less(Body.position.x, 10.9f);
        }

        [UnityTest]
        public IEnumerator Player_CannotGetBehindTheTellerCounter()
        {
            yield return Walk(new Vector2(0f, 3.0f), Vector2.up, 1.5f);

            // The counter runs from wall to wall of the staff area, so the lobby ends at its front.
            Assert.Less(Body.position.y, 3.7f);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksIn()
        {
            var station = GameObject.Find("Station_Bank").GetComponent<Station>();
            Station entered = null;
            station.PlayerEntered += s => entered = s;

            Teleport(station.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, entered);
            Assert.IsTrue(station.IsPlayerInside);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksOut()
        {
            var station = GameObject.Find("Station_Bank").GetComponent<Station>();
            Station exited = null;
            station.PlayerExited += s => exited = s;

            Teleport(station.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Teleport(new Vector2(0f, -1.5f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, exited);
            Assert.IsFalse(station.IsPlayerInside);
        }

        [UnityTest]
        public IEnumerator Cashier_WorksBehindTheCounterAtTheOpenWindow()
        {
            var cashier = GameObject.Find("Cajera").transform;
            var counter = GameObject.Find("TellerCounter").transform;
            var station = GameObject.Find("Station_Bank").transform;
            yield return null;

            Assert.Greater(cashier.position.y, counter.position.y, "the cashier stands on the staff side");
            Assert.AreEqual(cashier.position.x, station.position.x, 0.01f, "the month is closed at the cashier's window");
            Assert.Less(station.position.y, counter.position.y, "the customer side is in front of the counter");
        }

        [UnityTest]
        public IEnumerator FarStars_MoveLessThanTheCamera()
        {
            yield return new WaitForSeconds(0.5f);

            var farStars = GameObject.Find("Stars_Far").transform;

            // Factor 0.88: the layer follows the camera by 88 percent of its position.
            Assert.AreEqual(Cam.transform.position.x * 0.88f, farStars.position.x, 0.05f);
            Assert.AreEqual(Cam.transform.position.y * 0.88f, farStars.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator Atm_OpensTheWithdrawal()
        {
            yield return InteriorUi.StartOfflineRun();
            var atm = Object.FindObjectsByType<Station>(FindObjectsSortMode.None).First(s => s.Type == StationType.Atm);

            InteriorUi.InteractNow(atm);
            yield return null;

            Assert.IsTrue(InteriorUi.ModalOpen, "the cash machine opens its window");
            StringAssert.Contains("Retiro de efectivo", InteriorUi.ModalText);
        }

        [UnityTest]
        public IEnumerator Window4_OpensTheDeposit()
        {
            yield return InteriorUi.StartOfflineRun();
            var window = GameObject.Find("Thing_Teller3").GetComponent<Station>();

            InteriorUi.InteractNow(window);
            yield return null;

            Assert.IsTrue(InteriorUi.ModalOpen, "the deposit window opens");
            StringAssert.Contains("Caja 4", InteriorUi.ModalText);
            StringAssert.Contains("Depositar", InteriorUi.ModalText);
        }

        [UnityTest]
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheBank()
        {
            Teleport(MapLayout.BancoMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name);
            Assert.AreEqual(-8.5f, Body.position.x, 0.6f);
            Assert.AreEqual(4.3f, Body.position.y, 0.6f);
        }
    }

    /// <summary>The other two interiors have a vendor of their own and a door back to the town.</summary>
    public sealed class TiendaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Tienda;

        [UnityTest]
        public IEnumerator Vendor_StandsBehindTheCounter()
        {
            var vendor = GameObject.Find("Vendedor");
            var counter = GameObject.Find("Counter");
            yield return null;

            Assert.IsNotNull(vendor);
            Assert.Greater(vendor.transform.position.y, counter.transform.position.y);
        }

        [UnityTest]
        public IEnumerator Vendor_TalksOnlyWhenThePlayerIsClose()
        {
            var vendor = GameObject.Find("Vendedor").GetComponentInChildren<PlayerAnimator>();
            Assert.IsTrue(vendor.HasRig, "the vendor is a person with moving parts");

            Teleport(new Vector2(-6f, -3f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(vendor.Talking);

            Teleport((Vector2)vendor.transform.position + new Vector2(0f, -1.5f));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(vendor.Talking);
        }

        [UnityTest]
        public IEnumerator Vendor_GreetsThePlayerWhoComesClose()
        {
            var vendor = GameObject.Find("Vendedor").GetComponentInChildren<PlayerAnimator>();

            Teleport((Vector2)vendor.transform.position + new Vector2(0f, -1.5f));
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(vendor.IsWaving);
        }

        [UnityTest]
        public IEnumerator Station_FiresWhenThePlayerWalksIn()
        {
            var station = GameObject.Find("Station_Store").GetComponent<Station>();
            Station entered = null;
            station.PlayerEntered += s => entered = s;

            Teleport(station.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreSame(station, entered);
        }

        [UnityTest]
        public IEnumerator Shopkeeper_GoesHomeAtNightAndComesBackInTheMorning()
        {
            yield return InteriorUi.StartOfflineRun();
            var vendor = GameObject.Find("Vendedor").GetComponent<NightShift>();
            Assert.IsNotNull(vendor, "the shopkeeper closes at night");
            var clock = GameSession.Instance.Loop.Clock;
            for (int hour = 0; hour < 30 && !clock.IsNight; hour++)
            {
                clock.Advance(GameRules.SecondsPerGameHour, GameRules.SecondsPerGameHour);
            }

            Assert.IsTrue(clock.IsNight);
            for (float waited = 0f; waited < 4f && !vendor.Gone; waited += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(vendor.Gone, "walked off and is gone");
            Assert.IsFalse(vendor.GetComponent<Collider2D>().enabled, "nobody to bump into");

            GameSession.Instance.StartNewGame();
            yield return null;
            yield return null;
            Assert.IsFalse(vendor.Gone, "back at the post in the morning");
        }

        [UnityTest]
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheShop()
        {
            Teleport(MapLayout.TiendaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(8.5f, Body.position.x, 0.6f);
            Assert.AreEqual(4.3f, Body.position.y, 0.6f);
        }
    }

    public sealed class CasaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Casa;

        [UnityTest]
        public IEnumerator Player_IsBlockedByTheBedroomWall()
        {
            yield return Walk(new Vector2(-6.5f, -0.3f), Vector2.up, 1.5f);

            // The wall between the living room and the bedroom stands at y = 0.8.
            Assert.Less(Body.position.y, 0.8f);
        }

        [UnityTest]
        public IEnumerator Player_EntersTheBedroomThroughItsDoor()
        {
            yield return Walk(new Vector2(-2.9f, -0.3f), Vector2.up, 1.5f);

            Assert.Greater(Body.position.y, 1.3f, "the doorway lets the player through");
        }

        [UnityTest]
        public IEnumerator House_HasEveryRoom()
        {
            yield return null;

            foreach (var name in new[] { "Bed", "Toilet", "Shower", "Fridge", "Sofa", "DiningTable", "BillsDesk" })
            {
                Assert.IsNotNull(GameObject.Find(name), name);
            }
        }

        [UnityTest]
        public IEnumerator Wardrobe_RecolorsTheBodyAndTheClothesStayAcrossScenes([Values(false, true)] bool female)
        {
            PlayerProfile.Set("Alex", female);
            try
            {
                yield return SceneManager.LoadSceneAsync(SceneNames.Casa);
                yield return null;
                FindPlayer();
                yield return InteriorUi.StartOfflineRun();
                var shirt = GamePalette.Hex("#F8F5E8");

                Assert.IsTrue(GameSession.Instance.Loop.Wear(PlayerOutfit.Sunday, PlayerOutfit.Swimsuit));
                PlayerOutfit.Wear(Body.gameObject, PlayerOutfit.Sunday);
                Assert.Less(Distance(shirt, TorsoColor()), 0.03f, "the Sunday shirt is on");

                yield return SceneManager.LoadSceneAsync(SceneNames.Banco);
                yield return null;
                yield return null;
                FindPlayer();
                Assert.Less(Distance(shirt, TorsoColor()), 0.03f, "the clothes come along to the next scene");
            }
            finally
            {
                GameSession.Instance.Loop.Wear(PlayerOutfit.Everyday, PlayerOutfit.Swimsuit);
                PlayerProfile.Reset();
            }
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private Color TorsoColor()
        {
            return Body.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Torso").color;
        }

        /// <summary>
        /// Plays the scene of a piece of furniture from where the player would press E, and measures the longest move of a
        /// single frame: walking is a few hundredths, a jump to the seat is far more.
        /// </summary>
        [UnityTest]
        public IEnumerator Furniture_ScenesWalkToTheSeatWithoutJumping(
            [Values("Thing_Sofa", "Thing_Tv", "Thing_DiningTable")] string thing)
        {
            yield return InteriorUi.StartOfflineRun();
            var station = GameObject.Find(thing).GetComponent<Station>();
            var starts = new System.Collections.Generic.Dictionary<string, Vector2>
            {
                { "Thing_Sofa", new Vector2(-3.2f, -1.8f) },
                { "Thing_Tv", new Vector2(-6.9f, -0.8f) },
                { "Thing_DiningTable", new Vector2(5.0f, -4.3f) }
            };
            Teleport(starts[thing]);
            yield return new WaitForFixedUpdate();

            InteriorUi.Interact(station);
            float longest = 0f;
            Vector2 last = Body.position;
            for (float waited = 0f; waited < 12f && Interaction.Playing; waited += Time.deltaTime)
            {
                yield return null;
                longest = Mathf.Max(longest, Vector2.Distance(last, Body.position));
                last = Body.position;
            }

            Assert.Less(longest, 0.5f, thing + " moves the player in one jump");
        }

        [UnityTest]
        public IEnumerator HouseForRent_IsNotUsableUntilItIsRentedAndHasNoMother()
        {
            HouseVisit.Enter("casa_vecina_d");
            string heard = null;
            System.Action<string> listener = text => heard = text;
            Thoughts.Said += listener;
            try
            {
                yield return SceneManager.LoadSceneAsync(SceneNames.Casa);
                yield return null;
                FindPlayer();
                yield return InteriorUi.StartOfflineRun();

                Assert.IsNull(GameObject.Find("Mama"), "the mother lives in her own house");
                InteriorUi.Interact(GameObject.Find("Thing_Shower").GetComponent<Station>());
                yield return null;
                Assert.IsFalse(Interaction.Playing, "no shower in a house that is not the player's");
                Assert.AreEqual(HouseVisit.NotMineThought, heard);
            }
            finally
            {
                Thoughts.Said -= listener;
                HouseVisit.Enter(string.Empty);
            }
        }

        [UnityTest]
        public IEnumerator MothersHouse_KeepsTheMotherAndItsThingsWork()
        {
            HouseVisit.Enter(string.Empty);
            yield return InteriorUi.StartOfflineRun();

            Assert.IsNotNull(GameObject.Find("Mama"));
            InteriorUi.Interact(GameObject.Find("Thing_Sink").GetComponent<Station>());
            yield return null;
            Assert.IsTrue(Interaction.Playing || InteriorUi.ModalOpen, "the sink works in the mother's house");

            Interaction.SkipCurrent();
            for (float waited = 0f; waited < 5f && Interaction.Playing; waited += Time.deltaTime)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Scene_ThatIsCutByASceneChangeDoesNotLeaveTheGameBusy()
        {
            yield return InteriorUi.StartOfflineRun();
            InteriorUi.Interact(GameObject.Find("Thing_Sink").GetComponent<Station>());
            yield return null;
            Assert.IsTrue(Interaction.Playing);

            yield return SceneManager.LoadSceneAsync(SceneNames.Casa);
            yield return null;
            Assert.IsFalse(Interaction.Playing, "the prompts and the phone work again in the new scene");
            Assert.IsFalse(PlayerController.Frozen);
        }

        [UnityTest]
        public IEnumerator Shower_IsOfferedInTheBathroomButNotThroughTheKitchenWall()
        {
            yield return InteriorUi.StartOfflineRun();
            var prompt = InteriorUi.Root.Find("Prompt").gameObject;

            Teleport(new Vector2(0.8f, 3.2f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(prompt.activeSelf, "on the bath mat");
            StringAssert.Contains("Bañarte", prompt.GetComponentInChildren<Text>().text);

            // The kitchen side of the wall at x = 1.6, still inside the zone of the shower.
            Teleport(new Vector2(2.0f, 3.6f));
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(prompt.activeSelf && prompt.GetComponentInChildren<Text>().text.Contains("Bañarte"),
                "the shower cannot be used from the kitchen");
        }

        [UnityTest]
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheHouse()
        {
            Teleport(MapLayout.CasaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(-19.5f, Body.position.x, 0.6f);
            Assert.AreEqual(4.3f, Body.position.y, 0.6f);
        }
    }

    /// <summary>The savings cooperative: the advisor's counter with the task inside, and the way back to its door.</summary>
    public sealed class CooperativaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Cooperativa;

        [UnityTest]
        public IEnumerator Cooperative_HasTheAdvisorTheTaskAndTheTermSavings()
        {
            yield return null;

            var station = GameObject.Find("Station_Coop").GetComponent<Station>();
            Assert.AreEqual(StationType.Coop, station.Type);
            Assert.Greater(GameObject.Find("AsesoraCoop").transform.position.y, station.transform.position.y, "the advisor is behind the counter");
            Assert.IsNotNull(GameObject.Find("Thing_TermSavings"));
            Assert.AreEqual(0f, Body.position.x, 0.5f);
            Assert.AreEqual(MapLayout.CooperativaMat.y + 1.3f, Body.position.y, 0.5f, "the player comes in on the mat");
        }

        [UnityTest]
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheCooperative()
        {
            Teleport(MapLayout.CooperativaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name);
            Assert.AreEqual(-31.5f, Body.position.x, 0.6f);
            Assert.AreEqual(4.3f, Body.position.y, 0.6f);
        }
    }

    /// <summary>Plays the Soda El Muelle interior: diner tables, counter with Doña Lucía, kitchen and time clock.</summary>
    public sealed class SodaPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Soda;

        [UnityTest]
        public IEnumerator Player_AppearsInFrontOfTheDoormat()
        {
            yield return null;

            Assert.AreEqual(0f, Body.position.x, 0.5f);
            Assert.AreEqual(MapLayout.SodaMat.y + 1.3f, Body.position.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator DonaLucia_WorksBehindTheCounter()
        {
            var lucia = GameObject.Find("DonaLucia");
            var counter = GameObject.Find("DinerCounter");
            yield return null;

            Assert.IsNotNull(lucia);
            Assert.IsNotNull(counter);
            Assert.Greater(lucia.transform.position.y, counter.transform.position.y);
        }

        [UnityTest]
        public IEnumerator Soda_HasWorkClockAndCustomerStation()
        {
            yield return null;

            Assert.IsNotNull(GameObject.Find("Station_Soda"), "soda customer station exists");
            Assert.IsNotNull(GameObject.Find("Station_Work"), "employee time punch clock station exists");
            Assert.IsNotNull(GameObject.Find("TimeClock"), "wall time clock visual exists");
        }

        [UnityTest]
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheSoda()
        {
            Teleport(MapLayout.SodaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name);
            Assert.AreEqual(12.5f, Body.position.x, 0.6f);
            Assert.AreEqual(-25.5f, Body.position.y, 0.6f);
        }
    }
}

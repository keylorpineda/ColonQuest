using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the Banco interior: walls, furniture, the station, the vendor and the way back out.</summary>
    public sealed class BancoPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Banco;

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
        public IEnumerator Doormat_LeadsBackToTheTownInFrontOfTheHouse()
        {
            Teleport(MapLayout.CasaMat);
            yield return WaitForScene(SceneNames.Town, 3f);
            FindPlayer();

            Assert.AreEqual(-19.5f, Body.position.x, 0.6f);
            Assert.AreEqual(4.3f, Body.position.y, 0.6f);
        }
    }
}

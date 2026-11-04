using System.Collections;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>The rented room on its real scene: what is there at the start and what the store adds to it.</summary>
    public sealed class RoomPlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static GameSession Session => GameSession.Instance;

        private IEnumerator StartOfflineRun()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            Session.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;
        }

        private static IEnumerator LoadRoom()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Cuarto);
            yield return null;
        }

        private static GameObject Piece(string name)
        {
            return GameObject.Find(name);
        }

        [UnityTest]
        public IEnumerator Town_HasTheDoorOfTheRoom()
        {
            yield return null;
            var door = GameObject.Find("Door_" + SceneNames.Cuarto).GetComponent<Door>();
            Assert.AreEqual(SceneNames.Cuarto, door.TargetScene);
            Assert.IsNull(GameObject.Find("Door_Casa_Vecina_C"), "the yellow house is the one with rooms now");
        }

        [UnityTest]
        public IEnumerator Room_StartsBare()
        {
            yield return StartOfflineRun();
            yield return LoadRoom();

            Assert.IsNotNull(Piece("Mattress"));
            Assert.IsNotNull(Piece("Thing_Bed"), "the user can sleep on the mattress");
            Assert.IsNotNull(Piece("Station_Home"), "the papers of the month are in the room");
            Assert.IsNotNull(Piece("PlasticChair"));
            Assert.IsNull(Piece("Bed"));
            Assert.IsNull(Piece("Thing_Fridge"));
            Assert.IsNull(Piece("Thing_Tv"));
            Assert.IsNull(Piece("Thing_DiningTable"));
        }

        [UnityTest]
        public IEnumerator Room_TheNapIsOnTheBedNotOnTheFloor()
        {
            yield return StartOfflineRun();
            yield return LoadRoom();
            FindPlayer();
            var ui = Object.FindFirstObjectByType<UI.GameUi>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var modal = (UI.ModalPanel)typeof(UI.GameUi).GetField("modal", flags).GetValue(ui);
            Vector2 bed = Piece("Thing_Bed").transform.position;

            typeof(UI.GameUi).GetMethod("ShowBedChoices", flags).Invoke(ui, new object[] { bed });
            yield return null;
            if (modal.IsRevealing)
            {
                modal.Press(0);
                yield return null;
            }

            modal.Press(0);
            yield return null;
            Assert.IsTrue(Interaction.Playing, "the nap has started");

            float closest = float.MaxValue;
            for (float waited = 0f; Interaction.Playing && waited < 15f && !modal.IsOpen; waited += Time.unscaledDeltaTime)
            {
                closest = Mathf.Min(closest, Vector2.Distance(Body.position, bed));
                yield return null;
            }

            Assert.Less(closest, 0.3f, "the user lies down on the bed");
        }

        [UnityTest]
        public IEnumerator Room_ShowsWhatWasBought()
        {
            yield return StartOfflineRun();
            Session.Loop.Holdings.AddFurniture(FurnitureKind.Bed);
            Session.Loop.Holdings.AddFurniture(FurnitureKind.Tv);
            yield return LoadRoom();

            Assert.IsNull(Piece("Mattress"), "the bed takes its place");
            Assert.IsNotNull(Piece("Bed"));
            Assert.IsNotNull(Piece("Thing_Bed"));
            Assert.IsNotNull(Piece("Thing_Tv"));
            Assert.IsNull(Piece("Thing_Fridge"));
        }

        [UnityTest]
        public IEnumerator Store_HasTheFurnitureCorner()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Tienda);
            yield return null;
            Assert.AreEqual(StationType.Furniture, Piece("Station_Furniture").GetComponent<Station>().Type);
        }

        [UnityTest]
        public IEnumerator Negotiation_BuysThePieceInInstallments()
        {
            yield return StartOfflineRun();
            FurnitureModel table = null;
            foreach (var item in MarketAssumptions.Furniture)
            {
                if (item.Kind == FurnitureKind.Table)
                {
                    table = item;
                }
            }

            int installments = Session.State.Installments.Count;
            Session.BeginFurnitureNegotiation(table);
            Assert.IsNotNull(Session.CurrentNegotiation);
            Session.AcceptDeal();
            yield return null;

            Assert.IsNull(Session.CurrentNegotiation);
            Assert.IsNull(Session.CurrentFurniture);
            Assert.IsTrue(Session.Holdings.Has(FurnitureKind.Table));
            Assert.AreEqual(installments + 1, Session.State.Installments.Count);
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>The station: the cell gate keeps the prisoner in while the sentence lasts, and the judge has an office.</summary>
    public sealed class JailPlayTests : PlayTestBase
    {
        private static readonly Vector2 InsideTheCell = new Vector2(-7.6f, 2.2f);
        private const float CellFrontY = 0.9f;

        protected override string SceneToLoad => SceneNames.Carcel;

        [UnityTearDown]
        public IEnumerator OpenTheGate()
        {
            CellGate.Unlock(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prisoner_CannotWalkOutWhileTheGateIsLocked()
        {
            CellGate.Lock(false);
            yield return Walk(InsideTheCell, Vector2.down, 1.5f);

            Assert.Greater(Body.position.y, CellFrontY, "the gate stops the player inside the cell");
        }

        [UnityTest]
        public IEnumerator Prisoner_WalksOutOnceTheGateOpens()
        {
            CellGate.Lock(false);
            yield return null;
            CellGate.Unlock(true);
            yield return new WaitForSeconds(0.6f);
            yield return Walk(InsideTheCell, Vector2.down, 1.5f);

            Assert.Less(Body.position.y, CellFrontY, "with the gate open the player leaves the cell");
        }

        [UnityTest]
        public IEnumerator Judge_SitsInAnOfficeWithADoorFromTheHall()
        {
            yield return null;

            var judge = GameObject.Find("Juez");
            Assert.IsNotNull(judge);
            Assert.Greater(judge.transform.position.x, 4.2f, "the judge is behind the office wall");

            // Walking in from the hall through the door reaches the judge's desk area.
            yield return Walk(new Vector2(2.5f, -2.75f), Vector2.right, 1.2f);
            Assert.Greater(Body.position.x, 4.6f, "the door of the office is open");
        }
    }
}

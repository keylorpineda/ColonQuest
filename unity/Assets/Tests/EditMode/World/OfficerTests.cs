using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class OfficerTests
    {
        [SetUp]
        public void NoOffense()
        {
            Offenses.Reset();
        }

        [Test]
        public void NoOffense_TheOfficerDoesNothingEvenRightNextToThePlayer()
        {
            Assert.AreEqual(OfficerAction.Idle, OfficerLogic.Decide(Vector2.zero, new Vector2(0.5f, 0f), OffenseKind.None, true));
            Assert.AreEqual(OfficerAction.Idle, OfficerLogic.Decide(Vector2.zero, new Vector2(0.5f, 0f), OffenseKind.None, false));
        }

        [Test]
        public void Offense_AnOfficerWhoSeesThePlayerGoesAfterHim()
        {
            Assert.AreEqual(OfficerAction.Pursue, OfficerLogic.Decide(Vector2.zero, new Vector2(4f, 0f), OffenseKind.Trespass, false));
        }

        [Test]
        public void Offense_AnOfficerWhoDoesNotSeeThePlayerDoesNothing()
        {
            Assert.AreEqual(OfficerAction.Idle, OfficerLogic.Decide(Vector2.zero, new Vector2(OfficerLogic.SightRange + 1f, 0f), OffenseKind.Trespass, false));
        }

        [Test]
        public void Offense_AnOfficerWhoWasCalledComesFromAnywhere()
        {
            Assert.AreEqual(OfficerAction.Pursue, OfficerLogic.Decide(Vector2.zero, new Vector2(40f, 0f), OffenseKind.Trespass, true));
        }

        [Test]
        public void Offense_WithinReachTheOfficerStopsThePlayer()
        {
            Assert.AreEqual(OfficerAction.Stop, OfficerLogic.Decide(Vector2.zero, new Vector2(OfficerLogic.StopRange, 0f), OffenseKind.Trespass, false));
            Assert.AreEqual(OfficerAction.Pursue, OfficerLogic.Decide(Vector2.zero, new Vector2(OfficerLogic.StopRange + 0.2f, 0f), OffenseKind.Trespass, false));
        }

        [Test]
        public void Offenses_RaiseAndClear()
        {
            Assert.AreEqual(OffenseKind.None, Offenses.Current);
            Offenses.Raise(OffenseKind.Trespass);
            Assert.AreEqual(OffenseKind.Trespass, Offenses.Current);

            Offenses.Clear(OffenseKind.None);
            Assert.AreEqual(OffenseKind.Trespass, Offenses.Current, "clearing a different offense changes nothing");

            Offenses.Clear(OffenseKind.Trespass);
            Assert.AreEqual(OffenseKind.None, Offenses.Current);
        }
    }
}

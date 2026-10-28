using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class IncidentClocksTests
    {
        [Test]
        public void TrespassClock_RunsOutOnceAndOnlyOnce()
        {
            var clock = new TrespassClock(2f);

            Assert.IsFalse(clock.Tick(1f));
            Assert.AreEqual(1f, clock.Remaining, 0.0001f);
            Assert.IsTrue(clock.Tick(1.5f), "the step that crosses zero");
            Assert.IsTrue(clock.Expired);
            Assert.IsFalse(clock.Tick(1f), "nothing happens twice");
            Assert.AreEqual(0f, clock.Remaining);
        }

        [Test]
        public void TrespassClock_IgnoresNegativeTimeAndABadStart()
        {
            var clock = new TrespassClock(1f);
            Assert.IsFalse(clock.Tick(-5f));
            Assert.AreEqual(1f, clock.Remaining, 0.0001f);

            var broken = new TrespassClock(-4f);
            Assert.IsTrue(broken.Tick(0.1f), "no time at all means it ends at once");
        }

        // ---------------------------------------------------------------- collectors' patrol

        [Test]
        public void Patrol_IsNearOnlyInsideTheBox()
        {
            Assert.IsTrue(CollectorPatrol.IsNear(Vector2.zero, new Vector2(3f, 2f)));
            Assert.IsFalse(CollectorPatrol.IsNear(Vector2.zero, new Vector2(3.5f, 0f)));
            Assert.IsFalse(CollectorPatrol.IsNear(Vector2.zero, new Vector2(0f, 3f)));
        }

        [Test]
        public void Patrol_StopsOnlyWhenArmedAndNear()
        {
            var patrol = new CollectorPatrol(25f, 40f);

            Assert.IsFalse(patrol.TryStop(false, Vector2.zero, Vector2.one), "nobody owes, so it just drives by");
            Assert.IsFalse(patrol.TryStop(true, Vector2.zero, new Vector2(20f, 0f)), "too far");
            Assert.AreEqual(CollectorPatrol.Mode.Driving, patrol.Current);

            Assert.IsTrue(patrol.TryStop(true, Vector2.zero, Vector2.one));
            Assert.AreEqual(CollectorPatrol.Mode.Stopped, patrol.Current);
            Assert.IsFalse(patrol.TryStop(true, Vector2.zero, Vector2.one), "already stopped");
        }

        [Test]
        public void Patrol_AfterTheStopItDrivesOnAndDoesNotStopAgainForAWhile()
        {
            var patrol = new CollectorPatrol(25f, 40f);
            patrol.TryStop(true, Vector2.zero, Vector2.one);

            Assert.IsFalse(patrol.Tick(CollectorPatrol.StopSeconds + 0.1f));
            Assert.AreEqual(CollectorPatrol.Mode.Cooling, patrol.Current);
            Assert.IsFalse(patrol.TryStop(true, Vector2.zero, Vector2.one), "cooling down");

            Assert.IsFalse(patrol.Tick(40.1f), "back to driving, but not a new entrance");
            Assert.AreEqual(CollectorPatrol.Mode.Driving, patrol.Current);
        }

        [Test]
        public void Patrol_AfterLeavingTheMapItWaitsThenEntersAgain()
        {
            var patrol = new CollectorPatrol(25f, 40f);
            patrol.LeftTheMap();

            Assert.AreEqual(CollectorPatrol.Mode.Waiting, patrol.Current);
            Assert.IsFalse(patrol.Tick(24f));
            Assert.IsTrue(patrol.Tick(2f), "the step in which it re-enters");
            Assert.AreEqual(CollectorPatrol.Mode.Driving, patrol.Current);
        }

        [Test]
        public void SoundMix_TheNewPlacesHaveTheirOwnSoundscape()
        {
            Assert.AreEqual(MusicTrack.Store, SoundMix.Compute(SceneNames.Agencia, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.Bank, SoundMix.Compute(SceneNames.Carcel, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.House, SoundMix.Compute(SceneNames.Hospital, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.House, SoundMix.Compute(SceneNames.Vecino, Vector2.zero).Track);
        }
    }
}

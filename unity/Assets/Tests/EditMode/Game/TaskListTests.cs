using System.Linq;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class TaskListTests
    {
        private static GameLoop ArrivingLoop()
        {
            var loop = new GameLoop();
            loop.BeginArrivalDay();
            loop.Clock.SkipTo(GameRules.WorkStartHour * 60f);
            return loop;
        }

        [Test]
        public void ArrivalDay_TheFirstShiftWaitsForTheRoomAndTheInsurance()
        {
            var loop = ArrivingLoop();
            Assert.AreEqual(ClockInResult.NotReady, loop.ClockIn());
            Assert.IsFalse(loop.Job.WorkedToday, "nothing is counted while not ready");

            loop.MarkRoomVisited();
            Assert.AreEqual(ClockInResult.NotReady, loop.ClockIn(), "the insurance is still missing");

            Assert.IsTrue(loop.BuyHealthInsurance());
            Assert.AreEqual(ClockInResult.OnTime, loop.ClockIn());
        }

        [Test]
        public void ArrivalDay_TheFirstShiftIsNeverLate()
        {
            var loop = ArrivingLoop();
            loop.MarkRoomVisited();
            loop.BuyHealthInsurance();
            loop.Clock.SkipTo((GameRules.WorkStartHour + 3) * 60f);
            Assert.AreEqual(ClockInResult.OnTime, loop.ClockIn(), "getting ready took the morning, and that is fine");
            Assert.IsFalse(loop.ArrivalDay, "after the first shift the day of arrival is over");
        }

        [Test]
        public void ArrivalDay_ARunMadeAnyOtherWayIsNotAsked()
        {
            var loop = new GameLoop();
            Assert.IsTrue(loop.ReadyForFirstShift);
            Assert.AreNotEqual(ClockInResult.NotReady, loop.ClockIn());
        }

        [Test]
        public void ArrivalDay_IsKeptInTheSavedRun()
        {
            var loop = ArrivingLoop();
            loop.MarkRoomVisited();
            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());
            Assert.IsTrue(restored.ArrivalDay);
            Assert.IsTrue(restored.VisitedRoom);
        }

        [Test]
        public void List_OnTheDayOfArrivalShowsTheThreeStepsAndTicksThem()
        {
            var loop = ArrivingLoop();
            var items = TaskList.For(loop);
            CollectionAssert.AreEqual(new[] { TaskList.VisitRoom, TaskList.TakeInsurance, TaskList.FirstShift }, items.Select(i => i.Text));
            Assert.IsTrue(items.All(i => !i.Done));

            loop.MarkRoomVisited();
            Assert.IsTrue(TaskList.For(loop)[0].Done);
        }

        [Test]
        public void List_AfterTheFirstShiftShowsTheWorkOfTheDay()
        {
            var loop = new GameLoop();
            var items = TaskList.For(loop);
            Assert.AreEqual(TaskList.Work, items[0].Text);
            Assert.IsFalse(items[0].Done);

            loop.ClockIn();
            Assert.IsFalse(TaskList.For(loop)[0].Done, "still at work");
            loop.FinishShift();
            Assert.IsTrue(TaskList.For(loop)[0].Done);
            Assert.AreEqual(TaskList.Sleep, TaskList.For(loop).Last().Text);
        }
    }
}

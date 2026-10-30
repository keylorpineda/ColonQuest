using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class CareerTests
    {
        private static void Afternoon(GameLoop loop)
        {
            loop.Clock.SkipTo(13f * 60f);
        }

        private static void WorkOnTime(GameLoop loop, int days)
        {
            for (int i = 0; i < days; i++)
            {
                loop.Clock.StartMonth();
                loop.ClockIn();
                loop.FinishShift();
                loop.Job.EndDay();
            }
        }

        private static void FinishCourse(GameLoop loop, CourseKind kind)
        {
            for (int i = 0; i < Career.Course(kind).Afternoons; i++)
            {
                loop.Courses.AttendedToday = false;
                loop.Clock.StartMonth();
                Afternoon(loop);
                Assert.AreEqual(ClassRefusal.None, loop.CheckClass(kind));
                loop.TakeClass(kind);
            }
        }

        [Test]
        public void Salaries_GrowUpTheLadderFromTheLegalMinimum()
        {
            Assert.AreEqual(GameRules.MinimumWageGross, Career.GrossSalary(JobPosition.Helper));
            Assert.Greater(Career.GrossSalary(JobPosition.Cashier), Career.GrossSalary(JobPosition.Helper));
            Assert.Greater(Career.GrossSalary(JobPosition.Manager), Career.GrossSalary(JobPosition.Cashier));
        }

        [Test]
        public void Class_IsOnlyInTheAfternoon()
        {
            var loop = new GameLoop();
            Assert.AreEqual(ClassRefusal.OutsideTheAfternoon, loop.CheckClass(CourseKind.Ina));

            Afternoon(loop);
            Assert.AreEqual(ClassRefusal.None, loop.CheckClass(CourseKind.Ina));

            loop.Clock.SkipTo(18f * 60f);
            Assert.AreEqual(ClassRefusal.OutsideTheAfternoon, loop.CheckClass(CourseKind.Ina));
        }

        [Test]
        public void Class_ChargesOnlyWhenEnrollingAndTakesTheAfternoon()
        {
            var loop = new GameLoop();
            Afternoon(loop);
            long cash = loop.State.Cash;
            float minutes = loop.Clock.Minutes;

            Assert.IsTrue(loop.TakeClass(CourseKind.Ina));
            Assert.AreEqual(cash - Career.Course(CourseKind.Ina).Price, loop.State.Cash);
            Assert.AreEqual(minutes + Career.ClassMinutes, loop.Clock.Minutes, 0.01f);
            Assert.AreEqual(ClassRefusal.AlreadyToday, loop.CheckClass(CourseKind.Ina));

            loop.Sleep();
            Afternoon(loop);
            long afterFirst = loop.State.Cash;
            Assert.IsTrue(loop.TakeClass(CourseKind.Ina));
            Assert.AreEqual(afterFirst, loop.State.Cash, "the second afternoon is already paid");
            Assert.IsTrue(loop.Courses.Done(CourseKind.Ina));
            Assert.AreEqual(ClassRefusal.AlreadyDone, loop.CheckClass(CourseKind.Ina));
        }

        [Test]
        public void Class_WithoutTheCashDoesNotEnroll()
        {
            var loop = new GameLoop();
            Afternoon(loop);
            loop.State.Cash = 100L;

            Assert.AreEqual(ClassRefusal.NotEnoughCash, loop.CheckClass(CourseKind.Una));
            Assert.IsFalse(loop.TakeClass(CourseKind.Una));
            Assert.AreEqual(100L, loop.State.Cash);
            Assert.IsFalse(loop.Courses.Enrolled(CourseKind.Una));
        }

        [Test]
        public void Promotion_NeedsTheDaysAndTheCourse()
        {
            var loop = new GameLoop();
            Assert.IsFalse(loop.CanBePromoted);

            WorkOnTime(loop, Career.PunctualDaysFor(JobPosition.Cashier));
            Assert.IsFalse(loop.CanBePromoted, "the days alone are not enough");

            var second = new GameLoop();
            FinishCourse(second, CourseKind.Ina);
            Assert.IsFalse(second.CanBePromoted, "the course alone is not enough");

            FinishCourse(loop, CourseKind.Ina);
            Assert.IsTrue(loop.CanBePromoted);
        }

        [Test]
        public void LateDays_DoNotCountForThePromotion()
        {
            var loop = new GameLoop();
            loop.Clock.SkipTo(8f * 60f);
            loop.ClockIn();
            Assert.AreEqual(1, loop.Job.DaysWorked);
            Assert.AreEqual(0, loop.Job.PunctualDays);
        }

        [Test]
        public void Promotion_RaisesTheNetSalaryThroughTheEngine()
        {
            var loop = new GameLoop();
            WorkOnTime(loop, 3);
            FinishCourse(loop, CourseKind.Ina);

            Assert.IsTrue(loop.Promote());

            Assert.AreEqual(JobPosition.Cashier, loop.Job.Position);
            Assert.AreEqual(FinanceEngine.CalculateNetSalary(Career.GrossSalary(JobPosition.Cashier)), loop.State.SalaryNet);
            Assert.IsFalse(loop.CanBePromoted, "the next one asks for more");
            Assert.IsFalse(loop.Promote());
        }

        [Test]
        public void Promotion_StopsAtTheTop()
        {
            var loop = new GameLoop();
            WorkOnTime(loop, 7);
            FinishCourse(loop, CourseKind.Ina);
            FinishCourse(loop, CourseKind.Una);
            Assert.IsTrue(loop.Promote());
            Assert.IsTrue(loop.Promote());

            Assert.AreEqual(Career.TopPosition, loop.Job.Position);
            Assert.IsFalse(loop.CanBePromoted);
        }

        [Test]
        public void Career_IsSavedWithTheRun()
        {
            var loop = new GameLoop();
            WorkOnTime(loop, 3);
            FinishCourse(loop, CourseKind.Ina);
            loop.Promote();

            var restored = new GameLoop();
            restored.Restore(loop.Snapshot());

            Assert.AreEqual(JobPosition.Cashier, restored.Job.Position);
            Assert.AreEqual(3, restored.Job.PunctualDays);
            Assert.IsTrue(restored.Courses.Done(CourseKind.Ina));
            Assert.AreEqual(loop.State.SalaryNet, restored.State.SalaryNet);
        }

        [TestCase("Curso del INA")]
        [TestCase("¡Te ascendieron!")]
        [TestCase("Encargada")]
        public void TheTexts_AreTranslated(string spanish)
        {
            Assert.AreNotEqual(spanish, Loc.Translate(spanish, GameLanguage.English));
        }
    }
}

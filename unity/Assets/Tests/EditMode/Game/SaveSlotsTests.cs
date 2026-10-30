using System;
using ColonQuest.Finance;
using ColonQuest.World;
using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class SaveSlotsTests
    {
        private static readonly DateTime When = new DateTime(2026, 10, 30, 14, 5, 0, DateTimeKind.Utc);

        private static SaveSlots NewSlots()
        {
            return new SaveSlots(new MemorySaveStore());
        }

        [Test]
        public void EmptySlot_ReadsAsNothing()
        {
            Assert.IsNull(NewSlots().Load(0));
        }

        [Test]
        public void Slots_AreIndependentAndOnlyThreeExist()
        {
            var slots = NewSlots();
            slots.Save(1, SaveSlot.From("Ana", new GameLoop(), When));

            Assert.IsNull(slots.Load(0));
            Assert.AreEqual("Ana", slots.Load(1).name);
            Assert.IsNull(slots.Load(2));

            slots.Save(SaveSlots.Count, SaveSlot.From("Fuera", new GameLoop(), When));
            Assert.IsNull(slots.Load(SaveSlots.Count));
            Assert.IsNull(slots.Load(-1));
        }

        [Test]
        public void Delete_EmptiesTheSlot()
        {
            var slots = NewSlots();
            slots.Save(0, SaveSlot.From("Ana", new GameLoop(), When));
            slots.Delete(0);
            Assert.IsNull(slots.Load(0));
        }

        [Test]
        public void BrokenSave_CountsAsEmpty()
        {
            var store = new MemorySaveStore();
            store.Write("colonquest.save.0", "{ this is not json");
            Assert.IsNull(new SaveSlots(store).Load(0));
        }

        [Test]
        public void Run_ComesBackTheSameAfterSavingAndLoading()
        {
            var loop = new GameLoop();
            loop.State.Cash = 77_000L;
            loop.State.Savings = 12_000L;
            loop.State.CreditCard.Balance = 150_000L;
            loop.State.Installments.Add(new Installment("Refri", 18_000L, 12));
            loop.State.InformalLoans.Add(new InformalLoan("Don Chema", 40_000L, 20m));
            loop.State.RecordHistory("paid_minimum_only");
            loop.Holdings.SetHome(new HomeHolding("Casa", true, 30_000_000L, 180_000L, 240));
            loop.Holdings.SetCar(new CarHolding("Carro", 4_000_000L, 60_000L));
            loop.Holdings.SetHealthInsurance(15_000L);
            loop.Holdings.AddFarm(FarmKind.Coffee, 200_000L, 1);
            loop.BuySwimsuit();
            loop.Sleep();
            loop.Clock.Advance(GameRules.SecondsPerGameHour * 3f, GameRules.SecondsPerGameHour);
            long cash = loop.State.Cash;

            var slots = NewSlots();
            slots.Save(2, SaveSlot.From("Ana", loop, When));

            var back = new GameLoop();
            back.Restore(slots.Load(2).run);

            Assert.AreEqual(cash, back.State.Cash);
            Assert.AreEqual(12_000L, back.State.Savings);
            Assert.AreEqual(150_000L, back.State.CreditCard.Balance);
            Assert.AreEqual(3.5m, back.State.CreditCard.MonthlyRatePct);
            Assert.AreEqual("Refri", back.State.Installments[back.State.Installments.Count - 1].Item);
            Assert.AreEqual(20m, back.State.InformalLoans[0].MonthlyRatePct);
            Assert.Contains("paid_minimum_only", back.State.History);
            Assert.AreEqual(180_000L, back.Holdings.Home.Monthly);
            Assert.AreEqual(4_000_000L, back.Holdings.Car.ResaleValue);
            Assert.AreEqual(15_000L, back.Holdings.HealthPremium);
            Assert.AreEqual(200_000L, back.Holdings.FarmInvested);
            Assert.IsTrue(back.HasSwimsuit);
            Assert.IsTrue(back.SleptThisMonth);
            Assert.AreEqual(2, back.Clock.Day);
            Assert.AreEqual("09:00", back.Clock.Time);
        }

        [Test]
        public void Run_RemembersThePlacesAlreadyUsedThisMonth()
        {
            var loop = new GameLoop();
            var gameEvent = new GameEvent { Choices = { new Choice { Id = "a" }, new Choice { Id = "b" } } };
            loop.Choose(gameEvent, "a", StationType.Garage);

            var back = new GameLoop();
            back.Restore(loop.Snapshot());

            Assert.AreEqual(1, back.TasksDone);
            Assert.IsTrue(back.AlreadyDid(StationType.Garage));
            Assert.IsFalse(back.CanDoTask(StationType.Garage));
            Assert.IsTrue(back.CanDoTask(StationType.Grocery));
        }

        [Test]
        public void Summary_ShowsWhatTheListNeeds()
        {
            var loop = new GameLoop();
            loop.Sleep();
            loop.CloseMonth();
            loop.Sleep();
            var slot = SaveSlot.From("Ana", loop, When);

            Assert.AreEqual(2, slot.Month);
            Assert.AreEqual(2, slot.Week);
            Assert.AreEqual(loop.State.Cash, slot.Cash);
            Assert.AreEqual(loop.State.Wellbeing, slot.Wellbeing);
            Assert.IsFalse(slot.Finished);
            StringAssert.Contains("/10", slot.SavedAtLabel);
        }

        [Test]
        public void Summary_KnowsAFinishedYear()
        {
            var loop = new GameLoop();
            for (int i = 0; i < GameRules.MonthsToPlay; i++)
            {
                loop.CloseMonth();
            }

            Assert.IsTrue(SaveSlot.From("Ana", loop, When).Finished);
        }
    }
}

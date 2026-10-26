using System.Collections.Generic;
using NUnit.Framework;

namespace ColonQuest.Finance.Tests
{
    [TestFixture]
    public sealed class FinanceEngineEffectsTests
    {
        [Test]
        public void ApplyChoice_EmergencyEvent_AppliesCashAndCreditCardChoicesCorrectly()
        {
            var gameEvent = new GameEvent
            {
                EventId = "evt_moto_danada",
                Type = EventType.Emergency,
                Title = "Se te dañó la moto",
                Narrative = "Don Rafa revisó la moto y hay que cambiarle el kit de arrastre.",
                Npc = new Npc("mecanico_don_rafa", "Don Rafa", "mechanic"),
                TeachingConcept = TeachingConcept.EmergencyFund,
                Choices = new List<Choice>
                {
                    new Choice(
                        "pay_cash",
                        "Pagar con efectivo",
                        new List<Effect>
                        {
                            new Effect(EffectKind.Expense, 85_000L, MoneySource.Cash),
                            new Effect(EffectKind.WellbeingChange, 5L),
                        }),
                    new Choice(
                        "pay_card",
                        "Pasarlo a la tarjeta",
                        new List<Effect>
                        {
                            new Effect(EffectKind.Expense, 85_000L, MoneySource.CreditCard),
                        }),
                },
            };

            var stateCash = new PlayerState { Cash = 120_000L, Wellbeing = 60 };
            FinanceEngine.ApplyChoice(stateCash, gameEvent, "pay_cash");
            Assert.That(stateCash.Cash, Is.EqualTo(35_000L));
            Assert.That(stateCash.Wellbeing, Is.EqualTo(65));
            Assert.That(stateCash.History, Does.Contain("evt_moto_danada:pay_cash"));

            var stateCard = new PlayerState
            {
                Cash = 120_000L,
                CreditCard = new CreditCard(100_000L, 300_000L, 3.5m, 10m),
            };
            FinanceEngine.ApplyChoice(stateCard, gameEvent, "pay_card");
            Assert.That(stateCard.Cash, Is.EqualTo(120_000L));
            Assert.That(stateCard.CreditCard.Balance, Is.EqualTo(185_000L));
        }

        [Test]
        public void ApplyEffect_CoversAllSevenEffectKindsAndMaintainsNonNegativeCash()
        {
            var state = new PlayerState
            {
                SalaryNet = 450_000L,
                Cash = 40_000L,
                Savings = 60_000L,
                EmergencyFund = 30_000L,
                CreditCard = new CreditCard(0L, 300_000L, 3.5m, 10m),
                Wellbeing = 70,
            };

            // 1. Income
            FinanceEngine.ApplyEffect(state, EventType.Income, new Effect(EffectKind.Income, 50_000L, MoneySource.Cash));
            Assert.That(state.Cash, Is.EqualTo(90_000L));

            // 2. Expense from EmergencyFund exceeding fund (50,000 from 30,000 fund -> 20,000 from Cash)
            FinanceEngine.ApplyEffect(
                state,
                EventType.Emergency,
                new Effect(EffectKind.Expense, 50_000L, MoneySource.EmergencyFund));
            Assert.That(state.EmergencyFund, Is.EqualTo(0L));
            Assert.That(state.Cash, Is.EqualTo(70_000L));

            // 3. NewInstallment
            FinanceEngine.ApplyEffect(
                state,
                EventType.Offer,
                new Effect(EffectKind.NewInstallment, 25_000L, months: 24),
                itemLabel: "Pantalla 65");
            Assert.That(state.Installments.Count, Is.EqualTo(1));
            Assert.That(state.Installments[0].Item, Is.EqualTo("Pantalla 65"));
            Assert.That(state.Installments[0].Monthly, Is.EqualTo(25_000L));
            Assert.That(state.Installments[0].MonthsLeft, Is.EqualTo(24));

            // 4. NewInformalLoan
            FinanceEngine.ApplyEffect(
                state,
                EventType.Emergency,
                new Effect(EffectKind.NewInformalLoan, 80_000L, months: 3),
                lenderLabel: "Don Beto");
            Assert.That(state.Cash, Is.EqualTo(150_000L));
            Assert.That(state.InformalLoans.Count, Is.EqualTo(1));
            Assert.That(state.InformalLoans[0].Lender, Is.EqualTo("Don Beto"));
            Assert.That(state.InformalLoans[0].Balance, Is.EqualTo(80_000L));
            Assert.That(state.InformalLoans[0].MonthlyRatePct, Is.EqualTo(20.0m));

            // 5. SavingsChange (negative exceeding savings rolls remainder to cash)
            FinanceEngine.ApplyEffect(state, EventType.Scam, new Effect(EffectKind.SavingsChange, -80_000L));
            Assert.That(state.Savings, Is.EqualTo(0L));
            Assert.That(state.Cash, Is.EqualTo(130_000L));

            // 6. WellbeingChange (clamped to [0, 100])
            FinanceEngine.ApplyEffect(state, EventType.Bonus, new Effect(EffectKind.WellbeingChange, 20L));
            Assert.That(state.Wellbeing, Is.EqualTo(90));

            // 7. PriceInflation (10% of ₡450,000 salary = ₡45,000 deducted from cash)
            FinanceEngine.ApplyEffect(state, EventType.MacroNews, new Effect(EffectKind.PriceInflation, 10L));
            Assert.That(state.Cash, Is.EqualTo(85_000L));
            Assert.That(state.History, Does.Contain("inflation_shock"));
        }

        [Test]
        public void ValidateEffect_RejectsOutOfRangeAmountsAndMonthsForEveryKind()
        {
            // Emergency expense range is [20,000, 250,000]
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Emergency, new Effect(EffectKind.Expense, 10_000L, MoneySource.Cash)));
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Emergency, new Effect(EffectKind.Expense, 900_000L, MoneySource.Cash)));

            // Expense without source is rejected
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Expense, new Effect(EffectKind.Expense, 25_000L, null)));

            // NewInstallment requires months in [3, 48] and amount in [5,000, 80,000]
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Offer, new Effect(EffectKind.NewInstallment, 25_000L, months: null)));
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Offer, new Effect(EffectKind.NewInstallment, 25_000L, months: 2)));
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Offer, new Effect(EffectKind.NewInstallment, 95_000L, months: 12)));

            // WellbeingChange range is [-20, 20]
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.Scam, new Effect(EffectKind.WellbeingChange, -35L)));

            // PriceInflation range is [1, 15]
            Assert.Throws<InvalidGameEventEffectException>(() =>
                EffectLimits.ValidateEffect(EventType.MacroNews, new Effect(EffectKind.PriceInflation, 25L)));
        }

        [Test]
        public void ApplyChoice_WhenSecondEffectIsInvalid_DoesNotMutatePlayerState()
        {
            var gameEvent = new GameEvent
            {
                EventId = "evt_invalido",
                Type = EventType.Emergency,
                Title = "Evento inválido",
                Narrative = "Un efecto fuera de rango no debe tocar el estado.",
                TeachingConcept = TeachingConcept.EmergencyFund,
                Choices = new List<Choice>
                {
                    new Choice(
                        "bad_choice",
                        "Opción con segundo efecto inválido",
                        new List<Effect>
                        {
                            new Effect(EffectKind.Expense, 40_000L, MoneySource.Cash),
                            new Effect(EffectKind.WellbeingChange, -50L), // out of [-20, 20]
                        }),
                    new Choice(
                        "ok_choice",
                        "Opción válida",
                        new List<Effect>
                        {
                            new Effect(EffectKind.Expense, 40_000L, MoneySource.Cash),
                        }),
                },
            };

            var state = new PlayerState { Cash = 100_000L, Wellbeing = 70 };

            Assert.Throws<InvalidGameEventEffectException>(() =>
                FinanceEngine.ApplyChoice(state, gameEvent, "bad_choice"));

            // State is completely untouched
            Assert.That(state.Cash, Is.EqualTo(100_000L));
            Assert.That(state.Wellbeing, Is.EqualTo(70));
            Assert.That(state.History, Is.Empty);
        }
    }
}

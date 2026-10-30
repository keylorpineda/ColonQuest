using System;
using System.Collections.Generic;

namespace ColonQuest.Finance
{
    public enum EventType
    {
        Income,
        Expense,
        Emergency,
        Offer,
        Scam,
        MacroNews,
        Bonus,
    }

    public enum EffectKind
    {
        Expense,
        Income,
        NewInstallment,
        NewInformalLoan,
        SavingsChange,
        WellbeingChange,
        PriceInflation,
    }

    public enum MoneySource
    {
        Cash,
        Savings,
        EmergencyFund,
        CreditCard,
    }

    public enum TeachingConcept
    {
        Budget,
        EmergencyFund,
        CompoundInterest,
        MinimumPayment,
        InstallmentsTotalCost,
        InformalLending,
        PyramidScheme,
        Inflation,
        ExchangeRate,
        PayrollDeductions,
        InvestInYourself,
    }

    [Serializable]
    public sealed class Npc
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public Npc()
        {
        }

        public Npc(string id, string name, string role)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Role = role ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class Effect
    {
        public EffectKind Kind { get; set; }
        public long Amount { get; set; }
        public MoneySource? Source { get; set; }
        public int? Months { get; set; }

        public Effect()
        {
        }

        public Effect(EffectKind kind, long amount, MoneySource? source = null, int? months = null)
        {
            Kind = kind;
            Amount = amount;
            Source = source;
            Months = months;
        }
    }

    [Serializable]
    public sealed class Choice
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<Effect> Effects { get; set; } = new List<Effect>();

        public Choice()
        {
        }

        public Choice(string id, string label, List<Effect> effects)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            Effects = effects ?? new List<Effect>();
        }
    }

    [Serializable]
    public sealed class GameEvent
    {
        public string EventId { get; set; } = string.Empty;
        public EventType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Narrative { get; set; } = string.Empty;
        public Npc? Npc { get; set; }
        public string? ImageKey { get; set; }
        public List<Choice> Choices { get; set; } = new List<Choice>();
        public TeachingConcept TeachingConcept { get; set; }
    }

    public readonly struct LongRange
    {
        public long Low { get; }
        public long High { get; }

        public LongRange(long low, long high)
        {
            Low = low;
            High = high;
        }

        public bool Contains(long value) => value >= Low && value <= High;
    }

    /// <summary>
    /// Raised when the engine rejects a <see cref="GameEvent"/> or <see cref="Effect"/> that violates
    /// the contract bounds defined in <c>server/app/game_master/limits.py</c>.
    /// </summary>
    public sealed class InvalidGameEventEffectException : ArgumentException
    {
        public InvalidGameEventEffectException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Second-layer validation rules mirroring <c>server/app/game_master/limits.py</c> and
    /// <c>shared/schemas/GameEvent.schema.json</c>.
    /// </summary>
    public static class EffectLimits
    {
        public static LongRange GetAllowedAmountRange(EventType eventType, EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Expense:
                case EffectKind.Income:
                    return GetCashFlowRange(eventType);
                case EffectKind.NewInstallment:
                    return new LongRange(5_000L, 80_000L);
                case EffectKind.NewInformalLoan:
                    return new LongRange(20_000L, 300_000L);
                case EffectKind.SavingsChange:
                    return new LongRange(-300_000L, 300_000L);
                case EffectKind.WellbeingChange:
                    return new LongRange(-20L, 20L);
                case EffectKind.PriceInflation:
                    return new LongRange(1L, 15L);
                default:
                    throw new InvalidGameEventEffectException($"Unsupported effect kind: {kind}.");
            }
        }

        public static LongRange GetCashFlowRange(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Income:
                    return new LongRange(10_000L, 300_000L);
                case EventType.Expense:
                    return new LongRange(5_000L, 150_000L);
                case EventType.Emergency:
                    return new LongRange(20_000L, 250_000L);
                case EventType.Offer:
                    return new LongRange(10_000L, 600_000L);
                case EventType.Scam:
                    return new LongRange(10_000L, 500_000L);
                case EventType.MacroNews:
                    return new LongRange(0L, 100_000L);
                case EventType.Bonus:
                    return new LongRange(10_000L, 450_000L);
                default:
                    throw new InvalidGameEventEffectException($"Unsupported event type: {eventType}.");
            }
        }

        public static LongRange? GetAllowedMonthsRange(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.NewInstallment:
                    return new LongRange(3L, 48L);
                case EffectKind.NewInformalLoan:
                    return new LongRange(1L, 12L);
                default:
                    return null;
            }
        }

        public static void ValidateEffect(EventType eventType, Effect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            bool allowsNegative = effect.Kind == EffectKind.SavingsChange || effect.Kind == EffectKind.WellbeingChange;
            if (effect.Amount < 0L && !allowsNegative)
            {
                throw new InvalidGameEventEffectException(
                    $"Effect {effect.Kind} cannot have a negative amount ({effect.Amount}).");
            }

            if (effect.Kind == EffectKind.Expense && !effect.Source.HasValue)
            {
                throw new InvalidGameEventEffectException("Expense effects must specify a money source.");
            }

            LongRange allowedAmount = GetAllowedAmountRange(eventType, effect.Kind);
            if (!allowedAmount.Contains(effect.Amount))
            {
                throw new InvalidGameEventEffectException(
                    $"Effect {effect.Kind} amount {effect.Amount} is outside allowed range [{allowedAmount.Low}, {allowedAmount.High}] for event type {eventType}.");
            }

            LongRange? allowedMonths = GetAllowedMonthsRange(effect.Kind);
            if (allowedMonths.HasValue)
            {
                if (!effect.Months.HasValue)
                {
                    throw new InvalidGameEventEffectException(
                        $"Effect {effect.Kind} requires a months value.");
                }

                if (!allowedMonths.Value.Contains(effect.Months.Value))
                {
                    throw new InvalidGameEventEffectException(
                        $"Effect {effect.Kind} months {effect.Months.Value} is outside allowed range [{allowedMonths.Value.Low}, {allowedMonths.Value.High}].");
                }
            }
        }

        public static bool TryValidateEffect(EventType eventType, Effect effect, out string? errorMessage)
        {
            try
            {
                ValidateEffect(eventType, effect);
                errorMessage = null;
                return true;
            }
            catch (Exception ex) when (ex is InvalidGameEventEffectException || ex is ArgumentNullException)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}

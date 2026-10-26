using System;
using System.Linq;

namespace ColonQuest.Finance
{
    public static partial class FinanceEngine
    {
        public const decimal DefaultInformalLoanMonthlyRatePct = 15.0m;

        /// <summary>
        /// Validates and applies a player's chosen <see cref="Choice"/> from a <see cref="GameEvent"/>.
        /// Every effect in the choice is validated before any state is mutated; if any effect is out of
        /// range, <see cref="InvalidGameEventEffectException"/> is thrown and <paramref name="state"/>
        /// remains untouched.
        /// </summary>
        public static void ApplyChoice(PlayerState state, GameEvent gameEvent, string choiceId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (gameEvent == null)
                throw new ArgumentNullException(nameof(gameEvent));
            if (string.IsNullOrWhiteSpace(choiceId))
                throw new ArgumentException("Choice id is required.", nameof(choiceId));
            if (gameEvent.Choices == null || gameEvent.Choices.Count < 2 || gameEvent.Choices.Count > 4)
                throw new InvalidGameEventEffectException("GameEvent must contain between 2 and 4 choices.");

            Choice? choice = gameEvent.Choices.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null)
                throw new InvalidGameEventEffectException($"Choice '{choiceId}' was not found in event '{gameEvent.EventId}'.");
            if (choice.Effects == null || choice.Effects.Count > 5)
                throw new InvalidGameEventEffectException($"Choice '{choiceId}' has an invalid effects list.");

            // Validate every effect before applying any change (all-or-nothing).
            foreach (Effect effect in choice.Effects)
            {
                EffectLimits.ValidateEffect(gameEvent.Type, effect);
            }

            string itemLabel = !string.IsNullOrWhiteSpace(gameEvent.Title) ? gameEvent.Title : gameEvent.EventId;
            string lenderLabel = gameEvent.Npc != null && !string.IsNullOrWhiteSpace(gameEvent.Npc.Name)
                ? gameEvent.Npc.Name
                : "Prestamista informal";

            foreach (Effect effect in choice.Effects)
            {
                ApplyValidatedEffect(state, effect, itemLabel, lenderLabel);
            }

            state.RecordHistory($"{gameEvent.EventId}:{choice.Id}");
        }

        /// <summary>
        /// Validates a single <see cref="Effect"/> against <paramref name="eventType"/> bounds and
        /// applies it to <paramref name="state"/>.
        /// </summary>
        public static void ApplyEffect(
            PlayerState state,
            EventType eventType,
            Effect effect,
            string itemLabel = "Compra a cuotas",
            string lenderLabel = "Prestamista informal")
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            EffectLimits.ValidateEffect(eventType, effect);
            ApplyValidatedEffect(state, effect, itemLabel, lenderLabel);
        }

        private static void ApplyValidatedEffect(
            PlayerState state,
            Effect effect,
            string itemLabel,
            string lenderLabel)
        {
            switch (effect.Kind)
            {
                case EffectKind.Expense:
                    ApplyExpense(state, effect.Amount, effect.Source ?? MoneySource.Cash);
                    break;

                case EffectKind.Income:
                    ApplyIncome(state, effect.Amount, effect.Source ?? MoneySource.Cash);
                    break;

                case EffectKind.NewInstallment:
                    ApplyNewInstallment(state, itemLabel, effect.Amount, effect.Months!.Value);
                    break;

                case EffectKind.NewInformalLoan:
                    ApplyNewInformalLoan(state, lenderLabel, effect.Amount, effect.Months!.Value);
                    break;

                case EffectKind.SavingsChange:
                    ApplySavingsChange(state, effect.Amount);
                    break;

                case EffectKind.WellbeingChange:
                    state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + (int) effect.Amount);
                    break;

                case EffectKind.PriceInflation:
                    ApplyPriceInflation(state, effect.Amount);
                    break;

                default:
                    throw new InvalidGameEventEffectException($"Unsupported effect kind: {effect.Kind}.");
            }
        }

        private static void ApplyExpense(PlayerState state, long amountColones, MoneySource source)
        {
            switch (source)
            {
                case MoneySource.Cash:
                    ChargeObligationWithDebtFallback(state, amountColones);
                    break;

                case MoneySource.Savings:
                    if (state.Savings >= amountColones)
                    {
                        state.Savings -= amountColones;
                    }
                    else
                    {
                        long remainingAfterSavings = amountColones - state.Savings;
                        state.Savings = 0L;
                        ChargeObligationWithDebtFallback(state, remainingAfterSavings);
                    }
                    break;

                case MoneySource.EmergencyFund:
                    if (state.EmergencyFund >= amountColones)
                    {
                        state.EmergencyFund -= amountColones;
                    }
                    else
                    {
                        long remainingAfterEmergency = amountColones - state.EmergencyFund;
                        state.EmergencyFund = 0L;
                        ChargeObligationWithDebtFallback(state, remainingAfterEmergency);
                    }
                    break;

                case MoneySource.CreditCard:
                    state.CreditCard.Balance = ContractLimits.ClampColones(state.CreditCard.Balance + amountColones);
                    break;

                default:
                    throw new InvalidGameEventEffectException($"Unsupported money source: {source}.");
            }
        }

        private static void ApplyIncome(PlayerState state, long amountColones, MoneySource destination)
        {
            switch (destination)
            {
                case MoneySource.Cash:
                    state.Cash = ContractLimits.ClampColones(state.Cash + amountColones);
                    break;
                case MoneySource.Savings:
                    state.Savings = ContractLimits.ClampColones(state.Savings + amountColones);
                    break;
                case MoneySource.EmergencyFund:
                    state.EmergencyFund = ContractLimits.ClampColones(state.EmergencyFund + amountColones);
                    break;
                case MoneySource.CreditCard:
                    long paydown = Math.Min(state.CreditCard.Balance, amountColones);
                    state.CreditCard.Balance -= paydown;
                    long surplus = amountColones - paydown;
                    if (surplus > 0L)
                        state.Cash = ContractLimits.ClampColones(state.Cash + surplus);
                    break;
                default:
                    throw new InvalidGameEventEffectException($"Unsupported money source: {destination}.");
            }
        }

        private static void ApplyNewInstallment(PlayerState state, string itemLabel, long monthlyColones, int months)
        {
            string normalizedItem = string.IsNullOrWhiteSpace(itemLabel)
                ? "Compra a cuotas"
                : (itemLabel.Length > 60 ? itemLabel.Substring(0, 60) : itemLabel);

            if (state.Installments.Count >= ContractLimits.MaxInstallments)
                throw new InvalidGameEventEffectException("Maximum number of active installments reached.");

            state.Installments.Add(new Installment(normalizedItem, monthlyColones, months));
        }

        private static void ApplyNewInformalLoan(PlayerState state, string lenderLabel, long principalColones, int months)
        {
            string normalizedLender = string.IsNullOrWhiteSpace(lenderLabel)
                ? "Prestamista informal"
                : (lenderLabel.Length > 60 ? lenderLabel.Substring(0, 60) : lenderLabel);

            if (state.InformalLoans.Count >= ContractLimits.MaxInformalLoans)
                throw new InvalidGameEventEffectException("Maximum number of informal loans reached.");

            // Shorter-term "gota a gota" loans carry steeper monthly rates (15% to 20%).
            decimal monthlyRatePct = months <= 3 ? 20.0m : DefaultInformalLoanMonthlyRatePct;
            state.Cash = ContractLimits.ClampColones(state.Cash + principalColones);
            state.InformalLoans.Add(new InformalLoan(normalizedLender, principalColones, monthlyRatePct));
            state.RecordHistory("took_informal_loan");
        }

        private static void ApplySavingsChange(PlayerState state, long signedAmountColones)
        {
            if (signedAmountColones >= 0L)
            {
                state.Savings = ContractLimits.ClampColones(state.Savings + signedAmountColones);
                return;
            }

            long deduction = Math.Abs(signedAmountColones);
            if (state.Savings >= deduction)
            {
                state.Savings -= deduction;
                return;
            }

            long remaining = deduction - state.Savings;
            state.Savings = 0L;
            ChargeObligationWithDebtFallback(state, remaining);
        }

        private static void ApplyPriceInflation(PlayerState state, long inflationRatePct)
        {
            long baseAmount = state.SalaryNet > 0L ? state.SalaryNet : 450_000L;
            long inflationCost = ApplyPercentage(baseAmount, inflationRatePct);
            ChargeObligationWithDebtFallback(state, inflationCost);
            state.RecordHistory("inflation_shock");
        }
    }
}

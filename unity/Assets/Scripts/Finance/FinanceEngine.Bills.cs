using System;

namespace ColonQuest.Finance
{
    public static partial class FinanceEngine
    {
        /// <summary>What a bill that went unpaid through a month close adds to itself, in percent.</summary>
        private const decimal BillLateFeePct = 10m;

        public static long CalculateBillLateFee(long amountDue)
        {
            if (amountDue < 0L || amountDue > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amountDue), "The amount must be between 0 and MaxColones.");

            return ApplyPercentage(amountDue, BillLateFeePct);
        }

        /// <summary>
        /// Pays down the card out of cash, outside the monthly cycle. Pays only what the cash reaches and what is owed; returns
        /// what was paid.
        /// </summary>
        public static long PayTowardCard(PlayerState state, long requested)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (requested < 0L || requested > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(requested), "The amount must be between 0 and MaxColones.");

            long paid = Math.Min(requested, Math.Min(state.Cash, state.CreditCard.Balance));
            state.Cash -= paid;
            state.CreditCard.Balance -= paid;
            if (paid > 0L)
            {
                state.RecordHistory("paid_extra_to_card");
            }

            return paid;
        }

        /// <summary>Pays a bill out of cash. Nothing changes when the cash is not enough.</summary>
        public static bool TryPayBill(PlayerState state, long amountDue)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (amountDue <= 0L || amountDue > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amountDue), "The amount must be positive and within MaxColones.");

            if (!TrySpendCash(state, amountDue))
                return false;

            state.RecordHistory("paid_bill");
            return true;
        }
    }
}

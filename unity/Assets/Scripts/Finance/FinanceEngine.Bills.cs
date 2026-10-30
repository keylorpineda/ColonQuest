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

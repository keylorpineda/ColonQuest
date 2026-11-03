using System;

namespace ColonQuest.Finance
{
    public static partial class FinanceEngine
    {
        /// <summary>The mood a new piece of furniture gives: the room starts to feel like a home.</summary>
        private const int FurnitureWellbeingBoost = 2;

        /// <summary>Buys a piece of furniture cash down. Nothing changes when the cash is missing or it is already owned.</summary>
        public static PurchaseResult TryBuyFurniture(PlayerState state, Holdings holdings, FurnitureKind kind, long price)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));
            if (price <= 0L || price > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive and within MaxColones.");

            if (holdings.Has(kind))
                return PurchaseResult.Failed(PurchaseFailure.AlreadyHasFurniture);
            if (!TrySpendCash(state, price))
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);

            DeliverFurniture(state, holdings, kind);
            return PurchaseResult.Success();
        }

        /// <summary>
        /// Buys a piece of furniture in installments: the commission is paid in cash when signing and the monthly charge starts
        /// running. Nothing changes when the cash for the commission is missing.
        /// </summary>
        public static PurchaseResult TryBuyFurnitureInInstallments(
            PlayerState state,
            Holdings holdings,
            FurnitureKind kind,
            string item,
            long monthlyCharge,
            int termMonths,
            long commission)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));
            if (string.IsNullOrWhiteSpace(item))
                throw new ArgumentException("Item is required.", nameof(item));
            if (monthlyCharge <= 0L || monthlyCharge > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(monthlyCharge), "Monthly charge must be positive and within MaxColones.");
            if (termMonths <= 0)
                throw new ArgumentOutOfRangeException(nameof(termMonths), "The term must be positive.");
            if (commission < 0L || commission > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(commission), "Commission must be within MaxColones.");

            if (holdings.Has(kind))
                return PurchaseResult.Failed(PurchaseFailure.AlreadyHasFurniture);
            if (state.Installments.Count >= ContractLimits.MaxInstallments)
                return PurchaseResult.Failed(PurchaseFailure.TooManyInstallments);
            if (commission > 0L && !TrySpendCash(state, commission))
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);

            state.Installments.Add(new Installment(TruncateItem(item), monthlyCharge, termMonths));
            DeliverFurniture(state, holdings, kind);
            return PurchaseResult.Success();
        }

        private static void DeliverFurniture(PlayerState state, Holdings holdings, FurnitureKind kind)
        {
            holdings.AddFurniture(kind);
            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + FurnitureWellbeingBoost);
            state.RecordHistory("bought_furniture_" + kind.ToString().ToLowerInvariant());
        }
    }
}

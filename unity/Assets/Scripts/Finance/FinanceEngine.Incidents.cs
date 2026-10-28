using System;

namespace ColonQuest.Finance
{
    /// <summary>What a collector visit did: how much of the demand was paid and how much debt it left behind.</summary>
    public readonly struct CollectionResult
    {
        public long Paid { get; }
        public long NewDebt { get; }

        public CollectionResult(long paid, long newDebt)
        {
            Paid = paid;
            NewDebt = newDebt;
        }
    }

    /// <summary>
    /// Money that leaves in a moment the player did not choose: a fine, a hospital bill, the collectors at the
    /// door. Every figure arrives as a parameter (the illustrative ones live in <c>MarketAssumptions</c>); this
    /// file only moves it, in whole colones, and never lets cash go below zero without a debt behind it.
    /// </summary>
    public static partial class FinanceEngine
    {
        /// <summary>A fine is paid from cash; what cash cannot cover goes to the card. Returns the part put on the card.</summary>
        public static long ChargeFine(PlayerState state, long fine)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            long onCard = ChargeObligationWithDebtFallback(state, fine);
            state.RecordHistory("paid_fine");
            return onCard;
        }

        /// <summary>
        /// A hospital bill. The emergency fund is what it exists for, so it pays first; then cash; whatever is
        /// left goes to the card. Returns the part put on the card.
        /// </summary>
        public static long ChargeMedicalBill(PlayerState state, long bill)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (bill < 0L || bill > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(bill), "Bill must be between 0 and MaxColones.");

            long fromFund = Math.Min(state.EmergencyFund, bill);
            state.EmergencyFund -= fromFund;
            long onCard = ChargeObligationWithDebtFallback(state, bill - fromFund);
            state.RecordHistory(fromFund >= bill ? "hospital_paid_from_fund" : "hospital_bill");
            return onCard;
        }

        /// <summary>The share of a bill the player pays when a policy covers part of it, rounded up to the colon.</summary>
        public static long PatientShare(long bill, int patientSharePct)
        {
            if (bill < 0L)
                throw new ArgumentOutOfRangeException(nameof(bill), "Bill cannot be negative.");
            if (patientSharePct < 0 || patientSharePct > 100)
                throw new ArgumentOutOfRangeException(nameof(patientSharePct), "Share must be between 0 and 100.");

            return (bill * patientSharePct + 99L) / 100L;
        }

        /// <summary>The debt a collector can ask for, in total: the informal loans and the card balance.</summary>
        public static long DebtOwed(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            long total = state.CreditCard.Balance;
            foreach (var loan in state.InformalLoans)
            {
                total += loan.Balance;
            }

            return total;
        }

        /// <summary>
        /// Pays the collectors out of cash and lowers the debt by what was paid, informal loans first because they
        /// grow fastest. Pays only what cash allows; returns what was paid.
        /// </summary>
        public static long PayCollectors(PlayerState state, long demand)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (demand < 0L || demand > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(demand), "Demand must be between 0 and MaxColones.");

            long paid = Math.Min(state.Cash, demand);
            state.Cash -= paid;

            long left = paid;
            foreach (var loan in state.InformalLoans)
            {
                long take = Math.Min(left, loan.Balance);
                loan.Balance -= take;
                left -= take;
            }

            state.InformalLoans.RemoveAll(loan => loan.Balance <= 0L);
            long fromCard = Math.Min(left, state.CreditCard.Balance);
            state.CreditCard.Balance -= fromCard;

            state.RecordHistory("paid_collectors");
            return paid;
        }

        /// <summary>
        /// Talks the collectors down: half of the demand is paid now and the other half becomes a new informal
        /// loan with a surcharge on top, at the usual rate of those loans.
        /// </summary>
        public static CollectionResult NegotiateCollectors(PlayerState state, long demand, int surchargePct)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (surchargePct < 0 || surchargePct > 100)
                throw new ArgumentOutOfRangeException(nameof(surchargePct), "Surcharge must be between 0 and 100.");

            long paid = PayCollectors(state, demand / 2L);
            long rest = demand - paid;
            long newDebt = rest + rest * surchargePct / 100L;
            if (newDebt > 0L && state.InformalLoans.Count < ContractLimits.MaxInformalLoans)
            {
                state.InformalLoans.Add(new InformalLoan("Cobradores", ContractLimits.ClampColones(newDebt), DefaultInformalLoanMonthlyRatePct));
            }

            state.RecordHistory("negotiated_collectors");
            return new CollectionResult(paid, newDebt);
        }

        /// <summary>Ignoring the collectors costs a late fee on the card and some peace of mind.</summary>
        public static long IgnoreCollectors(PlayerState state, long demand, int lateFeePct, int wellbeingLoss)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (lateFeePct < 0 || lateFeePct > 100)
                throw new ArgumentOutOfRangeException(nameof(lateFeePct), "Fee must be between 0 and 100.");

            long fee = demand * lateFeePct / 100L;
            state.CreditCard.Balance = ContractLimits.ClampColones(state.CreditCard.Balance + fee);
            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing - wellbeingLoss);
            state.RecordHistory("ignored_collectors");
            return fee;
        }

        /// <summary>Changes the wellbeing by a signed amount, kept between 0 and 100.</summary>
        public static void ChangeWellbeing(PlayerState state, int delta)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + delta);
        }
    }
}

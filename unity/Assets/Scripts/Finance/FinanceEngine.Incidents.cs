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
    /// Money that leaves in a moment the user did not choose: a fine, a hospital bill, the collectors at the
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

        /// <summary>The share of a bill the user pays when a policy covers part of it, rounded up to the colon.</summary>
        public static long PatientShare(long bill, int patientSharePct)
        {
            if (bill < 0L)
                throw new ArgumentOutOfRangeException(nameof(bill), "Bill cannot be negative.");
            if (patientSharePct < 0 || patientSharePct > 100)
                throw new ArgumentOutOfRangeException(nameof(patientSharePct), "Share must be between 0 and 100.");

            return (bill * patientSharePct + 99L) / 100L;
        }

        /// <summary>The debt a collector can ask for: the informal loans. The card has its own interest and its own bank.</summary>
        public static long DebtOwed(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            long total = 0L;
            foreach (var loan in state.InformalLoans)
            {
                total += loan.Balance;
            }

            return total;
        }

        /// <summary>Lowers the informal loans in order, removes the ones paid off and returns what could be lowered.</summary>
        private static long LowerInformalLoans(PlayerState state, long amount)
        {
            long left = amount;
            foreach (var loan in state.InformalLoans)
            {
                long take = Math.Min(left, loan.Balance);
                loan.Balance -= take;
                left -= take;
            }

            state.InformalLoans.RemoveAll(loan => loan.Balance <= 0L);
            return amount - left;
        }

        /// <summary>
        /// Pays the collectors out of cash and lowers the informal debt by what was paid. Pays only what cash allows and what
        /// is owed; returns what was paid.
        /// </summary>
        public static long PayCollectors(PlayerState state, long demand)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (demand < 0L || demand > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(demand), "Demand must be between 0 and MaxColones.");

            long paid = Math.Min(state.Cash, Math.Min(demand, DebtOwed(state)));
            state.Cash -= paid;
            LowerInformalLoans(state, paid);
            state.RecordHistory("paid_collectors");
            return paid;
        }

        /// <summary>The user pays the lender by choice, out of cash: all of the informal debt or what the cash reaches. Returns what was paid.</summary>
        public static long PayInformalDebt(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            long paid = Math.Min(state.Cash, DebtOwed(state));
            if (paid > 0L)
            {
                state.Cash -= paid;
                LowerInformalLoans(state, paid);
                state.RecordHistory("paid_informal_debt");
            }

            return paid;
        }

        /// <summary>
        /// Talks the collectors down: half of the demand is paid now and the other half moves out of the old debt into a new
        /// informal loan, with a surcharge on top, at the usual rate of those loans. Only the surcharge is new debt.
        /// </summary>
        public static CollectionResult NegotiateCollectors(PlayerState state, long demand, int surchargePct)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (surchargePct < 0 || surchargePct > 100)
                throw new ArgumentOutOfRangeException(nameof(surchargePct), "Surcharge must be between 0 and 100.");

            long paid = PayCollectors(state, demand / 2L);
            long moved = LowerInformalLoans(state, demand - paid);
            long newDebt = moved + moved * surchargePct / 100L;
            if (newDebt > 0L)
            {
                if (state.InformalLoans.Count < ContractLimits.MaxInformalLoans)
                {
                    state.InformalLoans.Add(new InformalLoan("Cobradores", ContractLimits.ClampColones(newDebt), DefaultInformalLoanMonthlyRatePct));
                }
                else
                {
                    var last = state.InformalLoans[state.InformalLoans.Count - 1];
                    last.Balance = ContractLimits.ClampColones(last.Balance + newDebt);
                }
            }

            state.RecordHistory("negotiated_collectors");
            return new CollectionResult(paid, newDebt);
        }

        /// <summary>Ignoring the collectors costs a late fee on the informal debt and some peace of mind.</summary>
        public static long IgnoreCollectors(PlayerState state, long demand, int lateFeePct, int wellbeingLoss)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (lateFeePct < 0 || lateFeePct > 100)
                throw new ArgumentOutOfRangeException(nameof(lateFeePct), "Fee must be between 0 and 100.");

            long fee = 0L;
            if (state.InformalLoans.Count > 0)
            {
                fee = demand * lateFeePct / 100L;
                var first = state.InformalLoans[0];
                first.Balance = ContractLimits.ClampColones(first.Balance + fee);
            }

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

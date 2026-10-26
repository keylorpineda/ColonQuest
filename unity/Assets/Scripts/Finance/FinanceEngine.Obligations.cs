using System;
using System.Collections.Generic;
using System.Linq;

namespace ColonQuest.Finance
{
    /// <summary>
    /// Deterministic truth for an installment purchase (<c>compra a cuotas</c>), including hidden
    /// monthly insurance and upfront commission.
    /// </summary>
    public readonly struct InstallmentQuote
    {
        public long CashPrice { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }
        public long InsuranceMonthly { get; }
        public long Commission { get; }
        public long BaseMonthlyPayment { get; }
        public long TotalMonthlyPayment => BaseMonthlyPayment + InsuranceMonthly;
        public long TotalCost => (TotalMonthlyPayment * TermMonths) + Commission;
        public long OverpaymentColones => TotalCost - CashPrice;

        public InstallmentQuote(
            long cashPrice,
            int termMonths,
            decimal monthlyRatePct,
            long insuranceMonthly,
            long commission,
            long baseMonthlyPayment)
        {
            CashPrice = cashPrice;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            InsuranceMonthly = insuranceMonthly;
            Commission = commission;
            BaseMonthlyPayment = baseMonthlyPayment;
        }
    }

    public static partial class FinanceEngine
    {
        public const long InstallmentRoundingStepColones = 100L;

        /// <summary>
        /// Calculates the exact French amortization quote and total cost of an installment purchase,
        /// rounding the base monthly payment up to the next 100 colones to match the backend seller sheet
        /// (<c>server/app/negotiation/seller_sheet.py</c>).
        /// </summary>
        public static InstallmentQuote CalculateInstallmentQuote(
            long cashPriceColones,
            int termMonths,
            decimal monthlyRatePct,
            long insuranceMonthlyColones = 0L,
            long commissionColones = 0L)
        {
            if (cashPriceColones <= 0L || cashPriceColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(cashPriceColones), "Cash price must be positive and within MaxColones.");
            if (termMonths < 1 || termMonths > ContractLimits.MaxInstallmentMonths)
                throw new ArgumentOutOfRangeException(nameof(termMonths), "Term months must be between 1 and 120.");
            if (monthlyRatePct < 0m || monthlyRatePct > 100m)
                throw new ArgumentOutOfRangeException(nameof(monthlyRatePct), "Monthly rate must be between 0 and 100.");
            if (insuranceMonthlyColones < 0L || insuranceMonthlyColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(insuranceMonthlyColones), "Insurance fee cannot be negative.");
            if (commissionColones < 0L || commissionColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(commissionColones), "Commission cannot be negative.");

            decimal baseRaw;
            if (monthlyRatePct == 0m)
            {
                baseRaw = (decimal) cashPriceColones / termMonths;
            }
            else
            {
                double r = (double) (monthlyRatePct / 100m);
                double denom = 1.0 - Math.Pow(1.0 + r, -termMonths);
                decimal factor = (decimal) (r / denom);
                baseRaw = cashPriceColones * factor;
            }

            long roundedBase = (long) (decimal.Ceiling(baseRaw / InstallmentRoundingStepColones) * InstallmentRoundingStepColones);

            return new InstallmentQuote(
                cashPriceColones,
                termMonths,
                monthlyRatePct,
                insuranceMonthlyColones,
                commissionColones,
                roundedBase);
        }

        /// <summary>
        /// Calculates the Costa Rican December <c>aguinaldo</c> (Ley N.° 2412: 1/12th of total wages
        /// earned from December to November, exempt from CCSS and income tax).
        /// </summary>
        public static long CalculateAguinaldo(IReadOnlyList<long> monthlySalariesColones)
        {
            if (monthlySalariesColones == null)
                throw new ArgumentNullException(nameof(monthlySalariesColones));
            if (monthlySalariesColones.Count == 0 || monthlySalariesColones.Count > 12)
                throw new ArgumentOutOfRangeException(
                    nameof(monthlySalariesColones),
                    "Monthly salaries list must contain between 1 and 12 months.");

            decimal sum = 0m;
            for (int i = 0; i < monthlySalariesColones.Count; i++)
            {
                long salary = monthlySalariesColones[i];
                if (salary < 0L || salary > ContractLimits.MaxColones)
                    throw new ArgumentOutOfRangeException(nameof(monthlySalariesColones), "Monthly salary out of range.");
                sum += salary;
            }

            return RoundColones(sum / 12m);
        }

        /// <summary>
        /// Calculates the Costa Rican December <c>aguinaldo</c> for a constant monthly salary over
        /// <paramref name="monthsWorked"/> months (1 to 12).
        /// </summary>
        public static long CalculateAguinaldo(long monthlySalaryColones, int monthsWorked = 12)
        {
            if (monthlySalaryColones < 0L || monthlySalaryColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(monthlySalaryColones), "Salary must be between 0 and MaxColones.");
            if (monthsWorked < 1 || monthsWorked > 12)
                throw new ArgumentOutOfRangeException(nameof(monthsWorked), "Months worked must be between 1 and 12.");

            return RoundColones((decimal) monthlySalaryColones * monthsWorked / 12m);
        }

        /// <summary>
        /// Deducts an obligatory expense from cash first; if cash is insufficient, covers the deficit
        /// by charging the credit card so that cash never becomes negative without associated debt.
        /// Returns the amount that had to be financed on the credit card.
        /// </summary>
        public static long ChargeObligationWithDebtFallback(PlayerState state, long amountColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (amountColones < 0L || amountColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(amountColones), "Amount must be between 0 and MaxColones.");
            if (amountColones == 0L)
                return 0L;

            if (state.Cash >= amountColones)
            {
                state.Cash -= amountColones;
                return 0L;
            }

            long paidFromCash = state.Cash;
            long deficit = amountColones - paidFromCash;
            state.Cash = 0L;
            state.CreditCard.Balance = ContractLimits.ClampColones(state.CreditCard.Balance + deficit);
            return deficit;
        }

        /// <summary>
        /// Processes all active monthly installments (<c>cuotas</c>), decrementing <c>MonthsLeft</c>
        /// and removing completed plans. Any cash shortfall is rolled onto the credit card balance.
        /// </summary>
        public static long ProcessMonthlyInstallments(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.Installments == null || state.Installments.Count == 0)
                return 0L;

            long totalCharged = 0L;
            var remaining = new List<Installment>(state.Installments.Count);

            foreach (Installment installment in state.Installments)
            {
                if (installment.MonthsLeft <= 0)
                    continue;

                ChargeObligationWithDebtFallback(state, installment.Monthly);
                totalCharged += installment.Monthly;

                int nextMonthsLeft = installment.MonthsLeft - 1;
                if (nextMonthsLeft > 0)
                {
                    remaining.Add(new Installment(installment.Item, installment.Monthly, nextMonthsLeft));
                }
            }

            state.Installments = remaining;
            return totalCharged;
        }

        /// <summary>
        /// Applies the annual December vehicle circulation permit (<c>marchamo</c>).
        /// Uses cash first and rolls any deficit onto the credit card so cash never goes negative.
        /// </summary>
        public static long ApplyMarchamo(PlayerState state, long marchamoAmountColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (marchamoAmountColones < 0L || marchamoAmountColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(marchamoAmountColones), "Marchamo must be between 0 and MaxColones.");

            return ChargeObligationWithDebtFallback(state, marchamoAmountColones);
        }

        /// <summary>
        /// Accrues monthly interest on all informal loans (<c>préstamos gota a gota</c>) and returns
        /// the total interest added across all loans.
        /// </summary>
        public static long AccrueInformalLoansInterest(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.InformalLoans == null || state.InformalLoans.Count == 0)
                return 0L;

            long totalInterest = 0L;
            foreach (InformalLoan loan in state.InformalLoans)
            {
                if (loan.Balance <= 0L)
                    continue;
                long interest = ApplyPercentage(loan.Balance, loan.MonthlyRatePct);
                loan.Balance = ContractLimits.ClampColones(loan.Balance + interest);
                totalInterest += interest;
            }

            state.InformalLoans.RemoveAll(l => l.Balance <= 0L);
            return totalInterest;
        }

        /// <summary>
        /// Pays down a specific informal loan using available cash. Removes the loan when its balance reaches zero.
        /// </summary>
        public static long PayInformalLoan(PlayerState state, int loanIndex, long requestedPaymentColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.InformalLoans == null || loanIndex < 0 || loanIndex >= state.InformalLoans.Count)
                throw new ArgumentOutOfRangeException(nameof(loanIndex), "Invalid informal loan index.");
            if (requestedPaymentColones < 0L)
                throw new ArgumentOutOfRangeException(nameof(requestedPaymentColones), "Payment cannot be negative.");

            InformalLoan loan = state.InformalLoans[loanIndex];
            long actualPaid = Math.Min(state.Cash, Math.Min(requestedPaymentColones, loan.Balance));
            state.Cash -= actualPaid;
            loan.Balance -= actualPaid;

            if (loan.Balance == 0L)
                state.InformalLoans.RemoveAt(loanIndex);

            return actualPaid;
        }
    }
}

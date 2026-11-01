using System;

namespace ColonQuest.Finance
{
    /// <summary>
    /// Itemized breakdown of a Costa Rican monthly salary (<c>colilla de pago</c>).
    /// All figures are integer colones.
    /// </summary>
    public readonly struct PayrollBreakdown
    {
        public long GrossSalary { get; }
        public long SemDeduction { get; }
        public long IvmDeduction { get; }
        public long BancoPopularDeduction { get; }
        public long TotalSocialCharges => SemDeduction + IvmDeduction + BancoPopularDeduction;
        public long IncomeTax { get; }
        public long TotalDeductions => TotalSocialCharges + IncomeTax;
        public long NetSalary => GrossSalary - TotalDeductions;

        public PayrollBreakdown(
            long grossSalary,
            long semDeduction,
            long ivmDeduction,
            long bancoPopularDeduction,
            long incomeTax)
        {
            GrossSalary = grossSalary;
            SemDeduction = semDeduction;
            IvmDeduction = ivmDeduction;
            BancoPopularDeduction = bancoPopularDeduction;
            IncomeTax = incomeTax;
        }
    }

    /// <summary>
    /// Result of a monthly credit card billing and payment cycle.
    /// </summary>
    public readonly struct CreditCardCycleResult
    {
        public long StartingBalance { get; }
        public long MinimumPaymentDue { get; }
        public long AmountPaid { get; }
        public long UnpaidBalanceBeforeInterest { get; }
        public long InterestCharged { get; }
        public long EndingBalance { get; }

        public CreditCardCycleResult(
            long startingBalance,
            long minimumPaymentDue,
            long amountPaid,
            long unpaidBalanceBeforeInterest,
            long interestCharged,
            long endingBalance)
        {
            StartingBalance = startingBalance;
            MinimumPaymentDue = minimumPaymentDue;
            AmountPaid = amountPaid;
            UnpaidBalanceBeforeInterest = unpaidBalanceBeforeInterest;
            InterestCharged = interestCharged;
            EndingBalance = endingBalance;
        }
    }

    /// <summary>
    /// Pure deterministic C# financial engine for ColonQuest.
    /// <para>
    /// Rule 1: The AI tells the story; <see cref="FinanceEngine"/> keeps the books.
    /// Every money amount is an integer number of colones (<see cref="long"/>); <c>float</c> and
    /// <c>double</c> are never used.
    /// </para>
    /// </summary>
    public static partial class FinanceEngine
    {
        // =====================================================================
        // Official Costa Rican Payroll Rates (2026)
        // Sources:
        // 1. Caja Costarricense de Seguro Social (CCSS) & Ley de Protección al
        //    Trabajador N.° 7983 — Worker payroll deductions (cuota obrera):
        //    - Seguro de Enfermedad y Maternidad (SEM): 5.50%
        //    - Invalidez, Vejez y Muerte (IVM, effective Jan 1, 2026): 4.33%
        //    - Aporte de Trabajadores al Banco Popular (LPT): 1.00%
        //    Total mandatory worker social charges = 10.83% (1,083 basis points).
        //
        // 2. Ministerio de Hacienda (Costa Rica) — Impuesto Único sobre las
        //    Rentas Percibidas por el Trabajo Personal Dependiente (Ley N.° 7092,
        //    monthly salary tax brackets for fiscal period 2026):
        //    - Up to ₡918,000: 0% (exempt)
        //    - Over ₡918,000 up to ₡1,347,000: 10%
        //    - Over ₡1,347,000 up to ₡2,364,000: 15%
        //    - Over ₡2,364,000 up to ₡4,727,000: 20%
        //    - Over ₡4,727,000: 25%
        // =====================================================================

        public const decimal SemRatePct = 5.50m;
        public const decimal IvmRatePct = 4.33m;
        public const decimal BancoPopularRatePct = 1.00m;
        public const decimal TotalWorkerSocialRatePct = SemRatePct + IvmRatePct + BancoPopularRatePct; // 10.83%

        public const long TaxBracket1ExemptLimit = 918_000L;
        public const long TaxBracket2UpperLimit = 1_347_000L;
        public const long TaxBracket3UpperLimit = 2_364_000L;
        public const long TaxBracket4UpperLimit = 4_727_000L;

        public const decimal TaxBracket1RatePct = 10m;
        public const decimal TaxBracket2RatePct = 15m;
        public const decimal TaxBracket3RatePct = 20m;
        public const decimal TaxBracket4RatePct = 25m;

        public const long MinimumCardPaymentFloorColones = 5_000L;

        /// <summary>
        /// Rounds a decimal colones amount to the nearest whole colón (half away from zero).
        /// </summary>
        public static long RoundColones(decimal exactColones) =>
            (long) decimal.Round(exactColones, 0, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Computes a percentage of an integer colones amount, rounded to the nearest integer colón.
        /// </summary>
        public static long ApplyPercentage(long amountColones, decimal ratePct)
        {
            if (amountColones < 0)
                throw new ArgumentOutOfRangeException(nameof(amountColones), "Amount cannot be negative.");
            if (ratePct < 0m)
                throw new ArgumentOutOfRangeException(nameof(ratePct), "Rate percentage cannot be negative.");
            if (amountColones == 0L || ratePct == 0m)
                return 0L;

            return RoundColones(amountColones * (ratePct / 100m));
        }

        /// <summary>
        /// Calculates the itemized Costa Rican monthly payroll deductions (CCSS SEM, IVM, Banco Popular,
        /// and progressive salary income tax) for a given gross monthly salary in colones.
        /// </summary>
        public static PayrollBreakdown CalculatePayroll(long grossSalaryColones)
        {
            if (grossSalaryColones < 0 || grossSalaryColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(grossSalaryColones),
                    "Gross salary must be between 0 and MaxColones.");

            if (grossSalaryColones == 0L)
                return new PayrollBreakdown(0L, 0L, 0L, 0L, 0L);

            long sem = ApplyPercentage(grossSalaryColones, SemRatePct);
            long ivm = ApplyPercentage(grossSalaryColones, IvmRatePct);
            long bancoPopular = ApplyPercentage(grossSalaryColones, BancoPopularRatePct);
            long incomeTax = CalculateSalaryIncomeTax(grossSalaryColones);

            return new PayrollBreakdown(grossSalaryColones, sem, ivm, bancoPopular, incomeTax);
        }

        /// <summary>
        /// Returns the net monthly salary in colones after mandatory Costa Rican CCSS, Banco Popular
        /// and income tax deductions.
        /// </summary>
        public static long CalculateNetSalary(long grossSalaryColones) =>
            CalculatePayroll(grossSalaryColones).NetSalary;

        /// <summary>
        /// Calculates progressive monthly salary income tax (<c>Impuesto al Salario</c>) according to
        /// the Ministerio de Hacienda brackets.
        /// </summary>
        public static long CalculateSalaryIncomeTax(long grossSalaryColones)
        {
            if (grossSalaryColones < 0 || grossSalaryColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(grossSalaryColones),
                    "Gross salary must be between 0 and MaxColones.");

            if (grossSalaryColones <= TaxBracket1ExemptLimit)
                return 0L;

            decimal tax = 0m;

            long taxable1 = Math.Min(grossSalaryColones, TaxBracket2UpperLimit) - TaxBracket1ExemptLimit;
            if (taxable1 > 0)
                tax += taxable1 * (TaxBracket1RatePct / 100m);

            if (grossSalaryColones > TaxBracket2UpperLimit)
            {
                long taxable2 = Math.Min(grossSalaryColones, TaxBracket3UpperLimit) - TaxBracket2UpperLimit;
                tax += taxable2 * (TaxBracket2RatePct / 100m);
            }

            if (grossSalaryColones > TaxBracket3UpperLimit)
            {
                long taxable3 = Math.Min(grossSalaryColones, TaxBracket4UpperLimit) - TaxBracket3UpperLimit;
                tax += taxable3 * (TaxBracket3RatePct / 100m);
            }

            if (grossSalaryColones > TaxBracket4UpperLimit)
            {
                long taxable4 = grossSalaryColones - TaxBracket4UpperLimit;
                tax += taxable4 * (TaxBracket4RatePct / 100m);
            }

            return RoundColones(tax);
        }

        /// <summary>
        /// Computes monthly interest on an unpaid credit card balance in whole colones.
        /// </summary>
        public static long CalculateCardInterest(long unpaidBalanceColones, decimal monthlyRatePct)
        {
            if (unpaidBalanceColones < 0 || unpaidBalanceColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(unpaidBalanceColones),
                    "Unpaid balance must be between 0 and MaxColones.");
            if (monthlyRatePct < 0m || monthlyRatePct > 100m)
                throw new ArgumentOutOfRangeException(
                    nameof(monthlyRatePct),
                    "Monthly rate percentage must be between 0 and 100.");

            return ApplyPercentage(unpaidBalanceColones, monthlyRatePct);
        }

        /// <summary>
        /// Computes the required monthly minimum payment for a credit card.
        /// If the balance is positive and <see cref="CreditCard.MinPaymentPct"/> is positive, applies
        /// a floor of <see cref="MinimumCardPaymentFloorColones"/> (capped at the current balance).
        /// </summary>
        public static long CalculateMinimumPayment(CreditCard card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (card.Balance <= 0L || card.MinPaymentPct <= 0m)
                return 0L;

            long rawMinimum = ApplyPercentage(card.Balance, card.MinPaymentPct);
            long withFloor = Math.Max(rawMinimum, MinimumCardPaymentFloorColones);
            return Math.Min(card.Balance, withFloor);
        }

        /// <summary>
        /// Processes a monthly credit card payment from the player's cash and accrues monthly interest
        /// on any remaining unpaid balance. Cash never goes negative.
        /// </summary>
        public static CreditCardCycleResult ProcessCreditCardCycle(PlayerState state, long requestedPaymentColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.CreditCard == null)
                throw new ArgumentException("PlayerState.CreditCard cannot be null.", nameof(state));
            if (requestedPaymentColones < 0L)
                throw new ArgumentOutOfRangeException(
                    nameof(requestedPaymentColones),
                    "Requested payment cannot be negative.");

            CreditCard card = state.CreditCard;
            long startingBalance = card.Balance;
            long minDue = CalculateMinimumPayment(card);

            long maxPayable = Math.Min(state.Cash, startingBalance);
            long actualPaid = Math.Min(requestedPaymentColones, maxPayable);

            state.Cash -= actualPaid;
            long unpaid = startingBalance - actualPaid;

            long interest = CalculateCardInterest(unpaid, card.MonthlyRatePct);
            long endingBalance = ContractLimits.ClampColones(unpaid + interest);
            card.Balance = endingBalance;

            if (startingBalance > 0L && unpaid > 0L && actualPaid <= minDue)
            {
                state.RecordHistory("paid_minimum_only");
            }

            return new CreditCardCycleResult(
                startingBalance,
                minDue,
                actualPaid,
                unpaid,
                interest,
                endingBalance);
        }

        /// <summary>
        /// Convenience helper that pays only the credit card's monthly minimum payment and accrues
        /// compound interest on the remaining balance.
        /// </summary>
        public static CreditCardCycleResult PayCreditCardMinimum(PlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            long minDue = CalculateMinimumPayment(state.CreditCard);
            return ProcessCreditCardCycle(state, minDue);
        }
    }
}

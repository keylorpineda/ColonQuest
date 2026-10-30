using System;

namespace ColonQuest.Finance
{
    /// <summary>
    /// Parameters for executing a monthly financial tick in <see cref="FinanceEngine.RunMonthlyTick"/>.
    /// </summary>
    public sealed class MonthlyTickOptions
    {
        public long FixedLivingExpensesColones { get; set; }
        public bool PayOnlyMinimumOnCard { get; set; } = true;
        public long RequestedCardPaymentColones { get; set; }
        public long EmergencyFundContributionColones { get; set; }
        public bool TrackSkippedSavings { get; set; } = true;
        public bool IncludeDecemberAguinaldo { get; set; } = true;
        public long GrossSalaryForAguinaldoColones { get; set; }
        public int MonthsWorkedInYear { get; set; } = 12;
        public long DecemberMarchamoColones { get; set; }
        public bool AdvanceMonthNumber { get; set; } = true;

        /// <summary>What comes off the salary for being late or missing work. See <see cref="FinanceEngine.CalculateDockedPay"/>.</summary>
        public long SalaryDockedColones { get; set; }
    }

    /// <summary>
    /// Summary of all cash-flow and debt movements during a single monthly tick.
    /// </summary>
    public readonly struct MonthlyTickReport
    {
        public int MonthProcessed { get; }
        public long SalaryCredited { get; }
        public long AguinaldoCredited { get; }
        public long InstallmentsCharged { get; }
        public long MarchamoCharged { get; }
        public CreditCardCycleResult CardCycle { get; }
        public long InformalLoanInterestAccrued { get; }
        public long EmergencyFundContributed { get; }
        public long EndingCash { get; }
        public long EndingCardBalance { get; }
        public long SalaryDocked { get; }

        public MonthlyTickReport(
            int monthProcessed,
            long salaryCredited,
            long aguinaldoCredited,
            long installmentsCharged,
            long marchamoCharged,
            CreditCardCycleResult cardCycle,
            long informalLoanInterestAccrued,
            long emergencyFundContributed,
            long endingCash,
            long endingCardBalance,
            long salaryDocked = 0L)
        {
            MonthProcessed = monthProcessed;
            SalaryCredited = salaryCredited;
            AguinaldoCredited = aguinaldoCredited;
            InstallmentsCharged = installmentsCharged;
            MarchamoCharged = marchamoCharged;
            CardCycle = cardCycle;
            InformalLoanInterestAccrued = informalLoanInterestAccrued;
            EmergencyFundContributed = emergencyFundContributed;
            EndingCash = endingCash;
            EndingCardBalance = endingCardBalance;
            SalaryDocked = salaryDocked;
        }
    }

    public static partial class FinanceEngine
    {
        /// <summary>A month of pay counts 30 days, as Costa Rican payroll does.</summary>
        private const int PayrollDaysPerMonth = 30;

        /// <summary>A missed day of the story stands for a whole week, a quarter of the month.</summary>
        private const int PayrollWeeksPerMonth = 4;

        /// <summary>
        /// The pay lost in a month: one day for each late arrival that is docked and one week for each week missed. Never more
        /// than the salary itself.
        /// </summary>
        public static long CalculateDockedPay(long salaryNetColones, int dockedLateDays, int missedWeeks)
        {
            if (salaryNetColones < 0L || salaryNetColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(salaryNetColones),
                    "Net salary must be between 0 and MaxColones.");
            if (dockedLateDays < 0)
                throw new ArgumentOutOfRangeException(nameof(dockedLateDays), "Docked days cannot be negative.");
            if (missedWeeks < 0)
                throw new ArgumentOutOfRangeException(nameof(missedWeeks), "Missed weeks cannot be negative.");

            long docked = salaryNetColones / PayrollDaysPerMonth * dockedLateDays
                + salaryNetColones / PayrollWeeksPerMonth * missedWeeks;
            return Math.Min(salaryNetColones, docked);
        }

        /// <summary>
        /// Transfers up to <paramref name="requestedColones"/> from available cash into the player's
        /// emergency fund. Cash never becomes negative.
        /// </summary>
        public static long ContributeToEmergencyFund(
            PlayerState state,
            long requestedColones,
            bool trackSkippedSavings = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (requestedColones < 0L || requestedColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(requestedColones),
                    "Emergency fund contribution must be between 0 and MaxColones.");

            long actualContribution = Math.Min(state.Cash, requestedColones);
            state.Cash -= actualContribution;
            state.EmergencyFund = ContractLimits.ClampColones(state.EmergencyFund + actualContribution);

            if (actualContribution == 0L && trackSkippedSavings)
            {
                state.RecordHistory("skipped_savings");
            }

            return actualContribution;
        }

        /// <summary>
        /// Withdraws up to <paramref name="requestedColones"/> from the emergency fund into cash.
        /// </summary>
        public static long WithdrawFromEmergencyFund(PlayerState state, long requestedColones)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (requestedColones < 0L || requestedColones > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(
                    nameof(requestedColones),
                    "Withdrawal amount must be between 0 and MaxColones.");

            long actualWithdrawal = Math.Min(state.EmergencyFund, requestedColones);
            state.EmergencyFund -= actualWithdrawal;
            state.Cash = ContractLimits.ClampColones(state.Cash + actualWithdrawal);
            return actualWithdrawal;
        }

        /// <summary>
        /// Executes the deterministic monthly financial tick: salary, December aguinaldo and marchamo,
        /// fixed living expenses, active installments, credit card cycle, informal loan interest,
        /// and emergency fund contribution.
        /// </summary>
        public static MonthlyTickReport RunMonthlyTick(PlayerState state, MonthlyTickOptions? options = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            MonthlyTickOptions opts = options ?? new MonthlyTickOptions();
            int monthProcessed = state.Month;

            // 1. Salary
            long salaryDocked = Math.Max(0L, Math.Min(state.SalaryNet, opts.SalaryDockedColones));
            long salaryCredited = state.SalaryNet - salaryDocked;
            state.Cash = ContractLimits.ClampColones(state.Cash + salaryCredited);

            // 2. December aguinaldo
            long aguinaldoCredited = 0L;
            if (monthProcessed == 12 && opts.IncludeDecemberAguinaldo)
            {
                long baseSalary = opts.GrossSalaryForAguinaldoColones > 0L
                    ? opts.GrossSalaryForAguinaldoColones
                    : state.SalaryNet;
                aguinaldoCredited = CalculateAguinaldo(baseSalary, opts.MonthsWorkedInYear);
                state.Cash = ContractLimits.ClampColones(state.Cash + aguinaldoCredited);
            }

            // 3. Fixed living expenses
            if (opts.FixedLivingExpensesColones > 0L)
            {
                ChargeObligationWithDebtFallback(state, opts.FixedLivingExpensesColones);
            }

            // 4. Monthly installments
            long installmentsCharged = ProcessMonthlyInstallments(state);

            // 5. December marchamo
            long marchamoCharged = 0L;
            if (monthProcessed == 12 && opts.DecemberMarchamoColones > 0L)
            {
                ApplyMarchamo(state, opts.DecemberMarchamoColones);
                marchamoCharged = opts.DecemberMarchamoColones;
            }

            // 6. Credit card payment & interest cycle
            CreditCardCycleResult cardCycle = opts.PayOnlyMinimumOnCard
                ? PayCreditCardMinimum(state)
                : ProcessCreditCardCycle(state, opts.RequestedCardPaymentColones);

            // 7. Informal loans interest ("gota a gota")
            long informalInterest = AccrueInformalLoansInterest(state);

            // 8. Emergency fund contribution
            long emergencyContributed = ContributeToEmergencyFund(
                state,
                opts.EmergencyFundContributionColones,
                opts.TrackSkippedSavings);

            // 9. Advance month (1..12)
            if (opts.AdvanceMonthNumber)
            {
                state.Month = state.Month >= ContractLimits.MaxMonth ? ContractLimits.MinMonth : state.Month + 1;
            }

            return new MonthlyTickReport(
                monthProcessed,
                salaryCredited,
                aguinaldoCredited,
                installmentsCharged,
                marchamoCharged,
                cardCycle,
                informalInterest,
                emergencyContributed,
                state.Cash,
                state.CreditCard.Balance,
                salaryDocked);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace ColonQuest.Finance
{
    /// <summary>
    /// Contract bounds shared with the FastAPI backend (<c>shared/schemas/PlayerState.schema.json</c>).
    /// Every money amount is an integer number of Costa Rican colones (never <c>float</c> or <c>double</c>).
    /// </summary>
    public static class ContractLimits
    {
        public const long MaxColones = 100_000_000L;
        public const int MinMonth = 1;
        public const int MaxMonth = 12;
        public const int MinWellbeing = 0;
        public const int MaxWellbeing = 100;
        public const int MaxInstallmentMonths = 120;
        public const int MaxInstallments = 20;
        public const int MaxInformalLoans = 10;
        public const int MaxHistoryEntries = 50;

        public static long ClampColones(long amount) =>
            Math.Max(0L, Math.Min(MaxColones, amount));

        public static int ClampWellbeing(int wellbeing) =>
            Math.Max(MinWellbeing, Math.Min(MaxWellbeing, wellbeing));
    }

    /// <summary>
    /// Credit card state in whole colones, with monthly interest and minimum payment percentages.
    /// </summary>
    [Serializable]
    public sealed class CreditCard
    {
        public long Balance { get; set; }
        public long Limit { get; set; }
        public decimal MonthlyRatePct { get; set; }
        public decimal MinPaymentPct { get; set; }

        public CreditCard()
        {
        }

        public CreditCard(long balance, long limit, decimal monthlyRatePct, decimal minPaymentPct)
        {
            if (balance < 0 || balance > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(balance), "Card balance must be between 0 and MaxColones.");
            if (limit < 0 || limit > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(limit), "Card limit must be between 0 and MaxColones.");
            if (monthlyRatePct < 0m || monthlyRatePct > 100m)
                throw new ArgumentOutOfRangeException(nameof(monthlyRatePct), "Monthly rate must be between 0 and 100.");
            if (minPaymentPct < 0m || minPaymentPct > 100m)
                throw new ArgumentOutOfRangeException(nameof(minPaymentPct), "Minimum payment percentage must be between 0 and 100.");

            Balance = balance;
            Limit = limit;
            MonthlyRatePct = monthlyRatePct;
            MinPaymentPct = minPaymentPct;
        }

        public long AvailableCredit => Math.Max(0L, Limit - Balance);

        public CreditCard Clone() => new CreditCard(Balance, Limit, MonthlyRatePct, MinPaymentPct);
    }

    /// <summary>
    /// Fixed monthly installment purchase (<c>cuota</c>).
    /// </summary>
    [Serializable]
    public sealed class Installment
    {
        public string Item { get; set; } = string.Empty;
        public long Monthly { get; set; }
        public int MonthsLeft { get; set; }

        public Installment()
        {
        }

        public Installment(string item, long monthly, int monthsLeft)
        {
            if (string.IsNullOrWhiteSpace(item) || item.Length > 60)
                throw new ArgumentException("Item name must be 1 to 60 characters.", nameof(item));
            if (monthly < 0 || monthly > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(monthly), "Monthly payment must be between 0 and MaxColones.");
            if (monthsLeft < 0 || monthsLeft > ContractLimits.MaxInstallmentMonths)
                throw new ArgumentOutOfRangeException(nameof(monthsLeft), "Months left must be between 0 and 120.");

            Item = item;
            Monthly = monthly;
            MonthsLeft = monthsLeft;
        }

        public Installment Clone() => new Installment(Item, Monthly, MonthsLeft);
    }

    /// <summary>
    /// High-interest informal loan (<c>préstamo gota a gota</c>).
    /// </summary>
    [Serializable]
    public sealed class InformalLoan
    {
        public string Lender { get; set; } = string.Empty;
        public long Balance { get; set; }
        public decimal MonthlyRatePct { get; set; }

        public InformalLoan()
        {
        }

        public InformalLoan(string lender, long balance, decimal monthlyRatePct)
        {
            if (string.IsNullOrWhiteSpace(lender) || lender.Length > 60)
                throw new ArgumentException("Lender name must be 1 to 60 characters.", nameof(lender));
            if (balance < 0 || balance > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(balance), "Loan balance must be between 0 and MaxColones.");
            if (monthlyRatePct < 0m || monthlyRatePct > 100m)
                throw new ArgumentOutOfRangeException(nameof(monthlyRatePct), "Monthly rate must be between 0 and 100.");

            Lender = lender;
            Balance = balance;
            MonthlyRatePct = monthlyRatePct;
        }

        public InformalLoan Clone() => new InformalLoan(Lender, Balance, MonthlyRatePct);
    }

    /// <summary>
    /// Player financial state mirroring <c>shared/schemas/PlayerState.schema.json</c>.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public string CharacterName { get; set; } = string.Empty;
        public int Week { get; set; } = 1;
        public string JobPosition { get; set; } = string.Empty;
        public int Month { get; set; } = 1;
        public long SalaryNet { get; set; }
        public long Cash { get; set; }
        public long Savings { get; set; }
        public long EmergencyFund { get; set; }
        public CreditCard CreditCard { get; set; } = new CreditCard(0L, 300_000L, 3.5m, 10m);
        public List<Installment> Installments { get; set; } = new List<Installment>();
        public List<InformalLoan> InformalLoans { get; set; } = new List<InformalLoan>();
        public List<string> History { get; set; } = new List<string>();
        public int Wellbeing { get; set; } = 70;

        public PlayerState Clone()
        {
            return new PlayerState
            {
                CharacterName = CharacterName,
                Week = Week,
                JobPosition = JobPosition,
                Month = Month,
                SalaryNet = SalaryNet,
                Cash = Cash,
                Savings = Savings,
                EmergencyFund = EmergencyFund,
                CreditCard = CreditCard?.Clone() ?? new CreditCard(0L, 0L, 0m, 0m),
                Installments = Installments?.Select(i => i.Clone()).ToList() ?? new List<Installment>(),
                InformalLoans = InformalLoans?.Select(l => l.Clone()).ToList() ?? new List<InformalLoan>(),
                History = History != null ? new List<string>(History) : new List<string>(),
                Wellbeing = Wellbeing,
            };
        }

        public void RecordHistory(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;
            string trimmed = tag.Length > 60 ? tag.Substring(0, 60) : tag;
            if (History.Count >= ContractLimits.MaxHistoryEntries)
                History.RemoveAt(0);
            History.Add(trimmed);
        }
    }
}

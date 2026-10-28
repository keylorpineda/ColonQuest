using System;

namespace ColonQuest.Finance
{
    /// <summary>What it costs to rent: the monthly rent, the deposit and the whole lease.</summary>
    public readonly struct RentQuote
    {
        public long MonthlyRent { get; }
        public int LeaseMonths { get; }
        public long Deposit { get; }

        /// <summary>What leaves the pocket when signing.</summary>
        public long UpfrontCost => Deposit;

        /// <summary>The rent of the whole contract, which is owed even when the run ends sooner.</summary>
        public long LeaseCost => MonthlyRent * LeaseMonths;

        public RentQuote(long monthlyRent, int leaseMonths, long deposit)
        {
            MonthlyRent = monthlyRent;
            LeaseMonths = leaseMonths;
            Deposit = deposit;
        }
    }

    /// <summary>The true figures of a home loan (<c>hipoteca</c>) with a down payment (<c>prima</c>).</summary>
    public readonly struct MortgageQuote
    {
        public long Price { get; }
        public long DownPayment { get; }
        public long ClosingCosts { get; }
        public long Financed { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }
        public long MonthlyPayment { get; }

        /// <summary>What leaves the pocket on the day of the purchase: the down payment and the closing costs.</summary>
        public long UpfrontCost => DownPayment + ClosingCosts;

        public long TotalPaid => UpfrontCost + MonthlyPayment * TermMonths;

        /// <summary>What the bank earns: every payment minus the money it lent.</summary>
        public long TotalInterest => MonthlyPayment * TermMonths - Financed;

        public MortgageQuote(long price, long downPayment, long closingCosts, long financed, int termMonths, decimal monthlyRatePct, long monthlyPayment)
        {
            Price = price;
            DownPayment = downPayment;
            ClosingCosts = closingCosts;
            Financed = financed;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            MonthlyPayment = monthlyPayment;
        }
    }

    public static partial class FinanceEngine
    {
        public const int MaxDepositMonths = 3;
        public const int HomeWellbeingBoost = 5;

        public static RentQuote QuoteRent(long monthlyRent, int leaseMonths, int depositMonths)
        {
            if (monthlyRent <= 0L || monthlyRent > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(monthlyRent), "Rent must be positive and within MaxColones.");
            if (leaseMonths < 1 || leaseMonths > ContractLimits.MaxInstallmentMonths)
                throw new ArgumentOutOfRangeException(nameof(leaseMonths), "Lease months must be between 1 and the contract limit.");
            if (depositMonths < 0 || depositMonths > MaxDepositMonths)
                throw new ArgumentOutOfRangeException(nameof(depositMonths), "Deposit must be between 0 and 3 months of rent.");

            return new RentQuote(monthlyRent, leaseMonths, monthlyRent * depositMonths);
        }

        /// <summary>
        /// The loan for a home: the down payment is a share of the price, the bank lends the rest and the
        /// payment comes from <see cref="LoanMath"/>. The term cannot pass the installment limit of the contract.
        /// </summary>
        public static MortgageQuote QuoteMortgage(long price, decimal downPaymentPct, decimal closingCostsPct, decimal monthlyRatePct, int termMonths)
        {
            if (price <= 0L || price > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive and within MaxColones.");
            if (downPaymentPct < 0m || downPaymentPct >= 100m)
                throw new ArgumentOutOfRangeException(nameof(downPaymentPct), "Down payment must be at least 0 and below 100 percent.");
            if (closingCostsPct < 0m || closingCostsPct > 100m)
                throw new ArgumentOutOfRangeException(nameof(closingCostsPct), "Closing costs must be between 0 and 100 percent.");

            long downPayment = ApplyPercentage(price, downPaymentPct);
            long closingCosts = ApplyPercentage(price, closingCostsPct);
            long financed = price - downPayment;
            long payment = LoanMath.MonthlyPayment(financed, monthlyRatePct, termMonths);
            return new MortgageQuote(price, downPayment, closingCosts, financed, termMonths, monthlyRatePct, payment);
        }

        /// <summary>The share of the net salary that a monthly payment takes, rounded down. Zero when there is no salary.</summary>
        public static long ShareOfSalaryPct(long monthlyColones, long salaryNet)
        {
            if (salaryNet <= 0L || monthlyColones <= 0L)
                return 0L;
            return monthlyColones * 100L / salaryNet;
        }

        /// <summary>
        /// Signs a lease: the deposit leaves the cash now and the rent becomes a monthly installment that
        /// lasts the whole contract. Nothing changes when it cannot be done.
        /// </summary>
        public static PurchaseResult TryRentHome(PlayerState state, Holdings holdings, string name, RentQuote quote)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));

            if (holdings.Home != null)
                return PurchaseResult.Failed(PurchaseFailure.AlreadyHasHome);
            if (state.Installments.Count >= ContractLimits.MaxInstallments)
                return PurchaseResult.Failed(PurchaseFailure.TooManyInstallments);
            if (state.Cash < quote.UpfrontCost)
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);

            if (quote.UpfrontCost > 0L)
            {
                TrySpendCash(state, quote.UpfrontCost);
            }

            state.Installments.Add(new Installment(TruncateItem("Alquiler: " + name), quote.MonthlyRent, quote.LeaseMonths));
            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + HomeWellbeingBoost);
            state.RecordHistory("rented_home");
            holdings.SetHome(new HomeHolding(name, false, 0L, quote.MonthlyRent, quote.LeaseMonths));
            return PurchaseResult.Success();
        }

        /// <summary>
        /// Buys a home: the down payment and closing costs leave the cash now and the mortgage becomes a
        /// monthly installment. Nothing changes when it cannot be done.
        /// </summary>
        public static PurchaseResult TryBuyHome(PlayerState state, Holdings holdings, string name, MortgageQuote quote)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));

            if (holdings.Home != null)
                return PurchaseResult.Failed(PurchaseFailure.AlreadyHasHome);
            if (state.Installments.Count >= ContractLimits.MaxInstallments)
                return PurchaseResult.Failed(PurchaseFailure.TooManyInstallments);
            if (state.Cash < quote.UpfrontCost)
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);

            if (quote.UpfrontCost > 0L)
            {
                TrySpendCash(state, quote.UpfrontCost);
            }

            state.Installments.Add(new Installment(TruncateItem("Hipoteca: " + name), quote.MonthlyPayment, quote.TermMonths));
            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + HomeWellbeingBoost);
            state.RecordHistory("bought_home");
            holdings.SetHome(new HomeHolding(name, true, quote.Price, quote.MonthlyPayment, quote.TermMonths));
            return PurchaseResult.Success();
        }

        private static string TruncateItem(string item)
        {
            return item.Length > 60 ? item.Substring(0, 60) : item;
        }
    }
}

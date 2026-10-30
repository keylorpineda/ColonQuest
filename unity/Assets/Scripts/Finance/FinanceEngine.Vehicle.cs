using System;

namespace ColonQuest.Finance
{
    /// <summary>Everything the agency and the law ask for when buying a car with a down payment and installments.</summary>
    public sealed class CarPlan
    {
        public string Name { get; }
        public long Price { get; }
        public long DownPayment { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }

        /// <summary>The policy tied to the loan, charged inside the monthly payment.</summary>
        public long InsuranceMonthly { get; }

        public long Commission { get; }

        /// <summary>The circulation permit (<c>marchamo</c>) of the first year, paid when the car is registered.</summary>
        public long MarchamoFirstYear { get; }

        /// <summary>Fuel and upkeep, every month.</summary>
        public long RunningCostsMonthly { get; }

        /// <summary>What the car sells for the day after the purchase.</summary>
        public long ResaleValue { get; }

        public CarPlan(
            string name,
            long price,
            long downPayment,
            int termMonths,
            decimal monthlyRatePct,
            long insuranceMonthly,
            long commission,
            long marchamoFirstYear,
            long runningCostsMonthly,
            long resaleValue)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));
            if (price <= 0L || price > ContractLimits.MaxColones)
                throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive and within MaxColones.");
            if (downPayment < 0L || downPayment >= price)
                throw new ArgumentOutOfRangeException(nameof(downPayment), "Down payment must be at least 0 and less than the price.");
            if (marchamoFirstYear < 0L || runningCostsMonthly < 0L || resaleValue < 0L)
                throw new ArgumentOutOfRangeException(nameof(marchamoFirstYear), "Costs and values cannot be negative.");

            Name = name;
            Price = price;
            DownPayment = downPayment;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            InsuranceMonthly = insuranceMonthly;
            Commission = commission;
            MarchamoFirstYear = marchamoFirstYear;
            RunningCostsMonthly = runningCostsMonthly;
            ResaleValue = resaleValue;
        }
    }

    /// <summary>The true cost of a car: the loan, the cash needed up front and what it costs to keep running.</summary>
    public readonly struct CarQuote
    {
        public CarPlan Plan { get; }

        /// <summary>The installment quote of the financed part only (the price minus the down payment).</summary>
        public InstallmentQuote Financing { get; }

        public CarQuote(CarPlan plan, InstallmentQuote financing)
        {
            Plan = plan;
            Financing = financing;
        }

        /// <summary>Cash that must be on hand: the down payment and the opening commission.</summary>
        public long UpfrontCash => Plan.DownPayment + Plan.Commission;

        /// <summary>The installment: the payment plus the insurance.</summary>
        public long MonthlyInstallment => Financing.TotalMonthlyPayment;

        /// <summary>The installment plus fuel and upkeep: what the car takes out of every month.</summary>
        public long MonthlyAllIn => MonthlyInstallment + Plan.RunningCostsMonthly;

        /// <summary>What is paid for the car itself by the end of the term: down payment, installments and commission.</summary>
        public long TotalPaidForTheCar => Plan.DownPayment + Financing.TotalCost;

        /// <summary>What the credit added on top of the price: interest, insurance and commission.</summary>
        public long ExtraOverPrice => TotalPaidForTheCar - Plan.Price;
    }

    public static partial class FinanceEngine
    {
        public const int CarWellbeingBoost = 5;

        public static CarQuote QuoteCar(CarPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            var financing = CalculateInstallmentQuote(
                plan.Price - plan.DownPayment,
                plan.TermMonths,
                plan.MonthlyRatePct,
                plan.InsuranceMonthly,
                plan.Commission);
            return new CarQuote(plan, financing);
        }

        /// <summary>
        /// Buys a car: the down payment and the commission must be in cash, the installment starts running and
        /// the first marchamo is charged now, with the card covering whatever cash cannot (reported in the
        /// result). Nothing changes when the cash for the first two is missing.
        /// </summary>
        public static PurchaseResult TryBuyCar(PlayerState state, Holdings holdings, CarPlan plan)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (holdings == null)
                throw new ArgumentNullException(nameof(holdings));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            if (holdings.Car != null)
                return PurchaseResult.Failed(PurchaseFailure.AlreadyHasCar);
            if (state.Installments.Count >= ContractLimits.MaxInstallments)
                return PurchaseResult.Failed(PurchaseFailure.TooManyInstallments);

            var quote = QuoteCar(plan);
            if (state.Cash < quote.UpfrontCash)
                return PurchaseResult.Failed(PurchaseFailure.NotEnoughCash);

            if (quote.UpfrontCash > 0L)
            {
                TrySpendCash(state, quote.UpfrontCash);
            }

            state.Installments.Add(new Installment(TruncateItem("Carro: " + plan.Name), quote.MonthlyInstallment, plan.TermMonths));
            long onCard = ApplyMarchamo(state, plan.MarchamoFirstYear);
            state.Wellbeing = ContractLimits.ClampWellbeing(state.Wellbeing + CarWellbeingBoost);
            state.RecordHistory("bought_car");
            holdings.SetCar(new CarHolding(plan.Name, plan.ResaleValue, plan.RunningCostsMonthly));
            return PurchaseResult.Success(onCard);
        }
    }
}

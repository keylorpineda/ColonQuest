using System;
using System.Collections.Generic;

namespace ColonQuest.Finance
{
    public enum FarmKind
    {
        Coffee,
        Cattle
    }

    public enum PurchaseFailure
    {
        None,
        AlreadyHasHome,
        AlreadyHasCar,
        NotEnoughCash,
        TooManyInstallments
    }

    /// <summary>
    /// How a purchase ended. When it did not go through, nothing in the player's state changed.
    /// <see cref="FinancedOnCard"/> is the part of an obligation that cash could not cover and went to the card.
    /// </summary>
    public readonly struct PurchaseResult
    {
        public bool Bought { get; }
        public PurchaseFailure Failure { get; }
        public long FinancedOnCard { get; }

        public PurchaseResult(bool bought, PurchaseFailure failure, long financedOnCard)
        {
            Bought = bought;
            Failure = failure;
            FinancedOnCard = financedOnCard;
        }

        public static PurchaseResult Success(long financedOnCard = 0L) => new PurchaseResult(true, PurchaseFailure.None, financedOnCard);

        public static PurchaseResult Failed(PurchaseFailure failure) => new PurchaseResult(false, failure, 0L);
    }

    /// <summary>The home the player lives in because of a purchase: rented or bought with a mortgage.</summary>
    public sealed class HomeHolding
    {
        public string Name { get; }
        public bool Owned { get; }

        /// <summary>The price paid for an owned home, zero for a rental.</summary>
        public long Value { get; }

        /// <summary>The rent or the mortgage payment, per month.</summary>
        public long Monthly { get; }

        public int Months { get; }

        public HomeHolding(string name, bool owned, long value, long monthly, int months)
        {
            Name = name;
            Owned = owned;
            Value = value;
            Monthly = monthly;
            Months = months;
        }
    }

    public sealed class CarHolding
    {
        public string Name { get; }

        /// <summary>What the car would sell for today, not what was paid for it.</summary>
        public long ResaleValue { get; }

        /// <summary>Fuel and upkeep, charged with the living expenses at the end of every month.</summary>
        public long MonthlyRunningCosts { get; }

        public CarHolding(string name, long resaleValue, long monthlyRunningCosts)
        {
            Name = name;
            ResaleValue = resaleValue;
            MonthlyRunningCosts = monthlyRunningCosts;
        }
    }

    public sealed class FarmStake
    {
        public FarmKind Kind { get; }
        public long Invested { get; private set; }

        /// <summary>Months of the run already closed when the money went in.</summary>
        public int MonthsPlayedWhenBought { get; }

        public FarmStake(FarmKind kind, long invested, int monthsPlayedWhenBought)
        {
            Kind = kind;
            Invested = invested;
            MonthsPlayedWhenBought = monthsPlayedWhenBought;
        }

        public void Add(long amount)
        {
            Invested = ContractLimits.ClampColones(Invested + amount);
        }
    }

    /// <summary>
    /// What the player owns beyond cash, savings and debts: a home, a car and money tied up in the farm.
    /// It lives next to <see cref="PlayerState"/>, so the contract with the backend does not change.
    /// </summary>
    public sealed class Holdings
    {
        private readonly List<FarmStake> farm = new List<FarmStake>();

        public HomeHolding? Home { get; private set; }
        public CarHolding? Car { get; private set; }

        /// <summary>The monthly premium of the health insurance, or zero when the player has none.</summary>
        public long HealthPremium { get; private set; }

        public bool HasHealthInsurance => HealthPremium > 0L;
        public IReadOnlyList<FarmStake> Farm => farm;

        public bool IsEmpty => Home == null && Car == null && farm.Count == 0 && HealthPremium == 0L;

        public long MonthlyRunningCosts => (Car != null ? Car.MonthlyRunningCosts : 0L) + HealthPremium;

        public long FarmInvested
        {
            get
            {
                long total = 0L;
                foreach (var stake in farm)
                {
                    total += stake.Invested;
                }

                return total;
            }
        }

        /// <summary>What the holdings are worth now: the owned home at its price, the car at resale and the farm at cost.</summary>
        public long AssetValue
        {
            get
            {
                long total = FarmInvested;
                if (Home != null && Home.Owned)
                {
                    total += Home.Value;
                }

                if (Car != null)
                {
                    total += Car.ResaleValue;
                }

                return total;
            }
        }

        public void SetHome(HomeHolding home)
        {
            Home = home ?? throw new ArgumentNullException(nameof(home));
        }

        /// <summary>Takes out the health insurance; the premium is charged every month with the other running costs.</summary>
        public void SetHealthInsurance(long monthlyPremium)
        {
            if (monthlyPremium <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(monthlyPremium), "The premium must be positive.");
            }

            HealthPremium = monthlyPremium;
        }

        public void SetCar(CarHolding car)
        {
            Car = car ?? throw new ArgumentNullException(nameof(car));
        }

        /// <summary>Adds money to the stake of that kind, or opens one: there is one stake for coffee and one for cattle.</summary>
        public void AddFarm(FarmKind kind, long amount, int monthsPlayed)
        {
            foreach (var stake in farm)
            {
                if (stake.Kind == kind)
                {
                    stake.Add(amount);
                    return;
                }
            }

            farm.Add(new FarmStake(kind, amount, monthsPlayed));
        }
    }
}

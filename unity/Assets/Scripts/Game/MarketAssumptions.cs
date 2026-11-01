using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;

namespace ColonQuest.Game
{
    // ======================================================================================================
    // ILLUSTRATIVE VALUES. Every price, rate and percentage in this file was made up to show an idea in the
    // game (a mortgage is heavy, a car costs more than its installment, a farm takes years to pay off). None
    // of them comes from a real source. Before any of them is written anywhere as a fact (the README, the
    // video, a slide, the text of the game without the warning below), verify it against a real source: the
    // banks and the Superintendencia General de Entidades Financieras for rates, the Registro Nacional and
    // the INS for cars, the ICAFE, CORFOGA and the MAG for coffee and cattle.
    // All of it lives in this one file so there is a single place to check and to correct.
    // ======================================================================================================

    public enum ListingKind
    {
        ForSale,
        ForRent
    }

    /// <summary>A house the real estate agency shows. A sale has a price; a rental has a monthly rent.</summary>
    public sealed class HomeListing
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ListingKind Kind { get; }
        public long Price { get; }
        public long MonthlyRent { get; }

        public HomeListing(string id, string name, string description, ListingKind kind, long price, long monthlyRent)
        {
            Id = id;
            Name = name;
            Description = description;
            Kind = kind;
            Price = price;
            MonthlyRent = monthlyRent;
        }

        public bool ForSale => Kind == ListingKind.ForSale;
    }

    /// <summary>
    /// A car of the agency. The negotiation is client only: <see cref="ToSellerOffer"/> builds an offer that the
    /// backend does not know, so the conversation is answered by <see cref="LocalSeller"/>.
    /// </summary>
    public sealed class CarModel
    {
        public string Id { get; }
        public string Name { get; }
        public string SellerName { get; }
        public long Price { get; }
        public decimal DownPaymentPct { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }
        public long InsuranceMonthly { get; }
        public decimal CommissionPct { get; }
        public decimal MarchamoPct { get; }
        public long RunningCostsMonthly { get; }
        public decimal ResalePct { get; }

        public CarModel(
            string id,
            string name,
            string sellerName,
            long price,
            decimal downPaymentPct,
            int termMonths,
            decimal monthlyRatePct,
            long insuranceMonthly,
            decimal commissionPct,
            decimal marchamoPct,
            long runningCostsMonthly,
            decimal resalePct)
        {
            Id = id;
            Name = name;
            SellerName = sellerName;
            Price = price;
            DownPaymentPct = downPaymentPct;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            InsuranceMonthly = insuranceMonthly;
            CommissionPct = commissionPct;
            MarchamoPct = marchamoPct;
            RunningCostsMonthly = runningCostsMonthly;
            ResalePct = resalePct;
        }

        public long DownPayment => FinanceEngine.ApplyPercentage(Price, DownPaymentPct);
        public long Commission => FinanceEngine.ApplyPercentage(Price, CommissionPct);
        public long MarchamoFirstYear => FinanceEngine.ApplyPercentage(Price, MarchamoPct);
        public long ResaleValue => FinanceEngine.ApplyPercentage(Price, ResalePct);

        public CarPlan ToPlan()
        {
            return new CarPlan(Name, Price, DownPayment, TermMonths, MonthlyRatePct, InsuranceMonthly, Commission, MarchamoFirstYear, RunningCostsMonthly, ResaleValue);
        }

        public SellerOffer ToSellerOffer()
        {
            return new SellerOffer("car_" + Id, Name.ToLowerInvariant(), SellerName, Price, TermMonths, MonthlyRatePct, InsuranceMonthly, Commission, DownPayment);
        }
    }

    /// <summary>
    /// A piece of furniture of the store. Like a car, its negotiation is client only: <see cref="ToSellerOffer"/> builds an
    /// offer that the backend does not know, so the conversation is answered by <see cref="LocalSeller"/>.
    /// </summary>
    public sealed class FurnitureModel
    {
        public FurnitureKind Kind { get; }
        public string Name { get; }
        public string SellerItem { get; }
        public string Description { get; }
        public long Price { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }
        public long InsuranceMonthly { get; }
        public long Commission { get; }

        public FurnitureModel(FurnitureKind kind, string name, string sellerItem, string description, long price, int termMonths, decimal monthlyRatePct, long insuranceMonthly, long commission)
        {
            Kind = kind;
            Name = name;
            SellerItem = sellerItem;
            Description = description;
            Price = price;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            InsuranceMonthly = insuranceMonthly;
            Commission = commission;
        }

        public SellerOffer ToSellerOffer()
        {
            return new SellerOffer("furniture_" + Kind.ToString().ToLowerInvariant(), SellerItem, "Kevin", Price, TermMonths, MonthlyRatePct, InsuranceMonthly, Commission);
        }
    }

    public sealed class FarmOption
    {
        public FarmKind Kind { get; }
        public string Name { get; }
        public string Description { get; }

        /// <summary>Months from the purchase until the money comes back. The engine uses this one number.</summary>
        public int MonthsToPayoff { get; }

        /// <summary>The range the text may quote, for things that do not take the same time every time.</summary>
        public int MonthsMin { get; }

        public int MonthsMax { get; }
        public int LowPct { get; }
        public int BasePct { get; }
        public int HighPct { get; }

        public FarmOption(FarmKind kind, string name, string description, int monthsToPayoff, int monthsMin, int monthsMax, int lowPct, int basePct, int highPct)
        {
            Kind = kind;
            Name = name;
            Description = description;
            MonthsToPayoff = monthsToPayoff;
            MonthsMin = monthsMin;
            MonthsMax = monthsMax;
            LowPct = lowPct;
            BasePct = basePct;
            HighPct = highPct;
        }

        public FarmProjection Project(long invested)
        {
            return FinanceEngine.ProjectFarm(Kind, invested, MonthsToPayoff, LowPct, BasePct, HighPct);
        }
    }

    public static class MarketAssumptions
    {
        /// <summary>To show next to any figure of this file, so nobody takes it for a real quote.</summary>
        public const string Disclaimer = "Cifras de ejemplo del juego, no son datos oficiales ni una promesa de ganancia. Hay que verificarlas con una fuente real antes de decidir con plata de verdad.";

        public const string ShortDisclaimer = "Cifras de ejemplo del juego.";

        // ---------------------------------------------------------------- housing

        public const decimal HomeDownPaymentPct = 20m;
        public const decimal HomeClosingCostsPct = 4m;
        public const decimal MortgageMonthlyRatePct = 0.85m;

        /// <summary>The longest term the installment contract allows (10 years), shorter than a real mortgage.</summary>
        public const int MortgageTermMonths = ContractLimits.MaxInstallmentMonths;

        public const int LeaseMonths = 12;
        public const int RentDepositMonths = 1;

        public static readonly IReadOnlyList<HomeListing> Homes = new List<HomeListing>
        {
            new HomeListing("casa_vecina_b", "Casa Vecina B", "Casa de dos cuartos con patio, en el barrio del oeste.", ListingKind.ForSale, 38_000_000L, 0L),
            new HomeListing("casa_vecina_d", "Casa Vecina D", "Casa chiquita sobre la calle, cerca del banco.", ListingKind.ForRent, 0L, 260_000L),
            new HomeListing("casa_almendros_1", "Casa Los Almendros 1", "Casa de tres cuartos en la urbanización nueva del norte.", ListingKind.ForSale, 52_000_000L, 0L),
            new HomeListing("casa_almendros_2", "Casa Los Almendros 2", "Casa de dos cuartos en la urbanización nueva del norte.", ListingKind.ForRent, 0L, 340_000L)
        };

        public static HomeListing FindHome(string id)
        {
            foreach (var home in Homes)
            {
                if (home.Id == id)
                {
                    return home;
                }
            }

            return null;
        }

        public static RentQuote QuoteRent(HomeListing home)
        {
            return FinanceEngine.QuoteRent(home.MonthlyRent, LeaseMonths, RentDepositMonths);
        }

        public static MortgageQuote QuoteMortgage(HomeListing home)
        {
            return FinanceEngine.QuoteMortgage(home.Price, HomeDownPaymentPct, HomeClosingCostsPct, MortgageMonthlyRatePct, MortgageTermMonths);
        }

        // ---------------------------------------------------------------- cars

        public static readonly IReadOnlyList<CarModel> Cars = new List<CarModel>
        {
            new CarModel("compacto", "Compacto usado 2017", "Don Chepe", 3_200_000L, 10m, 48, 1.5m, 18_000L, 3m, 3m, 50_000L, 85m),
            new CarModel("sedan", "Sedán 2022", "Don Chepe", 7_500_000L, 15m, 60, 1.4m, 32_000L, 3m, 3m, 65_000L, 85m),
            new CarModel("suv", "SUV familiar 2024", "Don Chepe", 14_000_000L, 20m, 72, 1.3m, 55_000L, 3m, 3m, 90_000L, 80m)
        };

        public static CarModel FindCar(string id)
        {
            foreach (var car in Cars)
            {
                if (car.Id == id)
                {
                    return car;
                }
            }

            return null;
        }

        // ---------------------------------------------------------------- the law, the hospital and the collectors

        // All of these are illustrative, like everything in this file: check them against a real source (the
        // courts, the CCSS and the INS for health care, the SUGEF for collections) before stating any of them as fact.

        /// <summary>The fine for entering a house that is not yours, in colones.</summary>
        public const long TrespassFine = 120_000L;

        /// <summary>The swimsuit of the wardrobe, in colones.</summary>
        public const long SwimsuitPrice = 18_000L;

        /// <summary>What a swim in the sea does to the mood, once a month.</summary>
        public const int SwimWellbeing = 5;

        /// <summary>What a night in the own bed does to the mood, once a month.</summary>
        public const int RestWellbeing = 3;

        /// <summary>The mood lost at the end of a month in which the player never slept in the own bed.</summary>
        public const int SkippedSleepWellbeingLoss = 2;

        /// <summary>The rent of the room the player starts in, per month, until moving to a home of the real estate agency.</summary>
        public const long RoomRent = 90_000L;

        /// <summary>What a service charges this month: electricity follows the appliances, and a house uses more than a room.</summary>
        public static long BillAmount(BillKind kind, Holdings holdings)
        {
            bool house = holdings.Home != null;
            switch (kind)
            {
                case BillKind.Electricity:
                    return (house ? 15_000L : 7_000L)
                        + (holdings.Has(FurnitureKind.Fridge) ? 6_000L : 0L)
                        + (holdings.Has(FurnitureKind.Tv) ? 2_500L : 0L);
                case BillKind.Water:
                    return house ? 9_000L : 4_000L;
                case BillKind.Internet:
                    return 18_000L;
                default:
                    return 9_000L;
            }
        }

        /// <summary>The mood a promotion gives.</summary>
        public const int PromotionWellbeing = 5;

        /// <summary>What a real bed adds to the rest of the month, against the mattress on the floor.</summary>
        public const int BedRestBonus = 2;

        public static readonly IReadOnlyList<FurnitureModel> Furniture = new List<FurnitureModel>
        {
            new FurnitureModel(FurnitureKind.Table, "Mesa con dos sillas", "comedor de dos sillas", "Para comer sentado y anotar las cuentas del mes.", 75_000L, 12, 3.2m, 0L, 3_000L),
            new FurnitureModel(FurnitureKind.Tv, "Tele de 32 pulgadas", "televisor de 32 pulgadas", "Para las noticias, los partidos y los anuncios de cuotas.", 130_000L, 12, 3.2m, 1_500L, 5_000L),
            new FurnitureModel(FurnitureKind.Bed, "Cama con colchón", "juego de cama", "Adiós al colchón en el piso. Descansás mejor.", 165_000L, 12, 2.9m, 1_500L, 5_000L),
            new FurnitureModel(FurnitureKind.Fridge, "Refri pequeña", "refrigerador pequeño", "La comida aguanta más, y el recibo de la luz sube un poco.", 240_000L, 18, 2.9m, 2_000L, 8_000L)
        };

        /// <summary>The mood lost for each week the player did not show up at work.</summary>
        public const int MissedWorkWellbeingLoss = 3;

        /// <summary>What cooking at home saves against eating out, in colones, once a month.</summary>
        public const long HomeCookingSaving = 6_000L;

        public const int CookWellbeing = 1;

        /// <summary>The bail asks this for each minute that is left of the term in the cell, in colones.</summary>
        public const long BailPerMinute = 8_000L;

        /// <summary>The bail asks this for each game day that is left of the sentence in the cell, in colones.</summary>
        public const long BailPerDay = 40_000L;

        /// <summary>Days of the month lost to the night in jail and the hearing.</summary>
        public const int JailDaysLost = 2;

        public const int JailWellbeingLoss = 12;

        /// <summary>Seconds the owner gives before calling the police.</summary>
        public const float TrespassGraceSeconds = 9f;

        /// <summary>What a hospital stay costs a person with no health insurance.</summary>
        public const long HospitalBill = 380_000L;

        /// <summary>The monthly premium of the health insurance.</summary>
        public const long HealthInsurancePremium = 16_000L;

        /// <summary>The share of the hospital bill an insured patient still pays, in percent.</summary>
        public const int InsuredPatientSharePct = 15;

        public const int HospitalDaysLost = 1;

        public const int HospitalWellbeingLoss = 15;

        /// <summary>A card at or above this share of its limit counts as being in arrears.</summary>
        public const int ArrearsCardUsagePct = 90;

        /// <summary>The collectors ask for this share of what is owed, between the two limits.</summary>
        public const int CollectionDemandPct = 25;

        public const long CollectionMinDemand = 20_000L;
        public const long CollectionMaxDemand = 250_000L;

        /// <summary>When negotiating, the half not paid now comes back with this surcharge, as a new informal loan.</summary>
        public const int CollectionNegotiateSurchargePct = 10;

        /// <summary>Ignoring the collectors adds this share of the demand as a late fee on the card.</summary>
        public const int CollectionIgnoreFeePct = 12;

        public const int CollectionIgnoreWellbeingLoss = 10;

        // ---------------------------------------------------------------- farm

        /// <summary>The stakes the farm accepts, in colones.</summary>
        public static readonly long[] FarmStakes = { 50_000L, 100_000L, 200_000L };

        public static readonly IReadOnlyList<FarmOption> Farm = new List<FarmOption>
        {
            new FarmOption(FarmKind.Coffee, "Café", "Se siembra y se espera la cosecha. La plata queda metida años.", 36, 36, 36, -30, 25, 60),
            new FarmOption(FarmKind.Cattle, "Ganado", "Se compran terneros y se venden cuando están listos.", 21, 18, 24, -20, 20, 40)
        };

        public static FarmOption FindFarm(FarmKind kind)
        {
            foreach (var option in Farm)
            {
                if (option.Kind == kind)
                {
                    return option;
                }
            }

            return null;
        }
    }
}

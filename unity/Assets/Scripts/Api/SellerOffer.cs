using System;
using System.Collections.Generic;

namespace ColonQuest.Api
{
    /// <summary>
    /// The seller's hidden sheet for one installment offer. It mirrors <c>server/app/negotiation/seller_sheet.py</c>
    /// so the local seller, used when the backend does not answer, quotes the same true figures. The
    /// numbers come from this class and never from a model.
    /// </summary>
    public sealed class SellerOffer
    {
        private const decimal RoundTo = 100m;

        public string OfferId { get; }
        public string Item { get; }
        public string SellerName { get; }
        public long CashPrice { get; }
        public int TermMonths { get; }
        public decimal MonthlyRatePct { get; }
        public long InsuranceMonthly { get; }
        public long Commission { get; }

        public SellerOffer(string offerId, string item, string sellerName, long cashPrice, int termMonths, decimal monthlyRatePct, long insuranceMonthly, long commission)
        {
            OfferId = offerId;
            Item = item;
            SellerName = sellerName;
            CashPrice = cashPrice;
            TermMonths = termMonths;
            MonthlyRatePct = monthlyRatePct;
            InsuranceMonthly = insuranceMonthly;
            Commission = commission;
        }

        /// <summary>French amortization of the cash price, rounded up to the next 100 colones.</summary>
        public long MonthlyPayment
        {
            get
            {
                decimal rate = MonthlyRatePct / 100m;
                decimal raw;
                if (rate == 0m)
                {
                    raw = (decimal)CashPrice / TermMonths;
                }
                else
                {
                    decimal growth = 1m;
                    for (int i = 0; i < TermMonths; i++)
                    {
                        growth *= 1m + rate;
                    }

                    raw = CashPrice * rate / (1m - 1m / growth);
                }

                return (long)(Math.Ceiling(raw / RoundTo) * RoundTo);
            }
        }

        /// <summary>What leaves the player's pocket every month: the payment plus the insurance.</summary>
        public long MonthlyCharge => MonthlyPayment + InsuranceMonthly;

        public long TotalCost => MonthlyCharge * TermMonths + Commission;

        /// <summary>What the player pays on top of the cash price by buying in installments.</summary>
        public long ExtraOverCash => TotalCost - CashPrice;

        public string Describe(SellerFact fact)
        {
            switch (fact)
            {
                case SellerFact.CashPrice: return FormatColones(CashPrice);
                case SellerFact.MonthlyPayment: return FormatColones(MonthlyPayment);
                case SellerFact.TermMonths: return TermMonths + " meses";
                case SellerFact.InterestRate: return MonthlyRatePct.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "% mensual";
                case SellerFact.InsuranceFee: return FormatColones(InsuranceMonthly) + " al mes";
                case SellerFact.Commission: return FormatColones(Commission);
                case SellerFact.TotalCost: return FormatColones(TotalCost);
                default: return string.Empty;
            }
        }

        /// <summary>Costa Rican style: ₡31 500.</summary>
        public static string FormatColones(long amount)
        {
            return "₡" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
        }

        // ---------------------------------------------------------------- catalog

        public static readonly IReadOnlyList<SellerOffer> Catalog = new List<SellerOffer>
        {
            new SellerOffer("celular_gama_alta", "celular de gama alta", "Kevin", 350_000L, 24, 2.5m, 3_500L, 15_000L),
            new SellerOffer("pantalla_65", "pantalla de 65 pulgadas", "Kevin", 650_000L, 36, 2.9m, 4_500L, 25_000L),
            new SellerOffer("refri_doble_puerta", "refrigeradora de doble puerta", "Doña Marta", 450_000L, 30, 2.7m, 0L, 20_000L),
            new SellerOffer("moto_125", "moto 125 cc", "Doña Marta", 1_200_000L, 48, 2.2m, 9_000L, 40_000L)
        };

        /// <summary>The offer for a month: they rotate, so a run sees different products.</summary>
        public static SellerOffer ForMonth(int monthsPlayed)
        {
            int index = ((monthsPlayed % Catalog.Count) + Catalog.Count) % Catalog.Count;
            return Catalog[index];
        }

        public static SellerOffer Find(string offerId)
        {
            foreach (var offer in Catalog)
            {
                if (offer.OfferId == offerId)
                {
                    return offer;
                }
            }

            return null;
        }
    }
}

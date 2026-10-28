using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using EventType = ColonQuest.Finance.EventType;

namespace ColonQuest.Game
{
    public sealed class NegotiationTurn
    {
        public string Player = string.Empty;
        public string Seller = string.Empty;
        public bool FromLocalSeller;
    }

    /// <summary>What to show after a deal: the real cost of the installments and how well the player asked.</summary>
    public sealed class DealReport
    {
        public string Item = string.Empty;
        public bool Bought;
        public long CashPrice;
        public long TotalCost;
        public long ExtraOverCash;
        public int QuestionsAsked;
        public int QuestionsTotal;
        public bool AskedAboutTotalCost;
        public bool AskedAboutHiddenFees;

        /// <summary>Why the deal did not go through, when it was refused after the player said yes.</summary>
        public string Note = string.Empty;
    }

    /// <summary>
    /// One haggling session with a seller: the conversation, which key questions the player has
    /// asked so far and which facts were revealed. It has no Unity code, so it is tested alone.
    /// The deal itself is a <see cref="GameEvent"/> that goes through the finance engine.
    /// </summary>
    public sealed class Negotiation
    {
        public const string BuyChoice = "buy";
        public const string SkipChoice = "skip";

        private readonly List<NegotiationTurn> turns = new List<NegotiationTurn>();
        private readonly List<KeyQuestion> asked = new List<KeyQuestion>();
        private readonly List<SellerFact> revealed = new List<SellerFact>();

        public const string StoreVenue = "Tienda";

        public SellerOffer Offer { get; }

        /// <summary>Where the conversation happens, for the title of the window.</summary>
        public string Venue { get; set; } = StoreVenue;

        /// <summary>
        /// True for an offer the backend does not know (the car agency): its turns never go to the server, the
        /// local seller answers them all.
        /// </summary>
        public bool IsLocalOnly => SellerOffer.Find(Offer.OfferId) == null;

        public DealStatus Status { get; private set; } = DealStatus.Open;
        public IReadOnlyList<NegotiationTurn> Turns => turns;
        public IReadOnlyList<KeyQuestion> KeyQuestionsAsked => asked;
        public IReadOnlyList<SellerFact> RevealedFacts => revealed;

        public Negotiation(SellerOffer offer)
        {
            Offer = offer;
        }

        public int KeyQuestionCount => System.Enum.GetValues(typeof(KeyQuestion)).Length;

        public bool IsClosed => Status != DealStatus.Open;

        public string Opening => LocalSeller.Greeting(Offer);

        /// <summary>Adds the seller's answer. Facts and questions accumulate over the whole conversation.</summary>
        public void Record(string playerText, NegotiationReply reply, bool fromLocalSeller)
        {
            turns.Add(new NegotiationTurn { Player = playerText, Seller = reply.NpcReply, FromLocalSeller = fromLocalSeller });
            foreach (var question in reply.KeyQuestionsAsked)
            {
                if (!asked.Contains(question))
                {
                    asked.Add(question);
                }
            }

            foreach (var fact in reply.RevealedFacts)
            {
                if (!revealed.Contains(fact))
                {
                    revealed.Add(fact);
                }
            }

            Status = reply.DealStatus;
        }

        /// <summary>Closes the conversation from a button instead of from the typed text.</summary>
        public void Close(DealStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// The deal as an event: buying costs the opening commission in cash and adds the installment with
        /// the insurance. The engine checks both against the contract ranges, like any other event.
        /// </summary>
        public GameEvent ToEvent()
        {
            var buy = new List<Effect>();
            if (Offer.Commission > 0L)
            {
                buy.Add(new Effect(EffectKind.Expense, Offer.Commission, MoneySource.Cash));
            }

            buy.Add(new Effect(EffectKind.NewInstallment, Offer.MonthlyCharge, null, Offer.TermMonths));
            buy.Add(new Effect(EffectKind.WellbeingChange, 5L));

            return new GameEvent
            {
                EventId = "evt_deal_" + Offer.OfferId,
                Type = EventType.Offer,
                Title = Capitalize(Offer.Item),
                Narrative = LocalSeller.Greeting(Offer),
                Npc = new Npc("seller_" + Offer.SellerName.ToLowerInvariant().Replace(' ', '_'), Offer.SellerName, "seller"),
                TeachingConcept = TeachingConcept.InstallmentsTotalCost,
                Choices = new List<Choice>
                {
                    new Choice(BuyChoice, "Comprar a cuotas", buy),
                    new Choice(SkipChoice, "No comprar", new List<Effect>())
                }
            };
        }

        public DealReport Report(bool bought)
        {
            return new DealReport
            {
                Item = Offer.Item,
                Bought = bought,
                CashPrice = Offer.CashPrice,
                TotalCost = Offer.TotalCost,
                ExtraOverCash = Offer.ExtraOverCash,
                QuestionsAsked = asked.Count,
                QuestionsTotal = KeyQuestionCount,
                AskedAboutTotalCost = asked.Contains(KeyQuestion.TotalCost),
                AskedAboutHiddenFees = asked.Contains(KeyQuestion.HiddenFees) || asked.Contains(KeyQuestion.Insurance)
            };
        }

        private static string Capitalize(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}

using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Api
{
    public enum DealStatus
    {
        Open,
        Accepted,
        Rejected,
        WalkedAway
    }

    /// <summary>Facts on the seller's hidden sheet that the NPC can reveal.</summary>
    public enum SellerFact
    {
        CashPrice,
        MonthlyPayment,
        TermMonths,
        InterestRate,
        InsuranceFee,
        Commission,
        TotalCost
    }

    /// <summary>Questions a financially literate player should ask. They feed the impact metric.</summary>
    public enum KeyQuestion
    {
        TotalCost,
        HiddenFees,
        InterestRate,
        Term,
        CashPrice,
        Insurance
    }

    public sealed class NegotiationReply
    {
        public string Transcript = string.Empty;
        public string NpcReply = string.Empty;
        public List<SellerFact> RevealedFacts = new List<SellerFact>();
        public List<KeyQuestion> KeyQuestionsAsked = new List<KeyQuestion>();
        public DealStatus DealStatus;
    }

    public sealed class SessionSummary
    {
        public int MonthsPlayed;
        public int Decisions;
        public List<TeachingConcept> ConceptsTaught = new List<TeachingConcept>();
        public int Negotiations;
        public int DealsAccepted;
        public List<KeyQuestion> KeyQuestionsAsked = new List<KeyQuestion>();
        public long TotalDebt;
        public long NetWorth;
        public string Narrative = string.Empty;

        /// <summary>"model" when the AI wrote the narrative, "fallback" when the backend used its template.</summary>
        public string Source = string.Empty;
    }

    public sealed class HealthStatus
    {
        public string Status = string.Empty;
        public string Version = string.Empty;
        public string Database = string.Empty;

        /// <summary>"normal", "downgraded" or "fallback_only"; empty when the backend does not report a budget.</summary>
        public string BudgetMode = string.Empty;

        /// <summary>Today's spend and limit in dollars: the Meta API, and Baseten for the open-weight sellers.</summary>
        public float MetaSpentUsd;
        public float MetaBudgetUsd;
        public float OpenWeightsSpentUsd;
        public float OpenWeightsBudgetUsd;
    }

    /// <summary>Where a value came from: the backend, or the local catalog used when the backend failed.</summary>
    public enum ResultSource
    {
        Remote,
        Fallback
    }

    /// <summary>The outcome of one API call. Errors are values, never exceptions, so a bad response cannot break the game.</summary>
    public sealed class ApiResult<T>
    {
        public bool Ok;
        public T Value;
        public string Error = string.Empty;
        public long StatusCode;
        public ResultSource Source;

        /// <summary>Who wrote it according to the backend ("agent", "model", "cache", "fallback"...); empty when it does not say.</summary>
        public string Origin = string.Empty;

        public static ApiResult<T> Success(T value, ResultSource source = ResultSource.Remote, long statusCode = 200)
        {
            return new ApiResult<T> { Ok = true, Value = value, Source = source, StatusCode = statusCode };
        }

        public static ApiResult<T> Failure(string error, long statusCode = 0)
        {
            return new ApiResult<T> { Ok = false, Error = error ?? string.Empty, StatusCode = statusCode };
        }
    }
}

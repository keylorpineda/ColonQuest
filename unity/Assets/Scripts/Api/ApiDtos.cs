using System;
using System.Collections.Generic;

namespace ColonQuest.Api
{
    // Wire shapes of the backend contract (shared/schemas). Field names are the camelCase
    // names on the wire so that JsonUtility reads and writes them directly. Enums travel as
    // snake_case strings and optional values as an empty string or zero; ApiMapper turns all
    // of this into the finance engine types and back.

    [Serializable]
    public sealed class CreditCardDto
    {
        public long balance;
        public long limit;
        public float monthlyRatePct;
        public float minPaymentPct;
    }

    [Serializable]
    public sealed class InstallmentDto
    {
        public string item = string.Empty;
        public long monthly;
        public int monthsLeft;
    }

    [Serializable]
    public sealed class InformalLoanDto
    {
        public string lender = string.Empty;
        public long balance;
        public float monthlyRatePct;
    }

    [Serializable]
    public sealed class PlayerStateDto
    {
        public string characterName = string.Empty;
        public int week = 1;
        public string jobPosition = string.Empty;
        public int month;
        public long salaryNet;
        public long cash;
        public long savings;
        public long emergencyFund;
        public CreditCardDto creditCard = new CreditCardDto();
        public List<InstallmentDto> installments = new List<InstallmentDto>();
        public List<InformalLoanDto> informalLoans = new List<InformalLoanDto>();
        public List<string> history = new List<string>();
        public int wellbeing;
        public string language = "es";
    }

    [Serializable]
    public sealed class NpcDto
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string role = string.Empty;
    }

    [Serializable]
    public sealed class EffectDto
    {
        public string kind = string.Empty;
        public long amount;
        public string source = string.Empty;
        public int months;
    }

    [Serializable]
    public sealed class ChoiceDto
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public List<EffectDto> effects = new List<EffectDto>();
    }

    [Serializable]
    public sealed class GameEventDto
    {
        public string eventId = string.Empty;
        public string type = string.Empty;
        public string title = string.Empty;
        public string narrative = string.Empty;
        public NpcDto npc = new NpcDto();
        public string imageKey = string.Empty;
        public List<ChoiceDto> choices = new List<ChoiceDto>();
        public string teachingConcept = string.Empty;
    }

    [Serializable]
    public sealed class FallbackCatalogDto
    {
        public List<GameEventDto> events = new List<GameEventDto>();
    }

    [Serializable]
    public sealed class CreateSessionRequestDto
    {
        public string classCode;
        public string language = "es";
    }

    [Serializable]
    public sealed class SessionCreatedDto
    {
        public string sessionId = string.Empty;
    }

    [Serializable]
    public sealed class DecisionRequestDto
    {
        public int month;
        public string eventId = string.Empty;
        public string teachingConcept = string.Empty;
        public string choiceId = string.Empty;
        public PlayerStateDto stateAfter = new PlayerStateDto();
    }

    [Serializable]
    public sealed class NegotiationRequestDto
    {
        public string offerId = string.Empty;
        public string text = string.Empty;
        public string language = "es";
    }

    [Serializable]
    public sealed class NegotiationReplyDto
    {
        public string transcript = string.Empty;
        public string npcReply = string.Empty;
        public List<string> revealedFacts = new List<string>();
        public List<string> keyQuestionsAsked = new List<string>();
        public string dealStatus = string.Empty;
    }

    [Serializable]
    public sealed class TranscriptDto
    {
        public string transcript = string.Empty;
    }

    [Serializable]
    public sealed class SessionSummaryDto
    {
        public int monthsPlayed;
        public int decisions;
        public List<string> conceptsTaught = new List<string>();
        public int negotiations;
        public int dealsAccepted;
        public List<string> keyQuestionsAsked = new List<string>();
        public long totalDebt;
        public long netWorth;
        public string narrative = string.Empty;
        public string source = string.Empty;
    }

    [Serializable]
    public sealed class BudgetDto
    {
        public float spentUsd;
        public float budgetUsd;
        public float remainingUsd;
        public string mode = string.Empty;
    }

    [Serializable]
    public sealed class HealthDto
    {
        public string status = string.Empty;
        public string version = string.Empty;
        public string database = string.Empty;
        public BudgetDto budget = new BudgetDto();
    }
}

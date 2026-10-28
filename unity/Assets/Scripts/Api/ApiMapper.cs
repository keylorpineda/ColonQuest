using System.Collections.Generic;
using ColonQuest.Finance;

namespace ColonQuest.Api
{
    /// <summary>
    /// Turns the wire DTOs into the finance engine types and back. Money stays in whole
    /// colones (long). A response that breaks the contract throws <see cref="ApiFormatException"/>,
    /// which the service answers with a fallback event.
    /// </summary>
    public static class ApiMapper
    {
        public const int MinChoices = 2;
        public const int MaxChoices = 4;
        public const int MaxEffectsPerChoice = 5;

        // ---------------------------------------------------------------- player state

        public static PlayerStateDto ToDto(PlayerState state)
        {
            var dto = new PlayerStateDto
            {
                month = state.Month,
                salaryNet = state.SalaryNet,
                cash = state.Cash,
                savings = state.Savings,
                emergencyFund = state.EmergencyFund,
                wellbeing = state.Wellbeing,
                creditCard = new CreditCardDto
                {
                    balance = state.CreditCard.Balance,
                    limit = state.CreditCard.Limit,
                    monthlyRatePct = (float)state.CreditCard.MonthlyRatePct,
                    minPaymentPct = (float)state.CreditCard.MinPaymentPct
                }
            };

            foreach (var installment in state.Installments)
            {
                dto.installments.Add(new InstallmentDto { item = installment.Item, monthly = installment.Monthly, monthsLeft = installment.MonthsLeft });
            }

            foreach (var loan in state.InformalLoans)
            {
                dto.informalLoans.Add(new InformalLoanDto { lender = loan.Lender, balance = loan.Balance, monthlyRatePct = (float)loan.MonthlyRatePct });
            }

            dto.history.AddRange(state.History);
            return dto;
        }

        public static DecisionRequestDto ToDto(int month, string eventId, TeachingConcept concept, string choiceId, PlayerState stateAfter)
        {
            return new DecisionRequestDto
            {
                month = month,
                eventId = eventId,
                teachingConcept = WireEnum<TeachingConcept>.ToWire(concept),
                choiceId = choiceId,
                stateAfter = ToDto(stateAfter)
            };
        }

        // ---------------------------------------------------------------- game event

        /// <summary>Maps and validates an event. Every effect must be inside the ranges the finance engine accepts.</summary>
        public static GameEvent FromDto(GameEventDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.eventId))
            {
                throw new ApiFormatException("The event has no id.");
            }

            var type = WireEnum<EventType>.Parse(dto.type, "event type");
            var gameEvent = new GameEvent
            {
                EventId = dto.eventId,
                Type = type,
                Title = dto.title ?? string.Empty,
                Narrative = dto.narrative ?? string.Empty,
                ImageKey = string.IsNullOrEmpty(dto.imageKey) ? null : dto.imageKey,
                TeachingConcept = WireEnum<TeachingConcept>.Parse(dto.teachingConcept, "teaching concept")
            };

            if (dto.npc != null && !string.IsNullOrEmpty(dto.npc.id))
            {
                gameEvent.Npc = new Npc(dto.npc.id, dto.npc.name, dto.npc.role);
            }

            int choiceCount = dto.choices?.Count ?? 0;
            if (choiceCount < MinChoices || choiceCount > MaxChoices)
            {
                throw new ApiFormatException($"The event needs {MinChoices} to {MaxChoices} choices, it has {choiceCount}.");
            }

            var ids = new HashSet<string>();
            foreach (var choiceDto in dto.choices)
            {
                if (string.IsNullOrEmpty(choiceDto.id) || !ids.Add(choiceDto.id))
                {
                    throw new ApiFormatException("Choice ids must be present and unique.");
                }

                if ((choiceDto.effects?.Count ?? 0) > MaxEffectsPerChoice)
                {
                    throw new ApiFormatException($"A choice has more than {MaxEffectsPerChoice} effects.");
                }

                var effects = new List<Effect>();
                foreach (var effectDto in choiceDto.effects ?? new List<EffectDto>())
                {
                    var effect = FromDto(effectDto);
                    if (!EffectLimits.TryValidateEffect(type, effect, out string error))
                    {
                        throw new ApiFormatException(error);
                    }

                    effects.Add(effect);
                }

                gameEvent.Choices.Add(new Choice(choiceDto.id, choiceDto.label, effects));
            }

            return gameEvent;
        }

        private static Effect FromDto(EffectDto dto)
        {
            var kind = WireEnum<EffectKind>.Parse(dto.kind, "effect kind");
            MoneySource? source = null;
            if (!string.IsNullOrEmpty(dto.source))
            {
                source = WireEnum<MoneySource>.Parse(dto.source, "money source");
            }

            int? months = dto.months > 0 ? dto.months : (int?)null;
            return new Effect(kind, dto.amount, source, months);
        }

        // ---------------------------------------------------------------- replies

        public static NegotiationReply FromDto(NegotiationReplyDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.npcReply))
            {
                throw new ApiFormatException("The negotiation reply is empty.");
            }

            return new NegotiationReply
            {
                Transcript = dto.transcript ?? string.Empty,
                NpcReply = dto.npcReply,
                RevealedFacts = WireEnum<SellerFact>.ParseKnown(dto.revealedFacts),
                KeyQuestionsAsked = WireEnum<KeyQuestion>.ParseKnown(dto.keyQuestionsAsked),
                DealStatus = WireEnum<DealStatus>.Parse(dto.dealStatus, "deal status")
            };
        }

        public static SessionSummary FromDto(SessionSummaryDto dto)
        {
            if (dto == null)
            {
                throw new ApiFormatException("The summary is empty.");
            }

            return new SessionSummary
            {
                MonthsPlayed = dto.monthsPlayed,
                Decisions = dto.decisions,
                ConceptsTaught = WireEnum<TeachingConcept>.ParseKnown(dto.conceptsTaught),
                Negotiations = dto.negotiations,
                DealsAccepted = dto.dealsAccepted,
                KeyQuestionsAsked = WireEnum<KeyQuestion>.ParseKnown(dto.keyQuestionsAsked),
                TotalDebt = dto.totalDebt,
                NetWorth = dto.netWorth,
                Narrative = dto.narrative ?? string.Empty,
                Source = dto.source ?? string.Empty
            };
        }

        public static HealthStatus FromDto(HealthDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.status))
            {
                throw new ApiFormatException("The health reply is empty.");
            }

            return new HealthStatus
            {
                Status = dto.status,
                Version = dto.version ?? string.Empty,
                Database = dto.database ?? string.Empty,
                BudgetMode = dto.budget?.mode ?? string.Empty
            };
        }
    }
}

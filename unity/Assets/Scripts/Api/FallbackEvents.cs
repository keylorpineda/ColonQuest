using System;
using System.Collections.Generic;
using ColonQuest.Finance;
using UnityEngine;
using EventType = ColonQuest.Finance.EventType;

namespace ColonQuest.Api
{
    /// <summary>
    /// The hand-written events the game plays when the backend fails, takes longer than the
    /// timeout or answers something that breaks the contract. The catalog is a copy of
    /// <c>server/app/game_master/fallback_events.json</c> kept in <c>Resources/fallback_events.json</c>.
    /// Every event is validated with the same rules as a remote one, so a bad copy cannot reach the player.
    /// </summary>
    public static class FallbackEvents
    {
        private const string ResourceName = "fallback_events";

        private static List<GameEvent> catalog;

        public static IReadOnlyList<GameEvent> Catalog
        {
            get
            {
                if (catalog == null)
                {
                    var asset = Resources.Load<TextAsset>(ResourceName);
                    catalog = asset != null ? Parse(asset.text) : new List<GameEvent>();
                    if (catalog.Count == 0)
                    {
                        catalog.Add(LastResort());
                    }
                }

                return catalog;
            }
        }

        /// <summary>Reads a catalog and keeps only the events that pass validation.</summary>
        public static List<GameEvent> Parse(string json)
        {
            var events = new List<GameEvent>();
            FallbackCatalogDto dto;
            try
            {
                dto = JsonUtility.FromJson<FallbackCatalogDto>(json);
            }
            catch (ArgumentException)
            {
                return events;
            }

            foreach (var eventDto in dto?.events ?? new List<GameEventDto>())
            {
                try
                {
                    events.Add(ApiMapper.FromDto(eventDto));
                }
                catch (ApiFormatException)
                {
                    // A broken entry is skipped; the rest of the catalog still works.
                }
            }

            return events;
        }

        /// <summary>
        /// Picks the event for this month: the one that fits the player's situation when there is one
        /// (a maxed out card calls the lender, big savings call the pyramid), otherwise the next
        /// event in the catalog that was not played yet.
        /// </summary>
        public static GameEvent Next(PlayerState state, ISet<string> seen)
        {
            var events = Catalog;
            seen = seen ?? new HashSet<string>();

            var preferred = Preferred(state);
            if (preferred.HasValue)
            {
                foreach (var candidate in events)
                {
                    if (candidate.TeachingConcept == preferred.Value && !seen.Contains(candidate.EventId))
                    {
                        return candidate;
                    }
                }
            }

            int start = Math.Max(0, state.Month - 1) % events.Count;
            for (int i = 0; i < events.Count; i++)
            {
                var candidate = events[(start + i) % events.Count];
                if (!seen.Contains(candidate.EventId))
                {
                    return candidate;
                }
            }

            return events[start];
        }

        private static TeachingConcept? Preferred(PlayerState state)
        {
            if (state.CreditCard != null && state.CreditCard.Limit > 0 && state.CreditCard.Balance * 10L >= state.CreditCard.Limit * 7L)
            {
                return TeachingConcept.InformalLending;
            }

            if (state.SalaryNet > 0 && state.Savings >= state.SalaryNet)
            {
                return TeachingConcept.PyramidScheme;
            }

            return null;
        }

        /// <summary>Used only if the catalog asset is missing, so the player is never left without an event.</summary>
        private static GameEvent LastResort()
        {
            return new GameEvent
            {
                EventId = "evt_marchamo",
                Type = EventType.Expense,
                Title = "Toca pagar el marchamo",
                Narrative = "Llegó el recibo del marchamo y hay que pagarlo antes de que se venza.",
                TeachingConcept = TeachingConcept.Budget,
                Choices = new List<Choice>
                {
                    new Choice("pay_cash", "Pagarlo de contado", new List<Effect> { new Effect(EffectKind.Expense, 60000L, MoneySource.Cash) }),
                    new Choice("pay_card", "Pasarlo a la tarjeta", new List<Effect> { new Effect(EffectKind.Expense, 60000L, MoneySource.CreditCard) })
                }
            };
        }
    }
}

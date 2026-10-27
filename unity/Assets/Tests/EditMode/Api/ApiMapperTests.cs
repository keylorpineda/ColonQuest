using System;
using System.Collections.Generic;
using ColonQuest.Finance;
using NUnit.Framework;
using UnityEngine;
using EventType = ColonQuest.Finance.EventType;

namespace ColonQuest.Api.Tests
{
    [TestFixture]
    public sealed class ApiMapperTests
    {
        // The example event of the contract, with explicit nulls where the backend may send them.
        private const string EventJson = @"{
            ""eventId"": ""evt_moto_dañada"",
            ""type"": ""emergency"",
            ""title"": ""Se te dañó la moto"",
            ""narrative"": ""Texto corto."",
            ""npc"": { ""id"": ""mecanico_don_rafa"", ""name"": ""Don Rafa"", ""role"": ""mechanic"" },
            ""imageKey"": ""scene_taller_mecanico"",
            ""choices"": [
                { ""id"": ""pay_cash"", ""label"": ""Pagar con efectivo"",
                  ""effects"": [ { ""kind"": ""expense"", ""amount"": 85000, ""source"": ""cash"", ""months"": null } ] },
                { ""id"": ""card"", ""label"": ""Pasarlo a la tarjeta"",
                  ""effects"": [ { ""kind"": ""expense"", ""amount"": 85000, ""source"": ""credit_card"" } ] }
            ],
            ""teachingConcept"": ""emergency_fund""
        }";

        private static GameEvent Map(string json)
        {
            return ApiMapper.FromDto(JsonUtility.FromJson<GameEventDto>(json));
        }

        [Test]
        public void WireEnum_UsesSnakeCaseNamesOfTheContract()
        {
            Assert.AreEqual("macro_news", WireEnum<EventType>.ToWire(EventType.MacroNews));
            Assert.AreEqual("new_informal_loan", WireEnum<EffectKind>.ToWire(EffectKind.NewInformalLoan));
            Assert.AreEqual("credit_card", WireEnum<MoneySource>.ToWire(MoneySource.CreditCard));
            Assert.AreEqual("installments_total_cost", WireEnum<TeachingConcept>.ToWire(TeachingConcept.InstallmentsTotalCost));
            Assert.AreEqual("walked_away", WireEnum<DealStatus>.ToWire(DealStatus.WalkedAway));
        }

        [Test]
        public void WireEnum_RoundTripsEveryValueOfTheClosedEnums()
        {
            foreach (EventType value in Enum.GetValues(typeof(EventType)))
            {
                Assert.AreEqual(value, WireEnum<EventType>.Parse(WireEnum<EventType>.ToWire(value), "type"));
            }

            foreach (TeachingConcept value in Enum.GetValues(typeof(TeachingConcept)))
            {
                Assert.AreEqual(value, WireEnum<TeachingConcept>.Parse(WireEnum<TeachingConcept>.ToWire(value), "concept"));
            }

            foreach (KeyQuestion value in Enum.GetValues(typeof(KeyQuestion)))
            {
                Assert.AreEqual(value, WireEnum<KeyQuestion>.Parse(WireEnum<KeyQuestion>.ToWire(value), "question"));
            }
        }

        [Test]
        public void ParseKnown_SkipsValuesThisClientDoesNotKnow()
        {
            var parsed = WireEnum<SellerFact>.ParseKnown(new[] { "insurance_fee", "from_the_future", "total_cost" });

            CollectionAssert.AreEqual(new[] { SellerFact.InsuranceFee, SellerFact.TotalCost }, parsed);
        }

        [Test]
        public void Event_MapsFromTheContractExample()
        {
            var gameEvent = Map(EventJson);

            Assert.AreEqual("evt_moto_dañada", gameEvent.EventId);
            Assert.AreEqual(EventType.Emergency, gameEvent.Type);
            Assert.AreEqual(TeachingConcept.EmergencyFund, gameEvent.TeachingConcept);
            Assert.AreEqual("Don Rafa", gameEvent.Npc.Name);
            Assert.AreEqual("scene_taller_mecanico", gameEvent.ImageKey);
            Assert.AreEqual(2, gameEvent.Choices.Count);

            var effect = gameEvent.Choices[0].Effects[0];
            Assert.AreEqual(EffectKind.Expense, effect.Kind);
            Assert.AreEqual(85000L, effect.Amount);
            Assert.AreEqual(MoneySource.Cash, effect.Source);
            Assert.IsNull(effect.Months, "null months in the JSON means no months");
        }

        [Test]
        public void Event_WithoutNpcOrImageMapsToNulls()
        {
            string json = EventJson.Replace(@"""npc"": { ""id"": ""mecanico_don_rafa"", ""name"": ""Don Rafa"", ""role"": ""mechanic"" },", @"""npc"": null,")
                .Replace(@"""imageKey"": ""scene_taller_mecanico"",", @"""imageKey"": null,");

            var gameEvent = Map(json);

            Assert.IsNull(gameEvent.Npc);
            Assert.IsNull(gameEvent.ImageKey);
        }

        [Test]
        public void Event_WithAnEffectOutOfRangeIsRejected()
        {
            // An emergency may cost at most 250 000 colones.
            string json = EventJson.Replace("85000", "900000");

            Assert.Throws<ApiFormatException>(() => Map(json));
        }

        [Test]
        public void Event_WithAnUnknownEnumIsRejected()
        {
            Assert.Throws<ApiFormatException>(() => Map(EventJson.Replace("emergency\",", "volcano\",")));
            Assert.Throws<ApiFormatException>(() => Map(EventJson.Replace("\"cash\"", "\"bitcoin\"")));
        }

        [Test]
        public void Event_NeedsBetweenTwoAndFourChoices()
        {
            var dto = JsonUtility.FromJson<GameEventDto>(EventJson);
            dto.choices.RemoveAt(1);

            Assert.Throws<ApiFormatException>(() => ApiMapper.FromDto(dto));
        }

        [Test]
        public void Event_RejectsDuplicateChoiceIds()
        {
            var dto = JsonUtility.FromJson<GameEventDto>(EventJson);
            dto.choices[1].id = dto.choices[0].id;

            Assert.Throws<ApiFormatException>(() => ApiMapper.FromDto(dto));
        }

        [Test]
        public void Installment_NeedsMonthsInsideTheAllowedRange()
        {
            string json = EventJson.Replace("\"type\": \"emergency\"", "\"type\": \"offer\"").Replace(
                @"""kind"": ""expense"", ""amount"": 85000, ""source"": ""cash"", ""months"": null",
                @"""kind"": ""new_installment"", ""amount"": 18500, ""months"": 24");

            Assert.AreEqual(24, Map(json).Choices[0].Effects[0].Months);
            Assert.Throws<ApiFormatException>(() => Map(json.Replace("\"months\": 24", "\"months\": 99")));
        }

        [Test]
        public void PlayerState_SerializesToTheCamelCaseContractWithWholeColones()
        {
            var state = new PlayerState
            {
                Month = 3,
                SalaryNet = 450000L,
                Cash = 120000L,
                Savings = 50000L,
                CreditCard = new CreditCard(180000L, 300000L, 3.5m, 10m),
                Wellbeing = 62
            };
            state.Installments.Add(new Installment("Celular", 25000L, 33));
            state.History.Add("paid_minimum_only");

            string json = JsonUtility.ToJson(ApiMapper.ToDto(state));

            StringAssert.Contains("\"salaryNet\":450000", json);
            StringAssert.Contains("\"emergencyFund\":0", json);
            StringAssert.Contains("\"monthsLeft\":33", json);
            StringAssert.Contains("\"monthlyRatePct\":3.5", json);
            StringAssert.Contains("\"history\":[\"paid_minimum_only\"]", json);
            StringAssert.DoesNotContain("450000.", json);
        }

        [Test]
        public void Decision_CarriesTheConceptAndTheStateAfter()
        {
            var state = new PlayerState { Month = 4, Cash = 5000L };

            var dto = ApiMapper.ToDto(4, "evt_x", TeachingConcept.MinimumPayment, "pay_min", state);

            Assert.AreEqual("minimum_payment", dto.teachingConcept);
            Assert.AreEqual("pay_min", dto.choiceId);
            Assert.AreEqual(5000L, dto.stateAfter.cash);
        }

        [Test]
        public void NegotiationReply_MapsFactsQuestionsAndStatus()
        {
            var reply = ApiMapper.FromDto(JsonUtility.FromJson<NegotiationReplyDto>(@"{
                ""transcript"": ""¿Cuánto pago en total?"",
                ""npcReply"": ""Con el seguro son 31 500 al mes."",
                ""revealedFacts"": [""insurance_fee"", ""total_cost""],
                ""keyQuestionsAsked"": [""total_cost"", ""hidden_fees""],
                ""dealStatus"": ""open""
            }"));

            Assert.AreEqual(DealStatus.Open, reply.DealStatus);
            CollectionAssert.AreEqual(new[] { SellerFact.InsuranceFee, SellerFact.TotalCost }, reply.RevealedFacts);
            CollectionAssert.AreEqual(new[] { KeyQuestion.TotalCost, KeyQuestion.HiddenFees }, reply.KeyQuestionsAsked);
        }

        [Test]
        public void NegotiationReply_WithoutAnNpcReplyIsRejected()
        {
            Assert.Throws<ApiFormatException>(() => ApiMapper.FromDto(JsonUtility.FromJson<NegotiationReplyDto>(@"{ ""dealStatus"": ""open"" }")));
        }
    }
}

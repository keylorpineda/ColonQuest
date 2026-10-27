using System;
using System.Collections;
using System.Collections.Generic;
using ColonQuest.Finance;
using NUnit.Framework;

namespace ColonQuest.Api.Tests
{
    /// <summary>A transport that answers from a queue, without a network.</summary>
    internal sealed class FakeTransport : IHttpTransport
    {
        public readonly Queue<HttpResponseData> Replies = new Queue<HttpResponseData>();
        public readonly List<HttpRequestData> Requests = new List<HttpRequestData>();

        public static HttpResponseData Ok(string json) => new HttpResponseData { StatusCode = 200, Text = json };

        public static HttpResponseData Status(long code) => new HttpResponseData { StatusCode = code, Text = "{}" };

        public static HttpResponseData Timeout() => new HttpResponseData { TimedOut = true, NetworkError = "timeout" };

        public IEnumerator Send(HttpRequestData request, Action<HttpResponseData> done)
        {
            Requests.Add(request);
            done(Replies.Count > 0 ? Replies.Dequeue() : Status(500));
            yield break;
        }
    }

    [TestFixture]
    public sealed class ApiServiceTests
    {
        private const string SessionId = "11111111-2222-3333-4444-555555555555";

        private const string EventJson = @"{
            ""eventId"": ""evt_remote"", ""type"": ""expense"", ""title"": ""Remoto"", ""narrative"": ""Texto."",
            ""choices"": [
                { ""id"": ""a"", ""label"": ""Pagar"", ""effects"": [ { ""kind"": ""expense"", ""amount"": 30000, ""source"": ""cash"" } ] },
                { ""id"": ""b"", ""label"": ""No pagar"", ""effects"": [] }
            ],
            ""teachingConcept"": ""budget""
        }";

        private FakeTransport transport;
        private ApiService service;

        [SetUp]
        public void SetUp()
        {
            transport = new FakeTransport();
            service = new ApiService(transport, new ApiConfig { BaseUrl = "http://test/api" });
        }

        /// <summary>Runs a coroutine to the end, including the coroutines it yields.</summary>
        private static void Run(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested)
                {
                    Run(nested);
                }
            }
        }

        private void StartSession()
        {
            transport.Replies.Enqueue(FakeTransport.Ok("{\"sessionId\":\"" + SessionId + "\"}"));
            Run(service.CreateSession(null, _ => { }));
        }

        private static PlayerState State(int month = 1)
        {
            return new PlayerState { Month = month, SalaryNet = 450000L, Cash = 100000L };
        }

        [Test]
        public void CreateSession_StoresTheSessionId()
        {
            ApiResult<string> result = null;
            transport.Replies.Enqueue(FakeTransport.Ok("{\"sessionId\":\"" + SessionId + "\"}"));

            Run(service.CreateSession("ABC123", r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(SessionId, service.SessionId);
            Assert.AreEqual("http://test/api/sessions", transport.Requests[0].Url);
            Assert.AreEqual("{\"classCode\":\"ABC123\"}", transport.Requests[0].JsonBody);
        }

        [Test]
        public void CreateSession_WithoutAClassSendsAnEmptyBody()
        {
            StartSession();

            Assert.AreEqual("{}", transport.Requests[0].JsonBody);
        }

        [Test]
        public void CreateSession_FailsQuietlyWhenTheServerIsDown()
        {
            ApiResult<string> result = null;
            transport.Replies.Enqueue(FakeTransport.Status(503));

            Run(service.CreateSession(null, r => result = r));

            Assert.IsFalse(result.Ok);
            Assert.IsFalse(service.HasSession);
        }

        [Test]
        public void Event_ComesFromTheBackendWhenItAnswers()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(EventJson));
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(ResultSource.Remote, result.Source);
            Assert.AreEqual("evt_remote", result.Value.EventId);
            Assert.AreEqual("http://test/api/sessions/" + SessionId + "/event", transport.Requests[1].Url);
            StringAssert.Contains("\"salaryNet\":450000", transport.Requests[1].JsonBody);
        }

        [Test]
        public void Event_FallsBackWhenTheBackendFails()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Status(500));
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.IsTrue(result.Ok, "the monthly event never fails");
            Assert.AreEqual(ResultSource.Fallback, result.Source);
            Assert.IsNotNull(result.Value);
        }

        [Test]
        public void Event_FallsBackWhenTheRequestTimesOut()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Timeout());
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.AreEqual(ResultSource.Fallback, result.Source);
            Assert.AreEqual("timeout", result.Error);
        }

        [Test]
        public void Event_UsesTheEightSecondTimeout()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(EventJson));

            Run(service.RequestEvent(State(), _ => { }));

            Assert.AreEqual(8f, transport.Requests[1].TimeoutSeconds);
        }

        [Test]
        public void Event_FallsBackWhenTheJsonIsBroken()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok("this is not json"));
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.AreEqual(ResultSource.Fallback, result.Source);
        }

        [Test]
        public void Event_FallsBackWhenAnEffectBreaksTheContractRanges()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(EventJson.Replace("30000", "99999999")));
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.AreEqual(ResultSource.Fallback, result.Source);
        }

        [Test]
        public void Event_WithoutASessionIsAlwaysLocal()
        {
            ApiResult<GameEvent> result = null;

            Run(service.RequestEvent(State(), r => result = r));

            Assert.AreEqual(ResultSource.Fallback, result.Source);
            Assert.AreEqual(0, transport.Requests.Count, "no request is made without a session");
        }

        [Test]
        public void Event_FallbackDoesNotRepeatTheSameEventInARow()
        {
            ApiResult<GameEvent> first = null;
            ApiResult<GameEvent> second = null;

            Run(service.RequestEvent(State(1), r => first = r));
            Run(service.RequestEvent(State(1), r => second = r));

            Assert.AreNotEqual(first.Value.EventId, second.Value.EventId);
        }

        [Test]
        public void Decision_IsPostedWithTheStateAfter()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok("{\"decisionId\":1}"));
            var gameEvent = FallbackEvents.Catalog[0];
            ApiResult<bool> result = null;

            Run(service.RecordDecision(2, gameEvent, gameEvent.Choices[0].Id, State(2), r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("http://test/api/sessions/" + SessionId + "/decisions", transport.Requests[1].Url);
            StringAssert.Contains("\"stateAfter\"", transport.Requests[1].JsonBody);
        }

        [Test]
        public void Decision_FailureIsReportedButDoesNotThrow()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Status(500));
            var gameEvent = FallbackEvents.Catalog[0];
            ApiResult<bool> result = null;

            Run(service.RecordDecision(2, gameEvent, gameEvent.Choices[0].Id, State(2), r => result = r));

            Assert.IsFalse(result.Ok);
        }

        [Test]
        public void Negotiation_SendsTheTextAndMapsTheReply()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(
                "{\"transcript\":\"hola\",\"npcReply\":\"Mae, son 31 500.\",\"revealedFacts\":[\"monthly_payment\"],\"keyQuestionsAsked\":[\"total_cost\"],\"dealStatus\":\"open\"}"));
            ApiResult<NegotiationReply> result = null;

            Run(service.SendNegotiationText("celular_kevin", "cuanto pago en total", r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("Mae, son 31 500.", result.Value.NpcReply);
            StringAssert.Contains("\"offerId\":\"celular_kevin\"", transport.Requests[1].JsonBody);
        }

        [Test]
        public void Negotiation_FailsWithoutBreakingWhenTheServerIsDown()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Timeout());
            ApiResult<NegotiationReply> result = null;

            Run(service.SendNegotiationText("celular_kevin", "hola", r => result = r));

            Assert.IsFalse(result.Ok);
        }

        [Test]
        public void Health_ReadsTheBudgetMode()
        {
            ApiResult<HealthStatus> result = null;
            transport.Replies.Enqueue(FakeTransport.Ok(
                "{\"status\":\"ok\",\"version\":\"1.0\",\"database\":\"ok\",\"budget\":{\"spentUsd\":1.5,\"budgetUsd\":10,\"remainingUsd\":8.5,\"mode\":\"downgraded\"}}"));

            Run(service.CheckHealth(r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("downgraded", result.Value.BudgetMode);
            Assert.AreEqual("http://test/api/health", transport.Requests[0].Url);
        }

        [Test]
        public void Summary_MapsTheNarrativeAndTheSource()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(
                "{\"monthsPlayed\":12,\"decisions\":12,\"conceptsTaught\":[\"budget\"],\"negotiations\":1,\"dealsAccepted\":0,\"keyQuestionsAsked\":[\"total_cost\"],\"totalDebt\":50000,\"netWorth\":120000,\"narrative\":\"Buen año.\",\"source\":\"fallback\"}"));
            ApiResult<SessionSummary> result = null;

            Run(service.RequestSummary(r => result = r));

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(120000L, result.Value.NetWorth);
            Assert.AreEqual("fallback", result.Value.Source);
        }
    }
}

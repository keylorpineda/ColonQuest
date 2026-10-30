using System;
using ColonQuest.Finance;
using UnityEngine;

namespace ColonQuest.Api
{
    /// <summary>
    /// The component the game uses to talk to the backend. It runs the <see cref="ApiService"/>
    /// coroutines and survives scene changes, so the session id is kept between the town and the
    /// interiors. Results arrive in the callback on the main thread.
    /// </summary>
    public sealed class ApiClient : MonoBehaviour
    {
        private static ApiClient instance;

        public ApiService Service { get; private set; }

        /// <summary>The shared client. It is created the first time it is asked for.</summary>
        public static ApiClient Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ApiClient");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<ApiClient>();
                    instance.Service = new ApiService(new UnityWebRequestTransport(), ApiConfig.Current);
                }

                return instance;
            }
        }

        /// <summary>Replaces the transport and config, for tests and for pointing at another backend.</summary>
        public void Configure(IHttpTransport transport, ApiConfig config)
        {
            Service = new ApiService(transport, config);
        }

        public Coroutine CreateSession(string classCode, Action<ApiResult<string>> done)
        {
            return StartCoroutine(Service.CreateSession(classCode, done));
        }

        public Coroutine RequestEvent(PlayerState state, Action<ApiResult<GameEvent>> done)
        {
            return StartCoroutine(Service.RequestEvent(state, done));
        }

        public Coroutine RecordDecision(int month, GameEvent gameEvent, string choiceId, PlayerState stateAfter, Action<ApiResult<bool>> done)
        {
            return StartCoroutine(Service.RecordDecision(month, gameEvent, choiceId, stateAfter, done));
        }

        public Coroutine SendNegotiationText(string offerId, string text, Action<ApiResult<NegotiationReply>> done)
        {
            return StartCoroutine(Service.SendNegotiationText(offerId, text, done));
        }

        public Coroutine SendNegotiationVoice(string offerId, byte[] wav, Action<ApiResult<NegotiationReply>> done)
        {
            return StartCoroutine(Service.SendNegotiationVoice(offerId, wav, done));
        }

        public Coroutine RequestSummary(PlayerState finalState, Action<ApiResult<SessionSummary>> done)
        {
            return StartCoroutine(Service.RequestSummary(finalState, done));
        }

        public Coroutine RequestImage(string key, Action<ApiResult<byte[]>> done)
        {
            return StartCoroutine(Service.RequestImage(key, done));
        }

        public Coroutine CheckHealth(Action<ApiResult<HealthStatus>> done)
        {
            return StartCoroutine(Service.CheckHealth(done));
        }
    }
}

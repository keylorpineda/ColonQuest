using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ColonQuest.Finance;
using UnityEngine;

namespace ColonQuest.Api
{
    /// <summary>
    /// The calls Unity makes to the backend, written as coroutines over an <see cref="IHttpTransport"/>.
    /// Nothing here throws: every failure becomes an <see cref="ApiResult{T}"/>. The monthly event is
    /// the exception to "fail": it always succeeds, with a local event when the backend does not answer
    /// in time or answers something that breaks the contract.
    /// </summary>
    public sealed class ApiService
    {
        private readonly IHttpTransport transport;
        private readonly ApiConfig config;
        private readonly HashSet<string> seenEvents = new HashSet<string>();

        /// <summary>
        /// True while the texts written by the models cannot be shown: the backend writes them only in Spanish, so in English
        /// the local events, seller and summary answer instead, all of them translated. The game sets this (to "the game is in
        /// English"); the telemetry and the voice transcription still go to the backend.
        /// </summary>
        public static Func<bool> LocalTextsOnly = () => false;

        /// <summary>The language the backend writes in, "es" or "en". The game sets this from its own language setting.</summary>
        public static Func<string> Language = () => "es";

        public ApiService(IHttpTransport transport, ApiConfig config)
        {
            this.transport = transport;
            this.config = config;
        }

        /// <summary>Empty until a session is created. Without one every event comes from the local catalog.</summary>
        public string SessionId { get; private set; } = string.Empty;

        public bool HasSession => !string.IsNullOrEmpty(SessionId);

        public void RestoreSession(string sessionId)
        {
            SessionId = sessionId ?? string.Empty;
        }

        public IEnumerator CreateSession(string classCode, Action<ApiResult<string>> done)
        {
            string lang = Language();
            string body;
            if (string.IsNullOrEmpty(classCode))
            {
                body = lang == "en" ? "{\"language\":\"en\"}" : "{}";
            }
            else
            {
                body = lang == "en"
                    ? "{\"classCode\":\"" + Escape(classCode) + "\",\"language\":\"en\"}"
                    : "{\"classCode\":\"" + Escape(classCode) + "\"}";
            }
            HttpResponseData response = null;
            yield return transport.Send(Post("sessions", body, config.Timeout), r => response = r);

            var result = Parse<SessionCreatedDto, string>(response, dto =>
            {
                if (string.IsNullOrEmpty(dto.sessionId))
                {
                    throw new ApiFormatException("The session has no id.");
                }

                return dto.sessionId;
            });

            if (result.Ok)
            {
                SessionId = result.Value;
            }

            done(result);
        }

        /// <summary>Asks the game master for this month's event. Always calls <paramref name="done"/> with a valid event.</summary>
        public IEnumerator RequestEvent(PlayerState state, Action<ApiResult<GameEvent>> done)
        {
            ApiResult<GameEvent> result = null;

            if (HasSession && !LocalTextsOnly())
            {
                string body = JsonUtility.ToJson(ApiMapper.ToDto(state));
                HttpResponseData response = null;
                yield return transport.Send(Post($"sessions/{SessionId}/event", body, config.EventTimeout), r => response = r);
                result = Parse<GameEventDto, GameEvent>(response, ApiMapper.FromDto);
                result.Origin = HeaderOf(response, "X-Event-Source");
            }
            else
            {
                result = ApiResult<GameEvent>.Failure("no session");
            }

            if (!result.Ok || result.Value == null)
            {
                string reason = result.Error;
                result = ApiResult<GameEvent>.Success(FallbackEvents.Next(state, seenEvents), ResultSource.Fallback, result.StatusCode);
                result.Error = reason;
            }

            seenEvents.Add(result.Value.EventId);
            done(result);
        }

        /// <summary>Telemetry for the impact metrics. A failure here never blocks the player.</summary>
        public IEnumerator RecordDecision(int month, GameEvent gameEvent, string choiceId, PlayerState stateAfter, Action<ApiResult<bool>> done)
        {
            if (!HasSession)
            {
                done(ApiResult<bool>.Failure("no session"));
                yield break;
            }

            string body = JsonUtility.ToJson(ApiMapper.ToDto(month, gameEvent.EventId, gameEvent.TeachingConcept, choiceId, stateAfter));
            HttpResponseData response = null;
            yield return transport.Send(Post($"sessions/{SessionId}/decisions", body, config.Timeout), r => response = r);

            done(response.IsSuccess ? ApiResult<bool>.Success(true, ResultSource.Remote, response.StatusCode) : Failure<bool>(response));
        }

        /// <summary>One negotiation turn with typed text, the fallback that always exists next to voice.</summary>
        public IEnumerator SendNegotiationText(string offerId, string text, Action<ApiResult<NegotiationReply>> done)
        {
            if (!HasSession || LocalTextsOnly())
            {
                done(ApiResult<NegotiationReply>.Failure(HasSession ? "local texts only" : "no session"));
                yield break;
            }

            string lang = Language();
            string body = JsonUtility.ToJson(new NegotiationRequestDto { offerId = offerId, text = text, language = lang });
            HttpResponseData response = null;
            yield return transport.Send(Post($"sessions/{SessionId}/negotiation", body, config.NegotiationTimeout), r => response = r);
            var result = Parse<NegotiationReplyDto, NegotiationReply>(response, ApiMapper.FromDto);
            result.Origin = HeaderOf(response, "X-Reply-Source");
            done(result);
        }

        /// <summary>
        /// The words of a WAV clip. The backend transcribes it, Meta is never called from here. 503 (voice
        /// down), 422 (no speech or a bad clip) and network errors come back as failures so the UI can
        /// ask for text.
        /// </summary>
        public IEnumerator Transcribe(byte[] wav, Action<ApiResult<string>> done)
        {
            if (!HasSession)
            {
                done(ApiResult<string>.Failure("no session"));
                yield break;
            }

            if (wav == null || wav.Length == 0)
            {
                done(ApiResult<string>.Failure("empty audio"));
                yield break;
            }

            var form = new MultipartForm().AddFile("audio", "voice.wav", "audio/wav", wav);
            var request = new HttpRequestData
            {
                Method = "POST",
                Url = config.Url($"sessions/{SessionId}/transcribe"),
                RawBody = form.ToBytes(),
                ContentType = form.ContentType,
                TimeoutSeconds = config.VoiceTimeout
            };

            HttpResponseData response = null;
            yield return transport.Send(request, r => response = r);
            var result = Parse<TranscriptDto, string>(response, dto => dto.transcript);
            if (result.Ok && string.IsNullOrWhiteSpace(result.Value))
            {
                result = ApiResult<string>.Failure("empty transcript", response.StatusCode);
            }

            done(result);
        }

        /// <summary>Closes the session: the backend needs the final state to compute net worth and debt.</summary>
        public IEnumerator RequestSummary(PlayerState finalState, Action<ApiResult<SessionSummary>> done)
        {
            if (!HasSession || LocalTextsOnly())
            {
                done(ApiResult<SessionSummary>.Failure(HasSession ? "local texts only" : "no session"));
                yield break;
            }

            HttpResponseData response = null;
            yield return transport.Send(Post($"sessions/{SessionId}/summary", JsonUtility.ToJson(ApiMapper.ToDto(finalState)), config.SummaryTimeout), r => response = r);
            done(Parse<SessionSummaryDto, SessionSummary>(response, ApiMapper.FromDto));
        }

        /// <summary>A portrait or a scene by key. The backend answers with a stand-in picture when it cannot make the real one; that is a failure here.</summary>
        public IEnumerator RequestImage(string key, Action<ApiResult<byte[]>> done)
        {
            HttpResponseData response = null;
            var request = new HttpRequestData { Method = "GET", Url = config.Url("images/" + key), TimeoutSeconds = config.ImageTimeout };
            yield return transport.Send(request, r => response = r);
            if (!response.IsSuccess)
            {
                done(Failure<byte[]>(response));
                yield break;
            }

            foreach (var header in response.Headers)
            {
                if (string.Equals(header.Key, "X-Image-Source", StringComparison.OrdinalIgnoreCase) && header.Value == "fallback")
                {
                    done(ApiResult<byte[]>.Failure("stand-in picture", response.StatusCode));
                    yield break;
                }
            }

            if (response.Bytes == null || response.Bytes.Length == 0)
            {
                done(ApiResult<byte[]>.Failure("empty picture", response.StatusCode));
                yield break;
            }

            done(ApiResult<byte[]>.Success(response.Bytes, ResultSource.Remote, response.StatusCode));
        }

        public IEnumerator CheckHealth(Action<ApiResult<HealthStatus>> done)
        {
            HttpResponseData response = null;
            var request = new HttpRequestData { Method = "GET", Url = config.Url("health"), TimeoutSeconds = config.Timeout };
            yield return transport.Send(request, r => response = r);
            done(Parse<HealthDto, HealthStatus>(response, ApiMapper.FromDto));
        }

        /// <summary>What the models and agents did in this session. It also answers after the session is closed.</summary>
        public IEnumerator RequestTrace(Action<ApiResult<SessionTraceDto>> done)
        {
            if (!HasSession)
            {
                done(ApiResult<SessionTraceDto>.Failure("no session"));
                yield break;
            }

            HttpResponseData response = null;
            var request = new HttpRequestData { Method = "GET", Url = config.Url($"sessions/{SessionId}/trace"), TimeoutSeconds = config.Timeout };
            yield return transport.Send(request, r => response = r);
            done(Parse<SessionTraceDto, SessionTraceDto>(response, dto => dto));
        }

        /// <summary>Opens a class of the Class Mode and answers with its code.</summary>
        public IEnumerator CreateClass(string name, Action<ApiResult<string>> done)
        {
            string body = JsonUtility.ToJson(new CreateClassRequestDto { name = name });
            HttpResponseData response = null;
            yield return transport.Send(Post("classes", body, config.Timeout), r => response = r);
            done(Parse<ClassCreatedDto, string>(response, dto =>
            {
                if (string.IsNullOrEmpty(dto.code))
                {
                    throw new ApiFormatException("The class has no code.");
                }

                return dto.code;
            }));
        }

        /// <summary>The teacher's panel of a class: figures computed by the backend, explained by a model.</summary>
        public IEnumerator RequestClassDashboard(string code, Action<ApiResult<ClassDashboardDto>> done)
        {
            HttpResponseData response = null;
            var request = new HttpRequestData { Method = "GET", Url = config.Url($"classes/{code}/dashboard"), TimeoutSeconds = config.Timeout };
            yield return transport.Send(request, r => response = r);

            // JsonUtility reads a null number as 0, and "nobody yet" is not the same as "nobody".
            response.Text = Regex.Replace(response.Text ?? string.Empty, "\"(riskyChoicePct|trapDetectionPct|averageFinalDebt)\"\\s*:\\s*null", "\"$1\":" + ClassDashboardDto.NoData);
            done(Parse<ClassDashboardDto, ClassDashboardDto>(response, dto => dto));
        }

        // ---------------------------------------------------------------- helpers

        private static string HeaderOf(HttpResponseData response, string name)
        {
            foreach (var header in response.Headers)
            {
                if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return header.Value;
                }
            }

            return string.Empty;
        }

        private HttpRequestData Post(string path, string jsonBody, float timeout)
        {
            return new HttpRequestData { Method = "POST", Url = config.Url(path), JsonBody = jsonBody, TimeoutSeconds = timeout };
        }

        /// <summary>Turns a response into a result: transport and HTTP errors, bad JSON and contract breaks all become failures.</summary>
        private static ApiResult<TOut> Parse<TDto, TOut>(HttpResponseData response, Func<TDto, TOut> map)
        {
            if (!response.IsSuccess)
            {
                return Failure<TOut>(response);
            }

            try
            {
                var dto = JsonUtility.FromJson<TDto>(response.Text);
                return ApiResult<TOut>.Success(map(dto), ResultSource.Remote, response.StatusCode);
            }
            catch (ArgumentException error)
            {
                return ApiResult<TOut>.Failure("bad json: " + error.Message, response.StatusCode);
            }
            catch (ApiFormatException error)
            {
                return ApiResult<TOut>.Failure(error.Message, response.StatusCode);
            }
        }

        private static ApiResult<T> Failure<T>(HttpResponseData response)
        {
            string reason = !string.IsNullOrEmpty(response.NetworkError) ? response.NetworkError : "http " + response.StatusCode;
            return ApiResult<T>.Failure(reason, response.StatusCode);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

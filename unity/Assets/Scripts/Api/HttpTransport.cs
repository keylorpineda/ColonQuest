using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ColonQuest.Api
{
    public sealed class HttpRequestData
    {
        public string Method = "GET";
        public string Url = string.Empty;
        public string JsonBody;
        public byte[] RawBody;
        public string ContentType;
        public float TimeoutSeconds = ApiConfig.DefaultTimeoutSeconds;
    }

    public sealed class HttpResponseData
    {
        /// <summary>0 when the request never reached the server (offline, blocked by CORS or timed out).</summary>
        public long StatusCode;
        public string Text = string.Empty;
        public byte[] Bytes = new byte[0];
        public string NetworkError = string.Empty;
        public bool TimedOut;
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public bool IsSuccess => string.IsNullOrEmpty(NetworkError) && StatusCode >= 200 && StatusCode < 300;
    }

    /// <summary>One HTTP round trip as a coroutine. Tests replace it with a fake.</summary>
    public interface IHttpTransport
    {
        IEnumerator Send(HttpRequestData request, Action<HttpResponseData> done);
    }

    /// <summary>UnityWebRequest transport. It works the same in the editor, on desktop and in WebGL.</summary>
    public sealed class UnityWebRequestTransport : IHttpTransport
    {
        public IEnumerator Send(HttpRequestData request, Action<HttpResponseData> done)
        {
            var response = new HttpResponseData();
            using (var web = new UnityWebRequest(request.Url, request.Method))
            {
                web.downloadHandler = new DownloadHandlerBuffer();

                byte[] body = request.RawBody;
                if (body == null && request.JsonBody != null)
                {
                    body = Encoding.UTF8.GetBytes(request.JsonBody);
                }

                if (body != null)
                {
                    web.uploadHandler = new UploadHandlerRaw(body);
                    web.SetRequestHeader("Content-Type", request.ContentType ?? "application/json");
                }

                web.SetRequestHeader("Accept", "application/json");

                var operation = web.SendWebRequest();
                float started = Time.realtimeSinceStartup;

                // The watchdog is ours because UnityWebRequest.timeout is not honored on every platform.
                while (!operation.isDone)
                {
                    if (Time.realtimeSinceStartup - started > request.TimeoutSeconds)
                    {
                        web.Abort();
                        response.TimedOut = true;
                        response.NetworkError = "timeout";
                        done(response);
                        yield break;
                    }

                    yield return null;
                }

                response.StatusCode = web.responseCode;
                response.Bytes = web.downloadHandler.data ?? new byte[0];
                response.Text = web.downloadHandler.text ?? string.Empty;

                if (web.result == UnityWebRequest.Result.ConnectionError || web.result == UnityWebRequest.Result.DataProcessingError)
                {
                    response.NetworkError = web.error ?? "network error";
                }

                var headers = web.GetResponseHeaders();
                if (headers != null)
                {
                    response.Headers = headers;
                }
            }

            done(response);
        }
    }
}

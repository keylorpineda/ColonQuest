namespace ColonQuest.Api
{
    /// <summary>
    /// Where the backend lives and how long the game waits for it. There are no keys here: the
    /// model keys only exist on the server. Change <see cref="BaseUrl"/> before the first call,
    /// for example from a deploy script, or call <see cref="Use"/>.
    /// </summary>
    public sealed class ApiConfig
    {
        /// <summary>Local backend started with <c>uv run uvicorn app.main:app --reload</c>.</summary>
        public const string LocalUrl = "http://localhost:8000/api";

        /// <summary>The deployed backend. The editor and the tests keep the local one; every build uses this one.</summary>
        public const string ProductionUrl = "https://colon-quest-api.onrender.com/api";

        /// <summary>The year summary is written by a bigger model, so it gets longer; after this the game writes its own.</summary>
        public const float SummaryTimeoutSeconds = 25f;

        /// <summary>A picture is generated once, the first time it is asked for, and then served from the cache.</summary>
        public const float ImageTimeoutSeconds = 30f;

        /// <summary>The game master has this long before the game plays a local event instead.</summary>
        public const float EventTimeoutSeconds = 8f;

        public const float DefaultTimeoutSeconds = 10f;

        /// <summary>A clip goes up and only its words come back; the backend gives up on the transcription at 8 s.</summary>
        public const float VoiceTimeoutSeconds = 12f;

        /// <summary>Longer than the 10 s the backend gives its seller, so a slow turn gets the backend's own scripted answer.</summary>
        public const float NegotiationTimeoutSeconds = 13f;

#if UNITY_EDITOR
        public string BaseUrl = LocalUrl;
#else
        public string BaseUrl = ProductionUrl;
#endif
        public float SummaryTimeout = SummaryTimeoutSeconds;
        public float ImageTimeout = ImageTimeoutSeconds;
        public float EventTimeout = EventTimeoutSeconds;
        public float Timeout = DefaultTimeoutSeconds;
        public float VoiceTimeout = VoiceTimeoutSeconds;
        public float NegotiationTimeout = NegotiationTimeoutSeconds;

        public static ApiConfig Current { get; private set; } = new ApiConfig();

        public static void Use(string baseUrl)
        {
            Current = new ApiConfig { BaseUrl = baseUrl.TrimEnd('/') };
        }

        public string Url(string path)
        {
            return BaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
        }
    }
}

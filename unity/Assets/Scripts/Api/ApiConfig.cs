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

        /// <summary>The game master has this long before the game plays a local event instead.</summary>
        public const float EventTimeoutSeconds = 8f;

        public const float DefaultTimeoutSeconds = 10f;

        public string BaseUrl = LocalUrl;
        public float EventTimeout = EventTimeoutSeconds;
        public float Timeout = DefaultTimeoutSeconds;

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

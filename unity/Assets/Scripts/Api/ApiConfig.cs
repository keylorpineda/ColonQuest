using System;
using System.IO;

namespace ColonQuest.Api
{
    /// <summary>
    /// Where the backend lives and how long the game waits for it. There are no keys here: the
    /// model keys only exist on the server. A build asks for its backend in <see cref="EnvVariable"/> and then
    /// in <see cref="EnvFile"/> beside the exe, so it can change without building again; see docs/run-and-build.md.
    /// </summary>
    public sealed class ApiConfig
    {
        /// <summary>Local backend started with <c>uv run uvicorn app.main:app --reload</c>.</summary>
        public const string LocalUrl = "http://localhost:8000/api";

        /// <summary>The deployed backend. The editor and the tests keep the local one; every build uses this one.</summary>
        public const string ProductionUrl = "https://colon-quest-api.onrender.com/api";

        /// <summary>The environment variable that says where the backend is.</summary>
        public const string EnvVariable = "COLONQUEST_API_URL";

        /// <summary>The file beside the exe that says it too, with lines like <c>COLONQUEST_API_URL=http://...</c>.</summary>
        public const string EnvFile = "colonquest.env";

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
        public string BaseUrl = BuildUrl;
#endif
        public float SummaryTimeout = SummaryTimeoutSeconds;
        public float ImageTimeout = ImageTimeoutSeconds;
        public float EventTimeout = EventTimeoutSeconds;
        public float Timeout = DefaultTimeoutSeconds;
        public float VoiceTimeout = VoiceTimeoutSeconds;
        public float NegotiationTimeout = NegotiationTimeoutSeconds;

        public static ApiConfig Current { get; private set; } = new ApiConfig();

        /// <summary>
        /// The same backend for the demo lab, which waits longer: a judge is there to watch the real model answer, and a
        /// server that was asleep needs most of a minute for its first reply. Nothing here falls back in silence.
        /// </summary>
        public ApiConfig ForDemo()
        {
            return new ApiConfig
            {
                BaseUrl = BaseUrl,
                Timeout = 70f,
                EventTimeout = 25f,
                NegotiationTimeout = 25f,
                VoiceTimeout = 20f,
                SummaryTimeout = 50f,
                ImageTimeout = 45f
            };
        }

        /// <summary>The first address that looks like one, from the variable and then the file; otherwise the fallback.</summary>
        public static string Resolve(string variable, string fileText, string fallback)
        {
            if (IsUrl(variable))
            {
                return variable.Trim().TrimEnd('/');
            }

            foreach (string line in (fileText ?? string.Empty).Split('\n'))
            {
                string text = line.Trim();
                string prefix = EnvVariable + "=";
                if (text.StartsWith(prefix, StringComparison.Ordinal) && IsUrl(text.Substring(prefix.Length)))
                {
                    return text.Substring(prefix.Length).Trim().TrimEnd('/');
                }
            }

            return fallback;
        }

        private static bool IsUrl(string text)
        {
            string url = (text ?? string.Empty).Trim();
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

#if !UNITY_EDITOR
        private static string buildUrl;

        // Read on first use: Current, a static field above, already needs it while the class starts.
        private static string BuildUrl => buildUrl ?? (buildUrl = Resolve(Environment.GetEnvironmentVariable(EnvVariable), ReadEnvFile(), ProductionUrl));

        private static string ReadEnvFile()
        {
            try
            {
                string exe = Path.GetFullPath(Environment.GetCommandLineArgs()[0]);
                string file = Path.Combine(Path.GetDirectoryName(exe), EnvFile);
                return File.Exists(file) ? File.ReadAllText(file) : string.Empty;
            }
            catch (Exception)
            {
                // A browser has no such file, and a file that cannot be read is the same as none.
                return string.Empty;
            }
        }
#endif

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

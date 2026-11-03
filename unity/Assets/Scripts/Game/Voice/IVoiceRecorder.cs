namespace ColonQuest.Game
{
    public enum VoiceRecorderState
    {
        Idle,

        /// <summary>Waiting for the microphone, for example while the browser asks for permission.</summary>
        Starting,
        Recording,

        /// <summary>The clip is ready to take with <see cref="IVoiceRecorder.TakeWav"/>.</summary>
        Ready,
        Failed
    }

    /// <summary>
    /// Records one short clip for the negotiation. The WebGL build records in the browser and the
    /// desktop builds use Unity's microphone; both stop by themselves at <see cref="WavEncoder.MaxSeconds"/>.
    /// Call <see cref="Tick"/> every frame while it is in use.
    /// </summary>
    public interface IVoiceRecorder
    {
        /// <summary>False when there is no microphone or the browser cannot record.</summary>
        bool IsSupported { get; }

        VoiceRecorderState State { get; }

        /// <summary>Seconds recorded so far.</summary>
        float Seconds { get; }

        /// <summary>How loud the microphone is right now, 0 to 1; stays at 0 when the platform cannot tell.</summary>
        float Level { get; }

        /// <summary>True when the clip also ends by itself after the player stops talking.</summary>
        bool StopsOnSilence { get; }

        void Start();

        /// <summary>Stops listening; the clip becomes <see cref="VoiceRecorderState.Ready"/>.</summary>
        void Stop();

        /// <summary>Drops whatever was recorded and releases the microphone.</summary>
        void Cancel();

        void Tick();

        /// <summary>The finished clip as a 16-bit PCM WAV, or null. The recorder goes back to idle.</summary>
        byte[] TakeWav();
    }

    public static class VoiceRecorders
    {
        /// <summary>The recorder for the platform the game runs on.</summary>
        public static IVoiceRecorder Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGlVoiceRecorder();
#else
            return new MicrophoneRecorder();
#endif
        }
    }
}

using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// Decides when the player finished talking, so the clip goes out without tapping again. It learns the noise of the room in
    /// the first moment, waits to hear a voice over it and answers true after a quiet pause. It never ends a clip where nobody
    /// spoke: without a voice, the player stops it by hand. Pure C#, tested in EditMode.
    /// </summary>
    public sealed class SilenceGate
    {
        public const float PauseSeconds = 1.1f;
        private const float LearnSeconds = 0.25f;
        private const float MaxNoise = 0.03f;
        private const float MinVoice = 0.015f;
        private const float MinQuiet = 0.008f;

        private float elapsed;
        private float noise;
        private float peak;
        private float quiet;
        private bool heard;

        /// <summary>True once a voice was heard, whatever came after.</summary>
        public bool Heard => heard;

        /// <summary>Feeds the loudness (RMS, 0 to 1) of the last moment; true when the player finished talking.</summary>
        public bool Feed(float level, float deltaSeconds)
        {
            elapsed += deltaSeconds;
            if (elapsed < LearnSeconds)
            {
                noise = Math.Min(MaxNoise, Math.Max(noise, level));
                return false;
            }

            if (!heard)
            {
                if (level < Math.Max(MinVoice, noise * 2.5f))
                {
                    return false;
                }

                heard = true;
            }

            peak = Math.Max(peak, level);
            quiet = level < Math.Max(MinQuiet, peak * 0.2f) ? quiet + deltaSeconds : 0f;
            return quiet >= PauseSeconds;
        }
    }
}

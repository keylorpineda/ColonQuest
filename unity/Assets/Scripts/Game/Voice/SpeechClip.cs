using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// Leaves a clip ready to transcribe: without the long silences around the voice, where the noise of the room confuses the
    /// model, and at a level it hears well. It never cuts inside what was said. Pure C#, tested in EditMode.
    /// </summary>
    public static class SpeechClip
    {
        private const float WindowSeconds = 0.02f;
        private const float MarginSeconds = 0.3f;
        private const float MinVoice = 0.006f;
        private const float VoiceShareOfPeak = 0.08f;
        private const float TargetPeak = 0.7f;
        private const float MostGain = 8f;

        public static float[] Prepare(float[] samples, int sampleRate)
        {
            int window = Math.Max(1, (int)(sampleRate * WindowSeconds));
            int windows = samples.Length / window;
            if (windows == 0)
            {
                return samples;
            }

            float mean = 0f;
            foreach (float sample in samples)
            {
                mean += sample / samples.Length;
            }

            var levels = new float[windows];
            float loudest = 0f;
            for (int w = 0; w < windows; w++)
            {
                float sum = 0f;
                for (int i = 0; i < window; i++)
                {
                    float sample = samples[w * window + i] - mean;
                    sum += sample * sample;
                }

                levels[w] = (float)Math.Sqrt(sum / window);
                loudest = Math.Max(loudest, levels[w]);
            }

            float voice = Math.Max(MinVoice, loudest * VoiceShareOfPeak);
            int first = Array.FindIndex(levels, level => level >= voice);
            if (first < 0)
            {
                return samples;
            }

            int last = Array.FindLastIndex(levels, level => level >= voice);
            int margin = (int)(sampleRate * MarginSeconds);
            int from = Math.Max(0, first * window - margin);
            int to = Math.Min(samples.Length, (last + 1) * window + margin);

            float peak = 0f;
            for (int i = from; i < to; i++)
            {
                peak = Math.Max(peak, Math.Abs(samples[i] - mean));
            }

            float gain = peak > 0f ? Math.Min(MostGain, TargetPeak / peak) : 1f;
            var clip = new float[to - from];
            for (int i = 0; i < clip.Length; i++)
            {
                clip[i] = (samples[from + i] - mean) * gain;
            }

            return clip;
        }
    }
}

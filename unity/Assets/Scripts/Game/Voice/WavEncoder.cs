using System;

namespace ColonQuest.Game
{
    /// <summary>
    /// Turns recorded samples into a 16-bit PCM WAV, the format the backend sends to the transcriber.
    /// Clips longer than <see cref="MaxSeconds"/> are cut. Pure C#, tested in EditMode.
    /// </summary>
    public static class WavEncoder
    {
        /// <summary>The negotiation listens for at most this long; the backend refuses longer clips.</summary>
        public const int MaxSeconds = 15;

        private const int HeaderSize = 44;

        /// <summary>Interleaved samples in [-1, 1]; values outside are clipped.</summary>
        public static byte[] FromFloats(float[] samples, int channels, int sampleRate)
        {
            Validate(channels, sampleRate);
            samples = samples ?? new float[0];
            int count = Limit(samples.Length, channels, sampleRate);
            var pcm = new short[count];
            for (int i = 0; i < count; i++)
            {
                float value = Math.Max(-1f, Math.Min(1f, samples[i]));
                pcm[i] = (short)Math.Round(value < 0f ? value * 32768f : value * 32767f);
            }

            return FromPcm16(pcm, channels, sampleRate);
        }

        /// <summary>Interleaved 16-bit samples.</summary>
        public static byte[] FromPcm16(short[] samples, int channels, int sampleRate)
        {
            Validate(channels, sampleRate);
            samples = samples ?? new short[0];
            int count = Limit(samples.Length, channels, sampleRate);
            int dataSize = count * 2;
            var wav = new byte[HeaderSize + dataSize];

            WriteAscii(wav, 0, "RIFF");
            WriteInt(wav, 4, HeaderSize - 8 + dataSize);
            WriteAscii(wav, 8, "WAVE");
            WriteAscii(wav, 12, "fmt ");
            WriteInt(wav, 16, 16);
            WriteShort(wav, 20, 1);
            WriteShort(wav, 22, channels);
            WriteInt(wav, 24, sampleRate);
            WriteInt(wav, 28, sampleRate * channels * 2);
            WriteShort(wav, 32, channels * 2);
            WriteShort(wav, 34, 16);
            WriteAscii(wav, 36, "data");
            WriteInt(wav, 40, dataSize);

            for (int i = 0; i < count; i++)
            {
                WriteShort(wav, HeaderSize + i * 2, samples[i]);
            }

            return wav;
        }

        /// <summary>Whole frames only, and never more than <see cref="MaxSeconds"/>.</summary>
        private static int Limit(int length, int channels, int sampleRate)
        {
            int frames = Math.Min(length / channels, MaxSeconds * sampleRate);
            return frames * channels;
        }

        private static void Validate(int channels, int sampleRate)
        {
            if (channels < 1 || channels > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "WAV clips are mono or stereo.");
            }

            if (sampleRate < 8000 || sampleRate > 96000)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Unsupported sample rate.");
            }
        }

        private static void WriteAscii(byte[] buffer, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                buffer[offset + i] = (byte)text[i];
            }
        }

        private static void WriteInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteShort(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
        }
    }
}

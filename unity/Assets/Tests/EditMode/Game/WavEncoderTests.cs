using System;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class WavEncoderTests
    {
        private static int Int(byte[] wav, int offset) => BitConverter.ToInt32(wav, offset);

        private static short Short(byte[] wav, int offset) => BitConverter.ToInt16(wav, offset);

        private static string Ascii(byte[] wav, int offset) => System.Text.Encoding.ASCII.GetString(wav, offset, 4);

        [Test]
        public void Header_DescribesMono16BitPcm()
        {
            byte[] wav = WavEncoder.FromPcm16(new short[] { 1, 2, 3, 4 }, 1, 16000);

            Assert.AreEqual("RIFF", Ascii(wav, 0));
            Assert.AreEqual(wav.Length - 8, Int(wav, 4));
            Assert.AreEqual("WAVE", Ascii(wav, 8));
            Assert.AreEqual("fmt ", Ascii(wav, 12));
            Assert.AreEqual(16, Int(wav, 16), "PCM fmt chunk size");
            Assert.AreEqual(1, Short(wav, 20), "PCM format");
            Assert.AreEqual(1, Short(wav, 22), "channels");
            Assert.AreEqual(16000, Int(wav, 24), "sample rate");
            Assert.AreEqual(32000, Int(wav, 28), "byte rate");
            Assert.AreEqual(2, Short(wav, 32), "block align");
            Assert.AreEqual(16, Short(wav, 34), "bits per sample");
            Assert.AreEqual("data", Ascii(wav, 36));
            Assert.AreEqual(8, Int(wav, 40));
            Assert.AreEqual(44 + 8, wav.Length);
            Assert.AreEqual(3, Short(wav, 48), "samples are little endian");
        }

        [Test]
        public void Stereo_SetsTheChannelsAndRates()
        {
            byte[] wav = WavEncoder.FromPcm16(new short[] { 1, 2, 3, 4 }, 2, 48000);

            Assert.AreEqual(2, Short(wav, 22));
            Assert.AreEqual(48000 * 4, Int(wav, 28));
            Assert.AreEqual(4, Short(wav, 32));
        }

        [Test]
        public void Floats_AreScaledAndClipped()
        {
            byte[] wav = WavEncoder.FromFloats(new[] { 0f, 1f, -1f, 0.5f, 3f, -3f }, 1, 16000);

            Assert.AreEqual(0, Short(wav, 44));
            Assert.AreEqual(short.MaxValue, Short(wav, 46));
            Assert.AreEqual(short.MinValue, Short(wav, 48));
            Assert.AreEqual(16384, Short(wav, 50));
            Assert.AreEqual(short.MaxValue, Short(wav, 52), "clipped up");
            Assert.AreEqual(short.MinValue, Short(wav, 54), "clipped down");
        }

        [Test]
        public void LongClips_AreCutAtFifteenSeconds()
        {
            const int rate = 16000;
            var samples = new float[rate * 20];

            byte[] wav = WavEncoder.FromFloats(samples, 1, rate);

            Assert.AreEqual(WavEncoder.MaxSeconds * rate * 2, Int(wav, 40));
            Assert.AreEqual(44 + WavEncoder.MaxSeconds * rate * 2, wav.Length);
        }

        [Test]
        public void StereoCut_KeepsWholeFrames()
        {
            byte[] wav = WavEncoder.FromPcm16(new short[] { 1, 2, 3 }, 2, 16000);

            Assert.AreEqual(4, Int(wav, 40), "the half frame is dropped");
        }

        [Test]
        public void EmptyClip_IsAValidHeaderWithNoData()
        {
            byte[] wav = WavEncoder.FromFloats(null, 1, 16000);

            Assert.AreEqual(44, wav.Length);
            Assert.AreEqual(0, Int(wav, 40));
        }

        [Test]
        public void BadFormat_IsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WavEncoder.FromPcm16(new short[2], 3, 16000));
            Assert.Throws<ArgumentOutOfRangeException>(() => WavEncoder.FromPcm16(new short[2], 1, 100));
        }
    }
}

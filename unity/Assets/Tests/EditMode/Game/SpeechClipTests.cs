using System;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class SpeechClipTests
    {
        private const int Rate = 16000;

        private static float[] Clip(float quietSeconds, float voiceSeconds, float voiceLevel)
        {
            int quiet = (int)(quietSeconds * Rate);
            int voice = (int)(voiceSeconds * Rate);
            var clip = new float[quiet * 2 + voice];
            for (int i = 0; i < voice; i++)
            {
                clip[quiet + i] = voiceLevel * (float)Math.Sin(i * 0.2);
            }

            return clip;
        }

        private static float Peak(float[] clip)
        {
            float peak = 0f;
            foreach (float sample in clip)
            {
                peak = Math.Max(peak, Math.Abs(sample));
            }

            return peak;
        }

        [Test]
        public void Prepare_TakesOutTheLongSilencesButKeepsAMargin()
        {
            var clip = SpeechClip.Prepare(Clip(2f, 1f, 0.5f), Rate);
            Assert.AreEqual(1.6f, clip.Length / (float)Rate, 0.1f, "one second of voice and 0.3 s on each side");
        }

        [Test]
        public void Prepare_NeverCutsAPauseInsideWhatWasSaid()
        {
            var first = Clip(0f, 1f, 0.5f);
            var whole = new float[first.Length * 2 + 2 * Rate];
            Array.Copy(first, whole, first.Length);
            Array.Copy(first, 0, whole, first.Length + 2 * Rate, first.Length);

            var clip = SpeechClip.Prepare(whole, Rate);

            Assert.AreEqual(whole.Length / (float)Rate, clip.Length / (float)Rate, 0.1f);
        }

        [Test]
        public void Prepare_RaisesAWeakVoiceWithinALimit()
        {
            Assert.AreEqual(0.7f, Peak(SpeechClip.Prepare(Clip(0.5f, 1f, 0.2f), Rate)), 0.01f);
            Assert.AreEqual(0.08f, Peak(SpeechClip.Prepare(Clip(0.5f, 1f, 0.01f), Rate)), 0.01f, "the gain has a limit, so noise does not become a roar");
        }

        [Test]
        public void Prepare_LeavesASilentClipAlone()
        {
            var silent = new float[Rate];
            Assert.AreSame(silent, SpeechClip.Prepare(silent, Rate));
            var tiny = new float[3];
            Assert.AreSame(tiny, SpeechClip.Prepare(tiny, Rate));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class DogMotionTests
    {
        [Test]
        public void ExcitementFor_GrowsAsThePlayerComesCloser()
        {
            Assert.AreEqual(0f, DogMotion.ExcitementFor(DogMotion.NoticeRadius + 0.1f));
            Assert.AreEqual(0f, DogMotion.ExcitementFor(float.MaxValue), "no player in the scene");
            float near = DogMotion.ExcitementFor(0.5f);
            float far = DogMotion.ExcitementFor(DogMotion.NoticeRadius - 0.1f);
            Assert.Greater(near, far);
            Assert.LessOrEqual(near, 0.6f, "only petting makes it fully excited");
        }

        [Test]
        public void TailWag_IsWiderWhenHappy()
        {
            float calm = 0f;
            float happy = 0f;
            for (float phase = 0f; phase < Mathf.PI * 2f; phase += 0.05f)
            {
                calm = Mathf.Max(calm, Mathf.Abs(DogMotion.TailWag(phase, 0f)));
                happy = Mathf.Max(happy, Mathf.Abs(DogMotion.TailWag(phase, 1f)));
            }

            Assert.Greater(happy, calm * 3f);
            Assert.Greater(DogMotion.TailSpeed(1f), DogMotion.TailSpeed(0f));
        }

        [Test]
        public void Breath_StaysCloseToTheRestingSize()
        {
            for (float t = 0f; t < 10f; t += 0.13f)
            {
                Assert.That(DogMotion.Breath(t, true, 0f), Is.InRange(0.95f, 1.05f));
                Assert.That(DogMotion.Breath(t, false, 1f), Is.InRange(0.95f, 1.05f));
            }
        }

        [Test]
        public void EarTwitch_IsShortAndComesBack()
        {
            Assert.AreEqual(0f, DogMotion.EarTwitch(-1f));
            Assert.AreEqual(0f, DogMotion.EarTwitch(0f), 0.001f);
            Assert.Greater(DogMotion.EarTwitch(0.16f), 10f);
            Assert.AreEqual(0f, DogMotion.EarTwitch(1f));
        }

        [Test]
        public void HeartFloat_FadesInRisesAndFadesOut()
        {
            DogMotion.HeartFloat(0f, out float rise0, out float alpha0);
            DogMotion.HeartFloat(0.2f, out _, out float alphaTop);
            DogMotion.HeartFloat(1f, out float rise1, out float alpha1);

            Assert.AreEqual(0f, alpha0, 0.001f);
            Assert.AreEqual(1f, alphaTop, 0.001f);
            Assert.AreEqual(0f, alpha1, 0.001f);
            Assert.Greater(rise1, rise0);
        }

        [Test]
        public void Hop_OnlyWhileJoyful()
        {
            for (float t = 0f; t < 3f; t += 0.1f)
            {
                Assert.AreEqual(0f, DogMotion.Hop(t, 0f));
                Assert.That(DogMotion.Hop(t, 1f), Is.InRange(0f, 0.0601f));
            }
        }
    }
}

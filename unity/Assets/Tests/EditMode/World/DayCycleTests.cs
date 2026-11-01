using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class DayCycleTests
    {
        [Test]
        public void ProgressOf_GoesFromTheFirstMorningToTheLastEvening()
        {
            Assert.AreEqual(0f, DayCycle.ProgressOf(0, 0, 3, 3), 0.0001f);
            Assert.AreEqual(1f / 3f, DayCycle.ProgressOf(1, 0, 3, 3), 0.0001f);
            Assert.AreEqual(2f / 3f, DayCycle.ProgressOf(1, 3, 3, 3), 0.0001f, "all tasks done is the end of the month");
            Assert.AreEqual(1f, DayCycle.ProgressOf(3, 0, 3, 3), 0.0001f);
        }

        [Test]
        public void ProgressOf_NeverLeavesTheRange()
        {
            Assert.AreEqual(1f, DayCycle.ProgressOf(9, 9, 3, 3), 0.0001f);
            Assert.AreEqual(0f, DayCycle.ProgressOf(0, 0, 0, 0), 0.0001f);
        }

        [Test]
        public void TintFor_IsClearAtMiddayAndHeavierAtTheEnds()
        {
            float midday = DayCycle.TintFor(0.35f).a;

            Assert.AreEqual(0f, midday, 0.0001f);
            Assert.Greater(DayCycle.TintFor(0f).a, midday);
            Assert.Greater(DayCycle.TintFor(1f).a, DayCycle.TintFor(0f).a, "dusk is the darkest");
        }

        [Test]
        public void TintFor_DuskIsBluerThanMorning()
        {
            var morning = DayCycle.TintFor(0f);
            var dusk = DayCycle.TintFor(1f);

            Assert.Greater(morning.r, morning.b);
            Assert.Greater(dusk.b, dusk.r);
        }

        [Test]
        public void TintFor_ChangesSmoothly()
        {
            var previous = DayCycle.TintFor(0f);
            for (int i = 1; i <= 100; i++)
            {
                var tint = DayCycle.TintFor(i / 100f);
                Assert.Less(Mathf.Abs(tint.a - previous.a), 0.02f);
                previous = tint;
            }
        }
    

        [Test]
        public void LightFor_IsBrightAtNoonDimmerAtNightAndNeverTooDark()
        {
            DayCycle.LightFor(0.4f, out Color noon, out float noonIntensity);
            DayCycle.LightFor(1f, out Color night, out float nightIntensity);

            Assert.Greater(noonIntensity, nightIntensity);
            Assert.GreaterOrEqual(nightIntensity, DayCycle.MinLightIntensity, "the town always stays readable");
            Assert.Greater(night.b, night.r, "the night light is blue");
            Assert.AreEqual(1f, noon.r, 0.0001f);
        }

        [Test]
        public void LightFor_SunsetIsWarmAndValuesStayInRange()
        {
            DayCycle.LightFor(0.75f, out Color sunset, out _);
            Assert.Greater(sunset.r, sunset.b + 0.3f, "six o'clock is golden");

            for (float p = -0.5f; p <= 1.5f; p += 0.05f)
            {
                DayCycle.LightFor(p, out Color color, out float intensity);
                Assert.That(intensity, Is.InRange(DayCycle.MinLightIntensity, 1f));
                Assert.That(color.r, Is.InRange(0f, 1f));
                Assert.That(color.g, Is.InRange(0f, 1f));
                Assert.That(color.b, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void LampIntensity_FadesInAndFlickersAFewPercent()
        {
            Assert.AreEqual(0f, DayCycle.LampIntensity(0f, 0.9f, 0.05f, 0.7f), 0.0001f, "off by day");
            Assert.AreEqual(0.9f, DayCycle.LampIntensity(1f, 0.9f, 0.05f, 0.5f), 0.0001f, "the middle of the noise is the full intensity");
            Assert.AreEqual(0.45f, DayCycle.LampIntensity(0.5f, 0.9f, 0f, 1f), 0.0001f, "half way on is half as bright");
            Assert.That(DayCycle.LampIntensity(1f, 1f, 0.05f, 1f), Is.InRange(0.949f, 1.051f));
            Assert.That(DayCycle.LampIntensity(1f, 1f, 0.05f, 0f), Is.InRange(0.949f, 1.051f));
        }

        [Test]
        public void TintAt_ScalesOnlyTheOpacity()
        {
            var full = DayCycle.TintFor(1f);
            var haze = DayTint.TintAt(1f, 0.3f);

            Assert.AreEqual(full.r, haze.r, 0.0001f);
            Assert.AreEqual(full.g, haze.g, 0.0001f);
            Assert.AreEqual(full.b, haze.b, 0.0001f);
            Assert.AreEqual(full.a * 0.3f, haze.a, 0.0001f);
            Assert.AreEqual(full, DayTint.TintAt(1f, 1f));
        }
    }
}

using ColonQuest.Game;
using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.Game.Tests
{
    /// <summary>The toucan's flight into the main menu and the volcano smoke.</summary>
    [TestFixture]
    public sealed class MenuFlightTests
    {
        [Test]
        public void Bezier_StartsAndEndsOnItsEnds()
        {
            var a = new Vector2(0f, 0f);
            var d = new Vector2(10f, -4f);
            Assert.AreEqual(a, MenuMotion.Bezier(a, new Vector2(3f, 5f), new Vector2(7f, 5f), d, 0f));
            Assert.AreEqual(d, MenuMotion.Bezier(a, new Vector2(3f, 5f), new Vector2(7f, 5f), d, 1f));
            Assert.AreEqual(d, MenuMotion.Bezier(a, new Vector2(3f, 5f), new Vector2(7f, 5f), d, 3f), "past the end it stays on the branch");
        }

        [Test]
        public void GlideEase_BrakesNearTheEnd()
        {
            Assert.AreEqual(0f, MenuMotion.GlideEase(0f), 0.0001f);
            Assert.AreEqual(1f, MenuMotion.GlideEase(1f), 0.0001f);
            float early = MenuMotion.GlideEase(0.1f) - MenuMotion.GlideEase(0f);
            float late = MenuMotion.GlideEase(1f) - MenuMotion.GlideEase(0.9f);
            Assert.Greater(early, late * 10f, "it covers much more ground at first than while landing");
        }

        [Test]
        public void FlapAngle_SwingsAroundItsLift()
        {
            for (float t = 0f; t < 2f; t += 0.01f)
            {
                Assert.That(MenuMotion.FlapAngle(t, 5.5f, 38f, 42f), Is.InRange(4f - 0.001f, 80f + 0.001f));
            }
        }

        [Test]
        public void LandingSquash_SettlesBackToFullHeight()
        {
            Assert.Less(MenuMotion.LandingSquash(0f), 1f, "it sinks on the branch as it lands");
            Assert.AreEqual(1f, MenuMotion.LandingSquash(0.7f), 0.0001f);
            Assert.AreEqual(1f, MenuMotion.LandingSquash(-1f), 0.0001f);
        }

        [Test]
        public void Pulse_GoesOutAndBack()
        {
            Assert.AreEqual(0f, MenuMotion.Pulse(0f), 0.0001f);
            Assert.AreEqual(1f, MenuMotion.Pulse(0.5f), 0.0001f);
            Assert.AreEqual(0f, MenuMotion.Pulse(1f), 0.0001f);
        }

        [Test]
        public void Smoke_RisesBendsRightGrowsAndThinsAway()
        {
            Assert.AreEqual(Vector2.zero, MenuMotion.SmokeOffset(0f, 1.3f));
            var top = MenuMotion.SmokeOffset(1f, 1.3f);
            Assert.Greater(top.y, 200f, "it rises out of the picture");
            Assert.Greater(top.x, 100f, "the wind bends it to the right, as in the drawing");
            Assert.Less(MenuMotion.SmokeScale(0f), MenuMotion.SmokeScale(1f));
            Assert.AreEqual(0f, MenuMotion.SmokeAlpha(0f), 0.0001f);
            Assert.AreEqual(1f, MenuMotion.SmokeAlpha(0.4f), 0.0001f);
            Assert.AreEqual(0f, MenuMotion.SmokeAlpha(1f), 0.0001f);
        }
    }
}

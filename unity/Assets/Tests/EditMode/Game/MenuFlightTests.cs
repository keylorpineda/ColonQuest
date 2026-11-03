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
        public void SmokeSway_StaysOnTheCraterAndBendsHigherUp()
        {
            float low = 0f;
            float high = 0f;
            for (float seconds = 0f; seconds < 20f; seconds += 0.05f)
            {
                Assert.AreEqual(0f, MenuMotion.SmokeSway(0f, seconds), 0.0001f, "the base never leaves the crater");
                low = Mathf.Max(low, Mathf.Abs(MenuMotion.SmokeSway(20f, seconds)));
                high = Mathf.Max(high, Mathf.Abs(MenuMotion.SmokeSway(165f, seconds)));
            }

            Assert.Greater(high, low * 3f, "the top moves much more than the base");
            Assert.Less(high, 16f, "a sway, not a column that leaves its place in the picture");
        }

        [Test]
        public void SmokeSway_ClimbsTheColumn()
        {
            // A wave that rises shows the same bend higher up a moment later.
            float now = MenuMotion.SmokeSway(100f, 3f) / Mathf.Clamp01(100f / 170f);
            float later = MenuMotion.SmokeSway(100f, 3f + 3.4f) / Mathf.Clamp01(100f / 170f);
            Assert.AreNotEqual(now, later, "the fast waves and the lean keep it from repeating exactly");
            Assert.AreNotEqual(MenuMotion.SmokeSway(100f, 3f), MenuMotion.SmokeSway(120f, 3f));
        }

        [Test]
        public void SmokeAxis_FollowsTheDrawnColumnToTheRight()
        {
            Assert.AreEqual(0f, MenuMotion.SmokeAxis(0f), 0.0001f);
            Assert.AreEqual(104f, MenuMotion.SmokeAxis(165f), 0.0001f);
            Assert.AreEqual(104f, MenuMotion.SmokeAxis(500f), 0.0001f, "past the top it stays on the last point");
            for (float height = 0f; height < 165f; height += 5f)
            {
                Assert.LessOrEqual(MenuMotion.SmokeAxis(height), MenuMotion.SmokeAxis(height + 5f));
            }
        }

        [Test]
        public void Wisps_AreFaintAndGrow()
        {
            Assert.Less(MenuMotion.WispScale(0f), MenuMotion.WispScale(1f));
            Assert.AreEqual(0f, MenuMotion.WispAlpha(0f), 0.0001f);
            Assert.AreEqual(0.5f, MenuMotion.WispAlpha(0.5f), 0.0001f);
            Assert.AreEqual(0f, MenuMotion.WispAlpha(1f), 0.0001f);
        }

        [Test]
        public void SpringStep_PassesTheTargetAndSettlesOnIt()
        {
            float value = 0f;
            float velocity = 0f;
            float highest = 0f;
            for (int i = 0; i < 300; i++)
            {
                MenuMotion.SpringStep(ref value, ref velocity, 10f, 140f, 15f, 1f / 60f);
                highest = Mathf.Max(highest, value);
            }

            Assert.Greater(highest, 10f, "a little overshoot gives it weight");
            Assert.Less(highest, 13f, "but it never swings wildly");
            Assert.AreEqual(10f, value, 0.01f);
        }

        [Test]
        public void HeadDrift_IsSmall()
        {
            for (float seconds = 0f; seconds < 30f; seconds += 0.1f)
            {
                Assert.LessOrEqual(Mathf.Abs(MenuMotion.HeadDrift(seconds)), 1.6f + 0.001f);
            }
        }
    }
}

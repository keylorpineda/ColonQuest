using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class SurfacesTests
    {
        [Test]
        public void Town_RoadSidewalkGrassAndSandSoundDifferent()
        {
            Assert.AreEqual(SurfaceKind.Road, Surfaces.At(SceneNames.Town, new Vector2(10f, 2f)));
            Assert.AreEqual(SurfaceKind.Sidewalk, Surfaces.At(SceneNames.Town, new Vector2(10f, 4.8f)));
            Assert.AreEqual(SurfaceKind.Grass, Surfaces.At(SceneNames.Town, new Vector2(-30f, 15f)));
            Assert.AreEqual(SurfaceKind.Sand, Surfaces.At(SceneNames.Town, new Vector2(-10f, -24f)));
        }

        [Test]
        public void Bridges_AndThePier_AreWood()
        {
            Assert.AreEqual(SurfaceKind.Wood, Surfaces.At(SceneNames.Town, new Vector2(29.5f, 2f)));
            Assert.AreEqual(SurfaceKind.Wood, Surfaces.At(SceneNames.Town, new Vector2(5.5f, MapLayout.ShoreY - 1f)));
        }

        [Test]
        public void Interiors_FollowTheirFloor()
        {
            Assert.AreEqual(SurfaceKind.Wood, Surfaces.At(SceneNames.Casa, Vector2.zero));
            Assert.AreEqual(SurfaceKind.Wood, Surfaces.At(SceneNames.Finca, Vector2.zero));
            Assert.AreEqual(SurfaceKind.Tile, Surfaces.At(SceneNames.Banco, Vector2.zero));
            Assert.AreEqual(SurfaceKind.Tile, Surfaces.At(SceneNames.Inmobiliaria, Vector2.zero));
        }

        [Test]
        public void Hints_StaySilentInTheMiddleOfTheTown()
        {
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, new Vector2(0f, 8f)).Kind);
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Casa, new Vector2(100f, 100f)).Kind);
        }

        [Test]
        public void Hints_TheEdgeOfTheMapSendsThePlayerBack()
        {
            var west = WorldHints.Check(SceneNames.Town, new Vector2(MapLayout.PlayWestX + 0.4f, 8f));
            var east = WorldHints.Check(SceneNames.Town, new Vector2(MapLayout.PlayEastX - 0.4f, 8f));
            Assert.AreEqual(HintKind.MapEdge, west.Kind);
            Assert.Greater(west.Away.x, 0f);
            Assert.AreEqual(HintKind.MapEdge, east.Kind);
            Assert.Less(east.Away.x, 0f);
        }

        [Test]
        public void Hints_WaterMentionsTheSwimsuitExceptOnBridgesAndThePier()
        {
            Assert.AreEqual(HintKind.Water, WorldHints.Check(SceneNames.Town, new Vector2(29.5f, 15f)).Kind);
            Assert.AreEqual(HintKind.Water, WorldHints.Check(SceneNames.Town, new Vector2(-10f, MapLayout.ShoreY + 0.3f)).Kind);
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, new Vector2(29.5f, 2f)).Kind);
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, new Vector2(5.5f, MapLayout.ShoreY + 0.3f)).Kind);
        }

        [Test]
        public void Hints_TheSidewalksOfEveryBridgeAreNotWater()
        {
            foreach (float y in new[] { -1.2f, 5.0f, -15.2f, -8.8f, 24.8f, 31.6f })
            {
                Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, new Vector2(29.5f, y)).Kind, "sidewalk at y " + y);
            }

            Assert.AreEqual(HintKind.Water, WorldHints.Check(SceneNames.Town, new Vector2(29.5f, 8f)).Kind, "past the railing");
        }

        [Test]
        public void Hints_BesideThePierTheStepBackLeavesTheWaterZoneWithoutCrossingTheBeach()
        {
            // On the sand just west of the pier, pressed against the sea wall.
            var at = new Vector2(MapLayout.PierXMin - 0.5f, MapLayout.ShoreY + 0.3f);
            var hint = WorldHints.Check(SceneNames.Town, at);
            Assert.AreEqual(WaterBody.Sea, hint.Water);
            Assert.AreEqual(Vector2.up, hint.Away, "back to the sand, not onto the pier");

            float seconds = WalkWatch.NudgeSeconds(hint.Depth);
            Vector2 after = at + hint.Away * WalkWatch.WalkSpeed * seconds;
            Assert.AreEqual(HintKind.None, WorldHints.Check(SceneNames.Town, after).Kind, "out of the warning");
            Assert.Less(Vector2.Distance(at, after), 1.5f, "a short step, not across the beach");
        }

        [Test]
        public void Hints_EveryStepBackIsShort()
        {
            Assert.LessOrEqual(WalkWatch.NudgeSeconds(10f), 0.35f);
            Assert.GreaterOrEqual(WalkWatch.NudgeSeconds(-1f), 0.1f);
        }

        [Test]
        public void Thoughts_AreRaisedToTheListeners()
        {
            string heard = null;
            System.Action<string> listener = text => heard = text;
            Thoughts.Said += listener;
            Thoughts.Say("hola");
            Thoughts.Said -= listener;
            Assert.AreEqual("hola", heard);
        }
    }
}

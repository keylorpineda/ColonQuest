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

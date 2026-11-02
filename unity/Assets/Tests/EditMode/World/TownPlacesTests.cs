using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class TownPlacesTests
    {
        private static readonly Rect Bounds = new Rect(-10f, -20f, 100f, 50f);

        [Test]
        public void ToMap_PutsTheCornersOfTheTownOnTheCornersOfThePicture()
        {
            Assert.AreEqual(Vector2.zero, TownPlaces.ToMap(new Vector2(-10f, -20f), Bounds));
            Assert.AreEqual(Vector2.one, TownPlaces.ToMap(new Vector2(90f, 30f), Bounds));
            Assert.AreEqual(new Vector2(0.5f, 0.5f), TownPlaces.ToMap(new Vector2(40f, 5f), Bounds));
        }

        [Test]
        public void View_IsSquareInTheWorldAndCenteredOnThePlayer()
        {
            var view = TownPlaces.View(new Vector2(40f, 5f), Bounds, 20f);

            Assert.AreEqual(0.2f, view.width, 0.0001f);
            Assert.AreEqual(0.4f, view.height, 0.0001f);
            Assert.AreEqual(0.5f, view.center.x, 0.0001f);
            Assert.AreEqual(0.5f, view.center.y, 0.0001f);
        }

        [Test]
        public void View_NearAnEdgeStopsAtTheEdgeOfThePicture()
        {
            var corner = TownPlaces.View(new Vector2(-10f, -20f), Bounds, 20f);
            Assert.AreEqual(0f, corner.xMin, 0.0001f);
            Assert.AreEqual(0f, corner.yMin, 0.0001f);

            var far = TownPlaces.View(new Vector2(90f, 30f), Bounds, 20f);
            Assert.AreEqual(1f, far.xMax, 0.0001f);
            Assert.AreEqual(1f, far.yMax, 0.0001f);
        }

        [Test]
        public void View_BiggerThanTheTownShowsItAll()
        {
            var all = TownPlaces.View(new Vector2(0f, 0f), Bounds, 1000f);

            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), all);
        }

        [Test]
        public void EveryPlace_AndKind_IsTranslated()
        {
            foreach (var place in TownPlaces.All)
            {
                Assert.IsTrue(Translations.English.ContainsKey(place.Name), place.Name);
            }

            foreach (PlaceKind kind in System.Enum.GetValues(typeof(PlaceKind)))
            {
                Assert.IsTrue(Translations.English.ContainsKey(TownPlaces.KindName(kind)), kind.ToString());
            }
        }
    }
}

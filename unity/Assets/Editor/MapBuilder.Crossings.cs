using UnityEngine;
using ColonQuest.World;

namespace ColonQuest.EditorTools
{
    public static partial class MapBuilder
    {
        /// <summary>
        /// The pedestrian lights of the main crosswalks and an officer next to each group. The lights only mark the
        /// offense; the fine exists only when the officer reaches the player (see <see cref="PoliceOfficer"/>).
        /// </summary>
        private static void BuildCrossings(Transform parent)
        {
            // Every crosswalk on the main streets has its pedestrian signal and an officer watching within sight range.
            BuildSignal(parent, "Signal_PlazaEast", 3.6f, 0f, 4f, 0f);
            BuildSignal(parent, "Signal_PlazaWest", -3.6f, 0f, 4f, 0f);
            BuildSignal(parent, "Signal_South", 3.6f, -14f, -10f, 7f);
            BuildSignal(parent, "Signal_East", 62f, 0f, 4f, 3f);
            BuildSignal(parent, "Signal_NorthAvenue", MapLayout.NorthPathX, AvenueRoadY, AvenueRoadY + 4f, 5f);

            BuildStreetOfficer(parent, "Oficial_Plaza", new Vector2(0.4f, 4.4f));
            BuildStreetOfficer(parent, "Oficial_Sur", new Vector2(5.2f, -9.4f));
            BuildStreetOfficer(parent, "Oficial_Este", new Vector2(60.4f, 4.4f));
            BuildStreetOfficer(parent, "Oficial_Norte", new Vector2(MapLayout.NorthPathX - 1.8f, AvenueRoadY + 4.4f));
        }

        private static void BuildSignal(Transform parent, string name, float x, float yMin, float yMax, float phase)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            // A pole on the sidewalk, next to the stripes, with a green lamp over a red one.
            float poleX = x + 1.35f;
            float poleY = yMax + 0.9f;
            Shape(root, "Pole", square, new Vector2(poleX, poleY - 0.2f), new Vector2(0.14f, 1.1f), GamePalette.Hex("#3A3F52"), OrderProp);
            Shape(root, "Box", roundedSmall, new Vector2(poleX, poleY + 0.55f), new Vector2(0.42f, 0.78f), GamePalette.Hex("#1D2238"), OrderProp + 1);
            var wait = Shape(root, "WaitLamp", circle, new Vector2(poleX, poleY + 0.73f), new Vector2(0.24f, 0.24f), GamePalette.Hex("#F04040"), OrderProp + 2);
            var walk = Shape(root, "WalkLamp", circle, new Vector2(poleX, poleY + 0.40f), new Vector2(0.24f, 0.24f), GamePalette.Hex("#4DD973"), OrderProp + 2);

            var center = new Vector2(x, (yMin + yMax) * 0.5f);
            var size = new Vector2(2.4f, yMax - yMin);
            root.gameObject.AddComponent<CrossingSignal>().Configure(center, size, phase, walk, wait);
        }

        private static void BuildStreetOfficer(Transform parent, string name, Vector2 position)
        {
            var officer = BuildNpc(parent, name, position, new Look { Skin = "#3C78D8", Extra = Accessory.Cap, ExtraColor = "#1D2238" }, true);
            officer.gameObject.AddComponent<PoliceOfficer>();
        }
    }
}

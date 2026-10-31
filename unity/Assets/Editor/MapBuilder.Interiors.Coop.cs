using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The savings cooperative: a counter at the back with the advisor and the task station, a desk where the term
    /// savings are explained, chairs to wait, posters and a plant. The door mat leads back to the town.
    /// </summary>
    public static partial class MapBuilder
    {
        private static void BuildCoopLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.14f);
            WallPlate(parts.Labels, "COOPERATIVA", new Vector2(0f, 4.9f), 3.2f, "#2E8B57", GamePalette.White, 0.034f);

            // The counter of the advisor, the same person who used to wait outside.
            var counter = IL(parts.Props, "CoopCounter");
            Solid(counter, "Counter", rounded, 0f, 2.4f, 6.0f, 0.9f, "#8B5A3C");
            Deco(counter, "Counter_Top", roundedSmall, 0f, 2.45f, 5.8f, 0.4f, "#D9D2C5", 1f, 8);
            Deco(counter, "Monitor", roundedSmall, 1.2f, 2.55f, 0.6f, 0.42f, "#2A3040", 1f, 9);
            Deco(counter, "Screen", roundedSmall, 1.2f, 2.57f, 0.48f, 0.3f, "#55E08F", 1f, 10);
            Deco(counter, "Brochures", roundedSmall, -1.6f, 2.45f, 0.7f, 0.3f, "#FFD23F", 1f, 9);
            BuildNpc(parts.People, "AsesoraCoop", new Vector2(0f, 3.45f), new Look { Skin = "#2E8B57", Extra = Accessory.Vest, ExtraColor = "#F1F4FF" }, false);
            BuildStation(parts.Stations, "Station_Coop", StationType.Coop, "Cooperativa de ahorro", new Vector2(0f, 1.25f));

            // The desk of the term savings, on the right.
            var term = IL(parts.Props, "TermDesk");
            Solid(term, "Desk", rounded, 5.8f, 1.4f, 2.4f, 0.8f, "#7A6A52");
            Deco(term, "Papers", roundedSmall, 5.4f, 1.45f, 0.5f, 0.35f, "#FFFFFF", 1f, 8);
            Deco(term, "Calculator", roundedSmall, 6.3f, 1.45f, 0.35f, 0.45f, "#2A3040", 1f, 8);
            WallPlate(parts.Labels, "AHORRO A PLAZO", new Vector2(5.8f, 3.6f), 2.6f, "#1D4F8C", GamePalette.White, 0.024f);
            BuildThing(parts.Stations, "TermSavings", "Preguntar por el ahorro a plazo", new Vector2(5.8f, 0.3f), new Vector2(1.6f, 0.4f),
                "\"En el ahorro a plazo deja la plata quieta unos meses y le pagamos más interés que en una cuenta normal.\"",
                "\"A diferencia del banco, aquí usted es asociado: las ganancias de la cooperativa se reparten entre los socios.\"",
                "\"Si saca la plata antes del plazo, pierde parte del interés. Ahorre a plazo solo lo que no va a ocupar.\"");

            // Chairs to wait on the left, facing the counter.
            var chairs = IL(parts.Props, "WaitingChairs");
            foreach (float y in new[] { -0.6f, -2.0f })
            {
                Solid(chairs, "ChairRow", rounded, -5.6f, y, 3.6f, 0.6f, "#2E8B57");
            }

            // Posters on the back wall.
            var posters = IL(parts.Labels, "Posters");
            Deco(posters, "Poster", rounded, -5.6f, 3.8f, 2.6f, 1.4f, "#F8F5E8", 1f, 5);
            Label(posters, "AHORRÁ CADA QUINCENA", new Vector2(-5.6f, 4.0f), GamePalette.Hex("#2E8B57"), 0.022f, TextAnchor.MiddleCenter);
            Label(posters, "POCO A POCO SE LLENA", new Vector2(-5.6f, 3.6f), GamePalette.Outline, 0.02f, TextAnchor.MiddleCenter);

            Plant(IL(parts.Props, "Plants"), -8.0f, -4.4f);
            CeilingLight(parts.Lights, new Vector2(0f, 1.0f), 5.0f, "#FFF3C4", 0.08f);
            CeilingLight(parts.Lights, new Vector2(-5.6f, -1.3f), 4.0f, "#FFF3C4", 0.06f);
        }
    }
}

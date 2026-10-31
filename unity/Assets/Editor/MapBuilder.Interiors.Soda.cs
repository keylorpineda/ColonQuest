using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The interior of Soda El Muelle: the diner where the player works their first job.
    /// Holds the dining tables, the service counter with Doña Lucía, the kitchen in the back,
    /// and the time punch clock station on the wall for marking shifts.
    /// </summary>
    public static partial class MapBuilder
    {
        private static void BuildSodaLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFF3D6", 0.18f);

            WallPlate(parts.Labels, "SODA EL MUELLE", new Vector2(0f, 5.5f), 3.6f, "#008B8B", GamePalette.White, 0.034f);

            // Counter separating kitchen from customer area
            var counter = IL(parts.Props, "DinerCounter");
            Solid(counter, "Counter", rounded, 0f, 0.5f, 9.2f, 0.9f, "#8B5A3C");
            for (float sx = -4.0f; sx <= 4.0f; sx += 0.8f)
            {
                Deco(counter, "Counter_Slat", square, sx, 0.32f, 0.04f, 0.42f, "#6E442B", 0.7f, 7);
            }
            Deco(counter, "CounterTop", roundedSmall, 0f, 0.55f, 9.4f, 0.35f, "#E8D8B8", 1f, 8);
            Solid(counter, "Register", roundedSmall, -1.8f, 0.72f, 0.8f, 0.6f, "#333A48");
            Deco(counter, "Screen", roundedSmall, -1.8f, 0.82f, 0.5f, 0.28f, "#55E08F", 1f, 9);
            Deco(counter, "CoffeeMachine", roundedSmall, 2.2f, 0.8f, 1.2f, 0.7f, "#C0392B", 1f, 8);
            Deco(counter, "Pot", circle, 2.2f, 0.75f, 0.35f, 0.35f, "#1D2230", 1f, 9);
            Deco(counter, "MenuBoard", roundedSmall, 0f, 0.82f, 1.5f, 0.3f, "#2A3342", 1f, 8);
            Deco(counter, "PastryCase", roundedSmall, 3.6f, 0.72f, 1.1f, 0.42f, "#BFE3F5", 0.85f, 8);
            Deco(counter, "Empanada1", circle, 3.4f, 0.72f, 0.24f, 0.16f, "#E09A3E", 1f, 9);
            Deco(counter, "Empanada2", circle, 3.8f, 0.72f, 0.24f, 0.16f, "#E09A3E", 1f, 9);

            // Counter stools
            var stools = IL(parts.Props, "Stools");
            foreach (float x in new[] { -3.4f, -1.2f, 1.2f, 3.4f })
            {
                Solid(stools, "Stool", circle, x, -0.4f, 0.7f, 0.7f, "#C0392B");
                Deco(stools, "StoolRim", circle, x, -0.4f, 0.76f, 0.76f, "#8B251B", 1f, 7);
            }

            // Doña Lucía NPC behind the counter
            BuildNpc(parts.People, "DonaLucia", new Vector2(0f, 1.6f), new Look { Skin = "#F2994A", Extra = Accessory.Cap, ExtraColor = "#00A8A8" }, true);

            // The soda customer service station
            BuildStation(parts.Stations, "Station_Soda", StationType.Soda, "Soda El Muelle", new Vector2(0f, -0.6f));

            // Kitchen area behind the counter
            var kitchen = IL(parts.Props, "Kitchen");
            Solid(kitchen, "Stove", roundedSmall, -3.5f, 4.2f, 1.8f, 1.2f, "#4A5260");
            Deco(kitchen, "Burner1", circle, -3.9f, 4.3f, 0.4f, 0.4f, "#1D2230", 1f, 8);
            Deco(kitchen, "Burner2", circle, -3.1f, 4.3f, 0.4f, 0.4f, "#1D2230", 1f, 8);
            Deco(kitchen, "Pan", circle, -3.9f, 4.3f, 0.35f, 0.35f, "#7F8C8D", 1f, 9);
            Solid(kitchen, "PrepTable", rounded, 0f, 4.2f, 2.8f, 1.0f, "#BDC3C7");
            Solid(kitchen, "Fridge", rounded, 3.6f, 4.2f, 1.5f, 1.5f, "#E8EDF5");
            Deco(kitchen, "FridgeDoor", square, 3.6f, 4.4f, 1.5f, 0.05f, "#AAB5C6", 1f, 8);

            // The Time Punch Clock Station on the wall next to the employee entrance
            var clock = IL(parts.Props, "TimeClock");
            Solid(clock, "TimeClock", roundedSmall, -6.8f, 2.7f, 1.0f, 1.3f, "#2C3440");
            Deco(clock, "Face", circle, -6.8f, 2.95f, 0.7f, 0.7f, "#FFFFFF", 1f, 9);
            Deco(clock, "Hands", square, -6.8f, 2.95f, 0.04f, 0.35f, "#1D2230", 1f, 10);
            Deco(clock, "CardSlot", square, -6.8f, 2.35f, 0.6f, 0.08f, "#1D2230", 1f, 9);
            BuildStation(parts.Stations, "Station_Work", StationType.Work, "Reloj marcador de la soda", new Vector2(-6.8f, 1.5f));

            // Dining tables in front
            var dining = IL(parts.Props, "Dining");
            // Left Table
            Solid(dining, "TableLeft", rounded, -5.2f, -2.6f, 2.2f, 1.4f, "#A67243");
            Deco(dining, "NapkinsL", roundedSmall, -5.2f, -2.6f, 0.35f, 0.35f, "#FFFFFF", 1f, 8);
            Deco(dining, "SauceBottleL", roundedSmall, -4.9f, -2.55f, 0.14f, 0.24f, "#6E4726", 1f, 9);
            Deco(dining, "SauceCapL", circle, -4.9f, -2.41f, 0.12f, 0.08f, "#2E8B57", 1f, 10);
            Deco(dining, "PlateL", circle, -5.65f, -2.6f, 0.36f, 0.36f, "#F8F5E8", 1f, 8);
            Solid(dining, "ChairL1", roundedSmall, -6.8f, -2.6f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairL2", roundedSmall, -3.6f, -2.6f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairL3", roundedSmall, -5.2f, -1.4f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairL4", roundedSmall, -5.2f, -3.8f, 0.7f, 0.7f, "#8B5A3C");

            // Right Table
            Solid(dining, "TableRight", rounded, 5.2f, -2.6f, 2.2f, 1.4f, "#A67243");
            Deco(dining, "NapkinsR", roundedSmall, 5.2f, -2.6f, 0.35f, 0.35f, "#FFFFFF", 1f, 8);
            Deco(dining, "SauceBottleR", roundedSmall, 4.9f, -2.55f, 0.14f, 0.24f, "#6E4726", 1f, 9);
            Deco(dining, "SauceCapR", circle, 4.9f, -2.41f, 0.12f, 0.08f, "#2E8B57", 1f, 10);
            Deco(dining, "PlateR", circle, 5.65f, -2.6f, 0.36f, 0.36f, "#F8F5E8", 1f, 8);
            Solid(dining, "ChairR1", roundedSmall, 3.6f, -2.6f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairR2", roundedSmall, 6.8f, -2.6f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairR3", roundedSmall, 5.2f, -1.4f, 0.7f, 0.7f, "#8B5A3C");
            Solid(dining, "ChairR4", roundedSmall, 5.2f, -3.8f, 0.7f, 0.7f, "#8B5A3C");

            // Warm diner lighting
            foreach (var at in new[] { new Vector2(-5f, 2f), new Vector2(5f, 2f), new Vector2(0f, -2.5f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#FFE8B0", 0.09f);
            }

            var plants = IL(parts.Props, "Plants");
            Plant(plants, -8.0f, -5.2f);
            Plant(plants, 8.0f, -5.2f);
        }
    }
}

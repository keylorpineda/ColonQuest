using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The two places that had no inside: the San Isidro farm (a barn with stalls, hay and Don Beto at his desk) and the real
    /// estate agency (a wall of photographs of the houses and the agent at her desk). The windows they open are the same
    /// as before; now the player walks in and talks to somebody.
    /// </summary>
    public static partial class MapBuilder
    {
        // ---------------------------------------------------------------- the farm

        private static void BuildFarmLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "Planks"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#7A4F2A", 0.2f);

            WallPlate(parts.Labels, "FINCA", new Vector2(0f, 6.1f), 2.4f, "#2F6B4A", GamePalette.White, 0.034f);

            // Hay along the left wall and sacks of coffee along the right one.
            var hay = IL(parts.Props, "Hay");
            foreach (var at in new[] { new Vector2(-9.0f, 4.9f), new Vector2(-7.2f, 4.9f), new Vector2(-9.0f, 3.7f), new Vector2(-9.0f, 2.5f) })
            {
                Solid(hay, "HayBale", roundedSmall, at.x, at.y, 1.5f, 1.0f, "#E2B84B");
                Deco(hay, "HayBand", square, at.x, at.y, 1.5f, 0.08f, "#B58A2E", 1f, 9);
            }

            var sacks = IL(parts.Props, "CoffeeSacks");
            foreach (var at in new[] { new Vector2(8.4f, 4.9f), new Vector2(9.7f, 4.9f), new Vector2(9.0f, 3.8f), new Vector2(9.7f, 2.7f) })
            {
                Solid(sacks, "Sack", rounded, at.x, at.y, 1.0f, 1.1f, "#B58A5C");
                Deco(sacks, "SackKnot", circle, at.x, at.y + 0.45f, 0.26f, 0.2f, "#8B5A3C", 1f, 9);
            }

            // Two stalls at the back with a cow in each: wooden rails in front of them.
            var stalls = IL(parts.Props, "Stalls");
            foreach (float x in new[] { -2.8f, 2.8f })
            {
                Deco(stalls, "StallFloor", rounded, x, 4.9f, 4.2f, 2.4f, "#C9A56B", 1f, 4);
                Solid(stalls, "Cow", rounded, x, 4.8f, 1.9f, 1.0f, "#F1F4FF");
                Deco(stalls, "CowSpot", circle, x - 0.4f, 4.9f, 0.55f, 0.4f, "#2A2F3D", 1f, 9);
                Deco(stalls, "CowSpot", circle, x + 0.35f, 4.7f, 0.4f, 0.3f, "#2A2F3D", 1f, 9);
                Solid(stalls, "CowHead", circle, x + 1.05f, 4.95f, 0.65f, 0.65f, "#F1F4FF");
                Deco(stalls, "CowEye", circle, x + 1.2f, 5.05f, 0.1f, 0.1f, "#2A2F3D", 1f, 10);
                Deco(stalls, "CowNose", roundedSmall, x + 1.3f, 4.8f, 0.3f, 0.2f, "#F4A6B8", 1f, 10);
                Solid(stalls, "RailTop", square, x, 3.6f, 4.2f, 0.12f, "#8B5A3C");
                Solid(stalls, "RailBottom", square, x, 3.25f, 4.2f, 0.12f, "#8B5A3C");
            }

            // Don Beto's desk, in the middle: the player talks to him about the stake in the farm.
            var desk = IL(parts.Props, "FarmDesk");
            Solid(desk, "Desk", rounded, 0f, -0.9f, 3.4f, 0.9f, "#8B5A3C");
            Deco(desk, "Ledger", roundedSmall, -0.7f, -0.8f, 0.8f, 0.5f, "#F8F5E8", 1f, 8);
            Deco(desk, "Coffee", circle, 0.9f, -0.8f, 0.3f, 0.3f, "#5B3A22", 1f, 9);
            BuildNpc(parts.People, "DonBeto", new Vector2(0f, 0.1f), new Look { Skin = "#7FB069", Extra = Accessory.Cap, ExtraColor = "#8B5A3C" }, false);
            BuildStation(parts.Stations, "Station_Farm", StationType.Farm, "Hablar de invertir en la finca", new Vector2(0f, -2.4f));

            foreach (var at in new[] { new Vector2(-6f, 1.5f), new Vector2(6f, 1.5f), new Vector2(0f, -3f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#FFE8B0", 0.09f);
            }

            var plants = IL(parts.Props, "Plants");
            Plant(plants, -10.0f, -5.8f);
            Plant(plants, 10.0f, -5.8f);
        }

        // ---------------------------------------------------------------- the real estate agency

        private static void BuildRealEstateLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.14f);

            WallPlate(parts.Labels, "INMOBILIARIA", new Vector2(0f, 5.5f), 3.4f, "#2A7F8C", GamePalette.White, 0.034f);

            // The wall of photographs: one framed picture of each house on offer, with its tag under it.
            var photos = IL(parts.Props, "Photos");
            var homes = MarketAssumptions.Homes;
            string[] walls = { "#E8B9A0", "#B9D3F2", "#CDB9E8", "#F2E0A0" };
            string[] roofs = { "#C0504D", "#2E5E9E", "#6A4C93", "#B5822E" };
            for (int i = 0; i < homes.Count && i < 4; i++)
            {
                float x = -6.9f + i * 4.6f;
                Deco(photos, "Frame", rounded, x, 3.5f, 3.4f, 2.5f, "#2A3345", 1f, 5);
                Deco(photos, "Sky", roundedSmall, x, 3.5f, 3.1f, 2.2f, "#BFE6F7", 1f, 6);
                Deco(photos, "Grass", roundedSmall, x, 2.65f, 3.1f, 0.5f, "#7FD79A", 1f, 7);
                Deco(photos, "HouseWall", square, x, 3.35f, 1.7f, 1.0f, walls[i], 1f, 8);
                Deco(photos, "HouseRoof", roundedSmall, x, 4.05f, 2.0f, 0.5f, roofs[i], 1f, 9);
                Deco(photos, "HouseDoor", square, x + 0.15f, 3.15f, 0.34f, 0.6f, "#6B4A2E", 1f, 9);
                Deco(photos, "HouseWindow", square, x - 0.45f, 3.45f, 0.4f, 0.34f, "#FFFFFF", 1f, 9);

                var tag = IL(parts.Labels, "PhotoTag" + i);
                bool forSale = homes[i].ForSale;
                Deco(tag, "Tag", roundedSmall, x, 2.0f, 2.6f, 0.52f, forSale ? "#E8505B" : "#1D4F8C", 1f, 9);
                Label(tag, forSale ? "SE VENDE" : "SE ALQUILA", new Vector2(x, 2.0f), GamePalette.White, 0.024f, TextAnchor.MiddleCenter);
            }

            // The agent's desk, with a seat for the client in front of it.
            var desk = IL(parts.Props, "AgentDesk");
            Solid(desk, "Desk", rounded, 0f, -0.7f, 3.6f, 0.9f, "#8B5A3C");
            Deco(desk, "Monitor", roundedSmall, -0.8f, -0.6f, 0.6f, 0.45f, "#2A3040", 1f, 8);
            Deco(desk, "Screen", roundedSmall, -0.8f, -0.58f, 0.46f, 0.3f, "#4DA3FF", 1f, 9);
            Deco(desk, "Keys", roundedSmall, 0.9f, -0.7f, 0.4f, 0.3f, "#FFD23F", 1f, 9);
            Deco(desk, "Folder", roundedSmall, 0.3f, -0.7f, 0.6f, 0.4f, "#F8F5E8", 1f, 8);
            BuildNpc(parts.People, "AgenteInmobiliaria", new Vector2(0f, 0.3f), new Look { Skin = "#4DA3FF", Extra = Accessory.Tie, ExtraColor = "#2A7F8C" }, false);
            BuildStation(parts.Stations, "Station_RealEstate", StationType.RealEstate, "Hablar con la agente y ver las casas", new Vector2(0f, -2.2f));

            // A small waiting corner with two armchairs and a plant.
            var corner = IL(parts.Props, "WaitingCorner");
            Rug(corner, -7.0f, -2.2f, 4.0f, 2.4f, "#8FB8C2", "#2A7F8C");
            Solid(corner, "Armchair", rounded, -8.0f, -2.2f, 1.1f, 1.0f, "#2A7F8C");
            Solid(corner, "Armchair", rounded, -6.0f, -2.2f, 1.1f, 1.0f, "#2A7F8C");

            foreach (var at in new[] { new Vector2(-6f, 2f), new Vector2(6f, 2f), new Vector2(0f, -3f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#E8FFFF", 0.08f);
            }

            var plants = IL(parts.Props, "Plants");
            Plant(plants, 8.8f, -2.0f);
            Plant(plants, -9.0f, 4.0f);
        }
    }
}

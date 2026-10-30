using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// An electronics and appliance store, the kind that sells on installments. It carries exactly what the
    /// seller offers: phones, big screens, fridges and a motorbike. The credit desk where the seller
    /// negotiates is on the right; the cash register is by the door.
    /// </summary>
    public static partial class MapBuilder
    {
        private static void BuildStoreLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.08f);

            BuildScreensWall(parts);
            BuildWhiteGoods(parts);
            BuildPhoneTables(parts);
            BuildMotorbike(parts);
            BuildCreditDesk(parts);
            BuildCheckout(parts);

            var signs = IL(parts.Labels, "StoreSigns");
            WallPlate(signs, "CRÉDITO FÁCIL", new Vector2(-0.1f, 5.9f), 2.2f, "#E8505B", GamePalette.White, 0.03f);

            var cameras = IL(parts.Labels, "SecurityCameras");
            SecurityCamera(cameras, new Vector2(-8.6f, 5.9f));
            SecurityCamera(cameras, new Vector2(8.6f, 5.9f));

            var plants = IL(parts.Props, "Plants");
            Plant(plants, -8.3f, -5.5f);
            Plant(plants, 8.3f, -5.5f);

            foreach (var at in new[] { new Vector2(-4.5f, 2.5f), new Vector2(4.5f, 2.5f), new Vector2(-4.5f, -2.8f), new Vector2(4.5f, -2.8f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#FFFFFF", 0.07f);
            }
        }

        /// <summary>Five screens on a long cabinet against the back wall.</summary>
        private static void BuildScreensWall(InteriorParts parts)
        {
            var screens = IL(parts.Props, "Screens");
            Solid(screens, "ScreenCabinet", rounded, -4.6f, 4.95f, 7.6f, 0.8f, "#3A3F52");
            string[] pictures = { "#4DA3FF", "#55E08F", "#FFD23F", "#E8505B", "#B28DFF" };
            for (int i = 0; i < pictures.Length; i++)
            {
                float x = -7.6f + i * 1.5f;
                Deco(screens, "Tv", roundedSmall, x, 5.15f, 1.35f, 0.8f, "#1D2238", 1f, 8);
                Deco(screens, "Tv_Picture", roundedSmall, x, 5.17f, 1.15f, 0.62f, pictures[i], 1f, 9);
                Deco(screens, "Tv_Shine", roundedSmall, x - 0.35f, 5.3f, 0.25f, 0.18f, "#FFFFFF", 0.6f, 10);
                Deco(screens, "PriceTag", roundedSmall, x, 4.6f, 0.6f, 0.2f, "#FFD23F", 1f, 9);
            }

            Flickering(Glow(parts.Lights, "Light_Screens", new Vector2(-4.6f, 4.6f), 5.5f, "#4DA3FF", 0.14f, OrderLight), 0.3f, 7f, 23f);
            WallPlate(parts.Labels, "PANTALLAS", new Vector2(-4.6f, 5.9f), 1.7f, "#F8F5E8", GamePalette.Outline, 0.026f);
        }

        /// <summary>Fridges, a washer and a stove along the back right.</summary>
        private static void BuildWhiteGoods(InteriorParts parts)
        {
            var goods = IL(parts.Props, "WhiteGoods");
            Solid(goods, "Fridge", rounded, 2.2f, 4.75f, 1.0f, 1.5f, "#E3E9F2");
            Deco(goods, "Fridge_Line", square, 2.2f, 5.05f, 1.0f, 0.04f, "#9AA3B8", 1f, 8);

            Solid(goods, "DoubleFridge", rounded, 3.65f, 4.75f, 1.4f, 1.5f, "#C9CFDB");
            Deco(goods, "Fridge_Split", square, 3.65f, 4.75f, 0.04f, 1.5f, "#8E96AA", 1f, 8);

            Solid(goods, "Washer", rounded, 5.2f, 4.85f, 1.1f, 1.2f, "#E3E9F2");
            Deco(goods, "Washer_Door", circle, 5.2f, 4.8f, 0.65f, 0.65f, "#7FB6E8", 1f, 8);
            Deco(goods, "Washer_Ring", ring, 5.2f, 4.8f, 0.72f, 0.72f, "#9AA3B8", 1f, 9);

            Solid(goods, "Stove", rounded, 6.6f, 4.9f, 1.1f, 1.0f, "#E3E9F2");
            foreach (var burner in new[] { new Vector2(6.35f, 5.1f), new Vector2(6.85f, 5.1f), new Vector2(6.35f, 4.7f), new Vector2(6.85f, 4.7f) })
            {
                Deco(goods, "Burner", ring, burner.x, burner.y, 0.3f, 0.3f, "#2A3040", 1f, 8);
            }

            foreach (float x in new[] { 2.2f, 3.65f, 5.2f, 6.6f })
            {
                Deco(goods, "PriceTag", roundedSmall, x, 3.85f, 0.6f, 0.2f, "#FFD23F", 1f, 9);
            }

            WallPlate(parts.Labels, "LÍNEA BLANCA", new Vector2(4.4f, 5.9f), 2.0f, "#F8F5E8", GamePalette.Outline, 0.026f);
        }

        /// <summary>Two display tables with phones and tablets, in the middle of the floor.</summary>
        private static void BuildPhoneTables(InteriorParts parts)
        {
            var tables = IL(parts.Props, "PhoneTables");
            string[] cases = { "#2A3040", "#E8505B", "#F8F5E8", "#4DA3FF", "#FFD23F" };
            foreach (float y in new[] { 1.8f, -0.6f })
            {
                Solid(tables, "DisplayTable", rounded, -4.2f, y, 3.6f, 1.0f, "#E8E2D6");
                for (int i = 0; i < cases.Length; i++)
                {
                    float x = -5.6f + i * 0.7f;
                    Deco(tables, y > 0f ? "Phone" : "Tablet", roundedSmall, x, y + 0.05f, y > 0f ? 0.32f : 0.5f, y > 0f ? 0.55f : 0.4f, cases[i], 1f, 8);
                    Deco(tables, "Screen", roundedSmall, x, y + 0.08f, y > 0f ? 0.22f : 0.4f, y > 0f ? 0.4f : 0.28f, "#4DA3FF", 1f, 9);
                }
            }

            WallPlate(parts.Labels, "CELULARES", new Vector2(-4.2f, 2.75f), 1.6f, "#1D4F8C", GamePalette.White, 0.024f);
            WallPlate(parts.Labels, "A 24 CUOTAS", new Vector2(-4.2f, 0.35f), 1.6f, "#E8505B", GamePalette.White, 0.024f);
        }

        /// <summary>A 125 cc motorbike on a low platform, the most expensive offer.</summary>
        private static void BuildMotorbike(InteriorParts parts)
        {
            var moto = IL(parts.Props, "Motorbike");
            Solid(moto, "Platform", rounded, 7.4f, 1.6f, 2.6f, 1.5f, "#3A3F52");
            Deco(moto, "PlatformTop", rounded, 7.4f, 1.6f, 2.4f, 1.3f, "#4A5068", 1f, 8);
            Deco(moto, "Shadow", roundedSmall, 7.4f, 1.2f, 2.1f, 0.2f, "#000000", 0.35f, 9);

            // Two wheels with a tire, a rim, a hub and spokes.
            foreach (float wx in new[] { 6.65f, 8.2f })
            {
                Deco(moto, "Tire", circle, wx, 1.6f, 0.82f, 0.82f, "#1D2238", 1f, 10);
                Deco(moto, "Rim", circle, wx, 1.6f, 0.56f, 0.56f, "#C9D2E3", 1f, 11);
                Deco(moto, "RimInner", circle, wx, 1.6f, 0.46f, 0.46f, "#2A3040", 1f, 12);
                Deco(moto, "SpokeH", roundedSmall, wx, 1.6f, 0.44f, 0.035f, "#C9D2E3", 1f, 13);
                Deco(moto, "SpokeV", roundedSmall, wx, 1.6f, 0.035f, 0.44f, "#C9D2E3", 1f, 13);
                Deco(moto, "Hub", circle, wx, 1.6f, 0.14f, 0.14f, "#9AA3B8", 1f, 14);
            }

            // Frame, engine, exhaust, tank, seat, tail, fork, handlebar, mirror, headlight and brake light.
            Deco(moto, "Exhaust", roundedSmall, 7.0f, 1.42f, 0.95f, 0.12f, "#9AA3B8", 1f, 12);
            Deco(moto, "Frame", roundedSmall, 7.4f, 1.85f, 1.1f, 0.1f, "#2A3040", 1f, 12);
            Deco(moto, "Engine", rounded, 7.4f, 1.68f, 0.52f, 0.4f, "#6B7285", 1f, 13);
            foreach (float fx in new[] { 7.25f, 7.4f, 7.55f })
            {
                Deco(moto, "Fin", roundedSmall, fx, 1.68f, 0.03f, 0.3f, "#4A5068", 1f, 14);
            }

            Deco(moto, "Tank", rounded, 7.65f, 2.05f, 0.66f, 0.34f, "#E8505B", 1f, 14);
            Deco(moto, "TankShine", roundedSmall, 7.7f, 2.15f, 0.34f, 0.06f, "#FF9AA2", 1f, 15);
            Deco(moto, "Seat", roundedSmall, 7.05f, 2.03f, 0.7f, 0.18f, "#2A3040", 1f, 15);
            Deco(moto, "Tail", roundedSmall, 6.62f, 1.98f, 0.42f, 0.2f, "#E8505B", 1f, 14);
            Deco(moto, "BrakeLight", circle, 6.4f, 1.98f, 0.1f, 0.1f, "#FF3B3B", 1f, 15);
            Deco(moto, "Fork", roundedSmall, 8.08f, 1.9f, 0.07f, 0.7f, "#C9D2E3", 1f, 13);
            Deco(moto, "Handlebar", roundedSmall, 8.0f, 2.28f, 0.34f, 0.07f, "#2A3040", 1f, 15);
            Deco(moto, "Grip", circle, 8.18f, 2.28f, 0.1f, 0.1f, "#1D2238", 1f, 16);
            Deco(moto, "MirrorStem", roundedSmall, 7.96f, 2.4f, 0.03f, 0.2f, "#9AA3B8", 1f, 15);
            Deco(moto, "Mirror", circle, 7.96f, 2.52f, 0.14f, 0.14f, "#C9D2E3", 1f, 16);
            Deco(moto, "Headlight", circle, 8.34f, 2.08f, 0.22f, 0.22f, "#FFF1A8", 1f, 16);
            Deco(moto, "HeadlightGlow", circle, 8.34f, 2.08f, 0.5f, 0.5f, "#FFF1A8", 0.25f, 15);
            WallPlate(parts.Labels, "MOTO 125 CC", new Vector2(7.4f, 2.7f), 1.8f, "#1D4F8C", GamePalette.White, 0.024f);
        }

        /// <summary>The seller's desk, where the installment plans are signed. The negotiation starts here.</summary>
        private static void BuildCreditDesk(InteriorParts parts)
        {
            var desk = IL(parts.Props, "CreditDesk");
            Solid(desk, "Counter", rounded, 4.4f, -0.3f, 2.8f, 0.9f, "#D6B656");
            Deco(desk, "Monitor", roundedSmall, 5.1f, -0.2f, 0.6f, 0.42f, "#2A3040", 1f, 8);
            Deco(desk, "Monitor_Screen", roundedSmall, 5.1f, -0.2f, 0.48f, 0.3f, "#4DA3FF", 1f, 9);
            Deco(desk, "Contract", roundedSmall, 3.9f, -0.45f, 0.6f, 0.42f, "#FFFFFF", 1f, 8);
            Deco(desk, "Contract_Lines", square, 3.9f, -0.4f, 0.4f, 0.04f, "#9AA3B8", 1f, 9);
            Deco(desk, "Contract_Lines", square, 3.9f, -0.52f, 0.4f, 0.04f, "#9AA3B8", 1f, 9);
            Deco(desk, "Pen", roundedSmall, 4.35f, -0.45f, 0.06f, 0.3f, "#1D4F8C", 1f, 9);
            Deco(desk, "Chair", circle, 4.4f, 1.1f, 0.55f, 0.55f, "#2A3345", 1f, 6);
            WallPlate(parts.Labels, "CRÉDITOS", new Vector2(4.4f, -1.05f), 1.3f, "#E8505B", GamePalette.White, 0.024f);

            BuildNpc(parts.People, "Vendedor", new Vector2(4.4f, 0.55f), new Look { Skin = "#F2B84B", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, false);
            BuildStation(parts.Stations, "Station_Store", StationType.Store, "Hablar con el vendedor", new Vector2(4.4f, -1.9f));
        }

        /// <summary>The cash register by the door, for buying cash down.</summary>
        private static void BuildCheckout(InteriorParts parts)
        {
            var till = IL(parts.Props, "Checkout");
            Solid(till, "Till", rounded, -5.0f, -4.2f, 2.6f, 0.8f, "#D6B656");
            Deco(till, "Register", roundedSmall, -5.7f, -4.15f, 0.6f, 0.45f, "#4B5568", 1f, 8);
            Deco(till, "Screen", roundedSmall, -5.7f, -4.05f, 0.4f, 0.18f, "#55E08F", 1f, 9);
            Deco(till, "Bags", roundedSmall, -4.3f, -4.15f, 0.5f, 0.35f, "#F8F5E8", 1f, 8);
            WallPlate(parts.Labels, "CAJA", new Vector2(-5.0f, -4.85f), 0.9f, "#1D4F8C", GamePalette.White, 0.024f);
            BuildNpc(parts.People, "Cajero", new Vector2(-5.0f, -3.35f), new Look { Skin = "#9AD0A8", Extra = Accessory.Vest, ExtraColor = "#E8505B" }, false);

            var accessories = IL(parts.Props, "AccessoriesRack");
            Solid(accessories, "Rack", rounded, -8.4f, -1.8f, 0.6f, 2.6f, "#8B5A3C");
            string[] items = { "#E8505B", "#4DA3FF", "#FFD23F", "#55E08F", "#B28DFF" };
            for (int i = 0; i < items.Length; i++)
            {
                Deco(accessories, "Case", roundedSmall, -8.4f, -2.8f + i * 0.5f, 0.35f, 0.3f, items[i], 1f, 8);
            }
        }
    }
}

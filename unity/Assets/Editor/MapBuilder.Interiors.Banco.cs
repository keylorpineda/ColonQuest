using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// A branch office laid out like a real one. The teller line runs along the back with four numbered
    /// windows, the staff area and the vault behind it; a roped queue leads to the windows. The manager and
    /// the credit advisor have glass offices in the back corners. The lobby has the waiting chairs with the
    /// turn screen, the information desk, the ticket machine and the ATMs by the door.
    /// </summary>
    public static partial class MapBuilder
    {
        private const float TellerCounterY = 4.2f;
        private const float StaffSideX = 7.5f;
        private const float OfficeWallY = 3.6f;
        private const string BankWallFace = "#DCE3F0";

        private static readonly float[] TellerWindows = { -5.4f, -1.8f, 1.8f, 5.4f };

        private static void BuildBankLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "MarbleFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 2f, "#FFFFFF", 0.1f);
            ZoneFloor(parts.Floor, "Floor_Staff", Rect.MinMaxRect(-StaffSideX, TellerCounterY, StaffSideX, half.y - 0.8f), "#C9CFDB", 1f, false);

            BuildTellerLine(parts);
            BuildBankOffices(parts, half);
            BuildQueue(parts);
            BuildBankLobby(parts, half);

            var walls = IL(parts.Labels, "BackWall");
            Deco(walls, "Logo", roundedSmall, 4.0f, 8.0f, 2.6f, 0.44f, "#1D4F8C");
            Label(walls, "BANCO DEL PUEBLO", new Vector2(4.0f, 8.0f), GamePalette.White, 0.03f, TextAnchor.MiddleCenter);
            Deco(walls, "Clock_Frame", circle, -4.0f, 8.0f, 0.6f, 0.6f, "#2A3345");
            Deco(walls, "Clock_Face", circle, -4.0f, 8.0f, 0.48f, 0.48f, "#F8F5E8", 1f, 8);
            Deco(walls, "Clock_Hand", square, -4.0f, 8.07f, 0.04f, 0.2f, "#2A3345", 1f, 9);
            Deco(walls, "Clock_Hand", square, -3.93f, 8.0f, 0.16f, 0.04f, "#2A3345", 1f, 9);

            var cameras = IL(parts.Labels, "SecurityCameras");
            foreach (var at in new[] { new Vector2(-11.6f, 7.9f), new Vector2(11.6f, 7.9f), new Vector2(-11.6f, -8.0f), new Vector2(11.6f, -8.0f) })
            {
                SecurityCamera(cameras, at);
            }

            foreach (var at in new[] { new Vector2(-6f, -2f), new Vector2(0f, -2f), new Vector2(6f, -2f), new Vector2(-6f, -6.2f), new Vector2(6f, -6.2f), new Vector2(0f, 5.8f) })
            {
                CeilingLight(parts.Lights, at, 5.2f, "#FFFFFF", 0.07f);
            }
        }

        /// <summary>The counter with four windows. Windows 2 and 4 are open; 1 and 3 have a closed sign.</summary>
        private static void BuildTellerLine(InteriorParts parts)
        {
            var counter = IL(parts.Props, "TellerLine");
            Solid(counter, "TellerCounter", rounded, 0f, TellerCounterY, StaffSideX * 2f, 1.0f, "#7A6A52");
            Deco(counter, "Counter_Top", roundedSmall, 0f, TellerCounterY - 0.2f, StaffSideX * 2f - 0.2f, 0.35f, "#E8E2D6", 1f, 8);

            // Glass from the counter up, split between windows.
            var glass = IL(parts.Props, "TellerGlass");
            Deco(glass, "Glass", square, 0f, TellerCounterY + 0.3f, StaffSideX * 2f - 0.3f, 0.12f, "#BFE3F5", 0.75f, 9);
            for (int i = 0; i <= TellerWindows.Length; i++)
            {
                float x = i == 0 ? -StaffSideX + 0.15f : (i == TellerWindows.Length ? StaffSideX - 0.15f : (TellerWindows[i - 1] + TellerWindows[i]) * 0.5f);
                Deco(glass, "Divider", roundedSmall, x, TellerCounterY + 0.05f, 0.14f, 1.0f, "#BFE3F5", 0.8f, 9);
            }

            var windows = IL(parts.Labels, "TellerNumbers");
            var staff = IL(parts.Props, "TellerDesks");
            for (int i = 0; i < TellerWindows.Length; i++)
            {
                float x = TellerWindows[i];
                bool open = i == 1 || i == 3;
                WallPlate(windows, "CAJA " + (i + 1), new Vector2(x, TellerCounterY + 0.75f), 1.0f, open ? "#1D4F8C" : "#6B7186", GamePalette.White, 0.024f);
                Deco(staff, "Monitor", roundedSmall, x + 0.55f, TellerCounterY + 0.05f, 0.55f, 0.4f, "#2A3040", 1f, 8);
                Deco(staff, "Monitor_Screen", roundedSmall, x + 0.55f, TellerCounterY + 0.05f, 0.45f, 0.28f, "#4DA3FF", 1f, 9);
                Deco(staff, "Tray", roundedSmall, x - 0.35f, TellerCounterY - 0.25f, 0.5f, 0.2f, "#9AA3B8", 1f, 9);
                Deco(staff, "Chair", circle, x, TellerCounterY + 1.55f, 0.55f, 0.55f, "#2A3345", 1f, 6);
                if (!open)
                {
                    WallPlate(windows, "CERRADO", new Vector2(x, TellerCounterY - 0.25f), 0.9f, "#E8505B", GamePalette.White, 0.02f);
                }
            }

            BuildNpc(parts.People, "Cajera", new Vector2(TellerWindows[1], TellerCounterY + 1.1f), new Look { Skin = "#9AD0A8", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, false);
            BuildNpc(parts.People, "Cajero2", new Vector2(TellerWindows[3], TellerCounterY + 1.1f), new Look { Skin = "#B28DFF", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, false);
            BuildStation(parts.Stations, "Station_Bank", StationType.Bank, "Caja 2", new Vector2(TellerWindows[1], TellerCounterY - 1.2f));

            // The staff side: the side walls close it, the vault sits in the back wall.
            var walls = IL(parts.Walls, "StaffWalls");
            foreach (float x in new[] { -StaffSideX, StaffSideX })
            {
                InnerWall(walls, new Vector2(x, TellerCounterY - 0.5f), new Vector2(x, 7.6f), BankWallFace);
            }

            var vault = IL(parts.Props, "Vault");
            Solid(vault, "Vault", circle, 0f, 6.9f, 2.0f, 2.0f, "#8E96AA");
            Deco(vault, "Vault_Ring", ring, 0f, 6.9f, 1.5f, 1.5f, "#C9CFDB", 1f, 8);
            Deco(vault, "Vault_Wheel", circle, 0f, 6.9f, 0.5f, 0.5f, "#FFD23F", 1f, 9);
            foreach (float angle in new[] { 0f, 60f, 120f })
            {
                Tilt(Deco(vault, "Spoke", square, 0f, 6.9f, 0.9f, 0.06f, "#6B7186", 1f, 9), angle);
            }

            var cabinets = IL(parts.Props, "StaffCabinets");
            foreach (float x in new[] { -6.6f, -3.6f, 3.6f, 6.6f })
            {
                Solid(cabinets, "Cabinet", rounded, x, 7.05f, 0.9f, 0.7f, "#8E96AA");
                Deco(cabinets, "Handle", square, x, 7.05f, 0.4f, 0.06f, "#2A3040", 1f, 8);
            }
        }

        /// <summary>The manager's office (left) and the credit advisor's office (right), behind glass.</summary>
        private static void BuildBankOffices(InteriorParts parts, Vector2 half)
        {
            var walls = IL(parts.Walls, "OfficeGlass");
            WallWithDoors(walls, OfficeWallY, -half.x, -StaffSideX, "#BFE3F5", -9.75f);
            WallWithDoors(walls, OfficeWallY, StaffSideX, half.x, "#BFE3F5", 9.75f);

            foreach (float side in new[] { -1f, 1f })
            {
                float cx = side * 9.75f;
                var office = IL(parts.Props, side < 0f ? "ManagerOffice" : "CreditOffice");
                Solid(office, "Desk", rounded, cx, 6.4f, 2.6f, 0.9f, "#7A6A52");
                Deco(office, "Laptop", roundedSmall, cx - 0.5f, 6.45f, 0.7f, 0.45f, "#2A3040", 1f, 8);
                Deco(office, "Papers", roundedSmall, cx + 0.6f, 6.4f, 0.5f, 0.36f, "#FFFFFF", 1f, 8);
                Deco(office, "Chair", circle, cx, 7.2f, 0.6f, 0.6f, "#2A3345", 1f, 6);
                Solid(office, "GuestChair", roundedSmall, cx - 0.6f, 5.3f, 0.55f, 0.5f, "#3B6FB6");
                Solid(office, "GuestChair", roundedSmall, cx + 0.6f, 5.3f, 0.55f, 0.5f, "#3B6FB6");
                Plant(office, cx + side * 1.8f, 4.3f, 0.8f);
                Flickering(Glow(parts.Lights, "Light_Desk", new Vector2(cx, 6.4f), 2.8f, "#FFD23F", 0.16f, OrderLight), 0.06f, 0.9f, cx);
            }

            var plates = IL(parts.Labels, "OfficeNames");
            WallPlate(plates, "GERENCIA", new Vector2(-9.75f, 8.0f), 1.6f, "#1D4F8C", GamePalette.White, 0.026f);
            WallPlate(plates, "CRÉDITOS", new Vector2(9.75f, 8.0f), 1.6f, "#1D4F8C", GamePalette.White, 0.026f);
            NpcAt(parts.People, "Gerente", new Vector2(-9.75f, 7.15f), "#F2B84B", "#2A3345");
            NpcAt(parts.People, "Asesora", new Vector2(9.75f, 7.15f), "#F2994A", "#E8505B");
        }

        private static void NpcAt(Transform parent, string name, Vector2 at, string skin, string tie)
        {
            BuildNpc(parent, name, at, new Look { Skin = skin, Extra = Accessory.Tie, ExtraColor = tie }, false);
        }

        /// <summary>A roped zigzag in front of the windows, open at the left end.</summary>
        private static void BuildQueue(InteriorParts parts)
        {
            var queue = IL(parts.Props, "QueuePosts");
            float[] rows = { 2.4f, 1.2f };
            foreach (float y in rows)
            {
                for (float x = -3.6f; x <= 3.61f; x += 1.8f)
                {
                    Solid(queue, "Post", circle, x, y, 0.26f, 0.26f, "#FFD23F");
                }

                Deco(queue, "Rope", square, 0f, y + 0.05f, 7.2f, 0.06f, "#8B3A3A", 1f, 8);
            }

            Decal(IL(parts.Floor, "QueueSign"), "Sign", roundedSmall, -4.6f, 1.8f, 1.2f, 0.5f, "#1D4F8C", 0.9f);
            Label(parts.Labels, "FILA", new Vector2(-4.6f, 1.8f), GamePalette.White, 0.03f, TextAnchor.MiddleCenter);
        }

        private static void BuildBankLobby(InteriorParts parts, Vector2 half)
        {
            // Waiting chairs in rows facing the windows, with the turn screen above them.
            var chairs = IL(parts.Props, "WaitingChairs");
            foreach (float y in new[] { -1.2f, -2.6f, -4.0f })
            {
                Solid(chairs, "ChairRow", rounded, -8.2f, y, 4.4f, 0.7f, "#3B6FB6");
                for (float x = -10.0f; x <= -6.39f; x += 0.9f)
                {
                    Deco(chairs, "Seat", roundedSmall, x, y - 0.05f, 0.7f, 0.45f, "#6FA0E0", 1f, 8);
                }
            }

            var screen = IL(parts.Props, "TurnScreen");
            Solid(screen, "Stand", roundedSmall, -8.2f, 1.0f, 0.2f, 0.4f, "#4B5568");
            Deco(screen, "Screen", rounded, -8.2f, 1.6f, 2.6f, 1.0f, "#1D2238", 1f, 8);
            Label(screen, "TURNO A-27", new Vector2(-8.2f, 1.75f), GamePalette.Hex("#FFD23F"), 0.032f, TextAnchor.MiddleCenter);
            Label(screen, "CAJA 2", new Vector2(-8.2f, 1.4f), GamePalette.Hex("#55E08F"), 0.028f, TextAnchor.MiddleCenter);

            var ticket = IL(parts.Props, "TicketMachine");
            Solid(ticket, "Machine", rounded, -2.6f, -6.0f, 0.8f, 0.9f, "#4B5568");
            Deco(ticket, "Screen", roundedSmall, -2.6f, -5.85f, 0.55f, 0.3f, "#55E08F", 1f, 8);
            Deco(ticket, "Slot", roundedSmall, -2.6f, -6.25f, 0.45f, 0.08f, "#F8F5E8", 1f, 8);

            // The board with today's rates, the same ones the game uses.
            var rates = IL(parts.Props, "RatesBoard");
            Solid(rates, "Board", rounded, -10.6f, -6.4f, 2.2f, 1.1f, "#1D2238");
            Label(rates, "TASAS DEL MES", new Vector2(-10.6f, -6.1f), GamePalette.White, 0.024f, TextAnchor.MiddleCenter);
            Label(rates, "TARJETA 3.5%", new Vector2(-10.6f, -6.4f), GamePalette.Hex("#E8505B"), 0.024f, TextAnchor.MiddleCenter);
            Label(rates, "AHORRO 0.3%", new Vector2(-10.6f, -6.7f), GamePalette.Hex("#55E08F"), 0.024f, TextAnchor.MiddleCenter);

            // Information desk on the right, with brochures.
            var info = IL(parts.Props, "InfoDesk");
            Solid(info, "Desk", rounded, 5.6f, -4.6f, 2.8f, 0.8f, "#7A6A52");
            Deco(info, "Desk_Top", roundedSmall, 5.6f, -4.75f, 2.6f, 0.3f, "#E8E2D6", 1f, 8);
            Deco(info, "Bell", circle, 4.7f, -4.65f, 0.2f, 0.2f, "#FFD23F", 1f, 9);
            WallPlate(parts.Labels, "INFORMACIÓN", new Vector2(5.6f, -5.25f), 1.8f, "#1D4F8C", GamePalette.White, 0.024f);
            NpcAt(parts.People, "Recepcionista", new Vector2(5.6f, -3.75f), "#F2994A", "#1D4F8C");
            Solid(info, "BrochureRack", rounded, 3.4f, -4.6f, 0.7f, 0.9f, "#B2475A");
            Deco(info, "Brochure", roundedSmall, 3.4f, -4.5f, 0.45f, 0.55f, "#F8F5E8", 1f, 8);

            // ATMs by the door, under their own sign.
            var atms = IL(parts.Props, "Atms");
            foreach (float y in new[] { -1.0f, -2.6f, -4.2f })
            {
                Solid(atms, "Atm", rounded, 11.35f, y, 0.9f, 1.3f, "#4B5568");
                Deco(atms, "Screen", roundedSmall, 11.3f, y + 0.25f, 0.6f, 0.45f, "#55E08F", 1f, 8);
                Deco(atms, "Keypad", roundedSmall, 11.3f, y - 0.35f, 0.5f, 0.25f, "#2A3040", 1f, 8);
                Flickering(Glow(parts.Lights, "Light_Atm", new Vector2(10.9f, y), 2.0f, "#55E08F", 0.14f, OrderLight), 0.15f, 2.2f, y);
            }

            WallPlate(parts.Labels, "CAJEROS", new Vector2(11.0f, 0.2f), 1.4f, "#1D4F8C", GamePalette.White, 0.024f);

            // The guard by the door, a flag and plants.
            NpcAt(parts.People, "Guardia", new Vector2(2.4f, -6.6f), "#4DA3FF", "#1D2238");
            Rug(IL(parts.Floor, "EntranceRunner"), 0f, -4.4f, 1.9f, 4.8f, "#8B3A3A", "#6B2A2A");

            var flag = IL(parts.Props, "Flag");
            Solid(flag, "Pole", roundedSmall, -3.9f, -7.0f, 0.12f, 1.6f, "#C9CFDB");
            string[] bands = { "#2D5FB8", "#F8F5E8", "#E8505B", "#F8F5E8", "#2D5FB8" };
            for (int i = 0; i < bands.Length; i++)
            {
                Deco(flag, "Band", square, -3.45f, -6.35f + (2 - i) * 0.1f, 0.8f, i == 2 ? 0.14f : 0.1f, bands[i], 1f, 8);
            }

            var plants = IL(parts.Props, "Plants");
            foreach (var at in new[] { new Vector2(-11.3f, -7.8f), new Vector2(11.3f, -7.8f), new Vector2(-11.3f, 2.9f), new Vector2(11.3f, 2.9f) })
            {
                Plant(plants, at.x, at.y, 1.1f);
            }

            var bins = IL(parts.Props, "Bins");
            Solid(bins, "Bin", roundedSmall, 9.6f, -7.8f, 0.5f, 0.6f, "#4A6B5A");
        }
    }
}

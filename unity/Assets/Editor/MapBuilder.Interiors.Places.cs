using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The newer buildings of the town: the car agency, the police station with its cells, the hospital with its
    /// upper floor and the neighbor's house. Same drawing helpers as the first three interiors; the furniture is
    /// all code, and every price written on a tag comes from <see cref="MarketAssumptions"/>.
    /// </summary>
    public static partial class MapBuilder
    {
        private static string Spaced(long amount)
        {
            return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');
        }

        /// <summary>A wall of glass: see-through, with a frame, that still stops the player.</summary>
        private static void GlassWall(Transform parent, Vector2 from, Vector2 to)
        {
            bool horizontal = Mathf.Abs(to.y - from.y) < 0.01f;
            var center = (from + to) * 0.5f;
            var size = horizontal ? new Vector2(Mathf.Abs(to.x - from.x), 0.16f) : new Vector2(0.16f, Mathf.Abs(to.y - from.y));
            Deco(parent, "GlassFrame", square, center.x, center.y, size.x, size.y, "#2A3345", 1f, 9);
            var pane = horizontal ? new Vector2(size.x, 0.7f) : new Vector2(0.5f, size.y);
            Deco(parent, "Glass", square, center.x, center.y + (horizontal ? -0.35f : 0f), pane.x, pane.y, "#BFE3F5", 0.32f, 8);
            Wall(parent, "GlassCollider", center, size);
        }

        private static void BuildExtraSpawns(Transform map, string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.Carcel:
                    BuildSpawnPoint(map, SpawnIds.JailCell, new Vector2(-7.6f, 3.0f));
                    break;
                case SceneNames.Hospital:
                    BuildSpawnPoint(map, SpawnIds.HospitalBed, new Vector2(6.4f, -2.6f));
                    BuildSpawnPoint(map, SpawnIds.HospitalFromAbove, new Vector2(-8.4f, 4.0f));
                    break;
            }
        }

        // ---------------------------------------------------------------- car agency

        private static void BuildAgencyLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.14f);

            // The showroom: a red carpet with the three cars of the agency and a price tag in front of each.
            var show = IL(parts.Props, "Showroom");
            Rug(show, -5.0f, 4.1f, 12.0f, 3.6f, "#C0504D", "#8A3330");
            string[] colors = { "#4DA3FF", "#E8505B", "#2F8A58" };
            string[] names = { "COMPACTO", "SEDAN", "SUV" };
            for (int i = 0; i < MarketAssumptions.Cars.Count && i < 3; i++)
            {
                float x = -8.4f + i * 3.7f;
                Car(show, show, x, 4.3f, colors[i]);
                var tag = IL(parts.Labels, "PriceTag" + i);
                Deco(tag, "Tag", roundedSmall, x, 2.75f, 2.4f, 0.62f, "#FFD23F", 1f, 9);
                Label(tag, names[i] + " " + MarketAssumptions.Cars[i].Name.Split(' ')[^1], new Vector2(x, 2.9f), GamePalette.Outline, 0.022f, TextAnchor.MiddleCenter);
                Label(tag, Spaced(MarketAssumptions.Cars[i].Price), new Vector2(x, 2.65f), GamePalette.Outline, 0.03f, TextAnchor.MiddleCenter);
            }

            WallPlate(parts.Labels, "AUTOS DON CHEPE", new Vector2(-5.0f, 6.0f), 3.2f, "#C0504D", GamePalette.White, 0.03f);

            // Two salesmen's desks in the middle of the floor.
            foreach (float x in new[] { -6.4f, -2.4f })
            {
                var desk = IL(parts.Props, "SalesDesk");
                Solid(desk, "Desk", rounded, x, -0.9f, 2.2f, 0.9f, "#8B5A3C");
                Deco(desk, "Monitor", roundedSmall, x - 0.4f, -0.8f, 0.55f, 0.4f, "#2A3040", 1f, 8);
                Deco(desk, "Screen", roundedSmall, x - 0.4f, -0.78f, 0.42f, 0.28f, "#4DA3FF", 1f, 9);
                Deco(desk, "Papers", roundedSmall, x + 0.5f, -0.9f, 0.5f, 0.35f, "#FFFFFF", 1f, 8);
                Solid(desk, "Chair", circle, x, 0.0f, 0.55f, 0.55f, "#2A3345");
            }

            BuildNpc(parts.People, "Vendedor1", new Vector2(-6.4f, 0.55f), new Look { Skin = "#4DA3FF", Extra = Accessory.Tie, ExtraColor = "#C0504D" }, false);
            BuildNpc(parts.People, "Vendedor2", new Vector2(-2.4f, 0.55f), new Look { Skin = "#B28DFF", Extra = Accessory.Tie, ExtraColor = "#2A7F8C" }, false);

            // Don Chepe's reception desk, by the way in: he welcomes the player here and the car negotiation opens here.
            var reception = IL(parts.Props, "Reception");
            Solid(reception, "Desk", rounded, 1.9f, -3.6f, 3.0f, 0.9f, "#8B5A3C");
            Deco(reception, "Bell", circle, 1.1f, -3.5f, 0.2f, 0.2f, "#FFD23F", 1f, 9);
            Deco(reception, "Contract", roundedSmall, 2.6f, -3.55f, 0.6f, 0.42f, "#FFFFFF", 1f, 8);
            Deco(reception, "Pen", roundedSmall, 2.2f, -3.55f, 0.06f, 0.3f, "#1D4F8C", 1f, 9);
            BuildNpc(parts.People, "DonChepe", new Vector2(1.9f, -2.65f), new Look { Skin = "#F2994A", Extra = Accessory.Tie, ExtraColor = "#C0504D" }, false);
            BuildStation(parts.Stations, "Station_CarDealer", StationType.CarDealer, "Hablar con Don Chepe y ver los carros", new Vector2(1.9f, -4.8f));
            WallPlate(parts.Labels, "BIENVENIDO", new Vector2(1.9f, -4.4f), 1.5f, "#1D4F8C", GamePalette.White, 0.024f);

            // The financing office behind glass.
            BuildFinancingOffice(parts);

            // The waiting room with free coffee.
            BuildWaitingRoom(parts);

            // The workshop, visible at the back through two wide openings.
            BuildWorkshop(parts);

            foreach (var at in new[] { new Vector2(-5f, 3f), new Vector2(-5f, -3f), new Vector2(6f, -2f), new Vector2(7f, 4f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#FFFFFF", 0.07f);
            }

            SecurityCamera(IL(parts.Labels, "SecurityCameras"), new Vector2(-10.2f, 6.1f));
            var plants = IL(parts.Props, "Plants");
            Plant(plants, 10.2f, -6.0f);
            Plant(plants, -2.0f, -6.0f);
        }

        private static void BuildFinancingOffice(InteriorParts parts)
        {
            var office = IL(parts.Walls, "FinancingOffice");
            float xMin = 4.4f;
            float xMax = 10.6f;
            float yMin = -3.0f;
            float yMax = 1.4f;
            GlassWall(office, new Vector2(xMin, yMin), new Vector2(xMin, yMax));
            GlassWall(office, new Vector2(xMin, yMin), new Vector2(5.3f, yMin));
            GlassWall(office, new Vector2(6.6f, yMin), new Vector2(xMax, yMin));
            GlassWall(office, new Vector2(xMin, yMax), new Vector2(xMax, yMax));
            OnGroundThreshold(office, new Vector2(5.95f, yMin), 1.3f);

            var inside = IL(parts.Props, "FinancingDesk");
            Solid(inside, "Desk", rounded, 7.8f, -0.4f, 2.8f, 0.9f, "#8B5A3C");
            Deco(inside, "Monitor", roundedSmall, 8.4f, -0.3f, 0.6f, 0.42f, "#2A3040", 1f, 8);
            Deco(inside, "Screen", roundedSmall, 8.4f, -0.28f, 0.48f, 0.3f, "#55E08F", 1f, 9);
            Deco(inside, "Contract", roundedSmall, 7.2f, -0.5f, 0.7f, 0.5f, "#FFFFFF", 1f, 8);
            Deco(inside, "Fine print", square, 7.2f, -0.55f, 0.5f, 0.03f, "#9AA3B8", 1f, 9);
            Deco(inside, "Fine print", square, 7.2f, -0.45f, 0.5f, 0.03f, "#9AA3B8", 1f, 9);
            Solid(inside, "Chair", circle, 7.8f, -1.3f, 0.5f, 0.5f, "#3B6FB6");
            BuildNpc(parts.People, "Financiera", new Vector2(7.8f, 0.6f), new Look { Skin = "#FF8FB1", Extra = Accessory.Vest, ExtraColor = "#2A7F8C" }, false);
            WallPlate(parts.Labels, "FINANCIAMIENTO", new Vector2(7.5f, 1.9f), 2.4f, "#1D4F8C", GamePalette.White, 0.026f);
            BuildThing(parts.Stations, "FinePrint", "Leer el contrato", new Vector2(7.2f, -0.5f), new Vector2(1.0f, 0.8f),
                "La letra chiquita es la que manda: seguro, comisión, plazo y tasa. Léela completa antes de firmar, no después.",
                "Una cuota baja no quiere decir un carro barato. Multiplica la cuota por los meses y compara con el precio de contado.");
        }

        private static void BuildWaitingRoom(InteriorParts parts)
        {
            var waiting = IL(parts.Props, "WaitingRoom");
            Rug(waiting, -8.0f, -4.5f, 5.0f, 3.0f, "#7DA6D6", "#4A6FA0");
            Solid(waiting, "Sofa", rounded, -8.7f, -5.5f, 2.6f, 0.8f, "#C0504D");
            Solid(waiting, "Sofa", rounded, -8.7f, -3.5f, 2.6f, 0.8f, "#C0504D");
            Solid(waiting, "CoffeeTable", rounded, -6.9f, -4.5f, 0.9f, 0.9f, "#E8E2D6");
            Deco(waiting, "Cup", circle, -6.9f, -4.45f, 0.22f, 0.22f, "#FFFFFF", 1f, 8);
            Deco(waiting, "Magazine", roundedSmall, -7.15f, -4.6f, 0.3f, 0.22f, "#FFD23F", 1f, 8);

            var coffee = IL(parts.Props, "CoffeeCorner");
            Solid(coffee, "Counter", rounded, -10.0f, -2.2f, 1.0f, 1.8f, "#D6B656");
            Deco(coffee, "Machine", roundedSmall, -10.0f, -1.8f, 0.55f, 0.6f, "#2A3040", 1f, 8);
            Deco(coffee, "Light", circle, -9.9f, -1.7f, 0.1f, 0.1f, "#E8505B", 1f, 9);
            Deco(coffee, "Cups", roundedSmall, -10.0f, -2.5f, 0.4f, 0.2f, "#FFFFFF", 1f, 8);
            WallPlate(parts.Labels, "CAFÉ GRATIS", new Vector2(-9.0f, -0.9f), 1.7f, "#8B5A3C", GamePalette.White, 0.024f);
            BuildThing(parts.Stations, "Coffee", "Tomar un café", new Vector2(-9.2f, -2.2f), new Vector2(1.2f, 1.6f),
                "El café es gratis. Lo que nunca es gratis es la comisión de apertura.",
                "Mientras esperas, haz cuentas: la prima, la cuota por los meses, el seguro y el marchamo de cada año.");
        }

        private static void BuildWorkshop(InteriorParts parts)
        {
            var shop = IL(parts.Floor, "WorkshopFloor");
            ZoneFloor(shop, "WorkshopZone", new Rect(3.2f, 2.5f, 7.8f, 4.0f), "#8E97AD", 1f, false);
            WallWithDoors(IL(parts.Walls, "WorkshopWall"), 2.4f, 3.2f, 10.9f, "#9AA3B8", 5.0f, 9.0f);
            WallPlate(parts.Labels, "TALLER", new Vector2(7.0f, 6.1f), 1.4f, "#FFD23F", GamePalette.Outline, 0.028f);

            var lift = IL(parts.Props, "Lift");
            Solid(lift, "Post", rounded, 5.4f, 4.6f, 0.4f, 1.8f, "#FFD23F");
            Solid(lift, "Post", rounded, 8.6f, 4.6f, 0.4f, 1.8f, "#FFD23F");
            Car(lift, lift, 7.0f, 4.7f, "#9AA3B8");

            var tools = IL(parts.Props, "Tools");
            Solid(tools, "ToolCabinet", rounded, 10.2f, 5.4f, 0.9f, 1.4f, "#E8505B");
            Deco(tools, "Drawer", square, 10.2f, 5.2f, 0.8f, 0.04f, "#8A3330", 1f, 8);
            Deco(tools, "Drawer", square, 10.2f, 5.6f, 0.8f, 0.04f, "#8A3330", 1f, 8);
            Solid(tools, "Tires", circle, 3.9f, 5.6f, 0.7f, 0.7f, "#1D2238");
            Solid(tools, "Tires", circle, 3.9f, 5.0f, 0.7f, 0.7f, "#2A3040");
            BuildNpc(parts.People, "Mecanico", new Vector2(9.0f, 3.6f), new Look { Skin = "#3C78D8", Extra = Accessory.Helmet, ExtraColor = "#FFD23F" }, false);
            BuildThing(parts.Stations, "Workshop", "Mirar el taller", new Vector2(7.0f, 3.2f), new Vector2(3.0f, 1.0f),
                "Un carro no es solo la cuota. Aquí se paga el mantenimiento, las llantas y las reparaciones, cada año.");
        }

        // ---------------------------------------------------------------- police station and jail

        private static void BuildJailLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.1f);

            // Four cells along the back. Each one has bars at the front, a gate that is open and a hard bed.
            var cells = IL(parts.Walls, "Cells");
            float[] centers = { -7.6f, -2.6f, 2.6f, 7.6f };
            for (int i = 0; i < centers.Length; i++)
            {
                float x = centers[i];
                float left = x - 2.2f;
                float right = x + 2.2f;
                InnerWall(cells, new Vector2(left, 0.9f), new Vector2(left, 6.2f), "#6B7285");
                InnerWall(cells, new Vector2(right, 0.9f), new Vector2(right, 6.2f), "#6B7285");
                BarsRun(cells, left, x - 0.7f, 0.9f);
                BarsRun(cells, x + 0.7f, right, 0.9f);

                var bed = IL(parts.Props, "CellBed" + i);
                Solid(bed, "Bunk", rounded, x - 0.9f, 4.9f, 1.6f, 0.9f, "#8E97AD");
                Deco(bed, "Blanket", roundedSmall, x - 0.9f, 4.95f, 1.3f, 0.5f, "#C0504D", 1f, 8);
                Solid(bed, "Toilet", roundedSmall, x + 1.4f, 5.6f, 0.5f, 0.5f, "#E3E9F2");
            }

            WallPlate(parts.Labels, "CELDAS", new Vector2(0f, 6.1f), 1.6f, "#2A3345", GamePalette.White, 0.028f);

            // The counter of the officer, in the middle of the room.
            var counter = IL(parts.Props, "Counter");
            Solid(counter, "Counter", rounded, -2.4f, -1.9f, 4.2f, 1.0f, "#6B7A99");
            Deco(counter, "Computer", roundedSmall, -3.2f, -1.8f, 0.7f, 0.45f, "#2A3040", 1f, 8);
            Deco(counter, "Screen", roundedSmall, -3.2f, -1.78f, 0.56f, 0.3f, "#55E08F", 1f, 9);
            Deco(counter, "Forms", roundedSmall, -1.5f, -1.85f, 0.6f, 0.4f, "#FFFFFF", 1f, 8);
            BuildNpc(parts.People, "Oficial", new Vector2(-2.4f, -0.9f), new Look { Skin = "#3C78D8", Extra = Accessory.Cap, ExtraColor = "#1D2238" }, false);
            WallPlate(parts.Labels, "MOSTRADOR", new Vector2(-2.4f, -2.7f), 1.6f, "#1D4F8C", GamePalette.White, 0.024f);
            BuildThing(parts.Stations, "Counter", "Hablar con el oficial", new Vector2(-2.4f, -2.9f), new Vector2(3.0f, 0.8f),
                "Las multas se pagan, y una multa que no se paga crece. Aquí quedan guardados los antecedentes.");

            // The judge's bench, on a step, with the scales of justice on the wall.
            var bench = IL(parts.Props, "JudgeBench");
            Rug(bench, 6.6f, -2.0f, 4.6f, 2.8f, "#8B5A3C", "#5A3A22");
            Solid(bench, "Bench", rounded, 6.6f, -1.6f, 3.6f, 1.0f, "#5A3A22");
            Deco(bench, "Gavel", roundedSmall, 7.6f, -1.5f, 0.4f, 0.18f, "#C58B52", 1f, 9);
            Deco(bench, "Plate", roundedSmall, 5.8f, -1.65f, 0.8f, 0.24f, "#FFD23F", 1f, 9);
            BuildNpc(parts.People, "Juez", new Vector2(6.6f, -0.55f), new Look { Skin = "#B28DFF", Extra = Accessory.Hood, ExtraColor = "#1D2238" }, false);
            WallPlate(parts.Labels, "JUZGADO", new Vector2(6.6f, -2.7f), 1.4f, "#1D2238", GamePalette.White, 0.024f);
            BuildThing(parts.Stations, "Judge", "Hablar con el juez", new Vector2(6.6f, -2.9f), new Vector2(3.4f, 0.8f),
                "Entrar a una casa ajena sale caro: la multa y los días que dejas de trabajar. Un antecedente también te cierra puertas cuando pides un préstamo.",
                "Una multa o un antecedente cuesta plata y tiempo. Esos días en el calabozo no se cobran salario.");

            var benches = IL(parts.Props, "WaitingBench");
            Solid(benches, "Bench", rounded, -7.6f, -3.8f, 3.0f, 0.6f, "#8B5A3C");
            Solid(benches, "Bench", rounded, -7.6f, -2.6f, 3.0f, 0.6f, "#8B5A3C");
            Plant(IL(parts.Props, "Plants"), 9.2f, -5.6f);

            foreach (var at in new[] { new Vector2(-7f, 3f), new Vector2(0f, 3f), new Vector2(7f, 3f), new Vector2(0f, -2.5f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#DDE6FF", 0.07f);
            }

            SecurityCamera(IL(parts.Labels, "SecurityCameras"), new Vector2(9.4f, 6.0f));
        }

        private static void BarsRun(Transform parent, float xFrom, float xTo, float y)
        {
            Deco(parent, "BarTop", square, (xFrom + xTo) * 0.5f, y + 0.3f, xTo - xFrom, 0.08f, "#2A3345", 1f, 9);
            Wall(parent, "BarsCollider", new Vector2((xFrom + xTo) * 0.5f, y), new Vector2(xTo - xFrom, 0.2f));
            for (float x = xFrom + 0.15f; x < xTo; x += 0.3f)
            {
                Deco(parent, "Bar", square, x, y, 0.07f, 0.9f, "#4B5568", 1f, 9);
            }
        }

        // ---------------------------------------------------------------- hospital

        private static void BuildHospitalLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.2f);

            // Reception, on the left by the entrance: the nurse and the health insurance desk.
            var reception = IL(parts.Props, "Reception");
            Solid(reception, "Desk", rounded, -6.8f, -3.4f, 4.4f, 1.0f, "#E3E9F2");
            Deco(reception, "Cross", square, -6.8f, -3.4f, 0.5f, 0.14f, "#E8505B", 1f, 9);
            Deco(reception, "Cross", square, -6.8f, -3.4f, 0.14f, 0.5f, "#E8505B", 1f, 9);
            Deco(reception, "Computer", roundedSmall, -8.0f, -3.3f, 0.7f, 0.45f, "#2A3040", 1f, 8);
            Deco(reception, "Screen", roundedSmall, -8.0f, -3.28f, 0.56f, 0.3f, "#4DA3FF", 1f, 9);
            BuildNpc(parts.People, "Enfermera", new Vector2(-6.8f, -2.4f), new Look { Skin = "#55E08F", Extra = Accessory.Cap, ExtraColor = "#FFFFFF" }, false);
            BuildStation(parts.Stations, "Station_HealthDesk", StationType.HealthDesk, "Hablar de un seguro de salud", new Vector2(-6.8f, -4.6f));
            WallPlate(parts.Labels, "RECEPCIÓN Y SEGURO", new Vector2(-6.8f, -4.15f), 2.8f, "#1D4F8C", GamePalette.White, 0.024f);

            // The waiting area.
            var waiting = IL(parts.Props, "WaitingArea");
            Rug(waiting, -9.0f, 0.2f, 4.4f, 3.4f, "#7DD0C8", "#3E9A90");
            for (int i = 0; i < 3; i++)
            {
                Solid(waiting, "Chair", rounded, -10.6f + i * 1.4f, 1.1f, 1.0f, 0.6f, "#4DA3FF");
                Solid(waiting, "Chair", rounded, -10.6f + i * 1.4f, -0.5f, 1.0f, 0.6f, "#4DA3FF");
            }

            // The emergency ward on the right: four beds with curtains and monitors.
            var ward = IL(parts.Props, "EmergencyWard");
            Rug(ward, 7.0f, -2.6f, 9.0f, 5.8f, "#C9E8EF", "#8FC3DA");
            for (int i = 0; i < 4; i++)
            {
                float x = 3.8f + (i % 2) * 6.0f;
                float y = i < 2 ? -0.4f : -4.4f;
                Solid(ward, "Bed", rounded, x, y, 2.0f, 1.0f, "#FFFFFF");
                Deco(ward, "Pillow", roundedSmall, x - 0.7f, y, 0.5f, 0.4f, "#9AD0E8", 1f, 8);
                Deco(ward, "Sheet", roundedSmall, x + 0.2f, y, 1.2f, 0.9f, "#7FB6E8", 1f, 8);
                Deco(ward, "Curtain", roundedSmall, x, y + 0.9f, 2.4f, 0.14f, "#B28DFF", 0.8f, 9);
                Solid(ward, "Monitor", roundedSmall, x + 1.5f, y + 0.3f, 0.5f, 0.6f, "#2A3040");
                Deco(ward, "Beat", square, x + 1.5f, y + 0.3f, 0.34f, 0.04f, "#55E08F", 1f, 9);
            }

            BuildNpc(parts.People, "Doctora", new Vector2(5.0f, -2.4f), new Look { Skin = "#FF8FB1", Extra = Accessory.Vest, ExtraColor = "#FFFFFF" }, false);
            WallPlate(parts.Labels, "URGENCIAS", new Vector2(7.0f, 6.1f), 2.0f, "#E8505B", GamePalette.White, 0.03f);
            BuildThing(parts.Stations, "EmergencyWard", "Mirar las camas", new Vector2(7.0f, -2.4f), new Vector2(4.0f, 1.2f),
                "Una noche en urgencias se paga entera cuando no hay seguro. El fondo de emergencia existe para golpes así.");

            // The stairs to the consulting rooms, at the back left.
            var stairs = IL(parts.Props, "Stairs");
            for (int i = 0; i < 5; i++)
            {
                Deco(stairs, "Step", square, -8.4f, 3.6f + i * 0.45f, 2.4f, 0.4f, i % 2 == 0 ? "#B7C0D0" : "#9AA3B8", 1f, 7);
            }

            var door = new GameObject("Door_Upstairs");
            door.transform.SetParent(parts.Stations, false);
            door.transform.localPosition = new Vector2(-8.4f, 5.9f);
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(2.0f, 0.8f);
            trigger.isTrigger = true;
            door.AddComponent<Door>().Configure(SceneNames.HospitalArriba, SpawnIds.FromTown, Vector2.up);
            WallPlate(parts.Labels, "CONSULTORIOS ARRIBA", new Vector2(-8.4f, 6.4f), 3.0f, "#1D4F8C", GamePalette.White, 0.024f);

            foreach (var at in new[] { new Vector2(-7f, 0f), new Vector2(3f, -2f), new Vector2(9f, -2f), new Vector2(-5f, -4f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#E8FFFF", 0.08f);
            }

            SecurityCamera(IL(parts.Labels, "SecurityCameras"), new Vector2(11.4f, 6.0f));
            Plant(IL(parts.Props, "Plants"), -11.0f, -5.8f);
        }

        private static void BuildHospitalUpperLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            FloorChecks(IL(parts.Floor, "TileFloor"), Rect.MinMaxRect(-half.x + 0.5f, -half.y + 0.5f, half.x - 0.5f, half.y - 0.8f), 1f, "#FFFFFF", 0.2f);

            // Three consulting rooms and the lab along the back, with a door each.
            var walls = IL(parts.Walls, "ConsultingRooms");
            float[] doors = { -7.8f, -2.6f, 2.6f, 7.8f };
            WallWithDoors(walls, 0.8f, -11f, 11f, "#9AA3B8", doors);
            foreach (float x in new[] { -5.2f, 0f, 5.2f })
            {
                InnerWall(walls, new Vector2(x, 0.8f), new Vector2(x, 5.7f), "#9AA3B8");
            }

            string[] names = { "CONSULTORIO 1", "CONSULTORIO 2", "CONSULTORIO 3", "LABORATORIO" };
            string[] lines =
            {
                "Una consulta sin seguro se paga completa, cada vez. Con seguro, la cuenta baja mucho.",
                "Prevenir sale más barato que curar: un chequeo a tiempo evita una cuenta grande después.",
                "El seguro no es plata perdida: es el amortiguador que te deja seguir pagando tus cuotas si algo pasa."
            };
            for (int i = 0; i < 4; i++)
            {
                float x = doors[i];
                var office = IL(parts.Props, "Office" + i);
                Solid(office, "Desk", rounded, x, 4.6f, 2.0f, 0.8f, i == 3 ? "#C9E8EF" : "#E8E2D6");
                Deco(office, "Monitor", roundedSmall, x - 0.4f, 4.7f, 0.5f, 0.38f, "#2A3040", 1f, 8);
                Deco(office, "Screen", roundedSmall, x - 0.4f, 4.72f, 0.38f, 0.26f, "#4DA3FF", 1f, 9);
                if (i < 3)
                {
                    Solid(office, "ExamBed", rounded, x + 0.4f, 2.3f, 1.6f, 0.8f, "#FFFFFF");
                    BuildNpc(parts.People, "Doctor" + (i + 1), new Vector2(x - 0.5f, 3.5f), new Look { Skin = i == 1 ? "#4DA3FF" : "#FF8FB1", Extra = Accessory.Vest, ExtraColor = "#FFFFFF" }, false);
                    BuildThing(parts.Stations, "Doctor" + (i + 1), "Hablar con el doctor", new Vector2(x - 0.5f, 2.6f), new Vector2(2.0f, 1.0f), lines[i]);
                }
                else
                {
                    Solid(office, "Microscope", roundedSmall, x + 0.4f, 3.0f, 0.5f, 0.7f, "#2A3040");
                    Solid(office, "Samples", roundedSmall, x - 0.6f, 3.0f, 0.9f, 0.5f, "#B28DFF");
                    BuildNpc(parts.People, "Laboratorista", new Vector2(x - 0.4f, 3.7f), new Look { Skin = "#55E08F", Extra = Accessory.Cap, ExtraColor = "#FFFFFF" }, false);
                }

                WallPlate(parts.Labels, names[i], new Vector2(x, 6.15f), 2.2f, "#1D4F8C", GamePalette.White, 0.022f);
            }

            // The corridor: chairs against the wall and the stairs down by the left end.
            var corridor = IL(parts.Props, "Corridor");
            for (int i = 0; i < 4; i++)
            {
                Solid(corridor, "Chair", rounded, 2.0f + i * 1.8f, -2.0f, 1.0f, 0.6f, "#4DA3FF");
            }

            Rug(corridor, 0f, -0.7f, 20f, 1.2f, "#7DD0C8", "#3E9A90");
            WallPlate(parts.Labels, "ESCALERAS", new Vector2(-8.0f, -5.7f), 1.6f, "#2A3345", GamePalette.White, 0.024f);
            Plant(IL(parts.Props, "Plants"), 10.0f, -5.4f);

            foreach (var at in new[] { new Vector2(-7f, 3f), new Vector2(0f, 3f), new Vector2(7f, 3f), new Vector2(0f, -3f) })
            {
                CeilingLight(parts.Lights, at, 5.0f, "#E8FFFF", 0.08f);
            }
        }

        // ---------------------------------------------------------------- the neighbor's house

        /// <summary>
        /// The same rooms as the player's house, with a neighbor in the living room. The player has no business
        /// here: the owner starts counting down as soon as the player comes in.
        /// </summary>
        private static void BuildNeighborLayout(InteriorParts parts, Room room)
        {
            BuildHouseLayout(parts, room);

            // The statement desk belongs to the player's own house, not to this one.
            foreach (var station in Object.FindObjectsByType<Station>(FindObjectsSortMode.None))
            {
                if (station.Type == StationType.Home)
                {
                    Object.DestroyImmediate(station.gameObject);
                }
            }

            BuildNpc(parts.People, "DonMario", new Vector2(3.0f, 0.4f), new Look { Skin = "#F2994A", Extra = Accessory.Cap, ExtraColor = "#2A7F8C" }, false);

            // The officer is not here until the owner calls: he comes in by the door and goes straight to the player.
            var officerNpc = BuildNpc(parts.People, "Oficial", MatFor(SceneNames.Vecino) + new Vector2(0f, 0.3f), new Look { Skin = "#3C78D8", Extra = Accessory.Cap, ExtraColor = "#1D2238" }, false);
            var officer = officerNpc.gameObject.AddComponent<PoliceOfficer>();
            officerNpc.gameObject.SetActive(false);

            var watch = new GameObject("TrespassWatch");
            var trespass = watch.AddComponent<TrespassWatch>();
            trespass.Configure("Don Mario", MarketAssumptions.TrespassGraceSeconds);
            trespass.SetOfficer(officer);
        }
    }
}

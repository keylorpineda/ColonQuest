using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The rented room where the player starts: stained walls, a mattress on the floor, a bare bulb and a plastic chair. The bed,
    /// the fridge, the TV and the table are built too, each one in a group with a <see cref="RoomPiece"/> that keeps it hidden
    /// until it is bought at the store.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly Vector2 RoomBedAt = new Vector2(-3f, 1.1f);
        private static readonly Vector2 RoomFridgeAt = new Vector2(3.9f, 2.0f);
        private static readonly Vector2 RoomTvAt = new Vector2(0.7f, 2.3f);
        private static readonly Vector2 RoomTableAt = new Vector2(2.7f, -0.6f);

        private static void BuildRoomLayout(InteriorParts parts, Room room)
        {
            BuildRoomWear(parts, room);

            // The mattress, on the floor, until the bed arrives. The place to sleep is the same with either.
            var mattress = Piece(parts.Props, "Mattress", "Bed", true);
            Solid(mattress, "Mattress", rounded, RoomBedAt.x, RoomBedAt.y, 2.0f, 2.3f, "#CFC6AE");
            Deco(mattress, "Stain", circle, RoomBedAt.x + 0.4f, RoomBedAt.y - 0.5f, 0.7f, 0.5f, "#A89A78", 0.7f, 8);
            Deco(mattress, "Stain", circle, RoomBedAt.x - 0.5f, RoomBedAt.y + 0.1f, 0.4f, 0.3f, "#A89A78", 0.6f, 8);
            Deco(mattress, "Blanket", roundedSmall, RoomBedAt.x, RoomBedAt.y - 0.75f, 1.9f, 0.7f, "#7C8598", 1f, 9);
            Deco(mattress, "Pillow", roundedSmall, RoomBedAt.x, RoomBedAt.y + 0.8f, 0.9f, 0.4f, "#E4DFD2", 1f, 9);

            var bed = Piece(parts.Props, "Bed", "Bed", false);
            Solid(bed, "Frame", rounded, RoomBedAt.x, RoomBedAt.y, 2.3f, 2.5f, "#8B5A3C");
            Deco(bed, "Sheet", rounded, RoomBedAt.x, RoomBedAt.y - 0.05f, 2.05f, 2.2f, "#F2EDE1", 1f, 8);
            Deco(bed, "Blanket", rounded, RoomBedAt.x, RoomBedAt.y - 0.4f, 2.05f, 1.4f, "#5B7FC4", 1f, 9);
            Deco(bed, "Pillow", roundedSmall, RoomBedAt.x, RoomBedAt.y + 0.8f, 1.1f, 0.42f, "#FFFFFF", 1f, 9);

            BuildThing(parts.Stations, "Bed", "Acostarte un rato", RoomBedAt, new Vector2(2.0f, 2.3f),
                "Te tirás un rato. Dormir bien también es plata: cansado se compra por impulso.",
                "Mañana hay que trabajar. Ponés la alarma y pensás en lo que te queda: {cash} en efectivo.");

            var chair = IL(parts.Props, "PlasticChair");
            Solid(chair, "Seat", roundedSmall, -3.4f, -1.9f, 0.75f, 0.7f, "#E9E6DC");
            Deco(chair, "Back", roundedSmall, -3.4f, -1.55f, 0.75f, 0.14f, "#C9C5B8", 1f, 8);
            Solid(chair, "TrashBin", roundedSmall, -4.2f, -2.4f, 0.42f, 0.50f, "#7F8C8D");
            Deco(chair, "TrashRim", roundedSmall, -4.2f, -2.15f, 0.46f, 0.10f, "#616A6B", 1f, 9);

            // The papers of the month, in a cardboard box: the account statement.
            var box = IL(parts.Props, "PapersBox");
            Solid(box, "Box", roundedSmall, 3.9f, -2.6f, 1.0f, 0.7f, "#B58D5C");
            Deco(box, "Flap", square, 3.9f, -2.42f, 1.0f, 0.05f, "#8E6B42", 1f, 8);
            Deco(box, "Bill", roundedSmall, 3.75f, -2.6f, 0.4f, 0.3f, "#FFFFFF", 1f, 8);
            Deco(box, "Bill", roundedSmall, 4.05f, -2.7f, 0.4f, 0.3f, "#FFF1A8", 1f, 8);
            BuildStation(parts.Stations, "Station_Home", StationType.Home, "Casa", new Vector2(3.0f, -2.5f));

            // The pet (zaguatico) on its warm cushion
            var pet = IL(parts.Props, "Pet");
            Deco(pet, "Cushion", roundedSmall, 2.2f, 1.8f, 1.05f, 0.62f, "#8C4A28", 1f, 7);
            Deco(pet, "CushionFleece", roundedSmall, 2.2f, 1.8f, 0.90f, 0.48f, "#D4B086", 1f, 8);
            Deco(pet, "Body", roundedSmall, 2.12f, 1.82f, 0.58f, 0.36f, "#C68B59", 1f, 9);
            Deco(pet, "Spot", circle, 2.06f, 1.88f, 0.22f, 0.16f, "#A66D3B", 1f, 10);
            var tail = Deco(pet, "Tail", roundedSmall, 1.78f, 1.88f, 0.24f, 0.10f, "#C68B59", 1f, 9);
            Tilt(tail, 30f);
            Deco(pet, "PawBack", roundedSmall, 2.42f, 1.70f, 0.24f, 0.09f, "#A66D3B", 1f, 9);
            Deco(pet, "PawFront", roundedSmall, 2.46f, 1.75f, 0.26f, 0.10f, "#C68B59", 1f, 10);
            Deco(pet, "PawToe", circle, 2.56f, 1.75f, 0.08f, 0.08f, "#E2B98A", 1f, 11);
            Deco(pet, "Head", circle, 2.36f, 1.90f, 0.32f, 0.32f, "#C68B59", 1f, 10);
            Deco(pet, "Collar", square, 2.28f, 1.86f, 0.06f, 0.18f, "#E8505B", 1f, 11);
            Deco(pet, "Tag", circle, 2.30f, 1.78f, 0.08f, 0.08f, "#FFD23F", 1f, 12);
            Deco(pet, "Muzzle", roundedSmall, 2.48f, 1.88f, 0.20f, 0.14f, "#E2B98A", 1f, 11);
            Deco(pet, "Nose", circle, 2.56f, 1.90f, 0.06f, 0.05f, "#1F1F1F", 1f, 12);
            Deco(pet, "Eye", circle, 2.40f, 1.94f, 0.05f, 0.05f, "#1F1F1F", 1f, 12);
            var ear = Deco(pet, "Ear", roundedSmall, 2.28f, 1.97f, 0.14f, 0.22f, "#7D4E27", 1f, 13);
            Tilt(ear, -18f);

            BuildThing(parts.Stations, "Pet", "Acariciar al zaguatico", new Vector2(2.2f, 1.8f), new Vector2(1.2f, 1.0f),
                "Acariciar al zaguatico: mueve la cola y te alegra el día. Cuidar una mascota vale la pena.");

            BuildRoomFurniture(parts);
        }

        /// <summary>A group of the room that shows up or goes away with the purchase of <paramref name="kind"/>.</summary>
        private static Transform Piece(Transform parent, string name, string kind, bool whileMissing)
        {
            var group = IL(parent, name);
            group.gameObject.AddComponent<RoomPiece>().Configure(kind, whileMissing);
            return group;
        }

        private static void BuildRoomWear(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            float wall = half.y - 0.5f;
            var wear = IL(parts.Labels, "StainedWalls");
            Deco(wear, "Damp", circle, -3.6f, wall, 1.8f, 0.8f, "#8F8468", 0.55f);
            Deco(wear, "Damp", circle, 2.8f, wall - 0.05f, 1.2f, 0.7f, "#8F8468", 0.45f);
            Deco(wear, "Damp", circle, 4.2f, wall + 0.1f, 0.7f, 0.5f, "#7E7458", 0.5f);
            Deco(wear, "Crack", square, -1.1f, wall, 0.05f, 0.7f, "#5C5442", 0.8f, 8);
            Deco(wear, "Crack", square, -0.95f, wall - 0.3f, 0.4f, 0.04f, "#5C5442", 0.8f, 8);

            // Wall calendar and clock
            Deco(wear, "Calendar", roundedSmall, -1.8f, wall + 0.1f, 0.6f, 0.75f, "#FFFFFF", 1f, 8);
            Deco(wear, "CalTop", square, -1.8f, wall + 0.38f, 0.54f, 0.14f, "#C0392B", 1f, 9);
            Deco(wear, "Clock_Out", circle, -4.2f, wall - 0.1f, 0.58f, 0.58f, "#7F8C8D", 1f, 8);
            Deco(wear, "Clock_Face", circle, -4.2f, wall - 0.1f, 0.46f, 0.46f, "#FFFFFF", 1f, 9);
            Deco(wear, "Clock_Hands", square, -4.2f, wall - 0.1f, 0.04f, 0.22f, "#1D2230", 1f, 10);

            var floor = IL(parts.Floor, "WornFloor");
            Decal(floor, "Scuff", circle, 1.3f, 0.4f, 1.6f, 1.0f, "#8F7E5E", 0.3f);
            Decal(floor, "Scuff", circle, -1.2f, -2.2f, 1.2f, 0.7f, "#8F7E5E", 0.3f);
            Decal(floor, "Doormat", roundedSmall, 0f, -2.6f, 1.4f, 0.65f, "#5D4037", 0.75f);

            // A bare bulb hanging from the ceiling, a little unsteady.
            var bulb = new Vector2(-0.4f, wall);
            var bulbRig = new GameObject("BulbRig").transform;
            bulbRig.SetParent(wear, false);
            bulbRig.localPosition = new Vector2(bulb.x, bulb.y + 0.42f);
            bulbRig.gameObject.AddComponent<FoliagePhysics>().Configure(2.8f, 1.35f, 4.2f, 0f, false);
            Deco(bulbRig, "Wire", square, 0f, -0.22f, 0.03f, 0.45f, "#2A3040", 1f, 8);
            Deco(bulbRig, "Bulb", circle, 0f, -0.52f, 0.2f, 0.24f, "#FFF3C4", 1f, 9);
            Flickering(Glow(parts.Lights, "Light_Bulb", new Vector2(-0.4f, 0.4f), 6.5f, "#FFE9A8", 0.1f, OrderLight), 0.12f, 2.2f, 4f);
        }

        private static void BuildRoomFurniture(InteriorParts parts)
        {
            var fridge = Piece(parts.Props, "Furniture_Fridge", "Fridge", false);
            Solid(fridge, "Fridge", rounded, RoomFridgeAt.x, RoomFridgeAt.y, 1.0f, 1.4f, "#E3E9F2");
            Deco(fridge, "Door_Line", square, RoomFridgeAt.x, RoomFridgeAt.y + 0.3f, 1.0f, 0.04f, "#9AA3B8", 1f, 8);
            Deco(fridge, "Handle", roundedSmall, RoomFridgeAt.x - 0.38f, RoomFridgeAt.y - 0.05f, 0.06f, 0.4f, "#9AA3B8", 1f, 8);
            BuildThing(fridge, "Fridge", "Abrir la refri", RoomFridgeAt, new Vector2(1.0f, 1.4f),
                "Ya no hay que comprar todo para el día. Lo que sobra aguanta y sale más barato.",
                "Una refri encendida también se paga: se nota en el recibo de la luz.");

            var tv = Piece(parts.Props, "Furniture_Tv", "Tv", false);
            Solid(tv, "Stand", rounded, RoomTvAt.x, RoomTvAt.y, 1.7f, 0.6f, "#5A4636");
            Deco(tv, "Tv", roundedSmall, RoomTvAt.x, RoomTvAt.y + 0.25f, 1.4f, 0.8f, "#1D2238", 1f, 8);
            Deco(tv, "Picture", roundedSmall, RoomTvAt.x, RoomTvAt.y + 0.27f, 1.2f, 0.62f, "#4DA3FF", 1f, 9);
            BuildThing(tv, "Tv", "Ver tele", RoomTvAt + new Vector2(0f, -0.6f), new Vector2(1.4f, 0.4f),
                "Anuncio: \"¡Llévelo hoy con solo ₡9 900 al mes!\". No dicen cuántos meses ni cuánto suma.",
                "Noticias: subió otra vez la canasta básica. Se siente en la pulpería.");

            var table = Piece(parts.Props, "Furniture_Table", "Table", false);
            Solid(table, "Table", rounded, RoomTableAt.x, RoomTableAt.y, 1.8f, 1.0f, "#8B5A3C");
            Solid(table, "Chair", roundedSmall, RoomTableAt.x - 0.45f, RoomTableAt.y - 0.95f, 0.55f, 0.5f, "#B07A45");
            Solid(table, "Chair", roundedSmall, RoomTableAt.x + 0.45f, RoomTableAt.y - 0.95f, 0.55f, 0.5f, "#B07A45");
            Deco(table, "Plate", circle, RoomTableAt.x - 0.4f, RoomTableAt.y, 0.36f, 0.36f, "#FFFFFF", 1f, 8);
            BuildThing(table, "DiningTable", "Sentarte a la mesa", RoomTableAt, new Vector2(1.8f, 1.0f),
                "Por fin una mesa. Anotás el presupuesto del mes: salario de {salary}, primero lo fijo, luego el ahorro.");
        }
    }
}

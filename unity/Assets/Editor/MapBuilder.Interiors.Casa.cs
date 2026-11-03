using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// A simple, working family house. The back half holds the bedroom, the bathroom and the kitchen; the
    /// front half is the living and dining room the front door opens into. The desk with the bills by the
    /// living room is where the player checks the account statement.
    /// </summary>
    public static partial class MapBuilder
    {
        private const float HouseSplitY = 0.8f;
        private const float BedroomRight = -2.0f;
        private const float BathroomRight = 1.6f;
        private const string HouseWallFace = "#E9DDC9";
        private const string OpeningMother = "Mama";
        private static readonly Vector2 MotherAt = new Vector2(2.4f, -0.8f);

        private static void BuildHouseLayout(InteriorParts parts, Room room)
        {
            var half = room.Size * 0.5f;
            float left = -half.x + 0.15f;
            float right = half.x - 0.15f;
            float top = half.y - 0.8f;
            float bottom = -half.y + 0.15f;

            ZoneFloor(parts.Floor, "Floor_Bedroom", Rect.MinMaxRect(left, HouseSplitY, BedroomRight, top), "#C9A27A", 0.5f, true);
            ZoneFloor(parts.Floor, "Floor_Bathroom", Rect.MinMaxRect(BedroomRight, HouseSplitY, BathroomRight, top), "#D7E7EE", 0.6f, false);
            ZoneFloor(parts.Floor, "Floor_Kitchen", Rect.MinMaxRect(BathroomRight, HouseSplitY, right, top), "#E9DCC4", 0.8f, false);
            ZoneFloor(parts.Floor, "Floor_Living", Rect.MinMaxRect(left, bottom, right, HouseSplitY), "#D8B48A", 0.5f, true);

            // Walls between the rooms. The kitchen opens to the dining room over a breakfast bar.
            var walls = IL(parts.Walls, "InnerWalls");
            WallWithDoors(walls, HouseSplitY, -half.x, BathroomRight, HouseWallFace, -2.9f, -0.2f);
            InnerWall(walls, new Vector2(BedroomRight, HouseSplitY), new Vector2(BedroomRight, half.y - 0.8f), HouseWallFace);
            InnerWall(walls, new Vector2(BathroomRight, HouseSplitY), new Vector2(BathroomRight, half.y - 0.8f), HouseWallFace);

            BuildBedroom(parts, room);
            BuildBathroom(parts);
            BuildKitchen(parts, room);
            BuildLivingRoom(parts, room);

            var signs = IL(parts.Labels, "RoomNames");
            WallPlate(signs, "CUARTO", new Vector2(-6.9f, 5.6f), 1.2f, "#F8F5E8", GamePalette.Outline, 0.026f);
            WallPlate(signs, "BAÑO", new Vector2(-0.2f, 5.6f), 0.9f, "#F8F5E8", GamePalette.Outline, 0.026f);
            WallPlate(signs, "COCINA", new Vector2(2.6f, 5.6f), 1.2f, "#F8F5E8", GamePalette.Outline, 0.026f);
        }

        private static void BuildBedroom(InteriorParts parts, Room room)
        {
            var bed = IL(parts.Props, "Bed");
            Solid(bed, "Bed", rounded, -5.1f, 3.85f, 2.3f, 2.4f, "#F2EDE1");
            Deco(bed, "Blanket", rounded, -5.1f, 3.45f, 2.3f, 1.6f, "#5B7FC4");
            Deco(bed, "Blanket_Fold", square, -5.1f, 4.2f, 2.3f, 0.12f, "#87A6DE", 1f, 8);
            Deco(bed, "Pillow", roundedSmall, -5.65f, 4.7f, 0.9f, 0.45f, "#FFFFFF", 1f, 8);
            Deco(bed, "Pillow", roundedSmall, -4.55f, 4.7f, 0.9f, 0.45f, "#FFFFFF", 1f, 8);

            var nightstands = IL(parts.Props, "Nightstands");
            foreach (float x in new[] { -6.75f, -3.45f })
            {
                Solid(nightstands, "Nightstand", rounded, x, 4.7f, 0.6f, 0.6f, "#8B5A3C");
                Deco(nightstands, "Lamp", circle, x, 4.75f, 0.3f, 0.3f, "#FFD23F", 1f, 8);
                Flickering(Glow(parts.Lights, "Light_Nightstand", new Vector2(x, 4.7f), 2.2f, "#FFD23F", 0.18f, OrderLight), 0.06f, 0.8f, x);
            }
            Deco(nightstands, "BookStack1", roundedSmall, -6.75f, 4.88f, 0.32f, 0.12f, "#C0392B", 1f, 9);
            Deco(nightstands, "BookStack2", roundedSmall, -6.75f, 4.96f, 0.28f, 0.10f, "#2980B9", 1f, 10);
            Deco(nightstands, "PhoneCharger", square, -3.45f, 4.95f, 0.02f, 0.45f, "#FFFFFF", 0.85f, 9);
            Deco(nightstands, "PhoneBody", roundedSmall, -3.45f, 4.75f, 0.16f, 0.28f, "#1E272C", 1f, 10);

            var wardrobe = IL(parts.Props, "Wardrobe");
            Solid(wardrobe, "Wardrobe", rounded, -7.6f, 2.3f, 0.9f, 2.2f, "#7A4F2A");
            BuildWardrobeInside(wardrobe);

            Rug(IL(parts.Floor, "BedroomRug"), -5.1f, 1.85f, 2.2f, 0.9f, "#B98E6A", "#8E6A4C");
            Window(IL(parts.Labels, "BedroomWindow"), -5.1f, 5.6f, 1.4f);
        }

        /// <summary>
        /// The inside of the wardrobe, the swimsuit (hidden until it is bought) and the two doors. The doors hang from a
        /// hinge on the wall side, named "Wardrobe_Doors", that the wardrobe scene squeezes to swing them open.
        /// </summary>
        private static void BuildWardrobeInside(Transform wardrobe)
        {
            Deco(wardrobe, "Inside", rounded, -7.6f, 2.3f, 0.8f, 2.1f, "#3A2718", 1f, 7);
            Deco(wardrobe, "Rail", square, -7.6f, 3.1f, 0.06f, 1.2f, "#9AA3B8", 1f, 8);
            var swimsuit = Group(wardrobe, "Wardrobe_Swimsuit");
            Deco(swimsuit, "Swimsuit", roundedSmall, -7.55f, 2.2f, 0.36f, 0.5f, "#4DA3FF", 1f, 9).enabled = false;
            Deco(swimsuit, "Swimsuit_Band", square, -7.55f, 2.35f, 0.36f, 0.06f, "#FFD23F", 1f, 10).enabled = false;
            Deco(swimsuit, "Hanger", square, -7.55f, 2.5f, 0.3f, 0.04f, "#C9CFDB", 1f, 10).enabled = false;

            var hinge = Group(wardrobe, "Wardrobe_Doors");
            hinge.localPosition = new Vector2(-8.05f, 2.3f);
            foreach (float y in new[] { 0.55f, -0.55f })
            {
                Deco(hinge, "Door_Outline", rounded, 0.45f, y, 0.94f, 1.12f, "#2A3040", 1f, 11);
                Deco(hinge, "Door", rounded, 0.45f, y, 0.88f, 1.06f, "#7A4F2A", 1f, 12);
                Deco(hinge, "Handle", circle, 0.75f, y > 0f ? y - 0.3f : y + 0.3f, 0.1f, 0.1f, "#FFD23F", 1f, 13);
            }
        }

        private static void BuildBathroom(InteriorParts parts)
        {
            var shower = IL(parts.Props, "Shower");
            Solid(shower, "Shower", rounded, 0.8f, 4.35f, 1.35f, 1.45f, "#9FD3E6");
            Deco(shower, "Drain", circle, 0.8f, 4.3f, 0.2f, 0.2f, "#6B7186", 1f, 8);
            Deco(shower, "Glass", square, 0.8f, 3.66f, 1.35f, 0.06f, "#E8F6FB", 0.9f, 8);
            Deco(shower, "Head", circle, 1.2f, 4.85f, 0.2f, 0.2f, "#C9CFDB", 1f, 8);

            var toilet = IL(parts.Props, "Toilet");
            Solid(toilet, "Tank", roundedSmall, -1.3f, 4.85f, 0.7f, 0.35f, "#F8F8F8");
            Solid(toilet, "Bowl", circle, -1.3f, 4.35f, 0.55f, 0.65f, "#F8F8F8");
            Deco(toilet, "Seat", ring, -1.3f, 4.35f, 0.5f, 0.6f, "#C9CFDB", 1f, 8);

            var sink = IL(parts.Props, "Sink");
            Solid(sink, "Vanity", rounded, -1.5f, 2.4f, 0.6f, 0.9f, "#8B5A3C");
            Deco(sink, "Basin", circle, -1.45f, 2.4f, 0.4f, 0.5f, "#F8F8F8", 1f, 8);
            Deco(sink, "Tap", circle, -1.7f, 2.4f, 0.1f, 0.1f, "#C9CFDB", 1f, 9);

            Rug(IL(parts.Floor, "BathMat"), 0.8f, 3.2f, 1.0f, 0.5f, "#7FB6E8", "#5B92C6");
            Deco(IL(parts.Props, "Towel"), "Towel", roundedSmall, 1.4f, 2.4f, 0.12f, 0.7f, "#E8505B");
        }

        private static void BuildKitchen(InteriorParts parts, Room room)
        {
            // The worktop along the back wall: sink, stove and fridge, the classic triangle.
            var counter = IL(parts.Props, "KitchenCounter");
            Solid(counter, "Worktop", rounded, 4.3f, 4.7f, 4.6f, 0.9f, "#D9D2C5");
            Deco(counter, "Sink", roundedSmall, 2.9f, 4.7f, 0.9f, 0.55f, "#9AA3B8", 1f, 8);
            Deco(counter, "Tap", circle, 2.9f, 5.0f, 0.12f, 0.12f, "#C9CFDB", 1f, 9);
            Deco(counter, "CuttingBoard", roundedSmall, 4.0f, 4.7f, 0.6f, 0.4f, "#C99A62", 1f, 8);

            var stove = IL(parts.Props, "Stove");
            Deco(stove, "Stove", rounded, 5.5f, 4.7f, 1.1f, 0.8f, "#4B5568", 1f, 8);
            foreach (var burner in new[] { new Vector2(5.25f, 4.85f), new Vector2(5.75f, 4.85f), new Vector2(5.25f, 4.55f), new Vector2(5.75f, 4.55f) })
            {
                Deco(stove, "Burner", ring, burner.x, burner.y, 0.28f, 0.28f, "#2A3040", 1f, 9);
            }

            Deco(IL(parts.Props, "Pot"), "Pot", circle, 5.25f, 4.85f, 0.3f, 0.3f, "#9AA3B8", 1f, 10);

            var fridge = IL(parts.Props, "Fridge");
            Solid(fridge, "Fridge", rounded, 7.5f, 4.45f, 1.0f, 1.4f, "#E3E9F2");
            Deco(fridge, "Door_Line", square, 7.5f, 4.75f, 1.0f, 0.04f, "#9AA3B8", 1f, 8);
            Deco(fridge, "Handle", roundedSmall, 7.1f, 4.4f, 0.06f, 0.4f, "#9AA3B8", 1f, 8);

            var cabinets = IL(parts.Labels, "UpperCabinets");
            Deco(cabinets, "Cabinets", rounded, 4.3f, 5.6f, 4.6f, 0.5f, "#A9743F");
            for (float x = 2.5f; x < 6.6f; x += 1.15f)
            {
                Deco(cabinets, "Cabinet_Line", square, x, 5.6f, 0.04f, 0.44f, "#7A4F2A", 1f, 8);
            }

            // The breakfast bar the kitchen shares with the dining room, with three stools.
            var bar = IL(parts.Props, "BreakfastBar");
            Solid(bar, "Bar", rounded, 6.1f, HouseSplitY, 4.1f, 0.6f, "#A9743F");
            Deco(bar, "Bar_Top", roundedSmall, 6.1f, HouseSplitY + 0.05f, 3.9f, 0.3f, "#D9D2C5", 1f, 8);
            Deco(bar, "Fruit", circle, 6.6f, HouseSplitY + 0.05f, 0.3f, 0.3f, "#FF9F3D", 1f, 9);
            foreach (float x in new[] { 4.9f, 6.1f, 7.3f })
            {
                Solid(bar, "Stool", circle, x, HouseSplitY - 0.7f, 0.45f, 0.45f, "#4B5568");
            }

            Deco(IL(parts.Props, "TrashCan"), "Bin", roundedSmall, 2.0f, 3.6f, 0.4f, 0.5f, "#4A6B5A");
            CeilingLight(parts.Lights, new Vector2(4.8f, 3.0f), 4.2f, "#FFF3C4", 0.08f);
        }

        private static void BuildLivingRoom(InteriorParts parts, Room room)
        {
            // Sofa facing the TV on the left wall.
            Rug(IL(parts.Floor, "LivingRug"), -6.0f, -2.6f, 3.6f, 3.0f, "#B65F4F", "#8F4637");

            var tv = IL(parts.Props, "TvStand");
            Solid(tv, "TvStand", rounded, -7.75f, -2.6f, 0.6f, 2.2f, "#5A4636");
            Deco(tv, "Tv", roundedSmall, -7.75f, -2.6f, 0.25f, 1.7f, "#1D2238", 1f, 8);
            Flickering(Glow(parts.Lights, "Light_Tv", new Vector2(-7.2f, -2.6f), 3.0f, "#4DA3FF", 0.16f, OrderLight), 0.3f, 6f, 17f);

            var sofa = IL(parts.Props, "Sofa");
            Solid(sofa, "Sofa", rounded, -4.4f, -2.6f, 1.0f, 2.8f, "#3B6FB6");
            Deco(sofa, "Backrest", roundedSmall, -4.1f, -2.6f, 0.35f, 2.8f, "#2D5A99", 1f, 8);
            foreach (float y in new[] { -3.5f, -2.6f, -1.7f })
            {
                Deco(sofa, "Cushion", roundedSmall, -4.55f, y, 0.55f, 0.8f, "#6FA0E0", 1f, 8);
            }

            var table = IL(parts.Props, "CoffeeTable");
            Solid(table, "CoffeeTable", rounded, -6.0f, -2.6f, 0.9f, 1.3f, "#8B5A3C");
            Deco(table, "Remote", roundedSmall, -6.0f, -2.3f, 0.14f, 0.35f, "#2A3040", 1f, 8);
            Deco(table, "Mug", circle, -5.9f, -2.9f, 0.2f, 0.2f, "#F8F5E8", 1f, 8);

            var lamp = IL(parts.Props, "FloorLamp");
            Solid(lamp, "Lamp", circle, -4.4f, -0.5f, 0.4f, 0.4f, "#FFD23F");
            Flickering(Glow(parts.Lights, "Light_FloorLamp", new Vector2(-4.4f, -0.5f), 2.6f, "#FFD23F", 0.18f, OrderLight), 0.06f, 0.8f, 3f);

            // Dining table next to the kitchen.
            var dining = IL(parts.Props, "DiningTable");
            Solid(dining, "Table", rounded, 5.0f, -2.6f, 2.4f, 1.3f, "#8B5A3C");
            foreach (float x in new[] { 4.4f, 5.6f })
            {
                Deco(dining, "Plate", circle, x, -2.3f, 0.36f, 0.36f, "#FFFFFF", 1f, 8);
                Deco(dining, "Plate", circle, x, -2.9f, 0.36f, 0.36f, "#FFFFFF", 1f, 8);
                Solid(dining, "Chair", roundedSmall, x, -1.5f, 0.55f, 0.5f, "#B07A45");
                Solid(dining, "Chair", roundedSmall, x, -3.7f, 0.55f, 0.5f, "#B07A45");
            }

            Deco(dining, "Vase", circle, 5.0f, -2.6f, 0.26f, 0.26f, "#55E08F", 1f, 9);
            CeilingLight(parts.Lights, new Vector2(5.0f, -2.6f), 4.0f, "#FFE9A8", 0.1f);

            // The house is the mother's: she stays in the living room, out of the way between the sofa and the table. The
            // opening moves her to the bedroom for the present (see GameUi.Opening.cs).
            BuildNpc(parts.People, OpeningMother, MotherAt, new Look
            {
                Skin = "#5FA38C", SkinTone = "#D9A47A", HairColor = "#555A66", Hair = HairStyle.Bun, Pants = "#6B5B50", Face = Expression.Warm
            }, false);
            BuildThing(parts.Stations, OpeningMother, "Hablar con tu mamá", MotherAt + new Vector2(0f, -0.5f), new Vector2(0.8f, 0.6f),
                "\"Antes de comprar algo, esperá un día. Si al otro día lo seguís queriendo, ahí sí.\"",
                "\"Apartá primero para el ahorro y después gastás. Al revés nunca alcanza.\"",
                "\"Si alguien te ofrece plata fácil, desconfiá. Nadie regala nada.\"");

            // The desk with the bills and the laptop: the account statement station.
            var desk = IL(parts.Props, "BillsDesk");
            Solid(desk, "Desk", rounded, -6.4f, -5.25f, 2.0f, 0.8f, "#7A4F2A");
            Deco(desk, "Laptop", roundedSmall, -6.7f, -5.2f, 0.7f, 0.45f, "#2A3040", 1f, 8);
            Deco(desk, "Laptop_Screen", roundedSmall, -6.7f, -5.2f, 0.56f, 0.3f, "#4DA3FF", 1f, 9);
            Deco(desk, "Bill", roundedSmall, -6.0f, -5.15f, 0.4f, 0.3f, "#FFFFFF", 1f, 8);
            Deco(desk, "Bill", roundedSmall, -5.75f, -5.3f, 0.4f, 0.3f, "#FFF1A8", 1f, 8);
            Solid(desk, "DeskChair", circle, -6.4f, -4.45f, 0.55f, 0.55f, "#3B6FB6");
            BuildStation(parts.Stations, "Station_Home", StationType.Home, "Casa", new Vector2(-6.4f, -3.9f));

            // The pet (zaguatico) resting on open floor
            var pet = IL(parts.Props, "Pet");
            Deco(pet, "Cushion", roundedSmall, 7.2f, -4.2f, 1.05f, 0.62f, "#8C4A28", 1f, 7);
            Deco(pet, "CushionFleece", roundedSmall, 7.2f, -4.2f, 0.90f, 0.48f, "#D4B086", 1f, 8);

            // The dog itself is a small rig that breathes, wags, dozes and cheers when petted (see BuildDog).
            BuildDog(pet, new Vector2(7.12f, -4.18f));

            var petTalk = BuildThing(parts.Stations, "Pet", "Acariciar al zaguatico", new Vector2(7.2f, -4.2f), new Vector2(1.3f, 1.1f),
                "Acariciar al zaguatico: mueve la cola y te alegra el día. Cuidar una mascota vale la pena.");

            // The same rooms serve the neighbors and the houses for sale: the dog decides at run time if it lives here.
            pet.gameObject.AddComponent<HouseDog>().Configure(petTalk.gameObject);

            // By the front door: shoes, coats and a plant.
            var entry = IL(parts.Props, "Entry");
            Solid(entry, "ShoeRack", rounded, 1.9f, -5.5f, 1.3f, 0.5f, "#8B5A3C");
            Deco(entry, "Shoe", roundedSmall, 1.6f, -5.5f, 0.3f, 0.16f, "#E8505B", 1f, 8);
            Deco(entry, "Shoe", roundedSmall, 2.1f, -5.5f, 0.3f, 0.16f, "#2A3345", 1f, 8);
            Solid(entry, "CoatStand", circle, -1.8f, -5.5f, 0.45f, 0.45f, "#7A4F2A");
            Deco(entry, "Coat", roundedSmall, -1.8f, -5.2f, 0.45f, 0.5f, "#4DA3FF", 1f, 8);

            var plants = IL(parts.Props, "Plants");
            Plant(plants, 7.6f, -5.4f);
            Plant(plants, -7.7f, -0.2f, 0.8f);

            CeilingLight(parts.Lights, new Vector2(-5.5f, -2.6f), 4.4f, "#FFE9A8", 0.08f);
            CeilingLight(parts.Lights, new Vector2(-5.0f, 3.0f), 3.8f, "#FFE9A8", 0.06f);
        }
    }
}

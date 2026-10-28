using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The bigger rooms. The original furniture of each building was built for a small room and is moved
    /// against the back wall; everything here fills the rest of the space. Each kind of detail is its own
    /// named group (a layer), so the Casa and the Tienda have about 25 and the Banco has more than 50.
    /// </summary>
    public static partial class MapBuilder
    {
        private static int interiorLayers;

        private static Transform IL(Transform parent, string name)
        {
            interiorLayers++;
            return Group(parent, name);
        }

        private static void Solid(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex)
        {
            Prop(g, name, sprite, new Vector2(x, y), new Vector2(w, h), GamePalette.Hex(hex));
        }

        private static SpriteRenderer Deco(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex, float alpha = 1f, int order = 7)
        {
            return Shape(g, name, sprite, new Vector2(x, y), new Vector2(w, h), Hex(hex, alpha), order);
        }

        private static SpriteRenderer Decal(Transform g, string name, Sprite sprite, float x, float y, float w, float h, string hex, float alpha = 1f)
        {
            string previous = currentLayer;
            currentLayer = LayerFloor;
            var renderer = Shape(g, name, sprite, new Vector2(x, y), new Vector2(w, h), Hex(hex, alpha), OrderRug + 2);
            currentLayer = previous;
            return renderer;
        }

        private static void Plant(Transform g, float x, float y, float scale = 1f)
        {
            Prop(g, "Pot", roundedSmall, new Vector2(x, y), new Vector2(0.6f, 0.5f) * scale, GamePalette.Hex("#C26A3A"));
            Deco(g, "Plant", circle, x, y + 0.35f * scale, 0.9f * scale, 0.9f * scale, "#3FA66A");
            Deco(g, "Leaf", circle, x - 0.15f * scale, y + 0.5f * scale, 0.4f * scale, 0.4f * scale, "#6BBF59");
        }

        private static void DustMotes(Transform g, System.Random random, Room room, int count)
        {
            string previous = currentLayer;
            currentLayer = LayerForeground;
            var half = room.Size * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var mote = Shape(g, "Mote", softCircle, new Vector2((float)random.NextDouble() * room.Size.x - half.x, (float)random.NextDouble() * room.Size.y - half.y), Vector2.one * 0.2f, Hex("#FFF6B8", 0.4f), 2);
                mote.gameObject.AddComponent<Drift>().Configure(new Vector2(0.1f + (float)random.NextDouble() * 0.12f, 0.06f), half);
            }

            currentLayer = previous;
        }

        private static void CeilingGlows(Transform lights, Room room, float step, string hex, float alpha)
        {
            var half = room.Size * 0.5f;
            for (float x = -half.x + step * 0.5f; x < half.x; x += step)
            {
                for (float y = -half.y + step * 0.5f; y < half.y; y += step)
                {
                    Flickering(Glow(lights, "Ceiling", new Vector2(x, y), step * 0.9f, hex, alpha, OrderLight), 0.05f, 0.3f, x * 3f + y);
                }
            }
        }

        private static void FloorChecks(Transform g, Room room, float tile, string hex, float alpha)
        {
            var half = room.Size * 0.5f;
            int row = 0;
            for (float y = -half.y + tile * 0.5f; y < half.y; y += tile, row++)
            {
                int col = 0;
                for (float x = -half.x + tile * 0.5f; x < half.x; x += tile, col++)
                {
                    if ((row + col) % 2 == 0)
                    {
                        Decal(g, "Check", square, x, y, tile, tile, hex, alpha);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- casa (16.5 x 12, about 27 layers)

        private static void BuildHomeExtras(Transform extras, Transform lights, Room room, System.Random random)
        {
            var planks = IL(extras, "FloorPlanks");
            var half = room.Size * 0.5f;
            for (float y = -half.y + 0.5f; y < half.y; y += 1.5f)
            {
                Decal(planks, "Plank", square, 0f, y, room.Size.x - 1f, 0.03f, "#7A4F2A", 0.25f);
            }

            Decal(IL(extras, "EntryRug"), "Rug", rounded, 0f, -4.1f, 2.4f, 1.6f, "#B2475A", 0.9f);

            var windows = IL(extras, "Windows");
            var curtains = IL(extras, "Curtains");
            foreach (float x in new[] { -7.3f, 5.5f })
            {
                Deco(windows, "Frame", rounded, x, 5.6f, 1.2f, 0.5f, "#2A3345");
                Deco(windows, "Glass", rounded, x, 5.6f, 1.06f, 0.36f, "#BFE3F5");
                Deco(windows, "Bar", square, x, 5.6f, 0.05f, 0.36f, "#2A3345");
                Deco(curtains, "Curtain", roundedSmall, x - 0.72f, 5.5f, 0.26f, 0.7f, "#B2475A");
                Deco(curtains, "Curtain", roundedSmall, x + 0.72f, 5.5f, 0.26f, 0.7f, "#B2475A");
            }

            var clock = IL(extras, "WallClock");
            Deco(clock, "Frame", circle, 7.0f, 5.6f, 0.55f, 0.55f, "#2A3345");
            Deco(clock, "Face", circle, 7.0f, 5.6f, 0.44f, 0.44f, "#F8F5E8", 1f, 8);
            Deco(clock, "Hand", square, 7.0f, 5.67f, 0.04f, 0.18f, "#2A3345", 1f, 9);

            var paintings = IL(extras, "WallPaintings");
            Deco(paintings, "Frame", roundedSmall, 7.7f, 5.6f, 0.5f, 0.4f, "#7A4F2A");
            Deco(paintings, "Art", roundedSmall, 7.7f, 5.6f, 0.4f, 0.3f, "#55B76A", 1f, 8);
            Deco(paintings, "Frame", roundedSmall, 1.75f, 5.6f, 0.3f, 0.4f, "#7A4F2A");

            var shelves = IL(extras, "WallShelves");
            Deco(shelves, "Shelf", roundedSmall, -8.0f, 4.6f, 0.5f, 0.08f, "#7A4F2A");
            Deco(shelves, "Trinket", circle, -8.0f, 4.72f, 0.14f, 0.14f, "#FFD23F");

            var dresser = IL(extras, "Dresser");
            Solid(dresser, "Dresser", rounded, -7.4f, 3.4f, 1.2f, 2.2f, "#8B5A3C");
            Deco(dresser, "Drawer", square, -7.4f, 3.4f, 1.0f, 0.04f, "#2A3040");
            Deco(dresser, "Drawer", square, -7.4f, 2.8f, 1.0f, 0.04f, "#2A3040");

            var books = IL(extras, "SideBookshelf");
            Solid(books, "Shelf", rounded, -7.4f, 0.4f, 1.0f, 2.4f, "#8B5A3C");
            for (int i = 0; i < 6; i++)
            {
                Deco(books, "Book", roundedSmall, -7.4f, -0.5f + i * 0.34f, 0.8f, 0.2f, i % 2 == 0 ? "#E8505B" : "#4DA3FF");
            }

            var lamps = IL(extras, "FloorLamps");
            foreach (var at in new[] { new Vector2(-7.5f, 5.0f), new Vector2(7.5f, 5.0f), new Vector2(6.4f, -2.0f) })
            {
                Solid(lamps, "Lamp", circle, at.x, at.y, 0.4f, 0.4f, "#FFD23F");
                Flickering(Glow(lights, "Light_Lamp", at, 2.6f, "#FFD23F", 0.2f, OrderLight), 0.06f, 0.8f, at.x);
            }

            var piano = IL(extras, "Piano");
            Solid(piano, "Piano", rounded, 7.3f, 3.0f, 1.4f, 2.4f, "#2A3345");
            for (int i = 0; i < 7; i++)
            {
                Deco(piano, "Key", square, 7.05f, 2.0f + i * 0.3f, 0.5f, 0.2f, "#F8F5E8");
            }

            Deco(IL(extras, "PianoStool"), "Stool", circle, 6.55f, 3.0f, 0.5f, 0.5f, "#8B5A3C");

            var armchairs = IL(extras, "Armchairs");
            Solid(armchairs, "Armchair", rounded, 7.4f, 0.2f, 1.0f, 1.0f, "#3B6FB6");
            Deco(armchairs, "Cushion", roundedSmall, 7.4f, 0.2f, 0.7f, 0.7f, "#6FA0E0");

            var sideTables = IL(extras, "SideTables");
            Solid(sideTables, "Table", circle, 7.4f, -1.2f, 0.7f, 0.7f, "#8B5A3C");
            Deco(sideTables, "Cup", circle, 7.4f, -1.2f, 0.2f, 0.2f, "#F8F5E8");

            var bed2 = IL(extras, "SecondBed");
            Solid(bed2, "Bed", rounded, -5.2f, -4.2f, 2.2f, 1.8f, "#4DA3FF");
            Deco(bed2, "Pillow", roundedSmall, -5.9f, -4.2f, 0.6f, 1.0f, "#F8F5E8");

            var night = IL(extras, "SecondNightstand");
            Solid(night, "Nightstand", rounded, -3.5f, -5.0f, 0.7f, 0.7f, "#8B5A3C");
            Deco(night, "Lamp", circle, -3.5f, -5.0f, 0.3f, 0.3f, "#FFD23F");

            var shoes = IL(extras, "ShoeRack");
            Solid(shoes, "Rack", rounded, -2.2f, -5.4f, 1.4f, 0.55f, "#8B5A3C");
            Deco(shoes, "Shoe", roundedSmall, -2.6f, -5.4f, 0.3f, 0.16f, "#E8505B");
            Deco(shoes, "Shoe", roundedSmall, -2.2f, -5.4f, 0.3f, 0.16f, "#4DA3FF");
            Deco(shoes, "Shoe", roundedSmall, -1.8f, -5.4f, 0.3f, 0.16f, "#2A3345");

            var coats = IL(extras, "CoatRack");
            Solid(coats, "Rack", circle, 2.0f, -5.3f, 0.5f, 0.5f, "#7A4F2A");
            Deco(coats, "Coat", roundedSmall, 2.0f, -5.0f, 0.5f, 0.6f, "#E8505B");

            var console = IL(extras, "EntryConsole");
            Solid(console, "Console", rounded, 3.7f, -5.3f, 1.6f, 0.6f, "#8B5A3C");
            Deco(console, "Mirror", circle, 3.7f, -4.85f, 0.7f, 0.7f, "#CFE8FF");
            Deco(console, "Keys", circle, 3.3f, -5.3f, 0.14f, 0.14f, "#FFD23F");

            var umbrellas = IL(extras, "UmbrellaStand");
            Solid(umbrellas, "Stand", circle, 5.2f, -5.4f, 0.5f, 0.5f, "#6B7186");
            Deco(umbrellas, "Umbrella", roundedSmall, 5.1f, -5.1f, 0.1f, 0.6f, "#4DA3FF");

            var cat = IL(extras, "CatBed");
            Deco(cat, "Bed", circle, 6.9f, -5.0f, 0.9f, 0.6f, "#B2475A", 1f, 6);
            Deco(cat, "Cat", circle, 6.9f, -4.95f, 0.5f, 0.34f, "#F2994A");
            Deco(cat, "Head", circle, 7.1f, -4.88f, 0.22f, 0.2f, "#F2994A", 1f, 8);

            var laundry = IL(extras, "LaundryBasket");
            Solid(laundry, "Basket", roundedSmall, -7.2f, -3.0f, 0.7f, 0.6f, "#B07A45");
            Deco(laundry, "Clothes", circle, -7.2f, -2.85f, 0.5f, 0.3f, "#E8505B");

            var toys = IL(extras, "Toys");
            Deco(toys, "Block", square, -7.3f, -4.9f, 0.3f, 0.3f, "#FFD23F", 1f, 6);
            Deco(toys, "Block", square, -6.9f, -4.7f, 0.3f, 0.3f, "#E8505B", 1f, 6);
            Deco(toys, "Ball", circle, -7.0f, -5.3f, 0.3f, 0.3f, "#4DA3FF", 1f, 6);

            var plants = IL(extras, "Plants");
            Plant(plants, 7.4f, -3.2f);
            Plant(plants, -7.4f, -1.6f);
            Plant(plants, 4.6f, -3.0f, 0.8f);

            var ceiling = IL(lights, "CeilingLights");
            CeilingGlows(ceiling, room, 4.2f, "#FFF3C4", 0.07f);

            DustMotes(IL(extras, "DustMotes"), random, room, 26);
        }

        // ---------------------------------------------------------------- tienda (18 x 12.6, about 27 layers)

        private static void BuildStoreExtras(Transform extras, Transform lights, Room room, System.Random random)
        {
            FloorChecks(IL(extras, "CheckerFloor"), room, 1f, "#FFFFFF", 0.1f);

            var racks = IL(extras, "ClothingRacks");
            string[] tones = { "#E8505B", "#4DA3FF", "#FFD23F", "#55E08F", "#B28DFF" };
            foreach (float y in new[] { 4.6f, 2.4f, 0.2f })
            {
                Solid(racks, "Rack", roundedSmall, -7.7f, y, 1.3f, 0.2f, "#6B7186");
                for (int i = 0; i < 6; i++)
                {
                    Deco(racks, "Garment", roundedSmall, -8.2f + i * 0.2f, y - 0.35f, 0.16f, 0.6f, tones[(i + (int)y) % tones.Length]);
                }
            }

            var mannequins = IL(extras, "Mannequins");
            foreach (float y in new[] { 3.4f, 1.6f })
            {
                Solid(mannequins, "Base", circle, 7.9f, y - 0.4f, 0.5f, 0.3f, "#2A3345");
                Deco(mannequins, "Torso", roundedSmall, 7.9f, y, 0.4f, 0.7f, "#E8505B");
                Deco(mannequins, "Head", circle, 7.9f, y + 0.5f, 0.24f, 0.24f, "#F2D6B0");
            }

            var carts = IL(extras, "ShoppingCarts");
            foreach (float x in new[] { -6.8f, -5.5f })
            {
                Solid(carts, "Cart", roundedSmall, x, -3.2f, 1.0f, 0.7f, "#9AA3B8");
                Deco(carts, "Basket", roundedSmall, x, -3.1f, 0.8f, 0.4f, "#CFE8FF", 0.7f);
            }

            var baskets = IL(extras, "BasketStack");
            Solid(baskets, "Stack", roundedSmall, -3.8f, -3.3f, 0.8f, 0.6f, "#E8505B");
            Deco(baskets, "Stack", roundedSmall, -3.8f, -3.2f, 0.7f, 0.4f, "#C93D48");

            var gates = IL(extras, "SecurityGates");
            foreach (float x in new[] { -1.6f, 1.6f })
            {
                Solid(gates, "Post", roundedSmall, x, -4.4f, 0.3f, 1.2f, "#6B7186");
                Flickering(Deco(gates, "Light", circle, x, -3.9f, 0.14f, 0.14f, "#55E08F"), 0.5f, 1.4f, x);
            }

            var lane = IL(extras, "SecondCheckout");
            Solid(lane, "Counter", rounded, 5.0f, -3.6f, 2.6f, 0.8f, "#D6B656");
            Deco(lane, "Belt", roundedSmall, 4.6f, -3.6f, 1.4f, 0.4f, "#2A3040");
            Deco(lane, "Register", roundedSmall, 6.0f, -3.55f, 0.6f, 0.4f, "#6B7186");

            var candy = IL(extras, "CandyDisplay");
            foreach (var color in new[] { "#FF7A8A", "#FFD23F", "#55E08F", "#4DA3FF" })
            {
                Deco(candy, "Candy", circle, 2.7f + candy.childCount * 0.22f, 1.95f, 0.16f, 0.16f, color);
            }

            var vending = IL(extras, "VendingMachine");
            Solid(vending, "Machine", rounded, 8.0f, -0.6f, 1.0f, 1.5f, "#E8505B");
            Deco(vending, "Window", roundedSmall, 8.0f, -0.4f, 0.7f, 0.8f, "#CFE8FF", 0.8f);
            Flickering(Deco(vending, "Glow", circle, 8.0f, -0.4f, 0.1f, 0.1f, "#FFD23F"), 0.4f, 2f, 3f);

            var atm = IL(extras, "MiniAtm");
            Solid(atm, "Atm", rounded, 8.0f, -3.0f, 0.9f, 1.2f, "#4B5568");
            Deco(atm, "Screen", roundedSmall, 8.0f, -2.8f, 0.6f, 0.4f, "#55E08F");

            var fitting = IL(extras, "FittingRooms");
            foreach (float y in new[] { 5.0f })
            {
                Solid(fitting, "Booth", rounded, 7.9f, y, 1.2f, 1.6f, "#B28DFF");
                Deco(fitting, "Curtain", roundedSmall, 7.9f, y - 0.1f, 0.9f, 1.1f, "#8E6BB8");
            }

            var posters = IL(extras, "WallPosters");
            string[] posterTones = { "#FFD23F", "#E8505B", "#4DA3FF", "#55E08F" };
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? -8.4f + i * 0.9f : 6.9f + (i - 2) * 0.9f;
                Deco(posters, "Poster", roundedSmall, x, 5.9f, 0.6f, 0.45f, posterTones[i]);
            }

            var signs = IL(extras, "HangingSigns");
            Label(signs, "OFERTA", new Vector2(-7.7f, 5.95f), GamePalette.Hex("#E8505B"), 0.05f, TextAnchor.MiddleCenter);
            Label(signs, "2 X 1", new Vector2(7.5f, 5.95f), GamePalette.Hex("#E8505B"), 0.05f, TextAnchor.MiddleCenter);

            var exit = IL(extras, "ExitSign");
            Deco(exit, "Sign", roundedSmall, 0f, -6.0f, 1.0f, 0.3f, "#55E08F");
            Label(exit, "SALIDA", new Vector2(0f, -6.0f), GamePalette.Outline, 0.03f, TextAnchor.MiddleCenter);

            var price = IL(extras, "PriceScreen");
            Solid(price, "Screen", rounded, -7.7f, -1.4f, 1.3f, 1.0f, "#1D2238");
            Label(price, "CUOTA 25 000", new Vector2(-7.7f, -1.25f), GamePalette.Hex("#55E08F"), 0.03f, TextAnchor.MiddleCenter);
            Label(price, "X 36 MESES", new Vector2(-7.7f, -1.55f), GamePalette.Hex("#FFD23F"), 0.03f, TextAnchor.MiddleCenter);

            var sideShelves = IL(extras, "SideShelves");
            Solid(sideShelves, "Shelf", rounded, -7.8f, -3.4f, 1.2f, 1.2f, "#C9753A");
            Deco(sideShelves, "Box", roundedSmall, -7.8f, -3.3f, 0.8f, 0.4f, "#E8D2A0");

            var boxes = IL(extras, "BoxStacks");
            Solid(boxes, "Boxes", rounded, 6.9f, -5.0f, 1.2f, 1.0f, "#A9743F");
            Deco(boxes, "Top", roundedSmall, 6.9f, -4.7f, 0.9f, 0.5f, "#C9945A");

            var scale = IL(extras, "Scale");
            Solid(scale, "Scale", roundedSmall, 3.2f, -5.2f, 0.8f, 0.5f, "#9AA3B8");
            Deco(scale, "Dial", circle, 3.2f, -5.1f, 0.24f, 0.24f, "#F8F5E8");

            var bags = IL(extras, "BagsDisplay");
            foreach (float x in new[] { -3.0f, -2.4f, -1.8f })
            {
                Deco(bags, "Bag", roundedSmall, x, -5.3f, 0.4f, 0.5f, tones[(int)(x * 10f + 100f) % tones.Length]);
            }

            var bin = IL(extras, "TrashBin");
            Solid(bin, "Bin", roundedSmall, 5.4f, -5.4f, 0.5f, 0.6f, "#4A6B5A");

            var extinguisher = IL(extras, "FireExtinguisher");
            Solid(extinguisher, "Tank", roundedSmall, -4.6f, -5.5f, 0.3f, 0.5f, "#E8505B");

            var plants = IL(extras, "Plants");
            Plant(plants, -8.3f, -5.2f);
            Plant(plants, 8.3f, -5.4f);
            Plant(plants, 8.3f, 6.0f, 0.8f);

            var tape = IL(extras, "QueueTape");
            Decal(tape, "Tape", square, 0f, -1.6f, 3.6f, 0.08f, "#FFD23F", 0.9f);
            Decal(tape, "Tape", square, 1.8f, -2.3f, 0.08f, 1.4f, "#FFD23F", 0.9f);

            var stickers = IL(extras, "FloorStickers");
            Decal(stickers, "Sticker", circle, 0f, -2.6f, 1.5f, 1.5f, "#E8505B", 0.85f);
            Label(stickers, "36 CUOTAS", new Vector2(0f, -2.6f), GamePalette.White, 0.03f, TextAnchor.MiddleCenter);

            var cameras = IL(extras, "SecurityCameras");
            foreach (var at in new[] { new Vector2(-8.6f, 5.9f), new Vector2(8.6f, 5.9f), new Vector2(-8.6f, -5.9f), new Vector2(8.6f, -5.9f) })
            {
                Deco(cameras, "Dome", circle, at.x, at.y, 0.34f, 0.34f, "#4A5166");
                Flickering(Deco(cameras, "Light", circle, at.x, at.y, 0.08f, 0.08f, "#E8505B", 1f, 8), 0.8f, 1.2f, at.x);
            }

            CeilingGlows(IL(lights, "CeilingLights"), room, 4.5f, "#FFFFFF", 0.07f);
            DustMotes(IL(extras, "DustMotes"), random, room, 26);
        }

        // ---------------------------------------------------------------- banco (24 x 16.8, 50+ layers)

        private static void BuildBankExtras(Transform extras, Transform lights, Transform people, Room room, System.Random random)
        {
            // Floor.
            FloorChecks(IL(extras, "MarbleFloor"), room, 2f, "#FFFFFF", 0.12f);
            Decal(IL(extras, "RedCarpet"), "Carpet", rounded, 0f, -3.6f, 1.9f, 8.2f, "#8B3A3A", 0.85f);
            Decal(IL(extras, "CarpetBorder"), "Border", ring, 0f, -3.6f, 2.2f, 8.5f, "#FFD23F", 0.5f);
            var mats = IL(extras, "EntranceMats");
            Decal(mats, "Mat", rounded, -2.4f, -7.2f, 1.8f, 0.9f, "#2A3345", 0.8f);
            Decal(mats, "Mat", rounded, 2.4f, -7.2f, 1.8f, 0.9f, "#2A3345", 0.8f);
            var arrows = IL(extras, "QueueArrows");
            for (int i = 0; i < 4; i++)
            {
                Decal(arrows, "Arrow", square, 0f, -6.0f + i * 1.2f, 0.12f, 0.6f, "#FFD23F", 0.6f);
            }

            // Entrance: arch, guard, flags, mat exit sign.
            var arch = IL(extras, "SecurityArch");
            foreach (float x in new[] { -0.95f, 0.95f })
            {
                Solid(arch, "Post", roundedSmall, x, -5.5f, 0.25f, 1.1f, "#6B7186");
            }

            Deco(arch, "Bar", roundedSmall, 0f, -4.95f, 2.2f, 0.16f, "#6B7186");
            Flickering(Deco(arch, "Light", circle, 0f, -4.95f, 0.14f, 0.14f, "#55E08F", 1f, 8), 0.5f, 1.5f, 1f);
            BuildNpc(IL(people, "Guard"), "Guardia", new Vector2(2.4f, -5.6f), new Look { Skin = "#4DA3FF", Extra = Accessory.Cap, ExtraColor = "#1D2238" }, false);

            var flags = IL(extras, "Flags");
            foreach (float x in new[] { -3.6f, 3.6f })
            {
                Solid(flags, "Pole", roundedSmall, x, -7.0f, 0.1f, 1.6f, "#C9CFDB");
                string[] bands = { "#2D5FB8", "#F8F5E8", "#E8505B", "#F8F5E8", "#2D5FB8" };
                for (int i = 0; i < 5; i++)
                {
                    Deco(flags, "Flag", square, x + 0.45f, -6.5f + (2 - i) * 0.1f - 0.05f, 0.8f, i == 2 ? 0.14f : 0.1f, bands[i]);
                }
            }

            var exits = IL(extras, "ExitSigns");
            Deco(exits, "Sign", roundedSmall, 0f, -8.15f, 1.0f, 0.3f, "#55E08F");
            Label(exits, "SALIDA", new Vector2(0f, -8.15f), GamePalette.Outline, 0.03f, TextAnchor.MiddleCenter);

            // Marble columns down both sides.
            var columns = IL(extras, "Columns");
            foreach (float x in new[] { -7.0f, 7.0f })
            {
                foreach (float y in new[] { -6.2f, -3.2f, -0.2f })
                {
                    Solid(columns, "Column", roundedSmall, x, y, 0.7f, 0.7f, "#F1F4FF");
                    Deco(columns, "Cap", roundedSmall, x, y + 0.3f, 0.9f, 0.14f, "#C9CFDB");
                }
            }

            // Reception, queue machine and information boards.
            var reception = IL(extras, "ReceptionDesk");
            Solid(reception, "Desk", rounded, -4.3f, -2.4f, 3.0f, 0.9f, "#7A6A52");
            Deco(reception, "Screen", roundedSmall, -4.0f, -2.3f, 0.7f, 0.45f, "#4DA3FF");
            Deco(reception, "Bell", circle, -5.0f, -2.35f, 0.2f, 0.2f, "#FFD23F");
            BuildNpc(IL(people, "Receptionist"), "Recepcionista", new Vector2(-4.3f, -1.5f), new Look { Skin = "#F2994A", Extra = Accessory.Tie, ExtraColor = "#E8505B" }, false);

            var tickets = IL(extras, "TicketDispenser");
            Solid(tickets, "Machine", rounded, 4.5f, -7.3f, 0.8f, 0.9f, "#4B5568");
            Deco(tickets, "Slot", roundedSmall, 4.5f, -7.1f, 0.5f, 0.1f, "#F8F5E8");

            var queue = IL(extras, "QueueScreen");
            Solid(queue, "Screen", rounded, 5.9f, -5.4f, 1.3f, 0.9f, "#1D2238");
            Label(queue, "TURNO 27", new Vector2(5.9f, -5.3f), GamePalette.Hex("#FFD23F"), 0.035f, TextAnchor.MiddleCenter);

            var rates = IL(extras, "InterestRateBoard");
            Solid(rates, "Board", rounded, -4.6f, -5.6f, 1.8f, 1.0f, "#1D2238");
            Label(rates, "TARJETA 3.5% AL MES", new Vector2(-4.6f, -5.45f), GamePalette.Hex("#E8505B"), 0.028f, TextAnchor.MiddleCenter);
            Label(rates, "AHORRO 0.3% AL MES", new Vector2(-4.6f, -5.75f), GamePalette.Hex("#55E08F"), 0.028f, TextAnchor.MiddleCenter);

            var loans = IL(extras, "LoanPosters");
            foreach (var at in new[] { new Vector2(-2.7f, -5.4f), new Vector2(2.4f, -4.4f) })
            {
                Solid(loans, "Stand", roundedSmall, at.x, at.y, 0.9f, 1.1f, "#F8F5E8");
                Label(loans, "PIDA EL COSTO TOTAL", at + new Vector2(0f, 0.05f), GamePalette.Outline, 0.02f, TextAnchor.MiddleCenter);
            }

            var brochures = IL(extras, "BrochureRacks");
            foreach (float x in new[] { -6.0f, 6.0f })
            {
                Solid(brochures, "Rack", rounded, x, -7.4f, 0.8f, 1.0f, "#B2475A");
                Deco(brochures, "Paper", roundedSmall, x, -7.3f, 0.5f, 0.6f, "#F8F5E8");
            }

            // Left wing: manager office with glass walls.
            var glassWalls = IL(extras, "OfficeGlass");
            Prop(glassWalls, "Glass", square, new Vector2(-9.1f, 4.6f), new Vector2(4.4f, 0.12f), Hex("#BFE3F5", 0.6f));
            Prop(glassWalls, "Glass", square, new Vector2(-6.9f, 6.4f), new Vector2(0.12f, 3.6f), Hex("#BFE3F5", 0.6f));
            var office = IL(extras, "OfficeDesk");
            Solid(office, "Desk", rounded, -9.4f, 6.4f, 2.6f, 1.0f, "#7A6A52");
            Deco(IL(extras, "OfficeLaptop"), "Laptop", roundedSmall, -9.4f, 6.5f, 0.7f, 0.45f, "#2A3040");
            Deco(IL(extras, "OfficeChair"), "Chair", circle, -9.4f, 5.6f, 0.7f, 0.7f, "#3B6FB6");
            var diplomas = IL(extras, "Diplomas");
            foreach (float x in new[] { -10.8f, -9.6f, -8.4f })
            {
                Deco(diplomas, "Frame", roundedSmall, x, 7.95f, 0.5f, 0.36f, "#7A4F2A");
                Deco(diplomas, "Paper", roundedSmall, x, 7.95f, 0.4f, 0.26f, "#F8F5E8", 1f, 8);
            }

            var filing = IL(extras, "FilingCabinets");
            foreach (float y in new[] { 7.0f, 6.0f, 5.0f })
            {
                Solid(filing, "Cabinet", rounded, -11.3f, y, 0.8f, 0.8f, "#8E96AA");
                Deco(filing, "Handle", square, -11.3f, y, 0.3f, 0.06f, "#2A3040");
            }

            // Left wing below the office: cubicles.
            var cubicles = IL(extras, "Cubicles");
            foreach (float y in new[] { 2.8f, 0.9f })
            {
                Prop(cubicles, "Partition", square, new Vector2(-9.0f, y + 0.9f), new Vector2(4.0f, 0.12f), GamePalette.Hex("#8E96AA"));
                Solid(cubicles, "Desk", rounded, -9.8f, y, 1.8f, 0.7f, "#7A6A52");
                Deco(cubicles, "Monitor", roundedSmall, -9.8f, y + 0.1f, 0.5f, 0.35f, "#4DA3FF");
            }

            var calculators = IL(extras, "Calculators");
            foreach (float y in new[] { 2.8f, 0.9f })
            {
                Deco(calculators, "Calc", roundedSmall, -9.0f, y, 0.3f, 0.4f, "#2A3040");
            }

            var penStands = IL(extras, "PenStands");
            Deco(penStands, "Pens", roundedSmall, -2.0f, 5.5f, 0.2f, 0.3f, "#E8505B");
            Deco(penStands, "Pens", roundedSmall, -4.4f, 5.4f, 0.2f, 0.3f, "#4DA3FF");

            // Right wing: deposit boxes, tellers, ATM row.
            var deposit = IL(extras, "SafeDepositWall");
            Deco(deposit, "Panel", rounded, 9.2f, 7.6f, 4.8f, 1.2f, "#6B7186", 1f, 6);
            for (int col = 0; col < 8; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    Deco(deposit, "Box", roundedSmall, 7.3f + col * 0.55f, 7.35f + row * 0.5f, 0.45f, 0.4f, "#C9CFDB", 1f, 8);
                    Deco(deposit, "Lock", circle, 7.3f + col * 0.55f, 7.35f + row * 0.5f, 0.08f, 0.08f, "#2A3040", 1f, 9);
                }
            }

            var secondCounter = IL(extras, "SecondTellerCounter");
            Solid(secondCounter, "Counter", rounded, 9.0f, 3.2f, 4.4f, 0.9f, "#7A6A52");
            foreach (float x in new[] { 7.4f, 9.0f, 10.6f })
            {
                Deco(secondCounter, "Glass", roundedSmall, x, 3.75f, 0.14f, 1.0f, "#BFE3F5", 0.7f);
            }

            BuildNpc(IL(people, "Teller2"), "Cajero2", new Vector2(9.0f, 4.4f), new Look { Skin = "#B28DFF", Extra = Accessory.Tie, ExtraColor = "#1D4F8C" }, false);

            var atms = IL(extras, "AtmRow");
            foreach (float y in new[] { 6.0f, 4.6f, 1.6f, 0.4f })
            {
                Solid(atms, "Atm", rounded, 11.3f, y, 1.0f, 1.1f, "#4B5568");
                Deco(atms, "Screen", roundedSmall, 11.3f, y + 0.2f, 0.7f, 0.4f, "#55E08F");
                Deco(atms, "Pad", roundedSmall, 11.3f, y - 0.3f, 0.5f, 0.2f, "#2A3040");
            }

            var counting = IL(extras, "CountingMachine");
            Solid(counting, "Machine", rounded, 7.6f, 3.2f, 0.7f, 0.5f, "#4B5568");

            var printer = IL(extras, "PrinterStation");
            Solid(printer, "Copier", rounded, 10.2f, -1.2f, 1.4f, 1.0f, "#DFE8F5");
            Deco(printer, "Panel", roundedSmall, 10.2f, -1.0f, 0.8f, 0.2f, "#4DA3FF");

            var shredder = IL(extras, "Shredder");
            Solid(shredder, "Shredder", roundedSmall, 11.3f, -2.2f, 0.6f, 0.6f, "#6B7186");

            var pig = IL(extras, "PiggyBankStatue");
            Solid(pig, "Pedestal", rounded, 9.4f, -3.4f, 1.4f, 0.8f, "#C9CFDB");
            Deco(pig, "Pig", circle, 9.4f, -2.9f, 1.0f, 0.7f, "#FF9FB0");
            Deco(pig, "Snout", circle, 9.9f, -2.85f, 0.3f, 0.24f, "#FF7A8A", 1f, 8);
            Deco(pig, "Ear", circle, 9.2f, -2.55f, 0.2f, 0.2f, "#FF7A8A", 1f, 8);

            var fountain = IL(extras, "CoinFountain");
            Solid(fountain, "Basin", circle, 9.6f, -5.6f, 2.0f, 1.2f, "#9AA3B8");
            Deco(fountain, "Water", circle, 9.6f, -5.6f, 1.7f, 0.95f, "#7FB6E8");
            for (int i = 0; i < 6; i++)
            {
                Flickering(Deco(fountain, "Coin", circle, 9.0f + i * 0.2f, -5.6f + ((i % 3) - 1) * 0.15f, 0.12f, 0.12f, "#FFD23F", 1f, 8), 0.6f, 1f + i * 0.3f, i);
            }

            var ropes = IL(extras, "VelvetRopes");
            foreach (float x in new[] { 6.0f, 7.0f, 8.0f })
            {
                Solid(ropes, "Post", circle, x, 1.0f, 0.3f, 0.3f, "#FFD23F");
            }

            Deco(ropes, "Rope", square, 7.0f, 1.0f, 2.0f, 0.06f, "#8B3A3A", 1f, 8);

            // Lower left: waiting lounge and children's corner.
            var lounge = IL(extras, "WaitingLounge");
            foreach (float y in new[] { -1.8f, -3.6f })
            {
                Solid(lounge, "Sofa", rounded, -9.4f, y, 3.0f, 0.9f, "#3B6FB6");
                Deco(lounge, "Cushion", roundedSmall, -9.4f, y, 2.6f, 0.5f, "#6FA0E0");
            }

            var coffee = IL(extras, "CoffeeTables");
            Solid(coffee, "Table", rounded, -9.4f, -2.7f, 1.4f, 0.7f, "#8B5A3C");

            var magazines = IL(extras, "Magazines");
            Deco(magazines, "Magazine", roundedSmall, -9.7f, -2.7f, 0.4f, 0.3f, "#E8505B");
            Deco(magazines, "Magazine", roundedSmall, -9.1f, -2.7f, 0.4f, 0.3f, "#FFD23F");

            var cooler = IL(extras, "WaterCooler");
            Solid(cooler, "Cooler", roundedSmall, -11.3f, -5.0f, 0.6f, 1.0f, "#CFE8FF");
            Deco(cooler, "Bottle", circle, -11.3f, -4.7f, 0.5f, 0.5f, "#7FB6E8");

            var machine = IL(extras, "CoffeeMachine");
            Solid(machine, "Machine", rounded, -10.2f, -5.0f, 0.8f, 0.8f, "#2A3040");
            Flickering(Deco(machine, "Light", circle, -10.2f, -4.9f, 0.1f, 0.1f, "#E8505B"), 0.7f, 1.6f, 4f);

            var kids = IL(extras, "ChildrensCorner");
            Decal(kids, "Rug", rounded, -9.2f, -6.6f, 3.2f, 2.0f, "#4DA3FF", 0.7f);
            Deco(kids, "Block", square, -9.8f, -6.5f, 0.4f, 0.4f, "#FFD23F", 1f, 6);
            Deco(kids, "Block", square, -9.2f, -6.8f, 0.4f, 0.4f, "#E8505B", 1f, 6);
            Deco(kids, "Ball", circle, -8.5f, -6.4f, 0.4f, 0.4f, "#55E08F", 1f, 6);

            var elevator = IL(extras, "Elevator");
            Solid(elevator, "Frame", rounded, -11.65f, -0.2f, 0.5f, 1.6f, "#8E96AA");
            Deco(elevator, "Door", square, -11.65f, -0.2f, 0.06f, 1.3f, "#2A3040");

            // Walls and ceiling.
            var clocks = IL(extras, "WallClocks");
            foreach (float x in new[] { -6.5f, 6.5f })
            {
                Deco(clocks, "Frame", circle, x, 8.0f, 0.6f, 0.6f, "#2A3345");
                Deco(clocks, "Face", circle, x, 8.0f, 0.48f, 0.48f, "#F8F5E8", 1f, 8);
                Deco(clocks, "Hand", square, x, 8.07f, 0.04f, 0.2f, "#2A3345", 1f, 9);
            }

            var logo = IL(extras, "BankLogo");
            Deco(logo, "Plate", roundedSmall, -3.2f, 8.0f, 2.4f, 0.4f, "#1D4F8C");
            Label(logo, "BANCO", new Vector2(-3.2f, 8.0f), GamePalette.White, 0.04f, TextAnchor.MiddleCenter);

            var deskLamps = IL(extras, "DeskLamps");
            foreach (var at in new[] { new Vector2(-9.9f, 6.9f), new Vector2(9.4f, 3.6f), new Vector2(-4.6f, -2.0f) })
            {
                Deco(deskLamps, "Lamp", circle, at.x, at.y, 0.3f, 0.3f, "#FFD23F");
                Flickering(Glow(lights, "Light_Desk", at, 2.8f, "#FFD23F", 0.2f, OrderLight), 0.06f, 0.9f, at.x);
            }

            var chandeliers = IL(lights, "Chandeliers");
            foreach (var at in new[] { new Vector2(-6f, -1f), new Vector2(0f, -1f), new Vector2(6f, -1f) })
            {
                Flickering(Glow(chandeliers, "Chandelier", at, 6f, "#FFE9A8", 0.14f, OrderLight), 0.04f, 0.4f, at.x);
            }

            CeilingGlows(IL(lights, "CeilingPanels"), room, 4f, "#FFFFFF", 0.06f);

            var beams = IL(extras, "WindowBeams");
            string previous = currentLayer;
            currentLayer = LayerForeground;
            for (int i = 0; i < 6; i++)
            {
                var beam = Shape(beams, "Beam", softCircle, new Vector2(-9f + i * 3.6f, 1f), new Vector2(1.6f, 14f), Hex("#FFF3C4", 0.06f), 3);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                Flickering(beam, 0.04f, 0.3f, i);
            }

            currentLayer = previous;

            var cameras = IL(extras, "SecurityCameras");
            foreach (var at in new[] { new Vector2(-11.6f, 8.0f), new Vector2(11.6f, 8.0f), new Vector2(-11.6f, -8.0f), new Vector2(11.6f, -8.0f) })
            {
                Deco(cameras, "Dome", circle, at.x, at.y, 0.4f, 0.4f, "#4A5166");
                Flickering(Deco(cameras, "Light", circle, at.x, at.y, 0.09f, 0.09f, "#E8505B", 1f, 8), 0.8f, 1.2f, at.x);
            }

            var bins = IL(extras, "TrashBins");
            foreach (var at in new[] { new Vector2(-6.9f, -7.8f), new Vector2(6.9f, -7.8f), new Vector2(11.4f, -0.6f) })
            {
                Solid(bins, "Bin", roundedSmall, at.x, at.y, 0.5f, 0.6f, "#4A6B5A");
            }

            var extinguishers = IL(extras, "FireExtinguishers");
            foreach (var at in new[] { new Vector2(-11.5f, 2.0f), new Vector2(11.5f, -4.4f) })
            {
                Solid(extinguishers, "Tank", roundedSmall, at.x, at.y, 0.3f, 0.5f, "#E8505B");
            }

            var plants = IL(extras, "LargePlants");
            foreach (var at in new[] { new Vector2(-6.4f, 4.4f), new Vector2(6.4f, 4.4f), new Vector2(-6.4f, -1.0f), new Vector2(6.4f, -1.0f), new Vector2(-11.2f, -7.6f), new Vector2(11.2f, -7.6f), new Vector2(11.2f, 7.4f), new Vector2(-6.4f, 8.0f) })
            {
                Plant(plants, at.x, at.y, 1.1f);
            }

            var benches = IL(extras, "WaitingBenches");
            Solid(benches, "Bench", rounded, 7.8f, -7.0f, 2.2f, 0.7f, "#3B6FB6");
            Solid(benches, "Bench", rounded, 3.0f, 0.3f, 2.4f, 0.7f, "#3B6FB6");

            var deskPlants = IL(extras, "DeskPlants");
            Deco(deskPlants, "Plant", circle, -1.2f, 1.4f + 4.2f, 0.4f, 0.4f, "#3FA66A");
            Deco(deskPlants, "Plant", circle, 10.2f, 3.5f, 0.4f, 0.4f, "#3FA66A");

            DustMotes(IL(extras, "DustMotes"), random, room, 40);
        }
    }
}

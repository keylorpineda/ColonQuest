using System.Collections.Generic;

namespace ColonQuest.EditorTools
{
    /// <summary>
    /// The v2 cast: one sheet per named person (see docs/characters-v2.md, section 4). A person with a sheet
    /// ignores the look the town gives it; everyone else keeps the color hash with the new head, hair and arms.
    /// </summary>
    public static partial class MapBuilder
    {
        private static readonly Look PlayerLook = new Look
        {
            Skin = "#E07A3F", SkinTone = "#D9A47A", HairColor = "#3A2418", Hair = HairStyle.Side, Pants = "#2F3E66",
            Face = Expression.Smile, Wear = Extras.Backpack
        };

        private static readonly Look PlayerLookF = new Look
        {
            Skin = "#C2477F", SkinTone = "#D9A47A", HairColor = "#3A2418", Hair = HairStyle.Long, Pants = "#2F3E66",
            Face = Expression.Smile, Wear = Extras.Backpack
        };

        private static readonly Dictionary<string, Look> Cast = new Dictionary<string, Look>
        {
            ["Vendedor"] = new Look
            {
                Skin = "#2E86C1", SkinTone = "#EBC29A", HairColor = "#2A1F1A", Hair = HairStyle.Spiky, Pants = "#3A3F52",
                Width = 0.92f, Height = 1.06f, Face = Expression.Smile, LookAside = 0.018f,
                Wear = Extras.Tie | Extras.Badge | Extras.Phone, WearColor = "#E8505B"
            },
            ["DonRafa"] = new Look
            {
                Skin = "#5B6E8C", SkinTone = "#8A5A3C", HairColor = "#2A1F1A", Hair = HairStyle.Buzz, Pants = "#3E4A63",
                Hat = Hat.Cap, HatColor = "#C0392B", BrimSide = -1, Face = Expression.Frown,
                Width = 1.14f, Height = 0.94f, Head = 1.05f, SquareJaw = true, Mustache = true,
                Wear = Extras.Overall, WearColor = "#3E5A86"
            },
            ["DonaMarta"] = new Look
            {
                Skin = "#D9707F", SkinTone = "#C98E66", HairColor = "#9A9A9A", Hair = HairStyle.Bun, Pants = "#5B4B4B",
                Face = Expression.Warm, Width = 1.1f, Height = 0.9f, Glasses = true, Blush = true, Wear = Extras.Apron
            },
            ["Prestamista"] = new Look
            {
                Skin = "#2D2D2D", SkinTone = "#B5835A", HairColor = "#1A1A1A", Hair = HairStyle.Buzz, Pants = "#1F2433",
                Hat = Hat.FlatCap, HatColor = "#3A3A3A", Face = Expression.Smirk, MouthX = 0.02f, Width = 1.12f,
                Goatee = true, HeavyLids = true, Wear = Extras.Chain | Extras.Pouch
            },
            ["Promotor"] = new Look
            {
                Skin = "#F4F1E8", SkinTone = "#E2B48C", HairColor = "#4A2F20", Hair = HairStyle.Side, Pants = "#2B3550",
                Face = Expression.Smile, LookAside = 0.02f, Width = 0.95f, Height = 1.04f,
                Wear = Extras.Tie | Extras.Watch | Extras.Phone, WearColor = "#7D3C98"
            },
            ["DonBeto"] = new Look
            {
                Skin = "#8FAF6A", SkinTone = "#A06A45", HairColor = "#6A6A6A", Hair = HairStyle.Bald, Pants = "#5A4A3A",
                Hat = Hat.Chonete, HatColor = "#E8D9A8", Face = Expression.Warm, Height = 0.98f, Mustache = true,
                Shoes = "#1E1E1E"
            },
            ["Cajera"] = new Look
            {
                Skin = "#EAF0F8", SkinTone = "#C68A62", HairColor = "#2A1F1A", Hair = HairStyle.Ponytail, Pants = "#1F3A5F",
                Face = Expression.Neutral, Skirt = true, Wear = Extras.Scarf | Extras.Lanyard, WearColor = "#1D4F8C"
            },
            ["Oficial"] = new Look
            {
                Skin = "#22324F", SkinTone = "#9C6B47", HairColor = "#1A1A1A", Hair = HairStyle.Buzz, Pants = "#1A2438",
                Hat = Hat.Police, HatColor = "#22324F", Face = Expression.Neutral, Width = 1.08f, LongSleeve = true,
                Wear = Extras.PoliceBadge
            },
            ["Juez"] = new Look
            {
                Skin = "#1E1E26", SkinTone = "#D7A27E", HairColor = "#5A3A28", Hair = HairStyle.Bob, Pants = "#1E1E26",
                Face = Expression.Neutral, Glasses = true, LongSleeve = true, Wear = Extras.Robe
            },
            ["Enfermera"] = new Look
            {
                Skin = "#7FC8B8", SkinTone = "#B07A52", HairColor = "#2A1F1A", Hair = HairStyle.Curly, Pants = "#7FC8B8",
                Face = Expression.Smile, Wear = Extras.Pen, WearColor = "#2E86C1"
            },
            ["Doctora"] = new Look
            {
                Skin = "#6FA8DC", SkinTone = "#F0CDB0", HairColor = "#B0452D", Hair = HairStyle.Ponytail, Pants = "#3A3F52",
                Face = Expression.Warm, Wear = Extras.Coat | Extras.Stethoscope
            },
            ["AsesoraCoop"] = new Look
            {
                Skin = "#2E8B57", SkinTone = "#8A5A3C", HairColor = "#1A1A1A", Hair = HairStyle.Afro, Pants = "#2F3E66",
                Face = Expression.Smile, Wear = Extras.Cardigan | Extras.Lanyard, WearColor = "#F1E4C8", WearColor2 = "#2E8B57"
            },
            ["DonaLucia"] = new Look
            {
                Skin = "#F2C14E", SkinTone = "#C98E66", HairColor = "#3A2418", Hair = HairStyle.Bun, Pants = "#7A4E3A",
                Hat = Hat.Beanie, HatColor = "#E8505B", Face = Expression.Warm, Width = 1.12f, Height = 0.92f,
                Blush = true, Wear = Extras.Apron
            },
            ["DonChepe"] = new Look
            {
                Skin = "#9E3D3D", SkinTone = "#E0B08A", HairColor = "#BDBDBD", Hair = HairStyle.Bald, Pants = "#3A3F52",
                Face = Expression.Smile, Width = 1.08f, Glasses = true, Mustache = true, Wear = Extras.Cardigan,
                WearColor = "#6B5B50"
            },
            ["DonMario"] = new Look
            {
                Skin = "#E9E2D0", SkinTone = "#B5835A", HairColor = "#555A66", Hair = HairStyle.Buzz, Pants = "#4A5A3A",
                Face = Expression.Neutral, Width = 1.05f, Height = 0.97f, Goatee = true, Wear = Extras.Stripe,
                WearColor = "#C0392B"
            },
            ["Financiera"] = new Look
            {
                Skin = "#F7F7F7", SkinTone = "#E7B994", HairColor = "#D9A441", Hair = HairStyle.Bob, Pants = "#2B2B3A",
                Face = Expression.Smirk, MouthX = 0.02f, Height = 1.03f, Skirt = true, HeavyLids = true,
                Wear = Extras.Cardigan | Extras.Bag, WearColor = "#2A7F8C"
            },
            ["AgenteInmobiliaria"] = new Look
            {
                Skin = "#34495E", SkinTone = "#9C6B47", HairColor = "#2A1F1A", Hair = HairStyle.Side, Pants = "#2C3E50",
                Face = Expression.Smile, Height = 1.05f, LongSleeve = true, Wear = Extras.Tie | Extras.Pen,
                WearColor = "#F2C14E"
            },
            ["Estudiante"] = new Look
            {
                Skin = "#C0392B", SkinTone = "#D9A47A", HairColor = "#1A1A1A", Hair = HairStyle.Long, Pants = "#2F3E66",
                Face = Expression.Smile, Width = 0.92f, Height = 0.97f, Wear = Extras.Backpack | Extras.Stripe,
                WearColor = "#F4F1E8"
            },
            ["Repartidor"] = new Look
            {
                Skin = "#F39C12", SkinTone = "#80553A", HairColor = "#1A1A1A", Hair = HairStyle.Buzz, Pants = "#2D2D2D",
                Hat = Hat.Helmet, HatColor = "#2D2D2D", Face = Expression.Neutral, LongSleeve = true,
                Wear = Extras.Stripe, WearColor = "#2D2D2D"
            }
        };

        /// <summary>The sheet of a named person, or the look the town gave it when there is none. Some names share a sheet.</summary>
        private static Look ApplyCast(string name, Look look)
        {
            string key = name;
            if (name == "Mecanico")
            {
                key = "DonRafa";
            }
            else if (name == "Cajero2")
            {
                key = "Cajera";
            }
            else if (name == "Estafador")
            {
                key = "Promotor";
            }
            else if (name.StartsWith("Doctor"))
            {
                key = "Doctora";
            }

            Look sheet;
            return Cast.TryGetValue(key, out sheet) ? sheet : look;
        }
    }
}

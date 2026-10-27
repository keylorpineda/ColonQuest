using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Flat colors shared by the map and the UI so the whole game reads as one
    /// Among Us style cartoon: chunky dark outlines, solid fills, no gradients.
    /// </summary>
    public static class GamePalette
    {
        public static readonly Color Space = Hex("#141A2E");
        public static readonly Color Outline = Hex("#0B0E1A");
        public static readonly Color Wall = Hex("#2B3550");

        public static readonly Color FloorHome = Hex("#D8B78A");
        public static readonly Color FloorStore = Hex("#5FB8AA");
        public static readonly Color FloorBank = Hex("#7D96D4");
        public static readonly Color FloorPlaza = Hex("#A9B0C4");
        public static readonly Color FloorCorridor = Hex("#8E97AD");

        public static readonly Color Marker = Hex("#FFD23F");
        public static readonly Color MoneyIn = Hex("#55E08F");
        public static readonly Color Debt = Hex("#E8505B");
        public static readonly Color Alert = Hex("#4DA3FF");
        public static readonly Color White = Hex("#F1F4FF");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        /// <summary>Multiplies the RGB channels, keeping alpha. Below 1 darkens, above 1 lightens.</summary>
        public static Color Shade(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The clothes the player wears, chosen at the wardrobe. The body is built once with the everyday clothes; a change of
    /// clothes recolors the shirt and the pants, and the first look is remembered so it can always come back. In the
    /// shower the clothes come off and a pixel blur covers the body.
    /// </summary>
    public static class PlayerOutfit
    {
        public const int Everyday = 0;
        public const int Sunday = 1;
        public const int Sport = 2;
        public const int Swimsuit = 3;

        /// <summary>What each outfit is called in the wardrobe, by number.</summary>
        public static readonly string[] Names = { "Ropa de diario", "Ropa de domingo", "Ropa deportiva", "Bañador" };

        private static readonly Color[] Shirts = { Color.clear, GamePalette.Hex("#F8F5E8"), GamePalette.Hex("#E8505B"), Color.clear };
        private static readonly Color[] Pants = { Color.clear, GamePalette.Hex("#2A3345"), GamePalette.Hex("#3A3F52"), GamePalette.Hex("#4DA3FF") };

        private static readonly Dictionary<string, float> ShirtParts = new Dictionary<string, float>
        {
            { "Torso", 1f }, { "Sleeve", 1f }, { "Collar", 0.78f }, { "TorsoShade", 0.84f }, { "TorsoLight", 1.15f }, { "SleeveEdge", 0.82f }
        };

        private static readonly Dictionary<string, float> PantsParts = new Dictionary<string, float>
        {
            { "Hips", 1f }, { "Skirt", 1f }, { "Crease", 0.78f }, { "Leg", 1f }
        };

        public static int Count => Names.Length;

        /// <summary>The name of the pixel blur of the shower (see <see cref="InteractionProps.Censor"/>).</summary>
        public const string CensorName = "Censor";

        /// <summary>Dresses the player in this outfit. The everyday one brings back the colors the body was built with.</summary>
        public static void Wear(GameObject player, int outfit)
        {
            var memory = Remember(player);
            if (memory == null)
            {
                return;
            }

            // Dressed again: the blur of the shower goes, even if the scene was skipped before it timed out.
            foreach (var part in player.GetComponentsInChildren<Transform>())
            {
                if (part.name == CensorName)
                {
                    Object.Destroy(part.gameObject);
                }
            }

            bool everyday = outfit <= Everyday || outfit >= Count;
            if (everyday)
            {
                // Exactly the colors the body was built with, shades and all.
                foreach (var pair in memory.Original)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.color = pair.Value;
                    }
                }

                return;
            }

            Color skin = memory.Skin;
            Color shirt = outfit == Swimsuit ? skin : Shirts[outfit];
            Color pants = Pants[outfit];
            Paint(memory, shirt, pants, outfit == Swimsuit);
        }

        /// <summary>Takes the clothes off for the shower: the shirt and the pants turn into skin, and the blur goes on top.</summary>
        public static void Undress(GameObject player)
        {
            var memory = Remember(player);
            if (memory == null)
            {
                return;
            }

            Paint(memory, memory.Skin, memory.Skin, true);
        }

        /// <summary>The color of the player's skin, taken from the hands.</summary>
        public static Color SkinOf(GameObject player)
        {
            var memory = Remember(player);
            return memory != null ? memory.Skin : Color.white;
        }

        private static void Paint(OutfitMemory memory, Color shirt, Color pants, bool bareArms)
        {
            foreach (var pair in memory.Original)
            {
                var renderer = pair.Key;
                if (renderer == null)
                {
                    continue;
                }

                string name = renderer.name;
                if (ShirtParts.TryGetValue(name, out float shirtShade))
                {
                    renderer.color = GamePalette.Shade(shirt, shirtShade);
                }
                else if ((name == "Forearm" || name == "Joint") && Same(pair.Value, memory.Shirt))
                {
                    // Long sleeves: the forearm is part of the shirt, unless the arms are bare.
                    renderer.color = bareArms ? memory.Skin : shirt;
                }
                else if (PantsParts.TryGetValue(name, out float pantsShade) && (name != "Leg" || Same(pair.Value, memory.Pants)))
                {
                    renderer.color = GamePalette.Shade(pants, pantsShade);
                }
            }
        }

        private static OutfitMemory Remember(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            var memory = player.GetComponent<OutfitMemory>();
            if (memory != null)
            {
                return memory;
            }

            memory = player.AddComponent<OutfitMemory>();
            foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                memory.Original[renderer] = renderer.color;
                switch (renderer.name)
                {
                    case "Torso":
                        memory.Shirt = renderer.color;
                        break;
                    case "Hips":
                    case "Skirt":
                        memory.Pants = renderer.color;
                        break;
                    case "Hand":
                        memory.Skin = renderer.color;
                        break;
                }
            }

            return memory;
        }

        private static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;
        }

        /// <summary>The colors the player's body was built with, kept on the player itself.</summary>
        private sealed class OutfitMemory : MonoBehaviour
        {
            public readonly Dictionary<SpriteRenderer, Color> Original = new Dictionary<SpriteRenderer, Color>();
            public Color Shirt;
            public Color Pants;
            public Color Skin;
        }
    }
}

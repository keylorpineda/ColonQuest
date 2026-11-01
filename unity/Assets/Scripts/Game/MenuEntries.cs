using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.Game
{
    public enum MenuEntry
    {
        Story,
        Demo,
        Settings,
        Credits,
        Quit
    }

    /// <summary>What the main menu offers and says. Kept apart from the screen so it can be tested.</summary>
    public static class MenuEntries
    {
        public const string Team = "DoWhile Team";

        public static readonly string[] Makers = { "Keylor", "Jefferson" };

        /// <summary>The Meta tools the game uses and what each one does for the player.</summary>
        public static readonly string[][] Tools =
        {
            new[] { "Muse Spark", "El game master que cuenta cada evento, con sus herramientas" },
            new[] { "Muse Spark Open", "Los vendedores que te esconden los cargos, en Baseten" },
            new[] { "Muse Voice Transcribe", "Entiende lo que le decís al vendedor" },
            new[] { "Muse Wonder", "Planifica tu año y narra cómo terminó" },
            new[] { "Muse Image", "Los retratos, las escenas y la postal del año" }
        };

        /// <summary>A browser tab cannot close itself, so the quit button exists everywhere else.</summary>
        public static bool QuitAvailable(RuntimePlatform platform)
        {
            return platform != RuntimePlatform.WebGLPlayer;
        }

        /// <summary>The buttons of the menu, top to bottom, for one platform.</summary>
        public static IReadOnlyList<MenuEntry> For(RuntimePlatform platform)
        {
            var list = new List<MenuEntry> { MenuEntry.Story, MenuEntry.Demo, MenuEntry.Settings, MenuEntry.Credits };
            if (QuitAvailable(platform))
            {
                list.Add(MenuEntry.Quit);
            }

            return list;
        }

        public static string LabelOf(MenuEntry entry)
        {
            switch (entry)
            {
                case MenuEntry.Story: return "Modo historia";
                case MenuEntry.Demo: return "Demo de funciones";
                case MenuEntry.Settings: return "Ajustes";
                case MenuEntry.Credits: return "Créditos";
                default: return "Salir";
            }
        }
    }
}

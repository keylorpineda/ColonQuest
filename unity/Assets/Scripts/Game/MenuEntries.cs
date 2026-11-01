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

        /// <summary>Who made the game and what each one was in charge of.</summary>
        public static readonly string[][] Makers =
        {
            new[] { "Keylor Pineda", "Backend, inteligencia artificial y motor financiero" },
            new[] { "Jefferson Abarca", "Unity, arte y flujo del juego" }
        };

        /// <summary>The Meta tools the game uses, what each one does for the player and the time measured for it (docs/adr/0004-model-roles.md).</summary>
        public static readonly string[][] Tools =
        {
            new[] { "Muse Spark", "El game master que cuenta cada evento, con sus herramientas", "3 a 4,4 s: el único que siempre llega a tiempo" },
            new[] { "Muse Spark Open", "Los vendedores que te esconden los cargos, en Baseten", "1,9 a 2,8 s, con pesos abiertos" },
            new[] { "Muse Voice Transcribe", "Entiende lo que le decís al vendedor", "0,7 a 1,5 s, y escribir siempre sirve" },
            new[] { "Muse Wonder", "Planifica tu año y narra cómo terminó", "6 a 10 s: solo donde nadie espera" },
            new[] { "Muse Image", "El valle del menú, el tucán, los retratos, las escenas y la postal del año", "14 a 17 s: todas se hicieron antes" }
        };

        /// <summary>The agents that decide and use tools, with what each one is after.</summary>
        public static readonly string[][] Agents =
        {
            new[] { "Game master", "Revisa tu historial y calcula deudas y compras reales antes de contarte qué pasa." },
            new[] { "Planificador del año", "Arma los doce meses según tu perfil y trae de vuelta lo que te costó." },
            new[] { "Vendedor", "Busca su comisión. No puede mentir en las cifras: solo calla hasta que preguntás." }
        };

        /// <summary>The game in figures, big number first. "Pruebas" adds the EditMode, PlayMode and server suites of 1 November.</summary>
        public static readonly string[][] Figures =
        {
            new[] { "5", "modelos de Meta" },
            new[] { "3", "agentes con herramientas" },
            new[] { "20", "imágenes de Muse Image" },
            new[] { "1 100+", "pruebas automáticas" }
        };

        /// <summary>What it is built with and what for.</summary>
        public static readonly string[][] Stack =
        {
            new[] { "Unity 6 y C#", "El juego y el motor financiero" },
            new[] { "FastAPI y Pydantic", "El servidor y sus contratos validados" },
            new[] { "PostgreSQL en Neon", "Partidas, decisiones y clases" },
            new[] { "Render", "Donde vive el servidor" },
            new[] { "Baseten", "Donde corre el modelo de pesos abiertos" }
        };

        /// <summary>The fonts in the build, as their files name them, and the license that asks for this credit.</summary>
        public static readonly string[][] Fonts =
        {
            new[] { "Baloo 2", "The Baloo 2 Project Authors. SIL Open Font License 1.1" },
            new[] { "Nunito", "The Nunito Project Authors. SIL Open Font License 1.1" }
        };

        /// <summary>The portraits that decorate the credits, all pregenerated with Muse Image.</summary>
        public static IReadOnlyList<string> CreditPortraits => DemoReadout.Portraits;

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

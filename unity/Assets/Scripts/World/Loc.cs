using System;
using System.Collections.Generic;

namespace ColonQuest.World
{
    public enum GameLanguage
    {
        Spanish,
        English
    }

    /// <summary>
    /// The text of the game in two languages. The Spanish text is the key: <c>Loc.T("Ajustes")</c> gives "Settings" in
    /// English and "Ajustes" in Spanish. A text with no English entry stays in Spanish, so a missing translation
    /// shows up as Spanish and never breaks the game. Costa Rican words are carried over into colloquial English
    /// (see <c>docs/localization.md</c> for the glossary).
    /// </summary>
    public static class Loc
    {
        /// <summary>Raised after the language changes, so every window can write its text again.</summary>
        public static event Action Changed;

        public static GameLanguage Current => GameSettings.Current.Language;

        /// <summary>Changes the language, keeps it and tells the windows.</summary>
        public static void Set(GameLanguage language)
        {
            var settings = GameSettings.Current;
            if (settings.Language == language)
            {
                return;
            }

            settings.Language = language;
            settings.Save();
            Changed?.Invoke();
        }

        public static string T(string spanish)
        {
            return Translate(spanish, Current);
        }

        /// <summary>The text in the language, or the Spanish text itself when it has no entry.</summary>
        public static string Translate(string spanish, GameLanguage language)
        {
            if (language == GameLanguage.Spanish || string.IsNullOrEmpty(spanish))
            {
                return spanish;
            }

            return Translations.English.TryGetValue(spanish, out string english) ? english : spanish;
        }

        /// <summary>A text with {0}, {1}... holes: it is translated first and the values go in afterwards.</summary>
        public static string F(string spanishTemplate, params object[] values)
        {
            return string.Format(T(spanishTemplate), values);
        }

        /// <summary>Every Spanish text that has an English entry, for tests and for the glossary.</summary>
        public static IReadOnlyCollection<string> KnownTexts => Translations.English.Keys;
    }
}

using System.Text;

namespace ColonQuest.World
{
    /// <summary>
    /// Who the player is in this run: the name and the gender chosen for a new game. The dialogues read it for the name and
    /// for the words that change with the gender, and <see cref="PlayerLookSwitch"/> reads it for the look.
    /// </summary>
    public static class PlayerProfile
    {
        public const int MaxNameLength = 16;
        public const string DefaultName = "Alex";

        public static string Name { get; private set; } = string.Empty;

        public static bool Female { get; private set; }

        public static bool HasName => !string.IsNullOrEmpty(Name);

        public static void Set(string name, bool female)
        {
            Name = name ?? string.Empty;
            Female = female;
        }

        public static void Reset()
        {
            Set(string.Empty, false);
        }

        /// <summary>The Spanish word or line for the gender of the player. Both forms have the same English entry.</summary>
        public static string Pick(string male, string female)
        {
            return Female ? female : male;
        }

        /// <summary>
        /// A name that is safe to show: no spaces at the ends or repeated, none of the marks that rich text and the text
        /// templates use, and not too long for a subtitle. An empty name gets the default one.
        /// </summary>
        public static string CleanName(string typed)
        {
            string name = Strip(typed);
            return name.Length > 0 ? name : DefaultName;
        }

        /// <summary>True when what was typed leaves a real name once it is cleaned, not only spaces or symbols.</summary>
        public static bool IsUsableName(string typed)
        {
            return Strip(typed).Length > 0;
        }

        private static string Strip(string typed)
        {
            var clean = new StringBuilder();
            foreach (char c in typed ?? string.Empty)
            {
                if (c == '<' || c == '>' || c == '{' || c == '}' || char.IsControl(c))
                {
                    continue;
                }

                if (char.IsWhiteSpace(c) && (clean.Length == 0 || clean[clean.Length - 1] == ' '))
                {
                    continue;
                }

                clean.Append(char.IsWhiteSpace(c) ? ' ' : c);
            }

            string name = clean.ToString().Trim();
            if (name.Length > MaxNameLength)
            {
                name = name.Substring(0, MaxNameLength).TrimEnd();
            }

            return name;
        }
    }
}

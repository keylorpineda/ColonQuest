using System.Collections.Generic;
using System.Text;

namespace ColonQuest.Game
{
    /// <summary>
    /// Shows text a few letters at a time, like someone talking. Rich text tags such as
    /// &lt;color=red&gt; do not count as letters and are closed again, so a half-shown line never breaks.
    /// </summary>
    public static class TextReveal
    {
        /// <summary>How many letters a person sees per second while a character talks.</summary>
        public const float LettersPerSecond = 40f;

        /// <summary>The number of visible letters in the text, not counting tags.</summary>
        public static int VisibleLength(string full)
        {
            if (string.IsNullOrEmpty(full))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < full.Length; i++)
            {
                if (full[i] == '<' && TagEnd(full, i) > i)
                {
                    i = TagEnd(full, i);
                    continue;
                }

                count++;
            }

            return count;
        }

        /// <summary>The start of the text with <paramref name="visible"/> letters showing and every open tag closed.</summary>
        public static string Prefix(string full, int visible)
        {
            if (string.IsNullOrEmpty(full) || visible <= 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            var open = new Stack<string>();
            int shown = 0;
            for (int i = 0; i < full.Length; i++)
            {
                if (full[i] == '<')
                {
                    int end = TagEnd(full, i);
                    if (end > i)
                    {
                        string tag = full.Substring(i, end - i + 1);
                        result.Append(tag);
                        Track(tag, open);
                        i = end;
                        continue;
                    }
                }

                if (shown >= visible)
                {
                    break;
                }

                result.Append(full[i]);
                shown++;
            }

            while (open.Count > 0)
            {
                result.Append("</").Append(open.Pop()).Append('>');
            }

            return result.ToString();
        }

        private static int TagEnd(string text, int start)
        {
            int end = text.IndexOf('>', start);
            if (end < 0 || end - start > 40)
            {
                return -1;
            }

            // A tag starts with a letter or a slash: "<color=red>" or "</color>", never "<3".
            char next = start + 1 < text.Length ? text[start + 1] : ' ';
            return char.IsLetter(next) || next == '/' ? end : -1;
        }

        private static void Track(string tag, Stack<string> open)
        {
            if (tag.StartsWith("</"))
            {
                if (open.Count > 0)
                {
                    open.Pop();
                }

                return;
            }

            int stop = 1;
            while (stop < tag.Length && char.IsLetter(tag[stop]))
            {
                stop++;
            }

            open.Push(tag.Substring(1, stop - 1));
        }
    }
}

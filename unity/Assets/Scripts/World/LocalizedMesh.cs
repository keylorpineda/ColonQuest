using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Keeps a sign of the world in the player's language. It remembers the Spanish text the sign was made with
    /// and writes it again whenever the language changes; a sign without an English entry keeps its Spanish text.
    /// </summary>
    /// <remarks>
    /// Every plate and board of the town was sized for its Spanish text. A translation can be longer ("RECEPCIÓN Y
    /// SEGURO" is "RECEPTION AND INSURANCE"), so the letters shrink until the new text is no wider than the Spanish one
    /// was: a sign never spills out of its plate, whatever the language.
    /// </remarks>
    [RequireComponent(typeof(TextMesh))]
    public sealed class LocalizedMesh : MonoBehaviour
    {
        [SerializeField] private string spanish = string.Empty;
        [SerializeField] private float baseCharacterSize;

        public string Spanish => spanish;

        public void Bind(string spanishText)
        {
            spanish = spanishText;
            var mesh = GetComponent<TextMesh>();
            if (mesh != null)
            {
                baseCharacterSize = mesh.characterSize;
            }

            Apply();
        }

        /// <summary>
        /// How much a text must shrink to be no wider than the original: 1 when it already fits (or when a width is
        /// unknown), less than 1 when it is wider.
        /// </summary>
        public static float FitScale(float originalWidth, float currentWidth)
        {
            if (originalWidth <= 0f || currentWidth <= originalWidth)
            {
                return 1f;
            }

            return originalWidth / currentWidth;
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        private void Apply()
        {
            var mesh = GetComponent<TextMesh>();
            if (mesh == null || string.IsNullOrEmpty(spanish))
            {
                return;
            }

            if (baseCharacterSize <= 0f)
            {
                baseCharacterSize = mesh.characterSize;
            }

            string text = Loc.T(spanish);
            mesh.text = text;
            float scale = text == spanish ? 1f : FitScale(WidestLine(mesh, spanish), WidestLine(mesh, text));
            mesh.characterSize = baseCharacterSize * scale;
        }

        /// <summary>The width of the widest line of a text in this mesh's font, in the font's own units (0 if unknown).</summary>
        private static float WidestLine(TextMesh mesh, string text)
        {
            var font = mesh.font;
            if (font == null || string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            int size = mesh.fontSize > 0 ? mesh.fontSize : font.fontSize;
            font.RequestCharactersInTexture(text, size, mesh.fontStyle);
            float widest = 0f;
            float line = 0f;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    widest = Mathf.Max(widest, line);
                    line = 0f;
                    continue;
                }

                // A glyph the font cannot give is counted as half the size, so it still takes room.
                line += font.GetCharacterInfo(c, out CharacterInfo info, size, mesh.fontStyle) ? info.advance : size * 0.5f;
            }

            return Mathf.Max(widest, line);
        }
    }
}

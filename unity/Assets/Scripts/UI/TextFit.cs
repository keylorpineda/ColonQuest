using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Keeps a label inside its box in any language, and gives it the right weight of the interface font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fitting: when the text no longer fits its rectangle (an English line longer than the Spanish one, a value with
    /// more digits), the font size goes down one step at a time until it fits, never below <see cref="MinFraction"/> of
    /// the size the code asked for. When the text gets shorter again the size comes back. A label that the code gives a
    /// new size, font or rectangle starts over from that new size, so code that sets <c>fontSize</c> still wins.
    /// </para>
    /// <para>
    /// Weight: the interface fonts are static files, one bold and one regular (the old variable font rendered as its
    /// thinnest weight). The code keeps asking with <c>fontStyle</c> as it always did: Bold gives the bold file, Normal
    /// the regular file, Italic the regular file slanted. Titles (another font) are left alone.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Text))]
    public sealed class TextFit : MonoBehaviour
    {
        /// <summary>The smallest a label may get, as a share of the size the code asked for.</summary>
        public const float MinFraction = 0.6f;

        private Text label;
        private RectTransform rect;
        private int baseSize;
        private int appliedSize;
        private string fittedText;
        private Vector2 fittedRect;
        private Font fittedFont;
        private FontStyle appliedStyle;
        private bool styleApplied;

        /// <summary>The size the code asked for, before any shrinking.</summary>
        public int BaseSize => baseSize > 0 ? baseSize : (label != null ? label.fontSize : 0);

        public static TextFit Attach(Text text)
        {
            var fit = text.GetComponent<TextFit>();
            if (fit == null)
            {
                fit = text.gameObject.AddComponent<TextFit>();
            }

            return fit;
        }

        /// <summary>The smallest size a label of <paramref name="size"/> may shrink to.</summary>
        public static int MinSizeFor(int size)
        {
            return Mathf.Max(10, Mathf.FloorToInt(size * MinFraction));
        }

        /// <summary>True when a text of this preferred size fits the box, with half a pixel of slack.</summary>
        public static bool Fits(float preferredWidth, float preferredHeight, Vector2 box, bool checkWidth)
        {
            bool heightFits = preferredHeight <= box.y + 0.5f;
            bool widthFits = !checkWidth || preferredWidth <= box.x + 0.5f;
            return heightFits && widthFits;
        }

        private void Awake()
        {
            label = GetComponent<Text>();
            rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            fittedText = null;
        }

        private void LateUpdate()
        {
            if (label == null)
            {
                return;
            }

            ApplyWeight();

            // The code changed the size itself: that is the new size to fit from.
            if (label.fontSize != appliedSize)
            {
                baseSize = label.fontSize;
                fittedText = null;
            }

            if (label.resizeTextForBestFit || label.verticalOverflow == VerticalWrapMode.Truncate)
            {
                // Best fit already does the job (the buttons), and a truncating box (the chat log) cuts on purpose:
                // shrinking it would only make a long conversation tiny.
                appliedSize = label.fontSize;
                return;
            }

            if (fittedText == label.text && fittedRect == rect.rect.size && fittedFont == label.font)
            {
                return;
            }

            Refit();
        }

        /// <summary>
        /// Gets the label ready to be measured by layout code in the same frame it was made: the right font file and the
        /// size the code asked for. The next frame fits it again to the box it ends up with.
        /// </summary>
        public void PrepareForMeasure()
        {
            if (label == null)
            {
                Awake();
            }

            ApplyWeight();
            if (baseSize <= 0)
            {
                baseSize = label.fontSize;
            }

            label.fontSize = baseSize;
            appliedSize = baseSize;
            fittedText = null;
        }

        /// <summary>Measures again now, for code that needs the final size in the same frame.</summary>
        public void Refit()
        {
            if (label == null)
            {
                return;
            }

            if (baseSize <= 0)
            {
                baseSize = label.fontSize;
            }

            fittedText = label.text;
            fittedRect = rect.rect.size;
            fittedFont = label.font;

            int size = baseSize;
            int min = MinSizeFor(baseSize);
            label.fontSize = size;
            if (!string.IsNullOrEmpty(label.text) && fittedRect.x > 1f && fittedRect.y > 1f)
            {
                while (size > min && !FitsNow())
                {
                    size--;
                    label.fontSize = size;
                }
            }

            appliedSize = label.fontSize;
        }

        private bool FitsNow()
        {
            var box = rect.rect.size;
            bool wraps = label.horizontalOverflow == HorizontalWrapMode.Wrap;
            var settings = label.GetGenerationSettings(new Vector2(wraps ? box.x : 0f, 0f));
            var generator = label.cachedTextGeneratorForLayout;
            float scale = label.pixelsPerUnit > 0f ? label.pixelsPerUnit : 1f;
            float height = generator.GetPreferredHeight(label.text, settings) / scale;
            float width = generator.GetPreferredWidth(label.text, settings) / scale;
            return Fits(width, height, box, !wraps);
        }

        /// <summary>Bold, normal or italic, mapped to the bold or the regular file of the interface font.</summary>
        private void ApplyWeight()
        {
            var bold = UiKit.Font;
            var regular = UiKit.RegularFont;
            if (label.font == UiKit.TitleFont)
            {
                // The title font is already a bold file: asking for bold on top would smear it.
                if (label.fontStyle == FontStyle.Bold)
                {
                    label.fontStyle = FontStyle.Normal;
                    fittedText = null;
                }
                else if (label.fontStyle == FontStyle.BoldAndItalic)
                {
                    label.fontStyle = FontStyle.Italic;
                    fittedText = null;
                }

                return;
            }

            if (label.font != bold && label.font != regular)
            {
                return;
            }

            if (styleApplied && label.fontStyle == appliedStyle)
            {
                return;
            }

            // A style the code set itself: read what it asks for and turn it into a font and a style.
            FontStyle asked = label.fontStyle;
            bool wantsBold = asked == FontStyle.Bold || asked == FontStyle.BoldAndItalic;
            bool wantsItalic = asked == FontStyle.Italic || asked == FontStyle.BoldAndItalic;
            label.font = wantsBold || regular == null ? bold : regular;
            label.fontStyle = wantsItalic ? FontStyle.Italic : FontStyle.Normal;
            appliedStyle = label.fontStyle;
            styleApplied = true;
            fittedText = null;
        }
    }
}

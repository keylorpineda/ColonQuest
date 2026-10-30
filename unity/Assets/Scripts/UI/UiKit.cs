using System;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Small helpers to build the Among Us style overlay from code: rounded cards with a thick dark
    /// outline, bold text and chunky buttons. The panels are built at runtime, so there are no
    /// prefabs or hand-made assets to keep in sync.
    /// </summary>
    public static class UiKit
    {
        public const float Outline = 6f;

        public static readonly Color Ink = GamePalette.Outline;
        public static readonly Color Paper = GamePalette.Hex("#F8F5E8");
        public static readonly Color Dim = new Color(0.02f, 0.03f, 0.08f, 0.62f);

        private static Sprite rounded;
        private static Font font;

        /// <summary>A white 9-sliced rounded rectangle, tinted per use.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null)
                {
                    rounded = MakeRounded(64, 22);
                }

                return rounded;
            }
        }

        private static Font titleFont;

        /// <summary>
        /// The font of the whole interface: a rounder one of the computer when it has it, and the built-in one in the browser,
        /// where there are no fonts of the system.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = SystemFont(new[] { "Trebuchet MS", "Segoe UI", "Verdana" });
                }

                return font;
            }
        }

        /// <summary>A heavier font for the name of the game and the big titles.</summary>
        public static Font TitleFont
        {
            get
            {
                if (titleFont == null)
                {
                    titleFont = SystemFont(new[] { "Segoe UI Black", "Arial Black", "Impact", "Trebuchet MS" });
                }

                return titleFont;
            }
        }

        private static Font SystemFont(string[] names)
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                try
                {
                    var found = Font.CreateDynamicFontFromOSFont(names, 32);
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch (System.Exception)
                {
                    // No system fonts here: the built-in one below does the job.
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Sprite disc;

        /// <summary>A white circle, tinted per use. Drawn as it is, not sliced, so it can be any size.</summary>
        public static Sprite Disc
        {
            get
            {
                if (disc == null)
                {
                    disc = MakeRounded(128, 64);
                    disc = Sprite.Create(disc.texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
                }

                return disc;
            }
        }

        /// <summary>A circle of this diameter.</summary>
        public static Image Round(Transform parent, string name, Color color, float diameter, Vector2 anchor, Vector2 position)
        {
            var rect = Rect(parent, name, new Vector2(diameter, diameter), anchor, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Disc;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite MakeRounded(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius), 0f);
                    float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            float border = radius + 2f;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image Fill(Transform parent, string name, Color color, Vector2 size, Vector2 anchor, Vector2 position)
        {
            var rect = Rect(parent, name, size, anchor, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A card: a dark outline with the colored body inside it. Returns the body.</summary>
        public static RectTransform Card(Transform parent, string name, Color color, Vector2 size, Vector2 anchor, Vector2 position)
        {
            var outline = Fill(parent, name, Ink, size, anchor, position);
            var body = Fill(outline.transform, "Body", color, size - new Vector2(Outline * 2f, Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
            return (RectTransform)body.transform;
        }

        public static Text Label(Transform parent, string text, int fontSize, Color color, Vector2 size, Vector2 anchor, Vector2 position, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Rect(parent, "Label_" + text, size, anchor, position);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = Loc.T(text);
            if (!string.IsNullOrEmpty(text))
            {
                // Texts that code fills in later start empty and are left alone.
                rect.gameObject.AddComponent<LocalizedText>().Bind(text);
            }

            return label;
        }

        /// <summary>A chunky button. The label is centered; <paramref name="onClick"/> runs when it is pressed.</summary>
        public static Button MakeButton(Transform parent, string text, Color color, Vector2 size, Vector2 anchor, Vector2 position, Action onClick)
        {
            var outline = Fill(parent, "Button_" + text, Ink, size, anchor, position);
            outline.raycastTarget = true;
            var body = Fill(outline.transform, "Body", color, size - new Vector2(Outline * 2f, Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
            body.raycastTarget = false;

            var button = outline.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            outline.gameObject.AddComponent<ButtonHover>().Bind(outline);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var label = Label(body.transform, text, 30, Ink, size - new Vector2(40f, 20f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            label.name = "Text";
            if (onClick != null)
            {
                button.onClick.AddListener(() =>
                {
                    AudioDirector.Click();
                    onClick();
                });
            }

            return button;
        }

        /// <summary>Changes the text of a button made by <see cref="MakeButton"/>.</summary>
        /// <summary>A one-line box to type in, with a grey hint while it is empty.</summary>
        public static InputField TextBox(Transform parent, string name, string hintText, Vector2 size, Vector2 anchor, Vector2 position, int limit)
        {
            var outline = Fill(parent, name, Ink, size, anchor, position);
            outline.raycastTarget = true;
            var body = Fill(outline.transform, "Body", Color.white, size - new Vector2(Outline * 2f, Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
            body.raycastTarget = false;

            var inner = size - new Vector2(50f, 16f);
            var text = Label(body.transform, string.Empty, 32, Ink, inner, new Vector2(0.5f, 0.5f), Vector2.zero);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.fontStyle = FontStyle.Normal;
            var hint = Label(body.transform, hintText, 28, new Color(0.5f, 0.52f, 0.6f, 1f), inner, new Vector2(0.5f, 0.5f), Vector2.zero);
            hint.fontStyle = FontStyle.Italic;

            var input = outline.gameObject.AddComponent<InputField>();
            input.targetGraphic = outline;
            input.textComponent = text;
            input.placeholder = hint;
            input.characterLimit = limit;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        public static void SetLabel(Button button, string text)
        {
            button.transform.Find("Body/Text").GetComponent<Text>().text = text;
        }

        /// <summary>
        /// A full-screen layer that dims what is behind it and catches every click. The window goes inside it.
        /// </summary>
        public static GameObject Overlay(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = Dim;
            return go;
        }

        /// <summary>
        /// The name of the portrait file of a character: lowercase, letters and digits, the rest as underscores
        /// ("Don Rafa" is "don_rafa"). The pregenerated portraits go in Resources/Portraits with these names.
        /// </summary>
        public static string PortraitKey(string name)
        {
            var key = new System.Text.StringBuilder();
            foreach (char c in (name ?? string.Empty).ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                key.Append(char.IsLetterOrDigit(c) && c < 128 ? c : '_');
            }

            return key.ToString().Trim('_');
        }

        /// <summary>
        /// A character's portrait: the pregenerated picture when Resources/Portraits has one, and otherwise a
        /// face in a color taken from the name, with eyes and a mouth. Returns the mouth, or null for a picture.
        /// </summary>
        public static RectTransform Portrait(RectTransform parent, string name, float size, Vector2 anchor, Vector2 position)
        {
            var face = Card(parent, "Portrait", Color.white, new Vector2(size, size), anchor, position);
            var picture = Resources.Load<Sprite>("Portraits/" + PortraitKey(name)) ?? RemoteImages.Portrait(name);
            if (picture != null)
            {
                var image = Fill(face, "Picture", Color.white, new Vector2(size - Outline * 2f, size - Outline * 2f), new Vector2(0.5f, 0.5f), Vector2.zero);
                image.sprite = picture;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                return null;
            }

            int hash = 0;
            foreach (char c in name ?? string.Empty)
            {
                hash = hash * 31 + c;
            }

            float hue = (hash & 0x7fffffff) % 360 / 360f;
            face.GetComponent<Image>().color = Color.HSVToRGB(hue, 0.35f, 0.98f);
            float k = size / 112f;
            Fill(face, "EyeL", Ink, new Vector2(12f, 18f) * k, new Vector2(0.5f, 0.5f), new Vector2(-20f, 14f) * k);
            Fill(face, "EyeR", Ink, new Vector2(12f, 18f) * k, new Vector2(0.5f, 0.5f), new Vector2(20f, 14f) * k);
            var lips = Fill(face, "Mouth", Ink, new Vector2(34f, 6f) * k, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f) * k);
            return lips.rectTransform;
        }

        /// <summary>The height a text needs when wrapped to <paramref name="width"/>.</summary>
        public static float MeasureHeight(Text label, float width)
        {
            var settings = label.GetGenerationSettings(new Vector2(width, 0f));
            return label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
        }
    }
}

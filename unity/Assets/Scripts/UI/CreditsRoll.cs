using System;
using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The credits, over the living art of the menu: the team stays on the left and everything else goes by on the right like
    /// the end of a film. It rolls by itself and waits a moment when the player scrolls or drags. Every line is a fact about
    /// the project; see <see cref="MenuEntries"/>.
    /// </summary>
    public sealed class CreditsRoll : MonoBehaviour
    {
        private const float PanelWidth = 690f;
        private const float PanelBleed = 12f;
        private const float RollMargin = 30f;
        private const float MaxColumn = 1120f;
        private const float RollSpeed = 46f;
        private const float StartDelay = 1.6f;
        private const float PauseAfterTouch = 4f;
        private const float EndHold = 5f;
        private const float SectionGap = 44f;

        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        private static readonly Color Cream = GamePalette.Hex("#F8F1DC");
        private static readonly Color Beak = GamePalette.Hex("#F2A23A");
        private static readonly Color Night = GamePalette.Hex("#17201E");

        private readonly List<KeyValuePair<Image, string>> waitingPictures = new List<KeyValuePair<Image, string>>();

        private RectTransform viewport;
        private RectTransform content;
        private ScrollRect scroll;
        private float builtWidth;
        private float resumeAt;
        private float nextPictureCheck;
        private float atTheEndSince = -1f;

        /// <summary>Builds the window, closed.</summary>
        public static GameObject Build(Transform parent, Action close)
        {
            var overlay = UiKit.Overlay(parent, "Credits");
            overlay.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.07f, 0.72f);
            BuildPanel(overlay.transform, close);

            var roll = overlay.AddComponent<CreditsRoll>();
            roll.BuildViewport(overlay.transform);
            overlay.SetActive(false);
            return overlay;
        }

        /// <summary>A side bar from edge to edge, so nothing of the menu shows beside it.</summary>
        private static void BuildPanel(Transform parent, Action close)
        {
            var card = UiKit.Card(parent, "Card", UiKit.Paper, new Vector2(PanelWidth, 1104f), MiddleLeft, new Vector2(-PanelBleed, 0f));
            UiKit.Header(card, "Créditos", GamePalette.Hex("#C0612B"));

            float y = -UiKit.HeaderHeight - 34f;
            var game = UiKit.PlainLabel(card, "Game", "Colón Quest", 76, UiKit.Ink, new Vector2(620f, 92f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            game.font = UiKit.TitleFont;
            y -= 92f;
            var motto = UiKit.Label(card, "Aprendé a manejar tu plata", 28, GameUiColors.Muted, new Vector2(620f, 40f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            motto.fontStyle = FontStyle.Normal;
            y -= 66f;

            UiKit.Label(card, "Desarrollado por", 26, GameUiColors.Muted, new Vector2(620f, 34f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            y -= 34f;
            var team = UiKit.PlainLabel(card, "Team", MenuEntries.Team, 58, UiKit.Ink, new Vector2(620f, 72f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            team.font = UiKit.TitleFont;
            y -= 88f;

            for (int i = 0; i < MenuEntries.Makers.Length; i++)
            {
                string maker = MenuEntries.Makers[i];
                var person = UiKit.Card(card, "Maker", Color.white, new Vector2(600f, 116f), Top, new Vector2(0f, y));
                var badge = UiKit.Round(person, "Badge", i == 0 ? Beak : GameUiColors.Blue, 84f, MiddleLeft, new Vector2(16f, 0f));
                UiKit.PlainLabel(badge.transform, "Initials", Initials(maker), 34, UiKit.Ink, new Vector2(84f, 84f), Middle, Vector2.zero, TextAnchor.MiddleCenter);
                UiKit.PlainLabel(person, "Name", maker, 38, UiKit.Ink, new Vector2(470f, 60f), MiddleLeft, new Vector2(116f, 0f));
                y -= 128f;
            }

            y -= 18f;
            UiKit.Label(card, "Hecho en Costa Rica", 34, GameUiColors.Gain, new Vector2(620f, 44f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            y -= 48f;
            var hackathon = UiKit.Label(card, "Para el Meta Global AI Developer Hackathon 2026", 23, UiKit.Ink, new Vector2(620f, 34f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            hackathon.fontStyle = FontStyle.Normal;
            y -= 36f;
            UiKit.PlainLabel(card, "Version", BuildInfo.Label, 20, GameUiColors.Muted, new Vector2(620f, 28f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);

            UiKit.MakeButton(card, "Volver", GameUiColors.Yellow, new Vector2(340f, 76f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), () => close());
        }

        private static string Initials(string name)
        {
            var parts = name.Split(' ');
            return parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1);
        }

        private void BuildViewport(Transform parent)
        {
            viewport = UiKit.Rect(parent, "Roll", Vector2.zero, Middle, Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = Middle;
            viewport.offsetMin = new Vector2(PanelWidth - PanelBleed + RollMargin, 0f);
            viewport.offsetMax = new Vector2(-RollMargin, 0f);

            // Invisible, but it has to be there for the wheel and the drag to reach the roll.
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UiKit.Rect(viewport, "Content", new Vector2(100f, 10f), Top, Vector2.zero);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 50f;
        }

        /// <summary>The window's real width is only known once it opens, so the roll is laid out here.</summary>
        private void OnEnable()
        {
            if (viewport == null)
            {
                return;
            }

            float width = Mathf.Min(MaxColumn, viewport.rect.width - 40f);
            if (!Mathf.Approximately(width, builtWidth))
            {
                builtWidth = width;
                Lay(width);
            }

            content.anchoredPosition = Vector2.zero;
            resumeAt = Time.unscaledTime + StartDelay;
            atTheEndSince = -1f;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (Input.mouseScrollDelta.y != 0f || Input.GetMouseButton(0))
            {
                resumeAt = now + PauseAfterTouch;
                atTheEndSince = -1f;
            }

            ShowArrivedPictures(now);
            if (now < resumeAt)
            {
                return;
            }

            float end = Mathf.Max(0f, content.sizeDelta.y - viewport.rect.height);
            float y = content.anchoredPosition.y;
            if (y < end)
            {
                content.anchoredPosition = new Vector2(0f, Mathf.Min(end, y + RollSpeed * Time.unscaledDeltaTime));
                return;
            }

            // The closing words stay a moment, and then the film starts over.
            if (atTheEndSince < 0f)
            {
                atTheEndSince = now;
            }
            else if (now - atTheEndSince > EndHold)
            {
                content.anchoredPosition = Vector2.zero;
                resumeAt = now + StartDelay;
                atTheEndSince = -1f;
            }
        }

        private void ShowArrivedPictures(float now)
        {
            if (waitingPictures.Count == 0 || now < nextPictureCheck)
            {
                return;
            }

            nextPictureCheck = now + 1f;
            for (int i = waitingPictures.Count - 1; i >= 0; i--)
            {
                var sprite = RemoteImages.Fetch(waitingPictures[i].Value);
                if (sprite != null)
                {
                    waitingPictures[i].Key.sprite = sprite;
                    waitingPictures[i].Key.color = Color.white;
                    waitingPictures.RemoveAt(i);
                }
            }
        }

        // ---------------------------------------------------------------- the roll

        private void Lay(float width)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            waitingPictures.Clear();
            float y = -46f;
            y = Models(width, y);
            y = AgentCards(width, y);
            y = Art(width, y);
            y = FiguresRow(width, y);
            y = TwoColumns("La tecnología", MenuEntries.Stack, width, y);
            y = TwoColumns("Las tipografías", MenuEntries.Fonts, width, y);
            y = Sound(width, y);
            y = Closing(width, y);
            content.sizeDelta = new Vector2(width, -y + 60f);
        }

        private float Heading(string spanish, float width, float y)
        {
            var label = UiKit.Label(content, spanish, 42, Beak, new Vector2(width, 56f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Night;
            outline.effectDistance = new Vector2(2f, -2f);
            UiKit.Fill(content, "Rule", Beak, new Vector2(160f, 5f), Top, new Vector2(0f, y - 58f));
            return y - 78f;
        }

        private float Models(float width, float y)
        {
            y = Heading("Los modelos de Meta", width, y);
            for (int i = 0; i < MenuEntries.Tools.Length; i++)
            {
                var tool = MenuEntries.Tools[i];
                var card = UiKit.Card(content, "Tool", UiKit.Paper, new Vector2(width, 128f), Top, new Vector2(0f, y));
                UiKit.Round(card, "Dot", GameUiColors.Cycle(i), 40f, MiddleLeft, new Vector2(20f, 0f));
                float textWidth = width - 100f;
                UiKit.PlainLabel(card, "Name", tool[0], 32, GameUiColors.Gain, new Vector2(textWidth, 40f), MiddleLeft, new Vector2(78f, 36f));
                var job = UiKit.Label(card, tool[1], 24, UiKit.Ink, new Vector2(textWidth, 34f), MiddleLeft, new Vector2(78f, 0f));
                job.fontStyle = FontStyle.Normal;
                var time = UiKit.Label(card, tool[2], 21, GameUiColors.Muted, new Vector2(textWidth, 30f), MiddleLeft, new Vector2(78f, -36f));
                time.fontStyle = FontStyle.Normal;
                y -= 138f;
            }

            return y - SectionGap;
        }

        private float AgentCards(float width, float y)
        {
            y = Heading("Los agentes", width, y);
            float each = (width - 24f) / MenuEntries.Agents.Length;
            for (int i = 0; i < MenuEntries.Agents.Length; i++)
            {
                var agent = MenuEntries.Agents[i];
                var card = UiKit.Card(content, "Agent", UiKit.Paper, new Vector2(each, 196f), new Vector2(0f, 1f), new Vector2(i * (each + 12f), y));
                UiKit.Label(card, agent[0], 27, GameUiColors.Gain, new Vector2(each - 40f, 42f), Top, new Vector2(0f, -12f), TextAnchor.MiddleCenter);
                var what = UiKit.Label(card, agent[1], 22, UiKit.Ink, new Vector2(each - 40f, 122f), Top, new Vector2(0f, -56f), TextAnchor.UpperCenter);
                what.fontStyle = FontStyle.Normal;
            }

            return y - 196f - SectionGap;
        }

        private float Art(float width, float y)
        {
            y = Heading("El arte", width, y);
            int count = MenuEntries.CreditPortraits.Count;
            float gap = 12f;
            float size = Mathf.Min(170f, (width - gap * (count - 1)) / count);
            float x = (width - (size * count + gap * (count - 1))) / 2f;
            for (int i = 0; i < count; i++)
            {
                var frame = UiKit.Fill(content, "Art", UiKit.Ink, new Vector2(size, size), new Vector2(0f, 1f), new Vector2(x + i * (size + gap), y));
                var picture = UiKit.Fill(frame.transform, "Picture", new Color(0.3f, 0.34f, 0.33f, 1f), new Vector2(size - UiKit.Outline, size - UiKit.Outline), Middle, Vector2.zero);
                picture.type = Image.Type.Simple;
                picture.sprite = null;
                picture.preserveAspect = true;
                waitingPictures.Add(new KeyValuePair<Image, string>(picture, MenuEntries.CreditPortraits[i]));
            }

            y -= size + 14f;
            var line = UiKit.Label(content, "Generado con Muse Image y un estilo fijo, sin texto ni números: las cifras las dibuja el juego.", 24, Cream, new Vector2(width, 66f), Top, new Vector2(0f, y), TextAnchor.UpperCenter);
            line.fontStyle = FontStyle.Normal;
            var sprites = UiKit.Label(content, "Los personajes, los carros y el pueblo se dibujan con código.", 24, Cream, new Vector2(width, 36f), Top, new Vector2(0f, y - 66f), TextAnchor.UpperCenter);
            sprites.fontStyle = FontStyle.Normal;
            return y - 102f - SectionGap;
        }

        private float FiguresRow(float width, float y)
        {
            y = Heading("En números", width, y);
            int count = MenuEntries.Figures.Length;
            float gap = 12f;
            float each = (width - gap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                var figure = MenuEntries.Figures[i];
                var card = UiKit.Card(content, "Figure", UiKit.Paper, new Vector2(each, 146f), new Vector2(0f, 1f), new Vector2(i * (each + gap), y));
                var number = UiKit.PlainLabel(card, "Number", figure[0], 50, UiKit.Ink, new Vector2(each - 24f, 66f), Top, new Vector2(0f, -10f), TextAnchor.MiddleCenter);
                number.font = UiKit.TitleFont;
                UiKit.Label(card, figure[1], 20, GameUiColors.Muted, new Vector2(each - 24f, 52f), Top, new Vector2(0f, -78f), TextAnchor.UpperCenter);
            }

            return y - 146f - SectionGap;
        }

        private float TwoColumns(string spanish, string[][] rows, float width, float y)
        {
            y = Heading(spanish, width, y);
            foreach (var row in rows)
            {
                var name = UiKit.PlainLabel(content, "Name", row[0], 28, Cream, new Vector2(width * 0.42f, 42f), new Vector2(0f, 1f), new Vector2(0f, y), TextAnchor.MiddleRight);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                var what = UiKit.Label(content, row[1], 23, Cream, new Vector2(width * 0.55f, 42f), new Vector2(1f, 1f), new Vector2(0f, y), TextAnchor.MiddleLeft);
                what.fontStyle = FontStyle.Normal;
                y -= 46f;
            }

            return y - SectionGap;
        }

        private float Sound(float width, float y)
        {
            y = Heading("El sonido", width, y);
            var line = UiKit.Label(content, "Cada sonido del juego lo genera el propio código: no hay un solo archivo de audio.", 24, Cream, new Vector2(width, 40f), Top, new Vector2(0f, y), TextAnchor.UpperCenter);
            line.fontStyle = FontStyle.Normal;
            return y - 44f - SectionGap;
        }

        private float Closing(float width, float y)
        {
            var thanks = UiKit.Label(content, "Gracias por jugar", 60, Cream, new Vector2(width, 84f), Top, new Vector2(0f, y), TextAnchor.MiddleCenter);
            thanks.font = UiKit.TitleFont;
            var outline = thanks.gameObject.AddComponent<Outline>();
            outline.effectColor = Night;
            outline.effectDistance = new Vector2(3f, -3f);
            var last = UiKit.Label(content, "Ojalá la próxima vez que un vendedor te sonría, sepás qué preguntarle.", 26, Beak, new Vector2(width, 44f), Top, new Vector2(0f, y - 92f), TextAnchor.MiddleCenter);
            last.fontStyle = FontStyle.Normal;
            return y - 140f;
        }
    }
}

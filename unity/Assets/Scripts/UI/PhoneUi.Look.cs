using System;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// How the phone looks and moves: a dark body with its side buttons, a wallpaper with the toucan of the menu, a status bar, a
    /// big clock, app icons drawn with simple shapes and a colored header for each app. It rises when it opens, drops when it
    /// closes, the icons pop in one after the other and each app comes in with a small zoom.
    /// </summary>
    public sealed partial class PhoneUi
    {
        private const float RiseSeconds = 0.34f;
        private const float DropSeconds = 0.16f;
        private const float AppSeconds = 0.18f;
        private const float IconSize = 108f;

        /// <summary>The wallpaper is the picture of the menu at the height of the screen, moved so the toucan is in the middle.</summary>
        private const float WallpaperWidth = ScreenHeight * 16f / 9f;
        private const float WallpaperShift = -0.34f * WallpaperWidth;

        private static readonly Color BodyColor = GamePalette.Hex("#191C25");
        private static readonly Color Rim = GamePalette.Hex("#3C4250");
        private static readonly Color Glass = new Color(1f, 1f, 1f, 0.86f);
        private static readonly Color SheetColor = GamePalette.Hex("#F3F4F8");
        private static readonly Color BarInk = GamePalette.Hex("#20242E");
        private static readonly Color SinpeColor = GamePalette.Hex("#2F80ED");
        private static readonly Color BankColor = GamePalette.Hex("#1E9E6A");
        private static readonly Color BillsColor = GamePalette.Hex("#0E8FA8");
        private static readonly Color SoundColor = GamePalette.Hex("#566072");
        private static readonly Color SearchColor = GamePalette.Hex("#E8912D");
        private static readonly Color CalculatorColor = GamePalette.Hex("#D9477A");
        private static readonly Color BlocksColor = GamePalette.Hex("#7A5AF5");
        private static readonly Color CasinoColor = GamePalette.Hex("#C9303C");

        private PhoneMotion motion;
        private RectTransform frame;
        private Image dim;
        private CanvasGroup contentFade;
        private GameObject wallpaper;
        private Text statusTime;
        private Image[] statusMarks;
        private Text bigClock;
        private Text dayLine;
        private bool closing;
        private float frameScale = 1f;

        private void BuildBody(RectTransform rootRect)
        {
            motion = root.AddComponent<PhoneMotion>();
            dim = root.AddComponent<Image>();
            dim.color = UiKit.Dim;

            var rim = UiKit.Fill(rootRect, "Frame", Rim, new Vector2(PhoneWidth, PhoneHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            rim.pixelsPerUnitMultiplier = 0.34f;
            frame = rim.rectTransform;

            // The side buttons stick out of the rim, behind the body.
            UiKit.Fill(frame, "Button_Power", Rim, new Vector2(10f, 110f), new Vector2(1f, 1f), new Vector2(6f, -230f));
            UiKit.Fill(frame, "Button_VolumeUp", Rim, new Vector2(10f, 70f), new Vector2(0f, 1f), new Vector2(-6f, -200f));
            UiKit.Fill(frame, "Button_VolumeDown", Rim, new Vector2(10f, 70f), new Vector2(0f, 1f), new Vector2(-6f, -285f));

            var body = UiKit.Fill(frame, "Body", BodyColor, new Vector2(PhoneWidth - 8f, PhoneHeight - 8f), new Vector2(0.5f, 0.5f), Vector2.zero);
            body.pixelsPerUnitMultiplier = 0.38f;

            var screen = UiKit.Fill(body.transform, "Screen", SheetColor, new Vector2(ScreenWidth, ScreenHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            screen.pixelsPerUnitMultiplier = 0.5f;
            screen.gameObject.AddComponent<Mask>();

            BuildWallpaper(screen.transform);
            content = UiKit.Rect(screen.transform, "Content", new Vector2(ScreenWidth, ScreenHeight - StatusHeight - BottomHeight), new Vector2(0f, 1f), new Vector2(0f, -StatusHeight));
            contentFade = content.gameObject.AddComponent<CanvasGroup>();
            BuildStatusBar(screen.transform);

            // The camera in the middle of the status bar.
            var island = UiKit.Fill(screen.transform, "Island", Color.black, new Vector2(120f, 30f), new Vector2(0.5f, 1f), new Vector2(0f, -12f));
            UiKit.Round(island.transform, "Lens", GamePalette.Hex("#26304A"), 14f, new Vector2(1f, 0.5f), new Vector2(-10f, 0f));
        }

        private void BuildWallpaper(Transform screen)
        {
            var rect = UiKit.Rect(screen, "Wallpaper", new Vector2(ScreenWidth, ScreenHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            wallpaper = rect.gameObject;

            var picture = UiKit.Rect(rect, "Picture", new Vector2(WallpaperWidth, ScreenHeight), new Vector2(0.5f, 0.5f), new Vector2(WallpaperShift, 0f));
            var image = picture.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Menu/KeyArt");
            image.color = image.sprite != null ? Color.white : GamePalette.Hex("#35506B");
            image.raycastTarget = false;

            // Darker behind the clock and the icons, so they read over any part of the picture.
            var veil = UiKit.Rect(rect, "Veil", new Vector2(ScreenWidth, ScreenHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            var shade = veil.gameObject.AddComponent<Image>();
            shade.color = new Color(0.05f, 0.08f, 0.13f, 0.34f);
            shade.raycastTarget = false;
        }

        private void BuildStatusBar(Transform screen)
        {
            statusTime = UiKit.Label(screen, string.Empty, 24, Color.white, new Vector2(120f, 36f), new Vector2(0f, 1f), new Vector2(30f, -10f));

            // The signal: four bars that grow; then the battery, with its tip.
            var marks = new System.Collections.Generic.List<Image>();
            var topRight = new Vector2(1f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float height = 7f + i * 4f;
                marks.Add(UiKit.Fill(screen, "Signal", Color.white, new Vector2(6f, height), topRight, new Vector2(-94f + i * 9f, -34f + height)));
            }

            var battery = UiKit.Fill(screen, "Battery", Color.white, new Vector2(38f, 18f), topRight, new Vector2(-30f, -16f));
            marks.Add(battery);
            marks.Add(UiKit.Fill(screen, "BatteryTip", Color.white, new Vector2(4f, 8f), topRight, new Vector2(-25f, -21f)));
            statusMarks = marks.ToArray();
        }

        private void PaintStatusBar(bool onWallpaper)
        {
            var color = onWallpaper ? Color.white : BarInk;
            statusTime.color = color;
            foreach (var mark in statusMarks)
            {
                mark.color = color;
            }
        }

        private void RefreshClock()
        {
            string time = session.Loop.Clock.Time;
            statusTime.text = time;
            if (bigClock != null)
            {
                bigClock.text = time;
                dayLine.text = Loc.F("Semana {0}", session.Loop.Clock.Day) + "  ·  " + Loc.F("Mes {0}  ·  {1}/{2} tareas", Mathf.Min(session.Loop.MonthsPlayed + 1, GameRules.MonthsToPlay), session.Loop.TasksDone, GameRules.TasksPerMonth);
            }
        }

        // ---------------------------------------------------------------- moving

        /// <summary>The phone rises from below the screen and lands with a small bounce. It always fits a short window.</summary>
        private void RiseIn()
        {
            closing = false;
            motion.Clear();
            float height = ((RectTransform)root.transform).rect.height;
            frameScale = height > 0f ? Mathf.Min(1f, (height - 30f) / (PhoneHeight + 14f)) : 1f;
            frame.localScale = new Vector3(frameScale, frameScale, 1f);

            float below = -(height * 0.5f + PhoneHeight * frameScale * 0.5f + 20f);
            motion.Run(0f, RiseSeconds, t => frame.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(below, 0f, PhoneMotion.Overshoot(t))));
            motion.Run(0f, RiseSeconds * 0.7f, t => dim.color = new Color(UiKit.Dim.r, UiKit.Dim.g, UiKit.Dim.b, UiKit.Dim.a * t));
        }

        private void DropOut()
        {
            closing = true;
            motion.Clear();
            float height = ((RectTransform)root.transform).rect.height;
            float below = -(height * 0.5f + PhoneHeight * frameScale * 0.5f + 20f);
            float from = frame.anchoredPosition.y;
            motion.Run(0f, DropSeconds, t =>
            {
                frame.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, below, t * t));
                dim.color = new Color(UiKit.Dim.r, UiKit.Dim.g, UiKit.Dim.b, UiKit.Dim.a * (1f - t));
            }, () =>
            {
                closing = false;
                root.SetActive(false);
            });
        }

        /// <summary>An app comes in with a little zoom and a fade.</summary>
        private void ZoomContentIn()
        {
            motion.Run(0f, AppSeconds, t =>
            {
                float eased = PhoneMotion.Settle(t);
                contentFade.alpha = eased;
                float scale = Mathf.Lerp(0.94f, 1f, eased);
                content.localScale = new Vector3(scale, scale, 1f);
            });
        }

        private void PopIn(RectTransform target, float delay)
        {
            motion.Run(delay, 0.26f, t =>
            {
                if (target != null)
                {
                    float scale = Mathf.Max(0f, PhoneMotion.Overshoot(t));
                    target.localScale = new Vector3(scale, scale, 1f);
                }
            });
        }

        // ---------------------------------------------------------------- home

        private void BuildHome()
        {
            bigClock = UiKit.Label(content, string.Empty, 92, Color.white, new Vector2(InnerWidth, 110f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.MiddleCenter);
            bigClock.font = UiKit.TitleFont;
            bigClock.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);
            dayLine = UiKit.Label(content, string.Empty, 24, Color.white, new Vector2(InnerWidth, 34f), new Vector2(0.5f, 1f), new Vector2(0f, -122f), TextAnchor.MiddleCenter);
            dayLine.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            RefreshClock();

            var notice = UiKit.Fill(content, "Notice", Glass, new Vector2(InnerWidth, 118f), new Vector2(0f, 1f), new Vector2(Margin, -170f));
            bool billsDue = session.Loop.Bills.Due.Count > 0;
            var mark = UiKit.Fill(notice.transform, "NoticeIcon", billsDue ? BillsColor : SinpeColor, new Vector2(62f, 62f), new Vector2(0f, 0.5f), new Vector2(14f, 0f));
            DrawIcon(billsDue ? App.Bills : App.Sinpe, mark.rectTransform, 62f);
            var noticeText = UiKit.Label(notice.transform, HomeNotice(), 21, UiKit.Ink, new Vector2(InnerWidth - 104f, 104f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f));
            noticeText.fontStyle = FontStyle.Normal;
            motion.Run(0.26f, 0.3f, t =>
            {
                if (notice != null)
                {
                    float eased = PhoneMotion.Overshoot(t);
                    notice.rectTransform.anchoredPosition = new Vector2(Margin, Mathf.LerpUnclamped(-20f, -170f, eased));
                    notice.color = new Color(Glass.r, Glass.g, Glass.b, Glass.a * Mathf.Clamp01(t * 2f));
                    noticeText.color = new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, Mathf.Clamp01(t * 2f));
                }
            });

            var tiles = new[]
            {
                (App.Sinpe, "SINPE", SinpeColor),
                (App.Bank, "Banco", BankColor),
                (App.Bills, "Recibos", BillsColor),
                (App.Goals, "Metas", GoalsColor),
                (App.Search, "Buscador", SearchColor),
                (App.Calculator, "Cuotas", CalculatorColor),
                (App.Blocks, "Bloques", BlocksColor),
                (App.Casino, "Casino", CasinoColor)
            };

            float column = (InnerWidth - IconSize) / 2f;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                var at = TileAt(i, column);
                var box = Tile(tile.Item2, tile.Item3, at, 0.1f + i * 0.035f, tile.Item1, () => Show(tile.Item1));
                if (tile.Item1 == App.Bills && billsDue)
                {
                    Badge(box, session.Loop.Bills.Due.Count);
                }
            }

            string sound = AudioDirector.Muted ? "Sonido: no" : "Sonido: sí";
            Tile(sound, SoundColor, TileAt(tiles.Length, column), 0.1f + tiles.Length * 0.035f, null, () =>
            {
                AudioDirector.Muted = !AudioDirector.Muted;
                Show(App.Home);
            });
        }

        private static Vector2 TileAt(int index, float column)
        {
            return new Vector2(Margin + (index % 3) * column, -300f - (index / 3) * 152f);
        }

        /// <summary>The red circle on an icon with how many things wait there.</summary>
        private static void Badge(RectTransform box, int count)
        {
            var badge = UiKit.Round(box, "Badge", GamePalette.Hex("#E5383B"), 38f, new Vector2(1f, 1f), new Vector2(8f, 8f));
            UiKit.Label(badge.transform, count.ToString(), 22, Color.white, new Vector2(38f, 38f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
        }

        /// <summary>An icon of the home screen. Without an app it is a switch, drawn as a speaker.</summary>
        private RectTransform Tile(string name, Color color, Vector2 position, float delay, App? target, Action click)
        {
            var box = UiKit.Rect(content, "Tile_" + name, new Vector2(IconSize, IconSize + 40f), new Vector2(0f, 1f), position);
            var shadow = UiKit.Fill(box, "Shadow", new Color(0f, 0f, 0f, 0.28f), new Vector2(IconSize, IconSize), new Vector2(0.5f, 1f), new Vector2(0f, -5f));
            shadow.pixelsPerUnitMultiplier = 0.85f;
            var icon = UiKit.Fill(box, "Icon", color, new Vector2(IconSize, IconSize), new Vector2(0.5f, 1f), Vector2.zero);
            icon.pixelsPerUnitMultiplier = 0.85f;
            icon.raycastTarget = true;
            if (target.HasValue)
            {
                DrawIcon(target.Value, icon.rectTransform, IconSize);
            }
            else
            {
                DrawSpeaker(icon.rectTransform, AudioDirector.Muted);
            }

            // The top half is a little lighter, as if the light came from above.
            var sheen = UiKit.Fill(icon.transform, "Sheen", new Color(1f, 1f, 1f, 0.1f), new Vector2(IconSize, IconSize * 0.5f), new Vector2(0.5f, 1f), Vector2.zero);
            sheen.pixelsPerUnitMultiplier = 0.85f;

            var label = UiKit.Label(box, name, 21, Color.white, new Vector2(IconSize + 30f, 30f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);

            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            var colors = button.colors;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                AudioDirector.Click();
                click();
            });
            icon.gameObject.AddComponent<ButtonHover>();
            PopIn(box, delay);
            return box;
        }

        // ---------------------------------------------------------------- icons

        /// <summary>The picture of an app, drawn in white with plain shapes, so it needs no font and no image file.</summary>
        private static void DrawIcon(App target, RectTransform icon, float size)
        {
            float u = size / 108f;
            switch (target)
            {
                case App.Sinpe:
                    Arrow(icon, new Vector2(0f, 15f) * u, 1f, u);
                    Arrow(icon, new Vector2(0f, -15f) * u, -1f, u);
                    break;

                case App.Bank:
                    Tilted(icon, new Vector2(0f, 20f) * u, new Vector2(46f, 46f) * u, 45f, Color.white);
                    Bar(icon, new Vector2(0f, 0f) * u, new Vector2(70f, 10f) * u, 0f, Color.white);
                    foreach (float x in new[] { -24f, 0f, 24f })
                    {
                        Bar(icon, new Vector2(x, -17f) * u, new Vector2(10f, 26f) * u, 0f, Color.white);
                    }

                    Bar(icon, new Vector2(0f, -34f) * u, new Vector2(74f, 10f) * u, 0f, Color.white);
                    break;

                case App.Goals:
                    Bar(icon, new Vector2(-24f, 2f) * u, new Vector2(7f, 62f) * u, 0f, Color.white);
                    Bar(icon, new Vector2(4f, 18f) * u, new Vector2(50f, 30f) * u, 0f, Color.white);
                    break;

                case App.Bills:
                    Bar(icon, Vector2.zero, new Vector2(56f, 74f) * u, 0f, Color.white);
                    Bar(icon, new Vector2(0f, 20f) * u, new Vector2(36f, 8f) * u, 0f, BillsColor);
                    Bar(icon, new Vector2(0f, 4f) * u, new Vector2(36f, 8f) * u, 0f, BillsColor);
                    Bar(icon, new Vector2(-6f, -12f) * u, new Vector2(24f, 8f) * u, 0f, BillsColor);
                    UiKit.Round(icon, "Paid", BillsColor, 14f * u, new Vector2(0.5f, 0.5f), new Vector2(14f, -24f) * u);
                    break;

                case App.Search:
                    UiKit.Round(icon, "Lens", Color.white, 54f * u, new Vector2(0.5f, 0.5f), new Vector2(-8f, 9f) * u);
                    UiKit.Round(icon, "Glass", SearchColor, 32f * u, new Vector2(0.5f, 0.5f), new Vector2(-8f, 9f) * u);
                    Bar(icon, new Vector2(21f, -20f) * u, new Vector2(34f, 11f) * u, -45f, Color.white);
                    break;

                case App.Calculator:
                    Bar(icon, Vector2.zero, new Vector2(58f, 74f) * u, 0f, Color.white);
                    Bar(icon, new Vector2(0f, 21f) * u, new Vector2(44f, 16f) * u, 0f, CalculatorColor);
                    for (int row = 0; row < 2; row++)
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            UiKit.Round(icon, "Key", CalculatorColor, 11f * u, new Vector2(0.5f, 0.5f), new Vector2(-16f + col * 16f, -6f - row * 17f) * u);
                        }
                    }

                    break;

                case App.Blocks:
                    foreach (var cell in new[] { new Vector2(-22f, 11f), new Vector2(0f, 11f), new Vector2(22f, 11f), new Vector2(0f, -11f) })
                    {
                        Bar(icon, cell * u, new Vector2(20f, 20f) * u, 0f, Color.white);
                    }

                    Bar(icon, new Vector2(-22f, -11f) * u, new Vector2(20f, 20f) * u, 0f, new Color(1f, 1f, 1f, 0.45f));
                    Bar(icon, new Vector2(22f, -11f) * u, new Vector2(20f, 20f) * u, 0f, new Color(1f, 1f, 1f, 0.45f));
                    break;

                default:
                    UiKit.Round(icon, "Coin", Color.white, 66f * u, new Vector2(0.5f, 0.5f), Vector2.zero);
                    UiKit.Round(icon, "CoinInner", CasinoColor, 50f * u, new Vector2(0.5f, 0.5f), Vector2.zero);
                    UiKit.Round(icon, "CoinCenter", Color.white, 22f * u, new Vector2(0.5f, 0.5f), Vector2.zero);
                    break;
            }
        }

        private static void DrawSpeaker(RectTransform icon, bool muted)
        {
            Bar(icon, new Vector2(-22f, 0f), new Vector2(18f, 26f), 0f, Color.white);
            Tilted(icon, new Vector2(-6f, 0f), new Vector2(30f, 30f), 45f, Color.white);
            if (muted)
            {
                Bar(icon, new Vector2(20f, 0f), new Vector2(38f, 8f), 45f, Color.white);
                Bar(icon, new Vector2(20f, 0f), new Vector2(38f, 8f), -45f, Color.white);
            }
            else
            {
                Bar(icon, new Vector2(16f, 0f), new Vector2(7f, 22f), 0f, Color.white);
                Bar(icon, new Vector2(28f, 0f), new Vector2(7f, 38f), 0f, Color.white);
            }
        }

        private static void Arrow(RectTransform icon, Vector2 at, float direction, float u)
        {
            Bar(icon, at + new Vector2(-6f * direction, 0f) * u, new Vector2(44f, 10f) * u, 0f, Color.white);
            Bar(icon, at + new Vector2(17f * direction, 7f) * u, new Vector2(22f, 10f) * u, -45f * direction, Color.white);
            Bar(icon, at + new Vector2(17f * direction, -7f) * u, new Vector2(22f, 10f) * u, 45f * direction, Color.white);
        }

        private static void Bar(RectTransform icon, Vector2 at, Vector2 size, float angle, Color color)
        {
            Tilted(icon, at, size, angle, color, 3f);
        }

        private static void Tilted(RectTransform icon, Vector2 at, Vector2 size, float angle, Color color, float roundness = 6f)
        {
            var bar = UiKit.Fill(icon, "Shape", color, size, new Vector2(0.5f, 0.5f), at);
            bar.pixelsPerUnitMultiplier = roundness;
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // ---------------------------------------------------------------- apps

        private static Color ColorOf(App target)
        {
            switch (target)
            {
                case App.Sinpe: return SinpeColor;
                case App.Bank: return BankColor;
                case App.Bills: return BillsColor;
                case App.Goals: return GoalsColor;
                case App.Search: return SearchColor;
                case App.Calculator: return CalculatorColor;
                case App.Blocks: return BlocksColor;
                default: return CasinoColor;
            }
        }

        /// <summary>The header of an app: a band with its color and its name.</summary>
        private Text Heading(string text)
        {
            var band = UiKit.Fill(content, "Header", ColorOf(app), new Vector2(InnerWidth, 52f), new Vector2(0f, 1f), new Vector2(Margin, 0f));
            band.pixelsPerUnitMultiplier = 1.6f;
            return UiKit.Label(band.transform, text, 30, Color.white, new Vector2(InnerWidth - 32f, 46f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft);
        }

        /// <summary>A button of the phone: flat, with a thin edge a little darker than its color instead of the thick game outline.</summary>
        private Button AddButton(string text, Color color, Vector2 size, Vector2 position, Action click)
        {
            var button = UiKit.MakeButton(content, text, color, size, new Vector2(0f, 1f), new Vector2(Margin + position.x, position.y), click);
            var edge = button.GetComponent<Image>();
            edge.color = Color.Lerp(new Color(color.r, color.g, color.b, 1f), UiKit.Ink, 0.28f);
            button.GetComponent<ButtonHover>().Bind(edge);
            var body = (RectTransform)button.transform.Find("Body");
            body.sizeDelta = size - new Vector2(4f, 4f);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = size.y > 60f ? 26 : 22;
            label.rectTransform.sizeDelta = size - new Vector2(16f, 8f);
            return button;
        }
    }
}

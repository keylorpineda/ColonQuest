using System.Collections;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The screen between the menu and the town (and back). It is the picture of the main menu, still alive: the volcano
    /// smokes and the toucan looks around on its branch, with the name of the game where the menu has it. Under them a tip in
    /// a paper card and a chunky bar with a colón coin that spins as it rolls. It keeps the screen for a minimum time so it
    /// never flashes, and it hides the change of scene.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private const float MinimumSeconds = 1.6f;
        private const float FadeSeconds = 0.3f;

        // The layout, in units of a screen 1080 high: everything stays left of the toucan.
        private const float ArtWidth = 1920f;
        private const float ArtHeight = 1080f;
        private const float ArtMargin = 1.15f;
        private const float ColumnX = 84f;
        private const float BlockWidth = 1200f;
        private const float BarHeight = 40f;
        private const float BarY = 86f;
        private const float CoinSize = 78f;
        private const float SheenWidth = 110f;

        private static readonly Color Cream = GamePalette.Hex("#F8F1DC");
        private static readonly Color Beak = GamePalette.Hex("#F2A23A");

        private static readonly string[] Tips =
        {
            "Antes de comprar a cuotas, pregunta cuánto pagas en total.",
            "Pagar solo el mínimo de la tarjeta es la forma más cara de deber.",
            "Un fondo de emergencia te evita pedir plata a quien te cobra de más.",
            "Si te prometen triplicar tu plata en un mes, desconfía."
        };

        private static LoadingScreen active;

        private CanvasGroup group;
        private RectTransform art;
        private RectTransform title;
        private RectTransform subtitle;
        private RectTransform barFill;
        private RectTransform sheen;
        private RectTransform coin;
        private Text waiting;
        private Texture2D shadeTexture;
        private float shown;

        public static bool IsLoading => active != null;

        /// <summary>Shows the loading screen and loads the scene behind it.</summary>
        public static void Load(string sceneName)
        {
            if (active != null)
            {
                return;
            }

            var go = new GameObject("LoadingScreen");
            DontDestroyOnLoad(go);
            active = go.AddComponent<LoadingScreen>();
            active.StartCoroutine(active.Run(sceneName));
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var back = UiKit.Overlay(transform, "Back");
            back.GetComponent<Image>().color = GamePalette.Hex("#17201E");
            back.GetComponent<Image>().raycastTarget = true;

            BuildBackdrop();
            BuildShade();
            BuildTitle();
            BuildTip();
            BuildBar();
            BuildFooter();
        }

        /// <summary>The same picture as the menu, so going from one to the other feels like one place.</summary>
        private void BuildBackdrop()
        {
            art = UiKit.Rect(transform, "KeyArt", new Vector2(ArtWidth, ArtHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            art.localScale = new Vector3(ArtMargin, ArtMargin, 1f);
            var image = art.gameObject.AddComponent<Image>();
            image.raycastTarget = false;

            image.sprite = Resources.Load<Sprite>("Menu/KeyArt_Plate");
            if (image.sprite == null)
            {
                image.sprite = Resources.Load<Sprite>("Menu/KeyArt");
                return;
            }

            MenuVolcanoSmoke.Create(art, ArtWidth);
            var toucan = MenuToucan.Create(art, ArtWidth, ArtHeight);
            if (toucan != null)
            {
                toucan.LandNow();
            }
        }

        /// <summary>The foot of the picture sinks into shadow, so the card and the bar sit on something quiet.</summary>
        private void BuildShade()
        {
            const int Height = 64;
            shadeTexture = new Texture2D(1, Height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var ink = GamePalette.Hex("#17201E");
            for (int y = 0; y < Height; y++)
            {
                float fade = 1f - y / (Height - 1f);
                shadeTexture.SetPixel(0, y, new Color(ink.r, ink.g, ink.b, 0.86f * fade * fade));
            }

            shadeTexture.Apply();
            var rect = UiKit.Rect(transform, "Shade", new Vector2(0f, 560f), new Vector2(0.5f, 0f), Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            var shade = rect.gameObject.AddComponent<RawImage>();
            shade.texture = shadeTexture;
            shade.raycastTarget = false;
        }

        private void BuildTitle()
        {
            var topLeft = new Vector2(0f, 1f);
            var name = UiKit.Label(transform, "Colón Quest", 104, Cream, new Vector2(800f, 130f), topLeft, new Vector2(ColumnX, -70f));
            name.font = UiKit.TitleFont;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ink(name, 5f);
            var shadow = name.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UiKit.Ink;
            shadow.effectDistance = new Vector2(7f, -9f);
            title = name.rectTransform;

            var tagline = UiKit.Label(transform, "Aprendé a manejar tu plata", 36, Cream, new Vector2(700f, 50f), topLeft, new Vector2(ColumnX + 4f, -204f));
            Ink(tagline, 3f);
            subtitle = tagline.rectTransform;
        }

        private void BuildTip()
        {
            var card = UiKit.Card(transform, "TipCard", UiKit.Paper, new Vector2(BlockWidth, 112f), Vector2.zero, new Vector2(ColumnX, 192f));
            var left = new Vector2(0f, 0.5f);
            var badge = UiKit.Card(card, "TipBadge", Beak, new Vector2(170f, 58f), left, new Vector2(18f, 0f));
            UiKit.Label(badge, "Consejo", 28, UiKit.Ink, new Vector2(150f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            UiKit.Label(card, Tips[Random.Range(0, Tips.Length)], 30, UiKit.Ink, new Vector2(BlockWidth - 240f, 84f), left, new Vector2(208f, 0f));
        }

        private void BuildBar()
        {
            waiting = UiKit.Label(transform, string.Empty, 34, Cream, new Vector2(500f, 50f), Vector2.zero, new Vector2(ColumnX + 4f, BarY + BarHeight + 12f));
            Ink(waiting, 3f);

            var outline = UiKit.Fill(transform, "BarOutline", UiKit.Ink, new Vector2(BlockWidth, BarHeight + 12f), Vector2.zero, new Vector2(ColumnX, BarY));
            var trough = UiKit.Fill(outline.transform, "BarBack", GamePalette.Hex("#3A4238"), new Vector2(BlockWidth - 12f, BarHeight), new Vector2(0.5f, 0.5f), Vector2.zero);
            barFill = UiKit.Fill(trough.transform, "BarFill", Beak, new Vector2(0f, BarHeight), new Vector2(0f, 0.5f), Vector2.zero).rectTransform;
            barFill.gameObject.AddComponent<RectMask2D>();
            UiKit.Fill(barFill, "BarLight", GamePalette.Hex("#FFC766"), new Vector2(BlockWidth, 10f), new Vector2(0f, 1f), new Vector2(0f, -5f));
            sheen = UiKit.Fill(barFill, "BarSheen", new Color(1f, 1f, 1f, 0.32f), new Vector2(SheenWidth, BarHeight), new Vector2(0f, 0.5f), Vector2.zero).rectTransform;

            coin = UiKit.Rect(outline.transform, "Coin", Vector2.one * CoinSize, new Vector2(0f, 0.5f), Vector2.zero);
            coin.pivot = new Vector2(0.5f, 0.5f);
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Round(coin, "CoinInk", UiKit.Ink, CoinSize, center, Vector2.zero);
            UiKit.Round(coin, "CoinEdge", GamePalette.Hex("#C98A1E"), CoinSize - 12f, center, Vector2.zero);
            UiKit.Round(coin, "CoinFace", GamePalette.Hex("#FFD23F"), CoinSize - 24f, center, Vector2.zero);
            UiKit.PlainLabel(coin, "CoinMark", "₡", 38, GamePalette.Hex("#8A5A10"), Vector2.one * CoinSize, center, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void BuildFooter()
        {
            var version = UiKit.Label(transform, BuildInfo.Label, 24, Cream, new Vector2(500f, 36f), Vector2.zero, new Vector2(ColumnX + 4f, 28f));
            version.name = "Version";
            Ink(version, 2f);
            var credit = UiKit.Label(transform, BuildInfo.Credit, 24, Cream, new Vector2(680f, 36f), Vector2.zero, new Vector2(ColumnX + BlockWidth - 680f, 28f), TextAnchor.MiddleRight);
            credit.name = "Credit";
            Ink(credit, 2f);
        }

        private static void Ink(Text label, float distance)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private IEnumerator Run(string sceneName)
        {
            Build();
            Time.timeScale = 1f;

            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }

            group.alpha = 1f;
            var load = SceneManager.LoadSceneAsync(sceneName);
            load.allowSceneActivation = false;

            float elapsed = 0f;
            while (load.progress < 0.9f || elapsed < MinimumSeconds || shown < 0.99f)
            {
                elapsed += Time.unscaledDeltaTime;
                Animate(elapsed, Mathf.Min(Mathf.Clamp01(load.progress / 0.9f), Mathf.Clamp01(elapsed / MinimumSeconds)));
                yield return null;
            }

            load.allowSceneActivation = true;
            while (!load.isDone)
            {
                yield return null;
            }

            // The new scene has started: let it settle one moment, then show it.
            yield return null;
            yield return null;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }

            active = null;
            Destroy(gameObject);
        }

        private void Animate(float elapsed, float progress)
        {
            // The bar follows the real progress smoothly, so a jump of the loader never reads as a jump of the bar.
            shown = Mathf.MoveTowards(Mathf.Lerp(shown, progress, 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime)), progress, 0.15f * Time.unscaledDeltaTime);
            float trough = BlockWidth - 12f;
            float width = trough * shown;
            barFill.sizeDelta = new Vector2(width, BarHeight);
            sheen.anchoredPosition = new Vector2(Mathf.Repeat(elapsed * 620f, trough + SheenWidth * 2f) - SheenWidth, 0f);

            // The coin rolls at the head of the bar: it spins on its edge and bobs a little.
            coin.anchoredPosition = new Vector2(6f + width, Mathf.Sin(elapsed * 5f) * 3f);
            coin.localScale = new Vector3(Mathf.Max(0.18f, Mathf.Abs(Mathf.Cos(elapsed * 3.4f))), 1f, 1f);

            waiting.text = Loc.T("Cargando") + new string('.', 1 + (int)(elapsed * 2.5f) % 3);

            // The name of the game arrives with the picture and then keeps still, as it does in the menu.
            float arrive = 1f - Mathf.Clamp01(elapsed / 0.45f);
            float offset = -36f * arrive * arrive;
            title.anchoredPosition = new Vector2(ColumnX + offset, -70f);
            subtitle.anchoredPosition = new Vector2(ColumnX + 4f + offset, -204f);
        }

        private void Update()
        {
            // The picture always covers the window, whatever its shape, with the same crop as the menu.
            if (art != null)
            {
                var window = ((RectTransform)transform).rect;
                float scale = Mathf.Max(window.width / ArtWidth, window.height / ArtHeight) * ArtMargin;
                art.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            if (shadeTexture != null)
            {
                Destroy(shadeTexture);
            }
        }
    }
}

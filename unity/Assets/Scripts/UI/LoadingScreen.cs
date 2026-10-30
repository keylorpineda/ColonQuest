using System.Collections;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The screen between the menu and the town (and back): the buttons and everything else are put aside, the name of the game
    /// grows in the middle, the version and the credit stay in the corners and a bar and a word tell the player it is loading.
    /// It keeps the screen for a minimum time so it never flashes, and it hides the change of scene.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private const float MinimumSeconds = 1.6f;
        private const float FadeSeconds = 0.3f;

        private static readonly string[] Tips =
        {
            "Antes de comprar a cuotas, pregunta cuánto pagas en total.",
            "Pagar solo el mínimo de la tarjeta es la forma más cara de deber.",
            "Un fondo de emergencia te evita pedir plata a quien te cobra de más.",
            "Si te prometen triplicar tu plata en un mes, desconfía."
        };

        private static LoadingScreen active;

        private CanvasGroup group;
        private RectTransform title;
        private RectTransform barFill;
        private Text waiting;
        private float barWidth;

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

            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);

            // Deep blue, lighter toward the middle, like the sky of the title at dusk.
            var back = UiKit.Overlay(transform, "Back");
            back.GetComponent<Image>().color = GamePalette.Hex("#12203F");
            back.GetComponent<Image>().raycastTarget = true;
            UiKit.Fill(transform, "Glow", new Color(0.35f, 0.55f, 0.9f, 0.22f), new Vector2(1500f, 760f), center, new Vector2(0f, 40f));
            UiKit.Fill(transform, "GlowCore", new Color(0.55f, 0.75f, 1f, 0.18f), new Vector2(900f, 420f), center, new Vector2(0f, 40f));

            var cream = GamePalette.Hex("#F6F0D4");
            var name = UiKit.Label(transform, "Colón Quest", 210, cream, new Vector2(1500f, 260f), center, new Vector2(0f, 90f), TextAnchor.MiddleCenter);
            name.font = UiKit.TitleFont;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = name.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(10f, -10f);
            title = name.rectTransform;

            var tagline = UiKit.Label(transform, "Aprendé a manejar tu plata", 48, new Color(cream.r, cream.g, cream.b, 0.9f), new Vector2(1200f, 60f), center, new Vector2(0f, -60f), TextAnchor.MiddleCenter);
            tagline.font = UiKit.TitleFont;

            // The bar and the word that says it is loading.
            barWidth = 640f;
            UiKit.Fill(transform, "BarOutline", UiKit.Ink, new Vector2(barWidth + 12f, 38f), bottom, new Vector2(0f, 184f));
            UiKit.Fill(transform, "BarBack", new Color(0.18f, 0.26f, 0.45f, 1f), new Vector2(barWidth, 26f), bottom, new Vector2(0f, 190f));
            barFill = UiKit.Fill(transform, "BarFill", GameUiColors.Yellow, new Vector2(barWidth, 26f), bottom, new Vector2(-barWidth * 0.5f, 190f)).rectTransform;
            barFill.pivot = new Vector2(0f, 0.5f);
            barFill.anchorMin = barFill.anchorMax = bottom;
            barFill.anchoredPosition = new Vector2(-barWidth * 0.5f, 203f);
            barFill.sizeDelta = new Vector2(0f, 26f);
            waiting = UiKit.Label(transform, string.Empty, 40, cream, new Vector2(800f, 56f), bottom, new Vector2(0f, 232f), TextAnchor.MiddleCenter);

            var tip = UiKit.Label(transform, Tips[Random.Range(0, Tips.Length)], 30, new Color(cream.r, cream.g, cream.b, 0.75f), new Vector2(1500f, 50f), bottom, new Vector2(0f, 120f), TextAnchor.MiddleCenter);
            tip.fontStyle = FontStyle.Italic;

            var version = UiKit.Label(transform, BuildInfo.Label, 28, new Color(cream.r, cream.g, cream.b, 0.8f), new Vector2(500f, 40f), new Vector2(0f, 0f), new Vector2(36f, 28f), TextAnchor.MiddleLeft);
            version.name = "Version";
            var credit = UiKit.Label(transform, BuildInfo.Credit, 24, new Color(cream.r, cream.g, cream.b, 0.65f), new Vector2(600f, 40f), new Vector2(1f, 0f), new Vector2(-36f, 28f), TextAnchor.MiddleRight);
            credit.name = "Credit";
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
            while (load.progress < 0.9f || elapsed < MinimumSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                Animate(elapsed, Mathf.Min(Mathf.Clamp01(load.progress / 0.9f), Mathf.Clamp01(elapsed / MinimumSeconds)));
                yield return null;
            }

            Animate(elapsed, 1f);
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
            barFill.sizeDelta = new Vector2(barWidth * progress, 26f);
            waiting.text = Loc.T("Cargando") + new string('.', 1 + (int)(elapsed * 2.5f) % 3);

            // The name of the game swells and settles while it waits.
            float scale = 1f + 0.035f * Mathf.Sin(elapsed * 2.2f) + Mathf.Max(0f, 0.12f * (1f - elapsed * 1.5f));
            title.localScale = new Vector3(scale, scale, 1f);
        }
    }
}

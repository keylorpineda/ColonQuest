using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The map, and a HUD that does not get in the way: smaller and translucent, it fades further when the player walks under
    /// it, and H hides it all for a clean view of the town.
    /// </summary>
    public sealed partial class GameUi
    {
        private const float HudScale = 0.82f;
        private const float HudAlpha = 0.84f;
        private const float HudAlphaOverPlayer = 0.28f;
        private const float HudFadePerSecond = 3.5f;

        private TownMap townMap;
        private CanvasGroup hudGroup;
        private RectTransform hudFrame;
        private Text hudHiddenHint;
        private bool hudHidden;
        private float hudAlpha = HudAlpha;
        private float mapAlpha = HudAlpha;

        /// <summary>True when the player has hidden the HUD with H.</summary>
        public bool HudHidden => hudHidden;

        private void BuildMapAndHud(RectTransform hudCard)
        {
            hudFrame = (RectTransform)hudCard.parent;
            hudFrame.localScale = new Vector3(HudScale, HudScale, 1f);
            hudGroup = hudFrame.gameObject.AddComponent<CanvasGroup>();
            hudGroup.blocksRaycasts = false;

            hudHiddenHint = UiKit.Label(canvas.transform, "[H] Mostrar", 22, Color.white, new Vector2(240f, 30f), new Vector2(0f, 1f), new Vector2(24f, -20f));
            var outline = hudHiddenHint.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            hudHiddenHint.gameObject.SetActive(false);

            townMap = new TownMap(canvas.transform);
        }

        /// <summary>The keys of the map and the HUD, and how much each one shows. Runs every frame, windows or not.</summary>
        private void TickMapAndHud()
        {
            bool windows = modal.IsOpen || phone.IsOpen || pause.IsOpen || negotiationPanel.IsOpen || talkPanel.IsOpen;
            if (Input.GetKeyDown(KeyCode.H) && !windows && !townMap.IsOpen && !openingPlaying)
            {
                hudHidden = !hudHidden;
            }

            hudFrame.gameObject.SetActive(!hudHidden);
            hudHiddenHint.gameObject.SetActive(hudHidden && !windows && !openingPlaying && !LoadingScreen.IsLoading);

            var hero = GameObject.FindWithTag(Station.PlayerTag);
            float step = HudFadePerSecond * Time.unscaledDeltaTime;
            hudAlpha = Mathf.MoveTowards(hudAlpha, IsOver(hero, hudFrame) ? HudAlphaOverPlayer : HudAlpha, step);
            mapAlpha = Mathf.MoveTowards(mapAlpha, IsOver(hero, townMap.MiniRect) ? HudAlphaOverPlayer : HudAlpha, step);
            hudGroup.alpha = hudAlpha;
            townMap.Tick(!hudHidden && !openingPlaying && !LoadingScreen.IsLoading, mapAlpha);
        }

        /// <summary>True when the player is drawn under this piece of the screen.</summary>
        private static bool IsOver(GameObject hero, RectTransform piece)
        {
            var camera = Camera.main;
            if (hero == null || camera == null || !piece.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3 point = camera.WorldToScreenPoint(hero.transform.position + Vector3.up * 0.8f);
            return RectTransformUtility.RectangleContainsScreenPoint(piece, point, null);
        }
    }
}

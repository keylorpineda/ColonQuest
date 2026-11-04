using System.Collections.Generic;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The minimap in the corner of the town and the full map on M. Both show one real picture of the town, taken the first
    /// time the user walks out: it looks exactly like the simulator and costs nothing per frame. Over it go the places, the
    /// user and, when the simulator wants the user somewhere, where to go.
    /// </summary>
    public sealed class TownMap
    {
        private const float MiniSize = 300f;
        private const float MiniUnits = 42f;
        private const int PicturePixelsPerUnit = 13;
        private const float FullHeight = 880f;
        private const float MiniDot = 14f;
        private const float FullDot = 22f;

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomLeft = Vector2.zero;
        private static readonly Color You = GamePalette.Hex("#FFD23F");
        private static readonly Color Goal = GamePalette.Hex("#FF4D6D");

        private static RenderTexture picture;
        private static Vector2 lastTownPosition;
        private static bool knowsTown;

        private readonly List<Marker> miniMarkers = new List<Marker>();
        private readonly List<Marker> fullMarkers = new List<Marker>();

        private readonly RectTransform miniRect;
        private readonly RawImage miniImage;
        private readonly RectTransform miniYou;
        private readonly RectTransform miniGoal;
        private readonly CanvasGroup miniGroup;
        private readonly GameObject mini;
        private readonly Text hint;
        private readonly GameObject full;
        private readonly RectTransform fullArea;
        private readonly RawImage fullImage;
        private readonly RectTransform fullYou;
        private readonly RectTransform fullGoal;
        private readonly Text fullNote;

        private Transform hero;
        private bool inTown;
        private bool pendingPicture;

        private sealed class Marker
        {
            public RectTransform Rect;
            public Vector2 World;
        }

        public TownMap(Transform canvas)
        {
            var frame = UiKit.Card(canvas, "Minimap", UiKit.Ink, new Vector2(MiniSize, MiniSize), Vector2.one, new Vector2(-24f, -24f));
            mini = frame.parent.gameObject;
            miniGroup = mini.AddComponent<CanvasGroup>();
            miniGroup.blocksRaycasts = false;
            miniRect = (RectTransform)mini.transform;

            var view = UiKit.Rect(frame, "View", frame.sizeDelta, Center, Vector2.zero);
            view.gameObject.AddComponent<RectMask2D>();
            miniImage = view.gameObject.AddComponent<RawImage>();
            miniImage.raycastTarget = false;
            miniGoal = Dot(view, "Goal", Goal, MiniDot + 6f);
            miniYou = Dot(view, "You", You, MiniDot + 4f);

            // On a strip of its own at the foot of the minimap, so it can be read over any part of the town.
            var strip = UiKit.Fill(frame, "HintStrip", new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.78f), new Vector2(frame.sizeDelta.x, 34f), new Vector2(0.5f, 0f), Vector2.zero);
            strip.type = Image.Type.Simple;
            hint = UiKit.Label(strip.transform, "[M] Mapa   [H] Ocultar", 21, Color.white, new Vector2(MiniSize - 24f, 30f), Center, Vector2.zero, TextAnchor.MiddleCenter);

            full = UiKit.Overlay(canvas, "TownMap");
            full.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.07f, 0.9f);
            var bounds = MapLayout.TownBounds;
            float width = FullHeight * bounds.width / bounds.height;
            fullArea = UiKit.Rect(full.transform, "Area", new Vector2(width, FullHeight), new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f));
            fullImage = fullArea.gameObject.AddComponent<RawImage>();
            fullImage.raycastTarget = false;
            fullGoal = Dot(fullArea, "Goal", Goal, FullDot + 10f);
            fullYou = Dot(fullArea, "You", You, FullDot + 6f);
            fullNote = UiKit.PlainLabel(fullArea, "Note", string.Empty, 34, Color.white, new Vector2(width - 120f, 120f), Center, Vector2.zero, TextAnchor.MiddleCenter);
            BuildLegend(width);

            full.SetActive(false);
            mini.SetActive(false);
        }

        public bool IsOpen => full.activeSelf;

        /// <summary>Where the simulator wants the user to go, or null. Set every frame by whoever knows it.</summary>
        public Vector2? GoalPosition { get; set; }

        /// <summary>The corner of the screen the minimap covers, for fading it when the user walks under it.</summary>
        public RectTransform MiniRect => miniRect;

        public void SceneLoaded(Scene scene)
        {
            inTown = scene.name == SceneNames.Town;
            hero = null;
            pendingPicture = inTown;
        }

        public void Open()
        {
            full.transform.SetAsLastSibling();
            full.SetActive(true);
            RefreshFull();
            fullImage.enabled = picture != null;
            if (!knowsTown)
            {
                fullNote.text = Loc.T("Salí al pueblo y el mapa se dibuja solo.");
            }
            else
            {
                fullNote.text = inTown ? string.Empty : Loc.T("Estás adentro. Esto es donde entraste.");
            }

            fullNote.rectTransform.anchoredPosition = knowsTown && !inTown ? new Vector2(0f, FullHeight * 0.5f - 60f) : Vector2.zero;
        }

        public void Close()
        {
            full.SetActive(false);
        }

        /// <summary>Takes the picture when the town is ready, and follows the user on the minimap.</summary>
        public void Tick(bool showMini, float alpha)
        {
            if (pendingPicture)
            {
                pendingPicture = false;
                EnsurePicture();
                FindPlaces();
            }

            bool show = showMini && inTown && picture != null;
            mini.SetActive(show);
            if (inTown)
            {
                FollowHero();
            }

            if (IsOpen)
            {
                RefreshFull();
            }

            if (!show)
            {
                return;
            }

            miniGroup.alpha = alpha;
            var view = TownPlaces.View(lastTownPosition, MapLayout.TownBounds, MiniUnits);
            miniImage.uvRect = view;
            Refresh((RectTransform)miniImage.transform, miniYou, miniGoal, miniMarkers, view);
        }

        private void RefreshFull()
        {
            Refresh(fullArea, fullYou, fullGoal, fullMarkers, new Rect(0f, 0f, 1f, 1f));
        }

        private void FollowHero()
        {
            if (hero == null)
            {
                var found = GameObject.FindWithTag(Station.PlayerTag);
                hero = found != null ? found.transform : null;
            }

            if (hero != null)
            {
                lastTownPosition = hero.position;
                knowsTown = true;
            }
        }

        private void Refresh(RectTransform area, RectTransform you, RectTransform goal, List<Marker> markers, Rect view)
        {
            var bounds = MapLayout.TownBounds;
            var size = area.rect.size;
            foreach (var marker in markers)
            {
                marker.Rect.anchoredPosition = OnArea(marker.World, bounds, view, size);
            }

            you.gameObject.SetActive(knowsTown);
            you.anchoredPosition = OnArea(lastTownPosition, bounds, view, size);
            float pulse = 1f + 0.16f * Mathf.Sin(Time.unscaledTime * 5f);
            you.localScale = new Vector3(pulse, pulse, 1f);

            bool hasGoal = GoalPosition.HasValue && inTown;
            goal.gameObject.SetActive(hasGoal);
            if (hasGoal)
            {
                goal.anchoredPosition = EdgeOf(OnArea(GoalPosition.Value, bounds, view, size), size, goal.sizeDelta.x * 0.6f);
            }
        }

        /// <summary>A place of the world on an area showing <paramref name="view"/> of the picture; the origin of the area is its corner.</summary>
        private static Vector2 OnArea(Vector2 world, Rect bounds, Rect view, Vector2 size)
        {
            var map = TownPlaces.ToMap(world, bounds);
            return new Vector2((map.x - view.xMin) / view.width * size.x, (map.y - view.yMin) / view.height * size.y);
        }

        /// <summary>A goal off the minimap stays on its edge, on the side to walk to.</summary>
        private static Vector2 EdgeOf(Vector2 position, Vector2 size, float margin)
        {
            return new Vector2(Mathf.Clamp(position.x, margin, size.x - margin), Mathf.Clamp(position.y, margin, size.y - margin));
        }

        private static RectTransform Dot(RectTransform parent, string name, Color color, float size)
        {
            var ring = UiKit.Round(parent, name, UiKit.Ink, size + 5f, BottomLeft, Vector2.zero);
            ring.rectTransform.pivot = Center;
            UiKit.Round(ring.transform, "Fill", color, size, Center, Vector2.zero).rectTransform.pivot = Center;
            return ring.rectTransform;
        }

        private void BuildLegend(float mapWidth)
        {
            var card = UiKit.Card(full.transform, "Legend", UiKit.Paper, new Vector2(350f, FullHeight), new Vector2(0.5f, 0.5f), new Vector2(mapWidth * 0.5f - 190f + 195f, 0f));
            var top = new Vector2(0.5f, 1f);
            var header = UiKit.Header(card, "Mapa", GamePalette.Hex("#2E7D9A"));
            header.fontSize = 46;

            float y = -UiKit.HeaderHeight - 26f;
            var row = UiKit.Rect(card, "You", new Vector2(310f, 40f), top, new Vector2(0f, y));
            UiKit.Round(row, "Dot", You, 26f, new Vector2(0f, 0.5f), Vector2.zero);
            UiKit.Label(row, "Vos", 26, UiKit.Ink, new Vector2(260f, 40f), new Vector2(0f, 0.5f), new Vector2(40f, 0f));
            y -= 48f;
            row = UiKit.Rect(card, "Goal", new Vector2(310f, 40f), top, new Vector2(0f, y));
            UiKit.Round(row, "Dot", Goal, 26f, new Vector2(0f, 0.5f), Vector2.zero);
            UiKit.Label(row, "A dónde ir", 26, UiKit.Ink, new Vector2(260f, 40f), new Vector2(0f, 0.5f), new Vector2(40f, 0f));
            y -= 62f;
            for (int i = 0; i <= (int)PlaceKind.Danger; i++)
            {
                var kind = (PlaceKind)i;
                row = UiKit.Rect(card, "Kind", new Vector2(310f, 40f), top, new Vector2(0f, y));
                UiKit.Round(row, "Dot", ColorOf(kind), 26f, new Vector2(0f, 0.5f), Vector2.zero);
                UiKit.Label(row, TownPlaces.KindName(kind), 26, UiKit.Ink, new Vector2(260f, 40f), new Vector2(0f, 0.5f), new Vector2(40f, 0f));
                y -= 48f;
            }

            UiKit.MakeButton(card, "Cerrar  [M]", GameUiColors.Yellow, new Vector2(290f, 76f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), Close);
        }

        private static Color ColorOf(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Home: return GamePalette.Hex("#4DA3FF");
                case PlaceKind.Work: return GamePalette.Hex("#FF9F1C");
                case PlaceKind.Money: return GamePalette.Hex("#3FCB7A");
                case PlaceKind.Shop: return GamePalette.Hex("#C77DFF");
                case PlaceKind.Service: return Color.white;
                default: return GamePalette.Hex("#E5383B");
            }
        }

        private void FindPlaces()
        {
            if (miniMarkers.Count > 0)
            {
                return;
            }

            foreach (var place in TownPlaces.All)
            {
                var found = GameObject.Find(place.ObjectName);
                if (found == null)
                {
                    continue;
                }

                var color = ColorOf(place.Kind);
                miniMarkers.Add(new Marker { Rect = Dot((RectTransform)miniImage.transform, "Place", color, MiniDot), World = found.transform.position });
                var dot = Dot(fullArea, "Place_" + place.ObjectName, color, FullDot);
                var label = UiKit.Label(dot, place.Name, 21, Color.white, new Vector2(240f, 28f), Center, new Vector2(0f, place.NameAbove ? 28f : -28f), TextAnchor.MiddleCenter);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = UiKit.Ink;
                outline.effectDistance = new Vector2(2f, -2f);
                fullMarkers.Add(new Marker { Rect = dot, World = found.transform.position });
            }

            // The user and the goal go over the places.
            miniGoal.SetAsLastSibling();
            miniYou.SetAsLastSibling();
            mini.GetComponentInChildren<RectMask2D>().transform.SetAsFirstSibling();
            fullGoal.SetAsLastSibling();
            fullYou.SetAsLastSibling();
            fullNote.transform.SetAsLastSibling();
        }

        /// <summary>One render of the whole town, without the user or the tint of the hour.</summary>
        private void EnsurePicture()
        {
            if (picture != null && picture.IsCreated())
            {
                Show();
                return;
            }

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }

            var bounds = MapLayout.TownBounds;
            picture = new RenderTexture(Mathf.RoundToInt(bounds.width * PicturePixelsPerUnit), Mathf.RoundToInt(bounds.height * PicturePixelsPerUnit), 16)
            {
                filterMode = FilterMode.Bilinear,
                name = "TownMapPicture"
            };

            var hidden = new List<Renderer>();
            var player = GameObject.FindWithTag(Station.PlayerTag);
            if (player != null)
            {
                hidden.AddRange(player.GetComponentsInChildren<Renderer>());
            }

            var tint = GameObject.Find("DayTint");
            if (tint != null)
            {
                hidden.AddRange(tint.GetComponentsInChildren<Renderer>());
            }

            var wereOn = new List<Renderer>();
            foreach (var renderer in hidden)
            {
                if (renderer.enabled)
                {
                    wereOn.Add(renderer);
                    renderer.enabled = false;
                }
            }

            var go = new GameObject("TownMapCamera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = bounds.height * 0.5f;
            camera.aspect = bounds.width / bounds.height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GamePalette.Hex("#3E7F4B");
            camera.targetTexture = picture;
            go.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            camera.Render();
            camera.targetTexture = null;
            Object.Destroy(go);

            foreach (var renderer in wereOn)
            {
                renderer.enabled = true;
            }

            Show();
        }

        private void Show()
        {
            miniImage.texture = picture;
            fullImage.texture = picture;
        }
    }
}

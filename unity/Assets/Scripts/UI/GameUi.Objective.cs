using System.Collections;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The current objective on the screen, in a normal run and in the guided one: a card at the top that says what to do, a
    /// mark that bounces over the place, an arrow on the edge of the screen when the place is out of sight, and the dot on the
    /// map. Tab hides the card and H hides everything, with the rest of the HUD.
    /// </summary>
    public sealed partial class GameUi
    {
        private const float MarkHeight = 1.5f;
        private const float MarkBounce = 0.22f;
        private const float EdgeMargin = 70f;

        private static readonly Color MarkColor = GamePalette.Hex("#FF4D6D");

        // The arrow beside the user of the guided run: far from the place, it says which way to walk without looking up.
        private const float GuideArrowDistance = 1.35f;
        private const float GuideArrowHidesWithin = 2.6f;

        private GameObject guideControls;
        private Transform guideArrow;
        private Transform objectiveMark;
        private RectTransform edgeArrow;
        private string objectiveShown;
        private float objectivePulse;

        private void BuildObjectiveMarks()
        {
            var ring = UiKit.Round((RectTransform)canvas.transform, "EdgeArrow", UiKit.Ink, 66f, new Vector2(0.5f, 0.5f), Vector2.zero);
            edgeArrow = ring.rectTransform;
            edgeArrow.pivot = new Vector2(0.5f, 0.5f);
            UiKit.Round(edgeArrow, "Fill", MarkColor, 56f, new Vector2(0.5f, 0.5f), Vector2.zero).rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var arrow = UiKit.Rect(edgeArrow, "Arrow", new Vector2(34f, 34f), new Vector2(0.5f, 0.5f), Vector2.zero);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            var image = arrow.gameObject.AddComponent<Image>();
            image.sprite = ArrowSprite();
            image.color = Color.white;
            image.raycastTarget = false;
            ring.raycastTarget = false;
            edgeArrow.gameObject.SetActive(false);
        }

        /// <summary>The object an objective points at from this scene, when it is here.</summary>
        private GameObject FindObjectiveTarget(Objective objective, string scene)
        {
            string name = Objectives.PointAt(objective, scene);
            if (name == null)
            {
                return null;
            }

            if (name != Objectives.WayOut)
            {
                return GameObject.Find(name);
            }

            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (door.TargetScene == SceneNames.Town)
                {
                    return door.gameObject;
                }
            }

            return null;
        }

        /// <summary>Runs every frame: the guided run says its own objective; otherwise it comes from the state of the run.</summary>
        private void TickWorkGuide()
        {
            townMap.GoalPosition = null;
            string scene = SceneManager.GetActiveScene().name;
            var objective = session.Guided ? guidedObjective : Objectives.ForGame(session.Loop, scene);
            bool free = !modal.IsOpen && !phone.IsOpen && !pause.IsOpen && !negotiationPanel.IsOpen && !talkPanel.IsOpen && !openingPlaying && !LoadingScreen.IsLoading;
            bool show = objective != null && free && !hudHidden && session.Loop.JailDaysRemaining == 0;
            workGuide.SetActive(show);
            GameObject target = objective != null && session.Loop.JailDaysRemaining == 0 ? FindObjectiveTarget(objective, scene) : null;
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            bool pointing = target != null && hero != null;

            // The map still knows the goal while a window covers it.
            if (pointing && scene == SceneNames.Town)
            {
                townMap.GoalPosition = target.transform.position;
            }

            TickObjectiveMark(show && pointing, pointing ? target.transform.position : Vector3.zero);
            TickEdgeArrow(show && pointing && !session.Guided, pointing ? target.transform.position : Vector3.zero);
            TickGuideArrow(show && pointing && session.Guided, hero, pointing ? target.transform.position : Vector3.zero);
            guideControls.SetActive(session.Guided);
            if (!show)
            {
                return;
            }

            string text = session.Guided
                ? Loc.F("Mes {0} de {1}", Mathf.Min(session.Loop.MonthsPlayed + 1, GameRules.MonthsToPlay), GameRules.MonthsToPlay) + "  ·  " + Loc.T(objective.Text)
                : Loc.T(objective.Text);
            if (text != objectiveShown)
            {
                if (objectiveShown != null && session.Guided)
                {
                    AudioDirector.Effect(SoundEffectKind.Chime);
                }

                objectiveShown = text;
                objectivePulse = 1f;
                workGuideText.text = text;
            }

            objectivePulse = Mathf.MoveTowards(objectivePulse, 0f, Time.unscaledDeltaTime * 3f);
            float pop = 1f + 0.08f * objectivePulse;
            workGuide.transform.localScale = new Vector3(pop, pop, 1f);

            workArrow.gameObject.SetActive(pointing);
            if (pointing)
            {
                Vector2 direction = (Vector2)target.transform.position - (Vector2)hero.transform.position;
                workArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
        }

        /// <summary>The mark that bounces over the place. It lives in the scene, so each scene makes its own.</summary>
        private void TickObjectiveMark(bool show, Vector3 at)
        {
            if (objectiveMark == null)
            {
                if (!show)
                {
                    return;
                }

                var go = new GameObject("ObjectiveMark");
                var drawn = go.AddComponent<SpriteRenderer>();
                drawn.sprite = ArrowSprite();
                drawn.color = MarkColor;
                drawn.sortingOrder = 32000;
                go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                objectiveMark = go.transform;
            }

            objectiveMark.gameObject.SetActive(show);
            if (show)
            {
                float size = session.Guided ? 2.1f : 1.5f;
                objectiveMark.localScale = new Vector3(size, size, 1f);
                float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f)) * MarkBounce;
                objectiveMark.position = at + new Vector3(0f, MarkHeight + bounce, -1f);
            }
        }

        private void TickGuideArrow(bool show, GameObject hero, Vector3 at)
        {
            if (guideArrow == null)
            {
                if (!show)
                {
                    return;
                }

                var go = new GameObject("GuideArrow");
                var drawn = go.AddComponent<SpriteRenderer>();
                drawn.sprite = ArrowSprite();
                drawn.color = MarkColor;
                drawn.sortingOrder = 32000;
                go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                guideArrow = go.transform;
            }

            Vector2 toPlace = show ? (Vector2)at - (Vector2)hero.transform.position : Vector2.zero;
            bool far = toPlace.magnitude > GuideArrowHidesWithin;
            guideArrow.gameObject.SetActive(show && far);
            if (show && far)
            {
                Vector2 way = toPlace.normalized;
                float push = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f)) * 0.15f;
                guideArrow.position = hero.transform.position + (Vector3)(way * (GuideArrowDistance + push)) + new Vector3(0f, 0.5f, -1f);
                guideArrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(way.y, way.x) * Mathf.Rad2Deg - 90f);
            }
        }

        /// <summary>Out of sight, an arrow on the edge of the screen says which way the place is.</summary>
        private void TickEdgeArrow(bool show, Vector3 at)
        {
            var camera = Camera.main;
            if (!show || camera == null)
            {
                edgeArrow.gameObject.SetActive(false);
                return;
            }

            Vector3 viewport = camera.WorldToViewportPoint(at);
            bool inSight = viewport.x > 0.03f && viewport.x < 0.97f && viewport.y > 0.03f && viewport.y < 0.97f;
            edgeArrow.gameObject.SetActive(!inSight);
            if (inSight)
            {
                return;
            }

            var size = ((RectTransform)canvas.transform).rect.size;
            float rightEdge = size.x * 0.5f - EdgeMargin - aiCalls.RoomNeeded;
            float leftEdge = -size.x * 0.5f + EdgeMargin;
            float topEdge = size.y * 0.5f - EdgeMargin;
            Vector2 away = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
            float scale = Mathf.Min(1f, Mathf.Min(
                away.x > 0f ? rightEdge / away.x : (away.x < 0f ? leftEdge / away.x : 1f),
                away.y != 0f ? topEdge / Mathf.Abs(away.y) : 1f));
            edgeArrow.anchoredPosition = away * scale;
            edgeArrow.GetChild(1).localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg - 90f);
        }

        // ---------------------------------------------------------------- the panel of calls to the models

        private const float TracePollSeconds = 2f;
        private bool wasBusy;
        private bool traceIsOld;

        private AiCallsPanel aiCalls;
        private float nextTracePoll;
        private bool tracePolling;

        /// <summary>The panel, the room it takes from the windows and, while it matters, what the backend reports.</summary>
        private void TickAiCalls()
        {
            TrafficCar.PassThrough = session.Guided;
            Patrol.PassThrough = session.Guided;
            bool windowsTyping = negotiationPanel.IsOpen || talkPanel.IsOpen || phone.IsTyping;
            if (Input.GetKeyDown(KeyCode.L) && !windowsTyping && !pause.IsOpen)
            {
                aiCalls.Toggle();
            }

            aiCalls.Tick(!openingPlaying && !LoadingScreen.IsLoading && !townMap.IsOpen && !pause.IsOpen && !endScreen.IsOpen);
            float room = aiCalls.RoomNeeded;
            modal.LeaveRoom(room);
            negotiationPanel.LeaveRoom(room);
            talkPanel.LeaveRoom(room);
            ((RectTransform)loadingCard.transform).anchoredPosition = new Vector2(-24f - room, -364f);

            // A reply that just arrived is read back at once, even if a question was already on its way before it.
            aiCalls.SetWaiting(session.Busy);
            traceIsOld |= wasBusy && !session.Busy;
            wasBusy = session.Busy;

            // The tab counts the calls too; only a run without a session asks nothing.
            if (!tracePolling && (traceIsOld || Time.unscaledTime >= nextTracePoll))
            {
                traceIsOld = false;
                var service = ApiClient.Instance != null ? ApiClient.Instance.Service : null;
                if (service != null && service.HasSession)
                {
                    StartCoroutine(PollTrace(service));
                }
            }
        }

        private IEnumerator PollTrace(ApiService service)
        {
            tracePolling = true;
            ApiResult<SessionTraceDto> result = null;
            yield return service.RequestTrace(r => result = r);
            tracePolling = false;
            nextTracePoll = Time.unscaledTime + TracePollSeconds;
            if (result != null && result.Ok)
            {
                aiCalls.Show(result.Value);
            }
        }
    }
}

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
            TickEdgeArrow(show && pointing, pointing ? target.transform.position : Vector3.zero);
            if (!show)
            {
                return;
            }

            string text = Loc.T(objective.Text);
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
                go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                objectiveMark = go.transform;
            }

            objectiveMark.gameObject.SetActive(show);
            if (show)
            {
                float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f)) * MarkBounce;
                objectiveMark.position = at + new Vector3(0f, MarkHeight + bounce, -1f);
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

        private const float TracePollSeconds = 3f;

        private AiCallsPanel aiCalls;
        private float nextTracePoll;
        private bool tracePolling;

        /// <summary>The panel, the room it takes from the windows and, while it matters, what the backend reports.</summary>
        private void TickAiCalls()
        {
            TrafficCar.PassThrough = session.Guided;
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

            // The tab counts the calls too; only a run without a session asks nothing.
            if (!tracePolling && Time.unscaledTime >= nextTracePoll)
            {
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

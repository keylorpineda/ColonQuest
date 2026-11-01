using System.Collections;
using System.Collections.Generic;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The demo for whoever will not play a whole year: each thing the game does, one click away and against the real
    /// backend. On the left the stations, in the middle the one being tried, and on the right what the models really did,
    /// read back from the server: which model, which provider, how long, how many tokens, what it cost and which tools it
    /// used. It has its own sessions, so it never touches a saved game, and it says so when an answer did not come from a
    /// model. Each station lives in its own file.
    /// </summary>
    public sealed partial class DemoLab : MonoBehaviour
    {
        public const int StationCount = 6;

        private const float StageWidth = 1920f;
        private const float StageHeight = 1080f;
        private const float RailX = 24f;
        private const float RailWidth = 320f;
        private const float WorkX = 360f;
        private const float WorkWidth = 1040f;
        private const float EvidenceX = 1416f;
        private const float EvidenceWidth = 480f;
        private const float BodyTop = -116f;
        private const float BodyHeight = 940f;
        private const float StationButtonHeight = 146f;

        /// <summary>The left edge and the width of what a station draws, inside the workspace.</summary>
        private const float Left = 28f;
        private const float Width = 972f;

        private const string NoServer = "No hubo respuesta del servidor. Revisá el internet y probá otra vez.";

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color Night = GamePalette.Hex("#17201E");
        private static readonly Color Cream = GamePalette.Hex("#F8F1DC");
        private static readonly Color Beak = GamePalette.Hex("#F2A23A");
        private static readonly Color Soft = GamePalette.Hex("#C9C3AE");
        private static readonly Color Locked = new Color(0.88f, 0.88f, 0.9f, 1f);

        private static readonly string[,] Stations =
        {
            { "Game master", "Muse Spark con herramientas" },
            { "Negociación", "Muse Spark Open y Voice Transcribe" },
            { "Plan y cierre", "Muse Wonder" },
            { "Arte del juego", "Muse Image" },
            { "Motor y controles", "Lo que le pone reglas a la IA" },
            { "Modo Clase", "El panel del profesor, con Muse Spark" }
        };

        /// <summary>Why each model has its job, with the times measured for docs/adr/0004-model-roles.md.</summary>
        private static readonly string[] WhyThisModel =
        {
            "Muse Spark es el único que responde siempre dentro de los 8 s del juego (3 a 4,4 s). Sus herramientas leen el historial y calculan deudas, pero nunca escriben el estado del jugador.",
            "Muse Spark Open corre con pesos abiertos en Baseten (1,9 a 2,8 s). Voice Transcribe pasa tu voz a texto (0,7 a 1,5 s). Si Baseten falla, contesta la API de Meta y el juego ni se entera.",
            "Muse Wonder es el mejor escritor, pero suele tardar de 6 a 10 s. Por eso solo trabaja donde nadie espera: planifica en segundo plano y narra detrás de una pantalla de carga.",
            "Muse Image tarda de 14 a 17 s por imagen. Por eso todas se generan antes y se guardan: el juego nunca la espera en vivo.",
            "Esta estación no llama a ningún modelo: es lo que mantiene a la IA dentro de las reglas.",
            "Las cifras salen de la base de datos. Muse Spark solo las explica en lenguaje claro; no inventa números."
        };

        private static DemoLab current;

        private readonly List<GameObject> stationButtons = new List<GameObject>();
        private readonly List<PendingPicture> pictures = new List<PendingPicture>();
        private readonly Dictionary<string, float> costs = new Dictionary<string, float>();

        private CanvasScaler scaler;
        private RectTransform stage;
        private RectTransform workspace;
        private RectTransform evidenceContent;
        private Text evidenceText;
        private ScrollRect evidenceScroll;
        private Text serverStatus;
        private Text waitLabel;
        private string waitText;
        private float waitStarted;
        private float nextPictureCheck;
        private string lastOriginNote = string.Empty;
        private HealthStatus health;

        public static bool IsOpen => current != null;

        public int Current { get; private set; }

        /// <summary>True while a station waits for the server. One thing at a time, so what is shown is what was asked.</summary>
        public bool Busy { get; private set; }

        public static void Open()
        {
            if (current == null)
            {
                current = new GameObject("DemoLab").AddComponent<DemoLab>();
            }
        }

        public void Close()
        {
            recorder?.Cancel();
            Destroy(gameObject);
            current = null;
        }

        private void Awake()
        {
            // The game sets these when a run starts; the lab can open before any run.
            ApiService.LocalTextsOnly = () => false;
            ApiService.Language = () => Loc.Current == GameLanguage.English ? "en" : "es";
            gmService = NewService();
            negotiationService = NewService();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(StageWidth, StageHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            FitToWindow();
            gameObject.AddComponent<GraphicRaycaster>();

            BuildBackdrop();
            stage = UiKit.Rect(transform, "Stage", new Vector2(StageWidth, StageHeight), Center, Vector2.zero);
            BuildTopBar();
            BuildRail();
            workspace = UiKit.Card(stage, "WorkCard", UiKit.Paper, new Vector2(WorkWidth, BodyHeight), TopLeft, new Vector2(WorkX, BodyTop));
            workspace.name = "Workspace";
            BuildEvidence();
            Show(0);
            StartCoroutine(CheckServer());
        }

        /// <summary>The stage always fits whole: a window narrower than 16:9 is matched by its width, any other by its height.</summary>
        private void FitToWindow()
        {
            scaler.matchWidthOrHeight = Screen.width / (float)Mathf.Max(1, Screen.height) < StageWidth / StageHeight ? 0f : 1f;
        }

        private void Update()
        {
            FitToWindow();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (waitLabel != null)
            {
                int seconds = Mathf.FloorToInt(Time.unscaledTime - waitStarted);
                string slow = seconds >= 12 ? "\n" + Loc.T("Si el servidor estaba dormido, despertarlo toma hasta un minuto.") : string.Empty;
                waitLabel.text = Loc.T(waitText) + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3) + "  " + seconds + " s" + slow;
            }

            TickPictures();
            TickNegotiation();
        }

        // ---------------------------------------------------------------- frame

        private void BuildBackdrop()
        {
            var plate = UiKit.Rect(transform, "Backdrop", Vector2.zero, Center, Vector2.zero);
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.one;
            plate.sizeDelta = Vector2.zero;
            var art = plate.gameObject.AddComponent<Image>();
            art.sprite = Resources.Load<Sprite>("Menu/KeyArt_Plate");
            art.color = art.sprite != null ? new Color(0.28f, 0.3f, 0.3f, 1f) : Night;
            art.raycastTarget = true;
        }

        private void BuildTopBar()
        {
            var title = UiKit.Label(stage, "Demo de funciones", 36, Cream, new Vector2(330f, 70f), TopLeft, new Vector2(RailX + 4f, -20f));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Night;
            outline.effectDistance = new Vector2(3f, -3f);

            UiKit.Label(stage, "Cada función del juego, con la IA real y sus datos a la vista", 26, Soft, new Vector2(860f, 40f), TopLeft, new Vector2(WorkX + 6f, -36f));
            serverStatus = UiKit.PlainLabel(stage, "ServerStatus", Loc.T("Conectando con el servidor..."), 24, Soft, new Vector2(330f, 60f), TopLeft, new Vector2(1250f, -26f), TextAnchor.MiddleRight);
            UiKit.MakeButton(stage, "Volver al menú", GameUiColors.Yellow, new Vector2(290f, 72f), TopLeft, new Vector2(1606f, -22f), Close);
        }

        private void BuildRail()
        {
            for (int i = 0; i < StationCount; i++)
            {
                int station = i;
                var button = UiKit.MakeButton(stage, Stations[i, 0], Cream, new Vector2(RailWidth, StationButtonHeight), TopLeft, new Vector2(RailX, BodyTop - i * (StationButtonHeight + 12f)), () => Show(station));
                button.name = "Station_" + i;
                var name = button.transform.Find("Body/Text").GetComponent<Text>();
                name.rectTransform.anchoredPosition = new Vector2(0f, 26f);
                name.rectTransform.sizeDelta = new Vector2(RailWidth - 40f, 62f);
                UiKit.Label(button.transform.Find("Body"), Stations[i, 1], 21, GameUiColors.Muted, new Vector2(RailWidth - 40f, 62f), Center, new Vector2(0f, -30f), TextAnchor.MiddleCenter);
                stationButtons.Add(button.transform.Find("Body").gameObject);
            }
        }

        private void BuildEvidence()
        {
            var card = UiKit.Card(stage, "EvidenceCard", Night, new Vector2(EvidenceWidth, BodyHeight), TopLeft, new Vector2(EvidenceX, BodyTop));
            UiKit.Label(card, "Qué hizo la IA", 34, Beak, new Vector2(EvidenceWidth - 56f, 50f), TopLeft, new Vector2(24f, -16f));

            var view = UiKit.Fill(card, "View", new Color(1f, 1f, 1f, 0.04f), new Vector2(EvidenceWidth - 36f, BodyHeight - 96f), TopLeft, new Vector2(12f, -72f));
            view.raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            evidenceContent = UiKit.Rect(view.transform, "Content", new Vector2(EvidenceWidth - 36f, 10f), new Vector2(0.5f, 1f), Vector2.zero);

            evidenceText = UiKit.PlainLabel(evidenceContent, "Text", string.Empty, 22, Cream, new Vector2(EvidenceWidth - 64f, 10f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), TextAnchor.UpperLeft);
            evidenceText.fontStyle = FontStyle.Normal;
            evidenceText.supportRichText = true;
            Destroy(evidenceText.GetComponent<TextFit>());

            evidenceScroll = view.gameObject.AddComponent<ScrollRect>();
            evidenceScroll.viewport = view.rectTransform;
            evidenceScroll.content = evidenceContent;
            evidenceScroll.horizontal = false;
            evidenceScroll.movementType = ScrollRect.MovementType.Clamped;
            evidenceScroll.inertia = false;
            evidenceScroll.scrollSensitivity = 40f;
        }

        // ---------------------------------------------------------------- stations

        public void Show(int station)
        {
            if (station != 1)
            {
                recorder?.Cancel();
            }

            Current = station;
            for (int i = 0; i < stationButtons.Count; i++)
            {
                stationButtons[i].GetComponent<Image>().color = i == station ? Beak : Cream;
            }

            lastOriginNote = string.Empty;
            Redraw();
            RenderEvidence(new List<SessionTraceDto>());
            StartCoroutine(RefreshEvidence());
        }

        /// <summary>Draws the current station again from its state: a station holds nothing on screen that it cannot rebuild.</summary>
        private void Redraw()
        {
            for (int i = workspace.childCount - 1; i >= 0; i--)
            {
                var child = workspace.GetChild(i);
                if (child.name != "TopBevel")
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
            }

            pictures.Clear();
            waitLabel = null;
            ClearNegotiationInput();
            switch (Current)
            {
                case 0: DrawGameMaster(); break;
                case 1: DrawNegotiation(); break;
                case 2: DrawYear(); break;
                case 3: DrawGallery(); break;
                case 4: DrawEngine(); break;
                default: DrawClass(); break;
            }
        }

        /// <summary>The services whose sessions the evidence panel reads for a station.</summary>
        private IEnumerable<ApiService> ServicesOf(int station)
        {
            switch (station)
            {
                case 0: return new[] { gmService };
                case 1: return new[] { negotiationService };
                case 2: return new[] { planService, yearService };
                case 5: return classServices;
                default: return new ApiService[0];
            }
        }

        private static ApiService NewService()
        {
            return ApiClient.Instance.NewDemoService();
        }

        private void DrawHeading(string title, string model, string intro)
        {
            var heading = UiKit.PlainLabel(workspace, "Heading", Loc.T(title), 44, UiKit.Ink, new Vector2(Width, 56f), TopLeft, new Vector2(Left, -14f));
            heading.font = UiKit.TitleFont;
            var who = UiKit.PlainLabel(workspace, "Model", model, 24, GameUiColors.Gain, new Vector2(Width, 32f), TopLeft, new Vector2(Left, -68f));
            who.horizontalOverflow = HorizontalWrapMode.Overflow;
            Note(intro, 23, GameUiColors.Muted, new Vector2(Width, 62f), new Vector2(Left, -100f));
        }

        private Text Note(string spanish, int size, Color color, Vector2 box, Vector2 position, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = UiKit.Label(workspace, spanish, size, color, box, TopLeft, position, alignment);
            label.fontStyle = FontStyle.Normal;
            return label;
        }

        /// <summary>A text that came from the backend or from a calculation, so it is shown as it is.</summary>
        private Text Plain(string text, int size, Color color, Vector2 box, Vector2 position, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = UiKit.PlainLabel(workspace, "Text", text, size, color, box, TopLeft, position, alignment);
            label.fontStyle = FontStyle.Normal;
            return label;
        }

        private Button Action(string spanish, Color color, Vector2 size, Vector2 position, System.Action onClick, bool enabled = true)
        {
            var button = UiKit.MakeButton(workspace, spanish, color, size, TopLeft, position, onClick);
            UiKit.SetFontSize(button, 25);

            // Best fit only shrinks a text that is not allowed to spill out of its box.
            button.transform.Find("Body/Text").GetComponent<Text>().verticalOverflow = VerticalWrapMode.Truncate;
            button.interactable = enabled && !Busy;
            return button;
        }

        /// <summary>A strip that says who wrote what is below it. Green only when a model really did.</summary>
        private void OriginBadge(string origin, Vector2 box, Vector2 position)
        {
            var color = DemoReadout.WrittenByAModel(origin) ? GameUiColors.Green : origin == "cache" ? GameUiColors.Blue : GameUiColors.Pink;
            var badge = UiKit.Card(workspace, "Origin", color, box, TopLeft, position);
            UiKit.PlainLabel(badge, "Text", DemoReadout.OriginOf(origin), 21, UiKit.Ink, box - new Vector2(28f, 12f), Center, Vector2.zero, TextAnchor.MiddleCenter);
        }

        private void DrawWait(Vector2 box, Vector2 position)
        {
            waitLabel = Plain(string.Empty, 28, GameUiColors.Muted, box, position, TextAnchor.UpperCenter);
            waitLabel.fontStyle = FontStyle.Italic;
        }

        private void BeginWait(string spanish)
        {
            Busy = true;
            waitText = spanish;
            waitStarted = Time.unscaledTime;
        }

        private void EndWait()
        {
            Busy = false;
            waitLabel = null;
        }

        private static IEnumerator EnsureSession(ApiService service, string classCode, System.Action<bool> done)
        {
            if (service.HasSession)
            {
                done(true);
                yield break;
            }

            ApiResult<string> result = null;
            yield return service.CreateSession(classCode, r => result = r);
            done(result.Ok);
        }

        // ---------------------------------------------------------------- pictures

        private sealed class PendingPicture
        {
            public Image Target;
            public string Key;
            public float GiveUpAt;
        }

        /// <summary>A place for a picture of the backend. It fills in by itself when the picture arrives; until then it is a plain tile.</summary>
        private Image Picture(RectTransform parent, string key, Vector2 size, Vector2 anchor, Vector2 position)
        {
            var frame = UiKit.Fill(parent, "Picture_" + key, UiKit.Ink, size, anchor, position);
            var image = UiKit.Fill(frame.transform, "Image", new Color(0.8f, 0.8f, 0.76f, 1f), size - new Vector2(UiKit.Outline, UiKit.Outline), Center, Vector2.zero);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.sprite = null;
            pictures.Add(new PendingPicture { Target = image, Key = key, GiveUpAt = Time.unscaledTime + 60f });
            nextPictureCheck = 0f;
            return image;
        }

        private void TickPictures()
        {
            if (pictures.Count == 0 || Time.unscaledTime < nextPictureCheck)
            {
                return;
            }

            nextPictureCheck = Time.unscaledTime + 0.5f;
            for (int i = pictures.Count - 1; i >= 0; i--)
            {
                var entry = pictures[i];
                var sprite = RemoteImages.Fetch(entry.Key);
                if (entry.Target != null && sprite != null)
                {
                    entry.Target.sprite = sprite;
                    entry.Target.color = Color.white;
                }

                if (entry.Target == null || sprite != null || Time.unscaledTime > entry.GiveUpAt)
                {
                    pictures.RemoveAt(i);
                }
            }

            if (Current == 3)
            {
                UpdateGalleryCount();
            }
        }

        // ---------------------------------------------------------------- evidence

        private IEnumerator CheckServer()
        {
            ApiResult<HealthStatus> result = null;
            yield return NewService().CheckHealth(r => result = r);
            if (result.Ok)
            {
                health = result.Value;
                serverStatus.text = Loc.T("Servidor en línea");
                serverStatus.color = GameUiColors.Green;
            }
            else
            {
                serverStatus.text = Loc.T("El servidor no responde");
                serverStatus.color = GameUiColors.Pink;
            }

            if (Current == 4)
            {
                RenderEvidence(new List<SessionTraceDto>());
            }
        }

        /// <summary>Reads back from the server what was really done in the current station.</summary>
        private IEnumerator RefreshEvidence()
        {
            int station = Current;
            var traces = new List<SessionTraceDto>();
            foreach (var service in new List<ApiService>(ServicesOf(station)))
            {
                if (service == null || !service.HasSession)
                {
                    continue;
                }

                ApiResult<SessionTraceDto> result = null;
                yield return service.RequestTrace(r => result = r);
                if (result.Ok)
                {
                    traces.Add(result.Value);
                    costs[service.SessionId] = result.Value.totalCostUsd;
                }
            }

            if (Current == station)
            {
                RenderEvidence(traces);
            }
        }

        private void RenderEvidence(List<SessionTraceDto> traces)
        {
            var text = new StringBuilder();
            text.Append(Loc.T(WhyThisModel[Current])).Append("\n\n");
            if (Current == 4)
            {
                AppendBudgets(text);
            }
            else if (Current == 3)
            {
                text.Append(Loc.T("Cada imagen costó $0.01 una sola vez. Las veinte juntas: $0.20. Verlas aquí no gasta nada.")).Append("\n\n");
            }

            float total = 0f;
            foreach (var cost in costs.Values)
            {
                total += cost;
            }

            if (total > 0f)
            {
                text.Append(Tint(Loc.F("Toda esta demo lleva gastados {0}", DemoReadout.Dollars(total)), "#F2A23A")).Append("\n\n");
            }

            if (!string.IsNullOrEmpty(lastOriginNote))
            {
                text.Append(lastOriginNote).Append("\n\n");
            }

            bool any = false;
            foreach (var trace in traces)
            {
                foreach (var call in trace.calls)
                {
                    any = true;
                    AppendCall(text, call);
                }

                foreach (var step in trace.toolSteps)
                {
                    any = true;
                    AppendStep(text, step);
                }
            }

            if (!any && Current != 3 && Current != 4)
            {
                text.Append(Tint(Loc.T("Probá algo: cada llamada real va a aparecer aquí, con su modelo, su tiempo y su costo."), "#C9C3AE"));
            }

            evidenceText.text = text.ToString();
            float height = evidenceText.preferredHeight + 30f;
            evidenceText.rectTransform.sizeDelta = new Vector2(evidenceText.rectTransform.sizeDelta.x, height);
            evidenceContent.sizeDelta = new Vector2(evidenceContent.sizeDelta.x, height);
            evidenceScroll.verticalNormalizedPosition = 0f;
        }

        private void AppendBudgets(StringBuilder text)
        {
            if (health == null)
            {
                text.Append(Tint(Loc.T("Todavía no hay datos del servidor."), "#C9C3AE"));
                return;
            }

            text.Append(Tint(Loc.T("Presupuesto de hoy, en vivo"), "#F2A23A")).Append('\n');
            text.Append(Loc.F("API de Meta: {0} de {1}", DemoReadout.Dollars(health.MetaSpentUsd), DemoReadout.Dollars(health.MetaBudgetUsd))).Append('\n');
            text.Append(Loc.F("Baseten: {0} de {1}", DemoReadout.Dollars(health.OpenWeightsSpentUsd), DemoReadout.Dollars(health.OpenWeightsBudgetUsd))).Append("\n\n");
            text.Append(Loc.T("Al 80% del presupuesto el game master baja a Muse Spark. Al 100% solo salen eventos de respaldo. El juego nunca se cae por eso.")).Append("\n\n");
        }

        private static void AppendCall(StringBuilder text, ModelCallDto call)
        {
            text.Append(Tint(DemoReadout.ModelName(call.model), "#F2A23A")).Append("  ").Append(DemoReadout.ProviderName(call.provider)).Append('\n');
            text.Append(DemoReadout.JobOf(call.endpoint)).Append('\n');
            text.Append(Tint(DemoReadout.Whole(call.latencyMs) + " ms", "#A8F0B8"));
            if (call.inputTokens + call.outputTokens > 0)
            {
                text.Append("  ·  ").Append(Loc.F("{0} tokens", DemoReadout.Whole(call.inputTokens + call.outputTokens)));
            }

            text.Append("  ·  ").Append(DemoReadout.Dollars(call.costUsd));
            if (!call.ok)
            {
                text.Append("  ·  ").Append(Tint(Loc.T("falló"), "#FF8A8A"));
            }

            text.Append("\n\n");
        }

        private static void AppendStep(StringBuilder text, ToolStepDto step)
        {
            text.Append(Tint(Loc.F("Herramienta: {0}", step.tool), "#8FD3FF")).Append('\n');
            text.Append(Tint(Shorten(step.arguments, 150), "#C9C3AE")).Append('\n');
            text.Append(Shorten(step.result, 220)).Append("\n\n");
        }

        private static string Shorten(string value, int length)
        {
            string safe = (value ?? string.Empty).Replace('<', '‹').Replace('>', '›');
            return safe.Length <= length ? safe : safe.Substring(0, length) + "...";
        }

        private static string Tint(string value, string hex)
        {
            return "<color=" + hex + "><b>" + value + "</b></color>";
        }
    }
}

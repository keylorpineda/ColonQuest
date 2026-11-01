using System.Collections;
using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// What Muse Wonder writes where nobody waits: the plan of the year, which arrives in the background over the base plan
    /// and changes with the profile of the player, and the story of how the year ended, on a postcard whose figures the game
    /// draws over a picture without text.
    /// </summary>
    public sealed partial class DemoLab
    {
        private const float PlanGiveUpSeconds = 45f;

        private ApiService planService;
        private ApiService yearService;
        private List<PlanMonthDto> plan;
        private string basePlan;
        private bool planWaiting;
        private bool planFromModel;
        private string planNotice;
        private int planProfile = -1;

        private SessionSummary yearSummary;
        private bool yearDebts;
        private string yearError;

        private void DrawYear()
        {
            DrawHeading("Plan y cierre", "Muse Wonder", "Al empezar una partida, Muse Wonder planifica los doce meses según quién sos, sin que te des cuenta de que está trabajando. Probá los dos perfiles y compará.");

            float y = -170f;
            Action("Perfil endeudado", planProfile == 0 ? Beak : GameUiColors.Blue, new Vector2(230f, 64f), new Vector2(Left, y), () => TryPlan(0), !planWaiting);
            Action("Perfil ahorrador", planProfile == 1 ? Beak : GameUiColors.Blue, new Vector2(230f, 64f), new Vector2(Left + 240f, y), () => TryPlan(1), !planWaiting);
            if (!string.IsNullOrEmpty(planNotice))
            {
                Note(planNotice, 22, planFromModel ? GameUiColors.Gain : GameUiColors.Muted, new Vector2(Width - 490f, 64f), new Vector2(Left + 490f, y), TextAnchor.MiddleLeft);
            }

            y -= 74f;
            DrawPlan(y);
            DrawClosing(-590f);
        }

        private void DrawPlan(float y)
        {
            const float width = 236f;
            const float height = 94f;
            for (int i = 0; i < 12; i++)
            {
                var position = new Vector2(Left + i % 4 * (width + 9f), y - i / 4 * (height + 8f));
                var month = plan != null && i < plan.Count ? plan[i] : null;
                var tile = UiKit.Card(workspace, "Month_" + (i + 1), month == null ? Locked : ColorOf(month.eventType), new Vector2(width, height), TopLeft, position);
                UiKit.PlainLabel(tile, "Month", Loc.F("Mes {0}", i + 1), 17, GameUiColors.Muted, new Vector2(width - 24f, 22f), TopLeft, new Vector2(10f, -6f));
                if (month != null)
                {
                    UiKit.PlainLabel(tile, "Concept", DemoReadout.ConceptName(month.teachingConcept), 19, UiKit.Ink, new Vector2(width - 24f, 24f), TopLeft, new Vector2(10f, -26f));
                    var theme = UiKit.PlainLabel(tile, "Theme", month.theme, 16, UiKit.Ink, new Vector2(width - 24f, 34f), TopLeft, new Vector2(10f, -48f), TextAnchor.UpperLeft);
                    theme.fontStyle = FontStyle.Normal;
                }
            }
        }

        private static Color ColorOf(string eventType)
        {
            switch (eventType)
            {
                case "scam": return GameUiColors.Pink;
                case "emergency": return GameUiColors.Yellow;
                case "offer": return GameUiColors.Blue;
                case "bonus":
                case "income": return GameUiColors.Green;
                default: return Color.white;
            }
        }

        private void DrawClosing(float y)
        {
            Note("Cómo terminó el año", 26, UiKit.Ink, new Vector2(320f, 36f), new Vector2(Left, y));
            y -= 40f;
            Action("Año con deudas", yearSummary != null && yearDebts ? Beak : GameUiColors.Blue, new Vector2(230f, 60f), new Vector2(Left, y), () => TryYear(true));
            Action("Año sin deudas", yearSummary != null && !yearDebts ? Beak : GameUiColors.Blue, new Vector2(230f, 60f), new Vector2(Left + 240f, y), () => TryYear(false));
            if (yearSummary != null)
            {
                OriginBadge(yearSummary.Source, new Vector2(Width - 490f, 60f), new Vector2(Left + 490f, y));
            }

            y -= 70f;
            if (Busy)
            {
                DrawWait(new Vector2(Width, 90f), new Vector2(Left, y - 40f));
                return;
            }

            if (!string.IsNullOrEmpty(yearError))
            {
                Note(yearError, 26, GameUiColors.Loss, new Vector2(Width, 90f), new Vector2(Left, y - 40f), TextAnchor.UpperCenter);
                return;
            }

            if (yearSummary == null)
            {
                return;
            }

            var postcard = Picture(workspace, PostcardKeys.For(yearSummary.TotalDebt), new Vector2(330f, 212f), TopLeft, new Vector2(Left, y));
            var frame = (RectTransform)postcard.transform.parent;
            var band = UiKit.Fill(frame, "Figures", new Color(0.09f, 0.13f, 0.12f, 0.82f), new Vector2(322f, 62f), new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            UiKit.PlainLabel(band.transform, "NetWorth", Loc.F("Patrimonio {0}", Money.Format(yearSummary.NetWorth)), 19, Cream, new Vector2(304f, 26f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), TextAnchor.MiddleCenter);
            UiKit.PlainLabel(band.transform, "Debt", Loc.F("Deudas {0}", Money.Format(yearSummary.TotalDebt)), 19, Cream, new Vector2(304f, 26f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), TextAnchor.MiddleCenter);
            Plain(yearSummary.Narrative, 21, UiKit.Ink, new Vector2(Width - 350f, 212f), new Vector2(Left + 350f, y));
        }

        private void TryPlan(int profile)
        {
            if (!Busy && !planWaiting)
            {
                StartCoroutine(RunPlan(profile));
            }
        }

        private IEnumerator RunPlan(int profile)
        {
            planProfile = profile;
            planService = NewService();
            plan = null;
            basePlan = null;
            planFromModel = false;
            planWaiting = true;
            planNotice = "Pidiendo el primer evento de la partida: ahí arranca el plan...";
            Redraw();

            // The plan is only made on the first event of a session, so each try needs a new one.
            var service = planService;
            bool connected = false;
            yield return EnsureSession(service, null, ok => connected = ok);
            if (!connected)
            {
                planWaiting = false;
                planNotice = NoServer;
                Redraw();
                yield break;
            }

            var state = profile == 0 ? DemoScenarios.CardAtTheLimit() : DemoScenarios.HighSavings();
            state.Month = 1;
            yield return service.RequestEvent(state, _ => { });

            planNotice = "El evento ya llegó. Muse Wonder sigue escribiendo el plan en segundo plano, y el jugador ya está jugando con el plan base...";
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < PlanGiveUpSeconds)
            {
                ApiResult<SessionTraceDto> result = null;
                yield return service.RequestTrace(r => result = r);
                if (service != planService)
                {
                    yield break;
                }

                if (result.Ok)
                {
                    costs[service.SessionId] = result.Value.totalCostUsd;
                    plan = result.Value.seasonPlan;
                    string themes = ThemesOf(plan);
                    basePlan = basePlan ?? themes;
                    if (Current == 2)
                    {
                        RenderEvidence(new List<SessionTraceDto> { result.Value });
                        if (!Busy)
                        {
                            Redraw();
                        }
                    }

                    if (DemoReadout.PlannerAnswered(result.Value))
                    {
                        planFromModel = DemoReadout.PlannedByTheModel(result.Value) && themes != basePlan;
                        break;
                    }
                }

                yield return new WaitForSecondsRealtime(2.5f);
            }

            planWaiting = false;
            planNotice = planFromModel ? "Este plan lo escribió Muse Wonder para este jugador. Y cambia con sus decisiones." : "Muse Wonder no llegó a tiempo: la partida sigue con el plan base, sin que el jugador lo note.";
            if (Current == 2 && !Busy)
            {
                Redraw();
            }
        }

        private static string ThemesOf(List<PlanMonthDto> months)
        {
            var themes = new System.Text.StringBuilder();
            foreach (var month in months)
            {
                themes.Append(month.theme);
            }

            return themes.ToString();
        }

        private void TryYear(bool debts)
        {
            if (!Busy)
            {
                StartCoroutine(RunYear(debts));
            }
        }

        private IEnumerator RunYear(bool debts)
        {
            yearDebts = debts;
            yearSummary = null;
            yearError = null;
            BeginWait("Muse Wonder está narrando el año");
            Redraw();

            // The summary closes its session, so each one gets a new one with a couple of decisions to talk about.
            yearService = NewService();
            bool connected = false;
            yield return EnsureSession(yearService, null, ok => connected = ok);
            ApiResult<SessionSummary> result = null;
            if (connected)
            {
                var finalState = DemoScenarios.YearEnd(debts);
                yield return RecordSampleDecisions(yearService, finalState, 2);
                yield return yearService.RequestSummary(finalState, r => result = r);
            }

            EndWait();
            if (result != null && result.Ok)
            {
                yearSummary = result.Value;
            }
            else
            {
                yearError = NoServer;
            }

            Redraw();
            yield return RefreshEvidence();
        }

        /// <summary>Decisions on the game's own local events, so the summary and the class figures have something real to count.</summary>
        private static IEnumerator RecordSampleDecisions(ApiService service, PlayerState state, int count)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                var sample = FallbackEvents.Next(state, seen);
                seen.Add(sample.EventId);
                yield return service.RecordDecision(i + 1, sample, sample.Choices[0].Id, state, _ => { });
            }
        }
    }
}

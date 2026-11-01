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
    /// The teacher's panel. To have something to show it plays a real small class against the backend: it opens a class,
    /// three made-up students make decisions and negotiate with the real seller, and the panel adds it all up with SQL and
    /// Muse Spark explains it. The screen says these are not real students.
    /// </summary>
    public sealed partial class DemoLab
    {
        private const int SampleStudents = 3;

        private static readonly string[] StudentQuestions =
        {
            "¿Cuánto pago en total, con seguro y comisiones?",
            "¿De cuánto es la cuota?",
            "¿Tiene algún cargo escondido o seguro obligatorio?"
        };

        private readonly List<ApiService> classServices = new List<ApiService>();
        private ClassDashboardDto dashboard;
        private string classError;
        private int studentsDone;

        private void DrawClass()
        {
            DrawHeading("Modo Clase", "Muse Spark", "El profesor le da un código a su grupo y ve, sin datos personales, qué conceptos les cuestan más. Esto arma una clase de prueba de verdad en el servidor, con tres estudiantes simulados.");

            Action("Simular una clase", GameUiColors.Yellow, new Vector2(330f, 68f), new Vector2(Left, -170f), TrySimulate);
            Note("Datos de esta simulación, no de estudiantes reales.", 22, GameUiColors.Loss, new Vector2(Width - 350f, 68f), new Vector2(Left + 350f, -170f), TextAnchor.MiddleLeft);

            if (Busy)
            {
                DrawWait(new Vector2(Width, 90f), new Vector2(Left, -340f));
                return;
            }

            if (!string.IsNullOrEmpty(classError))
            {
                Note(classError, 26, GameUiColors.Loss, new Vector2(Width, 90f), new Vector2(Left, -340f), TextAnchor.UpperCenter);
                return;
            }

            if (dashboard != null)
            {
                DrawDashboard();
            }
        }

        private void DrawDashboard()
        {
            float y = -256f;
            const float width = 234f;
            Tile("Estudiantes", dashboard.students.ToString(), Left, y, width);
            Tile("Partidas cerradas", dashboard.finishedGames.ToString(), Left + width + 12f, y, width);
            Tile("Detectaron la trampa", dashboard.trapDetectionPct == ClassDashboardDto.NoData ? "s/d" : dashboard.trapDetectionPct + "%", Left + (width + 12f) * 2f, y, width);
            Tile("Deuda promedio", dashboard.averageFinalDebt == ClassDashboardDto.NoData ? "s/d" : Money.Format(dashboard.averageFinalDebt), Left + (width + 12f) * 3f, y, width);

            y -= 116f;
            Note("Quiénes eligieron la opción riesgosa, por concepto", 24, UiKit.Ink, new Vector2(Width, 34f), new Vector2(Left, y));
            y -= 38f;
            int shown = 0;
            foreach (var concept in dashboard.concepts)
            {
                if (shown++ >= 4)
                {
                    break;
                }

                Plain(DemoReadout.ConceptName(concept.concept), 21, UiKit.Ink, new Vector2(330f, 30f), new Vector2(Left, y), TextAnchor.MiddleLeft);
                UiKit.Fill(workspace, "Track", Locked, new Vector2(520f, 22f), TopLeft, new Vector2(Left + 340f, y - 4f));
                if (concept.riskyChoicePct == ClassDashboardDto.NoData)
                {
                    Note("Todavía sin comparar", 19, GameUiColors.Muted, new Vector2(520f, 30f), new Vector2(Left + 340f, y), TextAnchor.MiddleCenter);
                }
                else
                {
                    UiKit.Fill(workspace, "Bar", GameUiColors.Loss, new Vector2(Mathf.Max(6f, 520f * concept.riskyChoicePct / 100f), 22f), TopLeft, new Vector2(Left + 340f, y - 4f));
                    Plain(concept.riskyChoicePct + "%", 21, UiKit.Ink, new Vector2(100f, 30f), new Vector2(Left + 872f, y), TextAnchor.MiddleRight);
                }

                y -= 36f;
            }

            y -= 10f;
            OriginBadge(dashboard.source, new Vector2(Width, 44f), new Vector2(Left, y));
            y -= 54f;
            Plain(dashboard.explanation, 22, UiKit.Ink, new Vector2(Width, 900f + y), new Vector2(Left, y));
        }

        private void Tile(string name, string value, float x, float y, float width)
        {
            var tile = UiKit.Card(workspace, "Tile", Color.white, new Vector2(width, 104f), TopLeft, new Vector2(x, y));
            UiKit.PlainLabel(tile, "Value", value, 34, UiKit.Ink, new Vector2(width - 24f, 50f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), TextAnchor.MiddleCenter);
            UiKit.Label(tile, name, 19, GameUiColors.Muted, new Vector2(width - 24f, 30f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), TextAnchor.MiddleCenter);
        }

        private void TrySimulate()
        {
            if (!Busy)
            {
                StartCoroutine(RunClass());
            }
        }

        private IEnumerator RunClass()
        {
            dashboard = null;
            classError = null;
            classServices.Clear();
            studentsDone = 0;
            BeginWait("Tres estudiantes están jugando y negociando con el vendedor real");
            Redraw();

            var teacher = NewService();
            ApiResult<string> created = null;
            yield return teacher.CreateClass(Loc.T("Clase de prueba"), r => created = r);
            if (!created.Ok)
            {
                classError = NoServer;
                EndWait();
                Redraw();
                yield break;
            }

            for (int i = 0; i < SampleStudents; i++)
            {
                StartCoroutine(RunStudent(created.Value, i));
            }

            while (studentsDone < SampleStudents)
            {
                yield return null;
            }

            waitText = "Muse Spark está redactando el informe para el profesor";
            ApiResult<ClassDashboardDto> panel = null;
            yield return teacher.RequestClassDashboard(created.Value, r => panel = r);
            if (panel.Ok)
            {
                dashboard = panel.Value;
                lastOriginNote = Tint(DemoReadout.JobOf("class_dashboard") + ": " + DemoReadout.OriginOf(dashboard.source), DemoReadout.WrittenByAModel(dashboard.source) ? "#A8F0B8" : "#FF8A8A");
            }
            else
            {
                classError = NoServer;
            }

            EndWait();
            Redraw();
            yield return RefreshEvidence();
        }

        /// <summary>One student: careful or careless according to the index, so the class has something to tell the teacher.</summary>
        private IEnumerator RunStudent(string classCode, int index)
        {
            var service = NewService();
            classServices.Add(service);
            bool connected = false;
            yield return EnsureSession(service, classCode, ok => connected = ok);
            if (connected)
            {
                bool careless = index != 2;
                var state = DemoScenarios.FirstMonth();
                var seen = new HashSet<string>();
                for (int month = 1; month <= 3; month++)
                {
                    var sample = FallbackEvents.Next(state, seen);
                    seen.Add(sample.EventId);
                    state.Month = month;
                    if (careless)
                    {
                        state.CreditCard.Balance += 60_000L;
                    }

                    yield return service.RecordDecision(month, sample, sample.Choices[0].Id, state.Clone(), _ => { });
                }

                yield return service.SendNegotiationText(SellerOffer.Catalog[index].OfferId, Loc.T(StudentQuestions[index]), _ => { });
                yield return service.RequestSummary(state, _ => { });
            }

            studentsDone++;
        }
    }
}

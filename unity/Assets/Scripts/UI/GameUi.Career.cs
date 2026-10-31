using System.Collections.Generic;
using System.Text;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>The board of courses and the moment of a promotion. The salary and the fees are the finance engine's.</summary>
    public sealed partial class GameUi
    {
        private static string RefusalText(ClassRefusal refusal)
        {
            switch (refusal)
            {
                case ClassRefusal.OutsideTheAfternoon:
                    return "Las clases son de tarde, entre las 12:00 y las 18:00.";
                case ClassRefusal.AlreadyToday:
                    return "Hoy ya fuiste a clase. Volvé otro día.";
                case ClassRefusal.NotEnoughCash:
                    return "No te alcanza el efectivo para matricular.";
                default:
                    return string.Empty;
            }
        }

        private void ShowCourses()
        {
            var ina = Career.Course(CourseKind.Ina);
            var una = Career.Course(CourseKind.Una);
            var books = session.Loop.Courses;

            string progressIna = books.Done(CourseKind.Ina) ? Loc.T("completado") : Loc.F("{0}/{1} tardes", books.Attended(CourseKind.Ina), ina.Afternoons);
            string progressUna = books.Done(CourseKind.Una) ? Loc.T("completado") : Loc.F("{0}/{1} tardes", books.Attended(CourseKind.Una), una.Afternoons);

            var text = new StringBuilder();
            text.AppendLine(Loc.F("• Horario de clases: de {0}:00 a {1}:00. Efectivo: {2}.", Career.AfternoonStartHour, Career.AfternoonEndHour, Money.Format(session.State.Cash)));
            text.AppendLine(Loc.F("• INA ({0}, {1} tardes): abre cajero. UNA ({2}, {3} tardes): abre encargado.", Money.Format(ina.Price), ina.Afternoons, Money.Format(una.Price), una.Afternoons));
            text.Append(Loc.F("• Tu avance: INA ({0}) · UNA ({1}).", progressIna, progressUna));

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var course in Career.Courses)
            {
                var info = course;
                if (books.Done(info.Kind))
                {
                    continue;
                }

                string progress = books.Enrolled(info.Kind)
                    ? Loc.F("Ir a clase ({0} de {1})", books.Attended(info.Kind) + 1, info.Afternoons)
                    : Loc.F("{0} tardes  ·  {1}", info.Afternoons, Money.Format(info.Price));
                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {Loc.T(info.Name)}  ·  {progress}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => TryClass(info)
                });
                index++;
            }

            if (index == 0)
            {
                text.Insert(0, Loc.T("Ya terminaste todos los cursos.") + "\n\n");
            }

            options.Add(Back(Loc.F("{0}.  Salir", index + 1)));
            modal.Show(Loc.T("Cursos"), "INA · CURSOS", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private void TryClass(CourseInfo course)
        {
            var refusal = session.Loop.CheckClass(course.Kind);
            if (refusal != ClassRefusal.None)
            {
                modal.Show(Loc.T(course.Name), Loc.T(course.Description), Loc.T(RefusalText(refusal)), new[] { Continue(ShowCourses) });
                return;
            }

            Vector2 me = HeroAt();
            var scene = new Interaction().Pose(PersonPose.Read, 1.4f).Sound(SoundEffectKind.Swish)
                .Fly("paper", me + new Vector2(0f, 0.6f), me + new Vector2(0f, 0.25f), 0.6f)
                .Pose(PersonPose.Sign, 1.6f).Sound(SoundEffectKind.Scribble).Wait(0.5f).Sound(SoundEffectKind.Scribble)
                .Pose(PersonPose.None, 0.1f)
                .Apply(() => session.TakeClass(course.Kind))
                .Say(string.Empty, session.Loop.Courses.Attended(course.Kind) + 1 >= course.Afternoons ? "Terminé el curso. Eso ya nadie me lo quita." : "Clase lista. Una tarde menos.");
            if (!scene.Play())
            {
                session.TakeClass(course.Kind);
            }
        }

        /// <summary>The boss shakes the player's hand, the new salary is set and a window says what changed.</summary>
        private Interaction AddPromotion(Interaction scene)
        {
            var next = session.Loop.Job.Position + 1;
            return scene.Say(Boss, Loc.F("Venís cumpliendo y estudiaste. Desde hoy sos {0}.", Loc.T(PositionName(next))))
                .Wait(2.6f).Pose(PersonPose.Handover, 1.2f).Sound(SoundEffectKind.Chime).Wait(0.6f).Sound(SoundEffectKind.Coin)
                .Pose(PersonPose.None, 0.1f)
                .Apply(ShowPromotion);
        }

        private void ShowPromotion()
        {
            long before = session.State.SalaryNet;
            if (!session.Promote())
            {
                return;
            }

            Confetti.Burst(canvas.transform);
            var text = new StringBuilder();
            text.AppendLine(Loc.F("Tu salario neto era {0} al mes.", Money.Format(before)));
            text.AppendLine(Loc.F("Ahora es {0} al mes.", Money.Format(session.State.SalaryNet)));
            text.AppendLine();
            text.Append(Loc.T("Lo que invertiste en vos también dio intereses."));
            modal.Show(Loc.T("¡Te ascendieron!"), Loc.T(PositionName(session.Loop.Job.Position)), text.ToString(), new[] { Continue(null) });
        }

        public static string PositionName(JobPosition position)
        {
            switch (position)
            {
                case JobPosition.Cashier:
                    return PlayerProfile.Pick("Cajero", "Cajera");
                case JobPosition.Manager:
                    return PlayerProfile.Pick("Encargado", "Encargada");
                case JobPosition.Administrator:
                    return PlayerProfile.Pick("Administrador", "Administradora");
                default:
                    return PlayerProfile.Pick("Ayudante", "Ayudante");
            }
        }
    }
}

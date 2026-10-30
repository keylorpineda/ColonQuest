using System;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The first job: the arrow that leads to work in the morning, and the scene at the time clock. The player puts the card in,
    /// the boss says what being late costs, and a short scene of sweeping, washing and serving covers the morning. The docked pay
    /// is worked out by the finance engine when the salary is paid.
    /// </summary>
    public sealed partial class GameUi
    {
        private const string Boss = "Doña Lucía";
        private const int ArrowSize = 48;

        private GameObject workGuide;
        private RectTransform workArrow;
        private Text workGuideText;
        private Station workStation;
        private GameObject homeDoor;

        private const string HomeDoorName = "Door_" + SceneNames.Cuarto;

        private void BuildWorkGuide()
        {
            var card = UiKit.Card(canvas.transform, "WorkGuide", UiKit.Paper, new Vector2(520f, 64f), new Vector2(0.5f, 1f), new Vector2(0f, -24f));
            workGuide = card.parent.gameObject;

            workArrow = UiKit.Rect(card, "Arrow", new Vector2(40f, 40f), new Vector2(0f, 0.5f), new Vector2(36f, 0f));

            // It turns around its own middle, not around the corner it is anchored to.
            workArrow.pivot = new Vector2(0.5f, 0.5f);
            var image = workArrow.gameObject.AddComponent<Image>();
            image.sprite = ArrowSprite();
            image.color = UiKit.Ink;
            image.raycastTarget = false;

            workGuideText = UiKit.Label(card, string.Empty, 26, UiKit.Ink, new Vector2(440f, 50f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), TextAnchor.MiddleCenter);
            workGuide.SetActive(false);
        }

        /// <summary>
        /// A long dart that points up, with a notch in its tail so its direction cannot be misread, drawn here so the arrow does
        /// not depend on a glyph of the font.
        /// </summary>
        private static Sprite ArrowSprite()
        {
            var texture = new Texture2D(ArrowSize, ArrowSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[ArrowSize * ArrowSize];
            for (int y = 0; y < ArrowSize; y++)
            {
                float halfWidth = (ArrowSize - y) * 0.38f;
                for (int x = 0; x < ArrowSize; x++)
                {
                    float side = Mathf.Abs(x + 0.5f - ArrowSize * 0.5f);
                    bool inside = side <= halfWidth && y >= ArrowSize * 0.34f - side * 0.95f;
                    pixels[y * ArrowSize + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, ArrowSize, ArrowSize), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// In the morning, until the player clocks in, it points at the time clock; at night, at the door of the room. Inside a
        /// building, where the arrow means nothing, it only says what to do.
        /// </summary>
        private void TickWorkGuide()
        {
            var clock = session.Loop.Clock;
            bool free = !modal.IsOpen && !phone.IsOpen && !pause.IsOpen && !negotiationPanel.IsOpen && !openingPlaying && !LoadingScreen.IsLoading;
            bool toWork = session.Loop.Job.ExpectedAt(clock.Minutes);
            bool show = free && !session.Loop.IsFinished && (toWork || clock.IsNight);
            workGuide.SetActive(show);
            if (!show)
            {
                return;
            }

            bool late = JobRecord.IsLate(clock.Minutes);
            GameObject target = toWork ? (workStation != null ? workStation.gameObject : null) : homeDoor;
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            if (target == null || hero == null)
            {
                workArrow.gameObject.SetActive(false);
                if (toWork)
                {
                    workGuideText.text = Loc.T(late ? "¡Vas tarde! Salí para la soda" : "Salí para la soda: entrás a las 7:00");
                }
                else
                {
                    bool inRoom = SceneManager.GetActiveScene().name == SceneNames.Cuarto;
                    workGuideText.text = Loc.T(inRoom ? "Ya es tarde: acostate a dormir" : "Ya es tarde: andá a tu cuarto");
                }

                return;
            }

            Vector2 direction = (Vector2)target.transform.position - (Vector2)hero.transform.position;
            workArrow.gameObject.SetActive(true);
            workArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            workGuideText.text = toWork
                ? Loc.T(late ? "¡Vas tarde al trabajo!" : "Al trabajo: entrás a las 7:00")
                : Loc.T("Ya es tarde: andá a tu cuarto");
        }

        private string WorkPrompt()
        {
            var job = session.Loop.Job;
            if (job.WorkedToday)
            {
                return "Ya trabajaste hoy. Tenés el resto del día libre.";
            }

            return job.ExpectedAt(session.Loop.Clock.Minutes) ? "[E]  Marcar la entrada" : "Ya pasó la mañana: hoy no marcaste.";
        }

        /// <summary>The card goes in the clock, the bell rings, the boss answers and the morning goes by at work.</summary>
        private Interaction WorkScene(Station station, Action then)
        {
            var result = session.ClockIn();
            if (result == ClockInResult.AlreadyWorked || result == ClockInResult.AfterMorning)
            {
                return null;
            }

            Vector2 me = Here(station);
            Vector2 machine = Here(station, 0f, 1.5f);
            var scene = new Interaction().WalkTo(me).Face(machine)
                .Pose(PersonPose.Reach, 0.6f).Fly("card", me + new Vector2(0.2f, 0.4f), machine, 0.5f).Sound(SoundEffectKind.Beep)
                .Wait(0.5f).Sound(SoundEffectKind.Chime)
                .Say(Boss, BossLine(result)).Wait(2.6f)
                .Pose(PersonPose.Sweep, 2.6f).Sound(SoundEffectKind.Swish).Wait(1.3f).Sound(SoundEffectKind.Swish).Wait(1.3f)
                .Pose(PersonPose.Wash, 2.2f).Sound(SoundEffectKind.Water).Wait(2.2f)
                .Pose(PersonPose.Handover, 1.2f).Fly("plate", me + new Vector2(0f, 0.3f), machine, 0.6f).Sound(SoundEffectKind.Coin).Wait(1.2f)
                .Pose(PersonPose.None, 0.2f)
                .Apply(then);

            // What counts is the state after clocking in: today may be the day that was missing.
            return session.Loop.CanBePromoted ? AddPromotion(scene) : scene.Say(string.Empty, "Listo por hoy. Tengo el resto del día libre.");
        }

        private static string BossLine(ClockInResult result)
        {
            switch (result)
            {
                case ClockInResult.LateWarned:
                    return "Llegó tarde. Por esta vez queda en llamada de atención.";
                case ClockInResult.LateDocked:
                    return "Otra vez tarde. Este día se lo rebajo de la quincena.";
                case ClockInResult.LateLastWarning:
                    return "Tres veces tarde este mes. Se lo rebajo, y a la próxima se me va.";
                default:
                    return "Buenos días. A tiempo, así me gusta. Hay trabajo.";
            }
        }
    }
}

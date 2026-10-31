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

        private string CurrentHomeScene => (session != null && session.Loop != null && session.Loop.Holdings != null && session.Loop.Holdings.Home != null)
            ? SceneNames.Casa
            : SceneNames.Cuarto;

        private string HomeDoorName => "Door_" + CurrentHomeScene;

        private void BuildWorkGuide()
        {
            var card = UiKit.Card(canvas.transform, "WorkGuide", GamePalette.Hex("#FFF3C9"), new Vector2(540f, 68f), new Vector2(0.5f, 1f), new Vector2(0f, -24f));
            workGuide = card.parent.gameObject;

            UiKit.Round(card, "ArrowBadgeOutline", UiKit.Ink, 48f, new Vector2(0f, 0.5f), new Vector2(14f, 0f));
            UiKit.Round(card, "ArrowBadge", GamePalette.Hex("#FFD23F"), 40f, new Vector2(0f, 0.5f), new Vector2(18f, 0f));
            workArrow = UiKit.Rect(card, "Arrow", new Vector2(28f, 28f), new Vector2(0f, 0.5f), new Vector2(38f, 0f));

            // It turns around its own middle, not around the corner it is anchored to.
            workArrow.pivot = new Vector2(0.5f, 0.5f);
            var image = workArrow.gameObject.AddComponent<Image>();
            image.sprite = ArrowSprite();
            image.color = UiKit.Ink;
            image.raycastTarget = false;

            workGuideText = UiKit.Label(card, string.Empty, 26, UiKit.Ink, new Vector2(440f, 50f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), TextAnchor.MiddleCenter);
            workGuideText.font = UiKit.TitleFont;
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
            string homeScene = CurrentHomeScene;
            if (homeDoor == null)
            {
                homeDoor = GameObject.Find("Door_" + homeScene);
            }

            GameObject target = toWork
                ? (workStation != null ? workStation.gameObject : GameObject.Find("Door_" + SceneNames.Soda))
                : homeDoor;
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
                    bool inHome = SceneManager.GetActiveScene().name == homeScene;
                    string dest = homeScene == SceneNames.Casa ? "casa" : "cuarto";
                    workGuideText.text = Loc.T(inHome ? "Ya es tarde: acostate a dormir" : "Ya es tarde: andá a tu " + dest);
                }

                return;
            }

            Vector2 direction = (Vector2)target.transform.position - (Vector2)hero.transform.position;
            workArrow.gameObject.SetActive(true);
            workArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            if (toWork)
            {
                workGuideText.text = Loc.T(late ? "¡Vas tarde al trabajo!" : "Al trabajo: entrás a las 7:00");
            }
            else
            {
                string dest = homeScene == SceneNames.Casa ? "casa" : "cuarto";
                workGuideText.text = Loc.T("Ya es tarde: andá a tu " + dest);
            }
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
                .Say(Boss, BossLine(result)).Wait(2.6f);

            // The shift itself, at the real furniture of the soda; Space skips it like any scene.
            for (int round = 0; round < ShiftRounds; round++)
            {
                ShiftRound(scene, session.Loop.Job.Position, round);
            }

            scene.WalkTo(me).Face(machine).Pose(PersonPose.None, 0.2f).Apply(then);

            // What counts is the state after clocking in: today may be the day that was missing.
            return session.Loop.CanBePromoted ? AddPromotion(scene) : scene.Say(string.Empty, "Listo por hoy. Tengo el resto del día libre.");
        }

        private const int ShiftRounds = 2;

        // Where the staff stands at the soda (see MapBuilder.Interiors.Soda.cs): in front of the stove, the prep table and the
        // fridge along the back wall, behind the register, and around the two ends of the counter to the dining tables.
        private static readonly Vector2 SodaStove = new Vector2(-3.5f, 3.05f);
        private static readonly Vector2 SodaPrep = new Vector2(0f, 3.15f);
        private static readonly Vector2 SodaFridge = new Vector2(3.6f, 2.95f);
        private static readonly Vector2 SodaPass = new Vector2(0.6f, 1.4f);
        private static readonly Vector2 SodaRegister = new Vector2(-1.8f, 1.4f);

        /// <summary>One round of the shift for the player's position at the soda.</summary>
        private static void ShiftRound(Interaction scene, JobPosition position, int round)
        {
            switch (position)
            {
                case JobPosition.Helper:
                    // The helper washes the dishes at the prep table, takes what is needed from the fridge and cooks.
                    scene.WalkTo(SodaPrep).Face(SodaPrep + Vector2.up).Pose(PersonPose.Wash, 2f).Sound(SoundEffectKind.Water)
                        .Fly("plate", SodaPrep + new Vector2(-0.4f, 0.6f), SodaPrep + new Vector2(0.4f, 0.6f), 0.5f).Wait(0.6f)
                        .WalkTo(SodaFridge).Face(SodaFridge + Vector2.up).Pose(PersonPose.Reach, 0.6f).Sound(SoundEffectKind.Knock).Wait(0.3f)
                        .WalkTo(SodaStove).Face(SodaStove + Vector2.up).Sound(SoundEffectKind.Sizzle).Pose(PersonPose.Cook, 2f)
                        .Steam(SodaStove + new Vector2(-0.4f, 1.2f), 1.8f).Wait(0.4f);
                    break;
                case JobPosition.Cashier:
                    // The cashier also waits tables: a plate from the pass to a table, one side each round.
                    Serve(scene, round);
                    scene.WalkTo(SodaRegister).Face(SodaRegister + Vector2.down).Pose(PersonPose.Count, 1.2f).Sound(SoundEffectKind.Coin);
                    break;
                default:
                    // Managing: the register is counted, a table is served and the papers are checked.
                    scene.WalkTo(SodaRegister).Face(SodaRegister + Vector2.down).Pose(PersonPose.Count, 1.6f).Sound(SoundEffectKind.Coin).Wait(0.3f);
                    Serve(scene, round);
                    scene.Pose(PersonPose.Read, 1.4f).Sound(SoundEffectKind.Swish);
                    break;
            }
        }

        /// <summary>Takes a plate from the pass around the end of the counter to a table, and comes back behind the counter.</summary>
        private static void Serve(Interaction scene, int round)
        {
            float side = round % 2 == 0 ? 1f : -1f;
            var behindEnd = new Vector2(side * 5.4f, 1.4f);
            var frontEnd = new Vector2(side * 5.4f, -0.8f);
            var table = new Vector2(side * 3.9f, -1.4f);
            scene.WalkTo(SodaPass).Face(SodaPrep).Pose(PersonPose.Reach, 0.5f).Fly("plate", SodaPrep + new Vector2(0f, 0.6f), SodaPass + new Vector2(0f, 0.3f), 0.5f)
                .WalkTo(behindEnd).WalkTo(frontEnd).WalkTo(table).Face(new Vector2(side * 5.2f, -2.6f))
                .Pose(PersonPose.Handover, 0.8f).Fly("plate", table + new Vector2(0f, 0.3f), new Vector2(side * 4.75f, -2.6f), 0.5f).Sound(SoundEffectKind.Chime)
                .Wait(0.3f).WalkTo(frontEnd).WalkTo(behindEnd);
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

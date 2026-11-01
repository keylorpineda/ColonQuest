using System.Collections;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The opening of a new life, in the house: the player turns 18, the mother gives a present, the phone brings the first job
    /// and the player says goodbye and walks out to the town. Short lines only, and Space skips it.
    /// </summary>
    public sealed partial class GameUi
    {
        public const string SkipHint = "[Espacio]  Saltar";

        private static readonly Vector2 OpeningStart = new Vector2(-6.0f, 1.8f);

        /// <summary>
        /// Where the mother stands for the present: by the foot of the bed, above the player's way to the bedroom door so the
        /// walk out never has to go through her. The rest of the time she is in the living room.
        /// </summary>
        private static readonly Vector2 OpeningMotherAt = new Vector2(-3.3f, 2.6f);

        private const string Mother = "Mama";

        /// <summary>How close the camera comes during the opening, as a share of its normal view.</summary>
        private const float OpeningCloseUp = 0.68f;

        /// <summary>How far along the way to the player the mother walks to hand over the present.</summary>
        private const float OpeningMotherApproach = 0.45f;

        private CinematicBars openingBars;
        private bool openingPending;
        private bool openingPlaying;

        /// <summary>False in the opening until the box changes hands: the HUD shows an empty pocket until then.</summary>
        private bool openingCashGiven;

        /// <summary>The next time the house loads, the opening plays there.</summary>
        public void PlayOpeningNext()
        {
            openingPending = true;
            HouseVisit.Enter(string.Empty);
        }

        private void TryStartOpening(UnityEngine.SceneManagement.Scene scene)
        {
            if (openingPending && scene.name == SceneNames.Casa)
            {
                openingPending = false;
                StartCoroutine(OpeningSequence());
            }
        }

        private IEnumerator OpeningSequence()
        {
            openingPlaying = true;
            openingCashGiven = false;
            Refresh();
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            if (hero != null)
            {
                hero.transform.position = OpeningStart;
                hero.GetComponent<Rigidbody2D>().position = OpeningStart;
            }

            var mother = GameObject.Find(Mother);
            if (mother != null)
            {
                mother.transform.position = OpeningMotherAt;
            }

            // The loading screen goes first: the story begins when the player can see it.
            while (LoadingScreen.IsLoading)
            {
                yield return null;
            }

            yield return null;
            sceneStation = null;
            bool finished = false;
            var scene = OpeningScene();
            if (scene.Play(() => finished = true))
            {
                while (!finished)
                {
                    yield return null;
                }
            }

            // A skipped scene ends here too: nothing of the film stays on the screen.
            HideOpeningBars();
            if (mother != null)
            {
                mother.transform.position = OpeningMotherAt;
            }

            openingPlaying = false;
            openingCashGiven = true;
            promptCard.SetActive(false);

            // The player stays in the bedroom after the goodbye and walks out on their own.
        }

        private Interaction OpeningScene()
        {
            Vector2 mom = PersonAt(Mother, OpeningMotherAt);
            Vector2 me = OpeningStart;
            Vector2 close = Vector2.Lerp(mom, me, OpeningMotherApproach);
            Vector2 aboveMom = mom + new Vector2(0f, 0.75f);
            Vector2 aboveMe = me + new Vector2(0f, 0.75f);
            string name = PlayerProfile.HasName ? PlayerProfile.Name : PlayerProfile.DefaultName;
            return new Interaction()

                // Like a film: out of black, the bars close in and the camera comes up to the two of them.
                .Fade(Color.black, 0f)
                .Face(mom)
                .Apply(ShowOpeningBars)
                .Zoom(OpeningCloseUp, 2.2f)
                .Fade(Color.clear, 1f)
                .NpcPose(Mother, PersonPose.Wave, 2f).Sound(SoundEffectKind.Chime).Burst("confetti", aboveMom, 18)
                .Say("Mamá", Loc.F("¡Feliz cumpleaños, {0}! Ya 18 años, ¿quién lo diría?", name)).Wait(3.2f)

                // She crosses the room to put the present in the player's hands.
                .NpcWalk(Mother, close, 0.8f).Wait(0.85f)
                .NpcPose(Mother, PersonPose.Handover, 1.2f).Sound(SoundEffectKind.Swish)
                .Fly("box", close, me + new Vector2(0f, 0.2f), 0.7f).Wait(0.75f)
                .HoldProp("box").Pose(PersonPose.Handover, 0.9f)

                // The money is in the box: it reaches the HUD now, even if the scene is skipped.
                .ReleaseProp()
                .Burst("coin", aboveMe, 8)
                .Apply(() =>
                {
                    openingCashGiven = true;
                    Refresh();
                    AudioDirector.Effect(SoundEffectKind.Coin);
                })
                .Say("Mamá", Loc.F("Adentro van {0} para que empecés. Es tu plata: cuidala.", Money.Format(session.State.Cash))).Wait(3f)
                .Pose(PersonPose.Wave, 1.4f)
                .Say(string.Empty, "Gracias, mami. Qué chiva.").Wait(1.2f)
                .Say("Mamá", "Ahora te toca manejar tu propia plata. Con cuidado, ¿oyó?").Wait(3.4f)

                // The phone interrupts: both look, and the camera leans in on the player.
                .Sound(SoundEffectKind.Beep).Wait(0.25f).Sound(SoundEffectKind.Beep)
                .Pose(PersonPose.Pocket, 0.3f).HoldProp("phone").Pose(PersonPose.Type, 0.9f)
                .Zoom(OpeningCloseUp * 0.92f, 1f)
                .Say("Mensaje", "¡Te contrataron! Empezás el lunes a las 7:00 en la soda El Muelle.").Wait(3.4f)
                .Say("Mensaje", "Antes del primer turno: pasá por tu cuarto y sacá el seguro de salud en el hospital.").Wait(3.4f)
                .Pose(PersonPose.Pocket, 0.25f).ReleaseProp().Pose(PersonPose.HandsUp, 1.2f).Sound(SoundEffectKind.Chime).Burst("confetti", aboveMe, 22)
                .Say(string.Empty, "¡Me contrataron, mami!").Wait(1.2f)
                .NpcPose(Mother, PersonPose.Wave, 3f)
                .Say("Mamá", "¡Qué orgullo! Ya tenés tu cuartito alquilado en el barrio. Cuidá esa platica.").Wait(3.2f)
                .NpcWalk(Mother, mom, 0.9f)

                // She steps back, the camera opens and the bars leave: now the player is in charge.
                .Zoom(1f, 1.4f)
                .Apply(HideOpeningBars)
                .Say(string.Empty, "Bueno. A empezar mi vida.").Wait(1.6f);
        }

        private void ShowOpeningBars()
        {
            if (openingBars == null)
            {
                openingBars = CinematicBars.Create(canvas.transform);
            }

            openingBars.Show();
        }

        private void HideOpeningBars()
        {
            if (openingBars != null)
            {
                openingBars.Hide();
            }
        }
    }

    /// <summary>The two black bars of a film, behind the rest of the interface, so a line of dialogue is never covered.</summary>
    public sealed class CinematicBars : MonoBehaviour
    {
        private const float Height = 84f;
        private const float Seconds = 0.7f;

        private RectTransform top;
        private RectTransform bottom;
        private float shown;
        private float goal;

        public static CinematicBars Create(Transform canvas)
        {
            var go = new GameObject("CinematicBars", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            go.transform.SetAsFirstSibling();
            var bars = go.AddComponent<CinematicBars>();
            bars.top = Bar(go.transform, new Vector2(0.5f, 1f));
            bars.bottom = Bar(go.transform, new Vector2(0.5f, 0f));
            bars.Place();
            return bars;
        }

        private static RectTransform Bar(Transform parent, Vector2 anchor)
        {
            var rect = UiKit.Rect(parent, "Bar", new Vector2(6000f, Height), anchor, Vector2.zero);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        public void Show()
        {
            goal = 1f;
        }

        public void Hide()
        {
            goal = 0f;
        }

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, goal, Time.unscaledDeltaTime / Seconds);
            Place();
        }

        /// <summary>From fully off the screen to fully on it, slowing down as they arrive.</summary>
        private void Place()
        {
            float hidden = (1f - Mathf.SmoothStep(0f, 1f, shown)) * Height;
            top.anchoredPosition = new Vector2(0f, hidden);
            bottom.anchoredPosition = new Vector2(0f, -hidden);
        }
    }
}

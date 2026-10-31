using System.Collections;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

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

            openingPlaying = false;
            openingCashGiven = true;
            promptCard.SetActive(false);

            // The player stays in the bedroom after the goodbye and walks out on their own.
        }

        private Interaction OpeningScene()
        {
            Vector2 mom = PersonAt(Mother, OpeningMotherAt);
            Vector2 me = OpeningStart;
            string name = PlayerProfile.HasName ? PlayerProfile.Name : PlayerProfile.DefaultName;
            return new Interaction()
                .Face(mom).Wait(0.6f)
                .NpcPose(Mother, PersonPose.Wave, 2f).Sound(SoundEffectKind.Chime)
                .Say("Mamá", Loc.F("¡Feliz cumpleaños, {0}! Ya 18 años, ¿quién lo diría?", name)).Wait(3.2f)
                .NpcPose(Mother, PersonPose.Handover, 1.2f).Sound(SoundEffectKind.Swish)
                .Fly("box", mom, me + new Vector2(0f, 0.2f), 0.9f).Wait(1f)

                // The money is in the box: it reaches the HUD now, even if the scene is skipped.
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
                .Sound(SoundEffectKind.Beep).Wait(0.25f).Sound(SoundEffectKind.Beep)
                .Pose(PersonPose.Pocket, 0.3f).HoldProp("phone").Pose(PersonPose.Type, 0.9f)
                .Say("Mensaje", "¡Te contrataron! Empezás el lunes a las 7:00 en la soda El Muelle.").Wait(3.4f)
                .Say("Mensaje", "Antes del primer turno: pasá por tu cuarto y sacá el seguro de salud en el hospital.").Wait(3.4f)
                .Pose(PersonPose.Pocket, 0.25f).ReleaseProp().Pose(PersonPose.HandsUp, 1.2f).Sound(SoundEffectKind.Chime)
                .Say(string.Empty, "¡Me contrataron, mami!").Wait(1.2f)
                .NpcPose(Mother, PersonPose.Wave, 3f)
                .Say("Mamá", "¡Qué orgullo! Ya tenés tu cuartito alquilado en el barrio. Cuidá esa platica.").Wait(3.2f)
                .Say(string.Empty, "Bueno. A empezar mi vida.").Wait(1.6f);
        }
    }
}

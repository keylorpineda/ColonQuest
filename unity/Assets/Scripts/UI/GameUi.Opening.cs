using System.Collections;
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
        private const string Mother = "Mama";

        private bool openingPending;
        private bool openingPlaying;

        /// <summary>The next time the house loads, the opening plays there.</summary>
        public void PlayOpeningNext()
        {
            openingPending = true;
            OpeningActor.Show = true;
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
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            if (hero != null)
            {
                hero.transform.position = OpeningStart;
                hero.GetComponent<Rigidbody2D>().position = OpeningStart;
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
            OpeningActor.Show = false;
            promptCard.SetActive(false);
            SceneTransition.Go(SceneNames.Town, SpawnIds.TownFromCasa);
        }

        private static Interaction OpeningScene()
        {
            Vector2 mom = PersonAt(Mother, new Vector2(-4.0f, 1.7f));
            Vector2 me = OpeningStart;
            string name = PlayerProfile.HasName ? PlayerProfile.Name : PlayerProfile.DefaultName;
            return new Interaction()
                .Face(mom).Wait(0.6f)
                .NpcPose(Mother, PersonPose.Wave, 2f).Sound(SoundEffectKind.Chime)
                .Say("Mamá", Loc.F("¡Feliz cumpleaños, {0}! Ya 18 años, ¿quién lo diría?", name)).Wait(3.2f)
                .NpcPose(Mother, PersonPose.Handover, 1.2f).Sound(SoundEffectKind.Swish)
                .Fly("box", mom, me + new Vector2(0f, 0.2f), 0.9f).Wait(1f)
                .Pose(PersonPose.Wave, 1.4f)
                .Say(string.Empty, "Gracias, mami. Qué chiva.").Wait(1.2f)
                .Say("Mamá", "Ahora te toca manejar tu propia plata. Con cuidado, ¿oyó?").Wait(3.4f)
                .Sound(SoundEffectKind.Beep).Wait(0.25f).Sound(SoundEffectKind.Beep)
                .Pose(PersonPose.Type, 1.2f).Fly("phone", me + new Vector2(0.25f, 0.1f), me + new Vector2(0.25f, 0.55f), 0.5f)
                .Say("Mensaje", "¡Te contrataron! Empezás el lunes a las 7:00 en la soda El Muelle.").Wait(3.4f)
                .Pose(PersonPose.HandsUp, 1.2f).Sound(SoundEffectKind.Chime)
                .Say(string.Empty, "¡Me contrataron, mami!").Wait(1.2f)
                .NpcPose(Mother, PersonPose.Wave, 3f)
                .Say("Mamá", "¡Qué orgullo! Andá a conocer el pueblo. Y cuidá esa platica.").Wait(3.2f)
                .WalkTo(new Vector2(-2.9f, 1.6f)).WalkTo(new Vector2(-2.9f, -0.3f))
                .Say(string.Empty, "Bueno. A empezar mi vida.")
                .WalkTo(new Vector2(0f, -3.4f)).Wait(0.8f);
        }
    }
}

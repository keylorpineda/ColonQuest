using System.Collections;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The hospital elevator, on both floors. Its <see cref="Door"/> hands it the trip instead of changing scene at once:
    /// the player presses the button, the chime sounds, the two leaves slide apart, the player steps in and the leaves
    /// close before the scene changes. On the other floor the elevator rings and opens for the player who arrives.
    /// The door logic (target scene and spawn) stays the same.
    /// </summary>
    public sealed class Elevator : MonoBehaviour
    {
        /// <summary>How wide each leaf stays when open: almost folded into the frame.</summary>
        public const float OpenWidth = 0.08f;

        [SerializeField] private string leftLeaf;
        [SerializeField] private string rightLeaf;
        [SerializeField] private Vector2 button;
        [SerializeField] private Vector2 inside;

        /// <summary>Set before the scene changes, so the elevator of the next floor opens for the player.</summary>
        public static bool Arriving { get; private set; }

        public void Configure(string leftLeafName, string rightLeafName, Vector2 buttonAt, Vector2 insideAt)
        {
            leftLeaf = leftLeafName;
            rightLeaf = rightLeafName;
            button = buttonAt;
            inside = insideAt;
        }

        /// <summary>Plays the ride and then goes to the other floor. Without a scene it goes at once, like a plain door.</summary>
        public void Ride(string targetScene, string targetSpawn)
        {
            if (Interaction.Playing || SceneTransition.IsBusy)
            {
                return;
            }

            var scene = new Interaction()
                .Face(button).Pose(PersonPose.Reach, 0.5f).Sound(SoundEffectKind.Beep)
                .Wait(0.3f).Sound(SoundEffectKind.Chime)
                .Swing(leftLeaf, OpenWidth, 0.5f).Swing(rightLeaf, OpenWidth, 0.5f).Wait(0.55f)
                .WalkTo(inside)
                .Swing(leftLeaf, 1f, 0.45f).Swing(rightLeaf, 1f, 0.45f).Wait(0.5f)
                .Apply(() => Go(targetScene, targetSpawn));
            if (!scene.Play())
            {
                Go(targetScene, targetSpawn);
            }
        }

        private static void Go(string targetScene, string targetSpawn)
        {
            Arriving = true;
            SceneTransition.Go(targetScene, targetSpawn);
        }

        private void Start()
        {
            if (Arriving)
            {
                Arriving = false;
                StartCoroutine(Arrive());
            }
        }

        /// <summary>The player just came up or down: chime, the leaves open and close again behind them.</summary>
        private IEnumerator Arrive()
        {
            yield return new WaitForSeconds(0.3f);
            AudioDirector.Effect(SoundEffectKind.Chime);
            new Interaction()
                .Swing(leftLeaf, OpenWidth, 0.45f).Swing(rightLeaf, OpenWidth, 0.45f).Wait(0.9f)
                .Swing(leftLeaf, 1f, 0.45f).Swing(rightLeaf, 1f, 0.45f).Wait(0.5f)
                .Play();
        }
    }
}

using ColonQuest.Game;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// Placed in the first scene. It creates the game session and the overlay once; both then live
    /// across scene changes, so coming back to the town does not start a second copy.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static GameUi ui;

        private void Awake()
        {
            var session = GameSession.Create();
            if (ui == null)
            {
                ui = GameUi.Create(session);
            }
        }
    }
}

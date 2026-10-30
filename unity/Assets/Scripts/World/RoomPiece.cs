using System;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A piece of furniture of the rented room: it is only there once it was bought. The mattress on the floor works the other
    /// way around and goes away when the bed arrives. The world knows nothing about money, so the session says what is owned.
    /// </summary>
    public sealed class RoomPiece : MonoBehaviour
    {
        /// <summary>Tells whether the player owns the piece with this name.</summary>
        public static Func<string, bool> Owned { get; set; }

        [SerializeField] private string piece;
        [SerializeField] private bool onlyWhileMissing;

        public void Configure(string pieceName, bool whileMissing)
        {
            piece = pieceName;
            onlyWhileMissing = whileMissing;
        }

        /// <summary>The scene is built with every piece; the ones that do not apply leave before the first frame.</summary>
        private void Awake()
        {
            bool owned = Owned != null && Owned(piece);
            gameObject.SetActive(owned != onlyWhileMissing);
        }
    }
}

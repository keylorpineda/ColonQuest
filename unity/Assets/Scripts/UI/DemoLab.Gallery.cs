using System.Collections.Generic;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>The twenty pictures Muse Image made for the game. A click shows one big.</summary>
    public sealed partial class DemoLab
    {
        private string bigPicture;
        private Text galleryCount;

        private void DrawGallery()
        {
            DrawHeading("Arte del juego", "Muse Image", "Retratos, escenas y postales, todos con el mismo estilo fijo. Ninguna imagen lleva texto ni números: las cifras las dibuja el juego encima, así que nunca salen mal escritas.");

            var keys = new List<string>(DemoReadout.Portraits);
            keys.AddRange(DemoReadout.Scenes);
            keys.AddRange(PostcardKeys.All);

            if (bigPicture != null)
            {
                Picture(workspace, bigPicture, new Vector2(Width, 640f), TopLeft, new Vector2(Left, -170f));
                Plain(bigPicture, 24, GameUiColors.Muted, new Vector2(Width - 320f, 60f), new Vector2(Left, -822f), TextAnchor.MiddleLeft);
                Action("Ver todas", GameUiColors.Yellow, new Vector2(300f, 60f), new Vector2(Left + Width - 300f, -822f), () =>
                {
                    bigPicture = null;
                    Redraw();
                });
                return;
            }

            const int columns = 5;
            const float width = 188f;
            const float height = 146f;
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                var position = new Vector2(Left + i % columns * (width + 8f), -170f - i / columns * (height + 8f));
                var image = Picture(workspace, key, new Vector2(width, height), TopLeft, position);
                var frame = image.transform.parent.GetComponent<Image>();
                frame.raycastTarget = true;
                frame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    AudioDirector.Click();
                    bigPicture = key;
                    Redraw();
                });
            }

            galleryCount = Plain(string.Empty, 22, GameUiColors.Muted, new Vector2(Width, 40f), new Vector2(Left, -790f), TextAnchor.MiddleCenter);
            UpdateGalleryCount();
        }

        private void UpdateGalleryCount()
        {
            if (galleryCount != null)
            {
                galleryCount.text = pictures.Count > 0 ? Loc.F("Faltan {0} por llegar del servidor. Tocá una para verla en grande.", pictures.Count) : Loc.T("Tocá una para verla en grande.");
            }
        }
    }
}

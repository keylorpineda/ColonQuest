using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Game;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// The pictures the backend serves (portraits of the characters, scenes of the events, the end postcard). Each one is
    /// asked for once, kept as a sprite, and used the next time a window opens. A picture that never arrives is simply not
    /// drawn: the game keeps its colored faces and plain windows, and never waits for the network to draw anything.
    /// </summary>
    public static class RemoteImages
    {
        /// <summary>The portraits the backend pregenerates, by the name of the character as <see cref="UiKit.PortraitKey"/> writes it.</summary>
        private static readonly Dictionary<string, string> Portraits = new Dictionary<string, string>
        {
            { "don_rafa", "npc_don_rafa_mecanico" },
            { "kevin", "npc_kevin_vendedor" },
            { "dona_marta", "npc_dona_marta_vendedora" },
            { "prestamista", "npc_prestamista_gota_a_gota" },
            { "promotor", "npc_amigo_piramide" },
            { "amigo_piramide", "npc_amigo_piramide" }
        };

        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> asked = new HashSet<string>();
        private static bool prefetched;

        /// <summary>The portrait of a character, if the backend has one for that name and it is already here.</summary>
        public static Sprite Portrait(string characterName)
        {
            return Portraits.TryGetValue(UiKit.PortraitKey(characterName), out var key) ? Get(key) : null;
        }

        /// <summary>The picture if it is already here. When it is not, it is requested and null is returned for now.</summary>
        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (sprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            Request(key);
            return null;
        }

        /// <summary>Like <see cref="Get"/>, without waiting for a game to be running: the demo lab shows pictures from the menu.</summary>
        public static Sprite Fetch(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (sprites.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            Download(key);
            return null;
        }

        /// <summary>Asks for the portraits and the postcard once there is a session, so they are ready when a window needs them.</summary>
        public static void Prefetch()
        {
            if (prefetched || !ApiClient.Instance.Service.HasSession)
            {
                return;
            }

            prefetched = true;
            foreach (var key in new HashSet<string>(Portraits.Values))
            {
                Request(key);
            }

            foreach (var key in PostcardKeys.All)
            {
                Request(key);
            }
        }

        private static void Request(string key)
        {
            if (ApiClient.Instance.Service.HasSession)
            {
                Download(key);
            }
        }

        private static void Download(string key)
        {
            if (!asked.Add(key))
            {
                return;
            }

            ApiClient.Instance.RequestImage(key, result =>
            {
                if (!result.Ok)
                {
                    // Allow a later try, for example when the backend was asleep.
                    asked.Remove(key);
                    return;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(result.Value))
                {
                    Object.Destroy(texture);
                    return;
                }

                sprites[key] = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            });
        }
    }
}

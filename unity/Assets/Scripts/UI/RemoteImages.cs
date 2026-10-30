using System.Collections.Generic;
using ColonQuest.Api;
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
        /// <summary>The illustration of the end postcard. It has no text or numbers: the figures are written over it by the game.</summary>
        public const string PostcardKey = "scene_postal_tu_ano";

        public static readonly string[] PortraitKeys =
        {
            "npc_don_rafa", "npc_kevin", "npc_dona_marta", "npc_prestamista", "npc_amigo_piramide"
        };

        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> asked = new HashSet<string>();
        private static bool prefetched;

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

        /// <summary>Asks for the portraits and the postcard once there is a session, so they are ready when a window needs them.</summary>
        public static void Prefetch()
        {
            if (prefetched || !ApiClient.Instance.Service.HasSession)
            {
                return;
            }

            prefetched = true;
            foreach (var key in PortraitKeys)
            {
                Request(key);
            }

            Request(PostcardKey);
        }

        private static void Request(string key)
        {
            if (!ApiClient.Instance.Service.HasSession || !asked.Add(key))
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

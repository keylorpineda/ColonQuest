using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The small objects of the animated interactions, drawn from plain shapes at run time (no art files): bills, a card, a
    /// receipt, a plate, a phone, steam, the Z of the sleeper and the glow of a screen. Each one flies, floats or flickers
    /// by itself and destroys itself when it is done.
    /// </summary>
    internal static class InteractionProps
    {
        private const int PropOrder = 3000;
        private const int OverlayOrder = 32000;

        private static Sprite square;
        private static Sprite circle;
        private static SpriteRenderer overlay;

        public static Color OverlayColor => overlay != null ? overlay.color : Color.clear;

        public static Sprite Square()
        {
            if (square == null)
            {
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                square = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }

            return square;
        }

        public static Sprite Circle()
        {
            if (circle == null)
            {
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size * 0.5f - d)));
                    }
                }

                texture.Apply();
                circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return circle;
        }

        // ---------------------------------------------------------------- the shapes

        private static int TopLayer()
        {
            var layers = SortingLayer.layers;
            return layers.Length > 0 ? layers[layers.Length - 1].id : 0;
        }

        private static SpriteRenderer Piece(Transform parent, bool round, Vector2 at, Vector2 size, Color color, float degrees, int order)
        {
            var go = new GameObject(round ? "Disc" : "Block");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = round ? Circle() : Square();
            renderer.color = color;
            renderer.sortingLayerID = TopLayer();
            renderer.sortingOrder = PropOrder + order;
            return renderer;
        }

        /// <summary>One of the objects by name: bills, paper, card, plate, receipt, coin, phone, stamp, key, drop, food.</summary>
        public static GameObject Make(string kind, Vector2 at)
        {
            var root = new GameObject("Prop_" + kind);
            root.transform.position = at;
            var t = root.transform;
            switch (kind)
            {
                case "bills":
                    for (int i = 0; i < 3; i++)
                    {
                        Piece(t, false, new Vector2(i * 0.04f - 0.04f, i * 0.025f), new Vector2(0.3f, 0.16f), new Color(0.37f, 0.75f, 0.45f), (i - 1) * 8f, i * 2);
                        Piece(t, true, new Vector2(i * 0.04f - 0.04f, i * 0.025f), new Vector2(0.08f, 0.08f), new Color(0.2f, 0.5f, 0.3f), 0f, i * 2 + 1);
                    }

                    break;
                case "paper":
                    Piece(t, false, Vector2.zero, new Vector2(0.24f, 0.32f), new Color(0.97f, 0.97f, 0.94f), 0f, 0);
                    for (int i = 0; i < 3; i++)
                    {
                        Piece(t, false, new Vector2(0f, 0.08f - i * 0.07f), new Vector2(0.16f, 0.02f), new Color(0.55f, 0.58f, 0.66f), 0f, 1);
                    }

                    break;
                case "receipt":
                    Piece(t, false, Vector2.zero, new Vector2(0.15f, 0.36f), new Color(0.98f, 0.98f, 0.96f), 0f, 0);
                    for (int i = 0; i < 4; i++)
                    {
                        Piece(t, false, new Vector2(0f, 0.12f - i * 0.07f), new Vector2(0.09f, 0.015f), new Color(0.5f, 0.52f, 0.6f), 0f, 1);
                    }

                    break;
                case "card":
                    Piece(t, false, Vector2.zero, new Vector2(0.32f, 0.2f), new Color(0.22f, 0.45f, 0.82f), 0f, 0);
                    Piece(t, false, new Vector2(-0.08f, 0.02f), new Vector2(0.07f, 0.05f), new Color(1f, 0.82f, 0.25f), 0f, 1);
                    break;
                case "plate":
                    Piece(t, true, Vector2.zero, new Vector2(0.4f, 0.28f), new Color(0.96f, 0.96f, 0.98f), 0f, 0);
                    Piece(t, true, new Vector2(0f, 0.01f), new Vector2(0.24f, 0.16f), new Color(0.82f, 0.55f, 0.28f), 0f, 1);
                    Piece(t, true, new Vector2(0.06f, 0.03f), new Vector2(0.1f, 0.08f), new Color(0.4f, 0.7f, 0.3f), 0f, 2);
                    break;
                case "coin":
                    Piece(t, true, Vector2.zero, new Vector2(0.16f, 0.16f), new Color(1f, 0.82f, 0.25f), 0f, 0);
                    Piece(t, true, Vector2.zero, new Vector2(0.09f, 0.09f), new Color(0.85f, 0.62f, 0.1f), 0f, 1);
                    break;
                case "phone":
                    Piece(t, false, Vector2.zero, new Vector2(0.14f, 0.24f), new Color(0.12f, 0.14f, 0.22f), 0f, 0);
                    Piece(t, false, new Vector2(0f, 0.01f), new Vector2(0.1f, 0.17f), new Color(0.45f, 0.8f, 1f), 0f, 1);
                    break;
                case "stamp":
                    Piece(t, false, Vector2.zero, new Vector2(0.26f, 0.16f), new Color(0.85f, 0.2f, 0.25f, 0.9f), -8f, 0);
                    Piece(t, false, Vector2.zero, new Vector2(0.2f, 0.1f), new Color(0.98f, 0.9f, 0.9f, 0.9f), -8f, 1);
                    break;
                case "box":
                    Piece(t, false, Vector2.zero, new Vector2(0.38f, 0.3f), new Color(0.74f, 0.55f, 0.32f), 0f, 0);
                    Piece(t, false, Vector2.zero, new Vector2(0.06f, 0.3f), new Color(0.9f, 0.8f, 0.55f), 0f, 1);
                    break;
                case "key":
                    Piece(t, true, new Vector2(-0.07f, 0f), new Vector2(0.1f, 0.1f), new Color(1f, 0.82f, 0.25f), 0f, 0);
                    Piece(t, false, new Vector2(0.04f, 0f), new Vector2(0.14f, 0.03f), new Color(1f, 0.82f, 0.25f), 0f, 0);
                    break;
                case "drop":
                    Piece(t, true, Vector2.zero, new Vector2(0.06f, 0.09f), new Color(0.55f, 0.8f, 1f, 0.85f), 0f, 0);
                    break;
                default:
                    Piece(t, true, Vector2.zero, new Vector2(0.14f, 0.14f), Color.white, 0f, 0);
                    break;
            }

            return root;
        }

        /// <summary>An object that travels from a point to another and vanishes. A bill fan spins a little on the way.</summary>
        public static void Fly(string kind, Vector2 from, Vector2 to, float seconds)
        {
            var prop = Make(kind, from);
            prop.AddComponent<PropMover>().Begin(from, to, Mathf.Max(0.1f, seconds), 0f, true, 0.06f);
        }

        /// <summary>White puffs that rise and fade, one every little while.</summary>
        public static void Steam(Vector2 at, float seconds)
        {
            int puffs = Mathf.Clamp(Mathf.RoundToInt(seconds * 4f), 3, 14);
            for (int i = 0; i < puffs; i++)
            {
                var puff = new GameObject("Steam");
                puff.transform.position = at;
                float size = 0.22f + (i % 3) * 0.06f;
                Piece(puff.transform, true, Vector2.zero, new Vector2(size, size), new Color(0.96f, 0.97f, 1f, 0.7f), 0f, 0);
                float dx = ((i % 5) - 2) * 0.05f;
                puff.AddComponent<PropMover>().Begin(at, at + new Vector2(dx, 0.9f), 1.1f, i * (seconds / puffs), true, 0.04f);
            }
        }

        /// <summary>Three Z letters, one bigger than the other, that float up and to the side.</summary>
        public static void Zzz(Vector2 at, float seconds)
        {
            int letters = 3;
            for (int i = 0; i < letters; i++)
            {
                var z = new GameObject("Z");
                z.transform.position = at;
                float s = 0.14f + i * 0.06f;
                var color = new Color(0.86f, 0.92f, 1f, 0.95f);
                Piece(z.transform, false, new Vector2(0f, s * 0.5f), new Vector2(s, s * 0.16f), color, 0f, 0);
                Piece(z.transform, false, new Vector2(0f, -s * 0.5f), new Vector2(s, s * 0.16f), color, 0f, 0);
                Piece(z.transform, false, Vector2.zero, new Vector2(s * 1.3f, s * 0.16f), color, 40f, 0);
                var to = at + new Vector2(0.25f + i * 0.2f, 0.7f + i * 0.35f);
                z.AddComponent<PropMover>().Begin(at, to, Mathf.Max(1.2f, seconds * 0.5f), i * Mathf.Max(0.3f, seconds / 4f), true, 0.03f);
            }
        }

        /// <summary>A rectangle that changes brightness quickly: the light of a television.</summary>
        public static void Flicker(Vector2 at, Vector2 size, Color color, float seconds)
        {
            var go = new GameObject("Screen");
            go.transform.position = at;
            var renderer = Piece(go.transform, false, Vector2.zero, size, color, 0f, 0);
            go.AddComponent<PropFlicker>().Begin(renderer, color, seconds);
        }

        // ---------------------------------------------------------------- the screen overlay

        private static SpriteRenderer Overlay()
        {
            if (overlay == null)
            {
                var go = new GameObject("InteractionOverlay");
                var camera = Camera.main;
                if (camera != null)
                {
                    go.transform.SetParent(camera.transform, false);
                    go.transform.localPosition = new Vector3(0f, 0f, 5f);
                }

                go.transform.localScale = new Vector3(400f, 400f, 1f);
                overlay = go.AddComponent<SpriteRenderer>();
                overlay.sprite = Square();
                overlay.color = Color.clear;
                overlay.sortingLayerID = TopLayer();
                overlay.sortingOrder = OverlayOrder;
            }

            return overlay;
        }

        public static void SetOverlay(Color color)
        {
            Overlay().color = color;
        }

        public static void ClearOverlay()
        {
            if (overlay != null)
            {
                overlay.color = Color.clear;
            }
        }
    }

    /// <summary>Moves a prop from a point to another, with a wobble, after a delay, and fades it out at the end.</summary>
    internal sealed class PropMover : MonoBehaviour
    {
        private Vector2 from;
        private Vector2 to;
        private float seconds;
        private float delay;
        private float wobble;
        private bool fade;
        private float age;
        private SpriteRenderer[] renderers;
        private float[] alphas;

        public void Begin(Vector2 start, Vector2 end, float duration, float wait, bool fadeOut, float shake)
        {
            from = start;
            to = end;
            seconds = duration;
            delay = wait;
            fade = fadeOut;
            wobble = shake;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            alphas = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                alphas[i] = renderers[i].color.a;
            }

            SetVisible(delay <= 0f);
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age < delay)
            {
                return;
            }

            float k = Mathf.Clamp01((age - delay) / seconds);
            SetVisible(true);
            Vector2 position = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
            position.x += Mathf.Sin(k * 14f) * wobble;
            transform.position = position;

            if (fade)
            {
                float alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                for (int i = 0; i < renderers.Length; i++)
                {
                    var color = renderers[i].color;
                    color.a = alphas[i] * alpha;
                    renderers[i].color = color;
                }
            }

            if (k >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Makes a shape change its brightness many times a second for a while, then removes it.</summary>
    internal sealed class PropFlicker : MonoBehaviour
    {
        private SpriteRenderer renderer2d;
        private Color color;
        private float seconds;
        private float age;

        public void Begin(SpriteRenderer target, Color tint, float duration)
        {
            renderer2d = target;
            color = tint;
            seconds = duration;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= seconds)
            {
                Destroy(gameObject);
                return;
            }

            float level = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(age * 11f) * Mathf.Cos(age * 4.3f));
            var tinted = color;
            tinted.a = color.a * level;
            renderer2d.color = tinted;
        }
    }
}

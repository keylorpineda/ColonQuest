using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    public enum ReactionKind
    {
        Alert,
        Money,
        Love,
        Angry
    }

    /// <summary>
    /// Displays a temporary reaction bubble over the player's head (Alert, Money, Love, Angry)
    /// popping in with a bounce (0 -> 1.15 -> 1 in 0.25s), staying 1.6s and fading out.
    /// </summary>
    public sealed class ReactionBubble : MonoBehaviour
    {
        private static Sprite circle;
        private static Sprite square;

        public static void Show(ReactionKind kind)
        {
            var player = GameObject.FindWithTag(Station.PlayerTag);
            if (player == null) return;

            var existing = player.transform.Find("ReactionBubble");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var bubble = new GameObject("ReactionBubble").AddComponent<ReactionBubble>();
            bubble.transform.SetParent(player.transform, false);
            bubble.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            bubble.Build(kind);
            bubble.StartCoroutine(bubble.Animate());
        }

        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        private static Sprite GetCircle()
        {
            if (circle != null) return circle;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d)));
                }
            }
            texture.Apply();
            circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circle;
        }

        private static Sprite GetSquare()
        {
            if (square != null) return square;
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
            texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return square;
        }

        private SpriteRenderer AddPart(string name, Sprite spr, Vector2 offset, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)offset;
            go.transform.localScale = (Vector3)size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = color;
            sr.sortingLayerName = "World";
            sr.sortingOrder = 950 + order;
            renderers.Add(sr);
            return sr;
        }

        private void Build(ReactionKind kind)
        {
            var cir = GetCircle();
            var sqr = GetSquare();
            var outline = new Color(0.11f, 0.13f, 0.22f, 1f);

            // Bubble background with outline
            AddPart("Outline", cir, Vector2.zero, Vector2.one * 0.52f, outline, 0);
            AddPart("Bg", cir, Vector2.zero, Vector2.one * 0.44f, Color.white, 1);

            switch (kind)
            {
                case ReactionKind.Alert:
                    AddPart("ExclBar", sqr, new Vector2(0f, 0.04f), new Vector2(0.06f, 0.18f), new Color(0.91f, 0.31f, 0.36f), 2);
                    AddPart("ExclDot", cir, new Vector2(0f, -0.10f), Vector2.one * 0.07f, new Color(0.91f, 0.31f, 0.36f), 2);
                    break;

                case ReactionKind.Money:
                    var gold = new Color(1f, 0.82f, 0.25f);
                    AddPart("Coin", cir, Vector2.zero, Vector2.one * 0.26f, gold, 2);
                    AddPart("Bar", sqr, Vector2.zero, new Vector2(0.05f, 0.22f), outline, 3);
                    AddPart("HBar", sqr, Vector2.zero, new Vector2(0.15f, 0.05f), outline, 3);
                    break;

                case ReactionKind.Love:
                    var red = new Color(1f, 0.32f, 0.42f);
                    AddPart("HeartL", cir, new Vector2(-0.05f, 0.03f), Vector2.one * 0.14f, red, 2);
                    AddPart("HeartR", cir, new Vector2(0.05f, 0.03f), Vector2.one * 0.14f, red, 2);
                    var bot = AddPart("HeartBot", sqr, new Vector2(0f, -0.04f), Vector2.one * 0.12f, red, 2);
                    bot.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;

                case ReactionKind.Angry:
                    var angryRed = new Color(0.95f, 0.25f, 0.25f);
                    var bar1 = AddPart("Cross1", sqr, Vector2.zero, new Vector2(0.06f, 0.24f), angryRed, 2);
                    bar1.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    var bar2 = AddPart("Cross2", sqr, Vector2.zero, new Vector2(0.06f, 0.24f), angryRed, 2);
                    bar2.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                    break;
            }
        }

        private IEnumerator Animate()
        {
            Vector3 basePos = transform.localPosition;
            transform.localScale = Vector3.zero;

            // Pop in (0 -> 1.15 -> 1.0 in 0.25s)
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.15f);
                transform.localScale = Vector3.one * (k * 1.15f);
                yield return null;
            }
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01((t - 0.15f) / 0.1f);
                transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);
                yield return null;
            }

            transform.localScale = Vector3.one;

            // Hold
            yield return new WaitForSeconds(0.95f);

            // Fade out over 0.4s and drift slightly up
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(t / 0.4f);
                transform.localPosition = basePos + new Vector3(0f, (t / 0.4f) * 0.12f, 0f);
                foreach (var sr in renderers)
                {
                    if (sr != null)
                    {
                        var c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

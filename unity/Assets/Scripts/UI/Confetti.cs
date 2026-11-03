using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>A burst of paper that falls down the whole screen when something good happens, and then cleans itself up.</summary>
    public sealed class Confetti : MonoBehaviour
    {
        private const int Pieces = 70;
        private const float Life = 2.6f;

        private static readonly Color[] Colors =
        {
            new Color(0.95f, 0.35f, 0.4f), new Color(1f, 0.82f, 0.25f), new Color(0.3f, 0.75f, 0.95f),
            new Color(0.4f, 0.85f, 0.5f), new Color(0.72f, 0.5f, 0.95f)
        };

        private RectTransform[] pieces;
        private Vector2[] speeds;
        private float[] spins;
        private float age;

        public static void Burst(Transform canvas)
        {
            var go = new GameObject("Confetti", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Confetti>().Build(rect);
        }

        private void Build(RectTransform area)
        {
            pieces = new RectTransform[Pieces];
            speeds = new Vector2[Pieces];
            spins = new float[Pieces];
            float width = area.rect.width > 0f ? area.rect.width : 1920f;
            float height = area.rect.height > 0f ? area.rect.height : 1080f;
            for (int i = 0; i < Pieces; i++)
            {
                var piece = UiKit.Rect(area, "Piece", new Vector2(Random.Range(10f, 18f), Random.Range(16f, 26f)), new Vector2(0.5f, 0.5f),
                    new Vector2(Random.Range(-width * 0.5f, width * 0.5f), height * 0.5f + Random.Range(0f, 320f)));
                var image = piece.gameObject.AddComponent<Image>();
                image.color = Colors[i % Colors.Length];
                image.raycastTarget = false;
                pieces[i] = piece;
                speeds[i] = new Vector2(Random.Range(-70f, 70f), -Random.Range(420f, 760f));
                spins[i] = Random.Range(-360f, 360f);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            age += dt;
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].anchoredPosition += speeds[i] * dt;
                pieces[i].Rotate(0f, 0f, spins[i] * dt);
            }

            if (age >= Life)
            {
                Destroy(gameObject);
            }
        }
    }
}

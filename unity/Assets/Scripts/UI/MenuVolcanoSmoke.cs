using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Animated smoke billowing from the volcano crater in the main menu background.
    /// Puffs rise, expand and drift to the right with the mountain breeze, dissipating smoothly into the sky.
    /// </summary>
    public sealed class MenuVolcanoSmoke : MonoBehaviour
    {
        private const int MaxPuffs = 16;
        private const float CraterX = -248f;
        private const float CraterY = 343f;
        private const float LifetimeMin = 5.2f;
        private const float LifetimeMax = 7.0f;
        private const float SpawnInterval = 0.42f;

        private static Sprite smokeSprite;

        private sealed class SmokePuff
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public float ScaleStart;
            public float ScaleEnd;
            public float MaxAlpha;
            public float WaveFreq;
            public float WaveAmp;
            public float RotSpeed;
            public float Seed;
            public bool Active;
        }

        private readonly List<SmokePuff> pool = new List<SmokePuff>();
        private float spawnTimer;

        public static MenuVolcanoSmoke Create(Transform parent)
        {
            var host = new GameObject("VolcanoSmoke", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(CraterX, CraterY);
            rect.sizeDelta = new Vector2(400f, 500f);

            return host.AddComponent<MenuVolcanoSmoke>();
        }

        private void Awake()
        {
            EnsureSprite();
            BuildPool();
            Prewarm(6.5f);
        }

        private static void EnsureSprite()
        {
            if (smokeSprite != null)
            {
                return;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / maxRadius);
                    float alpha = Mathf.SmoothStep(1f, 0f, t);
                    alpha = Mathf.Pow(alpha, 1.35f);
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            smokeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildPool()
        {
            for (int i = 0; i < MaxPuffs; i++)
            {
                var go = new GameObject("Puff_" + i, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(48f, 48f);

                var img = go.AddComponent<Image>();
                img.sprite = smokeSprite;
                img.raycastTarget = false;
                img.color = new Color(0.92f, 0.89f, 0.83f, 0f);

                go.SetActive(false);

                pool.Add(new SmokePuff
                {
                    Rect = rect,
                    Image = img,
                    Active = false
                });
            }
        }

        private void Prewarm(float duration)
        {
            const float step = 0.1f;
            int steps = Mathf.RoundToInt(duration / step);
            for (int i = 0; i < steps; i++)
            {
                Advance(step);
            }
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void Advance(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            spawnTimer += dt;
            if (spawnTimer >= SpawnInterval)
            {
                spawnTimer -= SpawnInterval;
                SpawnPuff();
            }

            for (int i = 0; i < pool.Count; i++)
            {
                var puff = pool[i];
                if (!puff.Active)
                {
                    continue;
                }

                puff.Age += dt;
                if (puff.Age >= puff.Lifetime)
                {
                    puff.Active = false;
                    puff.Rect.gameObject.SetActive(false);
                    continue;
                }

                float progress = puff.Age / puff.Lifetime;

                // Drift upwards and to the right with mountain wind
                puff.Position += puff.Velocity * dt;

                // Subtle lateral wave
                float wave = Mathf.Sin(puff.Age * puff.WaveFreq + puff.Seed) * puff.WaveAmp;
                var currentPos = puff.Position + new Vector2(wave, 0f);
                puff.Rect.anchoredPosition = currentPos;

                // Expand as smoke billows outward into the sky
                float scale = Mathf.Lerp(puff.ScaleStart, puff.ScaleEnd, Mathf.SmoothStep(0f, 1f, progress));
                puff.Rect.localScale = new Vector3(scale, scale, 1f);

                // Gentle rotation
                puff.Rect.localEulerAngles = new Vector3(0f, 0f, puff.Rect.localEulerAngles.z + puff.RotSpeed * dt);

                // Soft fade in, lingering body, gentle dispersion
                float alpha;
                if (progress < 0.18f)
                {
                    alpha = Mathf.SmoothStep(0f, puff.MaxAlpha, progress / 0.18f);
                }
                else
                {
                    alpha = Mathf.SmoothStep(puff.MaxAlpha, 0f, (progress - 0.18f) / 0.82f);
                }

                var color = puff.Image.color;
                color.a = alpha;
                puff.Image.color = color;
            }
        }

        private void SpawnPuff()
        {
            SmokePuff available = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].Active)
                {
                    available = pool[i];
                    break;
                }
            }

            if (available == null)
            {
                return;
            }

            available.Active = true;
            available.Age = 0f;
            available.Lifetime = Random.Range(LifetimeMin, LifetimeMax);
            available.ScaleStart = Random.Range(0.35f, 0.5f);
            available.ScaleEnd = Random.Range(2.0f, 2.7f);
            available.MaxAlpha = Random.Range(0.32f, 0.42f);
            available.WaveFreq = Random.Range(1.4f, 2.2f);
            available.WaveAmp = Random.Range(5f, 9f);
            available.RotSpeed = Random.Range(-12f, 12f);
            available.Seed = Random.Range(0f, 10f);

            // Crater origin with tiny variation
            available.Position = new Vector2(Random.Range(-5f, 5f), Random.Range(-3f, 3f));
            available.Velocity = new Vector2(Random.Range(18f, 30f), Random.Range(36f, 50f));

            available.Rect.anchoredPosition = available.Position;
            available.Rect.localScale = new Vector3(available.ScaleStart, available.ScaleStart, 1f);
            available.Rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

            var c = available.Image.color;
            c.a = 0f;
            available.Image.color = c;

            available.Rect.gameObject.SetActive(true);
        }
    }
}

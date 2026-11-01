using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Brings the keel-billed toucan on the main menu backdrop to life:
    /// - Banknote held crosswise in the beak flutters continuously in the tropical breeze.
    /// - Dynamic eye blinking with turquoise eye ring and natural timing (occasional double blinks).
    /// - Subtle head bobbing, breathing motion, and curious tilts toward menu buttons.
    /// - Perch landing bounce when the menu opens, settling smoothly with branch elasticity.
    /// - Gentle ambient mountain leaves drifting across the foreground.
    /// </summary>
    public sealed class MenuToucanMotion : MonoBehaviour
    {
        // Coordinates in 1920x1080 canvas centered on KeyArt:
        // KeyArt is 2048x1152, scale factor is 1920/2048 = 0.9375
        // Beak tip: x=1581, y=605 -> anchored: (1581-1024)*0.9375 = +522f, -(605-576)*0.9375 = -27f
        private const float BanknoteX = 522f;
        private const float BanknoteY = -27f;

        // Eye: x=1631, y=620 -> anchored: (1631-1024)*0.9375 = +569f, -(620-576)*0.9375 = -41f
        private const float EyeX = 569f;
        private const float EyeY = -41f;

        private static Sprite banknoteSprite;
        private static Sprite eyeSprite;
        private static Sprite eyelidSprite;
        private static Sprite leafSprite;

        private RectTransform rootGroup;
        private RectTransform headGroup;
        private RectTransform banknoteRect;
        private RectTransform eyeRect;
        private RectTransform eyelidRect;

        // Blinking
        private float nextBlinkAt;
        private float blinkTimer;
        private bool isBlinking;
        private bool doubleBlinkPending;

        // Arrival bounce
        private float arrivalTime = -10f;
        private bool isArriving;

        // Head tilt
        private float nextCuriousTiltAt;
        private float targetTiltAngle;
        private float currentTiltAngle;

        // Ambient leaves
        private sealed class Leaf
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public float SpeedX;
            public float SpeedY;
            public float WaveFreq;
            public float WaveAmp;
            public float RotSpeed;
            public float Seed;
            public float Lifetime;
            public float Age;
        }

        private readonly List<Leaf> leaves = new List<Leaf>();

        public static MenuToucanMotion Create(Transform parent)
        {
            var host = new GameObject("ToucanMotion", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1920f, 1080f);

            return host.AddComponent<MenuToucanMotion>();
        }

        private void Awake()
        {
            EnsureSprites();
            BuildRig();
            BuildAmbientLeaves();
            ScheduleNextBlink(Random.Range(1.5f, 3.5f));
            ScheduleNextTilt(Random.Range(2.5f, 5.0f));
        }

        public void PlayArrival()
        {
            arrivalTime = Time.unscaledTime;
            isArriving = true;
        }

        private static void EnsureSprites()
        {
            if (banknoteSprite != null)
            {
                return;
            }

            banknoteSprite = GenerateBanknoteSprite();
            eyeSprite = GenerateEyeSprite();
            eyelidSprite = GenerateEyelidSprite();
            leafSprite = GenerateLeafSprite();
        }

        private static Sprite GenerateBanknoteSprite()
        {
            const int width = 120;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];

            Color32 borderInk = new Color32(23, 32, 30, 255);
            Color32 darkGreen = new Color32(45, 95, 62, 255);
            Color32 lightGreen = new Color32(115, 178, 120, 255);
            Color32 cream = new Color32(248, 241, 220, 255);
            Color32 sealInk = new Color32(35, 75, 50, 255);

            float cx = (width - 1) * 0.5f;
            float cy = (height - 1) * 0.5f;
            float rx = 26f;
            float ry = 17f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 c;
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        c = borderInk;
                    }
                    else if (x <= 3 || x >= width - 4 || y <= 3 || y >= height - 4)
                    {
                        c = darkGreen;
                    }
                    else if (x == 5 || x == width - 6 || y == 5 || y == height - 6)
                    {
                        c = darkGreen;
                    }
                    else
                    {
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        float dist = dx * dx + dy * dy;
                        if (dist <= 0.65f)
                        {
                            c = cream;
                        }
                        else if (dist <= 0.85f)
                        {
                            c = sealInk;
                        }
                        else if (dist <= 1.05f)
                        {
                            c = darkGreen;
                        }
                        else
                        {
                            bool pattern = ((x + y) % 8 == 0) || ((x - y) % 8 == 0);
                            c = pattern ? darkGreen : lightGreen;
                        }
                    }
                    pixels[y * width + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            // Pivot at (0.9f, 0.5f) where the beak grips the right edge of the bill!
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.9f, 0.5f), 100f);
        }

        private static Sprite GenerateEyeSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.46f;

            Color32 turquoise = new Color32(82, 188, 222, 255);
            Color32 pupil = new Color32(18, 24, 25, 255);
            Color32 glint = new Color32(255, 255, 255, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / radius;
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                    else if (dist > 0.62f)
                    {
                        pixels[y * size + x] = turquoise;
                    }
                    else if (x >= center - 2 && x <= center && y >= center + 1 && y <= center + 3)
                    {
                        pixels[y * size + x] = glint;
                    }
                    else
                    {
                        pixels[y * size + x] = pupil;
                    }
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite GenerateEyelidSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.46f;

            Color32 lidColor = new Color32(40, 52, 48, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / radius;
                    if (dist <= 1.0f)
                    {
                        pixels[y * size + x] = lidColor;
                    }
                    else
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 1f), 100f);
        }

        private static Sprite GenerateLeafSprite()
        {
            const int w = 24;
            const int h = 12;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[w * h];
            Color32 leafGreen = new Color32(88, 148, 76, 210);
            float cx = (w - 1) * 0.5f;
            float cy = (h - 1) * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / (w * 0.45f);
                    float dy = (y - cy) / (h * 0.45f);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        pixels[y * w + x] = leafGreen;
                    }
                    else
                    {
                        pixels[y * w + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildRig()
        {
            // Root group for the entire toucan (settle bounce moves the whole bird)
            var rootGo = new GameObject("ToucanRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            rootGroup = (RectTransform)rootGo.transform;
            rootGroup.anchorMin = new Vector2(0.5f, 0.5f);
            rootGroup.anchorMax = new Vector2(0.5f, 0.5f);
            rootGroup.pivot = new Vector2(0.5f, 0.5f);
            rootGroup.anchoredPosition = Vector2.zero;
            rootGroup.sizeDelta = new Vector2(1920f, 1080f);

            // Head group (bobs with breathing and tilts curiously)
            var headGo = new GameObject("ToucanHead", typeof(RectTransform));
            headGo.transform.SetParent(rootGroup, false);
            headGroup = (RectTransform)headGo.transform;
            headGroup.anchorMin = new Vector2(0.5f, 0.5f);
            headGroup.anchorMax = new Vector2(0.5f, 0.5f);
            // Pivot around neck / base of head
            headGroup.pivot = new Vector2(0.5f, 0.5f);
            headGroup.anchoredPosition = Vector2.zero;
            headGroup.sizeDelta = new Vector2(1920f, 1080f);

            // Banknote overlay
            var billGo = new GameObject("Banknote", typeof(RectTransform));
            billGo.transform.SetParent(headGroup, false);
            banknoteRect = (RectTransform)billGo.transform;
            banknoteRect.anchorMin = new Vector2(0.5f, 0.5f);
            banknoteRect.anchorMax = new Vector2(0.5f, 0.5f);
            banknoteRect.pivot = new Vector2(0.9f, 0.5f);
            banknoteRect.anchoredPosition = new Vector2(BanknoteX, BanknoteY);
            banknoteRect.sizeDelta = new Vector2(115f, 62f);

            var billImg = billGo.AddComponent<Image>();
            billImg.sprite = banknoteSprite;
            billImg.raycastTarget = false;

            // Eye container
            var eyeGo = new GameObject("Eye", typeof(RectTransform));
            eyeGo.transform.SetParent(headGroup, false);
            eyeRect = (RectTransform)eyeGo.transform;
            eyeRect.anchorMin = new Vector2(0.5f, 0.5f);
            eyeRect.anchorMax = new Vector2(0.5f, 0.5f);
            eyeRect.pivot = new Vector2(0.5f, 0.5f);
            eyeRect.anchoredPosition = new Vector2(EyeX, EyeY);
            eyeRect.sizeDelta = new Vector2(30f, 18f);

            var eyeImg = eyeGo.AddComponent<Image>();
            eyeImg.sprite = eyeSprite;
            eyeImg.raycastTarget = false;

            // Eyelid overlay
            var lidGo = new GameObject("Eyelid", typeof(RectTransform));
            lidGo.transform.SetParent(eyeRect, false);
            eyelidRect = (RectTransform)lidGo.transform;
            eyelidRect.anchorMin = new Vector2(0.5f, 1f);
            eyelidRect.anchorMax = new Vector2(0.5f, 1f);
            eyelidRect.pivot = new Vector2(0.5f, 1f);
            eyelidRect.anchoredPosition = Vector2.zero;
            eyelidRect.sizeDelta = new Vector2(30f, 18f);
            eyelidRect.localScale = new Vector3(1f, 0f, 1f);

            var lidImg = lidGo.AddComponent<Image>();
            lidImg.sprite = eyelidSprite;
            lidImg.raycastTarget = false;
        }

        private void BuildAmbientLeaves()
        {
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("BreezeLeaf_" + i, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(24f, 12f);

                var img = go.AddComponent<Image>();
                img.sprite = leafSprite;
                img.raycastTarget = false;

                var leaf = new Leaf
                {
                    Rect = rect,
                    Image = img,
                    Lifetime = Random.Range(6.0f, 9.5f),
                    Age = Random.Range(0f, 8.0f),
                    SpeedX = Random.Range(-45f, -80f),
                    SpeedY = Random.Range(-10f, -24f),
                    WaveFreq = Random.Range(1.8f, 2.8f),
                    WaveAmp = Random.Range(12f, 20f),
                    RotSpeed = Random.Range(-35f, 35f),
                    Seed = Random.Range(0f, 20f),
                    Position = new Vector2(Random.Range(300f, 950f), Random.Range(-200f, 400f))
                };
                leaves.Add(leaf);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            UpdateArrivalBounce(now);
            UpdateBreathingAndTilt(now, dt);
            UpdateBanknoteFlutter(now);
            UpdateEyeBlink(now, dt);
            UpdateAmbientLeaves(now, dt);
        }

        private void UpdateArrivalBounce(float now)
        {
            if (!isArriving)
            {
                rootGroup.anchoredPosition = Vector2.zero;
                return;
            }

            float elapsed = now - arrivalTime;
            if (elapsed < 0f)
            {
                return;
            }

            // Damped spring oscillation simulating branch bounce upon toucan landing
            if (elapsed < 1.6f)
            {
                float decay = Mathf.Exp(-elapsed * 3.8f);
                float bounce = -18f * decay * Mathf.Cos(elapsed * 10f);
                rootGroup.anchoredPosition = new Vector2(0f, bounce);
            }
            else
            {
                rootGroup.anchoredPosition = Vector2.zero;
                isArriving = false;
            }
        }

        private void UpdateBreathingAndTilt(float now, float dt)
        {
            // Breathing: gentle vertical bob
            float breath = Mathf.Sin(now * 1.6f) * 2.2f;

            // Curious head tilt logic
            if (now >= nextCuriousTiltAt)
            {
                ScheduleNextTilt(Random.Range(3.5f, 6.5f));
                targetTiltAngle = Random.value < 0.6f ? Random.Range(-3.5f, 3.5f) : 0f;
            }

            // If hovering over buttons or keyboard active, tilt beak toward left menu buttons
            bool menuFocused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null;
            if (menuFocused)
            {
                targetTiltAngle = -3.8f;
            }

            currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, Mathf.Clamp01(dt * 4.5f));

            headGroup.anchoredPosition = new Vector2(0f, breath);
            headGroup.localEulerAngles = new Vector3(0f, 0f, currentTiltAngle);
        }

        private void UpdateBanknoteFlutter(float now)
        {
            // Continuous fast wave flutter of banknote paper held in beak
            float fastWave = Mathf.Sin(now * 10.5f) * 3.8f;
            float microFlutter = Mathf.Sin(now * 24f) * 1.6f;

            // Periodic wind gust lift
            float gustCycle = Mathf.Sin(now * 0.85f);
            float gust = gustCycle > 0.7f ? (gustCycle - 0.7f) / 0.3f * 7f : 0f;

            // Extra flutter during landing arrival
            float arrivalFlutter = 0f;
            if (isArriving)
            {
                float elapsed = now - arrivalTime;
                arrivalFlutter = Mathf.Sin(elapsed * 28f) * Mathf.Exp(-elapsed * 2.5f) * 12f;
            }

            float totalAngle = fastWave + microFlutter + gust + arrivalFlutter;
            banknoteRect.localEulerAngles = new Vector3(0f, 0f, totalAngle);

            // Subtle paper flex (squash & stretch in breeze)
            float paperFlex = 1.0f + Mathf.Sin(now * 12f) * 0.035f;
            banknoteRect.localScale = new Vector3(paperFlex, 2f - paperFlex, 1f);
        }

        private void UpdateEyeBlink(float now, float dt)
        {
            if (!isBlinking)
            {
                if (now >= nextBlinkAt)
                {
                    isBlinking = true;
                    blinkTimer = 0f;
                }
                else
                {
                    eyelidRect.localScale = new Vector3(1f, 0f, 1f);
                    return;
                }
            }

            const float blinkDuration = 0.13f;
            blinkTimer += dt;
            float t = blinkTimer / blinkDuration;

            if (t <= 1f)
            {
                // Smooth down and up eyelid motion
                float open = Mathf.Sin(t * Mathf.PI);
                eyelidRect.localScale = new Vector3(1f, open, 1f);
            }
            else
            {
                eyelidRect.localScale = new Vector3(1f, 0f, 1f);
                isBlinking = false;

                if (doubleBlinkPending)
                {
                    doubleBlinkPending = false;
                    ScheduleNextBlink(0.16f); // Quick follow-up blink
                }
                else
                {
                    // 25% chance of setting up a double blink
                    doubleBlinkPending = Random.value < 0.25f;
                    ScheduleNextBlink(Random.Range(2.8f, 5.5f));
                }
            }
        }

        private void ScheduleNextBlink(float delay)
        {
            nextBlinkAt = Time.unscaledTime + delay;
        }

        private void ScheduleNextTilt(float delay)
        {
            nextCuriousTiltAt = Time.unscaledTime + delay;
        }

        private void UpdateAmbientLeaves(float now, float dt)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                var leaf = leaves[i];
                leaf.Age += dt;
                if (leaf.Age >= leaf.Lifetime || leaf.Position.x < -1000f || leaf.Position.y < -550f)
                {
                    // Respawn from upper-right
                    leaf.Age = 0f;
                    leaf.Position = new Vector2(Random.Range(700f, 1050f), Random.Range(100f, 480f));
                }

                leaf.Position += new Vector2(leaf.SpeedX * dt, leaf.SpeedY * dt);
                float wave = Mathf.Sin(leaf.Age * leaf.WaveFreq + leaf.Seed) * leaf.WaveAmp;
                leaf.Rect.anchoredPosition = leaf.Position + new Vector2(0f, wave);

                float rot = leaf.Rect.localEulerAngles.z + leaf.RotSpeed * dt;
                leaf.Rect.localEulerAngles = new Vector3(0f, 0f, rot);

                // Fade in near edge, fade out as it exits
                float progress = leaf.Age / leaf.Lifetime;
                float alpha = progress < 0.2f ? progress / 0.2f : (progress > 0.8f ? (1f - progress) / 0.2f : 1f);
                var c = leaf.Image.color;
                c.a = alpha * 0.85f;
                leaf.Image.color = c;
            }
        }
    }
}

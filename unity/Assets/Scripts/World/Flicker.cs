using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Makes a sprite's alpha wander around its starting value with smooth noise.
    /// A high speed reads as a flickering screen, a low speed as twinkling or breathing.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Flicker : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.2f;
        [SerializeField] private float speed = 3f;
        [SerializeField] private float seed;
        [SerializeField] private bool nightOnly;

        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        public bool NightOnly => nightOnly;

        public void Configure(float flickerAmplitude, float flickerSpeed, float noiseSeed, bool isNightOnly = false)
        {
            amplitude = flickerAmplitude;
            speed = flickerSpeed;
            seed = noiseSeed;
            nightOnly = isNightOnly;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            if (nightOnly)
            {
                if (!DayTint.IsNight)
                {
                    Color offColor = baseColor;
                    offColor.a = 0f;
                    spriteRenderer.color = offColor;
                    return;
                }
            }

            float noise = Mathf.PerlinNoise(Time.time * speed, seed);
            Color color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a * Mathf.Lerp(1f - amplitude, 1f + amplitude, noise));
            spriteRenderer.color = color;
        }
    }
}

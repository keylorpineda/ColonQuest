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

        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        public void Configure(float flickerAmplitude, float flickerSpeed, float noiseSeed)
        {
            amplitude = flickerAmplitude;
            speed = flickerSpeed;
            seed = noiseSeed;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(Time.time * speed, seed);
            Color color = baseColor;
            color.a = Mathf.Clamp01(baseColor.a * Mathf.Lerp(1f - amplitude, 1f + amplitude, noise));
            spriteRenderer.color = color;
        }
    }
}

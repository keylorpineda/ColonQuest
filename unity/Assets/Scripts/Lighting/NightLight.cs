using ColonQuest.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ColonQuest.Lighting
{
    /// <summary>
    /// A lamp, a porch light or a lit window: a 2D point light that fades in when night falls
    /// (<see cref="DayTint.IsNight"/>) and out in the morning, with a faint living flicker. While it is off the light is
    /// disabled, so the daytime town pays nothing for it.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class NightLight : MonoBehaviour
    {
        /// <summary>Seconds it takes to come fully on or off.</summary>
        public const float FadeSeconds = 1.5f;

        [SerializeField] private float onIntensity = 0.9f;
        [SerializeField] private float flicker = 0.06f;
        [SerializeField] private float seed;

        private Light2D lamp;
        private float level;

        public void Configure(float intensity, float flickerAmount, float noiseSeed)
        {
            onIntensity = Mathf.Max(0f, intensity);
            flicker = Mathf.Clamp01(flickerAmount);
            seed = noiseSeed;
        }

        private void Awake()
        {
            lamp = GetComponent<Light2D>();
            level = DayTint.IsNight ? 1f : 0f;
            Apply();
        }

        private void Update()
        {
            level = Mathf.MoveTowards(level, DayTint.IsNight ? 1f : 0f, Time.deltaTime / FadeSeconds);
            Apply();
        }

        private void Apply()
        {
            bool on = level > 0.001f;
            if (lamp.enabled != on)
            {
                lamp.enabled = on;
            }

            if (on)
            {
                lamp.intensity = DayCycle.LampIntensity(level, onIntensity, flicker, Mathf.PerlinNoise(Time.time * 1.7f, seed));
            }
        }
    }
}

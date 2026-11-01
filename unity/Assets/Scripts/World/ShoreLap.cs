using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The foam line on the beach: it rushes up the sand and slides back, brighter as it arrives.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ShoreLap : MonoBehaviour
    {
        [SerializeField] private float reach = 0.16f;
        [SerializeField] private float period = 6f;
        [SerializeField] private float phase;
        [SerializeField] private float fade = 0.45f;

        private SpriteRenderer spriteRenderer;
        private Vector3 rest;
        private Color baseColor;

        public void Configure(float upReach, float seconds, float cyclePhase, float alphaFade)
        {
            reach = upReach;
            period = seconds;
            phase = cyclePhase;
            fade = Mathf.Clamp01(alphaFade);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rest = transform.localPosition;
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            float w = WaterMath.Lap(Time.time, period, phase);
            transform.localPosition = rest + new Vector3(0f, w * reach, 0f);
            var color = baseColor;
            color.a = baseColor.a * (1f - fade * (0.5f - 0.5f * w));
            spriteRenderer.color = color;
        }
    }
}

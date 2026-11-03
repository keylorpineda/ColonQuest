using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A line of swell on the sea: it rolls toward the beach and fades away before it gets there, then starts again
    /// further out. Many of them with different offsets keep the sea moving.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class Swell : MonoBehaviour
    {
        [SerializeField] private float travel = 5f;
        [SerializeField] private float period = 9f;
        [SerializeField] private float offset;

        private SpriteRenderer spriteRenderer;
        private Vector3 start;
        private Color baseColor;

        public void Configure(float distance, float seconds, float cycleOffset)
        {
            travel = distance;
            period = seconds;
            offset = cycleOffset;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            start = transform.localPosition;
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            float t = WaterMath.Cycle(Time.time, period, offset);
            transform.localPosition = start + new Vector3(0f, t * travel, 0f);
            var color = baseColor;
            color.a = baseColor.a * WaterMath.FadeInOut(t);
            spriteRenderer.color = color;
        }
    }
}

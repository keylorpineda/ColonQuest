using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The glowing ring drawn on a station. It pulses gently all the time and
    /// pulses harder while the player stands inside the station.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StationMarker : MonoBehaviour
    {
        [SerializeField] private Station station;
        [SerializeField] private float pulseSpeed = 3f;

        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private Color baseColor;

        public void Bind(Station target)
        {
            station = target;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            bool active = station != null && station.IsPlayerInside;

            transform.localScale = baseScale * (1f + wave * (active ? 0.25f : 0.12f));

            Color color = baseColor;
            color.a = Mathf.Lerp(active ? 0.9f : 0.45f, 1f, wave);
            spriteRenderer.color = color;
        }
    }
}

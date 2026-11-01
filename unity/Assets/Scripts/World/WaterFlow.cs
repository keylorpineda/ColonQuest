using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A light streak the river carries downstream. It wraps around and fades near both ends of its run.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WaterFlow : MonoBehaviour
    {
        [SerializeField] private float speed = 0.7f;
        [SerializeField] private float minY;
        [SerializeField] private float maxY;
        [SerializeField] private float fade = 2f;

        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        /// <summary>Moves down (south) at <paramref name="unitsPerSecond"/> between two local heights.</summary>
        public void Configure(float unitsPerSecond, float bottom, float top, float fadeLength)
        {
            speed = Mathf.Abs(unitsPerSecond);
            minY = Mathf.Min(bottom, top);
            maxY = Mathf.Max(bottom, top);
            fade = Mathf.Max(0f, fadeLength);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
        }

        private void Update()
        {
            if (maxY - minY <= 0.01f)
            {
                return;
            }

            var position = transform.localPosition;
            position.y -= speed * Time.deltaTime;
            if (position.y < minY)
            {
                position.y += maxY - minY;
            }

            transform.localPosition = position;
            var color = baseColor;
            color.a = baseColor.a * WaterMath.EdgeFade(position.y, minY, maxY, fade);
            spriteRenderer.color = color;
        }
    }
}

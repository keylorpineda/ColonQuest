using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Little puffs of dust (or sand) kicked up by the player's steps on the beach, the dirt paths and the street.
    /// A small pool of sprites lives apart from the player, so a puff stays where the foot left it.
    /// </summary>
    public sealed class FootDust : MonoBehaviour
    {
        private const int PoolSize = 8;
        private const float StrideLength = 0.85f;
        private const float Life = 0.55f;

        [SerializeField] private Sprite puffSprite;
        [SerializeField] private string sortingLayer = "Floor";
        [SerializeField] private int sortingOrder = 200;
        [SerializeField] private float footOffset = -0.45f;

        private SpriteRenderer[] puffs;
        private float[] born;
        private Vector3[] starts;
        private Color[] colors;
        private int next;
        private Vector3 lastPosition;
        private float travelled;
        private Transform holder;

        public void Configure(Sprite sprite, string layer, int order, float feet)
        {
            puffSprite = sprite;
            sortingLayer = layer;
            sortingOrder = order;
            footOffset = feet;
        }

        /// <summary>The color of a puff on this ground, or a clear color where the ground makes no dust.</summary>
        public static Color PuffColor(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Sand:
                    return new Color(0.95f, 0.88f, 0.68f, 0.55f);
                case SurfaceKind.Road:
                case SurfaceKind.Sidewalk:
                    return new Color(0.85f, 0.83f, 0.8f, 0.28f);
                default:
                    return Color.clear;
            }
        }

        private void Awake()
        {
            holder = new GameObject("FootDust").transform;
            puffs = new SpriteRenderer[PoolSize];
            born = new float[PoolSize];
            starts = new Vector3[PoolSize];
            colors = new Color[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Puff");
                go.transform.SetParent(holder, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = puffSprite;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                puffs[i] = renderer;
                born[i] = -10f;
            }

            lastPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (holder != null)
            {
                Destroy(holder.gameObject);
            }
        }

        private void Update()
        {
            if (puffSprite == null)
            {
                return;
            }

            Vector3 position = transform.position;
            float step = Vector2.Distance(position, lastPosition);
            lastPosition = position;

            // A jump to another place (a spawn or a scene start) is not a walk.
            if (step < 1f)
            {
                travelled += step;
            }

            if (travelled >= StrideLength)
            {
                travelled = 0f;
                var color = PuffColor(Surfaces.At(SceneManager.GetActiveScene().name, position));
                if (color.a > 0f)
                {
                    Spawn(position + new Vector3(Random.Range(-0.08f, 0.08f), footOffset, 0f), color);
                }
            }

            float now = Time.time;
            for (int i = 0; i < PoolSize; i++)
            {
                var puff = puffs[i];
                if (!puff.enabled)
                {
                    continue;
                }

                float t = (now - born[i]) / Life;
                if (t >= 1f)
                {
                    puff.enabled = false;
                    continue;
                }

                float size = Mathf.Lerp(0.18f, 0.5f, 1f - (1f - t) * (1f - t));
                puff.transform.position = starts[i] + new Vector3(0f, t * 0.12f, 0f);
                puff.transform.localScale = new Vector3(size, size * 0.6f, 1f);
                var c = colors[i];
                c.a *= 1f - t;
                puff.color = c;
            }
        }

        private void Spawn(Vector3 at, Color color)
        {
            int index = next;
            next = (next + 1) % PoolSize;
            starts[index] = at;
            colors[index] = color;
            born[index] = Time.time;
            var puff = puffs[index];
            puff.transform.position = at;
            puff.transform.localScale = Vector3.zero;
            puff.color = new Color(color.r, color.g, color.b, 0f);
            puff.enabled = true;
        }
    }
}

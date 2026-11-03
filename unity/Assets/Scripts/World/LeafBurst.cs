using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Pure math for a falling leaf, so it can be tested without the engine.</summary>
    public static class LeafMath
    {
        /// <summary>How far down a leaf falls over its whole life, in world units.</summary>
        public const float FallDistance = 1.2f;

        /// <summary>Side-to-side sway of a falling leaf, in world units.</summary>
        public const float SwayAmplitude = 0.25f;

        /// <summary>Where a leaf is, relative to where it started, at <paramref name="t01"/> of its life. <paramref name="side"/> is -1 or 1.</summary>
        public static Vector2 Offset(float t01, float side)
        {
            float t = Mathf.Clamp01(t01);
            return new Vector2(Mathf.Sin(t * 6f) * SwayAmplitude * side, -FallDistance * t);
        }

        /// <summary>Opacity of a leaf: fully visible, then fading out over the last 30% of its life.</summary>
        public static float Alpha(float t01)
        {
            float t = Mathf.Clamp01(t01);
            return t <= 0.7f ? 1f : Mathf.Clamp01((1f - t) / 0.3f);
        }
    }

    /// <summary>
    /// A few leaves that drift down when a plant is shaken. A fixed pool of sprites lives in a separate object,
    /// so the leaves do not move with the plant and nothing is created while playing.
    /// </summary>
    public sealed class LeafBurst : MonoBehaviour
    {
        private const int PoolSize = 6;
        private const float Life = 1.6f;
        private const float SpawnRadius = 0.6f;

        [SerializeField] private Sprite leafSprite;
        [SerializeField] private Color leafColor = Color.white;
        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int sortingOrder = 900;

        private SpriteRenderer[] leaves;
        private float[] born;
        private float[] sides;
        private float[] spins;
        private Vector3[] starts;
        private int next;
        private Transform holder;

        public void Configure(Sprite sprite, Color color, string layer, int order)
        {
            leafSprite = sprite;
            leafColor = color;
            sortingLayer = layer;
            sortingOrder = order;
        }

        /// <summary>Starts <paramref name="count"/> leaves falling around this object.</summary>
        public void Burst(int count)
        {
            if (leaves == null || leafSprite == null)
            {
                return;
            }

            int n = Mathf.Clamp(count, 0, PoolSize);
            for (int i = 0; i < n; i++)
            {
                int index = next;
                next = (next + 1) % PoolSize;
                Vector2 offset = Random.insideUnitCircle * SpawnRadius;
                starts[index] = transform.position + new Vector3(offset.x, offset.y, 0f);
                sides[index] = Random.value < 0.5f ? -1f : 1f;
                spins[index] = Random.Range(60f, 220f) * sides[index];
                born[index] = Time.time;
                leaves[index].enabled = true;
            }
        }

        private void Awake()
        {
            holder = new GameObject("LeafBurst").transform;
            leaves = new SpriteRenderer[PoolSize];
            born = new float[PoolSize];
            sides = new float[PoolSize];
            spins = new float[PoolSize];
            starts = new Vector3[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Leaf");
                go.transform.SetParent(holder, false);
                go.transform.localScale = Vector3.one * 0.22f;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = leafSprite;
                renderer.color = leafColor;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                leaves[i] = renderer;
                born[i] = -10f;
            }
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
            float now = Time.time;
            for (int i = 0; i < PoolSize; i++)
            {
                var leaf = leaves[i];
                if (!leaf.enabled)
                {
                    continue;
                }

                float t = (now - born[i]) / Life;
                if (t >= 1f)
                {
                    leaf.enabled = false;
                    continue;
                }

                Vector2 offset = LeafMath.Offset(t, sides[i]);
                leaf.transform.position = starts[i] + new Vector3(offset.x, offset.y, 0f);
                leaf.transform.rotation = Quaternion.Euler(0f, 0f, spins[i] * t * Life);
                var c = leafColor;
                c.a *= LeafMath.Alpha(t);
                leaf.color = c;
            }
        }
    }
}
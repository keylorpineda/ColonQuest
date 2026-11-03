using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Rings that spread on the water: now and then somewhere on the sea, and at the player's feet while they walk
    /// along the edge of the sea, the river or the pond. A small pool of sprites is reused, nothing is created after
    /// the start.
    /// </summary>
    public sealed class RippleField : MonoBehaviour
    {
        private const int PoolSize = 16;
        private const int DropPoolSize = 12;
        private const int DropsPerStep = 3;
        private const float DropLife = 0.4f;
        private const float DropHeight = 0.3f;
        private const float DropSize = 0.08f;
        private const float MaxRingAlpha = 0.8f;
        private const float PlayerRippleEvery = 0.3f;

        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Rect area;
        [SerializeField] private Rect avoid;
        [SerializeField] private float every = 0.7f;
        [SerializeField] private float life = 2.2f;
        [SerializeField] private string sortingLayer = "Default";
        [SerializeField] private int sortingOrder;
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Sprite dropSprite;

        private SpriteRenderer[] rings;
        private SpriteRenderer[] drops;
        private float[] dropBorn;
        private Vector2[] dropStart;
        private float[] dropSide;
        private int nextDrop;
        private float[] born;
        private float[] growTo;
        private int next;
        private float nextRandom;
        private float nextPlayerRipple;
        private Transform player;
        private Vector3 lastPlayerPosition;
        private float nextPlayerSearch;

        /// <summary>Rings on <paramref name="seaArea"/> (world units) except over <paramref name="keepClear"/>, like the pier.</summary>
        public void Configure(Sprite ring, Rect seaArea, Rect keepClear, string layer, int order, bool ripplesUnderPlayer)
        {
            ringSprite = ring;
            area = seaArea;
            avoid = keepClear;
            sortingLayer = layer;
            sortingOrder = order;
            followPlayer = ripplesUnderPlayer;
        }

        /// <summary>Seconds between random rings (each one varies 60 to 140 percent).</summary>
        public void ConfigureRate(float secondsBetweenRings)
        {
            every = Mathf.Max(0.05f, secondsBetweenRings);
        }

        /// <summary>Sprite for the drops that jump when the player steps in the water. Without it only rings are made.</summary>
        public void ConfigureDrops(Sprite drop)
        {
            dropSprite = drop;
        }

        private void Awake()
        {
            rings = new SpriteRenderer[PoolSize];
            born = new float[PoolSize];
            growTo = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Ripple");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = ringSprite;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.color = new Color(1f, 1f, 1f, 0f);
                renderer.enabled = false;
                rings[i] = renderer;
                born[i] = -100f;
            }

            drops = new SpriteRenderer[DropPoolSize];
            dropBorn = new float[DropPoolSize];
            dropStart = new Vector2[DropPoolSize];
            dropSide = new float[DropPoolSize];
            for (int i = 0; i < DropPoolSize; i++)
            {
                var go = new GameObject("Drop");
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(DropSize, DropSize, 1f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = dropSprite;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder + 1;
                renderer.color = new Color(1f, 1f, 1f, 0f);
                renderer.enabled = false;
                drops[i] = renderer;
                dropBorn[i] = -100f;
            }

            nextRandom = Time.time + Random.Range(0.2f, every);
        }

        private void Update()
        {
            if (ringSprite == null)
            {
                return;
            }

            float now = Time.time;
            if (now >= nextRandom && area.width > 0f && area.height > 0f)
            {
                nextRandom = now + every * Random.Range(0.6f, 1.4f);
                var point = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                if (!avoid.Contains(point))
                {
                    Spawn(point, Random.Range(0.9f, 1.5f));
                }
            }

            if (followPlayer)
            {
                FollowPlayer(now);
            }

            for (int i = 0; i < PoolSize; i++)
            {
                var ring = rings[i];
                if (!ring.enabled)
                {
                    continue;
                }

                float t = (now - born[i]) / life;
                if (t >= 1f)
                {
                    ring.enabled = false;
                    continue;
                }

                WaterMath.Ripple(t, 0.2f, growTo[i], out float size, out float alpha);
                ring.transform.localScale = new Vector3(size, size * 0.42f, 1f);
                ring.color = new Color(1f, 1f, 1f, alpha * MaxRingAlpha);
            }

            UpdateDrops(now);
        }

        private void UpdateDrops(float now)
        {
            for (int i = 0; i < DropPoolSize; i++)
            {
                var drop = drops[i];
                if (!drop.enabled)
                {
                    continue;
                }

                float t = (now - dropBorn[i]) / DropLife;
                if (t >= 1f)
                {
                    drop.enabled = false;
                    continue;
                }

                Vector2 offset = WaterMath.SplashOffset(t, dropSide[i], DropHeight, 0.18f);
                drop.transform.position = new Vector3(dropStart[i].x + offset.x, dropStart[i].y + offset.y, transform.position.z);
                drop.color = new Color(1f, 1f, 1f, 0.8f * WaterMath.SplashAlpha(t));
            }
        }

        private void FollowPlayer(float now)
        {
            if (player == null)
            {
                if (now < nextPlayerSearch)
                {
                    return;
                }

                nextPlayerSearch = now + 1f;
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                {
                    return;
                }

                player = found.transform;
                lastPlayerPosition = player.position;
            }

            if (now < nextPlayerRipple)
            {
                return;
            }

            nextPlayerRipple = now + PlayerRippleEvery;
            Vector3 position = player.position;
            bool moved = (position - lastPlayerPosition).sqrMagnitude > 0.0004f;
            lastPlayerPosition = position;
            if (!moved)
            {
                return;
            }

            var hint = WorldHints.Check(SceneManager.GetActiveScene().name, position);
            if (hint.Kind != HintKind.Water)
            {
                return;
            }

            // The water is on the other side of "away", just past the feet.
            Vector2 feet = (Vector2)position + new Vector2(0f, -0.4f);
            Vector2 splashAt = feet - hint.Away * 0.55f;
            Spawn(splashAt, 0.9f);
            if (dropSprite != null)
            {
                for (int i = 0; i < DropsPerStep; i++)
                {
                    SpawnDrop(splashAt + new Vector2(Random.Range(-0.1f, 0.1f), 0f), i - 1f + Random.Range(-0.3f, 0.3f));
                }
            }
        }

        private void SpawnDrop(Vector2 worldPoint, float side)
        {
            int index = nextDrop;
            nextDrop = (nextDrop + 1) % DropPoolSize;
            dropStart[index] = worldPoint;
            dropSide[index] = side;
            dropBorn[index] = Time.time;
            var drop = drops[index];
            drop.sprite = dropSprite;
            drop.transform.position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
            drop.color = new Color(1f, 1f, 1f, 0.8f);
            drop.enabled = true;
        }

        /// <summary>A ring at a point of the water, for something that falls in (a fish landing, a stone).</summary>
        public void SpawnAt(Vector2 worldPoint, float size)
        {
            if (rings != null && ringSprite != null)
            {
                Spawn(worldPoint, size);
            }
        }

        private void Spawn(Vector2 worldPoint, float size)
        {
            int index = next;
            next = (next + 1) % PoolSize;
            var ring = rings[index];
            ring.transform.position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);
            ring.transform.localScale = Vector3.zero;
            ring.color = new Color(1f, 1f, 1f, 0f);
            ring.enabled = true;
            born[index] = Time.time;
            growTo[index] = size;
        }
    }
}

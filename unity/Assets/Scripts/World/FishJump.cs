using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A small fish that now and then jumps out of a pond in an arc and drops back in, with a ring of water where it
    /// leaves and where it lands. It is hidden while it swims.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FishJump : MonoBehaviour
    {
        private const float JumpSeconds = 0.75f;

        [SerializeField] private Rect water;
        [SerializeField] private RippleField ripples;

        private SpriteRenderer body;
        private float nextJump;
        private float started = -10f;
        private Vector2 from;
        private Vector2 to;
        private bool splashed;

        public void Configure(Rect pond, RippleField rings)
        {
            water = pond;
            ripples = rings;
        }

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            body.enabled = false;
            nextJump = Time.time + Random.Range(3f, 7f);
        }

        private void Update()
        {
            float now = Time.time;
            if (now >= nextJump && now - started > JumpSeconds)
            {
                started = now;
                nextJump = now + Random.Range(6f, 12f);
                from = new Vector2(Random.Range(water.xMin + 0.6f, water.xMax - 1.4f), Random.Range(water.yMin + 0.5f, water.yMax - 0.5f));
                to = from + new Vector2(Random.Range(0.6f, 0.9f), Random.Range(-0.15f, 0.15f));
                splashed = false;
                if (ripples != null)
                {
                    ripples.SpawnAt(from, 0.8f);
                }
            }

            float t = (now - started) / JumpSeconds;
            if (t < 0f || t > 1f)
            {
                body.enabled = false;
                if (!splashed && t > 1f && now - started < JumpSeconds + 0.2f)
                {
                    splashed = true;
                    if (ripples != null)
                    {
                        ripples.SpawnAt(to, 1f);
                    }
                }

                return;
            }

            body.enabled = true;
            Vector2 ground = Vector2.Lerp(from, to, t);
            transform.position = new Vector3(ground.x, ground.y + WaterMath.JumpHeight(t, 0.55f), transform.position.z);
            // Nose up on the way out, nose down on the way in.
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(40f, -40f, t));
        }
    }
}

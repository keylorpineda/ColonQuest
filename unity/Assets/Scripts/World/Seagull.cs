using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Coastal wildlife that bobs and pecks while resting on the sand, takes flight
    /// diagonally when the player approaches, and later returns to perch.
    /// </summary>
    public sealed class Seagull : MonoBehaviour
    {
        public enum SeagullState
        {
            Idle,
            Flying,
            Away,
            Returning
        }

        [SerializeField] private float scareDistance = 2.5f;
        [SerializeField] private float flySpeed = 4.0f;
        [SerializeField] private float flightDuration = 2.0f;
        [SerializeField] private float awayDuration = 6.0f;
        [SerializeField] private float returnDuration = 1.0f;

        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Transform wingTransform;
        private Vector3 wingBaseScale;
        private Quaternion wingBaseRotation;
        private SpriteRenderer[] renderers;
        private Color[] originalColors;

        private SeagullState state = SeagullState.Idle;
        private float stateTimer;
        private float peckCooldown;
        private float peckTimer;
        private Vector2 flightDirection;
        private Transform player;
        private float seed;

        public SeagullState State => state;
        public Vector3 HomePosition => homePosition;
        public Transform PlayerTarget { get => player; set => player = value; }

        private void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.localRotation;
            seed = Mathf.Abs((transform.position.x * 13.37f + transform.position.y * 37.11f) % 100f);

            wingTransform = transform.Find("Wing");
            if (wingTransform != null)
            {
                wingBaseScale = wingTransform.localScale;
                wingBaseRotation = wingTransform.localRotation;
            }

            renderers = GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
            }

            peckCooldown = 2.5f + Mathf.Abs(Mathf.Sin(seed)) * 3f;
        }

        private void Start()
        {
            if (player == null) FindPlayer();
        }

        private void FindPlayer()
        {
            var p = GameObject.FindWithTag(Station.PlayerTag);
            if (p != null && p.activeInHierarchy) player = p.transform;
            else player = null;
        }

        private void Update()
        {
            if (player == null || !player.gameObject.activeInHierarchy) FindPlayer();

            switch (state)
            {
                case SeagullState.Idle:
                    UpdateIdle();
                    break;
                case SeagullState.Flying:
                    UpdateFlying();
                    break;
                case SeagullState.Away:
                    UpdateAway();
                    break;
                case SeagullState.Returning:
                    UpdateReturning();
                    break;
            }
        }

        private void UpdateIdle()
        {
            // Bobbing and wing tilt
            float bob = Mathf.Sin(Time.time * 2.8f + seed) * 0.02f;
            transform.position = homePosition + new Vector3(0f, bob, 0f);

            // Pecking behavior
            peckCooldown -= Time.deltaTime;
            if (peckCooldown <= 0f)
            {
                peckTimer = 0.45f;
                peckCooldown = 3.0f + Mathf.PingPong(Time.time, 3.0f);
            }

            if (peckTimer > 0f)
            {
                peckTimer -= Time.deltaTime;
                float angle = Mathf.Sin((0.45f - peckTimer) / 0.45f * Mathf.PI) * -18f;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                transform.localRotation = homeRotation;
            }

            if (wingTransform != null)
            {
                float wingBob = Mathf.Sin(Time.time * 2.0f + seed) * 4f;
                wingTransform.localRotation = wingBaseRotation * Quaternion.Euler(0f, 0f, wingBob);
            }

            // Check if player approaches
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist < scareDistance)
                {
                    Scare(player.position);
                }
            }
        }

        public void Scare(Vector2 scareSource)
        {
            state = SeagullState.Flying;
            stateTimer = 0f;
            float dirX = (transform.position.x >= scareSource.x) ? 1f : -1f;
            flightDirection = new Vector2(dirX * 0.9f, 1.1f).normalized;
        }

        private void UpdateFlying()
        {
            stateTimer += Time.deltaTime;

            // Move diagonally up and away
            transform.position += (Vector3)(flightDirection * flySpeed * Time.deltaTime);

            // Tilt body in flight direction
            float bodyAngle = (flightDirection.x >= 0 ? -15f : 15f);
            transform.localRotation = Quaternion.Euler(0f, 0f, bodyAngle);

            // Wing flaps rapidly
            if (wingTransform != null)
            {
                float flap = Mathf.Sin(stateTimer * 30f);
                wingTransform.localRotation = wingBaseRotation * Quaternion.Euler(0f, 0f, flap * 35f);
                wingTransform.localScale = new Vector3(wingBaseScale.x, wingBaseScale.y * (1f + flap * 0.3f), wingBaseScale.z);
            }

            // Fade out as it climbs
            float fadeStart = flightDuration * 0.5f;
            if (stateTimer >= fadeStart)
            {
                float progress = (stateTimer - fadeStart) / (flightDuration - fadeStart);
                SetAlpha(1f - Mathf.Clamp01(progress));
            }

            if (stateTimer >= flightDuration)
            {
                state = SeagullState.Away;
                stateTimer = 0f;
                SetAlpha(0f);
            }
        }

        private void UpdateAway()
        {
            stateTimer += Time.deltaTime;

            // Wait away duration and ensure player isn't standing right at the spot
            if (stateTimer >= awayDuration)
            {
                if (player == null || Vector2.Distance(homePosition, player.position) > scareDistance + 1.0f)
                {
                    state = SeagullState.Returning;
                    stateTimer = 0f;
                    // Start landing slightly above home
                    transform.position = homePosition + new Vector3(0f, 1.5f, 0f);
                    transform.localRotation = homeRotation;
                    if (wingTransform != null)
                    {
                        wingTransform.localScale = wingBaseScale;
                        wingTransform.localRotation = wingBaseRotation;
                    }
                }
            }
        }

        private void UpdateReturning()
        {
            stateTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stateTimer / returnDuration);

            // Descend smoothly and fade back in
            SetAlpha(t);
            transform.position = Vector3.Lerp(homePosition + new Vector3(0f, 1.5f, 0f), homePosition, t);

            if (wingTransform != null)
            {
                float flap = Mathf.Sin(stateTimer * 20f) * (1f - t);
                wingTransform.localRotation = wingBaseRotation * Quaternion.Euler(0f, 0f, flap * 25f);
            }

            if (t >= 1f)
            {
                state = SeagullState.Idle;
                transform.position = homePosition;
                transform.localRotation = homeRotation;
                SetAlpha(1f);
                if (wingTransform != null)
                {
                    wingTransform.localScale = wingBaseScale;
                    wingTransform.localRotation = wingBaseRotation;
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    Color c = originalColors[i];
                    c.a = originalColors[i].a * alpha;
                    renderers[i].color = c;
                }
            }
        }

        public void ConfigureTimers(float flyDur, float awayDur, float retDur)
        {
            flightDuration = flyDur;
            awayDuration = awayDur;
            returnDuration = retDur;
        }
    }
}

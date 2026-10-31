using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A pedestrian that walks back and forth between two points and rests at each end.
    /// Expects the usual character layout: Bob (PlayerAnimator) containing Visual.
    /// At nightfall, pedestrians with a registered home walk to their door and retire until morning.
    /// </summary>
    public sealed class Patrol : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField] private float speed = 1.3f;
        [SerializeField] private float pause = 1.6f;
        [SerializeField] private bool hasHome;
        [SerializeField] private Vector2 homePoint;

        private Transform visual;
        private PlayerAnimator animator;
        private Collider2D talkCollider;
        private Vector2 target;
        private float waitUntil;
        private float chatUntil;
        private bool isHome;

        public bool HasHome => hasHome;
        public Vector2 HomePoint => homePoint;
        public bool IsHome => isHome;

        public void Configure(Vector2 a, Vector2 b, float walkSpeed, float restSeconds)
        {
            pointA = a;
            pointB = b;
            speed = walkSpeed;
            pause = restSeconds;
            hasHome = false;
        }

        public void Configure(Vector2 a, Vector2 b, float walkSpeed, float restSeconds, Vector2 home)
        {
            pointA = a;
            pointB = b;
            speed = walkSpeed;
            pause = restSeconds;
            hasHome = true;
            homePoint = home;
        }

        /// <summary>The player talks to this walker: it stops where it is and turns to face them for a while.</summary>
        public void Greet(Vector2 playerPosition, float seconds)
        {
            chatUntil = Time.time + seconds;
            if (visual != null)
            {
                visual.localScale = new Vector3(playerPosition.x >= transform.position.x ? 1f : -1f, 1f, 1f);
            }
        }

        private void Start()
        {
            transform.position = pointA;
            target = pointB;

            var bob = transform.Find("Bob");
            if (bob != null)
            {
                animator = bob.GetComponent<PlayerAnimator>();
                visual = bob.Find("Visual");
            }

            var talk = transform.Find("Talk");
            if (talk != null)
            {
                talkCollider = talk.GetComponent<Collider2D>();
            }

            if (DayTint.IsNight && hasHome)
            {
                isHome = true;
                transform.position = homePoint;
                if (visual != null) visual.gameObject.SetActive(false);
                if (talkCollider != null) talkCollider.enabled = false;
                SetWalking(0f);
            }
        }

        private void Update()
        {
            bool night = DayTint.IsNight;

            if (hasHome)
            {
                if (night)
                {
                    if (isHome)
                    {
                        if (visual != null && visual.gameObject.activeSelf)
                        {
                            visual.gameObject.SetActive(false);
                            if (talkCollider != null) talkCollider.enabled = false;
                        }
                        SetWalking(0f);
                        return;
                    }

                    // Walking home
                    if (visual != null && !visual.gameObject.activeSelf)
                    {
                        visual.gameObject.SetActive(true);
                    }

                    Vector2 currentPos = transform.position;
                    Vector2 nextPos = Vector2.MoveTowards(currentPos, homePoint, speed * Time.deltaTime);
                    float dirX = nextPos.x - currentPos.x;
                    transform.position = nextPos;

                    if (visual != null && Mathf.Abs(dirX) > 0.0001f)
                    {
                        visual.localScale = new Vector3(dirX > 0f ? 1f : -1f, 1f, 1f);
                    }

                    SetWalking(1f);

                    if ((nextPos - homePoint).sqrMagnitude < 0.0001f)
                    {
                        isHome = true;
                        if (visual != null) visual.gameObject.SetActive(false);
                        if (talkCollider != null) talkCollider.enabled = false;
                        SetWalking(0f);
                    }
                    return;
                }
                else // Daytime
                {
                    if (isHome)
                    {
                        isHome = false;
                        transform.position = homePoint;
                        if (visual != null) visual.gameObject.SetActive(true);
                        if (talkCollider != null) talkCollider.enabled = true;
                        target = (homePoint - pointA).sqrMagnitude < (homePoint - pointB).sqrMagnitude ? pointA : pointB;
                    }
                }
            }
            else
            {
                if (visual != null && visual.gameObject.activeSelf == night)
                {
                    visual.gameObject.SetActive(!night);
                    if (talkCollider != null)
                    {
                        talkCollider.enabled = !night;
                    }
                }

                if (night)
                {
                    SetWalking(0f);
                    return;
                }
            }

            if (Time.time < waitUntil || Time.time < chatUntil)
            {
                SetWalking(0f);
                return;
            }

            Vector2 position = transform.position;
            Vector2 next = Vector2.MoveTowards(position, target, speed * Time.deltaTime);
            float dx = next.x - position.x;
            transform.position = next;

            if (visual != null && Mathf.Abs(dx) > 0.0001f)
            {
                visual.localScale = new Vector3(dx > 0f ? 1f : -1f, 1f, 1f);
            }

            SetWalking(1f);

            if ((next - target).sqrMagnitude < 0.0001f)
            {
                target = target == pointB ? pointA : pointB;
                waitUntil = Time.time + pause;
            }
        }

        private void SetWalking(float amount)
        {
            if (animator != null)
            {
                animator.SetSpeed01(amount);
            }
        }
    }
}

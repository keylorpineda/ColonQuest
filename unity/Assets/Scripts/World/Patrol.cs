using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A pedestrian that walks back and forth between two points and rests at each end.
    /// Expects the usual character layout: Bob (PlayerAnimator) containing Visual.
    /// </summary>
    public sealed class Patrol : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField] private float speed = 1.3f;
        [SerializeField] private float pause = 1.6f;

        private Transform visual;
        private PlayerAnimator animator;
        private Vector2 target;
        private float waitUntil;

        public void Configure(Vector2 a, Vector2 b, float walkSpeed, float restSeconds)
        {
            pointA = a;
            pointB = b;
            speed = walkSpeed;
            pause = restSeconds;
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
        }

        private void Update()
        {
            if (Time.time < waitUntil)
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

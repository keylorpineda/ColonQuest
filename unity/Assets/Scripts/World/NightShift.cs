using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A shopkeeper who goes home at night: when the evening comes they walk a few steps toward the back of their place and
    /// are gone, and in the morning they are back at their post. Their station stays, so it still says it is closed, but
    /// nobody stands there. A scene that loads at night starts without them.
    /// </summary>
    public sealed class NightShift : MonoBehaviour
    {
        private const float Speed = 1.4f;

        [SerializeField] private Vector2 exitOffset = new Vector2(0f, 1.4f);

        private Vector2 post;
        private Transform visual;
        private PlayerAnimator animator;
        private bool leaving;
        private bool gone;

        public bool Gone => gone;

        public void Configure(Vector2 offset)
        {
            exitOffset = offset;
        }

        private void Start()
        {
            post = transform.position;
            var bob = transform.Find("Bob");
            if (bob != null)
            {
                animator = bob.GetComponent<PlayerAnimator>();
                visual = bob.Find("Visual");
            }

            if (DayTint.IsNight)
            {
                Hide();
            }
        }

        private void Update()
        {
            if (!DayTint.IsNight)
            {
                if (gone || leaving)
                {
                    Show();
                }

                return;
            }

            if (gone)
            {
                return;
            }

            leaving = true;
            Vector2 target = post + exitOffset;
            Vector2 next = Vector2.MoveTowards(transform.position, target, Speed * Time.deltaTime);
            if (visual != null && Mathf.Abs(next.x - transform.position.x) > 0.0001f)
            {
                visual.localScale = new Vector3(next.x > transform.position.x ? 1f : -1f, 1f, 1f);
            }

            transform.position = next;
            SetWalking(1f);
            if ((next - target).sqrMagnitude < 0.0001f)
            {
                Hide();
            }
        }

        private void Hide()
        {
            leaving = false;
            gone = true;
            SetWalking(0f);
            SetPresent(false);
        }

        private void Show()
        {
            leaving = false;
            gone = false;
            transform.position = post;
            SetPresent(true);
        }

        private void SetPresent(bool present)
        {
            if (visual != null)
            {
                visual.gameObject.SetActive(present);
            }

            // Their own body and any talk zone they carry: nobody to bump into or to talk to at night.
            foreach (var collider in GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = present;
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

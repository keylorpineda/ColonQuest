using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Top-down player movement. Reads the Horizontal and Vertical axes (arrow keys
    /// and WASD) and moves the Rigidbody2D, so walls and furniture block it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private Transform visual;

        private Rigidbody2D body;
        private Vector2 input;
        private int facing = 1;

        /// <summary>Overlay panels turn this off while they are open so the player stands still.</summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>Set while a modal panel is open. It outlives scene changes, unlike <see cref="InputEnabled"/>.</summary>
        public static bool Blocked { get; set; }

        /// <summary>Sets the child that is mirrored to face the walking direction.</summary>
        public void SetVisual(Transform target)
        {
            visual = target;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            input = InputEnabled && !Blocked
                ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                : Vector2.zero;

            facing = MovementMath.FacingSign(input.x, facing);
            if (visual != null)
            {
                visual.localScale = new Vector3(facing, 1f, 1f);
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = MovementMath.Velocity(input, speed);
        }
    }
}

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

        /// <summary>Set while an animation moves the player (a hit, an arrest): the keys do nothing and the walk does not fight the animation.</summary>
        public static bool Frozen { get; set; }

        /// <summary>Sets the child that is mirrored to face the walking direction.</summary>
        public void SetVisual(Transform target)
        {
            visual = target;
        }

        /// <summary>Turns the player to face left (-1) or right (1), for the animated scenes.</summary>
        public void Face(int sign)
        {
            facing = sign >= 0 ? 1 : -1;
            if (visual != null)
            {
                visual.localScale = new Vector3(facing, 1f, 1f);
            }
        }

        private Vector2 nudge;
        private float nudgeUntil;

        /// <summary>Walks the player a few steps in a direction, as if they had turned around by themselves.</summary>
        public void Nudge(Vector2 direction, float seconds)
        {
            nudge = direction.normalized;
            nudgeUntil = Time.time + seconds;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (GetComponent<WalkWatch>() == null)
            {
                gameObject.AddComponent<WalkWatch>();
            }
        }

        private void Update()
        {
            input = InputEnabled && !Blocked && !Frozen
                ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                : Vector2.zero;

            if (Time.time < nudgeUntil && InputEnabled && !Blocked && !Frozen)
            {
                input = nudge;
            }

            facing = MovementMath.FacingSign(input.x, facing);
            if (visual != null)
            {
                visual.localScale = new Vector3(facing, 1f, 1f);
            }
        }

        private void FixedUpdate()
        {
            if (Frozen)
            {
                return;
            }

            body.linearVelocity = MovementMath.Velocity(input, speed);
        }
    }
}

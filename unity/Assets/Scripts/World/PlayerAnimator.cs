using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Walk cycle for the cartoon player: a bounce and a squash with every step while
    /// moving, and a slow breathing squash while standing still. Put it on the child
    /// that holds the visual; the Rigidbody2D is read from the parent.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private float referenceSpeed = 4.5f;
        [SerializeField] private float bobHeight = 0.09f;
        [SerializeField] private float stepRate = 11f;

        private Rigidbody2D body;
        private Vector3 restPosition;
        private float phase;
        private float moving;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody2D>();
            restPosition = transform.localPosition;
        }

        private void Update()
        {
            float target = body != null ? Mathf.Clamp01(body.linearVelocity.magnitude / referenceSpeed) : 0f;
            moving = Mathf.MoveTowards(moving, target, Time.deltaTime * 8f);
            phase += Time.deltaTime * stepRate * moving;

            float step = Mathf.Abs(Mathf.Sin(phase));
            float breathing = Mathf.Sin(Time.time * 2f) * 0.02f * (1f - moving);
            float squash = 1f + (step - 0.5f) * 0.1f * moving + breathing;

            transform.localPosition = restPosition + new Vector3(0f, step * bobHeight * moving, 0f);
            transform.localScale = new Vector3(1f / squash, squash, 1f);
        }
    }
}

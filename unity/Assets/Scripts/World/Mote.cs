using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>A speck of dust that floats upward with a gentle sway and wraps around its layer.</summary>
    public sealed class Mote : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 0.25f;
        [SerializeField] private float swayAmount = 0.15f;
        [SerializeField] private float swayPhase;
        [SerializeField] private Vector2 halfRange = new Vector2(14f, 9f);

        public void Configure(float rise, float sway, float phase, Vector2 range)
        {
            riseSpeed = rise;
            swayAmount = sway;
            swayPhase = phase;
            halfRange = range;
        }

        private void Update()
        {
            Vector3 position = transform.localPosition;
            position.y += riseSpeed * Time.deltaTime;
            position.x += Mathf.Sin(Time.time * 0.7f + swayPhase) * swayAmount * Time.deltaTime;

            if (position.y > halfRange.y)
            {
                position.y = -halfRange.y;
            }

            transform.localPosition = position;
        }
    }
}

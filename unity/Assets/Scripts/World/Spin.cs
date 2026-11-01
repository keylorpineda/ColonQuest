using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Smooth continuous rotation for decorative ceiling fans and turbines.
    /// </summary>
    public sealed class Spin : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 180f;

        public void Configure(float speed)
        {
            degreesPerSecond = speed;
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}

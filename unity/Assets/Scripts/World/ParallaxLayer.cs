using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Moves a group of sprites with the camera by a fraction, which gives depth.
    /// A factor of 0 stays fixed in the world, 1 stays fixed on the screen (infinitely far),
    /// and a negative factor slides past faster than the world, as foreground does.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float factor = 0.5f;

        private Transform cameraTransform;
        private Vector3 origin;

        public void SetFactor(float value)
        {
            factor = value;
        }

        private void Start()
        {
            origin = transform.position;
            var main = Camera.main;
            cameraTransform = main != null ? main.transform : null;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                return;
            }

            Vector3 shift = cameraTransform.position * factor;
            transform.position = origin + new Vector3(shift.x, shift.y, 0f);
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Smoothly follows a target and never shows anything outside the map bounds.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rect bounds = new Rect(-12f, -7f, 24f, 14f);
        [SerializeField] private float smoothTime = 0.15f;

        private Camera cam;
        private Vector3 velocity;

        public void Configure(Transform followTarget, Rect mapBounds)
        {
            target = followTarget;
            bounds = mapBounds;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // Start on the target so the first frame does not slide in from elsewhere.
            transform.position = Desired();
        }

        private void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }

        private Vector3 Desired()
        {
            if (target == null)
            {
                return transform.position;
            }

            float halfHeight = cam.orthographicSize;
            var halfView = new Vector2(halfHeight * cam.aspect, halfHeight);
            Vector2 center = CameraMath.ClampCenter(target.position, halfView, bounds);
            return new Vector3(center.x, center.y, transform.position.z);
        }
    }
}

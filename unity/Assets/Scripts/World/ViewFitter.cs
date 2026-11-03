using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Stretches a sprite on a camera so it always covers the whole view, whatever the
    /// window size or aspect ratio. Used for the vignette.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ViewFitter : MonoBehaviour
    {
        [SerializeField] private float margin = 1.05f;

        private Camera cam;

        public void Fit()
        {
            if (cam == null)
            {
                cam = GetComponentInParent<Camera>();
            }

            float height = cam.orthographicSize * 2f * margin;
            transform.localScale = new Vector3(height * cam.aspect, height, 1f);
        }

        private void LateUpdate()
        {
            Fit();
        }
    }
}

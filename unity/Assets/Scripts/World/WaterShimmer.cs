using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A tiled layer of caustic light that slides over the water. It wraps every tile, so the edge of the texture is
    /// never seen and the water seems to glitter and move all the time. Put it on a tiled sprite a little larger than
    /// the water it covers.
    /// </summary>
    public sealed class WaterShimmer : MonoBehaviour
    {
        [SerializeField] private float speedX = 0.25f;
        [SerializeField] private float speedY;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobRate = 0.4f;
        [SerializeField] private float tile = 2f;

        private Vector3 rest;

        /// <summary>Drift speeds in units per second, the vertical bob and the tile size in world units.</summary>
        public void Configure(float driftX, float driftY, float bob, float tileSize)
        {
            speedX = driftX;
            speedY = driftY;
            bobAmplitude = bob;
            tile = tileSize;
        }

        private void Awake()
        {
            rest = transform.localPosition;
        }

        private void Update()
        {
            float time = Time.time;
            float x = WaterMath.ShimmerDrift(time, speedX, tile);
            float y = WaterMath.ShimmerDrift(time, speedY, tile) + WaterMath.ShimmerBob(time, bobRate, bobAmplitude);
            transform.localPosition = rest + new Vector3(x, y, 0f);
        }
    }
}

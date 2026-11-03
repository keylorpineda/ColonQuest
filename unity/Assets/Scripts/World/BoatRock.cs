using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Rocks a boat or a buoy on the swell. It only turns the object, so it works with <see cref="Drift"/> moving it.</summary>
    public sealed class BoatRock : MonoBehaviour
    {
        [SerializeField] private float degrees = 3f;
        [SerializeField] private float period = 3.2f;
        [SerializeField] private float phase;

        private Quaternion rest;

        public void Configure(float maxDegrees, float seconds, float cyclePhase)
        {
            degrees = maxDegrees;
            period = seconds;
            phase = cyclePhase;
        }

        private void Awake()
        {
            rest = transform.localRotation;
        }

        private void Update()
        {
            transform.localRotation = rest * Quaternion.Euler(0f, 0f, WaterMath.Rock(Time.time, degrees, period, phase));
        }
    }
}

using ColonQuest.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ColonQuest.Lighting
{
    /// <summary>
    /// The sun of the town: the one global 2D light, colored and dimmed through the day by <see cref="DayCycle.LightFor"/>.
    /// It reads the same hour as the rest of the town (<see cref="DayTint.Progress"/>) and eases toward it, so the light
    /// never jumps, not even when the player sleeps and the morning comes.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class DayLight : MonoBehaviour
    {
        private Light2D sun;

        private void Awake()
        {
            sun = GetComponent<Light2D>();
            DayCycle.LightFor(DayTint.Progress, out Color color, out float intensity);
            sun.color = color;
            sun.intensity = intensity;
        }

        private void Update()
        {
            DayCycle.LightFor(DayTint.Progress, out Color color, out float intensity);
            float k = 1f - Mathf.Exp(-1.5f * Time.deltaTime);
            sun.color = Color.Lerp(sun.color, color, k);
            sun.intensity = Mathf.Lerp(sun.intensity, intensity, k);
        }
    }
}

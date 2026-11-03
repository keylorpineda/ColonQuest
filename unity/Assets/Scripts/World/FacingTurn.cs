using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Turns a character smoothly between left and right over 0.12 seconds, narrowing down to 15% and back out
    /// with 1 - |cos(t*pi)|, so the flip doesn't pop.
    /// </summary>
    public sealed class FacingTurn : MonoBehaviour
    {
        private const float Duration = 0.12f;

        private int facing = 1;
        private int fromFacing = 1;
        private int targetFacing = 1;
        private float turnTime = 1f;

        public int Facing => targetFacing;

        public void TurnTo(int sign)
        {
            if (sign == 0) return;
            sign = sign > 0 ? 1 : -1;
            if (sign == targetFacing) return;

            fromFacing = facing;
            targetFacing = sign;
            turnTime = 0f;
        }

        public void SnapTo(int sign)
        {
            if (sign == 0) return;
            sign = sign > 0 ? 1 : -1;
            facing = targetFacing = fromFacing = sign;
            turnTime = Duration;
            transform.localScale = new Vector3(facing, 1f, 1f);
        }

        private void Update()
        {
            if (turnTime >= Duration)
            {
                return;
            }

            turnTime += Time.deltaTime;
            float t = Mathf.Clamp01(turnTime / Duration);
            float width = PersonMotion.TurnWidth(t);
            facing = t >= 0.5f ? targetFacing : fromFacing;
            transform.localScale = new Vector3(facing * width, 1f, 1f);
        }
    }
}

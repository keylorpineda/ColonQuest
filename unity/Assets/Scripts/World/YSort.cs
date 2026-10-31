using UnityEngine;
using UnityEngine.Rendering;

namespace ColonQuest.World
{
    /// <summary>
    /// Draws a character or prop in front of things that are further up the screen and
    /// behind things that are further down, so the player can walk behind a tree.
    /// Attach it to the root of the object, next to its SortingGroup.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public sealed class YSort : MonoBehaviour
    {
        [SerializeField] private float footOffset;

        private SortingGroup group;

        /// <summary>Sorting order for an object whose feet are at this height. Lower on screen wins.</summary>
        public static int OrderFor(float footY)
        {
            return -Mathf.RoundToInt(footY * 200f);
        }

        /// <summary>Where the feet are relative to the object's position (usually negative).</summary>
        public void SetFootOffset(float offset)
        {
            footOffset = offset;
        }

        private void Awake()
        {
            group = GetComponent<SortingGroup>();
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            group.sortingOrder = OrderFor(transform.position.y + footOffset);
        }
    }
}

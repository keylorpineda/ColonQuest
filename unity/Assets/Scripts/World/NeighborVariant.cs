using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Chooses which neighbor interior decoration variant (0..3) to activate based on which house
    /// the player entered from (Door.RememberedReturn).
    /// Runs early before other components locate decorated props.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class NeighborVariant : MonoBehaviour
    {
        [SerializeField] private GameObject[] variants;

        public void Configure(GameObject[] variantGroups)
        {
            variants = variantGroups;
        }

        private void Awake()
        {
            int index = HouseVisit.VariantFor(Door.RememberedReturn);
            ApplyVariant(index);
        }

        public void ApplyVariant(int index)
        {
            if (variants == null)
            {
                return;
            }

            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] != null)
                {
                    variants[i].SetActive(i == index);
                }
            }
        }
    }
}

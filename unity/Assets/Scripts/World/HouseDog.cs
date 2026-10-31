using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// The dog of a house interior that several houses share. The mother's dog lives only in her house, not in the houses
    /// for sale or rent, and among the neighbors only some houses have one (see <see cref="HouseVisit.NeighborHasDog"/>).
    /// </summary>
    public sealed class HouseDog : MonoBehaviour
    {
        [SerializeField] private GameObject talk;

        public void Configure(GameObject talkZone)
        {
            talk = talkZone;
        }

        private void Start()
        {
            string scene = SceneManager.GetActiveScene().name;
            bool here = scene == SceneNames.Vecino ? HouseVisit.NeighborHasDog(Door.RememberedReturn) : !HouseVisit.InListing;
            gameObject.SetActive(here);
            if (talk != null)
            {
                talk.SetActive(here);
            }
        }
    }
}

using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The player is built with the two looks; this keeps the one of the run. It runs before anything reads the body, so the
    /// animator and the scenes only ever find one "Bob".
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerLookSwitch : MonoBehaviour
    {
        public const string AlternateBob = "Bob_F";

        private void Awake()
        {
            var main = transform.Find("Bob");
            var alternate = transform.Find(AlternateBob);
            if (alternate == null)
            {
                return;
            }

            if (!PlayerProfile.Female || main == null)
            {
                DestroyImmediate(alternate.gameObject);
                return;
            }

            DestroyImmediate(main.gameObject);
            alternate.name = "Bob";
            alternate.gameObject.SetActive(true);
            GetComponent<PlayerController>().SetVisual(alternate.Find("Visual"));
        }
    }
}

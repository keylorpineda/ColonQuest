using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// Keeps a label in the player's language. It remembers the Spanish text the label was made with and writes
    /// it again whenever the language changes. Labels that code fills in later (the money in the HUD) are made
    /// empty, so they never get this component.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        private string spanish = string.Empty;

        public string Spanish => spanish;

        public void Bind(string spanishText)
        {
            spanish = spanishText;
            Apply();
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        private void Apply()
        {
            var label = GetComponent<Text>();
            if (label != null)
            {
                label.text = Loc.T(spanish);
            }
        }
    }
}

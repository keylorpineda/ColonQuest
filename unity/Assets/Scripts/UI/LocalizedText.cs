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
        private GameLanguage applied;

        public string Spanish => spanish;

        public void Bind(string spanishText)
        {
            spanish = spanishText;
            Apply();
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;

            // Only a language that changed while this label was hidden needs writing again; a label the code
            // filled in itself (a button that says "Sound: on") keeps its text.
            if (applied != Loc.Current)
            {
                Apply();
            }
        }

        private void OnDisable()
        {
            Loc.Changed -= Apply;
        }

        private void Apply()
        {
            var label = GetComponent<Text>();
            applied = Loc.Current;
            if (label != null)
            {
                label.text = Loc.T(spanish);
            }
        }
    }
}

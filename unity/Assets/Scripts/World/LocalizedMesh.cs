using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Keeps a sign of the world in the player's language. It remembers the Spanish text the sign was made with
    /// and writes it again whenever the language changes; a sign without an English entry keeps its Spanish text.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class LocalizedMesh : MonoBehaviour
    {
        [SerializeField] private string spanish = string.Empty;

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
            var mesh = GetComponent<TextMesh>();
            if (mesh != null && !string.IsNullOrEmpty(spanish))
            {
                mesh.text = Loc.T(spanish);
            }
        }
    }
}

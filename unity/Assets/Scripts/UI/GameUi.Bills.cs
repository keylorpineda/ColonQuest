using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>What a cut service does to the home: without electricity the rooms go dark, and what needs a service only gets a thought.</summary>
    public sealed partial class GameUi
    {
        private GameObject powerCut;

        private void BuildPowerCut()
        {
            var rect = UiKit.Rect(canvas.transform, "PowerCut", Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var shade = rect.gameObject.AddComponent<Image>();
            shade.color = new Color(0.02f, 0.03f, 0.08f, 0.62f);
            shade.raycastTarget = false;
            powerCut = rect.gameObject;
            powerCut.SetActive(false);
        }

        private void RefreshPowerCut()
        {
            string scene = SceneManager.GetActiveScene().name;
            bool home = scene == SceneNames.Cuarto || scene == SceneNames.Casa;
            powerCut.SetActive(home && session.Loop.Bills.IsCut(BillKind.Electricity));
        }

        /// <summary>The thought for something that does not work because its service was cut, or null when it works.</summary>
        private string CutLine(Station station)
        {
            var bills = session.Loop.Bills;
            switch (station.name)
            {
                case "Thing_Tv":
                    return bills.IsCut(BillKind.Electricity) ? "Cortaron la luz. La tele no prende." : null;
                case "Thing_Fridge":
                    return bills.IsCut(BillKind.Electricity) ? "Cortaron la luz. La refri está apagada y la comida se echa a perder." : null;
                case "Thing_Shower":
                case "Thing_Sink":
                    return bills.IsCut(BillKind.Water) ? "Cortaron el agua. No sale ni una gota." : null;
                default:
                    return null;
            }
        }
    }
}

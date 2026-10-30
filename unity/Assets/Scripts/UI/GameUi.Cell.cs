using System.Collections.Generic;
using System.Text;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The cell, the wardrobe and the beach: the sentence the player serves after an arrest (with the bail as a way out),
    /// the swimsuit that is bought at home, and the swim it allows once a month.
    /// </summary>
    public sealed partial class GameUi
    {
        public const string LockedThought = "La puerta está cerrada con llave. Tengo que cumplir la condena o pagar la fianza.";
        public const string SentenceDoneThought = "Cumplí mi condena. Ya puedo irme.";

        private GameObject cellCard;
        private Text cellText;
        private Button bailButton;
        private float sentenceLeft;
        private int bailShownMinutes = -1;

        private void BuildCell()
        {
            var card = UiKit.Card(canvas.transform, "Cell", UiKit.Paper, new Vector2(620f, 200f), new Vector2(0.5f, 1f), new Vector2(0f, -150f));
            cellCard = card.parent.gameObject;
            cellText = UiKit.Label(card, string.Empty, 40, UiKit.Ink, new Vector2(560f, 60f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), TextAnchor.MiddleCenter);
            bailButton = UiKit.MakeButton(card, "Pagar fianza", GameUiColors.Yellow, new Vector2(520f, 76f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), PayBail);
            cellCard.SetActive(false);
        }

        /// <summary>The player is in the cell: the door stays locked until the term is served or the bail is paid.</summary>
        private void StartSentence(int seconds)
        {
            sentenceLeft = seconds;
            bailShownMinutes = -1;
            Door.Locked = true;
            Door.LockedThought = LockedThought;
        }

        private void ResetSentence()
        {
            sentenceLeft = 0f;
            Door.Locked = false;
            if (cellCard != null)
            {
                cellCard.SetActive(false);
            }
        }

        private void TickCell()
        {
            if (sentenceLeft <= 0f)
            {
                return;
            }

            bool atTheStation = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneNames.Carcel;
            if (!pause.IsOpen)
            {
                sentenceLeft -= Time.unscaledDeltaTime;
            }

            if (sentenceLeft <= 0f)
            {
                sentenceLeft = 0f;
                Door.Locked = false;
                cellCard.SetActive(false);
                subtitles.Show(string.Empty, Loc.T(SentenceDoneThought));
                return;
            }

            cellCard.SetActive(atTheStation && !modal.IsOpen);
            int seconds = Mathf.CeilToInt(sentenceLeft);
            cellText.text = Loc.F("Condena: {0}", JailSentence.Clock(seconds));

            int minutes = (seconds + 59) / 60;
            if (minutes != bailShownMinutes)
            {
                bailShownMinutes = minutes;
                UiKit.SetLabel(bailButton, Loc.F("Pagar fianza  {0}", Money.Format(JailSentence.BailCost(seconds))));
            }
        }

        private void PayBail()
        {
            if (sentenceLeft <= 0f)
            {
                return;
            }

            long cost = JailSentence.BailCost(Mathf.CeilToInt(sentenceLeft));
            if (session.PayBail(cost))
            {
                ResetSentence();
            }
        }

        // ---------------------------------------------------------------- the wardrobe and the beach

        private void ShowWardrobe()
        {
            if (session.Loop.HasSwimsuit)
            {
                Thoughts.Say("Ya tengo mi bañador guardado. Me toca ir a la playa.");
                return;
            }

            long price = MarketAssumptions.SwimsuitPrice;
            var text = new StringBuilder();
            text.AppendLine(Loc.T("En el ropero no hay nada para el agua. Un bañador es una compra chiquita, pero también sale de tu efectivo."));
            text.AppendLine();
            text.AppendLine(Loc.F("Bañador  {0}", Money.Format(price)));
            text.AppendLine(Loc.F("Tienes {0} en efectivo.", Money.Format(session.State.Cash)));
            text.AppendLine();
            text.Append(Loc.T("Cifras de ejemplo del juego."));

            var options = new List<ModalOption>();
            if (session.State.Cash >= price)
            {
                options.Add(new ModalOption { Label = Loc.F("1.  Comprar el bañador  ·  {0}", Money.Format(price)), Color = GameUiColors.Green, OnPress = () => session.BuySwimsuit() });
            }

            options.Add(Back(Loc.F("{0}.  Cerrar el ropero", options.Count + 1)));
            modal.Show(Loc.T("El ropero"), Loc.T("Ropa para ir a la playa"), text.ToString(), options);
        }

        private void ShowBeach()
        {
            if (!session.Loop.HasSwimsuit)
            {
                Thoughts.Say(WalkWatch.WaterThought);
                return;
            }

            if (session.Loop.SwamThisMonth)
            {
                Thoughts.Say("Ya me di mi chapuzón de este mes. Con una vez al mes basta para despejar la cabeza.");
                return;
            }

            session.Swim();
        }
    }
}

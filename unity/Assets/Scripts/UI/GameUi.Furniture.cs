using System.Collections.Generic;
using System.Text;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// The furniture corner of the store. Cash down goes through the till with its beep and its bag; in installments the player
    /// sits with the seller, who keeps the insurance and the commission quiet until asked. The money moves in the finance engine.
    /// </summary>
    public sealed partial class GameUi
    {
        private void ShowFurniture()
        {
            var text = new StringBuilder();
            text.AppendLine(Loc.T("Lo que comprés aparece en tu cuarto. De contado se paga una vez. A cuotas, preguntale al vendedor cuánto suma todo."));
            text.AppendLine();
            text.AppendLine(Loc.F("Tienes {0} en efectivo.", Money.Format(session.State.Cash)));
            text.AppendLine();
            text.Append(Loc.T(MarketAssumptions.ShortDisclaimer));

            var options = new List<ModalOption>();
            int index = 0;
            foreach (var piece in MarketAssumptions.Furniture)
            {
                var item = piece;
                if (session.Holdings.Has(item.Kind))
                {
                    continue;
                }

                options.Add(new ModalOption
                {
                    Label = $"{index + 1}.  {Loc.T(item.Name)}  ·  {Money.Format(item.Price)}",
                    Color = GameUiColors.Cycle(index),
                    OnPress = () => ShowFurnitureDetail(item)
                });
                index++;
            }

            if (index == 0)
            {
                text.Insert(0, Loc.T("Ya tienes todos los muebles de tu cuarto.") + "\n\n");
            }

            options.Add(Back(Loc.F("{0}.  Salir", index + 1)));
            modal.Show(Loc.T("Muebles"), "Kevin", text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        private void ShowFurnitureDetail(FurnitureModel item)
        {
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            text.AppendLine(Loc.T(item.Description));
            text.AppendLine();
            text.AppendLine(Loc.F("De contado  {0}", Money.Format(item.Price)));
            text.AppendLine(Loc.F("A cuotas: {0} meses con el vendedor", item.TermMonths));
            text.AppendLine();
            text.AppendLine(Loc.F("Tienes {0} en efectivo.", Money.Format(session.State.Cash)));

            var refusal = session.Loop.CheckBuyFurniture(item);
            if (refusal != PurchaseFailure.None)
            {
                text.AppendLine();
                text.AppendLine(Red(Loc.T("No te alcanza para pagarlo de contado.")));
            }
            else
            {
                options.Add(new ModalOption { Label = Loc.F("{0}.  Pagar de contado", options.Count + 1), Color = GameUiColors.Green, OnPress = () => PlayCheckout(item) });
            }

            options.Add(new ModalOption { Label = Loc.F("{0}.  Hablar de cuotas con Kevin", options.Count + 1), Color = GameUiColors.Yellow, OnPress = () => session.BeginFurnitureNegotiation(item) });
            options.Add(Back(Loc.F("{0}.  Volver a los muebles", options.Count + 1), ShowFurniture));
            modal.Show(Loc.T(item.Name), Loc.T("Muebles"), text.ToString(), options, null, MarketFont, MarketBodyHeight);
        }

        /// <summary>At the till: the bills go over, the scanner beeps, the receipt prints and the bag changes hands.</summary>
        private void PlayCheckout(FurnitureModel item)
        {
            Vector2 me = HeroAt();
            var scene = new Interaction().Pose(PersonPose.Handover, 0.7f).Sound(SoundEffectKind.Bills)
                .Fly("bills", me + new Vector2(0f, 0.4f), me + new Vector2(0f, 1.5f), 0.7f)
                .Wait(0.5f).Sound(SoundEffectKind.Beep).Wait(0.3f).Sound(SoundEffectKind.Printer)
                .Fly("box", me + new Vector2(0f, 1.5f), me + new Vector2(0f, 0.2f), 0.8f)
                .Wait(0.8f).Sound(SoundEffectKind.Coin)
                .Apply(() => session.BuyFurniture(item));
            if (!scene.Play())
            {
                session.BuyFurniture(item);
            }
        }
    }
}

using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;

namespace ColonQuest.UI
{
    /// <summary>
    /// The talks in the player's own words: with the collectors when they come, and with the lender and the pyramid stand
    /// before hearing their offer. The money moves only through the finance engine, after the talk, with the buttons.
    /// </summary>
    public sealed partial class GameUi
    {
        private void OnTalkChanged(DebtTalk talk)
        {
            if (talk == null)
            {
                talkPanel.Hide();
                PlayerController.Blocked = false;
                return;
            }

            if (talkPanel.IsOpen)
            {
                talkPanel.Refresh(talk);
            }
            else
            {
                talkPanel.Show(talk);
            }
        }

        /// <summary>At the lender's or the pyramid stand the character speaks at once; the offer and the way out are under the conversation.</summary>
        private void AskHowToTalk(StationType type)
        {
            session.BeginTalk(type == StationType.Lender ? TalkPartner.Lender : TalkPartner.Pyramid);
        }

        /// <summary>The ways out under the conversation. The arrangement of the collectors shows only once the player asked for it.</summary>
        private IList<ModalOption> TalkActions(DebtTalk talk)
        {
            var options = new List<ModalOption>();
            if (talk.Partner == TalkPartner.Collectors)
            {
                options.Add(new ModalOption
                {
                    Label = Loc.F("Pagar  ·  {0}", Money.Format(talk.Demand)), Color = GameUiColors.Green,
                    OnPress = () => CloseCollectorsTalk(session.PayCollectors)
                });
                if (talk.ArrangementOffered)
                {
                    options.Add(new ModalOption
                    {
                        Label = Loc.F("Aceptar el arreglo  ·  {0} hoy", Money.Format(talk.ArrangementToday)), Color = GameUiColors.Yellow,
                        OnPress = () => CloseCollectorsTalk(session.NegotiateCollectors)
                    });
                }

                options.Add(new ModalOption { Label = Loc.T("Ignorarlos"), Color = GameUiColors.Pink, OnPress = () => CloseCollectorsTalk(session.IgnoreCollectors) });
                return options;
            }

            var type = talk.Partner == TalkPartner.Lender ? StationType.Lender : StationType.Pyramid;
            long owed = talk.Partner == TalkPartner.Lender ? FinanceEngine.DebtOwed(session.State) : 0L;
            long cash = session.State.Cash;
            if (owed > 0L && cash > 0L)
            {
                string pay = cash >= owed ? Loc.F("Pagar todo  ·  {0}", Money.Format(owed)) : Loc.F("Abonar  ·  {0}", Money.Format(cash));
                options.Add(new ModalOption
                {
                    Label = pay, Color = GameUiColors.Blue,
                    OnPress = () =>
                    {
                        session.EndTalk();
                        session.PayLender();
                    }
                });
            }

            if (session.Loop.CanDoTask(type))
            {
                options.Add(new ModalOption
                {
                    Label = Loc.T("Escuchar la oferta"), Color = GameUiColors.Green,
                    OnPress = () =>
                    {
                        session.EndTalk();
                        session.BeginOffer(talk.Partner);
                    }
                });
            }

            options.Add(new ModalOption { Label = Loc.T("Irme"), Color = GameUiColors.Pink, OnPress = session.EndTalk });
            return options;
        }

        private void CloseCollectorsTalk(System.Action choice)
        {
            session.EndTalk();
            CollectorsLeave();
            choice();
        }
    }
}

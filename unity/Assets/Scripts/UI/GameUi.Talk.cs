using System.Collections.Generic;
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

        /// <summary>At the lender's or the pyramid stand: talk first in the player's words, hear the offer, or leave.</summary>
        private void AskHowToTalk(StationType type)
        {
            var partner = type == StationType.Lender ? TalkPartner.Lender : TalkPartner.Pyramid;
            string title = type == StationType.Lender ? "Prestamista" : "Invierte Ya";
            var options = new List<ModalOption>
            {
                new ModalOption { Label = Loc.T("1.  Hablar y negociar (texto o voz)"), Color = GameUiColors.Yellow, OnPress = () => session.BeginTalk(partner) }
            };

            if (session.Loop.CanDoTask(type))
            {
                options.Add(new ModalOption { Label = Loc.T("2.  Escuchar la oferta"), Color = GameUiColors.Green, OnPress = () => session.BeginTask(type) });
            }

            options.Add(Back(Loc.F("{0}.  Salir", options.Count + 1)));
            modal.Show(Loc.T(title), string.Empty, Loc.T("Antes de decir que sí, preguntá: cuánto pagás en total, la tasa, qué garantía hay y si es legal."), options);
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
            if (session.Loop.CanDoTask(type))
            {
                options.Add(new ModalOption
                {
                    Label = Loc.T("Escuchar la oferta"), Color = GameUiColors.Green,
                    OnPress = () =>
                    {
                        session.EndTalk();
                        session.BeginTask(type);
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

using System.Collections.Generic;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

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

            if (talk.Partner == TalkPartner.Neighbor || talk.Partner == TalkPartner.Stranger)
            {
                if (talk.Partner == TalkPartner.Stranger)
                {
                    options.Add(new ModalOption { Label = Loc.F("Enviarle {0}", Money.Format(DebtTalk.StrangerAsk)), Color = GameUiColors.Blue, OnPress = FallForTheStranger });
                }

                options.Add(new ModalOption { Label = Loc.T("Irme"), Color = GameUiColors.Pink, OnPress = session.EndTalk });
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

        /// <summary>The money leaves the account like any SINPE, and there was never a deposit to return.</summary>
        private void FallForTheStranger()
        {
            session.EndTalk();
            bool sent = session.SendSinpe(DebtTalk.StrangerAsk);
            string body = sent
                ? Loc.F("Revisás tu cuenta: nunca te cayó nada. Le mandaste {0} a un desconocido, y un SINPE no se devuelve.", Money.Format(DebtTalk.StrangerAsk))
                : Loc.T("No te alcanza en la cuenta, y por suerte: nunca te cayó ningún depósito.");
            modal.Show(Loc.T("Era una estafa"), string.Empty, body, new[] { Continue(null) });
        }

        // ---------------------------------------------------------------- the people of the town

        private const float PersonReach = 1.7f;

        /// <summary>Who sits or stands around town without a place of their own, besides those who walk.</summary>
        private static readonly string[] Bystanders =
        {
            "ClienteBarra", "ClienteMesa1", "ClienteMesa2", "ClienteMesa3", "PlayeroTowel", "PlayeraTowel", "PlayeroSombra", "DonMario", "Cajero", "Reo1", "Reo2", "Reo3"
        };

        private readonly List<Transform> people = new List<Transform>();

        private void BindPeople()
        {
            people.Clear();
            foreach (var walker in FindObjectsByType<Patrol>(FindObjectsSortMode.None))
            {
                people.Add(walker.transform);
            }

            foreach (string name in Bystanders)
            {
                var person = GameObject.Find(name);
                if (person != null)
                {
                    people.Add(person.transform);
                }
            }
        }

        /// <summary>The closest person within reach, or null. The guided run has its own characters to meet.</summary>
        private Transform NearbyPerson()
        {
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            if (hero == null || session.Guided)
            {
                return null;
            }

            Transform closest = null;
            float best = PersonReach;
            foreach (var person in people)
            {
                if (person == null || !person.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector2.Distance(person.position, hero.transform.position);
                if (distance < best)
                {
                    best = distance;
                    closest = person;
                }
            }

            return closest;
        }

        private void CloseCollectorsTalk(System.Action choice)
        {
            session.EndTalk();
            CollectorsLeave();
            choice();
        }
    }
}

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
    /// The windows of what happens to the player without asking: the owner who shouts from his house, the arrest,
    /// the car that runs the player over, the health insurance desk and the collectors at the curb. What each one
    /// costs is the finance engine's; this only shows it and moves the player to the next place.
    /// </summary>
    public sealed partial class GameUi
    {
        private GameObject warningCard;
        private Text warningText;

        private void BuildIncidents()
        {
            var card = UiKit.Card(canvas.transform, "Warning", GameUiColors.Pink, new Vector2(900f, 130f), new Vector2(0.5f, 1f), new Vector2(0f, -40f));
            warningCard = card.parent.gameObject;
            warningText = UiKit.Label(card, string.Empty, 34, UiKit.Ink, new Vector2(840f, 100f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter);
            warningCard.SetActive(false);

            PoliceOfficer.StoppedPlayer += OnStoppedByOfficer;
            TrafficCar.PlayerHit += OnPlayerHit;
            CollectorCar.Arrived += OnCollectorsArrived;
        }

        private void ReleaseIncidents()
        {
            PoliceOfficer.StoppedPlayer -= OnStoppedByOfficer;
            TrafficCar.PlayerHit -= OnPlayerHit;
            CollectorCar.Arrived -= OnCollectorsArrived;
            CollectorCar.Armed = false;
        }

        /// <summary>Runs every frame: shows the owner's countdown and tells the collectors whether the player owes enough.</summary>
        private void TickIncidents()
        {
            var watch = TrespassWatch.Active;
            bool shouting = watch != null && !modal.IsOpen;
            warningCard.SetActive(shouting);
            if (shouting)
            {
                warningText.text = watch.PoliceCalled ? watch.CalledWarning : watch.Warning + "  " + Mathf.CeilToInt(watch.Remaining) + " s";
            }

            CollectorCar.Armed = session.InArrears && !modal.IsOpen && !negotiationPanel.IsOpen && !phone.IsOpen && !pause.IsOpen;
        }

        // ---------------------------------------------------------------- the law

        /// <summary>
        /// A fine never comes by itself: an officer has to reach the player first. Only then the window opens, and the
        /// fine, the lost days and the trip to the station are charged when it is closed.
        /// </summary>
        private void OnStoppedByOfficer(OffenseKind offense)
        {
            if (offense == OffenseKind.RedLight && !SceneTransition.IsBusy && !modal.IsOpen)
            {
                ShowRedLightFine();
                return;
            }

            if (offense != OffenseKind.Trespass || SceneTransition.IsBusy || modal.IsOpen)
            {
                return;
            }

            var text = new StringBuilder();
            text.AppendLine(Loc.T("Un oficial te para y te pide los papeles: alguien llamó a la policía porque estás dentro de una casa que no es tuya."));
            text.AppendLine();
            text.AppendLine(Loc.F("Te pone una multa de {0}, te lleva a la comisaría y pierdes {1} días del mes.", Money.Format(MarketAssumptions.TrespassFine), MarketAssumptions.JailDaysLost));
            text.Append(Loc.F("Te quedas en la celda {0}. Puedes esperar a cumplir la condena o pagar la fianza.", JailSentence.Clock(JailSentence.TermSeconds(session.Loop.Arrests))));

            var options = new List<ModalOption>
            {
                new ModalOption
                {
                    Label = Loc.T("Continuar"),
                    Color = GameUiColors.Yellow,
                    OnPress = () =>
                    {
                        if (session.Arrest())
                        {
                            // The term depends on how many times the player was taken away before this one.
                            StartSentence(JailSentence.TermSeconds(session.Loop.Arrests - 1));
                            SceneTransition.Go(SceneNames.Carcel, SpawnIds.JailCell);
                        }
                    }
                }
            };

            modal.Show(Loc.T("El oficial te detiene"), Loc.T("Entrar a una casa ajena"), text.ToString(), options);
        }

        /// <summary>The officer saw the player cross on red. The fine is charged when the window is closed.</summary>
        private void ShowRedLightFine()
        {
            var text = new StringBuilder();
            text.AppendLine(Loc.T("Un oficial te vio cruzar con el semáforo en rojo y te para."));
            text.AppendLine();
            text.AppendLine(Loc.F("Te pone una multa de {0}. Esperar unos segundos en la acera no cuesta nada.", Money.Format(MarketAssumptions.RedLightFine)));
            text.AppendLine();
            text.Append(Loc.T("Cifras de ejemplo del juego."));

            var options = new List<ModalOption>
            {
                new ModalOption
                {
                    Label = Loc.T("Continuar"),
                    Color = GameUiColors.Yellow,
                    OnPress = () =>
                    {
                        CrossingSignal.ForgiveAll();
                        session.FineRedLight();
                    }
                }
            };

            modal.Show(Loc.T("El oficial te detiene"), Loc.T("Cruzar en rojo"), text.ToString(), options);
        }

        // ---------------------------------------------------------------- the street

        private void OnPlayerHit()
        {
            // A window that is open (an officer's fine, for one) is not interrupted by a car that goes by.
            if (SceneTransition.IsBusy || modal.IsOpen || !session.Hospitalize())
            {
                return;
            }

            SceneTransition.Go(SceneNames.Hospital, SpawnIds.HospitalBed);
        }

        // ---------------------------------------------------------------- health insurance

        private void ShowHealthDesk()
        {
            var text = new StringBuilder();
            var options = new List<ModalOption>();
            long premium = MarketAssumptions.HealthInsurancePremium;
            long bill = MarketAssumptions.HospitalBill;
            long insured = FinanceEngine.PatientShare(bill, MarketAssumptions.InsuredPatientSharePct);

            if (session.Holdings.HasHealthInsurance)
            {
                text.AppendLine(Loc.F("Ya tienes seguro de salud. Pagas {0} al mes y, si te internan, tu parte de la cuenta es de {1} %.", Money.Format(session.Holdings.HealthPremium), MarketAssumptions.InsuredPatientSharePct));
                options.Add(Back(Loc.T("1.  Salir")));
            }
            else
            {
                text.AppendLine(Loc.T("Un accidente no avisa. Una noche en el hospital, sin seguro, se paga completa."));
                text.AppendLine();
                text.AppendLine(Loc.F("Sin seguro, una internada como la de un atropello cuesta {0}.", Money.Format(bill)));
                text.AppendLine(Loc.F("Con seguro, tú pones {0} y pagas {1} cada mes, tengas o no tengas un accidente.", Money.Format(insured), Money.Format(premium)));
                text.AppendLine();
                text.AppendLine(Loc.T("El fondo de emergencia también sirve para esto: es la plata que ya está lista cuando llega el golpe."));
                text.AppendLine();
                text.Append(Loc.T("Cifras de ejemplo del juego. Es un seguro de salud genérico; no es el de ninguna institución."));
                options.Add(new ModalOption
                {
                    Label = Loc.F("1.  Contratar el seguro  ·  {0} al mes", Money.Format(premium)),
                    Color = GameUiColors.Green,
                    OnPress = () => { if (session.BuyHealthInsurance()) { ShowHealthDesk(); } }
                });
                options.Add(Back(Loc.T("2.  Por ahora no")));
            }

            modal.Show(Loc.T("Seguro de salud"), Loc.T("Recepción del hospital"), text.ToString(), options);
        }

        // ---------------------------------------------------------------- the collectors

        private void OnCollectorsArrived(CollectorCar car)
        {
            if (modal.IsOpen || SceneTransition.IsBusy)
            {
                return;
            }

            long demand = session.CollectionDemand;
            var text = new StringBuilder();
            text.AppendLine(Loc.T("Un carro negro, con los vidrios polarizados y la música a todo volumen, frena frente a ti. Se bajan dos hombres."));
            text.AppendLine();
            text.AppendLine(Loc.F("«Mae, usted debe plata. Hoy venimos por {0}.»", Money.Format(demand)));
            text.AppendLine();
            text.AppendLine(Loc.T("Pagar baja la deuda. Negociar paga la mitad hoy y deja el resto como un préstamo nuevo con recargo. Ignorarlos cuesta una multa por atraso y mucha tensión."));
            text.AppendLine();
            text.Append(Loc.T("Cifras de ejemplo del juego."));

            var options = new List<ModalOption>
            {
                new ModalOption { Label = Loc.F("1.  Pagar  ·  {0}", Money.Format(demand)), Color = GameUiColors.Green, OnPress = () => session.PayCollectors() },
                new ModalOption { Label = Loc.F("2.  Negociar  ·  pagar {0} hoy", Money.Format(demand / 2L)), Color = GameUiColors.Yellow, OnPress = () => session.NegotiateCollectors() },
                new ModalOption { Label = Loc.T("3.  Ignorarlos"), Color = GameUiColors.Pink, OnPress = () => session.IgnoreCollectors() }
            };

            modal.Show(Loc.T("Los cobradores"), Loc.T("Cobro por atraso"), text.ToString(), options);
        }
    }
}

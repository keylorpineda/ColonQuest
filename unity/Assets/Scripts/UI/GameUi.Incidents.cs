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

            TrespassWatch.Arrested += OnArrested;
            TrafficCar.PlayerHit += OnPlayerHit;
            CollectorCar.Arrived += OnCollectorsArrived;
        }

        private void ReleaseIncidents()
        {
            TrespassWatch.Arrested -= OnArrested;
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
                warningText.text = watch.Warning + "  " + Mathf.CeilToInt(watch.Remaining) + " s";
            }

            CollectorCar.Armed = session.InArrears && !modal.IsOpen && !negotiationPanel.IsOpen && !phone.IsOpen && !pause.IsOpen;
        }

        // ---------------------------------------------------------------- the law

        private void OnArrested()
        {
            if (SceneTransition.IsBusy || !session.Arrest())
            {
                return;
            }

            SceneTransition.Go(SceneNames.Carcel, SpawnIds.JailCell);
        }

        // ---------------------------------------------------------------- the street

        private void OnPlayerHit()
        {
            if (SceneTransition.IsBusy || !session.Hospitalize())
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
                text.AppendLine($"Ya tienes seguro de salud. Pagas {Money.Format(session.Holdings.HealthPremium)} al mes y, si te internan, tu parte de la cuenta es de {MarketAssumptions.InsuredPatientSharePct} %.");
                options.Add(Back("1.  Salir"));
            }
            else
            {
                text.AppendLine("Un accidente no avisa. Una noche en el hospital, sin seguro, se paga completa.");
                text.AppendLine();
                text.AppendLine($"Sin seguro, una internada como la de un atropello cuesta {Money.Format(bill)}.");
                text.AppendLine($"Con seguro, tú pones {Money.Format(insured)} y pagas {Money.Format(premium)} cada mes, tengas o no tengas un accidente.");
                text.AppendLine();
                text.AppendLine("El fondo de emergencia también sirve para esto: es la plata que ya está lista cuando llega el golpe.");
                text.AppendLine();
                text.Append(MarketAssumptions.ShortDisclaimer + " Es un seguro de salud genérico; no es el de ninguna institución.");
                options.Add(new ModalOption
                {
                    Label = $"1.  Contratar el seguro  ·  {Money.Format(premium)} al mes",
                    Color = GameUiColors.Green,
                    OnPress = () => { if (session.BuyHealthInsurance()) { ShowHealthDesk(); } }
                });
                options.Add(Back("2.  Por ahora no"));
            }

            modal.Show("Seguro de salud", "Recepción del hospital", text.ToString(), options);
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
            text.AppendLine("Un carro negro, con los vidrios polarizados y la música a todo volumen, frena frente a ti. Se bajan dos hombres.");
            text.AppendLine();
            text.AppendLine($"«Mae, usted debe plata. Hoy venimos por {Money.Format(demand)}.»");
            text.AppendLine();
            text.AppendLine("Pagar baja la deuda. Negociar paga la mitad hoy y deja el resto como un préstamo nuevo con recargo. Ignorarlos cuesta una multa por atraso y mucha tensión.");
            text.AppendLine();
            text.Append(MarketAssumptions.ShortDisclaimer);

            var options = new List<ModalOption>
            {
                new ModalOption { Label = $"1.  Pagar  ·  {Money.Format(demand)}", Color = GameUiColors.Green, OnPress = () => session.PayCollectors() },
                new ModalOption { Label = $"2.  Negociar  ·  pagar {Money.Format(demand / 2L)} hoy", Color = GameUiColors.Yellow, OnPress = () => session.NegotiateCollectors() },
                new ModalOption { Label = "3.  Ignorarlos", Color = GameUiColors.Pink, OnPress = () => session.IgnoreCollectors() }
            };

            modal.Show("Los cobradores", "Cobro por atraso", text.ToString(), options);
        }
    }
}

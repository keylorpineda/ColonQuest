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
            int days = JailSentence.TermDays(session.Loop.Arrests);
            text.AppendLine(Loc.F("Te pone una multa de {0}, te lleva a la comisaría y pierdes {1} días del mes.", Money.Format(MarketAssumptions.TrespassFine), days));
            text.Append(Loc.F("Te quedas en la celda {0} {1}. Puedes dormir en la litera para cumplir la condena o pagar la fianza.", days, days == 1 ? Loc.T("día") : Loc.T("días")));

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
                            StartSentence(session.Loop.JailDaysRemaining);
                            StartCoroutine(ArrestSequence());
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
            if (SceneTransition.IsBusy || modal.IsOpen || HitAnimation.Playing || session.Loop.HurtThisMonth)
            {
                return;
            }

            var player = GameObject.FindWithTag(Station.PlayerTag);
            effects.Flash(new Color(1f, 1f, 1f, 0.8f), 0.5f);
            HitAnimation.Play(player != null ? player.transform : null, TrafficCar.LastHitFrom, () =>
            {
                if (session.Hospitalize())
                {
                    SceneTransition.Go(SceneNames.Hospital, SpawnIds.HospitalBed);
                }
            });
        }

        /// <summary>
        /// The arrest, as a scene: siren and lights, the officer steps up beside the player, the player raises the hands,
        /// the handcuffs click on and they walk a few steps together. Then the station, where the cell gate slams shut.
        /// Space skips the scene; the sentence itself was already applied by the finance engine.
        /// </summary>
        private System.Collections.IEnumerator ArrestSequence()
        {
            AudioDirector.Effect(SoundEffectKind.Siren);
            effects.Siren(ArrestSirenSeconds);

            // The officer's lines belong to no place: they stay until they end.
            sceneStation = null;
            bool finished = false;
            var scene = ArrestScene();
            if (scene == null || !scene.Play(() => finished = true))
            {
                PlayerController.Frozen = true;
                yield return new WaitForSecondsRealtime(ArrestSirenSeconds);
                PlayerController.Frozen = false;
                finished = true;
            }

            while (!finished)
            {
                yield return null;
            }

            SceneTransition.Go(SceneNames.Carcel, SpawnIds.JailCell);
            while (SceneTransition.IsBusy)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            CellGate.Lock(true);
            effects.Flash(new Color(0f, 0f, 0f, 0.7f), 0.6f);
        }

        private const float ArrestSirenSeconds = 2.2f;

        /// <summary>The arrest with the officer who stopped the player, or null when no officer is close enough to act it.</summary>
        private Interaction ArrestScene()
        {
            var officer = NearestOfficer(HeroAt(), 6f);
            if (officer == null)
            {
                return null;
            }

            Vector2 me = HeroAt();
            float side = officer.position.x >= me.x ? 1f : -1f;
            Vector2 beside = me + new Vector2(side * 0.9f, 0f);
            Vector2 away = new Vector2(0f, -1.4f);
            string cop = officer.name;
            return new Interaction()
                .NpcWalk(cop, beside, 0.5f).Wait(0.5f).Face(beside)
                .Say("Oficial", PlayerProfile.Pick("¡Manos arriba! Queda detenido.", "¡Manos arriba! Queda detenida."))
                .Pose(PersonPose.HandsUp, 1.3f)
                .NpcPose(cop, PersonPose.Handover, 1.1f).Sound(SoundEffectKind.Cuffs)
                .Fly("cuffs", beside + new Vector2(0f, 0.3f), me + new Vector2(0f, 0.15f), 0.4f).Wait(0.6f)
                .Say("Oficial", "Acompáñeme a la comisaría.")
                .NpcWalk(cop, beside + away, 1.0f).WalkTo(me + away).Wait(0.3f);
        }

        private static Transform NearestOfficer(Vector2 at, float within)
        {
            Transform best = null;
            float bestDistance = within;
            foreach (var officer in FindObjectsByType<PoliceOfficer>(FindObjectsSortMode.None))
            {
                float distance = Vector2.Distance(officer.transform.position, at);
                if (officer.isActiveAndEnabled && distance <= bestDistance)
                {
                    best = officer.transform;
                    bestDistance = distance;
                }
            }

            return best;
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

        /// <summary>
        /// As the prologue tells it: the black car brakes in front of the player, two men get out and walk up to them, and only
        /// then the window with the choices opens. When the choice is made they walk back to the car and it drives off.
        /// </summary>
        private void OnCollectorsArrived(CollectorCar car)
        {
            if (modal.IsOpen || SceneTransition.IsBusy || Interaction.Playing)
            {
                return;
            }

            CollectorCar.Held = true;
            Vector2 at = car.transform.position;
            Vector2 me = HeroAt();
            string[] men = CollectorCar.MenNames;
            var left = me + new Vector2(-0.95f, 0.15f);
            var right = me + new Vector2(0.95f, 0.15f);
            var scene = new Interaction()
                .Wait(0.5f)
                .Apply(() => PlaceCollectors(at))
                .Sound(SoundEffectKind.DoorOpen).Reveal(men[0], true).Reveal(men[1], true)
                .NpcWalk(men[0], left, 1.3f).NpcWalk(men[1], right, 1.3f).Wait(1.35f)
                .Face(right)
                .Say("Cobrador", Loc.F("Mae, usted debe plata. Hoy venimos por {0}.", Money.Format(session.CollectionDemand))).Wait(2.4f)
                .Apply(ShowCollectorChoices);
            if (!scene.Play())
            {
                ShowCollectorChoices();
            }
        }

        /// <summary>The men step out next to the doors of the car, which has stopped wherever its brakes left it.</summary>
        private static void PlaceCollectors(Vector2 car)
        {
            for (int i = 0; i < CollectorCar.MenNames.Length; i++)
            {
                var man = GameObject.Find(CollectorCar.MenNames[i]);
                if (man != null)
                {
                    man.transform.position = car + new Vector2(i == 0 ? -0.6f : 0.6f, -0.7f);
                }
            }
        }

        /// <summary>The men walk back to the car, get in and it drives off.</summary>
        private void CollectorsLeave()
        {
            var car = CollectorCar.Instance;
            Vector2 at = car != null ? (Vector2)car.transform.position : HeroAt();
            string[] men = CollectorCar.MenNames;
            var scene = new Interaction()
                .NpcWalk(men[0], at + new Vector2(-0.6f, -0.7f), 1.1f).NpcWalk(men[1], at + new Vector2(0.6f, -0.7f), 1.1f).Wait(1.15f)
                .Sound(SoundEffectKind.Slam).Reveal(men[0], false).Reveal(men[1], false)
                .Apply(() => CollectorCar.Held = false);
            if (!scene.Play())
            {
                foreach (string man in men)
                {
                    var found = GameObject.Find(man);
                    if (found != null)
                    {
                        foreach (var renderer in found.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            renderer.enabled = false;
                        }
                    }
                }

                CollectorCar.Held = false;
            }
        }

        private void ShowCollectorChoices()
        {
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
                new ModalOption { Label = Loc.F("1.  Pagar  ·  {0}", Money.Format(demand)), Color = GameUiColors.Green, OnPress = () => { CollectorsLeave(); session.PayCollectors(); } },
                new ModalOption { Label = Loc.F("2.  Negociar  ·  pagar {0} hoy", Money.Format(demand / 2L)), Color = GameUiColors.Yellow, OnPress = () => { CollectorsLeave(); session.NegotiateCollectors(); } },
                new ModalOption { Label = Loc.T("3.  Ignorarlos"), Color = GameUiColors.Pink, OnPress = () => { CollectorsLeave(); session.IgnoreCollectors(); } }
            };

            modal.Show(Loc.T("Los cobradores"), Loc.T("Cobro por atraso"), text.ToString(), options);
        }
    }
}

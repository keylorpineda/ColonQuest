using System.Collections;
using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// The guided run: the same game, with the same money and the same models, on a short stretch of the town and for three
    /// months. It only says what to do next and keeps out what would get in the way (the clock, the traffic, the police, the
    /// other doors). Everything else, from the seller to the report at the end, is what a normal run shows.
    /// </summary>
    public sealed partial class GameUi
    {
        /// <summary>Where the run starts in the town: on the sidewalk, a few steps from the store.</summary>
        private static readonly Vector2 GuidedStart = new Vector2(11.5f, 4.8f);

        private bool guidedPending;
        private Coroutine guidedRun;
        private Objective guidedObjective;
        private StationType? guidedStation;
        private string guidedDoor;
        private StationType? guidedVisited;
        private bool demoLabNext;

        /// <summary>The next time the town loads, the guided run starts there.</summary>
        public void PlayGuidedNext()
        {
            if (guidedRun != null)
            {
                StopCoroutine(guidedRun);
                guidedRun = null;
            }

            guidedObjective = null;
            guidedStation = null;
            guidedPending = true;
            aiCalls.Clear();
            aiCalls.SetExpanded(true);
        }

        private void TryStartGuided(UnityEngine.SceneManagement.Scene scene)
        {
            if (guidedPending && scene.name == SceneNames.Town)
            {
                guidedPending = false;
                guidedRun = StartCoroutine(GuidedSequence());
            }
            else if (scene.name == SceneNames.Menu)
            {
                StopGuided();
                if (demoLabNext)
                {
                    demoLabNext = false;
                    StartCoroutine(OpenDemoLabOnMenu());
                }
            }
        }

        private void StopGuided()
        {
            if (guidedRun != null)
            {
                StopCoroutine(guidedRun);
            }

            guidedRun = null;
            guidedObjective = null;
            guidedStation = null;
            guidedDoor = null;
            Door.Allowed = null;
            session.LeaveGuided();
        }

        private static IEnumerator OpenDemoLabOnMenu()
        {
            yield return null;
            DemoLab.Open();
        }

        /// <summary>A place the guided run does not need yet says nothing, so the only prompt is the right one.</summary>
        private bool GuidedAllows(Station station)
        {
            return !session.Guided || (guidedStation.HasValue && station.Type == guidedStation.Value);
        }

        private bool GuidedDoorIsOpen(Door door)
        {
            return door.TargetScene == SceneNames.Town || door.TargetScene == guidedDoor;
        }

        /// <summary>True when nothing is on the screen waiting for the player or for the server.</summary>
        private bool GuidedIdle => !modal.IsOpen && !talkPanel.IsOpen && !negotiationPanel.IsOpen && !session.Busy && !Interaction.Playing
            && !SceneTransition.IsBusy && !LoadingScreen.IsLoading && session.CurrentEvent == null && session.CurrentNegotiation == null && session.CurrentTalk == null;

        /// <summary>Waits until the window and whatever it led to are over. Two frames apart, because one window can open the next.</summary>
        private IEnumerator UntilGuidedIdle()
        {
            do
            {
                yield return null;
            }
            while (!GuidedIdle);

            yield return null;
            while (!GuidedIdle)
            {
                yield return null;
            }
        }

        private IEnumerator GuidedSequence()
        {
            Door.Allowed = GuidedDoorIsOpen;
            Door.LockedThought = "Esa no es la ruta. Seguí la flecha.";
            while (LoadingScreen.IsLoading)
            {
                yield return null;
            }

            PlaceHero(GuidedStart);
            yield return null;

            ShowGuidedIntro();
            yield return UntilGuidedIdle();

            // Month 1: the salary, a few steps so the player learns the arrow, and an event written for this player.
            session.PayGuidedQuincena();
            guidedStation = StationType.Garage;
            guidedVisited = null;
            guidedObjective = new Objective("Caminá hasta el taller: ahí empieza tu primer mes", SceneNames.Town, "Station_Garage");
            while (guidedVisited != StationType.Garage)
            {
                yield return null;
            }

            guidedStation = null;
            guidedObjective = new Objective("El game master está leyendo tu situación");
            yield return UntilGuidedIdle();
            yield return WaitForThePlan();
            yield return CloseGuidedMonth();

            // Month 2: the seller, who keeps quiet about what nobody asks.
            session.PayGuidedQuincena();
            yield return UntilGuidedIdle();
            guidedDoor = SceneNames.Tienda;
            guidedStation = StationType.Store;
            guidedVisited = null;
            guidedObjective = new Objective("Andá a la tienda y preguntá el costo real", SceneNames.Tienda, "Station_Store");
            while (guidedVisited != StationType.Store)
            {
                yield return null;
            }

            guidedStation = null;
            guidedObjective = null;
            yield return UntilGuidedIdle();
            yield return CloseGuidedMonth();

            // Month 3: what comes after those decisions. The way out of the store first.
            session.PayGuidedQuincena();
            yield return UntilGuidedIdle();
            guidedDoor = null;
            var partner = GuidedRoute.Consequence(session.State);
            yield return GuidedConsequence(partner);
            yield return CloseGuidedMonth();
        }

        private void ShowGuidedIntro()
        {
            System.Action<GuidedProfile> choose = profile =>
            {
                GuidedRoute.Apply(session.State, profile);
                session.GuidedProfileChosen();
            };

            modal.Show(
                Loc.T("Tres meses con un salario real"),
                Loc.T("Simulador de finanzas personales"),
                Loc.T("Esta ruta resume tres meses en unos minutos, por eso todo pasa seguido. En el modo historia cada mes se vive con calma.\n\nGanás el salario mínimo. Seguí la flecha roja y apretá E al llegar. A la derecha ves cada llamada real a los modelos de Meta."),
                new List<ModalOption>
                {
                    new ModalOption { Label = Loc.T("1.  Llego justo y con la tarjeta cargada"), Color = GameUiColors.Yellow, OnPress = () => choose(GuidedProfile.Tight) },
                    new ModalOption { Label = Loc.T("2.  Tengo algo ahorrado"), Color = GameUiColors.Green, OnPress = () => choose(GuidedProfile.Saver) }
                });
        }

        /// <summary>The plan of Muse Wonder arrives in the background. The player walks meanwhile; the next event has to wait for it, or the plan would be lost.</summary>
        private IEnumerator WaitForThePlan()
        {
            guidedObjective = new Objective("Un momento: Muse Wonder está escribiendo tu año...");
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < GuidedRoute.PlanWaitSeconds && !DemoReadout.PlannerAnswered(aiCalls.Last))
            {
                yield return null;
            }

            guidedObjective = null;
        }

        private IEnumerator CloseGuidedMonth()
        {
            guidedObjective = null;
            session.CloseGuidedMonth();
            yield return UntilGuidedIdle();
        }

        private IEnumerator GuidedConsequence(TalkPartner partner)
        {
            bool lender = partner == TalkPartner.Lender;
            var station = lender ? StationType.Lender : StationType.Pyramid;
            guidedStation = station;
            guidedVisited = null;
            guidedObjective = lender
                ? new Objective("Andá a Préstamos Rápidos y preguntá antes de firmar", SceneNames.Town, "Station_Lender")
                : new Objective("Andá a Invierte Ya y preguntá de dónde sale la ganancia", SceneNames.Town, "Station_Pyramid");
            while (guidedVisited != station)
            {
                yield return null;
            }

            guidedStation = null;
            guidedObjective = null;
            yield return UntilGuidedIdle();

            // What was signed is in the history of the run, whatever the money was taken from.
            string offer = DebtTalk.OfferEvent(partner).EventId + ":" + DebtTalk.TakeOffer;
            bool signed = session.State.History.Exists(entry => entry.StartsWith(offer, System.StringComparison.Ordinal));
            if (signed && lender)
            {
                yield return new WaitForSeconds(GuidedRoute.CollectorsDelaySeconds);
                var car = CollectorCar.Instance;
                if (car != null)
                {
                    car.StopFor(HeroAt());
                    yield return UntilGuidedIdle();
                }
            }
            else if (signed)
            {
                modal.Show(
                    Loc.T("Invierte Ya dejó de contestar"),
                    string.Empty,
                    Loc.F("Los primeros cobraron con la plata de los que entraron después. Tus {0} no vuelven.", Money.Format(DebtTalk.ExampleLoan)),
                    new[] { Continue(null) });
                yield return UntilGuidedIdle();
            }
        }

        private static void PlaceHero(Vector2 at)
        {
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            if (hero != null)
            {
                hero.transform.position = at;
                hero.GetComponent<Rigidbody2D>().position = at;
            }
        }

        /// <summary>The way out of the guided run: the buttons that close the report of the three months.</summary>
        private List<ModalOption> GuidedEndOptions()
        {
            return new List<ModalOption>
            {
                new ModalOption { Label = Loc.T("Jugar libre en el pueblo"), Color = GameUiColors.Green, OnPress = Restart },
                new ModalOption
                {
                    Label = Loc.T("Ver cómo funciona por dentro"),
                    Color = GameUiColors.Yellow,
                    OnPress = () =>
                    {
                        demoLabNext = true;
                        GoToMenu();
                    }
                }
            };
        }
    }
}

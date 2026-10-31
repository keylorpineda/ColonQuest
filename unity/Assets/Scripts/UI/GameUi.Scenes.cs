using System;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// The little scenes of the everyday places, like the actions of The Sims. Every interaction walks the player to the spot,
    /// takes a pose, makes an object or another person react and plays a sound; only at the end it runs the original action
    /// (the window of options, the task, the swim), so a window never opens in place of the gesture. Space skips a scene.
    /// What an action costs or pays is always decided by the finance engine inside that action.
    /// </summary>
    public sealed partial class GameUi
    {
        private static readonly Color Night = new Color(0.02f, 0.03f, 0.1f, 0.94f);
        private static readonly Color Dawn = new Color(0.02f, 0.03f, 0.1f, 0f);
        private static readonly Color TvLight = new Color(0.45f, 0.72f, 1f, 0.55f);
        private static readonly Color FridgeLight = new Color(1f, 0.98f, 0.82f, 0.5f);
        private static readonly Color HeadLight = new Color(1f, 0.93f, 0.55f, 0.5f);

        private void OnSpoke(string speaker, string text)
        {
            ShowSpeech(sceneStation, string.IsNullOrEmpty(speaker) ? string.Empty : Loc.T(speaker), Loc.T(text));
        }

        /// <summary>The station whose scene plays now: the lines said in it belong to that place.</summary>
        private Station sceneStation;

        /// <summary>Plays the scene of the station, and runs the original action when the scene says so. Without a scene the action runs at once.</summary>
        private void Interact(Station station)
        {
            if (Interaction.Playing)
            {
                return;
            }

            if (InHouseNotMine())
            {
                Thoughts.Say(HouseVisit.NotMineThought);
                return;
            }

            sceneStation = station;

            var scene = SceneFor(station, () => InteractNow(station));
            if (scene == null || !scene.Play())
            {
                InteractNow(station);
            }
        }

        private static Vector2 HeroAt()
        {
            var hero = GameObject.FindWithTag(Station.PlayerTag);
            return hero != null ? (Vector2)hero.transform.position : Vector2.zero;
        }

        /// <summary>
        /// The phone: the hand goes to the pocket, brings it up and holds it while the screen is open; when it closes, the phone
        /// goes back to the pocket.
        /// </summary>
        private void OpenPhone()
        {
            PlayerController.Blocked = true;
            var hero = GameObject.FindWithTag("Player");
            if (hero != null && hero.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
            }

            var scene = new Interaction().Pose(PersonPose.Pocket, 0.28f).Sound(SoundEffectKind.Swish).HoldProp("phone")
                .Pose(PersonPose.Type, 0.5f).Sound(SoundEffectKind.Keys).Wait(0.12f).Sound(SoundEffectKind.Chime)
                .Apply(() => phone.Open())
                .WaitUntil(() => !phone.IsOpen)
                .Pose(PersonPose.Pocket, 0.25f).ReleaseProp().Sound(SoundEffectKind.Swish).Pose(PersonPose.None, 0.1f);
            if (!scene.Play())
            {
                phone.Open();
            }
        }

        /// <summary>
        /// Closing a deal at the store: the seller slides the contract over, the player signs it, the seller stamps it, they shake
        /// hands and the box changes owner. The purchase itself is made by the finance engine in <paramref name="accept"/>.
        /// </summary>
        private void PlayContract(Action accept)
        {
            negotiationPanel.Hide();
            Vector2 me = HeroAt();
            Vector2 seller = PersonAt("Vendedor", me + new Vector2(0f, 2f));
            var scene = new Interaction().Face(seller)
                .NpcPose("Vendedor", PersonPose.Handover, 1.2f).Say("Kevin", "Perfecto, mae. Firme aquí y es suyo.")
                .Sound(SoundEffectKind.Swish).Fly("paper", seller, me + new Vector2(0f, 0.3f), 0.8f)
                .Wait(0.9f).Pose(PersonPose.Sign, 1.8f).Sound(SoundEffectKind.Scribble)
                .Wait(0.5f).Sound(SoundEffectKind.Stamp).Fly("stamp", seller + new Vector2(0f, -0.3f), seller + new Vector2(0f, -0.4f), 0.3f)
                .Wait(0.5f).Pose(PersonPose.Handover, 0.8f).NpcPose("Vendedor", PersonPose.Wave, 1.2f)
                .Sound(SoundEffectKind.Coin).Fly("box", seller, me + new Vector2(0f, 0.2f), 0.9f)
                .Wait(0.9f).Apply(accept);
            if (!scene.Play())
            {
                accept();
            }
        }

        private static Vector2 Here(Station station, float dx = 0f, float dy = 0f)
        {
            return (Vector2)station.transform.position + new Vector2(dx, dy);
        }

        /// <summary>How far above the root of a town bench its seat is (see the bench in the map builder).</summary>
        private const float BenchSeatY = 0.3f;

        /// <summary>The seat of the house sofa relative to its station, which stands beside it.</summary>
        private static readonly Vector2 SofaSeatOffset = new Vector2(-0.95f, -0.45f);

        /// <summary>The seat of the sofa in this scene, or the fallback when there is none.</summary>
        private static Vector2 SofaSeat(Vector2 fallback)
        {
            var sofa = GameObject.Find("Thing_Sofa");
            return sofa != null ? (Vector2)sofa.transform.position + SofaSeatOffset : fallback;
        }

        private static Vector2 PersonAt(string name, Vector2 fallback)
        {
            var found = GameObject.Find(name);
            return found != null ? (Vector2)found.transform.position : fallback;
        }

        private Interaction SceneFor(Station station, Action then)
        {
            switch (station.Type)
            {
                case StationType.Bank:
                    return BankScene(station, then);
                case StationType.Atm:
                    return AtmScene(station, then);
                case StationType.Home:
                    return DeskScene(station, then);
                case StationType.Wardrobe:
                    return WardrobeScene(station, then);
                case StationType.Store:
                    return StoreScene(station, then);
                case StationType.Beach:
                    return BeachScene(station, then);
                case StationType.Work:
                    return WorkScene(station, then);
                case StationType.HealthDesk:
                    return SignScene(station, then, "Doctora", "La recepción", "Llene este formulario y firme aquí, por favor.", false);
                case StationType.RealEstate:
                    return SignScene(station, then, "AgenteInmobiliaria", "La agente", "Le muestro las casas. Si le gusta una, firmamos y le doy las llaves.", true);
                case StationType.CarDealer:
                    return SignScene(station, then, "DonChepe", "Don Chepe", "Mire qué carro tan bonito. Con una prima se lo lleva hoy mismo.", true);
                case StationType.Farm:
                    return SignScene(station, then, "DonBeto", "Don Beto", "La tierra da, pero hay que saber cuándo sembrar y cuándo esperar.", false);
                case StationType.Thing:
                    return ThingScene(station, then);
                case StationType.Garage:
                case StationType.Grocery:
                case StationType.Lender:
                case StationType.Pyramid:
                case StationType.Soda:
                case StationType.Coop:
                    if (!session.Loop.CanDoTask(station.Type))
                    {
                        return null;
                    }

                    // The game master starts writing now, so the greeting hides most of the wait.
                    session.PrefetchTask(station.Type);
                    return TalkScene(station, then);
                default:
                    return null;
            }
        }

        // ---------------------------------------------------------------- the bank

        /// <summary>Closing the month at the teller: the bills go over, the teller counts, the printer runs and the statement gets its stamp.</summary>
        private Interaction BankScene(Station station, Action then)
        {
            Vector2 me = Here(station);
            Vector2 teller = PersonAt("Cajera", Here(station, 0f, 2.3f));
            return WaitTurn(new Interaction().WalkTo(me + new Vector2(0f, -0.9f)).Face(teller), BankCustomer.Window2, "Cajera", "Cajera")
                .WalkTo(me).Face(teller)
                .Say("Cajera", "Buenas. ¿Viene a cerrar el mes?")
                .Pose(PersonPose.Handover, 0.7f)
                .Sound(SoundEffectKind.Bills).Fly("bills", me + new Vector2(0f, 0.4f), teller, 0.8f)
                .NpcPose("Cajera", PersonPose.Count, 1.6f).Wobble("Cajera", 0.3f)
                .Wait(0.9f).Sound(SoundEffectKind.Coin)
                .Wait(0.7f).Sound(SoundEffectKind.Printer)
                .Wait(0.5f).Fly("receipt", teller + new Vector2(0f, -0.2f), me + new Vector2(0f, 0.3f), 0.9f)
                .Wait(0.8f).Sound(SoundEffectKind.Stamp).Fly("stamp", teller + new Vector2(0f, -0.4f), teller + new Vector2(0f, -0.5f), 0.3f)
                .Pose(PersonPose.Read, 1.2f)
                .Apply(then);
        }

        /// <summary>
        /// When a customer is at the window, the player waits behind them: the teller asks for a moment and the scene goes
        /// on once the window is free, or after a few seconds at most.
        /// </summary>
        private static Interaction WaitTurn(Interaction scene, Vector2 window, string teller, string speaker)
        {
            if (!BankCustomer.IsBeingServed(window))
            {
                return scene;
            }

            float until = Time.time + BankWaitSeconds;
            return scene.NpcPose(teller, PersonPose.Wave, 0.8f).Say(speaker, "Un momentito, ya casi termino con el cliente.")
                .WaitUntil(() => !BankCustomer.IsBeingServed(window) || Time.time > until).Wait(0.4f);
        }

        /// <summary>The longest the player waits in line before the teller turns to them.</summary>
        private const float BankWaitSeconds = 8f;

        /// <summary>The cash machine: the card goes in, the keys are tapped, the bills come out and the receipt prints.</summary>
        private Interaction AtmScene(Station station, Action then)
        {
            Vector2 me = Here(station, 0f, -0.5f);
            Vector2 machine = Here(station, 0f, 0.5f);
            return new Interaction().WalkTo(me).Face(machine)
                .Pose(PersonPose.Reach, 0.5f).Fly("card", me + new Vector2(0.2f, 0.4f), machine, 0.5f).Sound(SoundEffectKind.Beep)
                .Wait(0.5f).Pose(PersonPose.Type, 1.4f).Sound(SoundEffectKind.Keys)
                .Wait(0.5f).Sound(SoundEffectKind.Beep).Wait(0.5f)
                .Sound(SoundEffectKind.Bills).Fly("bills", machine, me + new Vector2(0.2f, 0.4f), 0.8f)
                .Wait(0.7f).Sound(SoundEffectKind.Printer).Fly("receipt", machine, me + new Vector2(-0.2f, 0.3f), 0.9f)
                .Wait(0.7f).Apply(then);
        }

        // ---------------------------------------------------------------- the house

        /// <summary>The desk with the bills: the player sits with the papers, writes on them and reads the statement.</summary>
        private Interaction DeskScene(Station station, Action then)
        {
            Vector2 me = Here(station);
            return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f))
                .Fly("paper", Here(station, 0.6f, 0.6f), me + new Vector2(0f, 0.3f), 0.6f)
                .Pose(PersonPose.Read, 1.6f).Sound(SoundEffectKind.Swish)
                .Wait(0.4f).Pose(PersonPose.Sign, 1.1f).Sound(SoundEffectKind.Scribble)
                .Apply(then);
        }

        private Interaction ThingScene(Station station, Action then)
        {
            // A service that was cut leaves only a thought; the gesture would make no sense.
            if (CutLine(station) != null)
            {
                return null;
            }

            Vector2 me = Here(station);
            switch (SceneKey(station.name))
            {
                case "Thing_Bed":
                    return BedScene(station, then);

                case "Thing_CellCot":
                    return CellCotScene(station, then);

                case "Thing_Stove":
                    return StoveScene(station, then);

                case "Thing_Shower":
                {
                    // The clothes come off behind the glass with a pixel blur on the body, and go back on at the end.
                    var scene = new Interaction().WalkInto(me).Sound(SoundEffectKind.Swish).Pose(PersonPose.Reach, 0.5f)
                        .Undress(4.4f).Wait(0.3f).Pose(PersonPose.Wash, 3.2f).Sound(SoundEffectKind.Water).Steam(Here(station, 0f, 0.4f), 3f);
                    for (int i = 0; i < 6; i++)
                    {
                        scene.Fly("drop", Here(station, -0.25f + i * 0.1f, 1.1f), Here(station, -0.25f + i * 0.1f, -0.1f), 0.5f).Wait(0.45f);
                    }

                    return scene.Pose(PersonPose.None, 0.2f).Sound(SoundEffectKind.Swish).Wear(session.Loop.Outfit)
                        .Say(string.Empty, "Un baño corto y a seguir con las cuentas.").Apply(then);
                }

                case "Thing_Toilet":
                    // The screen dims while the player uses the toilet, the flush sounds and the light comes back.
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Fade(new Color(0.02f, 0.03f, 0.1f, 0.85f), 0.5f)
                        .Wait(1.2f).Sound(SoundEffectKind.Water).Wait(0.8f).Fade(Dawn, 0.5f).Apply(then);

                case "Thing_Tv":
                    // Watching from the sofa that faces the TV; without it, from the floor in front of the screen.
                    return new Interaction().SitOn(SofaSeat(Here(station, 0f, -1.3f)), 0.1f).Face(me)
                        .Pose(PersonPose.Sit, 3.4f).Sound(SoundEffectKind.Beep)
                        .Flicker(Here(station, 0f, 0.3f), new Vector2(1.1f, 0.7f), TvLight, 3.2f)
                        .Wait(1.6f).Sound(SoundEffectKind.Chime).Wait(1.6f).Apply(then);

                case "Thing_Screens":
                    return new Interaction().Face(me).Pose(PersonPose.Reach, 0.8f).Sound(SoundEffectKind.Beep)
                        .Flicker(Here(station, 0f, 0.4f), new Vector2(2.2f, 0.9f), TvLight, 2.2f).Wait(1.4f).Apply(then);

                case "Thing_Fridge":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f))
                        .Pose(PersonPose.Reach, 0.6f).Sound(SoundEffectKind.Knock)
                        .Flicker(Here(station, 0f, 0.4f), new Vector2(0.9f, 1.2f), FridgeLight, 1.6f)
                        .Wait(1.4f).Apply(then);

                case "Thing_Sofa":
                    return new Interaction().SitOn(SofaSeat(me), 0.1f).Face(Here(station, -3f, 0f)).Pose(PersonPose.Sit, 2.6f)
                        .Sound(SoundEffectKind.Swish).Wait(0.4f).Apply(then);

                case "Thing_Bench":
                    // The seat is above the root of the bench; the backrest is behind it.
                    return new Interaction().SitOn(Here(station, 0f, BenchSeatY), 2.6f).Sound(SoundEffectKind.Swish)
                        .Wait(0.4f).Apply(then);

                case "Thing_DiningTable":
                {
                    // The chair on the near side of the table, so the player sits facing the screen with the table behind.
                    Vector2 chair = Here(station, -0.6f, -0.95f);
                    return new Interaction().SitOn(chair, 0.1f).Pose(PersonPose.SitEat, 2.4f)
                        .Fly("plate", Here(station, 0.6f, 0.2f), chair + new Vector2(0f, 0.15f), 0.6f).Sound(SoundEffectKind.Munch)
                        .Wait(1.2f).Sound(SoundEffectKind.Munch).Wait(1.1f).Apply(then);
                }

                case "Thing_Coffee":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Pose(PersonPose.Eat, 1.8f)
                        .Steam(Here(station, 0f, 0.6f), 1.6f).Sound(SoundEffectKind.Swish).Wait(1.2f).Apply(then);

                case "Thing_Sink":
                case "Thing_Washer":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Pose(PersonPose.Wash, 1.8f)
                        .Sound(SoundEffectKind.Water).Fly("drop", Here(station, 0f, 0.9f), Here(station, 0f, 0.3f), 0.5f).Wait(1.4f).Apply(then);

                case "Thing_FinePrint":
                    return new Interaction().WalkTo(me).Fly("paper", Here(station, 0.5f, 0.6f), me + new Vector2(0f, 0.3f), 0.6f)
                        .Pose(PersonPose.Read, 2.2f).Sound(SoundEffectKind.Swish).Wait(0.6f).Apply(then);

                case "Thing_TicketMachine":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Pose(PersonPose.Reach, 0.6f).Sound(SoundEffectKind.Beep)
                        .Wait(0.4f).Sound(SoundEffectKind.Printer).Fly("paper", Here(station, 0f, 0.7f), me + new Vector2(0f, 0.3f), 0.9f)
                        .Wait(0.9f).Pose(PersonPose.Read, 1f).Sound(SoundEffectKind.Chime).Apply(then);

                case "Thing_Counter":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Pose(PersonPose.Handover, 0.7f)
                        .Fly("paper", me + new Vector2(0f, 0.4f), PersonAt("Oficial", Here(station, 0f, 1.5f)), 0.7f)
                        .NpcPose("Oficial", PersonPose.Sign, 1.8f).Sound(SoundEffectKind.Scribble).Wait(1.2f).Sound(SoundEffectKind.Stamp)
                        .Wait(0.5f).Apply(then);

                case "Thing_Judge":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f))
                        .NpcPose("Juez", PersonPose.Sign, 1.6f).Sound(SoundEffectKind.Scribble).Wait(1.0f)
                        .Sound(SoundEffectKind.Knock).Wobble("Juez", 0.5f).Wait(0.8f).Apply(then);

                case "Thing_EmergencyWard":
                    // The player only looks at the beds; there is no seat in the middle of the ward.
                    return new Interaction().WalkTo(me).Face(Here(station, 1f, 0f))
                        .Sound(SoundEffectKind.Beep).Wait(0.8f).Sound(SoundEffectKind.Beep).Wait(0.8f).Apply(then);

                case "Thing_Fountain":
                    return new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)).Pose(PersonPose.Handover, 0.7f)
                        .Fly("coin", me + new Vector2(0f, 0.5f), Here(station, 0f, 0.3f), 0.7f)
                        .Wait(0.7f).Sound(SoundEffectKind.Coin).Sound(SoundEffectKind.Splash)
                        .Say(string.Empty, "Pedí un deseo: llegar a fin de mes con ahorro.").Wait(1f).Apply(then);

                case "Thing_Talk":
                {
                    // A passer-by: the player waves and the other person waves back.
                    string person = station.transform.parent != null ? station.transform.parent.name : string.Empty;
                    return new Interaction().Face(Here(station)).Pose(PersonPose.Wave, 1.4f).NpcPose(person, PersonPose.Wave, 1.6f)
                        .Sound(SoundEffectKind.Chime).Wait(0.3f).Apply(then);
                }

                case "Thing_Guard":
                case "Thing_Receptionist":
                case "Thing_Cashier":
                    return new Interaction().Face(Here(station, 0f, 1f)).Pose(PersonPose.Wave, 1.4f).Sound(SoundEffectKind.Chime).Wait(0.3f).Apply(then);

                case "Thing_Teller3":
                {
                    var scene = WaitTurn(new Interaction().WalkTo(me).Face(Here(station, 0f, 1f)), BankCustomer.Window4, "Cajero2", "Cajero");
                    return scene.Pose(PersonPose.Wave, 1f).NpcPose("Cajero2", PersonPose.Wave, 1f).Sound(SoundEffectKind.Chime).Wait(0.3f).Apply(then);
                }

                case "Thing_Manager":
                case "Thing_CreditAdvisor":
                {
                    // The player stands on the guest side of the desk while the other person talks.
                    string person = station.name == "Thing_Manager" ? "Gerente" : "Asesora";
                    Vector2 desk = PersonAt(person, Here(station, 0f, 1.3f));
                    return new Interaction().WalkTo(me).Face(desk).Pose(PersonPose.Wave, 0.9f).NpcPose(person, PersonPose.Wave, 1.2f)
                        .Sound(SoundEffectKind.Chime).Wait(0.4f).Fly("paper", desk + new Vector2(0f, -0.4f), me + new Vector2(0f, 0.3f), 0.7f)
                        .Pose(PersonPose.Read, 1.2f).Apply(then);
                }

                case "Thing_Pet":
                    // The player bends to pet the dog and the dog wiggles with joy.
                    return new Interaction().Face(me).Pose(PersonPose.Reach, 1.6f).Wobble("Pet", 1.4f)
                        .Sound(SoundEffectKind.Swish).Wait(0.6f).Sound(SoundEffectKind.Chime).Wait(0.8f).Apply(then);

                case "Thing_Doctor":
                {
                    // Each doctor of the upper floor greets, writes the note and hands it over.
                    string doctor = station.name.Substring("Thing_".Length);
                    Vector2 desk = PersonAt(doctor, Here(station, 0f, 0.9f));
                    return new Interaction().Face(desk).NpcPose(doctor, PersonPose.Wave, 1.2f).Sound(SoundEffectKind.Chime)
                        .Wait(0.6f).NpcPose(doctor, PersonPose.Sign, 1.4f).Sound(SoundEffectKind.Scribble).Wait(1.0f)
                        .Fly("paper", desk + new Vector2(0f, -0.2f), me + new Vector2(0f, 0.3f), 0.6f).Pose(PersonPose.Read, 1.0f).Apply(then);
                }

                case "Thing_Motorbike":
                    return new Interaction().Face(me).Pose(PersonPose.Reach, 0.8f).Sound(SoundEffectKind.Keys)
                        .Fly("key", Here(station, 0f, 0.3f), me + new Vector2(0f, 0.3f), 0.5f).Wobble("Motorbike", 0.5f)
                        .Wait(0.5f).Pose(PersonPose.Read, 0.9f).Apply(then);

                case "Thing_Accessories":
                    return new Interaction().Face(me).Pose(PersonPose.Reach, 0.7f).Sound(SoundEffectKind.Swish)
                        .Fly("box", Here(station, 0f, 0.4f), me + new Vector2(0f, 0.3f), 0.6f).Wait(0.3f)
                        .Pose(PersonPose.Read, 1.2f).Wait(0.3f).Apply(then);

                case "Thing_Car":
                    // A look at the car: the key, the chirp of the alarm and the headlights blink twice.
                    return new Interaction().Face(me).Pose(PersonPose.Reach, 0.6f).Sound(SoundEffectKind.Keys).Wait(0.4f)
                        .Sound(SoundEffectKind.Beep).Flicker(Here(station, 0f, 0.1f), new Vector2(2.2f, 0.8f), HeadLight, 0.8f)
                        .Wait(0.5f).Sound(SoundEffectKind.Beep).Wait(0.5f).Apply(then);

                case "Thing_Workshop":
                    return new Interaction().Face(me).Pose(PersonPose.Read, 1.6f).Sound(SoundEffectKind.Knock)
                        .Wait(0.6f).Sound(SoundEffectKind.Knock).Wait(0.6f).Apply(then);

                case "Thing_RatesBoard":
                    return new Interaction().Face(me).Pose(PersonPose.Read, 1.8f).Sound(SoundEffectKind.Beep)
                        .Flicker(Here(station, 0f, 0.2f), new Vector2(2f, 0.9f), TvLight, 1.6f).Wait(1.2f).Apply(then);

                default:
                    return null;
            }
        }

        /// <summary>
        /// The scene a thing plays, by the name of its object. The people who chat on the street (Thing_Talk_DonMario...) all
        /// wave like a passer-by, and the three doctors of the upper floor share one scene.
        /// </summary>
        public static string SceneKey(string thingName)
        {
            if (thingName.StartsWith("Thing_Talk"))
            {
                return "Thing_Talk";
            }

            return thingName.StartsWith("Thing_Doctor") ? "Thing_Doctor" : thingName;
        }

        /// <summary>
        /// Sleeping: the player walks to the bed and lies down, the Z letters rise, the screen goes dark, the alarm rings and the
        /// morning comes back: the clock skips to the next week. Before the evening it is only a short nap.
        /// </summary>
        private Interaction BedScene(Station station, Action then)
        {
            var clock = session.Loop.Clock;
            if (!clock.IsNight)
            {
                // Before the evening it is only a rest: the player lies down a moment and gets up again.
                Vector2 spot = Here(station);
                return new Interaction().WalkInto(spot).Sound(SoundEffectKind.Swish).Pose(PersonPose.Sleep, 2.6f)
                    .Zzz(spot + new Vector2(0.2f, 0.5f), 2f).Wait(0.2f).Apply(then);
            }

            // The month moves on only at the bank, so the last week wakes up in the same week with a reminder.
            bool lastWeek = clock.Day + 1 >= GameRules.DaysPerMonth;
            bool missedWork = !session.Loop.Job.WorkedToday && !clock.IsLastDay;
            Vector2 bed = Here(station);
            return new Interaction().WalkInto(bed).Say(string.Empty, "Ya es tarde. Toca dormir.")
                .Pose(PersonPose.Sleep, 0.8f).Zzz(bed + new Vector2(0.2f, 0.5f), 3f)
                .Fade(Night, 1.4f).Wait(1.4f)
                .Apply(() => session.Sleep())
                .Sound(SoundEffectKind.Alarm).Wait(1.1f)
                .Pose(PersonPose.None, 0.3f).Fade(Dawn, 1f)
                .Say(string.Empty, lastWeek ? LastWeekThought : (missedWork ? MissedWorkThought : "Dormí rico. Amanecí con ganas de arreglar mis cuentas."));
        }

        private Interaction CellCotScene(Station station, Action then)
        {
            if (session.Loop.JailDaysRemaining <= 0)
            {
                return new Interaction().Say(string.Empty, Loc.T(SentenceDoneThought));
            }

            Vector2 cot = Here(station);
            int remainingAfter = Math.Max(0, session.Loop.JailDaysRemaining - 1);
            string thought = remainingAfter > 0
                ? Loc.F("Dormí en la litera. Me quedan {0} {1} de condena.", remainingAfter, remainingAfter == 1 ? Loc.T("día") : Loc.T("días"))
                : Loc.T(SentenceDoneThought);

            return new Interaction().WalkInto(cot).Say(string.Empty, Loc.T("Toca descansar en la celda..."))
                .Pose(PersonPose.Sleep, 0.8f).Zzz(cot + new Vector2(0.2f, 0.5f), 2.5f)
                .Fade(Night, 1.2f).Wait(1.2f)
                .Apply(() => session.SleepInJail())
                .Sound(SoundEffectKind.Alarm).Wait(0.8f)
                .Pose(PersonPose.None, 0.3f).Fade(Dawn, 1f)
                .Say(string.Empty, thought);
        }

        public const string LastWeekThought = "Última semana del mes: llegaron los recibos y toca ir al banco a cobrar el salario.";
        public const string MissedWorkThought = "Ayer no fui a trabajar. Eso me lo van a rebajar del salario.";

        /// <summary>
        /// The day ended away from the bed: the player nods off where they stand, the screen goes dark and the next morning
        /// comes with less mood. The finance engine applies the loss.
        /// </summary>
        private void FallAsleepScene()
        {
            bool lastWeek = session.Loop.Clock.Day + 1 >= GameRules.DaysPerMonth;
            bool missedWork = !session.Loop.Job.WorkedToday && !session.Loop.Clock.IsLastDay;
            Vector2 me = HeroAt();
            var scene = new Interaction().Say(string.Empty, "Se me hizo tardísimo... ya no aguanto el sueño.")
                .Zzz(me + new Vector2(0.2f, 0.6f), 2f).Fade(Night, 1.2f).Wait(1.2f)
                .Apply(() => session.FallAsleep())
                .Sound(SoundEffectKind.Alarm).Wait(0.8f).Fade(Dawn, 1f)
                .Say(string.Empty, lastWeek ? LastWeekThought : (missedWork ? MissedWorkThought : "Amanecí todo torcido. Mejor dormir en la cama la próxima vez."));
            if (!scene.Play())
            {
                session.FallAsleep();
            }
        }

        /// <summary>
        /// The wardrobe: the player stands at its doors, they swing open and, once bought, the swimsuit hangs inside. The
        /// window to buy it opens after the gesture, and the doors close again before the player is free. The wardrobe
        /// stands against the wall of the living room, so its doors face the bedroom on the right, not down the screen.
        /// </summary>
        private Interaction WardrobeScene(Station station, Action then)
        {
            bool owned = session.Loop.HasSwimsuit;
            return new Interaction().WalkTo(Here(station, WardrobeFront.x, WardrobeFront.y)).Face(Here(station, -1f, WardrobeFront.y))
                .Reveal(WardrobeSwimsuit, owned)
                .Pose(PersonPose.Reach, 0.5f).Sound(SoundEffectKind.Knock).Swing(WardrobeDoors, WardrobeOpen, 0.45f)
                .Wait(0.8f).Apply(then)
                .Wait(0.3f).Swing(WardrobeDoors, 1f, 0.35f).Sound(SoundEffectKind.Knock).Wait(0.4f);
        }

        /// <summary>Where the player stands to open the wardrobe, relative to its station: beside the doors, mid height.</summary>
        private static readonly Vector2 WardrobeFront = new Vector2(1.0f, 1.3f);

        private const string WardrobeDoors = "Wardrobe_Doors";
        private const string WardrobeSwimsuit = "Wardrobe_Swimsuit";

        /// <summary>The width of the open doors seen from above: almost edge-on.</summary>
        private const float WardrobeOpen = 0.18f;

        /// <summary>Cooking: the pan sizzles and the steam rises, the plate is served and eaten. Cooking at home saves against eating out.</summary>
        private Interaction StoveScene(Station station, Action then)
        {
            if (session.Loop.CookedThisMonth)
            {
                return null;
            }

            // The cutting board sits left of the stove on the same worktop, the fridge to its right.
            Vector2 pan = Here(station, -0.25f, 0.15f);
            Vector2 board = Here(station, -1.5f, -1f);
            Vector2 fridge = Here(station, 1.6f, -1f);
            return new Interaction()
                .WalkTo(fridge).Face(Here(station, 2f, 0f)).Pose(PersonPose.Reach, 0.6f).Sound(SoundEffectKind.Knock)
                .Flicker(Here(station, 2f, -0.3f), new Vector2(0.9f, 1.2f), FridgeLight, 0.8f).Wait(0.6f)
                .WalkTo(board).Face(Here(station, -1.5f, 0f)).Pose(PersonPose.Cook, 1.6f).Sound(SoundEffectKind.Knock).Wait(0.4f).Sound(SoundEffectKind.Knock)
                .WalkTo(Here(station, 0f, -1f)).Face(pan).Sound(SoundEffectKind.Sizzle).Pose(PersonPose.Cook, 2.6f).Steam(pan, 2.4f)
                .Pose(PersonPose.Handover, 0.6f).Fly("plate", pan, Here(station, 0f, -0.7f), 0.6f).Wait(0.4f)
                .Pose(PersonPose.Eat, 2f).Sound(SoundEffectKind.Munch).Wait(1f).Sound(SoundEffectKind.Munch).Wait(0.6f)
                .Say(string.Empty, Loc.F("Comer afuera me salía unos {0}. Esa plata hoy se queda en mi bolsa.", Money.Format(MarketAssumptions.HomeCookingSaving)))
                .Apply(() => session.Cook());
        }

        // ---------------------------------------------------------------- the store, the beach, the agencies and the people

        /// <summary>The seller comes to the counter, waves and slides a paper across before the talk begins.</summary>
        private Interaction StoreScene(Station station, Action then)
        {
            Vector2 me = Here(station);
            Vector2 seller = PersonAt("Vendedor", Here(station, 0f, 2.4f));
            return new Interaction().WalkTo(me).Face(seller)
                .NpcPose("Vendedor", PersonPose.Wave, 1.6f).Say("Kevin", PlayerProfile.HasName ? Loc.F("¡Hola, {0}! Venga, le enseño lo que tengo.", PlayerProfile.Name) : "¡Hola, mae! Venga, le enseño lo que tengo.")
                .Wait(1.2f).Sound(SoundEffectKind.Swish).Fly("paper", seller, me + new Vector2(0f, 0.3f), 0.8f)
                .Pose(PersonPose.Read, 1.4f).Sound(SoundEffectKind.Scribble)
                .Apply(then);
        }

        /// <summary>The swim: the player wades in, swims with the arms going and the water splashes. Without the swimsuit the thought plays instead.</summary>
        private Interaction BeachScene(Station station, Action then)
        {
            if (!session.Loop.HasSwimsuit || session.Loop.SwamThisMonth)
            {
                return null;
            }

            // In from the sand and back to it: the water's edge would warn about the swimsuit after the swim.
            Vector2 shore = Here(station, 0f, -0.9f);
            Vector2 sand = Here(station, 0f, 0.2f);
            var scene = new Interaction().WalkInto(shore, sand).Face(Here(station, 1f, -1f)).Sound(SoundEffectKind.Splash).Pose(PersonPose.Swim, 3.2f);
            for (int i = 0; i < 5; i++)
            {
                scene.Fly("drop", shore + new Vector2(-0.4f + i * 0.2f, 0.5f), shore + new Vector2(-0.4f + i * 0.2f, -0.1f), 0.5f).Wait(0.55f);
            }

            return scene.Sound(SoundEffectKind.Splash).Pose(PersonPose.None, 0.3f).Apply(then);
        }

        /// <summary>The desk of an agency: the other person greets, the papers come and the player signs before the options open.</summary>
        private Interaction SignScene(Station station, Action then, string who, string speaker, string line, bool keys)
        {
            Vector2 me = Here(station);
            Vector2 other = PersonAt(who, Here(station, 0f, 2f));
            var scene = new Interaction().WalkTo(me).Face(other).NpcPose(who, PersonPose.Wave, 1.5f).Say(speaker, line)
                .Wait(1.2f).Fly("paper", other, me + new Vector2(0f, 0.3f), 0.8f).Pose(PersonPose.Sign, 1.6f).Sound(SoundEffectKind.Scribble);
            if (keys)
            {
                scene.Wait(0.4f).Sound(SoundEffectKind.Coin).Fly("key", other, me + new Vector2(0f, 0.4f), 0.8f);
            }

            return scene.Wait(0.5f).Apply(then);
        }

        /// <summary>A task place (the garage, the grocery, the lender, the pyramid, the soda, the coop): the person greets and does what they do.</summary>
        private Interaction TalkScene(Station station, Action then)
        {
            string who;
            string speaker;
            string line;
            switch (station.Type)
            {
                case StationType.Garage:
                    who = "DonRafa";
                    speaker = "Don Rafa";
                    line = FinanceEngine.OwnsVehicle(session.State) ? "¡Pase, mae! Déjeme ver qué tiene su carro." : "¡Pase, mae! Vea qué chunches tengo por aquí.";
                    break;
                case StationType.Grocery:
                    who = "DonaMarta";
                    speaker = "Doña Marta";
                    line = "Buenas, mi amor. ¿Qué se le ofrece hoy?";
                    break;
                case StationType.Lender:
                    who = "Prestamista";
                    speaker = "El prestamista";
                    line = PlayerProfile.Pick("Tranquilo, amigo. Plata rápida, sin papeleo ni preguntas.", "Tranquila, amiga. Plata rápida, sin papeleo ni preguntas.");
                    break;
                case StationType.Pyramid:
                    who = "Promotor";
                    speaker = "Brayan";
                    line = "¡Mae, tengo el negocio de su vida! Triplica su plata en un mes.";
                    break;
                case StationType.Coop:
                    who = "AsesoraCoop";
                    speaker = "La asesora";
                    line = PlayerProfile.Pick("Bienvenido a la cooperativa. Veamos cómo cuidar sus ahorros.", "Bienvenida a la cooperativa. Veamos cómo cuidar sus ahorros.");
                    break;
                default:
                    who = "DonaLucia";
                    speaker = "Doña Lucía";
                    line = "¡Siéntese, mae! Hoy hay casado y fresco de mora.";
                    break;
            }

            Vector2 me = Here(station);
            Vector2 other = PersonAt(who, Here(station, 1.5f));
            var scene = new Interaction().WalkTo(me).Face(other).NpcPose(who, PersonPose.Wave, 1.5f).Say(speaker, line).Wait(1.1f);
            switch (station.Type)
            {
                case StationType.Garage:
                    scene.Sound(SoundEffectKind.Knock).Wobble("DonRafa", 0.5f).Wait(0.6f).Sound(SoundEffectKind.Knock).Wait(0.5f);
                    break;
                case StationType.Grocery:
                    scene.Pose(PersonPose.Handover, 0.7f).Fly("coin", me + new Vector2(0f, 0.4f), other, 0.7f).Sound(SoundEffectKind.Coin).Wait(0.6f);
                    break;
                case StationType.Lender:
                    scene.Pose(PersonPose.Handover, 0.7f).Sound(SoundEffectKind.Bills).Fly("bills", other, me + new Vector2(0f, 0.4f), 0.8f).Wait(0.9f);
                    break;
                case StationType.Pyramid:
                    scene.Fly("paper", other, me + new Vector2(0f, 0.3f), 0.8f).Pose(PersonPose.Read, 1.4f).Sound(SoundEffectKind.Swish).Wait(0.3f);
                    break;
                case StationType.Coop:
                    scene.Fly("paper", other, me + new Vector2(0f, 0.3f), 0.8f).Pose(PersonPose.Sign, 1.5f).Sound(SoundEffectKind.Scribble).Wait(0.3f);
                    break;
                default:
                    scene.Fly("plate", other, me + new Vector2(0f, 0.2f), 0.8f).Pose(PersonPose.Eat, 1.8f).Sound(SoundEffectKind.Munch).Wait(0.8f).Sound(SoundEffectKind.Munch);
                    break;
            }

            return scene.Apply(then);
        }
    }
}

using System;
using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>
    /// One run of the game: it joins the rules (<see cref="GameLoop"/>) with the backend
    /// (<see cref="ApiClient"/>) and tells the UI what happened through events. It lives across
    /// scene changes, so walking into a building does not lose the month.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameLoop Loop { get; private set; } = new GameLoop();

        /// <summary>True while a request to the backend is in flight.</summary>
        public bool Busy { get; private set; }

        public GameEvent CurrentEvent { get; private set; }
        public ResultSource CurrentEventSource { get; private set; }
        public StationType CurrentStation { get; private set; }
        public SessionSummary Summary { get; private set; }

        /// <summary>True while the summary of the year is being written, so the screen can say so.</summary>
        public bool Finishing { get; private set; }

        private const int MaxNegotiationText = 300;

        /// <summary>Shown in the text box when a spoken turn could not be understood.</summary>
        public const string VoiceFailedNotice = "No te entendí, escribilo.";

        /// <summary>The conversation at the store, or null when there is none.</summary>
        public Negotiation CurrentNegotiation { get; private set; }

        /// <summary>Set when the last spoken turn failed; cleared by the next turn.</summary>
        public string VoiceNotice { get; private set; }

        /// <summary>What the last deal cost and how well the player asked; shown with the result.</summary>
        public DealReport LastDeal { get; private set; }

        /// <summary>The car being negotiated at the agency, or null at the store. The agency is not known to the backend.</summary>
        public CarModel CurrentCar { get; private set; }

        /// <summary>Why the agency refused to close the deal, shown in the negotiation window; null when there is nothing to say.</summary>
        public string DealNotice { get; private set; }

        public Holdings Holdings => Loop.Holdings;

        public event Action<Negotiation> NegotiationStarted;
        public event Action<Negotiation> NegotiationUpdated;
        public event Action Changed;
        public event Action NewGameStarted;
        public event Action<GameEvent, ResultSource> EventReady;
        public event Action<ChoiceOutcome> ChoiceApplied;
        public event Action<MonthlyTickReport> MonthClosed;
        public event Action<SessionSummary> Finished;

        public PlayerState State => Loop.State;

        public static GameSession Create()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject("GameSession");
            DontDestroyOnLoad(go);
            return go.AddComponent<GameSession>();
        }

        private void Awake()
        {
            Instance = this;
            StartNewGame();
        }

        private void Update()
        {
            DayTint.Progress = DayCycle.ProgressOf(Loop.MonthsPlayed, Loop.TasksDone, GameRules.MonthsToPlay, GameRules.TasksPerMonth);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartNewGame()
        {
            Loop.NewGame();
            CasinoNet = 0L;
            SinpeSent = 0L;
            Busy = false;
            CurrentEvent = null;
            CurrentNegotiation = null;
            VoiceNotice = null;
            LastDeal = null;
            CurrentCar = null;
            DealNotice = null;
            Summary = null;

            // The session id arrives in a moment; until then every event would come from the local catalog.
            ApiClient.Instance.CreateSession(null, result =>
            {
                if (!result.Ok)
                {
                    Debug.Log("Playing offline: " + result.Error);
                }
            });

            NewGameStarted?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Asks the game master for an event for this place. The result arrives through <see cref="EventReady"/>.</summary>
        public void BeginTask(StationType station)
        {
            if (Busy || !Loop.CanDoTask(station))
            {
                return;
            }

            Busy = true;
            CurrentStation = station;
            LastDeal = null;
            Changed?.Invoke();
            ApiClient.Instance.RequestEvent(State.Clone(), result =>
            {
                Busy = false;
                CurrentEvent = result.Value;
                CurrentEventSource = result.Source;
                EventReady?.Invoke(result.Value, result.Source);
                Changed?.Invoke();
            });
        }

        public void Choose(string choiceId)
        {
            if (CurrentEvent == null)
            {
                return;
            }

            var gameEvent = CurrentEvent;
            int month = State.Month;
            ChoiceOutcome outcome;
            try
            {
                outcome = Loop.Choose(gameEvent, choiceId, CurrentStation);
            }
            catch (InvalidGameEventEffectException error)
            {
                // The engine refuses anything outside the contract and leaves the state untouched.
                Debug.LogWarning("Choice rejected: " + error.Message);
                CurrentEvent = null;
                Changed?.Invoke();
                return;
            }

            CurrentEvent = null;
            ApiClient.Instance.RecordDecision(month, gameEvent, choiceId, State.Clone(), result =>
            {
                if (!result.Ok)
                {
                    Debug.Log("Decision not recorded: " + result.Error);
                }
            });

            ChoiceApplied?.Invoke(outcome);
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- negotiation

        /// <summary>Opens the seller's counter at the store. The panel listens to <see cref="NegotiationStarted"/>.</summary>
        public void BeginNegotiation()
        {
            if (Busy || CurrentNegotiation != null || !Loop.CanDoTask(StationType.Store))
            {
                return;
            }

            CurrentStation = StationType.Store;
            LastDeal = null;
            VoiceNotice = null;
            CurrentNegotiation = new Negotiation(SellerOffer.ForMonth(Loop.MonthsPlayed));
            NegotiationStarted?.Invoke(CurrentNegotiation);
        }

        /// <summary>
        /// One turn typed by the player. The backend seller answers; when it cannot, the local seller
        /// does, so the conversation never stops.
        /// </summary>
        public void SendNegotiation(string text)
        {
            var negotiation = CurrentNegotiation;
            text = text == null ? string.Empty : text.Trim();
            if (negotiation == null || negotiation.IsClosed || Busy || text.Length == 0)
            {
                return;
            }

            if (text.Length > MaxNegotiationText)
            {
                text = text.Substring(0, MaxNegotiationText);
            }

            DealNotice = null;
            if (negotiation.IsLocalOnly)
            {
                // The backend does not know this offer: the local seller answers at once and nothing is sent.
                negotiation.Record(text, LocalSeller.Reply(negotiation.Offer, text), true);
                NegotiationUpdated?.Invoke(negotiation);
                Changed?.Invoke();
                return;
            }

            Busy = true;
            VoiceNotice = null;
            Changed?.Invoke();
            ApiClient.Instance.SendNegotiationText(negotiation.Offer.OfferId, text, result =>
            {
                Busy = false;
                if (CurrentNegotiation == negotiation)
                {
                    bool local = !result.Ok;
                    var reply = local ? LocalSeller.Reply(negotiation.Offer, text) : result.Value;
                    negotiation.Record(text, reply, local);
                    NegotiationUpdated?.Invoke(negotiation);
                }

                Changed?.Invoke();
            });
        }

        /// <summary>
        /// One turn spoken by the player. Only the backend can transcribe, so when it is missing or the
        /// voice fails nothing is recorded: <see cref="VoiceNotice"/> asks the player to type instead.
        /// </summary>
        public void SendNegotiationVoice(byte[] wav)
        {
            var negotiation = CurrentNegotiation;
            if (negotiation == null || negotiation.IsClosed || Busy || wav == null || wav.Length == 0)
            {
                return;
            }

            if (negotiation.IsLocalOnly || !ApiClient.Instance.Service.HasSession)
            {
                VoiceNotice = VoiceFailedNotice;
                NegotiationUpdated?.Invoke(negotiation);
                Changed?.Invoke();
                return;
            }

            Busy = true;
            VoiceNotice = null;
            Changed?.Invoke();
            ApiClient.Instance.SendNegotiationVoice(negotiation.Offer.OfferId, wav, result =>
            {
                Busy = false;
                if (CurrentNegotiation == negotiation)
                {
                    if (result.Ok)
                    {
                        negotiation.Record(result.Value.Transcript, result.Value, false);
                    }
                    else
                    {
                        Debug.Log("Voice turn failed: " + result.Error);
                        VoiceNotice = VoiceFailedNotice;
                    }

                    NegotiationUpdated?.Invoke(negotiation);
                }

                Changed?.Invoke();
            });
        }

        /// <summary>The player buys, from the button or because the seller closed the deal on the typed words.</summary>
        public void AcceptDeal()
        {
            ConcludeNegotiation(true);
        }

        /// <summary>The player leaves without buying.</summary>
        public void WalkAway()
        {
            ConcludeNegotiation(false);
        }

        /// <summary>Ends the conversation following how it closed (accepted means bought).</summary>
        public void FinishNegotiation()
        {
            if (CurrentNegotiation != null)
            {
                ConcludeNegotiation(CurrentNegotiation.Status == DealStatus.Accepted);
            }
        }

        private void ConcludeNegotiation(bool buy)
        {
            var negotiation = CurrentNegotiation;
            if (negotiation == null || Busy)
            {
                return;
            }

            if (negotiation.IsLocalOnly)
            {
                ConcludeCarDeal(negotiation, buy);
                return;
            }

            negotiation.Close(buy ? DealStatus.Accepted : DealStatus.WalkedAway);
            CurrentNegotiation = null;
            CurrentEvent = negotiation.ToEvent();
            LastDeal = negotiation.Report(buy);
            Choose(buy ? Negotiation.BuyChoice : Negotiation.SkipChoice);
        }

        // ---------------------------------------------------------------- car agency

        /// <summary>Opens the conversation with the agency seller about this car. The panel listens to <see cref="NegotiationStarted"/>.</summary>
        public void BeginCarNegotiation(CarModel car)
        {
            if (car == null || Busy || CurrentNegotiation != null || Loop.IsFinished || Holdings.Car != null)
            {
                return;
            }

            CurrentCar = car;
            LastDeal = null;
            VoiceNotice = null;
            DealNotice = null;
            CurrentNegotiation = new Negotiation(car.ToSellerOffer()) { Venue = "Agencia de carros" };
            NegotiationStarted?.Invoke(CurrentNegotiation);
        }

        /// <summary>
        /// Closes the car conversation. Buying goes through the finance engine and needs the cash for the down
        /// payment and the commission: when it is missing and the window is still open, the agency says so and
        /// waits; when the deal was already closed by the typed words, it ends without a purchase.
        /// </summary>
        private void ConcludeCarDeal(Negotiation negotiation, bool buy)
        {
            var car = CurrentCar;
            string note = string.Empty;
            if (buy && car != null)
            {
                var refusal = Loop.CheckBuyCar(car);
                if (refusal != PurchaseFailure.None)
                {
                    note = PurchaseMessages.For(refusal);
                    if (!negotiation.IsClosed)
                    {
                        DealNotice = note;
                        NegotiationUpdated?.Invoke(negotiation);
                        Changed?.Invoke();
                        return;
                    }

                    buy = false;
                }
            }

            negotiation.Close(buy ? DealStatus.Accepted : DealStatus.WalkedAway);
            CurrentNegotiation = null;
            CurrentCar = null;
            DealNotice = null;
            LastDeal = negotiation.Report(buy);
            LastDeal.Note = note;

            ChoiceOutcome outcome = buy ? Loop.BuyCar(car).Outcome : new ChoiceOutcome { ChoiceLabel = "No comprar" };
            ChoiceApplied?.Invoke(outcome);
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- things that happen to the player

        /// <summary>The police take the player away: a fine, days of the month lost and a lower mood.</summary>
        public bool Arrest()
        {
            if (Loop.IsFinished || Busy)
            {
                return false;
            }

            ChoiceApplied?.Invoke(Loop.ArrestPlayer());
            Changed?.Invoke();
            return true;
        }

        /// <summary>The player pays the bail and leaves the cell.</summary>
        public bool PayBail(long cost)
        {
            if (Loop.IsFinished || Busy)
            {
                return false;
            }

            ChoiceApplied?.Invoke(Loop.PayBail(cost));
            Changed?.Invoke();
            return true;
        }

        public bool BuySwimsuit()
        {
            return Apply(Loop.BuySwimsuit());
        }

        public bool Swim()
        {
            return Apply(Loop.Swim());
        }

        private bool Apply(ChoiceOutcome outcome)
        {
            if (outcome == null || Loop.IsFinished || Busy)
            {
                return false;
            }

            ChoiceApplied?.Invoke(outcome);
            Changed?.Invoke();
            return true;
        }

        /// <summary>An officer fines the player for crossing on red. Nothing else changes.</summary>
        public bool FineRedLight()
        {
            if (Loop.IsFinished || Busy)
            {
                return false;
            }

            ChoiceApplied?.Invoke(Loop.FineRedLight());
            Changed?.Invoke();
            return true;
        }

        /// <summary>A car ran the player over. It happens once a month at most; false when it did not count.</summary>
        public bool Hospitalize()
        {
            if (Loop.IsFinished || Busy || Loop.HurtThisMonth)
            {
                return false;
            }

            ChoiceApplied?.Invoke(Loop.HospitalizePlayer());
            Changed?.Invoke();
            return true;
        }

        public bool BuyHealthInsurance()
        {
            if (!Loop.BuyHealthInsurance())
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public bool InArrears => !Loop.IsFinished && Loop.InArrears;

        public long CollectionDemand => Loop.CollectionDemand;

        public void PayCollectors()
        {
            ChoiceApplied?.Invoke(Loop.PayCollectors());
            Changed?.Invoke();
        }

        public void NegotiateCollectors()
        {
            ChoiceApplied?.Invoke(Loop.NegotiateCollectors());
            Changed?.Invoke();
        }

        public void IgnoreCollectors()
        {
            ChoiceApplied?.Invoke(Loop.IgnoreCollectors());
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- homes and the farm

        /// <summary>Signs a lease. The finance engine moves the money; false when it cannot be done.</summary>
        public bool RentHome(HomeListing home)
        {
            return Apply(() => Loop.RentHome(home));
        }

        public bool BuyHome(HomeListing home)
        {
            return Apply(() => Loop.BuyHome(home));
        }

        public bool InvestInFarm(FarmOption option, long amount)
        {
            return Apply(() => Loop.InvestInFarm(option, amount));
        }

        private bool Apply(Func<AssetPurchase> purchase)
        {
            if (Busy || Loop.IsFinished || CurrentNegotiation != null)
            {
                return false;
            }

            var made = purchase();
            if (!made.Result.Bought)
            {
                Changed?.Invoke();
                return false;
            }

            LastDeal = null;
            ChoiceApplied?.Invoke(made.Outcome);
            Changed?.Invoke();
            return true;
        }

        /// <summary>What the casino has taken (negative) or paid (positive) in this run, to show how it adds up.</summary>
        public long CasinoNet { get; private set; }

        /// <summary>Everything sent by SINPE from the phone in this run.</summary>
        public long SinpeSent { get; private set; }

        /// <summary>Sends money by SINPE from the phone. Only the finance engine moves the cash; false when there is not enough.</summary>
        public bool SendSinpe(long amount)
        {
            if (Busy || Loop.IsFinished || amount <= 0L || !FinanceEngine.TrySpendCash(State, amount))
            {
                return false;
            }

            SinpeSent += amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>One bet at the phone's casino. The stake leaves the cash first; a win pays it back with the prize.</summary>
        public bool TryCasino(long stake, out CasinoResult result)
        {
            result = default;
            if (Busy || Loop.IsFinished || !CasinoGame.IsValidStake(stake) || !FinanceEngine.TrySpendCash(State, stake))
            {
                return false;
            }

            result = CasinoGame.Play(stake, UnityEngine.Random.Range(0, 100));
            if (result.Net > 0L)
            {
                FinanceEngine.CreditCash(State, stake + result.Net);
            }

            CasinoNet += result.Net;
            Changed?.Invoke();
            return true;
        }

        public void CloseMonth()
        {
            if (Busy || Loop.IsFinished)
            {
                return;
            }

            var report = Loop.CloseMonth();
            MonthClosed?.Invoke(report);
            Changed?.Invoke();
        }

        /// <summary>Closes the run: asks the backend for the summary and falls back to a local one.</summary>
        public void FinishRun()
        {
            if (Busy)
            {
                return;
            }

            Busy = true;
            Finishing = true;
            Changed?.Invoke();
            ApiClient.Instance.RequestSummary(State.Clone(), result =>
            {
                Busy = false;
                Finishing = false;
                Summary = result.Ok ? result.Value : LocalSummary();
                Finished?.Invoke(Summary);
                Changed?.Invoke();
            });
        }

        private SessionSummary LocalSummary()
        {
            return new SessionSummary
            {
                MonthsPlayed = Loop.MonthsPlayed,
                Decisions = Loop.Decisions,
                TotalDebt = GameRules.DebtBalance(State),
                NetWorth = GameRules.NetWorth(State),
                Narrative = "Terminaste tus meses. Cada decisión de plata tuvo su consecuencia, y estos son tus números.",
                Source = "local"
            };
        }
    }
}

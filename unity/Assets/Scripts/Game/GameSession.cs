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

        private const int MaxNegotiationText = 300;

        /// <summary>The conversation at the store, or null when there is none.</summary>
        public Negotiation CurrentNegotiation { get; private set; }

        /// <summary>What the last deal cost and how well the player asked; shown with the result.</summary>
        public DealReport LastDeal { get; private set; }

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
            Busy = false;
            CurrentEvent = null;
            CurrentNegotiation = null;
            LastDeal = null;
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

            Busy = true;
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

            negotiation.Close(buy ? DealStatus.Accepted : DealStatus.WalkedAway);
            CurrentNegotiation = null;
            CurrentEvent = negotiation.ToEvent();
            LastDeal = negotiation.Report(buy);
            Choose(buy ? Negotiation.BuyChoice : Negotiation.SkipChoice);
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
            Changed?.Invoke();
            ApiClient.Instance.RequestSummary(State.Clone(), result =>
            {
                Busy = false;
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

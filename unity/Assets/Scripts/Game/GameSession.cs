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

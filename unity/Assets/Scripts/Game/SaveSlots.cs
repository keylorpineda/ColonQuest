using System;
using System.Collections.Generic;
using System.Globalization;
using ColonQuest.Api;
using ColonQuest.Finance;
using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>A home, a car or a farm stake as it is written in a saved game.</summary>
    [Serializable]
    public sealed class HoldingsSnapshot
    {
        public bool hasHome;
        public string homeName = string.Empty;
        public bool homeOwned;
        public long homeValue;
        public long homeMonthly;
        public int homeMonths;

        public bool hasCar;
        public string carName = string.Empty;
        public long carResaleValue;
        public long carRunningCosts;

        public long healthPremium;

        public List<int> farmKinds = new List<int>();
        public List<long> farmInvested = new List<long>();
        public List<int> farmMonthsWhenBought = new List<int>();

        public List<int> furniture = new List<int>();
    }

    /// <summary>Everything a run needs to go on later: the finances, what the player owns, the month and the hour.</summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        public PlayerStateDto state = new PlayerStateDto();
        public HoldingsSnapshot holdings = new HoldingsSnapshot();
        public int monthsPlayed;
        public int tasksDone;
        public int decisions;
        public int arrests;
        public bool hurtThisMonth;
        public bool hasSwimsuit;
        public bool swamThisMonth;
        public bool sleptThisMonth;
        public bool cookedThisMonth;
        public List<int> doneStations = new List<int>();
        public int day = 1;
        public float minutes = GameRules.DayStartHour * 60f;
        public int daysWorked;
        public int latesThisMonth;
        public int missedThisMonth;
        public bool workedToday;
        public List<int> billKinds = new List<int>();
        public List<long> billAmounts = new List<long>();
        public List<int> billMonthsLate = new List<int>();
        public bool billsArrived;
        public int punctualDays;
        public int jobPosition;
        public List<int> courseAfternoons = new List<int>();
        public bool attendedToday;
        public List<int> goalKinds = new List<int>();
        public List<long> goalTargets = new List<long>();
        public List<long> goalStarts = new List<long>();
        public List<bool> goalsDone = new List<bool>();
        public long quincenaPaidThisMonth;
    }

    /// <summary>One saved game: its name, when it was last played and the run.</summary>
    [Serializable]
    public sealed class SaveSlot
    {
        public string name = string.Empty;
        public string characterName = string.Empty;
        public bool female;
        public string savedAt = string.Empty;
        public RunSnapshot run = new RunSnapshot();

        public int Month => run.monthsPlayed + 1;
        public int Week => run.day;
        public long Cash => run.state.cash;
        public long Savings => run.state.savings + run.state.emergencyFund;
        public int Wellbeing => run.state.wellbeing;

        /// <summary>True once the last month was closed: the run can be looked at, not played on.</summary>
        public bool Finished => run.monthsPlayed >= GameRules.MonthsToPlay;

        public static SaveSlot From(string name, GameLoop loop, DateTime now)
        {
            return new SaveSlot { name = name, savedAt = now.ToString("o", CultureInfo.InvariantCulture), run = loop.Snapshot() };
        }

        /// <summary>The date of the last game in the player's local time, short: "30/10 14:05".</summary>
        public string SavedAtLabel
        {
            get
            {
                return DateTime.TryParse(savedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)
                    ? when.ToLocalTime().ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
        }
    }

    /// <summary>Where the saved games are written. The game uses <see cref="PrefsSaveStore"/>; the tests use one in memory.</summary>
    public interface ISaveStore
    {
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    /// <summary>PlayerPrefs: the registry on Windows and IndexedDB in a WebGL build.</summary>
    public sealed class PrefsSaveStore : ISaveStore
    {
        public string Read(string key)
        {
            return PlayerPrefs.GetString(key, string.Empty);
        }

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>A store that lives only in memory: for the tests, so they never touch the player's real saved games.</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public string Read(string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public void Write(string key, string value)
        {
            values[key] = value;
        }

        public void Delete(string key)
        {
            values.Remove(key);
        }
    }

    /// <summary>The three saved games of story mode, as JSON. A slot that cannot be read counts as empty.</summary>
    public sealed class SaveSlots
    {
        public const int Count = 3;
        private const string KeyPrefix = "colonquest.save.";

        private readonly ISaveStore store;

        public SaveSlots(ISaveStore store)
        {
            this.store = store;
        }

        public SaveSlot Load(int index)
        {
            if (!IsValid(index))
            {
                return null;
            }

            string json = store.Read(KeyPrefix + index);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var slot = JsonUtility.FromJson<SaveSlot>(json);
                return slot != null && slot.run != null && slot.run.state != null ? slot : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public void Save(int index, SaveSlot slot)
        {
            if (IsValid(index) && slot != null)
            {
                store.Write(KeyPrefix + index, JsonUtility.ToJson(slot));
            }
        }

        public void Delete(int index)
        {
            if (IsValid(index))
            {
                store.Delete(KeyPrefix + index);
            }
        }

        private static bool IsValid(int index)
        {
            return index >= 0 && index < Count;
        }
    }

    /// <summary>Turns what the player owns into its saved shape and back.</summary>
    public static class HoldingsSaving
    {
        public static HoldingsSnapshot ToSnapshot(Holdings holdings)
        {
            var snapshot = new HoldingsSnapshot { healthPremium = holdings.HealthPremium };
            if (holdings.Home != null)
            {
                snapshot.hasHome = true;
                snapshot.homeName = holdings.Home.Name;
                snapshot.homeOwned = holdings.Home.Owned;
                snapshot.homeValue = holdings.Home.Value;
                snapshot.homeMonthly = holdings.Home.Monthly;
                snapshot.homeMonths = holdings.Home.Months;
            }

            if (holdings.Car != null)
            {
                snapshot.hasCar = true;
                snapshot.carName = holdings.Car.Name;
                snapshot.carResaleValue = holdings.Car.ResaleValue;
                snapshot.carRunningCosts = holdings.Car.MonthlyRunningCosts;
            }

            foreach (var stake in holdings.Farm)
            {
                snapshot.farmKinds.Add((int)stake.Kind);
                snapshot.farmInvested.Add(stake.Invested);
                snapshot.farmMonthsWhenBought.Add(stake.MonthsPlayedWhenBought);
            }

            foreach (var kind in holdings.Furniture)
            {
                snapshot.furniture.Add((int)kind);
            }

            return snapshot;
        }

        public static Holdings FromSnapshot(HoldingsSnapshot snapshot)
        {
            var holdings = new Holdings();
            if (snapshot == null)
            {
                return holdings;
            }

            if (snapshot.hasHome)
            {
                holdings.SetHome(new HomeHolding(snapshot.homeName, snapshot.homeOwned, snapshot.homeValue, snapshot.homeMonthly, snapshot.homeMonths));
            }

            if (snapshot.hasCar)
            {
                holdings.SetCar(new CarHolding(snapshot.carName, snapshot.carResaleValue, snapshot.carRunningCosts));
            }

            if (snapshot.healthPremium > 0L)
            {
                holdings.SetHealthInsurance(snapshot.healthPremium);
            }

            int stakes = Math.Min(snapshot.farmKinds.Count, Math.Min(snapshot.farmInvested.Count, snapshot.farmMonthsWhenBought.Count));
            for (int i = 0; i < stakes; i++)
            {
                holdings.AddFarm((FarmKind)snapshot.farmKinds[i], snapshot.farmInvested[i], snapshot.farmMonthsWhenBought[i]);
            }

            foreach (int kind in snapshot.furniture ?? new List<int>())
            {
                holdings.AddFurniture((FurnitureKind)kind);
            }

            return holdings;
        }
    }
}

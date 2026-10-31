using System.Globalization;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>Where the settings are kept. A test can swap it for a plain dictionary.</summary>
    public interface ISettingsStore
    {
        string Read(string key);

        void Write(string key, string value);
    }

    /// <summary>Keeps the settings in PlayerPrefs. A browser with blocked storage must not break the game, so every call is guarded.</summary>
    public sealed class PlayerPrefsStore : ISettingsStore
    {
        public string Read(string key)
        {
            try
            {
                return PlayerPrefs.GetString(key, string.Empty);
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        public void Write(string key, string value)
        {
            try
            {
                PlayerPrefs.SetString(key, value);
                PlayerPrefs.Save();
            }
            catch (System.Exception)
            {
                // Storage is blocked or full: the settings simply last for this session.
            }
        }
    }

    public enum TimeSpeed
    {
        Normal,
        Fast
    }

    /// <summary>
    /// What the player can change in the settings: sound on or off, the volume and clock speed. It is plain data with
    /// a text form (<c>"1;0.8"</c>) so it can be stored anywhere and tested without Unity.
    /// </summary>
    public sealed class GameSettings
    {
        public const string StoreKey = "colonquest.settings";
        public const float VolumeStep = 0.1f;
        public const float DefaultVolume = 1f;

        private float volume = DefaultVolume;

        private static GameSettings current;

        /// <summary>The settings in use. They are read from storage the first time they are asked for.</summary>
        public static GameSettings Current
        {
            get
            {
                if (current == null)
                {
                    current = Load(new PlayerPrefsStore());
                }

                return current;
            }
        }

        public bool SoundOn { get; set; } = true;

        /// <summary>The language of the text. Spanish unless the player chose English.</summary>
        public GameLanguage Language { get; set; } = GameLanguage.Spanish;

        /// <summary>Clock progression speed: normal (30s/h) or fast (10s/h, for judging and demos).</summary>
        public TimeSpeed Speed { get; set; } = TimeSpeed.Normal;

        /// <summary>Real seconds elapsed for one in-game hour.</summary>
        public float SecondsPerGameHour => Speed == TimeSpeed.Fast ? 10f : 30f;

        /// <summary>From 0 to 1. Any value outside that range is brought back into it.</summary>
        public float Volume
        {
            get => volume;
            set => volume = ClampVolume(value);
        }

        public static float ClampVolume(float value)
        {
            if (float.IsNaN(value))
            {
                return DefaultVolume;
            }

            // Rounded to hundredths so repeated steps never drift (0.1 added ten times is not exactly 1).
            return Mathf.Round(Mathf.Clamp01(value) * 100f) / 100f;
        }

        /// <summary>Moves the volume by whole steps; negative steps lower it.</summary>
        public void StepVolume(int steps)
        {
            Volume = volume + steps * VolumeStep;
        }

        /// <summary>The volume as the percentage the player sees.</summary>
        public int VolumePercent => Mathf.RoundToInt(volume * 100f);

        public string Serialize()
        {
            string text = (SoundOn ? "1" : "0") + ";" + volume.ToString("0.00", CultureInfo.InvariantCulture);
            if (Language == GameLanguage.English)
            {
                text += ";en";
            }

            if (Speed == TimeSpeed.Fast)
            {
                text += ";fast";
            }

            return text;
        }

        /// <summary>Reads the text form. Anything that does not parse gives the default for that part.</summary>
        public static GameSettings Parse(string text)
        {
            var settings = new GameSettings();
            if (string.IsNullOrEmpty(text))
            {
                return settings;
            }

            string[] parts = text.Split(';');
            if (parts.Length > 0 && (parts[0] == "0" || parts[0] == "1"))
            {
                settings.SoundOn = parts[0] == "1";
            }

            if (parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                settings.Volume = parsed;
            }

            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "en")
                {
                    settings.Language = GameLanguage.English;
                }
                else if (parts[i] == "fast")
                {
                    settings.Speed = TimeSpeed.Fast;
                }
            }

            return settings;
        }

        public static GameSettings Load(ISettingsStore store)
        {
            return Parse(store.Read(StoreKey));
        }

        public void Save(ISettingsStore store)
        {
            store.Write(StoreKey, Serialize());
        }

        /// <summary>Writes these settings to PlayerPrefs.</summary>
        public void Save()
        {
            Save(new PlayerPrefsStore());
        }
    }
}

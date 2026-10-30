using System.Collections.Generic;
using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class GameSettingsTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

            public string Read(string key) => Values.TryGetValue(key, out string value) ? value : string.Empty;

            public void Write(string key, string value) => Values[key] = value;
        }

        [Test]
        public void New_SoundIsOnAtFullVolume()
        {
            var settings = new GameSettings();

            Assert.IsTrue(settings.SoundOn);
            Assert.AreEqual(1f, settings.Volume, 0.0001f);
            Assert.AreEqual(100, settings.VolumePercent);
        }

        [TestCase(-3f, 0f)]
        [TestCase(0.37f, 0.37f)]
        [TestCase(7f, 1f)]
        [TestCase(float.NaN, 1f)]
        public void Volume_IsKeptBetweenZeroAndOne(float given, float expected)
        {
            var settings = new GameSettings { Volume = given };

            Assert.AreEqual(expected, settings.Volume, 0.0001f);
        }

        [Test]
        public void StepVolume_MovesByTenPercentAndStopsAtTheEnds()
        {
            var settings = new GameSettings();

            settings.StepVolume(1);
            Assert.AreEqual(100, settings.VolumePercent);

            for (int i = 0; i < 4; i++)
            {
                settings.StepVolume(-1);
            }

            Assert.AreEqual(60, settings.VolumePercent);

            settings.StepVolume(-20);
            Assert.AreEqual(0, settings.VolumePercent);
        }

        [Test]
        public void StepVolume_TenStepsDoNotDrift()
        {
            var settings = new GameSettings { Volume = 0f };

            for (int i = 0; i < 10; i++)
            {
                settings.StepVolume(1);
            }

            Assert.AreEqual(1f, settings.Volume);
        }

        [Test]
        public void SerializeAndParse_KeepTheValues()
        {
            var settings = new GameSettings { SoundOn = false, Volume = 0.4f };

            var again = GameSettings.Parse(settings.Serialize());

            Assert.IsFalse(again.SoundOn);
            Assert.AreEqual(0.4f, again.Volume, 0.0001f);
        }

        [Test]
        public void Serialize_UsesADotWhateverTheCulture()
        {
            Assert.AreEqual("1;0.70", new GameSettings { Volume = 0.7f }.Serialize());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage")]
        [TestCase(";")]
        [TestCase("2;abc")]
        public void Parse_BadTextGivesTheDefaults(string text)
        {
            var settings = GameSettings.Parse(text);

            Assert.IsTrue(settings.SoundOn);
            Assert.AreEqual(1f, settings.Volume, 0.0001f);
        }

        [Test]
        public void Parse_ReadsEachPartOnItsOwn()
        {
            var onlySound = GameSettings.Parse("0");
            var badSound = GameSettings.Parse("x;0.5");

            Assert.IsFalse(onlySound.SoundOn);
            Assert.AreEqual(1f, onlySound.Volume, 0.0001f);
            Assert.IsTrue(badSound.SoundOn);
            Assert.AreEqual(0.5f, badSound.Volume, 0.0001f);
        }

        [Test]
        public void SaveAndLoad_GoThroughTheStore()
        {
            var store = new MemoryStore();
            new GameSettings { SoundOn = false, Volume = 0.2f }.Save(store);

            var loaded = GameSettings.Load(store);

            Assert.IsTrue(store.Values.ContainsKey(GameSettings.StoreKey));
            Assert.IsFalse(loaded.SoundOn);
            Assert.AreEqual(0.2f, loaded.Volume, 0.0001f);
        }

        [Test]
        public void Load_FromAnEmptyStoreGivesTheDefaults()
        {
            var loaded = GameSettings.Load(new MemoryStore());

            Assert.IsTrue(loaded.SoundOn);
            Assert.AreEqual(100, loaded.VolumePercent);
        }
    }
}

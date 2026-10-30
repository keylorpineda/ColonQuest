using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    [TestFixture]
    public sealed class SoundMixTests
    {
        [Test]
        public void Beach_IsLouderThanThePlaza()
        {
            var beach = SoundMix.Compute(SceneNames.Town, new Vector2(0f, -25f));
            var plaza = SoundMix.Compute(SceneNames.Town, new Vector2(0f, 9f));
            Assert.Greater(beach.Sea, 0.6f);
            Assert.Less(plaza.Sea, 0.1f);
            Assert.Less(beach.Traffic, plaza.Traffic);
        }

        [Test]
        public void River_GrowsTowardTheEast()
        {
            var west = SoundMix.Compute(SceneNames.Town, new Vector2(0f, 2f));
            var east = SoundMix.Compute(SceneNames.Town, new Vector2(27f, 2f));
            Assert.AreEqual(0f, west.River, 0.001f);
            Assert.Greater(east.River, 0.5f);
        }

        [Test]
        public void EachPlaceHasItsOwnTune()
        {
            Assert.AreEqual(MusicTrack.Town, SoundMix.Compute(SceneNames.Town, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.Store, SoundMix.Compute(SceneNames.Tienda, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.Bank, SoundMix.Compute(SceneNames.Banco, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.House, SoundMix.Compute(SceneNames.Casa, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.Menu, SoundMix.Compute(SceneNames.Menu, Vector2.zero).Track);
        }

        [Test]
        public void Bank_HasPeopleTalkingAndTheStoreHasLouderMusic()
        {
            var bank = SoundMix.Compute(SceneNames.Banco, Vector2.zero);
            var store = SoundMix.Compute(SceneNames.Tienda, Vector2.zero);
            Assert.Greater(bank.Crowd, 0.4f);
            Assert.Greater(store.Music, bank.Music);
            Assert.AreEqual(0f, bank.Sea);
            Assert.AreEqual(0f, bank.Traffic);
        }

        [Test]
        public void Levels_StayBetweenZeroAndOneEverywhere()
        {
            for (float x = -45f; x <= 40f; x += 5f)
            {
                for (float y = -30f; y <= 17f; y += 5f)
                {
                    var l = SoundMix.Compute(SceneNames.Town, new Vector2(x, y));
                    foreach (float v in new[] { l.Music, l.Crowd, l.Sea, l.River, l.Traffic, l.Birds })
                    {
                        Assert.GreaterOrEqual(v, 0f);
                        Assert.LessOrEqual(v, 1f);
                    }
                }
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.World.Tests
{
    public sealed class HouseDoorTests
    {
        [Test]
        public void ResolveSpawn_AMatThatRemembersGoesBackToTheDoorOfTheHouse()
        {
            Assert.AreEqual("Town_From_Casa_Vecina_A", Door.ResolveSpawn(true, "Town_From_Casa_Vecina_A", SpawnIds.TownFromVecino));
        }

        [Test]
        public void ResolveSpawn_WithoutAMemoryTheMatUsesItsOwnSpawn()
        {
            Assert.AreEqual(SpawnIds.TownFromVecino, Door.ResolveSpawn(true, string.Empty, SpawnIds.TownFromVecino));
            Assert.AreEqual(SpawnIds.TownFromVecino, Door.ResolveSpawn(true, null, SpawnIds.TownFromVecino));
        }

        [Test]
        public void ResolveSpawn_OrdinaryDoorsIgnoreTheMemory()
        {
            Assert.AreEqual(SpawnIds.TownFromBanco, Door.ResolveSpawn(false, "Town_From_Casa_Vecina_A", SpawnIds.TownFromBanco));
        }

        [Test]
        public void NewPlaces_HaveTheirOwnMusicAndGround()
        {
            Assert.AreEqual(MusicTrack.Store, SoundMix.Compute(SceneNames.Inmobiliaria, Vector2.zero).Track);
            Assert.AreEqual(MusicTrack.House, SoundMix.Compute(SceneNames.Finca, Vector2.zero).Track);
        }
    }
}

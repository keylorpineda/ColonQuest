using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    [TestFixture]
    public sealed class SilenceGateTests
    {
        private const float Step = 0.05f;

        /// <summary>Feeds a level for some time and says whether the gate ended the clip during it.</summary>
        private static bool Feed(SilenceGate gate, float level, float seconds)
        {
            bool ended = false;
            for (float t = 0f; t < seconds; t += Step)
            {
                ended |= gate.Feed(level, Step);
            }

            return ended;
        }

        [Test]
        public void EndsAfterThePlayerStopsTalking()
        {
            var gate = new SilenceGate();
            Assert.IsFalse(Feed(gate, 0.002f, 0.3f), "the noise of the room");
            Assert.IsFalse(Feed(gate, 0.1f, 2f), "while talking");
            Assert.IsTrue(gate.Heard);
            Assert.IsFalse(Feed(gate, 0.002f, SilenceGate.PauseSeconds - 0.2f), "a short pause keeps listening");
            Assert.IsTrue(Feed(gate, 0.002f, 0.3f), "the pause is long enough");
        }

        [Test]
        public void APauseBetweenWordsStartsTheCountAgain()
        {
            var gate = new SilenceGate();
            Feed(gate, 0.002f, 0.3f);
            Feed(gate, 0.1f, 1f);
            Assert.IsFalse(Feed(gate, 0.002f, 0.9f));
            Assert.IsFalse(Feed(gate, 0.1f, 0.3f), "talking again");
            Assert.IsFalse(Feed(gate, 0.002f, 0.9f), "the count started over");
        }

        [Test]
        public void NeverEndsAClipWhereNobodySpoke()
        {
            var gate = new SilenceGate();
            Assert.IsFalse(Feed(gate, 0.002f, 15f));
            Assert.IsFalse(gate.Heard);
        }

        [Test]
        public void ALoudRoomIsNotTakenForAVoice()
        {
            var gate = new SilenceGate();
            Feed(gate, 0.02f, 0.3f);
            Assert.IsFalse(Feed(gate, 0.02f, 5f));
            Assert.IsFalse(gate.Heard, "the same noise as at the start");
            Assert.IsFalse(Feed(gate, 0.2f, 1f));
            Assert.IsTrue(gate.Heard);
            Assert.IsTrue(Feed(gate, 0.02f, SilenceGate.PauseSeconds + 0.2f), "back to the noise of the room counts as quiet");
        }
    }
}

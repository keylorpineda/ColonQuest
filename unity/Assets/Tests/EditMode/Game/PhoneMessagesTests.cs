using ColonQuest.Game;
using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class PhoneMessagesTests
    {
        [Test]
        public void SinpeNotice_RotatesAndNeverRunsOut()
        {
            string first = PhoneMessages.SinpeNotice(0);
            Assert.IsNotEmpty(first);
            Assert.AreNotEqual(first, PhoneMessages.SinpeNotice(1));
            Assert.AreEqual(first, PhoneMessages.SinpeNotice(4));
            Assert.IsNotEmpty(PhoneMessages.SinpeNotice(-1));
        }
    }
}

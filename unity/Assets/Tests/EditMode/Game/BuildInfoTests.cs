using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public sealed class BuildInfoTests
    {
        [Test]
        public void Label_NamesTheStageAndTheBuild()
        {
            Assert.AreEqual("0.1." + BuildInfo.Build, BuildInfo.Version);
            Assert.AreEqual("Alpha 0.1." + BuildInfo.Build, BuildInfo.Label);
        }

        [Test]
        public void Credit_NamesTheTeam()
        {
            StringAssert.Contains(MenuEntries.Team, BuildInfo.Credit);
        }
    }
}

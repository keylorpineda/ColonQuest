using NUnit.Framework;

namespace ColonQuest.Api.Tests
{
    public sealed class ApiConfigTests
    {
        private const string Fallback = "https://fallback/api";

        [Test]
        public void Resolve_TheVariableWinsOverTheFile()
        {
            string file = ApiConfig.EnvVariable + "=http://file/api";
            Assert.AreEqual("http://variable/api", ApiConfig.Resolve("http://variable/api/", file, Fallback));
        }

        [Test]
        public void Resolve_ReadsTheFileWithCommentsAndWindowsLineEnds()
        {
            string file = "# Which backend\r\n\r\n" + ApiConfig.EnvVariable + "=http://localhost:8000/api/\r\n";
            Assert.AreEqual("http://localhost:8000/api", ApiConfig.Resolve(null, file, Fallback));
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("not a url", "COLONQUEST_API_URL=neither")]
        [TestCase(null, "# COLONQUEST_API_URL=http://commented/api")]
        public void Resolve_WithoutAnAddressUsesTheFallback(string variable, string file)
        {
            Assert.AreEqual(Fallback, ApiConfig.Resolve(variable, file, Fallback));
        }
    }
}

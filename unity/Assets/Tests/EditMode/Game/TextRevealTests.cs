using NUnit.Framework;

namespace ColonQuest.Game.Tests
{
    public class TextRevealTests
    {
        [Test]
        public void Prefix_ShowsTheFirstLetters()
        {
            Assert.AreEqual("Hol", TextReveal.Prefix("Hola mae", 3));
            Assert.AreEqual("Hola mae", TextReveal.Prefix("Hola mae", 99));
            Assert.AreEqual(string.Empty, TextReveal.Prefix("Hola", 0));
        }

        [Test]
        public void Tags_DoNotCountAndAreClosed()
        {
            string full = "Total <color=#ff0000>-50 000</color> hoy";
            Assert.AreEqual(full.Replace("<color=#ff0000>", string.Empty).Replace("</color>", string.Empty).Length, TextReveal.VisibleLength(full));
            Assert.AreEqual("Total <color=#ff0000>-5</color>", TextReveal.Prefix(full, 8));
            Assert.AreEqual(full, TextReveal.Prefix(full, 999));
        }

        [Test]
        public void ALessThanSign_IsJustALetter()
        {
            Assert.AreEqual(3, TextReveal.VisibleLength("a<3"));
            Assert.AreEqual("a<", TextReveal.Prefix("a<3", 2));
        }
    }
}

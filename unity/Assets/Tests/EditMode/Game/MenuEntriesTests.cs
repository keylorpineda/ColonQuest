using NUnit.Framework;
using UnityEngine;

namespace ColonQuest.Game.Tests
{
    public sealed class MenuEntriesTests
    {
        [Test]
        public void For_Desktop_ListsAllFiveButtonsInOrder()
        {
            var entries = MenuEntries.For(RuntimePlatform.WindowsPlayer);

            CollectionAssert.AreEqual(
                new[] { MenuEntry.Story, MenuEntry.Demo, MenuEntry.Settings, MenuEntry.Credits, MenuEntry.Quit },
                entries);
        }

        [Test]
        public void For_Browser_HidesQuit()
        {
            var entries = MenuEntries.For(RuntimePlatform.WebGLPlayer);

            CollectionAssert.DoesNotContain(entries, MenuEntry.Quit);
            Assert.AreEqual(4, entries.Count);
            Assert.IsFalse(MenuEntries.QuitAvailable(RuntimePlatform.WebGLPlayer));
        }

        [TestCase(MenuEntry.Story, "Modo historia")]
        [TestCase(MenuEntry.Demo, "Demo de funciones")]
        [TestCase(MenuEntry.Settings, "Ajustes")]
        [TestCase(MenuEntry.Credits, "Créditos")]
        [TestCase(MenuEntry.Quit, "Salir")]
        public void LabelOf_IsTheSpanishName(MenuEntry entry, string label)
        {
            Assert.AreEqual(label, MenuEntries.LabelOf(entry));
        }

        [Test]
        public void Credits_NameBothMakersAndEveryToolWithAReasonAndATime()
        {
            CollectionAssert.AreEqual(new[] { "Keylor Pineda", "Jefferson Abarca" }, MenuEntries.Makers);

            Assert.AreEqual(5, MenuEntries.Tools.Length, "the five models the game uses");
            foreach (var tool in MenuEntries.Tools)
            {
                Assert.AreEqual(3, tool.Length);
                StringAssert.StartsWith("Muse ", tool[0]);
                Assert.IsTrue(ColonQuest.World.Translations.English.ContainsKey(tool[1]), tool[1]);
                Assert.IsTrue(ColonQuest.World.Translations.English.ContainsKey(tool[2]), tool[2]);
            }
        }

        [Test]
        public void Credits_EveryOtherLineIsTranslated()
        {
            foreach (var rows in new[] { MenuEntries.Agents, MenuEntries.Figures, MenuEntries.Stack })
            {
                foreach (var row in rows)
                {
                    Assert.IsTrue(ColonQuest.World.Translations.English.ContainsKey(row[1]), row[1]);
                }
            }
        }

        [Test]
        public void Credits_NameTheFontsInTheBuildWithTheirLicense()
        {
            Assert.AreEqual(2, MenuEntries.Fonts.Length);
            foreach (var font in MenuEntries.Fonts)
            {
                StringAssert.Contains("SIL Open Font License", font[1]);
            }
        }
    }

    public sealed class MenuMotionTests
    {
        [Test]
        public void Drift_MovesRightAtTheSpeed()
        {
            Assert.AreEqual(110f, MenuMotion.Drift(100f, 20f, 0.5f, -500f, 500f), 0.0001f);
        }

        [Test]
        public void Drift_ComesBackInOnTheLeftKeepingTheOvershoot()
        {
            Assert.AreEqual(-495f, MenuMotion.Drift(495f, 10f, 1f, -500f, 500f), 0.0001f);
        }

        [Test]
        public void Drift_ABrokenRangeStaysPut()
        {
            Assert.AreEqual(10f, MenuMotion.Drift(3f, 5f, 1f, 10f, 10f));
        }

        [Test]
        public void Bob_NeverLeavesTheAmplitude()
        {
            for (float t = 0f; t < 20f; t += 0.37f)
            {
                Assert.LessOrEqual(Mathf.Abs(MenuMotion.Bob(t, 1.3f, 14f)), 14f + 0.0001f);
            }
        }
    }
}

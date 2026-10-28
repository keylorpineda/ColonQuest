using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ColonQuest.World.Tests
{
    public sealed class LocTests
    {
        private static IEnumerable<string> Holes(string text)
        {
            return Regex.Matches(text, @"\{\d+\}").Cast<Match>().Select(m => m.Value).OrderBy(v => v);
        }

        [Test]
        public void Spanish_ReturnsTheTextAsItIs()
        {
            Assert.AreEqual("Ajustes", Loc.Translate("Ajustes", GameLanguage.Spanish));
            Assert.AreEqual("texto que no existe", Loc.Translate("texto que no existe", GameLanguage.Spanish));
        }

        [Test]
        public void English_TranslatesAKnownTextAndLeavesAnUnknownOneInSpanish()
        {
            Assert.AreEqual("Settings", Loc.Translate("Ajustes", GameLanguage.English));
            Assert.AreEqual("texto que no existe", Loc.Translate("texto que no existe", GameLanguage.English), "a missing entry never breaks the game");
            Assert.IsNull(Loc.Translate(null, GameLanguage.English));
            Assert.AreEqual(string.Empty, Loc.Translate(string.Empty, GameLanguage.English));
        }

        [Test]
        public void EveryEntry_HasAnEnglishTextThatIsNotEmptyAndKeepsItsHoles()
        {
            foreach (var pair in Translations.English)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Value), "empty English for: " + pair.Key);
                CollectionAssert.AreEqual(Holes(pair.Key).ToList(), Holes(pair.Value).ToList(), "the {n} holes differ in: " + pair.Key);
            }
        }

        [Test]
        public void EveryEntry_HasNoDashesOrEmojis()
        {
            foreach (var pair in Translations.English)
            {
                Assert.IsFalse(pair.Value.Contains("—") || pair.Value.Contains("–"), "dash in: " + pair.Value);
                Assert.IsFalse(pair.Value.Any(c => char.IsSurrogate(c)), "emoji in: " + pair.Value);
            }
        }

        [Test]
        public void English_TheTextsThatAreNotNamesAreNotLeftInSpanish()
        {
            var sameAsKey = Translations.English.Where(p => p.Key == p.Value).Select(p => p.Key).ToList();
            CollectionAssert.IsEmpty(sameAsKey.Where(k => k != "Cifras de ejemplo del juego."), "an entry that did not change was never translated");
        }

        [Test]
        public void F_FillsTheHolesAfterTranslating()
        {
            // The language is read from the saved settings; this checks the pure part with the translation table.
            string english = Loc.Translate("Mes {0} de {1}", GameLanguage.English);
            Assert.AreEqual("Month 2 of 3", string.Format(english, 2, 3));
        }

        // ---------------------------------------------------------------- the Costa Rican words

        [TestCase("Pulperia", "corner store")]
        [TestCase("Soda El Muelle", "diner")]
        [TestCase("Cuotas al mes", "Installments")]
        [TestCase("Ánimo", "Mood")]
        [TestCase("Deuda con prestamista", "Loan-shark")]
        [TestCase("Los cobradores", "collectors")]
        public void Glossary_CostaRicanWordsBecomeColloquialEnglish(string spanish, string expectedPart)
        {
            StringAssert.Contains(expectedPart, Loc.Translate(spanish, GameLanguage.English));
        }

        [Test]
        public void Glossary_MaeBecomesDudeAndPlataBecomesMoney()
        {
            string collectors = Loc.Translate("«Mae, usted debe plata. Hoy venimos por {0}.»", GameLanguage.English);
            StringAssert.Contains("Dude", collectors);
            StringAssert.Contains("money", collectors);
            StringAssert.DoesNotContain("plata", collectors);
            StringAssert.DoesNotContain("Mae", collectors);
        }

        [Test]
        public void Glossary_TheWordsWithoutAnEnglishTwinKeepTheirNameWithAnExplanation()
        {
            StringAssert.Contains("pulpería", Loc.Translate("Pulperia", GameLanguage.English));
            StringAssert.Contains("soda", Loc.Translate("Soda El Muelle", GameLanguage.English));
        }

        // ---------------------------------------------------------------- the saved language

        [Test]
        public void Settings_TheLanguageIsKeptInTheTextForm()
        {
            var settings = new GameSettings { SoundOn = false, Volume = 0.4f, Language = GameLanguage.English };

            var again = GameSettings.Parse(settings.Serialize());

            Assert.AreEqual(GameLanguage.English, again.Language);
            Assert.IsFalse(again.SoundOn);
            Assert.AreEqual("0;0.40;en", settings.Serialize());
        }

        [Test]
        public void Settings_SpanishAddsNothingToTheText()
        {
            Assert.AreEqual("1;1.00", new GameSettings().Serialize());
        }

        [TestCase("1;1.00;fr")]
        [TestCase("1;1.00;")]
        [TestCase("1;1.00;EN")]
        public void Settings_AnythingButEnIsSpanish(string text)
        {
            Assert.AreEqual(GameLanguage.Spanish, GameSettings.Parse(text).Language);
        }

        [Test]
        public void Settings_OldSavedTextWithoutALanguageStillLoads()
        {
            var old = GameSettings.Parse("0;0.50");

            Assert.IsFalse(old.SoundOn);
            Assert.AreEqual(0.5f, old.Volume, 0.0001f);
            Assert.AreEqual(GameLanguage.Spanish, old.Language);
        }
    }
}

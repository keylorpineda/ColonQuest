using ColonQuest.World;
using NUnit.Framework;

/// <summary>
/// The game starts in English, but these tests read its texts by their Spanish keys, so they run in Spanish. It has no
/// namespace so it covers the whole assembly.
/// </summary>
[SetUpFixture]
public sealed class GameTestsLanguage
{
    private GameLanguage saved;

    [OneTimeSetUp]
    public void UseSpanish()
    {
        saved = GameSettings.Current.Language;
        GameSettings.Current.Language = GameLanguage.Spanish;
    }

    [OneTimeTearDown]
    public void Restore()
    {
        GameSettings.Current.Language = saved;
        GameSettings.Current.Save();
    }
}

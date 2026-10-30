using System.Collections;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>Plays the main menu: its buttons, the online notice, the settings and the start of a story run.</summary>
    public sealed class MenuPlayTests
    {
        private static Transform Menu => GameObject.Find("MainMenu").transform;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            ResetSettings();
            yield return SceneManager.LoadSceneAsync(SceneNames.Menu);
            yield return null;
        }

        [TearDown]
        public void RestoreSettings()
        {
            ResetSettings();
        }

        private static void ResetSettings()
        {
            var settings = GameSettings.Current;
            settings.SoundOn = true;
            settings.Volume = 1f;
            settings.Language = GameLanguage.Spanish;
            settings.Save();
            AudioDirector.EnsureCreated();
            AudioDirector.Apply(settings);
        }

        private static Button ButtonAt(string path)
        {
            var found = Menu.Find(path);
            Assert.IsNotNull(found, "no button at " + path);
            return found.GetComponent<Button>();
        }

        private static bool Active(string path)
        {
            var found = Menu.Find(path);
            Assert.IsNotNull(found, "no object at " + path);
            return found.gameObject.activeSelf;
        }

        private static string LabelOf(string path)
        {
            return Menu.Find(path + "/Body/Text").GetComponent<Text>().text;
        }

        [UnityTest]
        public IEnumerator Menu_IsTheFirstSceneInTheBuild()
        {
            yield return null;

            Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name);
#if UNITY_EDITOR
            Assert.AreEqual(SceneNames.Menu, System.IO.Path.GetFileNameWithoutExtension(UnityEditor.EditorBuildSettings.scenes[0].path));
#endif
        }

        [UnityTest]
        public IEnumerator Menu_ShowsTheButtonsOfThePlatform()
        {
            yield return null;

            Assert.IsNotNull(Menu.Find("Button_Modo historia"));
            Assert.IsNotNull(Menu.Find("Button_Jugar online"));
            Assert.IsNotNull(Menu.Find("Button_Ajustes"));
            Assert.IsNotNull(Menu.Find("Button_Créditos"));
            Assert.AreEqual(MenuEntries.QuitAvailable(Application.platform), Menu.Find("Button_Salir") != null);
        }

        [UnityTest]
        public IEnumerator Online_OnlyShowsTheNoticeAndStaysInTheMenu()
        {
            yield return null;
            Assert.IsFalse(Active("Notice"));

            ButtonAt("Button_Jugar online").onClick.Invoke();
            yield return null;

            Assert.IsTrue(Active("Notice"));
            Assert.IsNotNull(Menu.Find("Notice/Card/Body/Label_" + MenuEntries.OnlineNotice));
            Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name, "it does not start anything");

            ButtonAt("Notice/Card/Body/Button_Entendido").onClick.Invoke();
            yield return null;
            Assert.IsFalse(Active("Notice"));
        }

        [UnityTest]
        public IEnumerator Credits_OpenAndCloseAndNameTheTeam()
        {
            yield return null;

            ButtonAt("Button_Créditos").onClick.Invoke();
            yield return null;
            Assert.IsTrue(Active("Credits"));
            Assert.IsNotNull(Menu.Find("Credits/Card/Body/Label_Hecho por " + MenuEntries.Team));
            Assert.IsNotNull(Menu.Find("Credits/Card/Body/Label_Keylor y Jefferson"));

            ButtonAt("Credits/Card/Body/Button_Volver").onClick.Invoke();
            yield return null;
            Assert.IsFalse(Active("Credits"));
        }

        [UnityTest]
        public IEnumerator Settings_SoundSwitchMutesTheAudioAndIsSaved()
        {
            yield return null;
            ButtonAt("Button_Ajustes").onClick.Invoke();
            yield return null;
            Assert.IsTrue(Active("Settings"));
            Assert.AreEqual("Sonido: activado", LabelOf("Settings/Card/Body/Button_Sonido"));

            ButtonAt("Settings/Card/Body/Button_Sonido").onClick.Invoke();
            yield return null;

            Assert.IsTrue(AudioDirector.Muted);
            Assert.AreEqual("Sonido: desactivado", LabelOf("Settings/Card/Body/Button_Sonido"));
            Assert.AreEqual("0;1.00", PlayerPrefs.GetString(GameSettings.StoreKey));

            ButtonAt("Settings/Card/Body/Button_Sonido").onClick.Invoke();
            yield return null;
            Assert.IsFalse(AudioDirector.Muted);
        }

        [UnityTest]
        public IEnumerator Settings_LanguageButtonSwitchesTheMenuToEnglishAndBack()
        {
            yield return null;
            ButtonAt("Button_Ajustes").onClick.Invoke();
            yield return null;
            Assert.AreEqual("Idioma: Español", LabelOf("Settings/Card/Body/Button_Idioma"));
            Assert.AreEqual("Modo historia", LabelOf("Button_Modo historia"));

            ButtonAt("Settings/Card/Body/Button_Idioma").onClick.Invoke();
            yield return null;

            Assert.AreEqual(GameLanguage.English, Loc.Current);
            Assert.AreEqual("Language: English", LabelOf("Settings/Card/Body/Button_Idioma"), "the button names the language in its own language");
            Assert.AreEqual("Story mode", LabelOf("Button_Modo historia"));
            Assert.AreEqual("Play online", LabelOf("Button_Jugar online"));
            Assert.AreEqual("Sound: on", LabelOf("Settings/Card/Body/Button_Sonido"));
            StringAssert.EndsWith(";en", PlayerPrefs.GetString(GameSettings.StoreKey));

            ButtonAt("Settings/Card/Body/Button_Volver").onClick.Invoke();
            ButtonAt("Button_Créditos").onClick.Invoke();
            yield return null;
            Assert.IsNotNull(Menu.Find("Credits/Card/Body/Label_Hecho por DoWhile Team"));
            Assert.AreEqual("Made by DoWhile Team", Menu.Find("Credits/Card/Body/Label_Hecho por DoWhile Team").GetComponent<Text>().text);

            ButtonAt("Credits/Card/Body/Button_Volver").onClick.Invoke();
            ButtonAt("Button_Ajustes").onClick.Invoke();
            ButtonAt("Settings/Card/Body/Button_Idioma").onClick.Invoke();
            yield return null;
            Assert.AreEqual(GameLanguage.Spanish, Loc.Current);
            Assert.AreEqual("Modo historia", LabelOf("Button_Modo historia"));
        }

        [UnityTest]
        public IEnumerator Settings_VolumeButtonsStepAndShowThePercent()
        {
            yield return null;
            ButtonAt("Button_Ajustes").onClick.Invoke();
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                ButtonAt("Settings/Card/Body/Button_-").onClick.Invoke();
            }

            yield return null;
            Assert.AreEqual(0.7f, AudioDirector.Volume, 0.0001f);
            Assert.AreEqual("70 %", Menu.Find("Settings/Card/Body/Label_").GetComponent<Text>().text);
            Assert.AreEqual("1;0.70", PlayerPrefs.GetString(GameSettings.StoreKey));

            for (int i = 0; i < 5; i++)
            {
                ButtonAt("Settings/Card/Body/Button_+").onClick.Invoke();
            }

            Assert.AreEqual(1f, AudioDirector.Volume, 0.0001f, "the volume stops at the top");

            ButtonAt("Settings/Card/Body/Button_Volver").onClick.Invoke();
            yield return null;
            Assert.IsFalse(Active("Settings"));
        }

        [UnityTest]
        public IEnumerator Story_StartsAFreshRunInTheTown()
        {
            // A run already under way, as when the player comes back to the menu from the pause window.
            var session = GameSession.Create();
            yield return null;
            session.BeginTask(StationType.Garage);
            yield return new WaitForSeconds(0.3f);
            session.Choose(session.CurrentEvent.Choices[0].Id);
            yield return null;
            Assert.AreEqual(1, session.Loop.TasksDone, "setup: a task was done");

            ButtonAt("Button_Modo historia").onClick.Invoke();
            yield return WaitFor(SceneNames.Town);

            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name);
            Assert.IsNotNull(GameSession.Instance);
            Assert.AreEqual(0, GameSession.Instance.Loop.TasksDone, "a new life starts from zero");
            Assert.AreEqual(0, GameSession.Instance.Loop.MonthsPlayed);
            Assert.IsNotNull(GameObject.Find("GameUi"));
            Assert.IsTrue(GameObject.Find("GameUi").GetComponent<Canvas>().enabled, "the overlay is back in the town");
        }

        private static IEnumerator WaitFor(string sceneName)
        {
            for (float waited = 0f; waited < 10f && SceneManager.GetActiveScene().name != sceneName; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }
    }

    /// <summary>Plays the pause window of the town: resume, settings and the way back to the menu.</summary>
    public sealed class PausePlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        private static GameUi GameUiComponent => Ui.GetComponent<GameUi>();

        [UnitySetUp]
        public IEnumerator StartOffline()
        {
            ApiClient.Instance.Configure(new OfflineTransport(), new ApiConfig());
            GameSession.Instance.StartNewGame();
            PlayerController.Blocked = false;
            Time.timeScale = 1f;
            yield return null;
        }

        [TearDown]
        public void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        private static void Click(string path)
        {
            var found = Ui.Find(path);
            Assert.IsNotNull(found, "no button at " + path);
            found.GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator Pause_StopsTimeAndBlocksThePlayerUntilResumed()
        {
            GameUiComponent.OpenPause();
            yield return null;

            Assert.IsTrue(GameUiComponent.PauseOpen);
            Assert.IsTrue(Ui.Find("Pause").gameObject.activeSelf);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(PlayerController.Blocked);

            Click("Pause/Card/Body/Button_Reanudar");
            yield return null;

            Assert.IsFalse(GameUiComponent.PauseOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(PlayerController.Blocked);
        }

        [UnityTest]
        public IEnumerator Pause_SettingsOpenInPlaceOfThePauseCardAndComeBack()
        {
            GameUiComponent.OpenPause();
            yield return null;

            Click("Pause/Card/Body/Button_Ajustes");
            yield return null;
            Assert.IsFalse(Ui.Find("Pause").gameObject.activeSelf);
            Assert.IsTrue(Ui.Find("Settings").gameObject.activeSelf);
            Assert.IsTrue(GameUiComponent.PauseOpen, "still paused while in the settings");

            Click("Settings/Card/Body/Button_Volver");
            yield return null;
            Assert.IsTrue(Ui.Find("Pause").gameObject.activeSelf);
            Assert.IsFalse(Ui.Find("Settings").gameObject.activeSelf);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pause_BackToMenuNeedsASecondPress()
        {
            GameUiComponent.OpenPause();
            yield return null;
            string path = "Pause/Card/Body/Button_" + PauseMenu.MenuLabel;

            Click(path);
            yield return null;
            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name, "one press only asks");
            Assert.AreEqual(PauseMenu.SureLabel, Ui.Find(path + "/Body/Text").GetComponent<Text>().text);

            Click(path);
            for (float waited = 0f; waited < 10f && SceneManager.GetActiveScene().name != SceneNames.Menu; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(GameObject.Find("GameUi").GetComponent<Canvas>().enabled, "the town overlay hides in the menu");
            Assert.IsFalse(PlayerController.Blocked);
        }
    }
}

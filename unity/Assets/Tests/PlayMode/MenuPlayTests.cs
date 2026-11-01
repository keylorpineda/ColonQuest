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

            // The saved games live in memory here: a test never touches the real ones.
            realSlots = GameSession.Slots;
            GameSession.Slots = new SaveSlots(new MemorySaveStore());
            yield return SceneManager.LoadSceneAsync(SceneNames.Menu);
            yield return null;
        }

        [TearDown]
        public void RestoreSettings()
        {
            ResetSettings();

            // The session outlives the test: without a slot it cannot write in the real saved games later.
            if (GameSession.Instance != null)
            {
                GameSession.Instance.StartNewGame();
            }

            GameSession.Slots = realSlots;
        }

        private SaveSlots realSlots;

        [UnityTest]
        public IEnumerator LoadingScreen_ShowsThePictureAndFinishesTheLoad()
        {
            LoadingScreen.Load(SceneNames.Menu);
            yield return null;
            Assert.IsTrue(LoadingScreen.IsLoading);
            var screen = GameObject.Find("LoadingScreen");
            Assert.IsNotNull(screen.transform.Find("KeyArt/VolcanoSmoke"), "the smoke of the menu picture");
            Assert.IsNotNull(screen.transform.Find("KeyArt/Toucan"), "the toucan of the menu picture");

            float waited = 0f;
            while (LoadingScreen.IsLoading && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(LoadingScreen.IsLoading, "the bar gets to the end and the screen goes away");
            yield return null;
            Assert.IsNull(GameObject.Find("LoadingScreen"));
            Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name);
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
            Assert.IsNotNull(Menu.Find("Credits/Card/Body/Label_Desarrollado por"));
            Assert.AreEqual(MenuEntries.Team, Menu.Find("Credits/Card/Body/Team").GetComponent<Text>().text);
            Assert.IsNotNull(Menu.Find("Credits/Card/Body/Header"), "every window has the same title band");

            ButtonAt("Credits/Card/Body/Button_Volver").onClick.Invoke();
            yield return null;
            Assert.IsFalse(Active("Credits"));
        }

        [UnityTest]
        public IEnumerator Quit_AsksBeforeClosingTheGame()
        {
            yield return null;
            if (Menu.Find("Button_Salir") == null)
            {
                Assert.Pass("this platform has no quit button");
            }

            ButtonAt("Button_Salir").onClick.Invoke();
            yield return null;
            Assert.IsTrue(Active("QuitConfirm"), "quitting asks first");

            ButtonAt("QuitConfirm/Card/Body/Button_Cancelar").onClick.Invoke();
            yield return null;
            Assert.IsFalse(Active("QuitConfirm"));
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
            Assert.AreEqual("Developed by", Menu.Find("Credits/Card/Body/Label_Desarrollado por").GetComponent<Text>().text);

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
            Assert.IsTrue(Active("Slots"), "the story button opens the saved games");
            ButtonAt(SlotButton(0, "Nueva partida")).onClick.Invoke();
            Assert.IsTrue(Active("NewGame"), "a new life first asks who the player is");
            yield return StartLifeAndSkipOpening();

            Assert.AreEqual(SceneNames.Casa, SceneManager.GetActiveScene().name, "after the goodbye the player stays in the house");
            Assert.IsNotNull(GameSession.Instance);
            Assert.AreEqual(0, GameSession.Instance.Loop.TasksDone, "a new life starts from zero");
            Assert.AreEqual(0, GameSession.Instance.Loop.MonthsPlayed);
            Assert.AreEqual(0, GameSession.Instance.ActiveSlot);
            Assert.IsNotNull(GameSession.Slots.Load(0), "a new game is saved from the start");
            Assert.IsNotNull(GameObject.Find("GameUi"));
            Assert.IsTrue(GameObject.Find("GameUi").GetComponent<Canvas>().enabled, "the overlay is back after the opening");
        }

        /// <summary>Presses Empezar in the new game window, waits for the opening in the house and skips it.</summary>
        private static IEnumerator StartLifeAndSkipOpening()
        {
            var nameBox = Menu.Find("NewGame/Card/Body/NameBox").GetComponent<InputField>();
            if (string.IsNullOrWhiteSpace(nameBox.text))
            {
                nameBox.text = "Alex";
            }

            ButtonAt("NewGame/Card/Body/Button_Empezar").onClick.Invoke();
            yield return WaitFor(SceneNames.Casa);
            for (float waited = 0f; waited < 10f && !Interaction.Playing; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(Interaction.Playing, "the opening plays in the house");
            Assert.IsNotNull(GameObject.Find("Mama"), "the mother is there for the opening");
            Vector3 before = GameObject.FindWithTag(Station.PlayerTag).transform.position;
            yield return new WaitForSeconds(1.2f);
            float closeUp = Camera.main.orthographicSize;
            Interaction.SkipCurrent();
            for (float waited = 0f; waited < 10f && Interaction.Playing; waited += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            yield return null;
            Assert.Greater(Camera.main.orthographicSize, closeUp, "the camera comes back from the close-up even when the scene is skipped");
            Assert.AreEqual(SceneNames.Casa, SceneManager.GetActiveScene().name, "the goodbye does not walk the player out");
            Assert.Less(Vector2.Distance(before, GameObject.FindWithTag(Station.PlayerTag).transform.position), 0.5f,
                "the player stays standing where the goodbye ended");
        }

        [UnityTest]
        public IEnumerator NewGame_DoesNotStartWithoutAName()
        {
            ButtonAt("Button_Modo historia").onClick.Invoke();
            ButtonAt(SlotButton(1, "Nueva partida")).onClick.Invoke();
            var nameBox = Menu.Find("NewGame/Card/Body/NameBox").GetComponent<InputField>();
            var question = Menu.Find("NewGame/Card/Body/NameQuestion").GetComponent<Text>();

            foreach (string blank in new[] { string.Empty, "    ", "<>{}" })
            {
                nameBox.text = blank;
                ButtonAt("NewGame/Card/Body/Button_Empezar").onClick.Invoke();
                yield return null;

                Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name, "still in the menu");
                Assert.IsTrue(Menu.Find("NewGame").gameObject.activeSelf, "the window stays open");
                Assert.AreEqual("Escribí tu nombre para empezar.", question.text);
                Assert.IsNull(GameSession.Slots.Load(1), "nothing is saved");
            }

            nameBox.text = "María";
            Assert.AreEqual("¿Cómo te llamás?", question.text, "typing takes the warning away");
        }

        [UnityTest]
        public IEnumerator NewGame_KeepsTheNameAndTheLook()
        {
            ButtonAt("Button_Modo historia").onClick.Invoke();
            ButtonAt(SlotButton(1, "Nueva partida")).onClick.Invoke();
            Menu.Find("NewGame/Card/Body/NameBox").GetComponent<InputField>().text = "  María  ";
            ButtonAt("NewGame/Card/Body/Button_Mujer").onClick.Invoke();
            yield return StartLifeAndSkipOpening();

            Assert.AreEqual("María", PlayerProfile.Name);
            Assert.IsTrue(PlayerProfile.Female);
            Assert.AreEqual("María", GameSession.Instance.RunName, "an empty game name takes the player's");
            var saved = GameSession.Slots.Load(1);
            Assert.AreEqual("María", saved.characterName);
            Assert.IsTrue(saved.female);

            yield return null;
            var player = GameObject.FindWithTag(Station.PlayerTag).transform;
            Assert.IsNotNull(player.Find("Bob"), "the scenes and the animator find the body by this name");
            Assert.IsTrue(player.Find("Bob").gameObject.activeSelf);
            Assert.IsNull(player.Find(PlayerLookSwitch.AlternateBob), "only one look stays");

            yield return SceneManager.LoadSceneAsync(SceneNames.Casa);
            yield return null;
            var mother = GameObject.Find("Mama");
            Assert.IsNotNull(mother, "the house is the mother's: she is still there after the opening");
            Assert.Less(mother.transform.position.y, 0.8f, "back in the living room, out of the bedroom");
            Assert.IsNotNull(GameObject.Find("Thing_Mama"), "the player can talk to her");
        }

        private static string SlotButton(int slot, string text)
        {
            return "Slots/Card/Body/Rows/Slot" + slot + "/Body/Button_" + text;
        }

        [UnityTest]
        public IEnumerator Continue_BringsTheSavedGameBack()
        {
            var saved = new GameLoop();
            saved.State.Cash = 77_000L;
            saved.Sleep();
            GameSession.Slots.Save(1, SaveSlot.From("Ana", saved, System.DateTime.UtcNow));

            ButtonAt("Button_Modo historia").onClick.Invoke();
            ButtonAt(SlotButton(1, "Continuar")).onClick.Invoke();
            yield return WaitFor(SceneNames.Town);

            var session = GameSession.Instance;
            Assert.AreEqual(1, session.ActiveSlot);
            Assert.AreEqual("Ana", session.RunName);
            Assert.AreEqual(saved.State.Cash, session.State.Cash);
            Assert.AreEqual(2, session.Loop.Clock.Day);
        }

        [UnityTest]
        public IEnumerator Delete_AsksOnceMoreBeforeErasing()
        {
            GameSession.Slots.Save(0, SaveSlot.From("Ana", new GameLoop(), System.DateTime.UtcNow));
            ButtonAt("Button_Modo historia").onClick.Invoke();

            ButtonAt(SlotButton(0, "Borrar")).onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameSession.Slots.Load(0), "the first press only asks");

            ButtonAt(SlotButton(0, "¿Seguro? Borrar")).onClick.Invoke();
            yield return null;
            Assert.IsNull(GameSession.Slots.Load(0));
            Assert.IsNotNull(Menu.Find(SlotButton(0, "Nueva partida")), "the slot is empty again");
        }

        [UnityTest]
        public IEnumerator New_OverASavedGameAsksOnceMore()
        {
            var saved = new GameLoop();
            saved.State.Cash = 77_000L;
            GameSession.Slots.Save(0, SaveSlot.From("Ana", saved, System.DateTime.UtcNow));
            ButtonAt("Button_Modo historia").onClick.Invoke();

            ButtonAt(SlotButton(0, "Nueva")).onClick.Invoke();
            yield return null;
            Assert.AreEqual(SceneNames.Menu, SceneManager.GetActiveScene().name, "the first press only asks");
            Assert.AreEqual(77_000L, GameSession.Slots.Load(0).Cash);

            ButtonAt(SlotButton(0, "¿Seguro? Empezar")).onClick.Invoke();
            yield return StartLifeAndSkipOpening();
            Assert.AreNotEqual(77_000L, GameSession.Slots.Load(0).Cash, "the new life took the slot");
        }

        [UnityTest]
        public IEnumerator BackToTheMenu_SavesTheRun()
        {
            ButtonAt("Button_Modo historia").onClick.Invoke();
            ButtonAt(SlotButton(2, "Nueva partida")).onClick.Invoke();
            yield return StartLifeAndSkipOpening();

            GameSession.Instance.Loop.Clock.Advance(GameRules.SecondsPerGameHour * 2f, GameRules.SecondsPerGameHour);
            yield return SceneManager.LoadSceneAsync(SceneNames.Menu);
            yield return null;

            Assert.AreEqual(GameSession.Instance.Loop.Clock.Hour, (int)(GameSession.Slots.Load(2).run.minutes / 60f));
            Assert.GreaterOrEqual(GameSession.Slots.Load(2).run.minutes, (GameRules.DayStartHour + 2) * 60f);
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
        public IEnumerator Pause_BackToMenuAsksToSaveAndYesSaves()
        {
            yield return LeaveFromPause(true);
            Assert.IsNotNull(GameSession.Slots.Load(0), "Sí writes the run in its slot");
        }

        [UnityTest]
        public IEnumerator Pause_BackToMenuAsksToSaveAndNoLeavesWithoutSaving()
        {
            yield return LeaveFromPause(false);
            Assert.IsNull(GameSession.Slots.Load(0), "No leaves the slot as it was");
        }

        private IEnumerator LeaveFromPause(bool save)
        {
            GameSession.Instance.AssignSlot(0, "Prueba", "Ana", true);
            GameSession.Slots = new SaveSlots(new MemorySaveStore());
            GameUiComponent.OpenPause();
            yield return null;

            Click("Pause/Card/Body/Button_" + PauseMenu.MenuLabel);
            yield return null;
            Assert.AreEqual(SceneNames.Town, SceneManager.GetActiveScene().name, "one press only asks");
            Assert.IsFalse(Ui.Find("Pause").gameObject.activeSelf);
            Assert.IsTrue(Ui.Find("PauseConfirm").gameObject.activeSelf);
            Assert.AreEqual(PauseMenu.SureLabel, Ui.Find("PauseConfirm/Card/Body/Label_" + PauseMenu.SureLabel).GetComponent<Text>().text);

            Click("PauseConfirm/Card/Body/Button_" + (save ? PauseMenu.YesLabel : PauseMenu.NoLabel));
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

using ColonQuest.Api;
using ColonQuest.Finance;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The three saved games of story mode, opened by "Modo historia". Each slot shows its name, the month and the week, the
    /// cash, the savings, the mood and when it was last played, with Continuar, Nueva and Borrar. Starting over a used slot
    /// and deleting one both ask for a second press, so a saved life is never lost by one click.
    /// </summary>
    public sealed partial class MainMenu
    {
        private GameObject slots;
        private RectTransform slotRows;
        private int confirmSlot = -1;
        private bool confirmDelete;

        private GameObject finishedReport;
        private Text reportTitle;
        private Text reportSubtitle;
        private Text reportBody;

        private void BuildSlots()
        {
            slots = UiKit.Overlay(root, "Slots");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(slots.transform, "Card", UiKit.Paper, new Vector2(1240f, 820f), center, Vector2.zero);

            var title = UiKit.Label(card, "Modo historia", 60, UiKit.Ink, new Vector2(1100f, 76f), top, new Vector2(0f, -26f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;
            UiKit.Label(card, "Elegí una partida guardada o empezá una vida nueva.", 30, GameUiColors.Muted, new Vector2(1100f, 44f), top, new Vector2(0f, -104f), TextAnchor.MiddleCenter);
            Divider(card, -160f, 1100f);

            slotRows = UiKit.Rect(card, "Rows", new Vector2(1160f, 560f), top, new Vector2(0f, -176f));
            UiKit.MakeButton(card, "Volver", GameUiColors.Pink, new Vector2(300f, 76f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), CloseWindows);
            slots.SetActive(false);
            BuildFinishedReport();
        }

        private void BuildFinishedReport()
        {
            finishedReport = UiKit.Overlay(root, "FinishedReport");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(finishedReport.transform, "Card", UiKit.Paper, new Vector2(1040f, 760f), center, Vector2.zero);

            reportTitle = UiKit.Label(card, "Reporte Final", 50, UiKit.Ink, new Vector2(960f, 64f), top, new Vector2(0f, -24f), TextAnchor.MiddleCenter);
            reportTitle.font = UiKit.TitleFont;
            reportSubtitle = UiKit.Label(card, string.Empty, 28, GameUiColors.Muted, new Vector2(960f, 38f), top, new Vector2(0f, -88f), TextAnchor.MiddleCenter);
            Divider(card, -136f, 960f);

            reportBody = UiKit.Label(card, string.Empty, 26, UiKit.Ink, new Vector2(940f, 480f), top, new Vector2(0f, -150f), TextAnchor.UpperLeft);
            reportBody.lineSpacing = 1.15f;

            UiKit.MakeButton(card, "Cerrar", GameUiColors.Green, new Vector2(280f, 68f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), () => finishedReport.SetActive(false));
            finishedReport.SetActive(false);
        }

        private void ShowSlots()
        {
            confirmSlot = -1;
            RefreshSlots();
            slots.SetActive(true);
        }

        /// <summary>Draws the three rows again from what is saved now.</summary>
        private void RefreshSlots()
        {
            for (int i = slotRows.childCount - 1; i >= 0; i--)
            {
                Destroy(slotRows.GetChild(i).gameObject);
            }

            var topLeft = new Vector2(0f, 1f);
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                int slot = i;
                var saved = GameSession.Slots.Load(slot);
                var row = UiKit.Card(slotRows, "Slot" + slot, saved != null ? GamePalette.Hex("#FFF6DD") : GamePalette.Hex("#EEF1F6"),
                    new Vector2(1160f, 166f), new Vector2(0.5f, 1f), new Vector2(0f, -slot * 184f));

                if (saved == null)
                {
                    UiKit.Label(row, Loc.F("Espacio {0}", slot + 1), 36, UiKit.Ink, new Vector2(600f, 50f), topLeft, new Vector2(28f, -22f));
                    UiKit.Label(row, "Vacío. Aquí empieza una vida nueva.", 28, GameUiColors.Muted, new Vector2(700f, 44f), topLeft, new Vector2(28f, -82f));
                    SlotButton(row, "Nueva partida", GameUiColors.Green, 0, () => NewInSlot(slot));
                    continue;
                }

                string heading = saved.name + "  ·  " + Loc.F("Mes {0}", saved.Month) + "  ·  " + Loc.F("Semana {0}", saved.Week);
                if (saved.Finished)
                {
                    heading = "[TERMINADA]  " + saved.name + "  ·  " + Loc.T("Año terminado");
                }

                UiKit.Label(row, heading, 34, UiKit.Ink, new Vector2(720f, 50f), topLeft, new Vector2(28f, -16f));
                string numbers = Loc.F("Efectivo {0}", Money.Format(saved.Cash)) + "   " + Loc.F("Ahorros {0}", Money.Format(saved.Savings))
                    + "   " + Loc.F("Ánimo {0}", saved.Wellbeing);
                UiKit.Label(row, numbers, 26, UiKit.Ink, new Vector2(720f, 40f), topLeft, new Vector2(28f, -70f));
                UiKit.Label(row, Loc.F("Última vez: {0}", saved.SavedAtLabel), 24, GameUiColors.Muted, new Vector2(720f, 36f), topLeft, new Vector2(28f, -112f));

                bool asking = confirmSlot == slot;
                if (!saved.Finished)
                {
                    SlotButton(row, "Continuar", GameUiColors.Green, 0, () => ContinueSlot(slot));
                }
                else
                {
                    SlotButton(row, "Ver reporte final", GameUiColors.Blue, 0, () => ViewFinishedReport(saved));
                }

                SlotButton(row, asking && !confirmDelete ? "¿Seguro? Empezar" : "Nueva", GameUiColors.Yellow, 1, () => NewInSlot(slot));
                SlotButton(row, asking && confirmDelete ? "¿Seguro? Borrar" : "Borrar", GameUiColors.Pink, 2, () => DeleteSlot(slot));
            }
        }

        private void ViewFinishedReport(SaveSlot saved)
        {
            if (finishedReport == null)
            {
                BuildFinishedReport();
            }

            var st = ApiMapper.FromDto(saved.run.state);
            long debt = GameRules.DebtBalance(st);
            long netWorth = GameRules.NetWorth(st);

            reportTitle.text = Loc.T("Reporte Final de la Partida");
            reportSubtitle.text = saved.name + "  ·  " + Loc.F("{0} meses jugados", saved.run.monthsPlayed);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Loc.F("Personaje: {0}", string.IsNullOrEmpty(saved.characterName) ? saved.name : saved.characterName));
            sb.AppendLine(Loc.F("Efectivo final: {0}   |   Ahorros: {1}", Money.Format(saved.Cash), Money.Format(saved.Savings)));
            sb.AppendLine(Loc.F("Deuda total: {0}   |   Patrimonio neto: {1}", Money.Format(debt), Money.Format(netWorth)));
            sb.AppendLine(Loc.F("Bienestar emocional: {0}/100", saved.Wellbeing));
            sb.AppendLine(Loc.F("Decisiones financieras tomadas: {0}", saved.run.decisions));
            sb.AppendLine();
            sb.AppendLine("<b>" + Loc.T("Evaluación de Salud Financiera:") + "</b>");

            var dummyLoop = new GameLoop();
            dummyLoop.Restore(saved.run);
            foreach (var line in ReportCard.Build(st, dummyLoop.Holdings, dummyLoop.Bills))
            {
                sb.AppendLine($"• {Loc.T(line.Concept)}: <b>{line.Grade}</b>");
            }

            var assets = AssetsReport.Build(dummyLoop.Holdings, dummyLoop.MonthsPlayed);
            if (assets.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<b>" + Loc.T("Bienes adquiridos:") + "</b>");
                foreach (var a in assets)
                {
                    sb.AppendLine($"• {a}");
                }
            }

            reportBody.text = sb.ToString();
            finishedReport.SetActive(true);
        }

        /// <summary>The buttons of a row, stacked on its right side: 0 at the top.</summary>
        private static void SlotButton(RectTransform row, string text, Color color, int index, System.Action onClick)
        {
            var button = UiKit.MakeButton(row, text, color, new Vector2(330f, 44f), new Vector2(1f, 1f), new Vector2(-24f, -12f - index * 50f), onClick);
            button.transform.Find("Body/Text").GetComponent<Text>().fontSize = 24;
        }

        private void NewInSlot(int slot)
        {
            bool used = GameSession.Slots.Load(slot) != null;
            if (used && !(confirmSlot == slot && !confirmDelete))
            {
                // Starting over a saved life asks once more.
                confirmSlot = slot;
                confirmDelete = false;
                RefreshSlots();
                return;
            }

            ShowNewGame(slot);
        }

        // ---------------------------------------------------------------- the new game

        private GameObject newGame;
        private InputField nameBox;
        private Text nameQuestion;
        private InputField runBox;
        private Image manButton;
        private Image womanButton;
        private int newGameSlot;
        private bool newGameFemale;

        private void BuildNewGame()
        {
            newGame = UiKit.Overlay(root, "NewGame");
            var center = new Vector2(0.5f, 0.5f);
            var top = new Vector2(0.5f, 1f);
            var card = UiKit.Card(newGame.transform, "Card", UiKit.Paper, new Vector2(960f, 760f), center, Vector2.zero);

            var title = UiKit.Label(card, "Nueva vida", 60, UiKit.Ink, new Vector2(860f, 76f), top, new Vector2(0f, -26f), TextAnchor.MiddleCenter);
            title.font = UiKit.TitleFont;
            Divider(card, -112f, 800f);

            nameQuestion = UiKit.Label(card, NameQuestion, 32, UiKit.Ink, new Vector2(760f, 44f), top, new Vector2(0f, -132f));
            nameQuestion.name = "NameQuestion";
            nameBox = UiKit.TextBox(card, "NameBox", "Tu nombre", new Vector2(760f, 80f), top, new Vector2(0f, -180f), PlayerProfile.MaxNameLength);
            nameBox.onValueChanged.AddListener(_ => AskForName(false));

            float y = -280f;
            var man = UiKit.MakeButton(card, "Hombre", NotChosen, new Vector2(370f, 84f), top, new Vector2(-195f, y), () => ChooseGender(false));
            var woman = UiKit.MakeButton(card, "Mujer", NotChosen, new Vector2(370f, 84f), top, new Vector2(195f, y), () => ChooseGender(true));
            manButton = man.transform.Find("Body").GetComponent<Image>();
            womanButton = woman.transform.Find("Body").GetComponent<Image>();

            UiKit.Label(card, "¿Cómo se llama la partida?", 32, UiKit.Ink, new Vector2(760f, 44f), top, new Vector2(0f, -384f));
            runBox = UiKit.TextBox(card, "RunBox", "Si la dejás vacía, lleva tu nombre", new Vector2(760f, 80f), top, new Vector2(0f, -432f), PlayerProfile.MaxNameLength);

            var bottom = new Vector2(0.5f, 0f);
            UiKit.MakeButton(card, "Volver", GameUiColors.Pink, new Vector2(300f, 84f), bottom, new Vector2(-170f, 40f), BackToSlots);
            UiKit.MakeButton(card, "Empezar", GameUiColors.Green, new Vector2(300f, 84f), bottom, new Vector2(170f, 40f), ConfirmNewGame);
            newGame.SetActive(false);
        }

        private static readonly Color NotChosen = GamePalette.Hex("#D8DCE6");
        private const string NameQuestion = "¿Cómo te llamás?";
        private const string NameMissing = "Escribí tu nombre para empezar.";

        /// <summary>The line above the name box asks for the name again, in red, until something is typed.</summary>
        private void AskForName(bool missing)
        {
            nameQuestion.text = Loc.T(missing ? NameMissing : NameQuestion);
            nameQuestion.color = missing ? GameUiColors.Loss : UiKit.Ink;
        }

        private void ShowNewGame(int slot)
        {
            newGameSlot = slot;
            nameBox.text = string.Empty;
            runBox.text = string.Empty;
            AskForName(false);
            ChooseGender(false);
            slots.SetActive(false);
            newGame.SetActive(true);
            nameBox.ActivateInputField();
        }

        private void ChooseGender(bool female)
        {
            newGameFemale = female;
            manButton.color = female ? NotChosen : GameUiColors.Yellow;
            womanButton.color = female ? GameUiColors.Yellow : NotChosen;
        }

        private void BackToSlots()
        {
            newGame.SetActive(false);
            ShowSlots();
        }

        private void ConfirmNewGame()
        {
            if (!PlayerProfile.IsUsableName(nameBox.text))
            {
                AskForName(true);
                nameBox.ActivateInputField();
                return;
            }

            string name = PlayerProfile.CleanName(nameBox.text);
            string run = string.IsNullOrWhiteSpace(runBox.text) ? name : PlayerProfile.CleanName(runBox.text);
            StartStory(newGameSlot, run, name, newGameFemale);
        }

        private void DeleteSlot(int slot)
        {
            if (!(confirmSlot == slot && confirmDelete))
            {
                confirmSlot = slot;
                confirmDelete = true;
                RefreshSlots();
                return;
            }

            GameSession.Slots.Delete(slot);
            confirmSlot = -1;
            RefreshSlots();
        }

        private void ContinueSlot(int slot)
        {
            var session = GameSession.Create();
            if (!session.ContinueGame(slot))
            {
                RefreshSlots();
                return;
            }

            slots.SetActive(false);
            LoadingScreen.Load(SceneNames.Town);
        }
    }
}

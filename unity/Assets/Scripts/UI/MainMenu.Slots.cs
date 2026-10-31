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
            var card = UiKit.Card(slots.transform, "Card", UiKit.Paper, new Vector2(1260f, 850f), center, Vector2.zero);

            UiKit.Header(card, "Modo historia", Beak);
            UiKit.Label(card, "Elegí una partida guardada o empezá una vida nueva.", 30, GameUiColors.Muted, new Vector2(1100f, 44f), top, new Vector2(0f, -114f), TextAnchor.MiddleCenter);
            Divider(card, -162f, 1140f);

            slotRows = UiKit.Rect(card, "Rows", new Vector2(1200f, 580f), top, new Vector2(0f, -170f));
            UiKit.MakeButton(card, "Volver", GameUiColors.Pink, new Vector2(300f, 76f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), CloseWindows);
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
                    new Vector2(1200f, 174f), new Vector2(0.5f, 1f), new Vector2(0f, -slot * 190f));

                if (saved == null)
                {
                    UiKit.PlainLabel(row, "Label_SlotTitle", Loc.F("Espacio {0}", slot + 1), 36, UiKit.Ink, new Vector2(650f, 48f), topLeft, new Vector2(32f, -32f));
                    UiKit.PlainLabel(row, "Label_EmptyDesc", Loc.T("Vacío. Aquí empieza una vida nueva."), 26, GameUiColors.Muted, new Vector2(650f, 44f), topLeft, new Vector2(32f, -92f));
                    var newPartidaBtn = UiKit.MakeButton(row, "Nueva partida", GameUiColors.Green, new Vector2(370f, 72f), new Vector2(1f, 0.5f), new Vector2(-32f, 0f), () => NewInSlot(slot));
                    UiKit.SetFontSize(newPartidaBtn, 28);
                    continue;
                }

                string heading = saved.name + "  ·  " + Loc.F("Mes {0}", saved.Month) + "  ·  " + Loc.F("Semana {0}", saved.Week);
                if (saved.Finished)
                {
                    heading = "[TERMINADA]  " + saved.name + "  ·  " + Loc.T("Año terminado");
                }

                UiKit.PlainLabel(row, "Heading", heading, 32, UiKit.Ink, new Vector2(740f, 44f), topLeft, new Vector2(32f, -18f));
                string numbers = Loc.F("Efectivo {0}", Money.Format(saved.Cash)) + "    " + Loc.F("Ahorros {0}", Money.Format(saved.Savings))
                    + "    " + Loc.F("Ánimo {0}", saved.Wellbeing);
                UiKit.PlainLabel(row, "Numbers", numbers, 25, UiKit.Ink, new Vector2(740f, 38f), topLeft, new Vector2(32f, -68f));
                UiKit.PlainLabel(row, "LastSaved", Loc.F("Última vez: {0}", saved.SavedAtLabel), 23, GameUiColors.Muted, new Vector2(740f, 34f), topLeft, new Vector2(32f, -114f));

                bool asking = confirmSlot == slot;
                if (!saved.Finished)
                {
                    var contBtn = UiKit.MakeButton(row, "Continuar", GameUiColors.Green, new Vector2(370f, 62f), new Vector2(1f, 1f), new Vector2(-32f, -16f), () => ContinueSlot(slot));
                    UiKit.SetFontSize(contBtn, 28);
                }
                else
                {
                    var repBtn = UiKit.MakeButton(row, "Ver reporte final", GameUiColors.Blue, new Vector2(370f, 62f), new Vector2(1f, 1f), new Vector2(-32f, -16f), () => ViewFinishedReport(saved));
                    UiKit.SetFontSize(repBtn, 26);
                }

                string newLabel = asking && !confirmDelete ? "¿Seguro? Empezar" : "Nueva";
                string delLabel = asking && confirmDelete ? "¿Seguro? Borrar" : "Borrar";

                var newBtn = UiKit.MakeButton(row, newLabel, GameUiColors.Yellow, new Vector2(176f, 56f), new Vector2(1f, 1f), new Vector2(-226f, -92f), () => NewInSlot(slot));
                UiKit.SetFontSize(newBtn, 22);

                var delBtn = UiKit.MakeButton(row, delLabel, GameUiColors.Pink, new Vector2(176f, 56f), new Vector2(1f, 1f), new Vector2(-32f, -92f), () => DeleteSlot(slot));
                UiKit.SetFontSize(delBtn, 22);
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

            UiKit.Header(card, "Nueva vida", Beak);

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

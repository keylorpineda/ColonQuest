using System.Collections;
using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>
    /// The talks with the collector, the lender and the pyramid stand, without waiting for them to come up in a game. Next to
    /// the chat is what each one knows, written by the engine, and a line opens when the player asks about it. The same
    /// <see cref="DebtTalk"/> as in the game, so what is tried here is what is played.
    /// </summary>
    public sealed partial class DemoLab
    {
        private const int TalksStation = 5;

        /// <summary>What the collector comes for in the demo. In a game it depends on the player's debts.</summary>
        private const long SampleDemand = 80_000L;

        private static readonly TalkPartner[] Partners = { TalkPartner.Collectors, TalkPartner.Lender, TalkPartner.Pyramid };

        private static readonly Dictionary<string, string> TalkFactNames = new Dictionary<string, string>
        {
            { "total_cost", "Cuánto es" },
            { "rate", "Interés" },
            { "arrangement", "Arreglo" },
            { "what_if_not_paid", "Si no se paga" },
            { "where_money_comes", "De dónde sale la plata" },
            { "guarantee", "Contrato" },
            { "legal", "Lo legal" }
        };

        private ApiService talkService;
        private DebtTalk talk;
        private int talkPartner;
        private string pendingTalk;

        /// <summary>One turn in the player's words. Public so a test can talk without a keyboard.</summary>
        public void TryTalk(string line)
        {
            if (!Busy && !string.IsNullOrWhiteSpace(line))
            {
                StartCoroutine(SendTalkLine(line.Trim()));
            }
        }

        private void DrawTalks()
        {
            if (talk == null)
            {
                ChoosePartner(0, false);
            }

            recorder = recorder ?? VoiceRecorders.Create();
            DrawHeading("Cobros y trampas", "Muse Spark Open  +  Muse Voice Transcribe", "Hablales con voz o por escrito. Cada uno tiene su personalidad y solo dice lo incómodo si le preguntás. Las cifras no las inventa el modelo: las pone el motor del juego.");

            float y = -170f;
            float buttonWidth = (610f - 20f) / Partners.Length;
            for (int i = 0; i < Partners.Length; i++)
            {
                int index = i;
                Action(new DebtTalk(Partners[i]).Name, i == talkPartner ? Beak : GameUiColors.Blue, new Vector2(buttonWidth, 62f), new Vector2(Left + i * (buttonWidth + 10f), y), () => ChoosePartner(index, true));
            }

            y -= 72f;
            string name = Loc.T(talk.Name);
            string player = Loc.T("Tú:").TrimEnd(':');
            var lines = new List<ChatLine> { new ChatLine(name, Loc.T(talk.Opening), false) };
            foreach (var turn in talk.Turns)
            {
                lines.Add(new ChatLine(player, turn.Player, true));
                lines.Add(new ChatLine(name, Loc.T(turn.Reply), false));
            }

            if (!string.IsNullOrEmpty(pendingTalk))
            {
                lines.Add(new ChatLine(player, pendingTalk, true));
            }

            ChatLine? wait = Busy ? new ChatLine(name, Loc.T(chatWait), false) : (ChatLine?)null;
            chatWaitLabel = ChatView.Build(workspace, new Vector2(610f, 372f), TopLeft, new Vector2(Left, y), lines, wait);
            y -= 382f;

            var questions = talk.SuggestedQuestions;
            for (int i = 0; i < questions.Length; i++)
            {
                string question = Loc.T(questions[i]);
                Action(questions[i], Cream, new Vector2(300f, 56f), new Vector2(Left + i % 2 * 310f, y - i / 2 * 64f), () => TryTalk(question));
            }

            y -= 132f;
            DrawChatInput(y, true);
            y -= 88f;
            Action("Empezar otra conversación", Cream, new Vector2(610f, 50f), new Vector2(Left, y), () => ChoosePartner(talkPartner, true));
            DrawTalkSheet();
        }

        /// <summary>What the character knows, from the same sheet the model gets. A line opens when the player asks about it.</summary>
        private void DrawTalkSheet()
        {
            float y = -170f;
            Note("La ficha que le dio el juego", 25, UiKit.Ink, new Vector2(SideWidth, 34f), new Vector2(SideX, y));
            y -= 38f;
            foreach (var line in talk.ToRequest(string.Empty, ApiService.Language()).sheet)
            {
                WireEnum<TalkFact>.TryParse(line.fact, out var fact);
                bool open = ContainsFact(fact);
                var row = UiKit.Card(workspace, "Fact_" + line.fact, open ? GameUiColors.Green : Locked, new Vector2(SideWidth, 104f), TopLeft, new Vector2(SideX, y));
                UiKit.Label(row, TalkFactNames[line.fact], 18, GameUiColors.Muted, new Vector2(SideWidth - 32f, 24f), new Vector2(0.5f, 1f), new Vector2(0f, -6f));
                var text = UiKit.PlainLabel(row, "Value", open ? line.text : "? ? ?", 17, UiKit.Ink, new Vector2(SideWidth - 32f, 64f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperLeft);
                text.fontStyle = FontStyle.Normal;
                y -= 108f;
            }
        }

        private bool ContainsFact(TalkFact fact)
        {
            foreach (var asked in talk.Asked)
            {
                if (asked == fact)
                {
                    return true;
                }
            }

            return false;
        }

        private void ChoosePartner(int index, bool redraw)
        {
            talkPartner = index;
            talk = new DebtTalk(Partners[index], Partners[index] == TalkPartner.Collectors ? SampleDemand : 0L);

            // Each conversation is a new session, so a long tour never runs into the backend's limit per session.
            talkService = NewService();
            negotiationNotice = null;
            lastOriginNote = string.Empty;
            if (redraw)
            {
                Redraw();
            }
        }

        private IEnumerator SendTalkLine(string line)
        {
            var current = talk;
            pendingTalk = line;
            negotiationNotice = null;
            chatWait = "Está pensando qué decirte";
            BeginWait(chatWait);
            Redraw();

            bool connected = false;
            yield return EnsureSession(talkService, null, ok => connected = ok);
            ApiResult<TalkReplyDto> result = null;
            if (connected)
            {
                yield return talkService.SendTalk(current.ToRequest(line, ApiService.Language()), r => result = r);
            }

            pendingTalk = null;
            EndWait();
            if (result != null && result.Ok)
            {
                current.Record(line, result.Value.npcReply, WireEnum<TalkFact>.ParseKnown(result.Value.askedFacts));
                lastOriginNote = Tint(DemoReadout.OriginOf("model"), "#A8F0B8");
            }
            else
            {
                // The same rules that answer in a game without a backend, and the panel says they did.
                current.Say(line);
                lastOriginNote = Tint(DemoReadout.OriginOf("rules"), "#FF8A8A");
            }

            Redraw();
            yield return RefreshEvidence();
        }

        private IEnumerator SendTalkVoice(byte[] wav)
        {
            chatWait = "Muse Voice Transcribe está escuchando";
            BeginWait(chatWait);
            Redraw();

            bool connected = false;
            yield return EnsureSession(talkService, null, ok => connected = ok);
            ApiResult<string> result = null;
            if (connected)
            {
                yield return talkService.Transcribe(wav, r => result = r);
            }

            EndWait();
            if (result == null || !result.Ok)
            {
                negotiationNotice = "No te entendí, probá otra vez o escribilo.";
                Redraw();
                yield break;
            }

            yield return SendTalkLine(result.Value);
        }
    }
}

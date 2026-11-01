using System.Collections;
using System.Collections.Generic;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.World;
using UnityEngine;
using UnityEngine.UI;

namespace ColonQuest.UI
{
    /// <summary>
    /// The negotiation station: talk or type to a seller who hides the insurance and the commission until asked. Next to the
    /// chat is the hidden sheet with the true figures, calculated by the engine; a line opens when the seller gives it away.
    /// </summary>
    public sealed partial class DemoLab
    {
        private const float SideX = 656f;
        private const float SideWidth = 344f;

        private static readonly string[] OfferNames = { "Celular", "Pantalla", "Refri", "Moto" };

        private static readonly string[] Questions =
        {
            "¿Cuánto pago en total?", "¿Tiene cargos escondidos?", "¿Cuál es la tasa de interés?", "¿Cuánto cuesta de contado?"
        };

        private static readonly string[] FactNames =
        {
            "Precio de contado", "Cuota mensual", "Plazo", "Tasa de interés", "Seguro", "Comisión", "Costo total"
        };

        private static readonly string[] QuestionNames =
        {
            "Costo total", "Cargos escondidos", "Tasa de interés", "Plazo", "Precio de contado", "Seguro"
        };

        private ApiService negotiationService;
        private readonly List<ChatLine> chat = new List<ChatLine>();
        private readonly HashSet<SellerFact> revealed = new HashSet<SellerFact>();
        private readonly HashSet<KeyQuestion> asked = new HashSet<KeyQuestion>();

        private IVoiceRecorder recorder;
        private VoiceButton voiceButton;
        private InputField chatInput;
        private SellerOffer offer;
        private string pendingLine;
        private string chatWait;
        private Text chatWaitLabel;
        private string negotiationNotice;
        private DealStatus dealStatus = DealStatus.Open;

        private void DrawNegotiation()
        {
            if (offer == null)
            {
                ChooseOffer(0, false);
            }

            recorder = recorder ?? VoiceRecorders.Create();
            DrawHeading("Negociación", "Muse Spark Open  +  Muse Voice Transcribe", "Preguntá como quieras, con voz o por escrito. El vendedor no puede mentir en las cifras, pero se calla lo que le conviene hasta que le preguntás. Tratá de sacarle los cargos escondidos.");

            float y = -170f;
            float buttonWidth = (610f - 30f) / 4f;
            for (int i = 0; i < SellerOffer.Catalog.Count; i++)
            {
                int index = i;
                bool chosen = SellerOffer.Catalog[i] == offer;
                Action(OfferNames[i], chosen ? Beak : GameUiColors.Blue, new Vector2(buttonWidth, 62f), new Vector2(Left + i * (buttonWidth + 10f), y), () => ChooseOffer(index, true));
            }

            y -= 72f;
            var lines = new List<ChatLine>(chat);
            if (!string.IsNullOrEmpty(pendingLine))
            {
                lines.Add(new ChatLine(Loc.T("Tú:").TrimEnd(':'), pendingLine, true));
            }

            ChatLine? wait = Busy ? new ChatLine(offer.SellerName, Loc.T(chatWait), false) : (ChatLine?)null;
            chatWaitLabel = ChatView.Build(workspace, new Vector2(610f, 372f), TopLeft, new Vector2(Left, y), lines, wait);
            y -= 382f;

            for (int i = 0; i < Questions.Length; i++)
            {
                string question = Loc.T(Questions[i]);
                Action(Questions[i], Cream, new Vector2(300f, 56f), new Vector2(Left + i % 2 * 310f, y - i / 2 * 64f), () => Ask(question), dealStatus == DealStatus.Open);
            }

            y -= 132f;
            DrawChatInput(y);
            y -= 88f;
            Action("Empezar otra conversación", Cream, new Vector2(610f, 50f), new Vector2(Left, y), () => ChooseOffer(SellerOfferIndex(), true));
            DrawSheet();
        }

        private void DrawChatInput(float y)
        {
            bool voice = recorder.IsSupported;
            bool free = !Busy && dealStatus == DealStatus.Open;
            float talkWidth = voice ? 170f : 0f;
            float sendWidth = 120f;
            float boxWidth = 610f - sendWidth - 10f - (voice ? talkWidth + 10f : 0f);

            chatInput = UiKit.TextBox(workspace, "Line", negotiationNotice ?? "Escribí aquí...", new Vector2(boxWidth, 76f), TopLeft, new Vector2(Left, y), 300);
            chatInput.interactable = free;
            Action("Enviar", GameUiColors.Blue, new Vector2(sendWidth, 76f), new Vector2(Left + boxWidth + 10f, y), SubmitLine, free);
            if (voice)
            {
                var talk = Action("Hablar", GameUiColors.Yellow, new Vector2(talkWidth, 76f), new Vector2(Left + boxWidth + sendWidth + 20f, y), ToggleVoice, free);
                voiceButton = new VoiceButton(talk, recorder, talkWidth);
            }
        }

        private void DrawSheet()
        {
            float y = -170f;
            Note("Ficha oculta del vendedor", 25, UiKit.Ink, new Vector2(SideWidth, 34f), new Vector2(SideX, y));
            y -= 38f;
            for (int i = 0; i < FactNames.Length; i++)
            {
                var fact = (SellerFact)i;
                bool open = revealed.Contains(fact);
                var row = UiKit.Card(workspace, "Fact_" + fact, open ? GameUiColors.Green : Locked, new Vector2(SideWidth, 52f), TopLeft, new Vector2(SideX, y));
                UiKit.Label(row, FactNames[i], 19, GameUiColors.Muted, new Vector2(170f, 40f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), TextAnchor.MiddleLeft);
                UiKit.PlainLabel(row, "Value", open ? offer.Describe(fact) : "? ? ?", 21, UiKit.Ink, new Vector2(146f, 40f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), TextAnchor.MiddleRight);
                y -= 56f;
            }

            y -= 6f;
            Note("Preguntas clave que hiciste", 25, UiKit.Ink, new Vector2(SideWidth, 34f), new Vector2(SideX, y));
            y -= 38f;
            for (int i = 0; i < QuestionNames.Length; i++)
            {
                bool done = asked.Contains((KeyQuestion)i);
                var chip = UiKit.Card(workspace, "Key_" + i, done ? GameUiColors.Green : Locked, new Vector2(168f, 46f), TopLeft, new Vector2(SideX + i % 2 * 176f, y - i / 2 * 50f));
                UiKit.Label(chip, QuestionNames[i], 18, done ? UiKit.Ink : GameUiColors.Muted, new Vector2(150f, 36f), Center, Vector2.zero, TextAnchor.MiddleCenter);
            }

            y -= 154f;
            Note(dealStatus == DealStatus.Open ? "Las cifras las calcula el motor, no el modelo. Y estas preguntas son la métrica de impacto del juego." : "El vendedor dio el trato por cerrado. Empezá otra conversación para seguir probando.", 20, GameUiColors.Muted, new Vector2(SideWidth, 100f), new Vector2(SideX, y));
        }

        private int SellerOfferIndex()
        {
            for (int i = 0; i < SellerOffer.Catalog.Count; i++)
            {
                if (SellerOffer.Catalog[i] == offer)
                {
                    return i;
                }
            }

            return 0;
        }

        private void ChooseOffer(int index, bool redraw)
        {
            offer = SellerOffer.Catalog[index];

            // A new conversation is a new session, so a long tour never runs into the backend's limit per session.
            negotiationService = NewService();
            chat.Clear();
            revealed.Clear();
            asked.Clear();
            dealStatus = DealStatus.Open;
            negotiationNotice = null;
            chat.Add(new ChatLine(offer.SellerName, LocalSeller.Greeting(offer), false));
            if (redraw)
            {
                Redraw();
            }
        }

        private void Ask(string line)
        {
            if (!Busy && dealStatus == DealStatus.Open)
            {
                StartCoroutine(SendLine(line));
            }
        }

        private void SubmitLine()
        {
            if (chatInput != null && !string.IsNullOrWhiteSpace(chatInput.text))
            {
                Ask(chatInput.text.Trim());
            }
        }

        private IEnumerator SendLine(string line)
        {
            pendingLine = line;
            negotiationNotice = null;
            chatWait = "Está pensando qué decirte";
            BeginWait(chatWait);
            Redraw();

            bool connected = false;
            yield return EnsureSession(negotiationService, null, ok => connected = ok);
            ApiResult<NegotiationReply> result = null;
            if (connected)
            {
                yield return negotiationService.SendNegotiationText(offer.OfferId, line, r => result = r);
            }

            pendingLine = null;
            EndWait();
            if (result == null || !result.Ok)
            {
                negotiationNotice = NoServer;
                Redraw();
                yield break;
            }

            string player = Loc.T("Tú:").TrimEnd(':');
            chat.Add(new ChatLine(player, line, true));
            chat.Add(new ChatLine(offer.SellerName, result.Value.NpcReply, false));
            revealed.UnionWith(result.Value.RevealedFacts);
            asked.UnionWith(result.Value.KeyQuestionsAsked);
            dealStatus = result.Value.DealStatus;
            lastOriginNote = Tint(DemoReadout.OriginOf(result.Origin), DemoReadout.WrittenByAModel(result.Origin) ? "#A8F0B8" : "#FF8A8A");
            Redraw();
            yield return RefreshEvidence();
        }

        private void ToggleVoice()
        {
            if (Busy)
            {
                return;
            }

            switch (recorder.State)
            {
                case VoiceRecorderState.Recording:
                    recorder.Stop();
                    break;
                case VoiceRecorderState.Starting:
                    recorder.Cancel();
                    break;
                default:
                    negotiationNotice = null;
                    recorder.Start();
                    break;
            }

            voiceButton?.Refresh();
        }

        private IEnumerator SendVoice(byte[] wav)
        {
            chatWait = "Muse Voice Transcribe está escuchando";
            BeginWait(chatWait);
            Redraw();

            bool connected = false;
            yield return EnsureSession(negotiationService, null, ok => connected = ok);
            ApiResult<string> result = null;
            if (connected)
            {
                yield return negotiationService.Transcribe(wav, r => result = r);
            }

            EndWait();
            if (result == null || !result.Ok)
            {
                negotiationNotice = "No te entendí, probá otra vez o escribilo.";
                Redraw();
                yield break;
            }

            yield return SendLine(result.Value);
        }

        /// <summary>Follows the recording and the dots of the wait, every frame the station is open.</summary>
        private void TickNegotiation()
        {
            if (Current != 1 || recorder == null)
            {
                return;
            }

            if (chatWaitLabel != null)
            {
                ChatView.AnimateWait(chatWaitLabel, Loc.T(chatWait));
            }

            if (chatInput != null && chatInput.isFocused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                SubmitLine();
            }

            if (recorder.State == VoiceRecorderState.Idle)
            {
                return;
            }

            recorder.Tick();
            if (recorder.State == VoiceRecorderState.Ready)
            {
                byte[] wav = recorder.TakeWav();
                if (wav != null && wav.Length > 0)
                {
                    StartCoroutine(SendVoice(wav));
                }
            }
            else if (recorder.State == VoiceRecorderState.Failed)
            {
                recorder.TakeWav();
                negotiationNotice = "No pude usar el micrófono, escribilo.";
                Redraw();
            }

            voiceButton?.Refresh();
        }

        private void ClearNegotiationInput()
        {
            chatInput = null;
            voiceButton = null;
            chatWaitLabel = null;
        }
    }
}

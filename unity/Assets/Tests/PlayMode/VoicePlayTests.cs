using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ColonQuest.Api;
using ColonQuest.Game;
using ColonQuest.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColonQuest.World.PlayModeTests
{
    /// <summary>A backend that opens sessions but whose voice and seller are down (503).</summary>
    internal sealed class VoiceDownTransport : IHttpTransport
    {
        public readonly List<HttpRequestData> Requests = new List<HttpRequestData>();

        public IEnumerator Send(HttpRequestData request, Action<HttpResponseData> done)
        {
            Requests.Add(request);
            if (request.Url.EndsWith("/sessions", StringComparison.Ordinal))
            {
                done(new HttpResponseData { StatusCode = 201, Text = "{\"sessionId\":\"11111111-2222-3333-4444-555555555555\"}" });
            }
            else
            {
                done(new HttpResponseData { StatusCode = 503, Text = "{\"detail\":\"voice is unavailable right now, use text input\"}" });
            }

            yield break;
        }
    }

    /// <summary>A microphone that records instantly and hands back a fixed clip, or fails on demand.</summary>
    internal sealed class FakeVoiceRecorder : IVoiceRecorder
    {
        public bool FailOnStart;
        public int Taken;

        public bool IsSupported => true;

        public VoiceRecorderState State { get; private set; }

        public float Seconds => State == VoiceRecorderState.Recording ? 3f : 0f;

        public void Start() => State = FailOnStart ? VoiceRecorderState.Failed : VoiceRecorderState.Recording;

        public void Stop() => State = VoiceRecorderState.Ready;

        public void Cancel() => State = VoiceRecorderState.Idle;

        public void Tick()
        {
        }

        public byte[] TakeWav()
        {
            bool ready = State == VoiceRecorderState.Ready;
            State = VoiceRecorderState.Idle;
            if (!ready)
            {
                return null;
            }

            Taken++;
            return WavEncoder.FromFloats(new float[1600], 1, 16000);
        }
    }

    /// <summary>Voice in the seller's window: when the backend cannot listen, the text box takes over.</summary>
    public sealed class VoicePlayTests : PlayTestBase
    {
        protected override string SceneToLoad => SceneNames.Town;

        private static GameSession Session => GameSession.Instance;

        private static Transform Ui => GameObject.Find("GameUi").transform;

        private static byte[] Clip => WavEncoder.FromFloats(new float[1600], 1, 16000);

        private static IEnumerator StartRun(IHttpTransport transport)
        {
            ApiClient.Instance.Configure(transport, new ApiConfig());
            Session.StartNewGame();
            PlayerController.Blocked = false;
            yield return null;
        }

        private static InputField TextBox(Transform panelParent)
        {
            return panelParent.Find("Negotiation").GetComponentInChildren<InputField>();
        }

        [UnityTest]
        public IEnumerator Voice_OfflineAsksToTypeAndKeepsTheTextBox()
        {
            yield return StartRun(new OfflineTransport());
            Session.BeginNegotiation();
            yield return null;

            Session.SendNegotiationVoice(Clip);
            yield return null;

            Assert.AreEqual(GameSession.VoiceFailedNotice, Session.VoiceNotice);
            Assert.IsFalse(Session.Busy);
            Assert.AreEqual(0, Session.CurrentNegotiation.Turns.Count, "nothing was heard, so nothing is recorded");
            var box = TextBox(Ui);
            Assert.IsNotNull(box, "the text box is still there");
            Assert.AreEqual(GameSession.VoiceFailedNotice, ((Text)box.placeholder).text);
        }

        [UnityTest]
        public IEnumerator Voice_BackendDownFallsBackToTextAndTheTextStillWorks()
        {
            var transport = new VoiceDownTransport();
            yield return StartRun(transport);
            Assert.IsTrue(ApiClient.Instance.Service.HasSession);

            Session.BeginNegotiation();
            Session.SendNegotiationVoice(Clip);
            yield return new WaitForSeconds(0.2f);

            var voiceCall = transport.Requests.Find(r => r.Url.EndsWith("/negotiation/voice", StringComparison.Ordinal));
            Assert.IsNotNull(voiceCall, "the clip went to the backend");
            StringAssert.StartsWith("multipart/form-data", voiceCall.ContentType);
            StringAssert.Contains("name=\"offerId\"", Encoding.UTF8.GetString(voiceCall.RawBody));
            Assert.AreEqual(GameSession.VoiceFailedNotice, Session.VoiceNotice);
            Assert.AreEqual(0, Session.CurrentNegotiation.Turns.Count);

            Session.SendNegotiation("Cuanto pago en total?");
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(1, Session.CurrentNegotiation.Turns.Count, "the typed turn is answered");
            Assert.IsTrue(Session.CurrentNegotiation.Turns[0].FromLocalSeller);
            Assert.IsNull(Session.VoiceNotice, "the notice goes away once the player types");
        }

        [UnityTest]
        public IEnumerator TalkButton_RecordsCountsAndSendsTheClip()
        {
            yield return StartRun(new OfflineTransport());
            var holder = new GameObject("VoiceTest", typeof(RectTransform)).transform;
            holder.SetParent(Ui, false);
            var recorder = new FakeVoiceRecorder();
            var panel = new NegotiationPanel(holder, Session, recorder);

            Session.BeginNegotiation();
            panel.Refresh();
            var talk = holder.Find("Negotiation/Card/Body/Button_Hablar").GetComponent<Button>();
            var label = talk.GetComponentInChildren<Text>();
            Assert.AreEqual("Hablar", label.text);

            talk.onClick.Invoke();
            panel.Tick();
            Assert.AreEqual(VoiceRecorderState.Recording, recorder.State);
            StringAssert.Contains("3/15", label.text, "the counter shows the seconds");

            talk.onClick.Invoke();
            panel.Tick();
            yield return null;

            Assert.AreEqual(1, recorder.Taken, "the clip was handed to the session");
            Assert.AreEqual(GameSession.VoiceFailedNotice, Session.VoiceNotice, "offline there is no transcription");
            panel.Refresh();
            yield return null; // the old window is destroyed at the end of the frame
            Assert.AreEqual(GameSession.VoiceFailedNotice, ((Text)TextBox(holder).placeholder).text);

            panel.Hide();
            UnityEngine.Object.Destroy(holder.gameObject);
        }

        [UnityTest]
        public IEnumerator TalkButton_MicrophoneProblemAsksToType()
        {
            yield return StartRun(new OfflineTransport());
            var holder = new GameObject("VoiceTest", typeof(RectTransform)).transform;
            holder.SetParent(Ui, false);
            var recorder = new FakeVoiceRecorder { FailOnStart = true };
            var panel = new NegotiationPanel(holder, Session, recorder);

            Session.BeginNegotiation();
            panel.Refresh();
            holder.Find("Negotiation/Card/Body/Button_Hablar").GetComponent<Button>().onClick.Invoke();
            panel.Tick();
            yield return null;

            Assert.AreEqual(VoiceRecorderState.Idle, recorder.State);
            Assert.AreEqual(0, recorder.Taken);
            StringAssert.Contains("escribilo", ((Text)TextBox(holder).placeholder).text);

            panel.Hide();
            UnityEngine.Object.Destroy(holder.gameObject);
        }
    }
}

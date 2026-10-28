using System.Collections;
using System.Text;
using NUnit.Framework;

namespace ColonQuest.Api.Tests
{
    [TestFixture]
    public sealed class MultipartFormTests
    {
        [Test]
        public void ContentType_CarriesTheBoundary()
        {
            var form = new MultipartForm("XyZ");

            Assert.AreEqual("multipart/form-data; boundary=XyZ", form.ContentType);
        }

        [Test]
        public void Body_HasEachPartAndTheClosingBoundary()
        {
            byte[] file = { 0, 1, 2, 255, 13, 10 };
            byte[] body = new MultipartForm("B0")
                .AddField("offerId", "celular_basico")
                .AddFile("audio", "voice.wav", "audio/wav", file)
                .ToBytes();

            string expectedHead =
                "--B0\r\n" +
                "Content-Disposition: form-data; name=\"offerId\"\r\n\r\n" +
                "celular_basico\r\n" +
                "--B0\r\n" +
                "Content-Disposition: form-data; name=\"audio\"; filename=\"voice.wav\"\r\n" +
                "Content-Type: audio/wav\r\n\r\n";
            byte[] head = Encoding.UTF8.GetBytes(expectedHead);
            byte[] tail = Encoding.UTF8.GetBytes("\r\n--B0--\r\n");

            Assert.AreEqual(head.Length + file.Length + tail.Length, body.Length);
            CollectionAssert.AreEqual(head, Slice(body, 0, head.Length));
            CollectionAssert.AreEqual(file, Slice(body, head.Length, file.Length), "binary data travels untouched");
            CollectionAssert.AreEqual(tail, Slice(body, head.Length + file.Length, tail.Length));
        }

        [Test]
        public void Names_CannotBreakTheHeader()
        {
            string body = Encoding.UTF8.GetString(new MultipartForm("B").AddField("a\"\r\nb", "v").ToBytes());

            StringAssert.Contains("name=\"a%22b\"", body);
        }

        [Test]
        public void DefaultBoundary_IsUnique()
        {
            Assert.AreNotEqual(new MultipartForm().Boundary, new MultipartForm().Boundary);
        }

        private static byte[] Slice(byte[] source, int start, int length)
        {
            var part = new byte[length];
            System.Array.Copy(source, start, part, 0, length);
            return part;
        }
    }

    [TestFixture]
    public sealed class NegotiationVoiceTests
    {
        private const string SessionId = "11111111-2222-3333-4444-555555555555";
        private static readonly byte[] Wav = { 82, 73, 70, 70, 1, 2, 3 };

        private FakeTransport transport;
        private ApiService service;

        [SetUp]
        public void SetUp()
        {
            transport = new FakeTransport();
            service = new ApiService(transport, new ApiConfig { BaseUrl = "http://test/api" });
        }

        private static void Run(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested)
                {
                    Run(nested);
                }
            }
        }

        private void StartSession()
        {
            transport.Replies.Enqueue(FakeTransport.Ok("{\"sessionId\":\"" + SessionId + "\"}"));
            Run(service.CreateSession(null, _ => { }));
        }

        private ApiResult<NegotiationReply> Send(byte[] wav)
        {
            ApiResult<NegotiationReply> result = null;
            Run(service.SendNegotiationVoice("celular_basico", wav, r => result = r));
            return result;
        }

        [Test]
        public void Voice_PostsAMultipartClipToTheSession()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(
                "{\"transcript\":\"Cuanto pago en total?\",\"npcReply\":\"Mae, son 31 500.\",\"revealedFacts\":[\"total_cost\"],\"keyQuestionsAsked\":[\"total_cost\"],\"dealStatus\":\"open\"}"));

            var result = Send(Wav);

            Assert.IsTrue(result.Ok, result.Error);
            Assert.AreEqual("Cuanto pago en total?", result.Value.Transcript);
            var request = transport.Requests[1];
            Assert.AreEqual("POST", request.Method);
            Assert.AreEqual("http://test/api/sessions/" + SessionId + "/negotiation/voice", request.Url);
            StringAssert.StartsWith("multipart/form-data; boundary=", request.ContentType);
            Assert.IsNull(request.JsonBody);
            string body = Encoding.UTF8.GetString(request.RawBody);
            StringAssert.Contains("name=\"offerId\"\r\n\r\ncelular_basico\r\n", body);
            StringAssert.Contains("name=\"audio\"; filename=\"voice.wav\"", body);
            Assert.AreEqual(ApiConfig.VoiceTimeoutSeconds, request.TimeoutSeconds);
        }

        [TestCase(503)]
        [TestCase(422)]
        [TestCase(500)]
        public void Voice_HttpErrorsAreFailures(int status)
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Status(status));

            var result = Send(Wav);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(status, result.StatusCode);
        }

        [Test]
        public void Voice_NetworkErrorIsAFailure()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Timeout());

            Assert.IsFalse(Send(Wav).Ok);
        }

        [Test]
        public void Voice_EmptyTranscriptIsAFailure()
        {
            StartSession();
            transport.Replies.Enqueue(FakeTransport.Ok(
                "{\"transcript\":\"\",\"npcReply\":\"Diay?\",\"revealedFacts\":[],\"keyQuestionsAsked\":[],\"dealStatus\":\"open\"}"));

            Assert.IsFalse(Send(Wav).Ok);
        }

        [Test]
        public void Voice_WithoutSessionOrAudioNeverCallsTheBackend()
        {
            Assert.IsFalse(Send(Wav).Ok, "no session");
            StartSession();
            Assert.IsFalse(Send(new byte[0]).Ok, "no audio");
            Assert.IsFalse(Send(null).Ok, "null audio");

            Assert.AreEqual(1, transport.Requests.Count, "only the session was created");
        }
    }
}

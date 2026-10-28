#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace ColonQuest.Game
{
    /// <summary>
    /// Browser recording through Plugins/WebGL/VoiceRecorder.jslib (getUserMedia). The browser builds
    /// the 16-bit WAV itself; this class only polls its state and copies the clip into C#.
    /// </summary>
    public sealed class WebGlVoiceRecorder : IVoiceRecorder
    {
        private const int JsIdle = 0;
        private const int JsStarting = 1;
        private const int JsRecording = 2;
        private const int JsReady = 3;

        [DllImport("__Internal")] private static extern int VoiceRecorder_IsSupported();
        [DllImport("__Internal")] private static extern void VoiceRecorder_Start(int maxSeconds);
        [DllImport("__Internal")] private static extern void VoiceRecorder_Stop();
        [DllImport("__Internal")] private static extern void VoiceRecorder_Reset();
        [DllImport("__Internal")] private static extern int VoiceRecorder_State();
        [DllImport("__Internal")] private static extern float VoiceRecorder_Seconds();
        [DllImport("__Internal")] private static extern int VoiceRecorder_ResultLength();
        [DllImport("__Internal")] private static extern void VoiceRecorder_CopyResult(byte[] buffer, int length);

        public bool IsSupported => VoiceRecorder_IsSupported() == 1;

        public VoiceRecorderState State { get; private set; } = VoiceRecorderState.Idle;

        public float Seconds => State == VoiceRecorderState.Recording ? VoiceRecorder_Seconds() : 0f;

        public void Start()
        {
            if (State == VoiceRecorderState.Starting || State == VoiceRecorderState.Recording)
            {
                return;
            }

            VoiceRecorder_Start(WavEncoder.MaxSeconds);
            Tick();
        }

        public void Stop()
        {
            VoiceRecorder_Stop();
            Tick();
        }

        public void Cancel()
        {
            VoiceRecorder_Reset();
            State = VoiceRecorderState.Idle;
        }

        public void Tick()
        {
            switch (VoiceRecorder_State())
            {
                case JsIdle:
                    State = VoiceRecorderState.Idle;
                    break;
                case JsStarting:
                    State = VoiceRecorderState.Starting;
                    break;
                case JsRecording:
                    State = VoiceRecorderState.Recording;
                    break;
                case JsReady:
                    State = VoiceRecorderState.Ready;
                    break;
                default:
                    State = VoiceRecorderState.Failed;
                    break;
            }
        }

        public byte[] TakeWav()
        {
            Tick();
            byte[] wav = null;
            if (State == VoiceRecorderState.Ready)
            {
                int length = VoiceRecorder_ResultLength();
                if (length > 0)
                {
                    wav = new byte[length];
                    VoiceRecorder_CopyResult(wav, length);
                }
            }

            if (State == VoiceRecorderState.Ready || State == VoiceRecorderState.Failed)
            {
                VoiceRecorder_Reset();
                State = VoiceRecorderState.Idle;
            }

            return wav;
        }
    }
}
#endif

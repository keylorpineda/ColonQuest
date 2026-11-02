#if !UNITY_WEBGL || UNITY_EDITOR
using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>Desktop and editor recording with Unity's <see cref="Microphone"/>, which does not work in WebGL.</summary>
    public sealed class MicrophoneRecorder : IVoiceRecorder
    {
        private const int SampleRate = 16000;

        // The loudness is read over the last tenth of a second.
        private const int WindowSamples = SampleRate / 10;
        private const float MeterGain = 10f;

        // A microphone that never starts delivering is a failure, not a long silence.
        private const float MostSecondsToStart = 2f;

        private readonly float[] window = new float[WindowSamples];
        private AudioClip clip;

        // Null is the microphone chosen in Windows, which is not always the first one of the list.
        private readonly string device = null;
        private float started;
        private float lastTick;
        private float loudness;
        private SilenceGate gate;
        private byte[] wav;

        public bool IsSupported => Microphone.devices.Length > 0;

        public VoiceRecorderState State { get; private set; } = VoiceRecorderState.Idle;

        public float Seconds => State == VoiceRecorderState.Recording ? Mathf.Min(Time.realtimeSinceStartup - started, WavEncoder.MaxSeconds) : 0f;

        public float Level => State == VoiceRecorderState.Recording ? Mathf.Clamp01(loudness * MeterGain) : 0f;

        public bool StopsOnSilence => true;

        public void Start()
        {
            if (State == VoiceRecorderState.Recording || State == VoiceRecorderState.Starting || !IsSupported)
            {
                if (!IsSupported)
                {
                    State = VoiceRecorderState.Failed;
                }

                return;
            }

            wav = null;
            clip = Microphone.Start(device, false, WavEncoder.MaxSeconds, SampleRate);
            if (clip == null)
            {
                State = VoiceRecorderState.Failed;
                return;
            }

            started = Time.realtimeSinceStartup;
            lastTick = started;
            loudness = 0f;
            gate = new SilenceGate();
            State = VoiceRecorderState.Starting;
        }

        public void Stop()
        {
            if (State != VoiceRecorderState.Recording)
            {
                return;
            }

            // Without looping the position returns to 0 once the clip is full, so 0 means "all of it".
            bool recording = Microphone.IsRecording(device);
            int frames = recording ? Microphone.GetPosition(device) : clip.samples;
            Microphone.End(device);

            if (frames <= 0)
            {
                Release();
                State = VoiceRecorderState.Failed;
                return;
            }

            var samples = new float[frames * clip.channels];
            clip.GetData(samples, 0);
            wav = WavEncoder.FromFloats(clip.channels == 1 ? SpeechClip.Prepare(samples, clip.frequency) : samples, clip.channels, clip.frequency);
            Release();
            State = VoiceRecorderState.Ready;
        }

        public void Cancel()
        {
            if (State == VoiceRecorderState.Recording || State == VoiceRecorderState.Starting)
            {
                Microphone.End(device);
            }

            Release();
            wav = null;
            State = VoiceRecorderState.Idle;
        }

        public void Tick()
        {
            if (State == VoiceRecorderState.Starting)
            {
                TickStarting();
                return;
            }

            if (State != VoiceRecorderState.Recording)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            float delta = now - lastTick;
            lastTick = now;
            if (!Microphone.IsRecording(device) || now - started >= WavEncoder.MaxSeconds || HeardTheEnd(delta))
            {
                Stop();
            }
        }

        /// <summary>A microphone takes a moment to wake up: "listening" and its sound wait for it, so no first word is lost.</summary>
        private void TickStarting()
        {
            float now = Time.realtimeSinceStartup;
            if (Microphone.GetPosition(device) > 0)
            {
                started = now;
                lastTick = now;
                State = VoiceRecorderState.Recording;
                ColonQuest.World.AudioDirector.Effect(ColonQuest.World.SoundEffectKind.Chime);
            }
            else if (now - started > MostSecondsToStart)
            {
                Microphone.End(device);
                Release();
                State = VoiceRecorderState.Failed;
            }
        }

        /// <summary>Measures the last moment of the clip and asks the gate whether the player finished.</summary>
        private bool HeardTheEnd(float delta)
        {
            int position = Microphone.GetPosition(device);
            if (position < WindowSamples || clip.channels != 1)
            {
                return false;
            }

            clip.GetData(window, position - WindowSamples);
            float sum = 0f;
            for (int i = 0; i < window.Length; i++)
            {
                sum += window[i] * window[i];
            }

            loudness = Mathf.Sqrt(sum / window.Length);
            return gate.Feed(loudness, delta);
        }

        public byte[] TakeWav()
        {
            var result = wav;
            wav = null;
            if (State == VoiceRecorderState.Ready || State == VoiceRecorderState.Failed)
            {
                State = VoiceRecorderState.Idle;
            }

            return result;
        }

        private void Release()
        {
            if (clip != null)
            {
                Object.Destroy(clip);
                clip = null;
            }
        }
    }
}
#endif

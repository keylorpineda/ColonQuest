#if !UNITY_WEBGL || UNITY_EDITOR
using UnityEngine;

namespace ColonQuest.Game
{
    /// <summary>Desktop and editor recording with Unity's <see cref="Microphone"/>, which does not work in WebGL.</summary>
    public sealed class MicrophoneRecorder : IVoiceRecorder
    {
        private const int SampleRate = 16000;

        private AudioClip clip;
        private string device;
        private float started;
        private byte[] wav;

        public bool IsSupported => Microphone.devices.Length > 0;

        public VoiceRecorderState State { get; private set; } = VoiceRecorderState.Idle;

        public float Seconds => State == VoiceRecorderState.Recording ? Mathf.Min(Time.realtimeSinceStartup - started, WavEncoder.MaxSeconds) : 0f;

        public void Start()
        {
            if (State == VoiceRecorderState.Recording || !IsSupported)
            {
                if (!IsSupported)
                {
                    State = VoiceRecorderState.Failed;
                }

                return;
            }

            wav = null;
            device = Microphone.devices[0];
            clip = Microphone.Start(device, false, WavEncoder.MaxSeconds, SampleRate);
            if (clip == null)
            {
                State = VoiceRecorderState.Failed;
                return;
            }

            started = Time.realtimeSinceStartup;
            State = VoiceRecorderState.Recording;
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
            wav = WavEncoder.FromFloats(samples, clip.channels, clip.frequency);
            Release();
            State = VoiceRecorderState.Ready;
        }

        public void Cancel()
        {
            if (State == VoiceRecorderState.Recording)
            {
                Microphone.End(device);
            }

            Release();
            wav = null;
            State = VoiceRecorderState.Idle;
        }

        public void Tick()
        {
            if (State == VoiceRecorderState.Recording && (!Microphone.IsRecording(device) || Time.realtimeSinceStartup - started >= WavEncoder.MaxSeconds))
            {
                Stop();
            }
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

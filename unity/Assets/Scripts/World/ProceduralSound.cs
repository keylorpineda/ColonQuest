using System;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Every sound of the game, written as math at start-up: no audio files to ship, which keeps the
    /// web build small. Each method makes a mono clip; loops are made seamless with a short crossfade.
    /// </summary>
    public static class ProceduralSound
    {
        public const int Rate = 22050;
        private const double TwoPi = Math.PI * 2.0;

        private static float Midi(int note)
        {
            return 440f * Mathf.Pow(2f, (note - 69) / 12f);
        }

        private static AudioClip ToClip(string name, float[] samples, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < samples.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(samples[i]));
            }

            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }

            var clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Blends the tail of <paramref name="source"/> into its head so the loop has no click.</summary>
        private static float[] Loopable(float[] source, int fade)
        {
            int length = source.Length - fade;
            var result = new float[length];
            Array.Copy(source, result, length);
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                result[i] = source[i] * w + source[length + i] * (1f - w);
            }

            return result;
        }

        // ---------------------------------------------------------------- music

        private struct Tune
        {
            public float Bpm;
            public int Bars;
            public float Pad;
            public float Melody;
            public float Chance;
            public float Decay;
            public bool Bass;
            public int Seed;
        }

        // Four soft chords (C, Am, F, G) in close voicing, so any bar joins the next without a jump.
        private static readonly int[][] Chords =
        {
            new[] { 60, 64, 67 },
            new[] { 57, 60, 64 },
            new[] { 57, 60, 65 },
            new[] { 55, 59, 62 }
        };

        private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };

        public static AudioClip Music(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Store:
                    return MakeTune("Music_Store", new Tune { Bpm = 112f, Bars = 4, Pad = 0.05f, Melody = 0.32f, Chance = 0.75f, Decay = 9f, Bass = true, Seed = 7 });
                case MusicTrack.Bank:
                    return MakeTune("Music_Bank", new Tune { Bpm = 60f, Bars = 4, Pad = 0.14f, Melody = 0.12f, Chance = 0.2f, Decay = 3f, Bass = false, Seed = 11 });
                case MusicTrack.House:
                    return MakeTune("Music_House", new Tune { Bpm = 72f, Bars = 4, Pad = 0.12f, Melody = 0.2f, Chance = 0.35f, Decay = 4f, Bass = false, Seed = 3 });
                default:
                    return MakeTune("Music_Town", new Tune { Bpm = 88f, Bars = 4, Pad = 0.1f, Melody = 0.26f, Chance = 0.55f, Decay = 6f, Bass = false, Seed = 5 });
            }
        }

        private static AudioClip MakeTune(string name, Tune tune)
        {
            float beat = 60f / tune.Bpm;
            float barLength = beat * 4f;
            int length = Mathf.RoundToInt(barLength * tune.Bars * Rate);
            var buffer = new float[length];
            var random = new System.Random(tune.Seed);

            for (int bar = 0; bar < tune.Bars; bar++)
            {
                var chord = Chords[bar % Chords.Length];
                int start = Mathf.RoundToInt(bar * barLength * Rate);
                int span = Mathf.RoundToInt(barLength * Rate);

                // The pad swells in and out inside each bar; the tails wrap around the end of the loop.
                foreach (int note in chord)
                {
                    AddPad(buffer, start, span, Midi(note), tune.Pad);
                }

                if (tune.Bass)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        AddPluck(buffer, start + Mathf.RoundToInt(b * beat * Rate), Midi(chord[0] - 24), 0.2f, 7f);
                    }
                }

                // Eight melody slots per bar; each is played with some chance, from the pentatonic scale.
                for (int step = 0; step < 8; step++)
                {
                    if (random.NextDouble() > tune.Chance)
                    {
                        continue;
                    }

                    int degree = Pentatonic[random.Next(Pentatonic.Length)] + (random.Next(3) == 0 ? 12 : 0);
                    AddPluck(buffer, start + Mathf.RoundToInt(step * beat * 0.5f * Rate), Midi(72 + degree), tune.Melody, tune.Decay);
                }
            }

            return ToClip(name, buffer, 0.6f);
        }

        private static void AddPad(float[] buffer, int start, int span, float frequency, float volume)
        {
            double step = TwoPi * frequency / Rate;
            double phase = 0.0;
            int total = span + span / 3;
            for (int i = 0; i < total; i++)
            {
                float envelope = Mathf.Sin(Mathf.PI * i / total);
                buffer[(start + i) % buffer.Length] += (float)Math.Sin(phase) * volume * envelope;
                phase += step;
            }
        }

        private static void AddPluck(float[] buffer, int start, float frequency, float volume, float decay)
        {
            double step = TwoPi * frequency / Rate;
            double phase = 0.0;
            int length = Rate * 2;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float envelope = Mathf.Exp(-t * decay) * Mathf.Clamp01(i / 90f);
                if (envelope < 0.001f)
                {
                    break;
                }

                float tone = (float)(Math.Sin(phase) + 0.3 * Math.Sin(phase * 2.0) + 0.1 * Math.Sin(phase * 3.0));
                buffer[(start + i) % buffer.Length] += tone * volume * envelope;
                phase += step;
            }
        }

        // ---------------------------------------------------------------- ambience

        /// <summary>People talking in the distance: bursts of voice-like buzz with two vowel resonances.</summary>
        public static AudioClip Crowd()
        {
            const float seconds = 8f;
            const int fade = Rate / 2;
            int length = Mathf.RoundToInt(seconds * Rate) + fade;
            var buffer = new float[length];
            var random = new System.Random(21);

            for (int voice = 0; voice < 8; voice++)
            {
                float f0 = 95f + (float)random.NextDouble() * 130f;
                float formantA = 400f + (float)random.NextDouble() * 400f;
                float formantB = 1100f + (float)random.NextDouble() * 900f;
                float time = (float)random.NextDouble() * 0.5f;

                while (time < seconds + 0.5f)
                {
                    float burst = 0.12f + (float)random.NextDouble() * 0.25f;
                    float glide = 1f + ((float)random.NextDouble() - 0.5f) * 0.25f;
                    AddSyllable(buffer, time, burst, f0 * glide, formantA, formantB);
                    time += burst + 0.05f + (float)random.NextDouble() * (voice % 2 == 0 ? 0.6f : 1.4f);
                }
            }

            return ToClip("Crowd", Loopable(buffer, fade), 0.5f);
        }

        private static void AddSyllable(float[] buffer, float time, float duration, float f0, float formantA, float formantB)
        {
            int start = Mathf.RoundToInt(time * Rate);
            int length = Mathf.RoundToInt(duration * Rate);
            double phase = 0.0;
            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                float t = i / (float)length;
                float envelope = Mathf.Sin(Mathf.PI * t);
                float value = 0f;
                for (int h = 1; h <= 8; h++)
                {
                    float frequency = f0 * h;
                    float gain = Resonance(frequency, formantA, 160f) + 0.6f * Resonance(frequency, formantB, 300f);
                    value += gain * (float)Math.Sin(phase * h) / h;
                }

                buffer[start + i] += value * envelope * 0.25f;
                phase += TwoPi * f0 / Rate;
            }
        }

        private static float Resonance(float frequency, float center, float width)
        {
            float d = (frequency - center) / width;
            return 1f / (1f + d * d);
        }

        /// <summary>Waves: noise that swells and fades, twice per loop, with a hiss at each crest.</summary>
        public static AudioClip Sea()
        {
            const float seconds = 12f;
            const int fade = Rate;
            int length = Mathf.RoundToInt(seconds * Rate) + fade;
            var buffer = new float[length];
            var random = new System.Random(33);
            float low = 0f;
            float lower = 0f;

            for (int i = 0; i < length; i++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.04f;
                lower += (low - lower) * 0.1f;
                float hiss = noise - low;

                float t = i / (float)Rate;
                float swell = Mathf.Pow(0.5f + 0.5f * Mathf.Sin((float)(TwoPi * t / 6f) - 1.2f), 1.6f);
                buffer[i] = (low * 1.4f + lower * 2.5f) * (0.35f + 0.65f * swell) + hiss * 0.5f * swell * swell;
            }

            return ToClip("Sea", Loopable(buffer, fade), 0.7f);
        }

        /// <summary>A river: steady, brighter noise with a slow ripple.</summary>
        public static AudioClip River()
        {
            const float seconds = 6f;
            const int fade = Rate / 2;
            int length = Mathf.RoundToInt(seconds * Rate) + fade;
            var buffer = new float[length];
            var random = new System.Random(45);
            float low = 0f;

            for (int i = 0; i < length; i++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.5f;
                float t = i / (float)Rate;
                float ripple = 0.8f + 0.2f * Mathf.Sin((float)(TwoPi * t * 3f / seconds));
                buffer[i] = low * ripple;
            }

            return ToClip("River", Loopable(buffer, fade), 0.5f);
        }

        // ---------------------------------------------------------------- one shots

        /// <summary>A car going past: an engine hum that rises while it comes and drops once it has gone by.</summary>
        public static AudioClip CarPass(int variant)
        {
            const float seconds = 3.4f;
            int length = Mathf.RoundToInt(seconds * Rate);
            var buffer = new float[length];
            var random = new System.Random(60 + variant);
            float baseFrequency = variant % 2 == 0 ? 62f : 82f;
            double phase = 0.0;
            float rumble = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * t), 2f);
                float doppler = 1.1f - 0.22f * Mathf.SmoothStep(0f, 1f, t);
                phase += TwoPi * baseFrequency * doppler / Rate;

                float engine = 0f;
                for (int h = 1; h <= 6; h++)
                {
                    engine += (float)Math.Sin(phase * h) / h;
                }

                rumble += (((float)random.NextDouble() * 2f - 1f) - rumble) * 0.08f;
                buffer[i] = (engine * 0.5f + rumble * 0.9f) * envelope;
            }

            return ToClip("CarPass" + variant, buffer, 0.7f);
        }

        /// <summary>Three short bird chirps.</summary>
        public static AudioClip Birds(int variant)
        {
            const float seconds = 0.9f;
            int length = Mathf.RoundToInt(seconds * Rate);
            var buffer = new float[length];
            double phase = 0.0;
            float baseFrequency = 2400f + variant * 350f;

            for (int chirp = 0; chirp < 3; chirp++)
            {
                int start = Mathf.RoundToInt((0.05f + chirp * 0.27f) * Rate);
                int span = Mathf.RoundToInt(0.14f * Rate);
                for (int i = 0; i < span && start + i < length; i++)
                {
                    float t = i / (float)span;
                    float frequency = baseFrequency * (1f + 0.5f * Mathf.Sin(t * Mathf.PI) + 0.1f * chirp);
                    phase += TwoPi * frequency / Rate;
                    buffer[start + i] += (float)Math.Sin(phase) * Mathf.Sin(t * Mathf.PI);
                }
            }

            return ToClip("Birds" + variant, buffer, 0.5f);
        }

        /// <summary>One syllable of someone talking: a short buzz with a vowel shape, played at different pitches.</summary>
        public static AudioClip Syllable(int variant)
        {
            const float seconds = 0.11f;
            int length = Mathf.RoundToInt(seconds * Rate);
            var buffer = new float[length];
            float f0 = 150f + variant * 18f;
            float formantA = 350f + variant * 70f;
            double phase = 0.0;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                float envelope = Mathf.Sin(Mathf.PI * t);
                float value = 0f;
                for (int h = 1; h <= 8; h++)
                {
                    value += Resonance(f0 * h, formantA, 200f) * (float)Math.Sin(phase * h) / h;
                }

                buffer[i] = value * envelope;
                phase += TwoPi * f0 * (1f + 0.15f * (0.5f - t)) / Rate;
            }

            return ToClip("Syllable" + variant, buffer, 0.5f);
        }
    }
}

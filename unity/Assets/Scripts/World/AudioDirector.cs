using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World
{
    /// <summary>
    /// Plays the soundscape: the tune of the place, voices in the bank, cars on the street, the sea on
    /// the beach, birds, and the babble of whoever the player talks to. It lives across scenes. What
    /// should be heard comes from <see cref="SoundMix"/>; the sounds themselves from <see cref="ProceduralSound"/>.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const float Master = 0.8f;
        private const float FadePerSecond = 0.6f;
        private const float CarSeconds = 3.4f;

        private static AudioDirector instance;

        private readonly Dictionary<MusicTrack, AudioSource> music = new Dictionary<MusicTrack, AudioSource>();
        private readonly AudioClip[] cars = new AudioClip[2];
        private readonly AudioClip[] birds = new AudioClip[3];
        private readonly AudioClip[] syllables = new AudioClip[8];

        private AudioSource crowd;
        private AudioSource sea;
        private AudioSource river;
        private AudioSource carSource;
        private AudioSource birdSource;
        private AudioSource voiceSource;

        private Transform player;
        private float nextCar = 6f;
        private float nextBirds = 4f;
        private bool muted;
        private Coroutine talking;

        /// <summary>True while all sound is off.</summary>
        public static bool Muted
        {
            get => instance != null && instance.muted;
            set
            {
                if (instance != null)
                {
                    instance.muted = value;
                }
            }
        }

        public static void EnsureCreated()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("AudioDirector");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioDirector>();
        }

        /// <summary>Someone talks: a quick babble of syllables, with a pitch that belongs to the speaker.</summary>
        public static void Talk(string text, string speaker)
        {
            if (instance != null && !instance.muted && !string.IsNullOrEmpty(text))
            {
                instance.StartTalk(text, speaker ?? string.Empty);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            foreach (MusicTrack track in new[] { MusicTrack.Town, MusicTrack.Store, MusicTrack.Bank, MusicTrack.House })
            {
                music[track] = Source("Music_" + track, true);
            }

            crowd = Source("Crowd", true);
            sea = Source("Sea", true);
            river = Source("River", true);
            carSource = Source("Cars", false);
            birdSource = Source("Birds", false);
            voiceSource = Source("Voice", false);
            StartCoroutine(Prepare());
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private AudioSource Source(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = loop ? 0f : 1f;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Makes the clips one per frame so the game never stalls, the tune of the town first.</summary>
        private IEnumerator Prepare()
        {
            Play(music[MusicTrack.Town], ProceduralSound.Music(MusicTrack.Town));
            yield return null;
            Play(sea, ProceduralSound.Sea());
            yield return null;
            Play(river, ProceduralSound.River());
            yield return null;
            Play(crowd, ProceduralSound.Crowd());
            yield return null;
            for (int i = 0; i < cars.Length; i++)
            {
                cars[i] = ProceduralSound.CarPass(i);
                yield return null;
            }

            for (int i = 0; i < birds.Length; i++)
            {
                birds[i] = ProceduralSound.Birds(i);
                yield return null;
            }

            for (int i = 0; i < syllables.Length; i++)
            {
                syllables[i] = ProceduralSound.Syllable(i);
            }

            yield return null;
            Play(music[MusicTrack.Store], ProceduralSound.Music(MusicTrack.Store));
            yield return null;
            Play(music[MusicTrack.Bank], ProceduralSound.Music(MusicTrack.Bank));
            yield return null;
            Play(music[MusicTrack.House], ProceduralSound.Music(MusicTrack.House));
        }

        private static void Play(AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.Play();
        }

        private void Update()
        {
            var levels = SoundMix.Compute(SceneManager.GetActiveScene().name, PlayerPosition());
            float scale = muted ? 0f : Master;
            float step = FadePerSecond * Time.unscaledDeltaTime;

            foreach (var pair in music)
            {
                Fade(pair.Value, pair.Key == levels.Track ? levels.Music * scale : 0f, step);
            }

            Fade(crowd, levels.Crowd * scale, step);
            Fade(sea, levels.Sea * scale, step);
            Fade(river, levels.River * scale, step);

            if (!muted)
            {
                Traffic(levels.Traffic, Time.unscaledDeltaTime);
                Birds(levels.Birds, Time.unscaledDeltaTime);
            }
        }

        private static void Fade(AudioSource source, float target, float step)
        {
            source.volume = Mathf.MoveTowards(source.volume, target, step);
        }

        private Vector2 PlayerPosition()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag(Station.PlayerTag);
                player = found != null ? found.transform : null;
            }

            return player != null ? (Vector2)player.position : Vector2.zero;
        }

        private void Traffic(float level, float dt)
        {
            nextCar -= dt;
            if (nextCar > 0f || level < 0.05f || cars[0] == null)
            {
                return;
            }

            var clip = cars[Random.Range(0, cars.Length)];
            bool toRight = Random.value < 0.5f;
            carSource.panStereo = toRight ? -0.9f : 0.9f;
            carSource.PlayOneShot(clip, Mathf.Clamp01(level * 0.7f) * Master);
            StartCoroutine(Pan(carSource, toRight ? -0.9f : 0.9f, toRight ? 0.9f : -0.9f, CarSeconds));
            nextCar = CarSeconds + Random.Range(2f, 9f) / (0.4f + level);
        }

        private void Birds(float level, float dt)
        {
            nextBirds -= dt;
            if (nextBirds > 0f || level < 0.3f || birds[0] == null)
            {
                return;
            }

            birdSource.panStereo = Random.Range(-0.8f, 0.8f);
            birdSource.PlayOneShot(birds[Random.Range(0, birds.Length)], 0.35f * level * Master);
            nextBirds = Random.Range(6f, 15f);
        }

        private static IEnumerator Pan(AudioSource source, float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds && source != null; t += Time.unscaledDeltaTime)
            {
                source.panStereo = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
        }

        // ---------------------------------------------------------------- talking

        private void StartTalk(string text, string speaker)
        {
            if (syllables[0] == null)
            {
                return;
            }

            if (talking != null)
            {
                StopCoroutine(talking);
            }

            talking = StartCoroutine(TalkRoutine(text, speaker));
        }

        private IEnumerator TalkRoutine(string text, string speaker)
        {
            int hash = 0;
            foreach (char c in speaker)
            {
                hash = hash * 31 + c;
            }

            hash &= 0x7fffffff;
            float basePitch = 0.8f + (hash % 100) / 100f * 0.55f;
            // About one syllable for every three letters, so the babble lasts as long as the text takes to appear.
            int count = Mathf.Clamp(text.Length / 3, 3, 70);
            var random = new System.Random(hash + text.Length);
            voiceSource.volume = 0.6f * Master;

            for (int i = 0; i < count; i++)
            {
                voiceSource.pitch = basePitch * (0.9f + (float)random.NextDouble() * 0.3f);
                voiceSource.PlayOneShot(syllables[random.Next(syllables.Length)]);
                yield return new WaitForSecondsRealtime(i % 5 == 4 ? 0.1f : 0.07f);
            }

            talking = null;
        }
    }
}

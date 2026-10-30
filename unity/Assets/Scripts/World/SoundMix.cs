using UnityEngine;

namespace ColonQuest.World
{
    public enum MusicTrack
    {
        None,
        Town,
        Store,
        Bank,
        House
    }

    /// <summary>How loud each layer of the soundscape should be at one spot. All values are 0 to 1.</summary>
    public struct SoundLevels
    {
        public MusicTrack Track;
        public float Music;
        public float Crowd;
        public float Sea;
        public float River;
        public float Traffic;
        public float Birds;
    }

    /// <summary>
    /// Decides what the player hears from the scene and the position: a soft tune everywhere, people
    /// talking in the bank, shop music in the store, cars on the streets, the sea on the beach and the
    /// river in the east. Pure math so it can be tested; <see cref="AudioDirector"/> plays the result.
    /// </summary>
    public static class SoundMix
    {
        // Centers of the two streets of the town and how far the traffic noise reaches.
        private const float StreetA = 2f;
        private const float StreetB = -12f;
        private const float TrafficReach = 10f;

        private const float BeachStartY = -19f;
        private const float RiverStartX = 18f;

        private static readonly Vector2 Plaza = new Vector2(0f, 9f);

        public static SoundLevels Compute(string scene, Vector2 position)
        {
            switch (scene)
            {
                case SceneNames.Banco:
                    return new SoundLevels { Track = MusicTrack.Bank, Music = 0.12f, Crowd = 0.6f };
                case SceneNames.Tienda:
                    return new SoundLevels { Track = MusicTrack.Store, Music = 0.5f, Crowd = 0.12f };
                case SceneNames.Casa:
                case SceneNames.Vecino:
                    return new SoundLevels { Track = MusicTrack.House, Music = 0.2f };
                case SceneNames.Agencia:
                    return new SoundLevels { Track = MusicTrack.Store, Music = 0.3f, Crowd = 0.15f };
                case SceneNames.Carcel:
                    return new SoundLevels { Track = MusicTrack.Bank, Music = 0.06f, Crowd = 0.3f };
                case SceneNames.Hospital:
                case SceneNames.HospitalArriba:
                    return new SoundLevels { Track = MusicTrack.House, Music = 0.1f, Crowd = 0.35f };
                case SceneNames.Town:
                    return Town(position);
                default:
                    return new SoundLevels { Track = MusicTrack.Town, Music = 0.3f };
            }
        }

        private static SoundLevels Town(Vector2 p)
        {
            float beach = Mathf.Clamp01((BeachStartY - p.y) / (BeachStartY - MapLayout.ShoreY));
            // On the sand the waves are loud; before it there is only a faint murmur of them.
            float sea = beach > 0f
                ? 0.15f + 0.65f * beach
                : 0.12f * Mathf.Clamp01((BeachStartY + 8f - p.y) / 8f);

            float river = 0.7f * Mathf.Clamp01((p.x - RiverStartX) / (MapLayout.RiverXMin - RiverStartX));

            float roadDistance = Mathf.Min(Mathf.Abs(p.y - StreetA), Mathf.Abs(p.y - StreetB));
            float traffic = (0.25f + 0.75f * Mathf.Clamp01(1f - (roadDistance - 1f) / TrafficReach)) * (1f - beach);

            float plaza = 0.25f * Mathf.Clamp01(1f - Vector2.Distance(p, Plaza) / 14f);

            return new SoundLevels
            {
                Track = MusicTrack.Town,
                Music = 0.34f - 0.14f * Mathf.Max(beach, river),
                Crowd = plaza,
                Sea = sea,
                River = river,
                Traffic = traffic,
                Birds = Mathf.Clamp01(1f - Mathf.Max(beach, river / 0.7f))
            };
        }
    }
}

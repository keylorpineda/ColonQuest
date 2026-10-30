using System;
using System.Collections;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// What the player goes through when a car runs them over, in a second and a half: the world slows for an instant at the
    /// hit, the player is thrown away from the car, spins and ends lying on the road with a ring of stars over the head. Then
    /// the hospital takes over. Pure effect: what the hit costs is decided by the finance engine, never here.
    /// </summary>
    public sealed class HitAnimation : MonoBehaviour
    {
        private const float StopSeconds = 0.14f;
        private const float FlightSeconds = 0.45f;
        private const float LyingSeconds = 1.0f;

        private static Sprite dot;

        public static bool Playing { get; private set; }

        /// <summary>Plays the hit on this player. <paramref name="finished"/> runs when the stars fade out.</summary>
        public static void Play(Transform player, Vector2 carPosition, Action finished)
        {
            if (Playing || player == null)
            {
                finished?.Invoke();
                return;
            }

            var host = new GameObject("HitAnimation").AddComponent<HitAnimation>();
            host.StartCoroutine(host.Run(player, carPosition, finished));
        }

        private IEnumerator Run(Transform player, Vector2 carPosition, Action finished)
        {
            Playing = true;
            PlayerController.Frozen = true;
            AudioDirector.Effect(EffectKind.Thud);

            var body = player.GetComponent<Rigidbody2D>();
            var bob = player.Find("Bob");
            var animator = bob != null ? bob.GetComponent<PlayerAnimator>() : null;
            if (animator != null)
            {
                animator.enabled = false;
            }

            float side = player.position.x >= carPosition.x ? 1f : -1f;
            Vector2 away = new Vector2(side, 0.25f).normalized;

            // The instant of the hit: everything slows down.
            Time.timeScale = 0.2f;
            yield return new WaitForSecondsRealtime(StopSeconds);
            Time.timeScale = 1f;

            // Thrown away from the car, turning over in the air.
            for (float t = 0f; t < FlightSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / FlightSeconds;
                if (body != null)
                {
                    body.linearVelocity = away * 7f * (1f - k);
                }

                if (bob != null)
                {
                    bob.localRotation = Quaternion.Euler(0f, 0f, -side * 100f * Mathf.SmoothStep(0f, 1f, k));
                    bob.localPosition = new Vector3(0f, 0.5f * Mathf.Sin(k * Mathf.PI), 0f);
                }

                yield return null;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            // Lying on the road, dizzy: three stars go round the head.
            var stars = MakeStars(player);
            for (float t = 0f; t < LyingSeconds; t += Time.unscaledDeltaTime)
            {
                if (bob != null)
                {
                    bob.localRotation = Quaternion.Euler(0f, 0f, -side * 92f);
                    bob.localPosition = new Vector3(0f, -0.1f, 0f);
                }

                for (int i = 0; i < stars.Length; i++)
                {
                    float angle = t * 7f + i * Mathf.PI * 2f / stars.Length;
                    stars[i].localPosition = new Vector3(Mathf.Cos(angle) * 0.55f, 0.55f + Mathf.Sin(angle) * 0.18f, 0f);
                }

                yield return null;
            }

            foreach (var star in stars)
            {
                Destroy(star.gameObject);
            }

            if (bob != null)
            {
                bob.localRotation = Quaternion.identity;
                bob.localPosition = Vector3.zero;
            }

            if (animator != null)
            {
                animator.enabled = true;
            }

            Playing = false;
            PlayerController.Frozen = false;
            finished?.Invoke();
            Destroy(gameObject);
        }

        private static Transform[] MakeStars(Transform player)
        {
            var renderer = player.GetComponentInChildren<SpriteRenderer>();
            var stars = new Transform[3];
            for (int i = 0; i < stars.Length; i++)
            {
                var go = new GameObject("Star");
                go.transform.SetParent(player, false);
                go.transform.localScale = Vector3.one * 0.22f;
                var sprite = go.AddComponent<SpriteRenderer>();
                sprite.sprite = Dot();
                sprite.color = new Color(1f, 0.85f, 0.2f, 1f);
                if (renderer != null)
                {
                    sprite.sortingLayerID = renderer.sortingLayerID;
                    sprite.sortingOrder = renderer.sortingOrder + 50;
                }

                stars[i] = go.transform;
            }

            return stars;
        }

        private static Sprite Dot()
        {
            if (dot == null)
            {
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size * 0.5f - d)));
                    }
                }

                texture.Apply();
                dot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            }

            return dot;
        }
    }
}

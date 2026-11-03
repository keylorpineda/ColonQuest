using System.Collections;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// The barred gate of the cell the player is locked in. While the sentence lasts it is shut and its collider keeps
    /// the player inside; with the bail paid or the term served it slides open behind the bars beside it. The lock is
    /// static, so it survives the change of scene from the town to the station.
    /// </summary>
    public sealed class CellGate : MonoBehaviour
    {
        private const float SlideSeconds = 0.45f;

        [SerializeField] private Transform leaf;
        [SerializeField] private Collider2D block;
        [SerializeField] private float openOffset = 1.4f;

        private static CellGate current;
        private Coroutine sliding;

        /// <summary>True while the player must stay in the cell.</summary>
        public static bool Locked { get; private set; }

        public void Configure(Transform gateLeaf, Collider2D gateBlock, float slideBy)
        {
            leaf = gateLeaf;
            block = gateBlock;
            openOffset = slideBy;
        }

        /// <summary>Shuts the gate. Animated, it slides across with the slam of the bars.</summary>
        public static void Lock(bool animate)
        {
            Locked = true;
            if (current != null)
            {
                current.Slide(true, animate);
            }
        }

        public static void Unlock(bool animate)
        {
            Locked = false;
            if (current != null)
            {
                current.Slide(false, animate);
            }
        }

        private void OnEnable()
        {
            current = this;
        }

        private void OnDisable()
        {
            if (current == this)
            {
                current = null;
            }
        }

        private void Start()
        {
            Slide(Locked, false);
        }

        private void Slide(bool shut, bool animate)
        {
            if (sliding != null)
            {
                StopCoroutine(sliding);
                sliding = null;
            }

            if (block != null)
            {
                block.enabled = shut;
            }

            float goal = shut ? 0f : openOffset;
            if (!animate || !isActiveAndEnabled)
            {
                SetLeaf(goal);
                return;
            }

            AudioDirector.Effect(shut ? SoundEffectKind.Slam : SoundEffectKind.Keys);
            sliding = StartCoroutine(SlideTo(goal));
        }

        private IEnumerator SlideTo(float goal)
        {
            float start = leaf != null ? leaf.localPosition.x : goal;
            for (float t = 0f; t < SlideSeconds; t += Time.deltaTime)
            {
                SetLeaf(Mathf.Lerp(start, goal, Mathf.SmoothStep(0f, 1f, t / SlideSeconds)));
                yield return null;
            }

            SetLeaf(goal);
            sliding = null;
        }

        private void SetLeaf(float x)
        {
            if (leaf != null)
            {
                var at = leaf.localPosition;
                at.x = x;
                leaf.localPosition = at;
            }
        }
    }
}

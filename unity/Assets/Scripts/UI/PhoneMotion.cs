using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.UI
{
    /// <summary>The small animations of the phone: a list of timed steps that run on real time, so a paused game still moves them.</summary>
    public sealed class PhoneMotion : MonoBehaviour
    {
        private sealed class Step
        {
            public float Delay;
            public float Seconds;
            public float Elapsed;
            public Action<float> Apply;
            public Action Done;
        }

        private readonly List<Step> steps = new List<Step>();

        /// <summary>Calls <paramref name="apply"/> with 0 now and then with the progress, from 0 to 1, after the delay.</summary>
        public void Run(float delay, float seconds, Action<float> apply, Action done = null)
        {
            apply(0f);
            steps.Add(new Step { Delay = delay, Seconds = Mathf.Max(0.01f, seconds), Apply = apply, Done = done });
        }

        public void Clear()
        {
            steps.Clear();
        }

        /// <summary>Starts fast and settles past the end before coming back, like something that lands.</summary>
        public static float Overshoot(float t)
        {
            const float back = 1.4f;
            float x = t - 1f;
            return 1f + x * x * ((back + 1f) * x + back);
        }

        /// <summary>Starts fast and slows down gently.</summary>
        public static float Settle(float t)
        {
            float x = 1f - t;
            return 1f - x * x * x;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var step in steps.ToArray())
            {
                if (!steps.Contains(step))
                {
                    continue;
                }

                if (step.Delay > 0f)
                {
                    step.Delay -= dt;
                    continue;
                }

                step.Elapsed += dt;
                float t = Mathf.Clamp01(step.Elapsed / step.Seconds);
                step.Apply(t);
                if (t >= 1f)
                {
                    steps.Remove(step);
                    step.Done?.Invoke();
                }
            }
        }
    }
}

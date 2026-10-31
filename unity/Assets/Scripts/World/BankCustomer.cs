using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// A person who comes into the bank, runs an errand and leaves: the cash machine, a window, the information desk or a
    /// wait on the chairs first. Every visit picks another errand and other times, so the lobby never repeats the same
    /// loop. Customers have no collider; they never block the player. The teller scenes ask <see cref="IsBeingServed"/>
    /// to make the player wait their turn.
    /// </summary>
    public sealed class BankCustomer : MonoBehaviour
    {
        /// <summary>One stop of an errand: where to stand and for how long, at least and at most.</summary>
        public readonly struct Stop
        {
            public readonly Vector2 Point;
            public readonly float MinSeconds;
            public readonly float MaxSeconds;

            public Stop(Vector2 point, float minSeconds = 0f, float maxSeconds = 0f)
            {
                Point = point;
                MinSeconds = minSeconds;
                MaxSeconds = maxSeconds;
            }
        }

        /// <summary>The door mat of the bank: customers come in and go out there.</summary>
        public static readonly Vector2 Door = new Vector2(0f, -7.6f);

        /// <summary>Where a customer stands in front of window 2 and window 4.</summary>
        public static readonly Vector2 Window2 = new Vector2(-1.8f, 3.0f);
        public static readonly Vector2 Window4 = new Vector2(5.4f, 3.0f);

        private const float Speed = 1.5f;
        private const float ServedRadius = 0.6f;

        private static readonly List<BankCustomer> All = new List<BankCustomer>();

        [SerializeField] private int seed;

        private readonly Queue<Stop> route = new Queue<Stop>();
        private Transform visual;
        private PlayerAnimator animator;
        private System.Random random;
        private Stop current;
        private float waitUntil;
        private bool holding;
        private bool outside;

        public void Configure(int routineSeed)
        {
            seed = routineSeed;
        }

        /// <summary>True when a customer stands at this window and is being served right now.</summary>
        public static bool IsBeingServed(Vector2 window)
        {
            foreach (var customer in All)
            {
                if (customer != null && !customer.outside && customer.holding
                    && Vector2.Distance(customer.transform.position, window) < ServedRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The errands a customer can run, each from the door and back to it. Paths go around the furniture.</summary>
        public static List<Stop[]> Errands()
        {
            var queueIn = new Vector2(-4.6f, 1.8f);
            var queueTop = new Vector2(-4.6f, 3.0f);
            var aisle = new Vector2(1.6f, -3.0f);
            return new List<Stop[]>
            {
                // The cash machine.
                new[] { new Stop(aisle), new Stop(new Vector2(9.9f, -2.6f), 4f, 8f), new Stop(aisle) },

                // A window, after the queue: window 2 pays and window 4 takes deposits.
                new[] { new Stop(aisle), new Stop(queueIn, 1f, 3f), new Stop(queueTop), new Stop(Window2, 5f, 10f), new Stop(new Vector2(-1.8f, 1.8f)), new Stop(aisle) },
                new[] { new Stop(aisle), new Stop(queueIn, 1f, 3f), new Stop(queueTop), new Stop(Window4, 5f, 9f), new Stop(new Vector2(6.4f, 1.8f)), new Stop(new Vector2(8.6f, -2.6f)), new Stop(aisle) },

                // The information desk.
                new[] { new Stop(new Vector2(5.6f, -5.7f), 4f, 7f) },

                // A ticket, a wait on the chairs and then the window.
                new[] { new Stop(new Vector2(-2.6f, -5.2f), 1f, 2f), new Stop(new Vector2(-7.3f, -0.6f), 6f, 12f), new Stop(queueIn, 1f, 2f), new Stop(queueTop), new Stop(Window2, 5f, 9f), new Stop(new Vector2(-1.8f, 1.8f)), new Stop(aisle) },

                // A look at the board of the exchange rate and out.
                new[] { new Stop(new Vector2(-10.6f, -5.5f), 3f, 5f), new Stop(new Vector2(-2.6f, -6.6f)) }
            };
        }

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void Start()
        {
            random = new System.Random(seed * 7919 + 13);
            var bob = transform.Find("Bob");
            if (bob != null)
            {
                animator = bob.GetComponent<PlayerAnimator>();
                visual = bob.Find("Visual");
            }

            // Every customer starts at another moment, so they do not walk in together.
            StayOutside(1f + seed * 3f);
        }

        private void Update()
        {
            if (DayTint.IsNight)
            {
                StayOutside(5f);
                return;
            }

            if (Time.time < waitUntil)
            {
                SetWalking(0f);
                return;
            }

            if (outside)
            {
                StartVisit();
                return;
            }

            if (holding)
            {
                holding = false;
                NextStop();
                return;
            }

            Vector2 position = transform.position;
            Vector2 next = Vector2.MoveTowards(position, current.Point, Speed * Time.deltaTime);
            float dx = next.x - position.x;
            transform.position = next;
            if (visual != null && Mathf.Abs(dx) > 0.0001f)
            {
                visual.localScale = new Vector3(dx > 0f ? 1f : -1f, 1f, 1f);
            }

            SetWalking(1f);
            if ((next - current.Point).sqrMagnitude > 0.0001f)
            {
                return;
            }

            if (current.MaxSeconds > 0f)
            {
                holding = true;
                waitUntil = Time.time + Range(current.MinSeconds, current.MaxSeconds);
                SetWalking(0f);
                return;
            }

            NextStop();
        }

        private void NextStop()
        {
            if (route.Count == 0)
            {
                StayOutside(Range(4f, 14f));
                return;
            }

            current = route.Dequeue();
        }

        private void StartVisit()
        {
            var errands = Errands();
            route.Clear();
            foreach (var stop in errands[random.Next(errands.Count)])
            {
                route.Enqueue(stop);
            }

            route.Enqueue(new Stop(Door));
            outside = false;
            holding = false;
            transform.position = Door;
            SetVisible(true);
            NextStop();
        }

        private void StayOutside(float seconds)
        {
            outside = true;
            holding = false;
            route.Clear();
            transform.position = Door;
            waitUntil = Time.time + seconds;
            SetVisible(false);
            SetWalking(0f);
        }

        private void SetVisible(bool value)
        {
            if (visual != null && visual.gameObject.activeSelf != value)
            {
                visual.gameObject.SetActive(value);
            }
        }

        private float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * Mathf.Max(0f, max - min);
        }

        private void SetWalking(float amount)
        {
            if (animator != null)
            {
                animator.SetSpeed01(amount);
            }
        }
    }
}

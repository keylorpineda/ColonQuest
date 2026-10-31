using System;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>The state of the collectors' car along its patrol. Plain data, so the rules can be tested.</summary>
    public sealed class CollectorPatrol
    {
        public const float StopSeconds = 6f;
        public const float DetectX = 3.2f;
        public const float DetectY = 2.4f;

        public enum Mode
        {
            Driving,
            Waiting,
            Stopped,
            Cooling
        }

        public Mode Current { get; private set; } = Mode.Driving;

        /// <summary>The seconds left in the current wait or stop.</summary>
        public float Timer { get; private set; }

        public float Gap { get; }
        public float Cooldown { get; }

        public CollectorPatrol(float gapSeconds, float cooldownSeconds)
        {
            Gap = gapSeconds;
            Cooldown = cooldownSeconds;
        }

        /// <summary>True when the player is close enough to the car, in both directions, to be stopped in front of it.</summary>
        public static bool IsNear(Vector2 car, Vector2 player)
        {
            return Mathf.Abs(car.x - player.x) <= DetectX && Mathf.Abs(car.y - player.y) <= DetectY;
        }

        /// <summary>The car left the map at the far end: it waits out of sight before coming back.</summary>
        public void LeftTheMap()
        {
            Current = Mode.Waiting;
            Timer = Gap;
        }

        /// <summary>The car stops in front of the player. Only a driving car that is allowed to stop does.</summary>
        public bool TryStop(bool armed, Vector2 car, Vector2 player)
        {
            if (Current != Mode.Driving || !armed || !IsNear(car, player))
            {
                return false;
            }

            Current = Mode.Stopped;
            Timer = StopSeconds;
            return true;
        }

        /// <summary>Moves the waits on. Returns true on the step in which the car may enter the map again.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (Current == Mode.Driving)
            {
                return false;
            }

            Timer -= deltaSeconds;
            if (Timer > 0f)
            {
                return false;
            }

            if (Current == Mode.Stopped)
            {
                Current = Mode.Cooling;
                Timer = Cooldown;
                return false;
            }

            bool wasWaiting = Current == Mode.Waiting;
            Current = Mode.Driving;
            return wasWaiting;
        }
    }

    /// <summary>
    /// The black car with tinted windows that collects what is owed. It patrols the main street from time to
    /// time; if the player is in arrears and ends up in front of it, it stops and raises <see cref="Arrived"/>.
    /// The car never moves money: the overlay asks the finance engine what to do next.
    /// </summary>
    public sealed class CollectorCar : MonoBehaviour
    {
        [SerializeField] private float speed = 3.4f;
        [SerializeField] private float minX = -46f;
        [SerializeField] private float maxX = 66f;

        private CollectorPatrol patrol;
        private Transform player;

        public static CollectorCar Instance { get; private set; }

        /// <summary>Set by the overlay every frame: true while the player owes enough for the collectors to stop.</summary>
        public static bool Armed { get; set; }

        /// <summary>Raised once each time the car stops in front of the player.</summary>
        public static event Action<CollectorCar> Arrived;

        /// <summary>The two men in the car. They wait hidden on the street until the car stops in front of the player.</summary>
        public static readonly string[] MenNames = { "Cobrador_1", "Cobrador_2" };

        /// <summary>While true a stopped car stays stopped: the men are out talking to the player.</summary>
        public static bool Held { get; set; }

        /// <summary>How hard the car brakes when it stops in front of the player, in units per second squared.</summary>
        private const float Brake = 9f;

        public bool IsVisible => patrol != null && patrol.Current != CollectorPatrol.Mode.Waiting;

        /// <summary>True while the car is stopped, so a window can stay open.</summary>
        public bool IsStopped => patrol != null && patrol.Current == CollectorPatrol.Mode.Stopped;

        public void Configure(float travelSpeed, float westX, float eastX)
        {
            speed = travelSpeed;
            minX = westX;
            maxX = eastX;
        }

        private float currentSpeed;

        private void OnEnable()
        {
            patrol = new CollectorPatrol(25f, 40f);
            Instance = this;
            Held = false;
            currentSpeed = speed;
            Traffic.Register(transform);
        }

        private void OnDisable()
        {
            Traffic.Unregister(transform);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag(Station.PlayerTag);
                player = found != null ? found.transform : null;
            }

            float dt = Time.deltaTime;
            if (patrol.Current == CollectorPatrol.Mode.Stopped && currentSpeed > 0f)
            {
                // It brakes on the street instead of stopping dead.
                currentSpeed = Mathf.Max(0f, currentSpeed - Brake * dt);
                SetX(transform.position.x + currentSpeed * dt);
            }

            if (!(Held && patrol.Current == CollectorPatrol.Mode.Stopped) && patrol.Tick(dt))
            {
                SetX(minX);
                SetVisible(true);
            }

            if (patrol.Current == CollectorPatrol.Mode.Driving || patrol.Current == CollectorPatrol.Mode.Cooling)
            {
                // It obeys the lights like any car; it only breaks the flow when it stops in front of the player.
                float target = CarRules.TargetSpeed(speed, Traffic.RoomAhead(transform, 1));
                currentSpeed = CarRules.NextSpeed(currentSpeed, target, dt);
                SetX(transform.position.x + currentSpeed * dt);

                if (player != null && patrol.TryStop(Armed, transform.position, player.position))
                {
                    Arrived?.Invoke(this);
                }

                if (transform.position.x > maxX)
                {
                    patrol.LeftTheMap();
                    SetVisible(false);
                }
            }
        }

        private void SetX(float x)
        {
            var at = transform.position;
            at.x = x;
            transform.position = at;
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }

            foreach (var collider in GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = visible;
            }
        }
    }
}

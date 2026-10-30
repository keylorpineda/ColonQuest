using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// How a car on its lane drives: it stops before a crosswalk whose pedestrian light is green, it goes on if it was
    /// already over the line when the light changed, and it keeps its distance from the car ahead. Pure numbers along the
    /// lane (x and a direction of +1 or -1), so the rules can be tested.
    /// </summary>
    public static class CarRules
    {
        /// <summary>Half the length of a car, from its middle to its bumper.</summary>
        public const float HalfLength = 1.3f;

        /// <summary>How far before the stripes the car stops.</summary>
        public const float LineMargin = 0.35f;

        /// <summary>The space left between two cars waiting in a row.</summary>
        public const float MinGap = 0.7f;

        /// <summary>From this much free road ahead the car starts to slow down, and it stops when there is none.</summary>
        public const float BrakeDistance = 2.6f;

        /// <summary>A light further than this does not concern the car yet.</summary>
        public const float LookAhead = 9f;

        /// <summary>How fast the speed changes, in units per second per second.</summary>
        public const float Acceleration = 3.5f;

        /// <summary>The stop line for a car going this way through a crosswalk that covers these x.</summary>
        public static float StopLine(float zoneMinX, float zoneMaxX, int direction)
        {
            return direction > 0 ? zoneMinX - LineMargin : zoneMaxX + LineMargin;
        }

        /// <summary>The road between the front bumper and a point ahead; negative once the bumper is past it.</summary>
        public static float RoomTo(float carX, int direction, float pointX)
        {
            float bumper = carX + direction * HalfLength;
            return (pointX - bumper) * direction;
        }

        /// <summary>
        /// True when the car must stop at this line: the pedestrians have green, the line is close, and the bumper has not
        /// passed it yet. A car already over the line when the light changes clears the crosswalk.
        /// </summary>
        public static bool MustStop(float roomToLine, bool walkLight)
        {
            return walkLight && roomToLine >= -0.05f && roomToLine <= LookAhead;
        }

        /// <summary>The free road behind the car ahead in the same lane, or infinity when that car is not ahead.</summary>
        public static float RoomBehind(float carX, int direction, float aheadX)
        {
            float ahead = (aheadX - carX) * direction;
            return ahead > 0f ? ahead - 2f * HalfLength - MinGap : float.PositiveInfinity;
        }

        /// <summary>The speed the car aims at with this much free road ahead.</summary>
        public static float TargetSpeed(float cruise, float room)
        {
            if (room <= 0f)
            {
                return 0f;
            }

            return room >= BrakeDistance ? cruise : cruise * room / BrakeDistance;
        }

        /// <summary>The next speed: it moves toward the target without jumps.</summary>
        public static float NextSpeed(float current, float target, float deltaSeconds)
        {
            return Mathf.MoveTowards(current, target, Acceleration * deltaSeconds);
        }
    }

    /// <summary>The cars driving in the scene, so each one can see the lights and the car ahead of it.</summary>
    public static class Traffic
    {
        /// <summary>Two cars this close in height share a lane.</summary>
        private const float LaneTolerance = 0.6f;

        private static readonly List<Transform> cars = new List<Transform>();

        public static void Register(Transform car)
        {
            if (!cars.Contains(car))
            {
                cars.Add(car);
            }
        }

        public static void Unregister(Transform car)
        {
            cars.Remove(car);
        }

        /// <summary>The free road ahead of this car: up to the next light it must stop at, or up to the car ahead.</summary>
        public static float RoomAhead(Transform self, int direction)
        {
            Vector2 at = self.position;
            float room = float.PositiveInfinity;

            foreach (var signal in CrossingSignal.All)
            {
                if (!signal.CoversLane(at.y))
                {
                    continue;
                }

                float line = CarRules.StopLine(signal.ZoneMinX, signal.ZoneMaxX, direction);
                float toLine = CarRules.RoomTo(at.x, direction, line);
                if (CarRules.MustStop(toLine, signal.WalkLight))
                {
                    room = Mathf.Min(room, toLine);
                }
            }

            foreach (var other in cars)
            {
                if (other == null || other == self || !other.gameObject.activeInHierarchy || Mathf.Abs(other.position.y - at.y) > LaneTolerance)
                {
                    continue;
                }

                room = Mathf.Min(room, CarRules.RoomBehind(at.x, direction, other.position.x));
            }

            return room;
        }
    }
}

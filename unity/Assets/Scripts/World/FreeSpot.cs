using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonQuest.World
{
    /// <summary>
    /// Finds where the player can stand next to something solid. The scenes aim at the middle of a station, and the middle
    /// of many stations is the furniture itself; walking there pushes against it and ends inside it. The search looks in
    /// rings around the point, closest first, and in each ring it tries below first: in this top-down view the front of
    /// a piece of furniture faces down the screen.
    /// </summary>
    public static class FreeSpot
    {
        public const float RingStep = 0.15f;
        public const float MaxDistance = 3f;
        public const int Directions = 16;

        /// <summary>Extra room around the player's feet, so the spot is not touching the furniture.</summary>
        private const float Clearance = 0.04f;

        private static readonly List<Collider2D> Hits = new List<Collider2D>();

        /// <summary>The closest free spot for this collider (the player's feet) around a point, or the point itself.</summary>
        public static Vector2 Near(Vector2 point, Collider2D feet)
        {
            if (feet == null)
            {
                return point;
            }

            Vector2 offset = feet.offset;
            float radius = Clearance + (feet is CircleCollider2D circle ? circle.radius : Mathf.Max(feet.bounds.extents.x, feet.bounds.extents.y));
            var body = feet.attachedRigidbody;
            return Search(point, p => IsFree(p + offset, radius, body));
        }

        /// <summary>The search itself, with the test of a spot given, so it can be checked without physics.</summary>
        public static Vector2 Search(Vector2 point, Func<Vector2, bool> free)
        {
            if (free(point))
            {
                return point;
            }

            foreach (var candidate in Candidates(point))
            {
                if (free(candidate))
                {
                    return candidate;
                }
            }

            return point;
        }

        /// <summary>Every spot tried after the point itself, in order: by ring, and inside a ring from below to above.</summary>
        public static IEnumerable<Vector2> Candidates(Vector2 point)
        {
            for (float distance = RingStep; distance <= MaxDistance + 0.001f; distance += RingStep)
            {
                yield return point + Direction(0) * distance;
                for (int i = 1; i < Directions / 2; i++)
                {
                    yield return point + Direction(i) * distance;
                    yield return point + Direction(-i) * distance;
                }

                yield return point + Direction(Directions / 2) * distance;
            }
        }

        /// <summary>Direction number 0 points down; positive numbers turn toward the right, negative toward the left.</summary>
        private static Vector2 Direction(int index)
        {
            float angle = -90f + index * (360f / Directions);
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static bool IsFree(Vector2 center, float radius, Rigidbody2D self)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            Hits.Clear();
            Physics2D.OverlapCircle(center, radius, filter, Hits);
            foreach (var hit in Hits)
            {
                if (hit != null && (self == null || hit.attachedRigidbody != self))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

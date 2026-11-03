using System;
using System.IO;
using UnityEngine;

namespace ColonQuest.EditorTools
{
    /// <summary>The body styles of the cars on the street.</summary>
    public enum CarShape
    {
        Hatchback,
        Sedan,
        Suv,
        Pickup
    }

    /// <summary>
    /// The cars, drawn in the inked cel style of the town. The body is in tones of grey so one sprite takes any paint color;
    /// the glass and the wheels are their own sprites. Coordinates are units around the middle of the car, nose to the right.
    /// </summary>
    public static partial class SpriteFactory
    {
        public const int CarWidth = 512;
        public const int CarHeight = 288;
        public const int CarPixelsPerUnit = 160;

        /// <summary>How far above the car's origin the middle of its sprites is.</summary>
        public const float CarCenterY = 0.1f;

        public const float WheelX = 0.8f;
        public const float WheelY = -0.36f;
        public const float WheelSize = 0.62f;

        private const float ArchRadius = 0.38f;
        private const float InkWidth = 0.032f;

        public const string CarWheelPath = Folder + "/CarWheel.png";

        private static readonly Color CarInk = new Color(0.08f, 0.09f, 0.12f, 1f);

        private sealed class CarDrawing
        {
            public Vector2[] Body;
            public Vector2[][] Panes;
            public float[] Seams;
            public float Belt;
            public float HandleX;
        }

        private static CarDrawing DrawingOf(CarShape shape)
        {
            switch (shape)
            {
                case CarShape.Sedan:
                    return new CarDrawing
                    {
                        Body = new[] { V(-1.32f, -0.42f), V(1.34f, -0.42f), V(1.40f, -0.22f), V(1.38f, -0.04f), V(1.22f, 0.08f), V(0.70f, 0.18f), V(0.30f, 0.56f), V(-0.46f, 0.60f), V(-0.86f, 0.24f), V(-1.30f, 0.18f), V(-1.38f, 0.04f), V(-1.38f, -0.26f) },
                        Panes = new[]
                        {
                            new[] { V(0.58f, 0.20f), V(0.28f, 0.50f), V(-0.10f, 0.54f), V(-0.10f, 0.20f) },
                            new[] { V(-0.17f, 0.20f), V(-0.17f, 0.54f), V(-0.44f, 0.54f), V(-0.72f, 0.22f) }
                        },
                        Seams = new[] { 0.52f, -0.14f, -0.72f },
                        Belt = 0.14f,
                        HandleX = -0.28f
                    };
                case CarShape.Suv:
                    return new CarDrawing
                    {
                        Body = new[] { V(-1.30f, -0.42f), V(1.30f, -0.42f), V(1.36f, -0.20f), V(1.34f, 0.06f), V(1.20f, 0.18f), V(0.74f, 0.28f), V(0.46f, 0.72f), V(-1.10f, 0.76f), V(-1.26f, 0.50f), V(-1.34f, 0.10f), V(-1.34f, -0.26f) },
                        Panes = new[]
                        {
                            new[] { V(0.62f, 0.30f), V(0.42f, 0.66f), V(-0.04f, 0.68f), V(-0.04f, 0.30f) },
                            new[] { V(-0.11f, 0.30f), V(-0.11f, 0.68f), V(-0.62f, 0.70f), V(-0.62f, 0.30f) },
                            new[] { V(-0.69f, 0.30f), V(-0.69f, 0.70f), V(-1.04f, 0.70f), V(-1.18f, 0.34f) }
                        },
                        Seams = new[] { 0.56f, -0.08f, -0.66f },
                        Belt = 0.24f,
                        HandleX = -0.22f
                    };
                case CarShape.Pickup:
                    return new CarDrawing
                    {
                        Body = new[] { V(-1.36f, -0.42f), V(1.32f, -0.42f), V(1.38f, -0.20f), V(1.36f, 0.02f), V(1.20f, 0.14f), V(0.76f, 0.22f), V(0.48f, 0.64f), V(-0.18f, 0.66f), V(-0.28f, 0.20f), V(-1.36f, 0.20f), V(-1.40f, 0.06f), V(-1.40f, -0.26f) },
                        Panes = new[]
                        {
                            new[] { V(0.62f, 0.24f), V(0.44f, 0.58f), V(-0.12f, 0.60f), V(-0.18f, 0.24f) }
                        },
                        Seams = new[] { 0.56f, -0.24f, -0.34f },
                        Belt = 0.16f,
                        HandleX = -0.12f
                    };
                default:
                    return new CarDrawing
                    {
                        Body = new[] { V(-1.30f, -0.42f), V(1.32f, -0.42f), V(1.38f, -0.20f), V(1.36f, -0.02f), V(1.22f, 0.10f), V(0.66f, 0.20f), V(0.26f, 0.62f), V(-0.70f, 0.66f), V(-1.02f, 0.50f), V(-1.26f, 0.18f), V(-1.36f, 0.02f), V(-1.36f, -0.26f) },
                        Panes = new[]
                        {
                            new[] { V(0.54f, 0.22f), V(0.24f, 0.56f), V(-0.14f, 0.58f), V(-0.14f, 0.22f) },
                            new[] { V(-0.21f, 0.22f), V(-0.21f, 0.58f), V(-0.66f, 0.60f), V(-0.94f, 0.26f), V(-0.94f, 0.22f) }
                        },
                        Seams = new[] { 0.50f, -0.18f, -0.90f },
                        Belt = 0.15f,
                        HandleX = -0.32f
                    };
            }
        }

        private static Vector2 V(float x, float y)
        {
            return new Vector2(x, y);
        }

        public static string CarBodyPath(CarShape shape)
        {
            return Folder + "/CarBody_" + shape + ".png";
        }

        public static string CarGlassPath(CarShape shape)
        {
            return Folder + "/CarGlass_" + shape + ".png";
        }

        /// <summary>Draws the car sprites that are missing, without touching the rest of the art.</summary>
        public static void EnsureCarsGenerated()
        {
            Directory.CreateDirectory(Folder);
            foreach (CarShape shape in Enum.GetValues(typeof(CarShape)))
            {
                var drawing = DrawingOf(shape);
                if (!File.Exists(CarBodyPath(shape)))
                {
                    WriteColor(CarBodyPath(shape), CarWidth, CarHeight, CarPixelsPerUnit, 0, (x, y) => CarBodyPixel(drawing, CarPoint(x, y)));
                }

                if (!File.Exists(CarGlassPath(shape)))
                {
                    WriteColor(CarGlassPath(shape), CarWidth, CarHeight, CarPixelsPerUnit, 0, (x, y) => CarGlassPixel(drawing, CarPoint(x, y)));
                }
            }

            if (!File.Exists(CarWheelPath))
            {
                WriteColor(CarWheelPath, 128, 128, 128, 0, CarWheelPixel);
            }
        }

        private static Vector2 CarPoint(float x, float y)
        {
            return new Vector2((x - CarWidth * 0.5f) / CarPixelsPerUnit, (y - CarHeight * 0.5f) / CarPixelsPerUnit + CarCenterY);
        }

        /// <summary>Distance to the edge of a polygon: negative inside.</summary>
        private static float PolygonDistance(Vector2[] points, Vector2 p)
        {
            float best = Vector2.Dot(p - points[0], p - points[0]);
            float sign = 1f;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i, i++)
            {
                Vector2 edge = points[j] - points[i];
                Vector2 toPoint = p - points[i];
                Vector2 closest = toPoint - edge * Mathf.Clamp01(Vector2.Dot(toPoint, edge) / Vector2.Dot(edge, edge));
                best = Mathf.Min(best, Vector2.Dot(closest, closest));
                bool a = p.y >= points[i].y;
                bool b = p.y < points[j].y;
                bool c = edge.x * toPoint.y > edge.y * toPoint.x;
                if ((a && b && c) || (!a && !b && !c))
                {
                    sign = -sign;
                }
            }

            return sign * Mathf.Sqrt(best);
        }

        private static Color CarBodyPixel(CarDrawing car, Vector2 p)
        {
            float edge = PolygonDistance(car.Body, p) - 0.02f;
            float coverage = Mathf.Clamp01(0.5f - edge * CarPixelsPerUnit);
            if (coverage <= 0f)
            {
                return Color.clear;
            }

            // The windows are holes: the glass and whoever sits behind it are drawn under the body.
            foreach (var pane in car.Panes)
            {
                float inside = PolygonDistance(pane, p);
                if (inside < 0f)
                {
                    return Color.clear;
                }
            }

            // The arches: black inside, with an inked lip.
            float arch = Mathf.Min(Vector2.Distance(p, V(WheelX, WheelY)), Vector2.Distance(p, V(-WheelX, WheelY))) - ArchRadius;
            if (arch < 0f)
            {
                return Tone(0.04f, coverage);
            }

            if (edge > -InkWidth || arch < InkWidth * 0.8f)
            {
                return new Color(CarInk.r, CarInk.g, CarInk.b, coverage);
            }

            float tone = 0.88f;
            if (p.y > car.Belt + 0.02f)
            {
                // The light of the sky along the roof.
                tone = edge > -0.1f ? 1f : 0.82f;
            }
            else if (p.y > car.Belt - 0.09f)
            {
                tone = 1f;
            }
            else if (p.y < -0.13f)
            {
                tone = 0.72f;
            }

            if (Mathf.Abs(p.y - car.Belt) < 0.012f)
            {
                tone = 0.5f;
            }

            // Dark plastic: the sill and the two bumpers.
            bool bumper = (p.x > 1.14f || p.x < -1.16f) && p.y < -0.1f;
            if (p.y < -0.31f || bumper)
            {
                tone = 0.26f;
            }
            else
            {
                foreach (float seam in car.Seams)
                {
                    if (Mathf.Abs(p.x - seam) < 0.009f && p.y < car.Belt + 0.02f)
                    {
                        tone = 0.42f;
                    }
                }

                bool handle = Mathf.Abs(p.x - car.HandleX) < 0.075f && Mathf.Abs(p.y - (car.Belt - 0.07f)) < 0.018f;
                if (handle)
                {
                    tone = 0.34f;
                }

                // A little shade around each arch rounds the fender.
                if (arch < 0.06f)
                {
                    tone *= 0.84f;
                }
            }

            return Tone(tone, coverage);
        }

        private static Color Tone(float tone, float alpha)
        {
            return new Color(tone, tone, tone, alpha);
        }

        private static Color CarGlassPixel(CarDrawing car, Vector2 p)
        {
            float edge = float.MaxValue;
            foreach (var pane in car.Panes)
            {
                edge = Mathf.Min(edge, PolygonDistance(pane, p));
            }

            float coverage = Mathf.Clamp01(0.5f - edge * CarPixelsPerUnit);
            if (coverage <= 0f)
            {
                return Color.clear;
            }

            if (edge > -0.022f)
            {
                return new Color(CarInk.r, CarInk.g, CarInk.b, coverage);
            }

            // Blue going pale toward the sky, with two slanted reflections.
            var glass = Color.Lerp(new Color(0.30f, 0.50f, 0.66f), new Color(0.66f, 0.86f, 0.95f), Mathf.InverseLerp(0.15f, 0.72f, p.y));
            float slant = Mathf.Repeat(p.x + p.y * 0.8f, 0.72f);
            if (slant < 0.11f || (slant > 0.17f && slant < 0.2f))
            {
                glass = Color.Lerp(glass, Color.white, 0.5f);
            }

            glass.a = 0.8f * coverage;
            return glass;
        }

        private static Color CarWheelPixel(float x, float y)
        {
            Vector2 p = new Vector2(x - 64f, y - 64f) / 64f;
            float r = p.magnitude;
            float coverage = Mathf.Clamp01((1f - r) * 64f);
            if (coverage <= 0f)
            {
                return Color.clear;
            }

            float angle = Mathf.Atan2(p.y, p.x);
            if (r > 0.6f)
            {
                // Rubber with the blocks of the tread, so the turning shows.
                bool block = r > 0.88f && Mathf.Repeat(angle * 16f / Mathf.PI, 2f) < 1f;
                return Tone(block ? 0.09f : 0.15f, coverage);
            }

            if (r > 0.55f)
            {
                return new Color(CarInk.r, CarInk.g, CarInk.b, 1f);
            }

            if (r < 0.15f)
            {
                return Tone(0.42f, 1f);
            }

            // Five spokes on a dark rim, lit from above.
            float light = Mathf.Lerp(0.72f, 0.96f, p.y * 0.5f + 0.5f);
            bool spoke = Mathf.Abs(Mathf.Repeat(angle / (Mathf.PI * 2f) * 5f, 1f) - 0.5f) < 0.2f * (0.22f / Mathf.Max(0.22f, r)) + 0.04f;
            return Tone(spoke || r > 0.47f ? light : 0.2f, 1f);
        }
    }
}
